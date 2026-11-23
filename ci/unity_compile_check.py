#!/usr/bin/env python3
"""
Compile-check the project's C# code against the real Unity engine assemblies, without a Unity licence.

The Unity editor needs an activated licence even in batch mode. To still get fast feedback on every push,
this script mimics Unity's script compilation:

  1. Unity engine/editor reference assemblies are copied out of the GameCI editor Docker image
     (the CI workflow does that and passes the folder with --unity-data).
  2. The UPM packages the code depends on (uGUI + TextMeshPro, Input System, Test Framework, NUnit)
     are downloaded from Unity's package registry as source (the workflow passes the folder with --packages).
  3. Every assembly definition is compiled with Roslyn (csc from the .NET SDK) in dependency order,
     using the same language version and the main scripting defines Unity uses.

It is a type check, not a full Unity build: it catches compile errors (missing usings, editor-only APIs in
runtime code, wrong references between assemblies) in seconds. The licensed Unity jobs then run the real
build and the EditMode/PlayMode tests.
"""
import argparse
import glob
import json
import os
import re
import subprocess
import sys

LANG_VERSION = "9.0"


def read_unity_version(project="."):
    try:
        with open(os.path.join(project, "ProjectSettings", "ProjectVersion.txt"), encoding="utf-8") as f:
            match = re.search(r"m_EditorVersion: (\d+)\.(\d+)\.(\d+)", f.read())
            return ".".join(match.groups()) if match else "6000.0.0"
    except OSError:
        return "6000.0.0"


def version_defines():
    defines = []
    for year in range(2017, 2024):
        for minor in range(1, 5):
            defines.append(f"UNITY_{year}_{minor}_OR_NEWER")
        defines.append(f"UNITY_{year}_OR_NEWER")
    major, minor = (int(part) for part in read_unity_version().split(".")[:2])
    for m in range(0, minor + 1):
        defines.append(f"UNITY_{major}_{m}_OR_NEWER")
    defines += [f"UNITY_{major}", f"UNITY_{major}_{minor}"]
    return defines


BASE_DEFINES = version_defines() + [
    "UNITY_STANDALONE", "UNITY_STANDALONE_WIN", "UNITY_64",
    "ENABLE_INPUT_SYSTEM", "ENABLE_LEGACY_INPUT_MANAGER",
    "ENABLE_MONO", "ENABLE_PHYSICS", "ENABLE_UNITYEVENTS", "ENABLE_UNITYWEBREQUEST",
    "UNITY_UGP_API", "UNITY_ASSERTIONS", "PLATFORM_STANDALONE", "PLATFORM_STANDALONE_WIN",
    "NET_STANDARD", "NET_STANDARD_2_0", "NET_STANDARD_2_1", "NETSTANDARD", "NETSTANDARD2_1",
    "CSHARP_7_3_OR_NEWER", "CSHARP_7_OR_LATER", "UNITY_INCLUDE_TESTS",
    "PACKAGE_INPUTSYSTEM_EXISTS",
]
EDITOR_DEFINES = ["UNITY_EDITOR", "UNITY_EDITOR_64", "UNITY_EDITOR_LINUX"]


UNITY_VERSION = read_unity_version()


def parse_version(text):
    numbers = re.findall(r"\d+", text)
    return tuple(int(n) for n in numbers[:3]) + (0,) * (3 - len(numbers[:3]))


def satisfies(version, expression):
    """Very small subset of Unity's versionDefines expression syntax: "1.2.3", "[a,b)", "(a,b]"..."""
    expression = (expression or "").strip()
    if not expression:
        return True
    v = parse_version(version)
    if expression[0] in "[(":
        low, high = (part.strip() for part in expression[1:-1].split(","))
        if low and (v < parse_version(low) if expression[0] == "[" else v <= parse_version(low)):
            return False
        if high and (v > parse_version(high) if expression[-1] == "]" else v >= parse_version(high)):
            return False
        return True
    return v >= parse_version(expression)


def resolve_version_defines(entries, installed):
    defines = []
    for entry in entries:
        name = entry.get("name", "")
        if name == "Unity":
            version = UNITY_VERSION
        elif name.startswith("com.unity.modules."):
            version = "1.0.0"
        elif name in installed:
            version = installed[name]
        else:
            continue
        if satisfies(version, entry.get("expression", "")):
            defines.append(entry["define"])
    return defines


def find_csc():
    sdk_roots = subprocess.run(["dotnet", "--list-sdks"], capture_output=True, text=True, check=True).stdout.split("\n")
    for line in reversed([l for l in sdk_roots if l.strip()]):
        version, path = line.split(" [", 1)
        csc = os.path.join(path.rstrip("]"), version, "Roslyn", "bincore", "csc.dll")
        if os.path.exists(csc):
            return csc
    sys.exit("csc.dll not found in the .NET SDK")


