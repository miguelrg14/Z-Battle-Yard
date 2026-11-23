#!/usr/bin/env python3
"""Prints a summary of the NUnit XML files written by the Unity Test Runner (totals and every failure)."""
import glob
import sys
import xml.etree.ElementTree as ET

folder = sys.argv[1] if len(sys.argv) > 1 else "artifacts"
files = sorted(glob.glob(f"{folder}/**/*.xml", recursive=True))
if not files:
    print(f"No test result files in {folder}")
    sys.exit(0)

failed_total = 0
for path in files:
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError as error:
        print(f"{path}: could not parse ({error})")
        continue
    run = root if root.tag == "test-run" else root.find(".//test-run")
    if run is None:
        continue
    print(f"{path}: total={run.get('total')} passed={run.get('passed')} failed={run.get('failed')} "
          f"skipped={run.get('skipped')} result={run.get('result')}")
    for case in root.iter("test-case"):
        status = case.get("result")
        print(f"  [{status}] {case.get('fullname')}")
        if status == "Failed":
            failed_total += 1
            message = case.findtext("./failure/message", default="").strip()
            trace = case.findtext("./failure/stack-trace", default="").strip()
            output = case.findtext("./output", default="").strip()
            print("      " + message.replace("\n", "\n      "))
            if trace:
                print("      " + trace[:1500].replace("\n", "\n      "))
            if output:
                print("      output: " + output[:1500].replace("\n", "\n      "))
print(f"Failed tests: {failed_total}")
