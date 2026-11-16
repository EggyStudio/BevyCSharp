#!/usr/bin/env python3
"""Stands for dotnet in the tests of build/test.py, so they need no dotnet and no suite.

Its tests are 40 of AlphaTests, 35 of BravoTests, 30 cases of a theory of CharlieTests and 5 of
NormTests. Asked for them with --list-tests it lists them, those a filter takes where it is given
one. Asked to run them with no filter, as the suite whole, it hangs,
grows until it is ended, dies with exit code 134, or ends as dotnet test does once MemoryGuard has
stopped its host at the cap, its line in the output or cut from it and in the file the script names,
as BCS_STANDIN says. With a filter, as a part, it passes the tests the
filter takes and writes their results file, holding 120 MB for a second and a half first where
BCS_STANDIN is hold.
"""

import os
import sys
import time

TESTS = ([f"Bevy.Tests.AlphaTests.Draws{i}" for i in range(40)]
         + [f"Bevy.Tests.BravoTests.Answers{i}" for i in range(35)]
         + [f"Bevy.Tests.CharlieTests.Loads(at: {i})" for i in range(30)]
         + [f"Bevy.Tests.NormTests.N_1_{i}" for i in range(5)])


def value(flag):
    args = sys.argv[1:]
    return args[args.index(flag) + 1] if flag in args else None


def tokens(filter_):
    """A filter's parentheses, its & and |, and its clauses, a backslash keeping the character after
    it in a clause, as vstest reads one."""
    found, clause, escaped = [], "", False
    for c in filter_:
        if escaped:
            clause, escaped = clause + c, False
        elif c == "\\":
            escaped = True
        elif c in "()&|":
            if clause:
                found.append(("clause", clause))
            found.append((c, c))
            clause = ""
        else:
            clause += c
    if clause:
        found.append(("clause", clause))
    return found


def clause_takes(name, clause):
    # A test's full name leaves out a theory's arguments, which its display name holds.
    for operator in ("!~", "~", "!=", "="):
        if operator in clause:
            field, wanted = clause.split(operator, 1)
            held = name.split("(")[0] if field == "FullyQualifiedName" else name
            match = wanted in held if "~" in operator else wanted == held
            return match != operator.startswith("!")
    return True


def taken(name, filter_):
    """Whether a filter takes a test, & binding before | and parentheses before both."""
    found = tokens(filter_)

    def either(at):
        value, at = both(at)
        while at < len(found) and found[at][0] == "|":
            other, at = both(at + 1)
            value = value or other
        return value, at

    def both(at):
        value, at = one(at)
        while at < len(found) and found[at][0] == "&":
            other, at = one(at + 1)
            value = value and other
        return value, at

    def one(at):
        if found[at][0] == "(":
            value, at = either(at + 1)
            return value, at + 1
        return clause_takes(name, found[at][1]), at + 1

    return either(0)[0]


def main():
    if "--list-tests" in sys.argv:
        print("The following Tests are available:")
        for name in TESTS:
            if value("--filter") is None or taken(name, value("--filter")):
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
        elif mode in ("cap", "cut"):
            # The guard's line, then vstest's account of the host it ended, whose tally of the
            # tests that ran before reads as a pass. Cut, the account quotes a window of the host's
            # output that ends before the line, which the guard wrote to the file the script named.
            said = ("[BevyCSharp] The process holds 3.76 GB of the machine's memory, past its cap of 3.75 GB, so it "
                    "stops here rather than take what the machine has left. The cap is Config.MemoryCap or "
                    "BCS_MEMORY_CAP_GB.")
            if mode == "cap":
                print(said, flush=True)
                print("The active test run was aborted. Reason: Test host process crashed", flush=True)
            else:
                with open(os.environ["BCS_MEMORY_CAP_SAID"], "w", encoding="utf-8") as f:
                    f.write(said + "\n")
                print("The active test run was aborted. Reason: Test host process crashed : 2026-10-26T06:30:00.000000Z  "
                      "INFO bevy_render::renderer: AdapterInfo { name: \"NVIDIA GeForce RTX", flush=True)
            print("Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1", flush=True)
            print("Test Run Aborted.", flush=True)
            return 1

    if os.environ.get("BCS_STANDIN") == "hold":
        # Written a byte a page, so its pages are the process's own, for the script to read.
        held = bytearray(120 * 1024 * 1024)
        for at in range(0, len(held), 4096):
            held[at] = 1
        time.sleep(1.5)
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