class Assembly:
    def __init__(self, asmdef_path):
        with open(asmdef_path, encoding="utf-8-sig") as f:
            data = json.load(f)
        self.path = asmdef_path
        self.dir = os.path.dirname(asmdef_path)
        self.name = data["name"]
        self.references = data.get("references", [])
        self.guid = None
        meta = asmdef_path + ".meta"
        if os.path.exists(meta):
            with open(meta, encoding="utf-8") as f:
                match = re.search(r"^guid: ([0-9a-f]{32})", f.read(), re.M)
                self.guid = match.group(1) if match else None
        self.include_platforms = data.get("includePlatforms", [])
        self.exclude_platforms = data.get("excludePlatforms", [])
        self.define_constraints = data.get("defineConstraints", [])
        self.version_defines = data.get("versionDefines", [])
        self.precompiled = data.get("precompiledReferences", [])
        self.override_references = data.get("overrideReferences", False)
        self.no_engine = data.get("noEngineReferences", False)
        self.allow_unsafe = data.get("allowUnsafeCode", False)
        self.sources = []

    @property
    def editor_only(self):
        return self.include_platforms == ["Editor"]


def collect_assemblies(roots):
    assemblies = []
    for root in roots:
        for asmdef in glob.glob(os.path.join(root, "**", "*.asmdef"), recursive=True):
            assemblies.append(Assembly(asmdef))
    by_dir = sorted(assemblies, key=lambda a: len(a.dir), reverse=True)
    for root in roots:
        for source in glob.glob(os.path.join(root, "**", "*.cs"), recursive=True):
            if any(part.endswith("~") for part in source.split(os.sep)):
                continue
            owner = next((a for a in by_dir if source.startswith(a.dir + os.sep)), None)
            if owner is not None:
                owner.sources.append(source)
    named = {a.name: a for a in assemblies}
    # References can be written as "GUID:<guid of the .asmdef>"
    for a in assemblies:
        if a.guid:
            named["GUID:" + a.guid] = a
    return named


