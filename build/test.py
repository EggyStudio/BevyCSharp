#!/usr/bin/env python3
"""Runs the tests and writes a page of what happened, at most 200 lines.

    build/test.py [PART ...] [--parts] [--no-build]    # every part, or the parts named
    build/test.py --read DIR                           # the page of results already in a folder
    build/test.py --digest DIR ...                     # one page from several systems' digests

The tests are three parts, each a process of its own: `bridge`, the bridge's own tests through
cargo, `renderer`, the same with the renderer's crates, and `suite`, the managed suite through
dotnet test. Each is held to a time and a memory. A process that ends by itself, passing or
failing, is read, the suite from its results file and cargo from what it prints under `failures:`.
One that is lost, by a crash, a hang, its time or its memory, or a bridge that does not build, is
said first on the page. A suite that is lost runs again in parts, each a process under the same
limits, so a part that is lost costs only its own tests. A part of the suite is a run of its test
classes in order, at least a hundred tests, and the last part takes whatever no other names, so no
test falls between two parts.

The page has the run's counts and a line for each part, the lost processes, the failures by cause,
the most frequent first, and the warnings, errors and unlogged lines the output repeated most, where
any repeat. It ends the log between two marking lines, and is written to TestResults/digest.md and digest.json. Under GitHub Actions it is also the
job's summary, with each cause, whole, an error annotation. What the processes print goes to
TestResults/output*.txt. It exits 0 when every test passed and no process was lost.

This is 3DEngine's build/test.py, its parts here the three processes from the start, as NORM.md's
N 6.7 and N 6.8 have it.
"""

import argparse
import json
import os
import platform
import re
import shlex
import signal
import subprocess
import sys
import time
import xml.etree.ElementTree as ET
from collections import Counter

# The page's limits and how it reaches GitHub, which build/step.py shares, imported without a
# compiled copy left beside them in build/.
sys.dont_write_bytecode = True
from page import ANNOTATIONS, LINE_WIDTH, annotate, fit, summarize

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECT = "BevyCSharp.Tests/BevyCSharp.Tests.csproj"
RESULTS = os.path.join(ROOT, "BevyCSharp.Tests", "TestResults")
MANIFEST = "native/Cargo.toml"
NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
PREFIX = "Bevy.Tests."

PARTS = ["bridge", "renderer", "suite"]
LABELS = {"bridge": "the bridge", "renderer": "the bridge's renderer", "suite": "the suite"}

CAUSES_SHOWN = 10
FRAMES_SHOWN = 6
MESSAGE_LINES = 5
TESTS_SHOWN = 4
REPEATED_SHOWN = 3
PART_SIZE = 100
BEGIN = "=" * 30 + " the page " + "=" * 30
END = "=" * 30 + " end of the page " + "=" * 30


# -- Running

class Process:
    """One run of tests in a process of its own, and how it ended."""

    def __init__(self, label, kind, filter_=None):
        self.label, self.kind, self.filter = label, kind, filter_
        self.name = "results-" + re.sub(r"\W+", "-", label).strip("-")
        self.output = self.name.replace("results", "output", 1) + ".txt"
        self.exit_code = None
        self.seconds = 0.0
        self.peak_mb = 0
        self.lost = None          # None, or "crash", "hang", "time", "memory" or "build"
        self.running = []         # the tests it was in, where the blame collector says
        self.after = None         # the last test to end before it was lost
        self.last_lines = []
        self.counts = Counter()
        self.results = {}         # a cargo part's, by test, as a results file holds them

    def summary(self):
        how = LOSSES[self.lost] if self.lost else "ended"
        held = f", {self.peak_mb:,} MB at most" if self.peak_mb else ""
        tests = f"{self.counts['Passed']:,} passed, {self.counts['Failed']:,} failed, {self.counts['NotExecuted']:,} skipped, "
        return f"{self.label}: {tests}{how} after {duration(self.seconds)}{held}, exit code {self.exit_code}, its output in {self.output}"


LOSSES = {"time": "ended at its time limit", "memory": "ended at its memory limit", "hang": "lost to a test that hung",
          "crash": "lost to a crash", "build": "did not build"}


