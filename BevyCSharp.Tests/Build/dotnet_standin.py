#!/usr/bin/env python3
"""Stands for dotnet in the tests of build/test.py, so they need no dotnet and no suite.

Its tests are 40 of AlphaTests, 35 of BravoTests, 30 of CharlieTests and 5 of NormTests. Asked for
them with --list-tests it lists them. Asked to run them with no filter, as the suite whole, it hangs,
grows until it is ended, dies with exit code 134, or ends as dotnet test does once MemoryGuard has
stopped its host at the cap, as BCS_STANDIN says. With a filter, as a part, it passes the tests the
filter takes and writes their results file.
"""

import os
import sys
import time

TESTS = ([f"Bevy.Tests.AlphaTests.Draws{i}" for i in range(40)]
         + [f"Bevy.Tests.BravoTests.Answers{i}" for i in range(35)]
         + [f"Bevy.Tests.CharlieTests.Loads{i}" for i in range(30)]
         + [f"Bevy.Tests.NormTests.N_1_{i}" for i in range(5)])


def value(flag):
    args = sys.argv[1:]
    return args[args.index(flag) + 1] if flag in args else None


def clause_takes(name, clause):
    if clause.startswith("FullyQualifiedName!~"):
        return clause[len("FullyQualifiedName!~"):] not in name
    if clause.startswith("FullyQualifiedName~"):
        return clause[len("FullyQualifiedName~"):] in name
    return True


def taken(name, filter_):
    # A filter is clauses joined by | or by &, as the script writes them, never both.
    if "|" in filter_:
        return any(clause_takes(name, clause) for clause in filter_.split("|"))
    return all(clause_takes(name, clause) for clause in filter_.split("&"))


def main():
    if "--list-tests" in sys.argv:
        print("The following Tests are available:")
        for name in TESTS:
            print("    " + name)
        return 0

    filter_ = value("--filter")
    if filter_ is None:
        mode = os.environ.get("BCS_STANDIN", "pass")
        print("  Passed Bevy.Tests.BravoTests.Answers0 [1 ms]", flush=True)
        if mode == "hang":
            time.sleep(600)
        elif mode == "grow":
            # 200 MB a second, up to a gigabyte, so a system where the script cannot read it
            # takes no more before the time limit ends it.
            held = []
            while True:
                if len(held) < 100:
                    held.append(b"x" * (10 * 1024 * 1024))
                time.sleep(0.05)
        elif mode == "die":
            print("the last words of a process about to die", flush=True)
            os._exit(134)
        elif mode == "cap":
            # The guard's line, then vstest's account of the host it ended, whose tally of the
            # tests that ran before reads as a pass.
            print("[BevyCSharp] The process holds 3.76 GB of the machine's memory, past its cap of 3.75 GB, so it "
                  "stops here rather than take what the machine has left. The cap is Config.MemoryCap or "
                  "BCS_MEMORY_CAP_GB.", flush=True)
            print("The active test run was aborted. Reason: Test host process crashed", flush=True)
            print("Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1", flush=True)
            print("Test Run Aborted.", flush=True)
            return 1

    names = [name for name in TESTS if filter_ is None or taken(name, filter_)]
    results = value("--results-directory")
    logger = next(arg for arg in sys.argv if arg.startswith("trx;LogFileName="))
    path = os.path.join(results, logger.split("=", 1)[1])
    rows = "".join(f'<UnitTestResult testName="{name}" outcome="Passed" />' for name in names)
    with open(path, "w", encoding="utf-8") as f:
        f.write('<?xml version="1.0" encoding="utf-8"?><TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">'
                f"<Results>{rows}</Results></TestRun>")
    for name in names:
        print(f"  Passed {name} [1 ms]")
    return 0


if __name__ == "__main__":
    sys.exit(main())