def compile_assembly(csc, asm, refs, defines, out_dir, nostdlib_refs, extra_sources=()):
    output = os.path.join(out_dir, asm.name + ".dll")
    rsp = os.path.join(out_dir, asm.name + ".rsp")
    sources = list(asm.sources) + list(extra_sources)
    with open(rsp, "w", encoding="utf-8") as f:
        f.write("-target:library\n-nologo\n-nostdlib+\n-noconfig\n-deterministic\n")
        f.write(f"-langversion:{LANG_VERSION}\n-out:\"{output}\"\n-nowarn:0169,0414,0649,0618,0067,0219,0108,0114,1701,1702\n")
        if asm.allow_unsafe:
            f.write("-unsafe\n")
        f.write("-define:" + ";".join(sorted(set(defines))) + "\n")
        for r in nostdlib_refs + refs:
            f.write(f"-r:\"{r}\"\n")
        for s in sources:
            f.write(f"\"{s}\"\n")
    result = subprocess.run(["dotnet", csc, "@" + rsp], capture_output=True, text=True)
    errors = [l for l in result.stdout.splitlines() if ": error " in l]
    return result.returncode == 0, errors, output


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--unity-data", required=True, help="Folder with the editor's Data/Managed and Data/NetStandard")
    parser.add_argument("--packages", required=True, help="Folder with the extracted UPM packages")
    parser.add_argument("--project", default=".")
    parser.add_argument("--out", default="artifacts/compile-check")
    args = parser.parse_args()

    os.makedirs(args.out, exist_ok=True)
    csc = find_csc()

    data = args.unity_data
    netstandard = glob.glob(os.path.join(data, "NetStandard", "ref", "2.1.0", "*.dll"))
    netstandard += glob.glob(os.path.join(data, "NetStandard", "compat", "2.1.0", "shims", "netfx", "*.dll"))
    # Managed/UnityEngine holds both the engine modules (UnityEngine.*) and the editor modules (UnityEditor.*)
    modules = glob.glob(os.path.join(data, "Managed", "UnityEngine", "*.dll"))
    engine = [p for p in modules if os.path.basename(p).startswith("UnityEngine")]
    editor = [p for p in modules if os.path.basename(p).startswith("UnityEditor")]
    if not editor:
        editor = glob.glob(os.path.join(data, "Managed", "UnityEditor.dll"))
    if not netstandard or not engine:
        sys.exit("Unity reference assemblies not found under " + data)

    packages_root = args.packages
    project_assets = os.path.join(args.project, "Assets")

    assemblies = collect_assemblies([packages_root, project_assets])
    with open(os.path.join(args.project, "Packages", "manifest.json"), encoding="utf-8") as f:
        installed = json.load(f)["dependencies"]
    nunit = glob.glob(os.path.join(packages_root, "**", "nunit.framework.dll"), recursive=True)

    # Assemblies we check: our own, plus every package assembly they need (runtime, not editor or tests)
    targets = ["ZBattleYard.Core", "ZBattleYard.Gameplay", "ZBattleYard.Tests.EditMode", "ZBattleYard.Tests.PlayMode"]
    compiled = {}
    failures = []

    aliases = {"Unity.ugui": "UnityEngine.UI"}   # uGUI's assembly in Unity 6 is still named UnityEngine.UI

    def build(name, stack=()):
        if name.startswith("UnityEngine.") and name.endswith("Module"):
            return None     # Engine modules are already referenced
        name = aliases.get(name, name)
        asm = assemblies.get(name)
        if asm is not None:
            name = asm.name
        if name in compiled:
            return compiled[name]
        if asm is None:
            print(f"  ! assembly '{name}' not available, skipping reference")
            compiled[name] = None
            return None
        if name in stack:
            sys.exit("Circular reference: " + " -> ".join(stack + (name,)))
        refs = []
        # uGUI-dependent packages: make sure UnityEngine.UI is referenced even if their reference didn't resolve
        implicit = ["UnityEngine.UI"] if name in ("Unity.TextMeshPro", "Unity.InputSystem") else []
        for ref in list(asm.references) + implicit:
            out = build(ref, stack + (name,))
            if out and out not in refs:
                refs.append(out)
        is_ours = asm.path.startswith(project_assets)
        uses_editor = asm.editor_only or (is_ours and "Tests" in asm.name)
        if not asm.no_engine:
            refs += engine
            if uses_editor:
                refs += editor
        if "nunit.framework.dll" in asm.precompiled and nunit:
            refs.append(nunit[0])
        defines = BASE_DEFINES + resolve_version_defines(asm.version_defines, installed) + (EDITOR_DEFINES if uses_editor else [])
        ok, errors, output = compile_assembly(csc, asm, refs, defines, args.out, netstandard)
        if not ok and not is_ours:
            # Newer Unity packages use engine-internal attributes (e.g. NoAutoStaticsCleanup) that are not in the
            # public reference assemblies. Attributes don't change behaviour, so stub them and retry once.
            missing = sorted({m for e in errors for m in re.findall(r"type or namespace name '(\w+Attribute)'", e)})
            if missing:
                stub = os.path.join(args.out, name + ".AttributeStubs.cs")
                with open(stub, "w", encoding="utf-8") as f:
                    for m in missing:
                        f.write(f"[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)] "
                                f"internal sealed class {m} : System.Attribute {{ }}\n")
                print(f"  ~ {name}: stubbing internal attributes {', '.join(missing)}")
                ok, errors, output = compile_assembly(csc, asm, refs, defines, args.out, netstandard, [stub])
        label = "OK  " if ok else "FAIL"
        print(f"[{label}] {name} ({len(asm.sources)} files)")
        if not ok:
            for e in errors[:200]:
                print("    " + e)
            if is_ours:
                failures.append(name)
            else:
                print(f"    (package assembly {name} failed; this is a limitation of the offline check)")
            compiled[name] = None
            return None
        compiled[name] = output
        return output

    for target in targets:
        build(target)

    # Default assembly (Assembly-CSharp): scripts in Assets without an asmdef, e.g. third-party packs
    loose = [s for s in glob.glob(os.path.join(project_assets, "**", "*.cs"), recursive=True)
             if not any(s.startswith(a.dir + os.sep) for a in assemblies.values() if a.path.startswith(project_assets))
             and f"{os.sep}Editor{os.sep}" not in s]
    if loose:
        class Loose:  # minimal stand-in for an asmdef
            name = "Assembly-CSharp"; sources = loose; allow_unsafe = False
        refs = engine + [p for n, p in compiled.items() if p and assemblies[n].path.startswith(project_assets) and "Tests" not in n]
        ok, errors, _ = compile_assembly(csc, Loose, refs, BASE_DEFINES, args.out, netstandard)
        print(f"[{'OK  ' if ok else 'FAIL'}] Assembly-CSharp ({len(loose)} files)")
        if not ok:
            for e in errors:
                print("    " + e)
            failures.append("Assembly-CSharp")

    if failures:
        print("\nCompile check failed: " + ", ".join(failures))
        sys.exit(1)
    print("\nAll project assemblies compile.")


if __name__ == "__main__":
    main()