def command_of(process, args, results):
    """What a process runs, cargo for the bridge's parts and dotnet for the suite's."""
    if process.kind == "cargo":
        command = shlex.split(args.cargo, posix=os.name != "nt") + ["test", "--manifest-path", MANIFEST, "--no-fail-fast"]
        if process.filter == "renderer":
            command += ["-p", "bevy_csharp", "--no-default-features", "--features", "render"]
        return command

    command = shlex.split(args.dotnet, posix=os.name != "nt") + ["test", args.project, "-c", args.configuration]
    if args.no_build:
        command.append("--no-build")
    if process.filter:
        command += ["--filter", process.filter]
    return command + ["--results-directory", results,
                      "--logger", f"trx;LogFileName={process.name}.trx",
                      "--logger", "console;verbosity=normal",
                      "--blame", "--blame-hang-timeout", f"{max(1, round(args.hang_minutes * 60))}s",
                      "--blame-hang-dump-type", "none"]


def run(process, args, results):
    command = command_of(process, args, results)
    output_path = os.path.join(results, process.output)
    started = time.monotonic()
    with open(output_path, "w", encoding="utf-8", errors="replace", newline="\n") as output:
        # A cargo part is compiled first, held to the time alone, since compiling the renderer's
        # crates takes rustc past any memory a test process is held to, and is no test process.
        if process.kind == "cargo":
            process.exit_code = watch(command + ["--no-run"], output, process, args, started, memory=False)
            if process.exit_code != 0 and not process.lost:
                process.lost = "build"
        if not process.lost:
            process.exit_code = watch(command, output, process, args, started, memory=True)
    process.seconds = time.monotonic() - started

    text = read_text(output_path)
    if process.kind == "cargo":
        return read_cargo(process, text)

    if process.lost is None:
        if "inactivity time of" in text:
            process.lost = "hang"
        elif "Test Run Aborted" in text or "test run was aborted" in text or not os.path.exists(os.path.join(results, process.name + ".trx")):
            process.lost = "crash"
    process.counts = Counter(outcome for outcome, _, _ in read_results(os.path.join(results, process.name + ".trx")).values())
    if process.lost:
        process.running = tests_running(text)
        process.after = last_ended(text)
        process.last_lines = last_lines(text)
    return process


def watch(command, output, process, args, started, memory):
    """Runs a command into the output, ending it at the time limit, and at the memory limit where asked."""
    options = {"start_new_session": True} if os.name != "nt" else {"creationflags": subprocess.CREATE_NEW_PROCESS_GROUP}
    child = subprocess.Popen(command, stdout=output, stderr=subprocess.STDOUT, cwd=ROOT, **options)
    limit_seconds = args.timeout_minutes * 60
    while child.poll() is None:
        time.sleep(0.25)
        held = largest_in_tree(child.pid) if memory else None
        if held is not None:
            process.peak_mb = max(process.peak_mb, held)
        if time.monotonic() - started > limit_seconds:
            process.lost = "time"
        elif held is not None and held > args.memory_mb:
            process.lost = "memory"
        if process.lost:
            kill_tree(child)
            break
    code = child.wait()
    output.flush()
    return code


def read_cargo(process, text):
    """A cargo part's tests, from the lines it prints for each test and under `failures:`."""
    for name, verdict in re.findall(r"^test (\S+) \.\.\. (ok|FAILED|ignored)", text, re.M):
        process.results[name] = ({"ok": "Passed", "FAILED": "Failed", "ignored": "NotExecuted"}[verdict], "", "")

    # Each failure's own output, between its `---- name stdout ----` and the next such line or the
    # list of failures that ends the section.
    for match in re.finditer(r"^---- (\S+) stdout ----\n(.*?)(?=^---- \S+ stdout ----$|^failures:$|\Z)", text, re.M | re.S):
        name, body = match.group(1), match.group(2)
        if name in process.results:
            process.results[name] = ("Failed", body.strip("\n"), "")

    process.counts = Counter(outcome for outcome, _, _ in process.results.values())
    if process.lost is None and process.exit_code != 0:
        if not re.search(r"^test result: ", text, re.M) or "(signal: " in text:
            # A test binary ended by a signal, which reports no result for the tests it had left.
            process.lost = "crash"
    if process.lost:
        ran = re.findall(r"^test (\S+) \.\.\. ", text, re.M)
        process.after = ran[-1] if ran else None
        errors = [line for line in text.splitlines() if line.startswith("error")][:4]
        process.last_lines = [line[:LINE_WIDTH] for line in (errors or last_lines(text))]
    return process


