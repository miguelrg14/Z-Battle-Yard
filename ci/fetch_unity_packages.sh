#!/usr/bin/env bash
# Gets the source of the UPM packages needed by the offline compile check (versions from packages-lock.json).
# Unity 6 ships some packages (uGUI, Test Framework) inside the editor: they are copied from the editor's
# BuiltInPackages folder (second argument). The rest are downloaded from Unity's package registry.
set -euo pipefail
out="${1:-packages}"
builtin="$(realpath "${2:-unity/BuiltInPackages}")"
mkdir -p "$out"
packages=$(python3 - <<'PY'
import json
deps = json.load(open("Packages/packages-lock.json"))["dependencies"]
names = ["com.unity.ugui", "com.unity.inputsystem", "com.unity.test-framework", "com.unity.ext.nunit"]
print(" ".join(name + "@" + deps[name]["version"] for name in names))
PY
)
cd "$out"
for package in $packages; do
  name="${package%@*}"
  if [ -d "$builtin/$name" ]; then
    echo "Using built-in $name"
    cp -r "$builtin/$name" "$name"
    continue
  fi
  echo "Downloading $package"
  archive=$(npm pack "$package" --registry https://packages.unity.com | tail -1)
  mkdir -p "$name"
  tar -xzf "$archive" -C "$name"
  rm "$archive"
done
ls