def last_lines(text):
    """What the process printed last before dotnet test's own account of the loss, and that account's reason."""
    lines = text.splitlines()
    epilogue = ("The active test run was aborted", "Data collector 'Blame'", "Results File:", "Test Run Aborted")
    end = next((i for i, line in enumerate(lines) if line.lstrip().startswith(epilogue)), len(lines))
    # Stack frames left out, since a crash prints dozens after the words that say what it was.
    before = [line for line in lines[:end] if line.strip() and not line.strip().startswith("at ")][-8:]
    reason = [line for line in lines[end:] if "Reason:" in line][:1]
    return [line[:LINE_WIDTH] for line in before + reason]


def tests_running(text):
    """The tests the blame collector names as running when the process was lost, or else the last ones to end."""
    match = re.search(r"The tests? running when the crash occurred:\s*\n(.*?)(?:\nThis test may|\nThese tests may|\Z)", text, re.S)
    if match:
        return [line.strip() for line in match.group(1).splitlines() if line.strip()][:TESTS_SHOWN]
    return []


def last_ended(text):
    ended = re.findall(r"^\s+(?:Passed|Failed|Skipped) (\S+)", text, re.M)
    return ended[-1] if ended else None


def list_tests(args):
    command = shlex.split(args.dotnet, posix=os.name != "nt") + ["test", args.project, "-c", args.configuration, "--list-tests"]
    if args.no_build:
        command.append("--no-build")
    text = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, errors="replace").stdout
    names, listing = [], False
    for line in text.splitlines():
        if "The following Tests are available" in line:
            listing = True
        elif listing and line.startswith("    ") and line.strip():
            names.append(line.strip())
    return names


def parts_of(names):
    """
    The suite in parts, each a run of its test classes in order holding PART_SIZE tests or more, and
    a last part for whatever no other part names. The tests here share one namespace, so a class is
    the unit a part is made of, as a namespace is in 3DEngine's.
    """
    counts = Counter(segment(name) for name in names if segment(name))
    parts, current, held = [], [], 0
    for name in sorted(counts):
        current.append(name)
        held += counts[name]
        if held >= PART_SIZE:
            parts.append(current)
            current, held = [], 0
    if current:
        parts.append(current)

    processes = [Process(f"{group[0]} to {group[-1]}" if len(group) > 1 else group[0], "dotnet",
                         "|".join(f"FullyQualifiedName~{PREFIX}{name}." for name in group)) for group in parts]
    rest = "&".join(f"FullyQualifiedName!~{PREFIX}{name}." for name in counts)
    processes.append(Process("everything else", "dotnet", rest or None))
    return processes


def segment(name):
    return name[len(PREFIX):].split(".")[0].split("(")[0] if name.startswith(PREFIX) else ""


# -- What a process holds

def largest_in_tree(pid):
    """The resident memory of the largest process in the tree under pid, in MB, or None where it cannot be read."""
    try:
        table = process_table()
    except Exception:
        return None
    if not table:
        return None
    tree, frontier = {pid}, [pid]
    while frontier:
        parent = frontier.pop()
        for child, (ppid, _) in table.items():
            if ppid == parent and child not in tree:
                tree.add(child)
                frontier.append(child)
    sizes = [table[p][1] for p in tree if p in table]
    return max(sizes) // (1024 * 1024) if sizes else None


def process_table():
    """Each process's parent and resident bytes."""
    if sys.platform.startswith("linux"):
        table = {}
        for entry in os.listdir("/proc"):
            if not entry.isdigit():
                continue
            try:
                with open(f"/proc/{entry}/status", encoding="ascii", errors="replace") as status:
                    fields = dict(line.split(":", 1) for line in status if ":" in line)
                table[int(entry)] = (int(fields["PPid"]), int(fields.get("VmRSS", "0 kB").split()[0]) * 1024)
            except (OSError, KeyError, ValueError):
                continue
        return table
    if sys.platform == "darwin":
        out = subprocess.run(["ps", "-A", "-o", "pid=,ppid=,rss="], capture_output=True, text=True).stdout
        return {int(p): (int(pp), int(rss) * 1024) for p, pp, rss in (line.split() for line in out.splitlines() if len(line.split()) == 3)}
    if os.name == "nt":
        return windows_process_table()
    return {}


def windows_process_table():
    import ctypes
    from ctypes import wintypes

    class Entry(ctypes.Structure):
        _fields_ = [("dwSize", wintypes.DWORD), ("cntUsage", wintypes.DWORD), ("th32ProcessID", wintypes.DWORD),
                    ("th32DefaultHeapID", ctypes.c_size_t), ("th32ModuleID", wintypes.DWORD), ("cntThreads", wintypes.DWORD),
                    ("th32ParentProcessID", wintypes.DWORD), ("pcPriClassBase", ctypes.c_long), ("dwFlags", wintypes.DWORD),
                    ("szExeFile", ctypes.c_wchar * 260)]

    class Counters(ctypes.Structure):
        _fields_ = [("cb", wintypes.DWORD), ("PageFaultCount", wintypes.DWORD), ("PeakWorkingSetSize", ctypes.c_size_t),
                    ("WorkingSetSize", ctypes.c_size_t), ("QuotaPeakPagedPoolUsage", ctypes.c_size_t),
                    ("QuotaPagedPoolUsage", ctypes.c_size_t), ("QuotaPeakNonPagedPoolUsage", ctypes.c_size_t),
                    ("QuotaNonPagedPoolUsage", ctypes.c_size_t), ("PagefileUsage", ctypes.c_size_t), ("PeakPagefileUsage", ctypes.c_size_t)]

    kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel32.CreateToolhelp32Snapshot.restype = wintypes.HANDLE
    kernel32.OpenProcess.restype = wintypes.HANDLE
    snapshot = kernel32.CreateToolhelp32Snapshot(0x2, 0)
    parents = {}
    entry = Entry()
    entry.dwSize = ctypes.sizeof(Entry)
    more = kernel32.Process32FirstW(snapshot, ctypes.byref(entry))
    while more:
        parents[entry.th32ProcessID] = entry.th32ParentProcessID
        more = kernel32.Process32NextW(snapshot, ctypes.byref(entry))
    kernel32.CloseHandle(snapshot)

    table = {}
    for pid, ppid in parents.items():
        handle = kernel32.OpenProcess(0x1000 | 0x0010, False, pid)
        if not handle:
            continue
        counters = Counters()
        counters.cb = ctypes.sizeof(Counters)
        if kernel32.K32GetProcessMemoryInfo(handle, ctypes.byref(counters), counters.cb):
            table[pid] = (ppid, counters.WorkingSetSize)
        kernel32.CloseHandle(handle)
    return table


def kill_tree(child):
    if os.name == "nt":
        subprocess.run(["taskkill", "/T", "/F", "/PID", str(child.pid)], capture_output=True)
        return
    try:
        table = process_table()
    except Exception:
        table = {}
    tree, frontier = [child.pid], [child.pid]
    while frontier:
        parent = frontier.pop()
        for pid, (ppid, _) in table.items():
            if ppid == parent and pid not in tree:
                tree.append(pid)
                frontier.append(pid)
    try:
        os.killpg(child.pid, signal.SIGKILL)
    except OSError:
        pass
    for pid in tree:
        try:
            os.kill(pid, signal.SIGKILL)
        except OSError:
            pass


# -- Reading results

def read_text(path):
    try:
        with open(path, encoding="utf-8", errors="replace") as f:
            return f.read()
    except OSError:
        return ""


def read_results(path):
    """Each test in a results file, by name, as its outcome, its message and its stack."""
    try:
        run_ = ET.parse(path).getroot()
    except (OSError, ET.ParseError):
        return {}
    results = {}
    for result in run_.iterfind("t:Results/t:UnitTestResult", NS):
        message = result.findtext("t:Output/t:ErrorInfo/t:Message", default="", namespaces=NS)
        stack = result.findtext("t:Output/t:ErrorInfo/t:StackTrace", default="", namespaces=NS)
        results[result.get("testName", "?")] = (result.get("outcome", ""), message, stack)
    return results


def first_line(text):
    for line in (text or "").splitlines():
        if line.strip():
            return line.strip()
    return ""


def plain_of(text):
    """A message's first line with paths and numbers taken out, which tells causes apart."""
    plain = re.sub(r"(?:[A-Za-z]:\\|/)[^\s'\",;)]+", "<path>", text)
    return re.sub(r"0x[0-9A-Fa-f]+|\d+(?:\.\d+)?", "#", plain)[:200]


def cause_of(message, stack):
    """The exception's type, its first line made plain, and the library's first frame, for a failed test of the suite."""
    line = first_line(message)
    match = re.match(r"^([A-Za-z_][\w.`+]*(?:Exception|Error))\s*:\s*(.*)$", line)
    kind, text = (match.group(1), match.group(2)) if match else ("assertion", line)
    frames = [frame_of(l) for l in stack.splitlines() if l.strip().startswith("at Bevy.")]
    library = next((f.split("(")[0] for f in frames if not f.startswith(PREFIX)), "")
    return kind, plain_of(text), library, line, frames[:FRAMES_SHOWN], [l.strip() for l in message.splitlines() if l.strip()]


def panic_of(body):
    """The panic's message made plain and where it was, for a failed test of the bridge."""
    lines = [line.rstrip() for line in body.splitlines() if line.strip() and not line.startswith("note: run with")]
    where = ""
    message = []
    for i, line in enumerate(lines):
        match = re.match(r"^thread '.*?'(?: \(\d+\))? panicked at (.+?):(\d+):\d+:$", line)
        if match:
            where = f"{match.group(1)}:{match.group(2)}"
            message = lines[i + 1:]
            break
    message = message or lines
    line = message[0].strip() if message else ""
    return "panic", plain_of(line), where, line, [], [l.strip() for l in message]


def frame_of(line):
    text = line.strip()[3:]
    match = re.match(r"^(.*?) in (.*):line (\d+)$", text)
    return f"{match.group(1)} in {os.path.basename(match.group(2).replace(chr(92), '/'))}:{match.group(3)}" if match else text


# A line Bevy's log wrote, after the time it was written at, and its level.
LOGGED = re.compile(r"^(?:\d{4}-\d\d-\d\dT[\d:.]+Z\s+)?(TRACE|DEBUG|INFO|WARN|ERROR)\s")


def repeated_lines(texts):
    """
    The lines the output repeated most, as one count for lines that differ only in their numbers.
    Only a line logged as a warning or an error counts, or one with no level, as an exception's
    message is, since a line logged below them repeats by design, as each app's start logs the
    adapter it draws on, and would fill the section where a system that throws in every frame was
    to be seen.
    """
    counts, first = Counter(), {}
    for text in texts:
        for line in text.splitlines():
            # Bevy's log colors its lines, and a page is read where the colors are not.
            stripped = re.sub(r"\x1b\[[0-9;]*m", "", line).strip()
            if (len(stripped) < 12 or stripped.startswith("at ") or re.match(r"^(Passed|Failed|Skipped) ", stripped)
                    or stripped.startswith("[xUnit.net") or re.match(r"^test \S+ \.\.\. ", stripped)
                    or re.match(r"^(Compiling|Running|running|test result:|Doc-tests|Finished|Executable) ", stripped)):
                continue
            logged = LOGGED.match(stripped)
            if logged and logged.group(1) in ("TRACE", "DEBUG", "INFO"):
                continue
            key = re.sub(r"\d+", "#", re.sub(r"^\[\s*[\d.]+s\]\s*", "", stripped))
            counts[key] += 1
            first.setdefault(key, stripped)
    return [(first[key], count) for key, count in counts.most_common(REPEATED_SHOWN) if count > 1]


# -- The page

def digest(results_dir, processes, listed, seconds):
    """What the run comes to, as the page and its annotations read it."""
    # The suite's results are its parts' where it ran again in parts, and its own otherwise.
    suite_parts = any(p.kind == "dotnet" and p.label != "the suite" for p in processes)
    if processes:
        trx = [os.path.join(results_dir, p.name + ".trx") for p in processes
               if p.kind == "dotnet" and (p.label != "the suite" or not suite_parts)]
        outputs = [os.path.join(results_dir, p.output) for p in processes]
    else:
        trx = [os.path.join(results_dir, f) for f in sorted(os.listdir(results_dir)) if f.endswith(".trx")]
        outputs = [os.path.join(results_dir, f) for f in sorted(os.listdir(results_dir)) if f.startswith("output") and f.endswith(".txt")]

    results = {}
    for path in trx:
        results.update((name, (outcome, message, stack, "the suite")) for name, (outcome, message, stack) in read_results(path).items())
    for p in processes:
        if p.kind == "cargo":
            results.update((tested(name, p.label), (outcome, message, stack, p.label)) for name, (outcome, message, stack) in p.results.items())
    if not processes:
        for path in outputs:
            label = os.path.basename(path)[len("output-"):-len(".txt")].replace("-", " ")
            if label in ("the bridge", "the bridge s renderer"):
                cargo = read_cargo(Process(LABELS["bridge"] if label == "the bridge" else LABELS["renderer"], "cargo", None), read_text(path))
                results.update((tested(name, cargo.label), (outcome, message, stack, cargo.label)) for name, (outcome, message, stack) in cargo.results.items())

    outcomes = Counter(outcome for outcome, _, _, _ in results.values())
    causes = {}
    for name, (outcome, message, stack, part) in sorted(results.items()):
        if outcome != "Failed":
            continue
        kind, plain, frame, line, frames, lines = panic_of(message) if part != "the suite" else cause_of(message, stack)
        key = f"{kind}|{plain}|{frame}"
        cause = causes.setdefault(key, {"key": key, "type": kind, "message": line, "lines": lines[:MESSAGE_LINES],
                                        "more_lines": max(0, len(lines) - MESSAGE_LINES), "frame": frame, "frames": frames,
                                        "count": 0, "tests": []})
        cause["count"] += 1
        cause["tests"].append(name)
    # A theory may be listed once by its method's name and report each case by its arguments, so a
    # listed name has a result where any case of its method has one.
    methods = {name.split("(")[0] for name in results}
    no_result = sum(1 for name in set(listed) if name not in results and name.split("(")[0] not in methods)

    return {
        "system": {"Darwin": "macOS"}.get(platform.system(), platform.system()),
        "commit": commit(),
        "passed": outcomes["Passed"], "failed": outcomes["Failed"], "skipped": outcomes["NotExecuted"], "no_result": no_result,
        "seconds": round(seconds), "peak_mb": max((p.peak_mb for p in processes), default=0),
        "processes": [{"label": p.label, "lost": p.lost, "seconds": round(p.seconds), "peak_mb": p.peak_mb, "exit_code": p.exit_code,
                       "running": p.running, "after": p.after, "last_lines": p.last_lines, "summary": p.summary()} for p in processes],
        "causes": sorted(causes.values(), key=lambda c: (-c["count"], c["key"])),
        "repeated": repeated_lines(read_text(path) for path in outputs),
    }


def tested(name, label):
    """A bridge test's name, marked where it ran with the renderer, since both parts run most of the same tests."""
    return f"{name} (renderer)" if label == LABELS["renderer"] else name


def head(d):
    return (f"Tests on {d['system']} at {d['commit']}: {d['passed']:,} passed, {d['failed']:,} failed, {d['skipped']:,} skipped, "
            f"{d['no_result']:,} without a result, in {duration(d['seconds'])}"
            + (f", {d['peak_mb']:,} MB at most" if d["peak_mb"] else ""))


def entry(cause):
    """A cause's message, its frames and its tests with a count of the rest, a line each."""
    shown = cause["tests"][:TESTS_SHOWN]
    more = len(cause["tests"]) - len(shown)
    message = cause.get("lines") or [cause["message"]]
    left = cause.get("more_lines", 0)
    return (message + ([f"({left:,} line{'s' if left != 1 else ''} more)"] if left else [])
            + [f"at {frame}" for frame in cause.get("frames", [])]
            + [", ".join(f"`{t}`" for t in shown) + (f" and {more:,} more" if more > 0 else "")])


def lost_entry(p):
    """A lost process's account, its tests and its last lines, a line each."""
    lines = [p["summary"]]
    if p["running"]:
        lines.append("In " + ", ".join(f"`{t}`" for t in p["running"]))
    elif p.get("after"):
        lines.append(f"After `{p['after']}`, the last test to end")
    return lines + p["last_lines"]


def page(d):
    lines = [f"## {head(d)}", ""]
    lines += [f"- {p['summary']}" for p in d["processes"]]
    lines.append("")
    for p in d["processes"]:
        if not p["lost"]:
            continue
        held = f" holding {p['peak_mb']:,} MB" if p["peak_mb"] else ""
        lines.append(f"### Lost: {p['label']}, {LOSSES[p['lost']]}, after {duration(p['seconds'])}{held}, exit code {p['exit_code']}")
        if p["running"]:
            lines.append("In " + ", ".join(f"`{t}`" for t in p["running"]))
        elif p.get("after"):
            lines.append(f"After `{p['after']}`, the last test to end")
        lines.append("Its last lines:")
        lines += ["    " + line for line in p["last_lines"]]
        lines.append("")

    causes = d["causes"]
    if causes:
        lines.append(f"### {d['failed']:,} failed, of {len(causes)} cause{'s' if len(causes) != 1 else ''}" + (f", the first {CAUSES_SHOWN} here" if len(causes) > CAUSES_SHOWN else ""))
        for cause in causes[:CAUSES_SHOWN]:
            lines.append(f"**{cause['count']:,} × {cause['type']}**" + (f" at `{cause['frame']}`" if cause["frame"] else ""))
            lines += ["    " + line for line in entry(cause)]
            lines.append("")

    if d["repeated"]:
        lines.append("### Repeated most in the output")
        lines += [f"- {count:,} × `{line}`" for line, count in d["repeated"]]
    return fit(lines)


def merged_page(digests):
    lines = ["## Tests by system", ""]
    lines += [f"- {head(d)}" for d in digests] or ["No system wrote a page, so each job ended before its tests did."]
    lines += [f"- Lost on {d['system']}: {p['summary']}" for d in digests for p in d["processes"] if p["lost"]]
    causes = {}
    for d in digests:
        for cause in d["causes"]:
            merged = causes.setdefault(cause["key"], dict(cause, count=0, tests=[], systems=[]))
            merged["count"] += cause["count"]
            merged["tests"] += [t for t in cause["tests"] if t not in merged["tests"]]
            merged["systems"].append(d["system"])
    ordered = sorted(causes.values(), key=lambda c: (-c["count"], c["key"]))
    if ordered:
        lines += ["", f"### Causes, the first {CAUSES_SHOWN} of {len(ordered)}" if len(ordered) > CAUSES_SHOWN else "### Causes"]
        for cause in ordered[:CAUSES_SHOWN]:
            lines.append(f"**{cause['count']:,} × {cause['type']}** on {', '.join(cause['systems'])}" + (f" at `{cause['frame']}`" if cause["frame"] else ""))
            lines += ["    " + line for line in entry(cause)]
    return fit(lines), ordered


def annotations(lost, causes, head_line, repeated):
    """
    The annotations, which anyone can read where a run's log and summary need signing in, an error
    for each lost process and each cause, ten at most, each with its whole entry of the page, and
    a notice with the page's head and the lines the output repeated most.
    """
    errors = [(f"Lost: {p['label']}", lost_entry(p)) for p in lost]
    errors += [(f"{c['count']:,} × {c['type']}" + (f" on {', '.join(c['systems'])}" if c.get("systems") else "")
                + (f" at {c['frame']}" if c["frame"] else ""), entry(c)) for c in causes]
    notice = [head_line] + [f"{count:,} × {line}" for line, count in repeated]
    return errors[:ANNOTATIONS], notice


def publish(lines, notes, results_dir=None, d=None):
    errors, notice = notes
    print(BEGIN)
    print("\n".join(lines))
    print(END)
    if results_dir is not None:
        with open(os.path.join(results_dir, "digest.md"), "w", encoding="utf-8", newline="\n") as f:
            f.write("\n".join(lines) + "\n")
        with open(os.path.join(results_dir, "digest.json"), "w", encoding="utf-8", newline="\n") as f:
            json.dump(d, f, indent=1)
    for title, message in errors:
        annotate("error", title, message)
    annotate("notice", "The tests", notice)
    summarize(lines)


def duration(seconds):
    seconds = round(seconds)
    return f"{seconds // 60} m {seconds % 60} s" if seconds >= 60 else f"{seconds} s"


def commit():
    if os.environ.get("GITHUB_SHA"):
        return os.environ["GITHUB_SHA"][:8]
    try:
        return subprocess.run(["git", "rev-parse", "--short=8", "HEAD"], cwd=ROOT, capture_output=True, text=True).stdout.strip() or "?"
    except OSError:
        return "?"


# -- Main

def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("names", nargs="*", metavar="PART", help="bridge, renderer or suite, every one when none is named")
    parser.add_argument("--parts", dest="all_parts", action="store_true", help="run the suite in its parts, each a process")
    parser.add_argument("--no-build", action="store_true", help="pass --no-build to dotnet test")
    parser.add_argument("--read", metavar="DIR", help="write the page of the results already in DIR")
    parser.add_argument("--digest", metavar="DIR", nargs="+", help="one page from the digest.json in each DIR")
    parser.add_argument("--results", default=RESULTS, help="where results, output and the page go")
    parser.add_argument("--project", default=PROJECT)
    parser.add_argument("--configuration", default="Debug", help="the configuration dotnet test builds and runs")
    parser.add_argument("--dotnet", default="dotnet", help="the command that stands for dotnet")
    parser.add_argument("--cargo", default="cargo", help="the command that stands for cargo")
    parser.add_argument("--timeout-minutes", type=float, default=40)
    parser.add_argument("--hang-minutes", type=float, default=5)
    parser.add_argument("--memory-mb", type=int, default=6144)
    args = parser.parse_args()
    for name in args.names:
        if name not in PARTS:
            parser.error(f"{name} is no part; the parts are {', '.join(PARTS)}")
    # A Windows console's code page may lack what the page writes, and Windows ends a line with
    # \r\n, so the log is UTF-8 with \n alone everywhere, as the files are, and reads the same
    # wherever it was made.
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace", newline="\n")

    if args.digest:
        digests = []
        for folder in args.digest:
            path = folder if folder.endswith(".json") else os.path.join(folder, "digest.json")
            try:
                with open(path, encoding="utf-8") as f:
                    digests.append(json.load(f))
            except (OSError, ValueError):
                print(f"no digest at {path}")
        lines, causes = merged_page(digests)
        lost = [dict(p, label=f"{p['label']} on {d['system']}") for d in digests for p in d["processes"] if p["lost"]]
        repeated = [r for d in digests for r in d["repeated"]][:REPEATED_SHOWN]
        publish(lines, annotations(lost, causes, "; ".join(head(d) for d in digests) or lines[-1], repeated))
        return 0 if digests and all(d["failed"] == 0 and not any(p["lost"] for p in d["processes"]) for d in digests) else 1

    if args.read:
        d = digest(args.read, [], [], 0)
        publish(page(d), annotations([], d["causes"], head(d), d["repeated"]), args.read, d)
        return 0 if d["failed"] == 0 else 1

    results = os.path.abspath(args.results)
    os.makedirs(results, exist_ok=True)
    for name in os.listdir(results):
        if (name.startswith("results") and name.endswith(".trx")) or (name.startswith("output") and name.endswith(".txt")) or name.startswith("digest."):
            os.remove(os.path.join(results, name))

    names = args.names or PARTS
    started = time.monotonic()
    processes, listed = [], []
    for name in [n for n in PARTS if n in names]:
        if name == "suite" and args.all_parts:
            listed = list_tests(args)
            processes += parts_of(listed)
        else:
            processes.append(Process(LABELS[name], "dotnet" if name == "suite" else "cargo", None if name != "renderer" else "renderer"))

    done = []
    for process in processes:
        done.append(run(process, args, results))
        print(process.summary(), flush=True)
        if process.label == "the suite" and process.lost:
            # Run again in parts, each a process, so a part that is lost costs only its own tests.
            listed = list_tests(args)
            for part in parts_of(listed):
                done.append(run(part, args, results))
                print(part.summary(), flush=True)

    d = digest(results, done, listed, time.monotonic() - started)
    publish(page(d), annotations([p for p in d["processes"] if p["lost"]], d["causes"], head(d), d["repeated"]), results, d)
    return 0 if d["failed"] == 0 and d["no_result"] == 0 and not any(p.lost for p in done) else 1


if __name__ == "__main__":
    sys.exit(main())
