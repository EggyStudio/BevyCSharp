#!/usr/bin/env python3
"""Runs a workflow step's script in bash and, where it fails having said nothing, says what failed.

    build/step.py SCRIPT [--workflow FILE] [--logs DIR]...

The jobs of the pack workflow that build and run programs on Linux run each of their steps through
this, as the shell GitHub runs a step's script in (`shell: python3 build/step.py {0}`). The script
runs as GitHub's own bash runs one, under `bash -e -o pipefail`, so the first command that fails
ends it, and what it prints is passed on as it comes. A step that fails and prints no `::error::` of
its own is given one, which names the step, the command that failed with its line and exit code,
the step's last lines, and the last lines at a warning or worse of each log written while it ran,
those `bcs open` keeps under build/sessions and those in the folders BCS_STEP_LOGS names, as the
test page names its causes (NORM.md, N 6.7). Before it, Courtyard's play ended the game job with an
exit code and nothing else, what stopped the game being in a log nobody read.

The step is named as the workflow names it, found by its script among the workflows under
.github/workflows, where an expression GitHub filled in matches whatever it was filled with. All of
them are searched rather than the one GITHUB_WORKFLOW_REF names, since the pack workflow's jobs are
a reusable workflow's and that variable names the workflow that called it. It exits with the
script's exit code.
"""

import argparse
import glob
import os
import re
import subprocess
import sys
import tempfile
import time
from collections import deque

sys.dont_write_bytecode = True
from page import annotate, fit, summarize

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SESSIONS = os.path.join(ROOT, "build", "sessions")
LAST_LINES = 6
LOG_LINES = 3

# Bevy colors its lines, and a log is read here where the colors are not.
ANSI = re.compile(r"\x1b\[[0-9;]*m")
# The time Bevy's log writes a line at, left off what is shown.
STAMP = re.compile(r"^\d{4}-\d\d-\d\dT[\d:.]+Z\s+")
# A line at a warning or worse, as Bevy's log levels it, a panic, an exception nothing caught, or
# a play script's own failure.
LEVEL = re.compile(r"^(WARN|ERROR)\s|panicked at|^Unhandled exception|^FAILED: ")

# Each command that fails is written to the file BCS_STEP_FAILED names, with its exit code and line,
# by a trap bash runs on an error, kept by functions and subshells (-E). The step's script is read in
# by the shell the trap is set in, so its lines are numbered as the workflow shows them.
PROLOGUE = """bcs_step_script=$1; shift
trap 'printf "%s\\t%s\\t%s\\n" "$?" "$LINENO" "$BASH_COMMAND" >> "$BCS_STEP_FAILED"' ERR
. "$bcs_step_script"
"""


def run(script):
    """Runs the script, printing what it prints, and returns its exit code, its last lines,
    whether it said an error of its own, and the commands that failed in it."""
    failed = tempfile.NamedTemporaryFile(prefix="bcs-step-", suffix=".txt", delete=False)
    failed.close()
    last = deque(maxlen=LAST_LINES)
    said = False
    try:
        environment = dict(os.environ, BCS_STEP_FAILED=failed.name)
        process = subprocess.Popen(["bash", "--noprofile", "--norc", "-eE", "-o", "pipefail", "-c", PROLOGUE, "step", script],
                                   stdout=subprocess.PIPE, stderr=subprocess.STDOUT, env=environment)
        for raw in process.stdout:
            line = raw.decode("utf-8", "replace").rstrip("\r\n")
            print(line, flush=True)
            said |= line.startswith("::error")
            if line.strip():
                last.append(ANSI.sub("", line))
        code = process.wait()
        with open(failed.name, encoding="utf-8", errors="replace") as f:
            commands = [line.rstrip("\n").split("\t", 2) for line in f if line.count("\t") >= 2]
    finally:
        os.remove(failed.name)
    return code, list(last), said, commands


def run_blocks(lines):
    """Each step's name, or None, and its script, as a workflow file holds them."""
    name = None
    for index, line in enumerate(lines):
        if m := re.match(r"^(\s*)- (\w[\w-]*):\s*(.*)$", line):
            # A step begins, by its name or by whatever key comes first in it.
            name = m.group(3).strip() if m.group(2) == "name" else None
            if m.group(2) != "run":
                continue
            line = line.replace("- run:", "  run:", 1)
        m = re.match(r"^(\s*)run:\s*(.*)$", line)
        if not m:
            continue
        indent, value = len(m.group(1)), m.group(2).strip()
        if not value:
            # A mapping, as a job's defaults hold the shell its steps run in, and no step's script.
            continue
        if value in ("|", "|-", "|+", ">", ">-"):
            block = []
            for following in lines[index + 1:]:
                if following.strip() and len(following) - len(following.lstrip()) <= indent:
                    break
                block.append(following)
            depth = min((len(b) - len(b.lstrip()) for b in block if b.strip()), default=0)
            text = "\n".join(b[depth:] for b in block)
        else:
            text = value
        yield name, text


def matches(text, script):
    """Whether a step's script as the workflow writes it is the one GitHub ran, an expression in it
    standing for whatever GitHub filled it in with."""
    parts = re.split(r"\$\{\{.*?\}\}", text.strip())
    return re.fullmatch("(?s:.*?)".join(re.escape(part) for part in parts), script.strip()) is not None


def step_name(script, workflows):
    """The step's name in the first workflow with a step whose script is the one given, or None."""
    for workflow in workflows:
        try:
            with open(workflow, encoding="utf-8") as f:
                lines = f.read().split("\n")
        except OSError:
            continue
        for name, text in run_blocks(lines):
            if matches(text, script):
                return name or f"Run {script.strip().splitlines()[0]}"
    return None


def log_lines(folders, since):
    """The last lines at a warning or worse of each log in the folders written since the time given."""
    found = []
    for folder in folders:
        for path in sorted(glob.glob(os.path.join(folder, "*.log"))):
            if os.path.getmtime(path) < since:
                continue
            with open(path, encoding="utf-8", errors="replace") as f:
                plain = (STAMP.sub("", ANSI.sub("", line.rstrip("\n"))) for line in f)
                logged = [line[:300] for line in plain if LEVEL.search(line)]
            found.append((os.path.basename(path), logged[-LOG_LINES:]))
    return found


def failure(name, code, last, commands, logs):
    """The error's title and lines, what failed and with what code, the step's last lines, and the
    logs' last lines at a warning or worse."""
    title = f"{name}: exit code {code}"
    # The command that ended the step is the last that failed with its code; one that failed inside
    # a substitution whose value was used does not end it.
    ending = next((c for c in reversed(commands) if c[0] == str(code)), None)
    lines = [f"`{ending[2].strip()}` on line {ending[1]} of the step ended with exit code {code}." if ending
             else f"The step ended with exit code {code} by its own exit, no command failing."]
    if last:
        lines += ["Its last lines:"] + ["    " + line for line in last]
    for log, logged in logs:
        lines.append(f"{log}, its last lines at a warning or worse:" if logged else f"{log} holds no line at a warning or worse.")
        lines += ["    " + line for line in logged]
    return title, fit(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("script")
    parser.add_argument("--workflow", help="the workflow file to name the step from, in place of those under .github/workflows")
    parser.add_argument("--logs", action="append", default=[], help="a folder of logs to read, beside build/sessions and BCS_STEP_LOGS's")
    args = parser.parse_args()
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    with open(args.script, encoding="utf-8", errors="replace") as f:
        text = f.read()

    started = time.time()
    code, last, said, commands = run(args.script)
    if code == 0 or said:
        return code

    workflows = [args.workflow] if args.workflow else sorted(glob.glob(os.path.join(ROOT, ".github", "workflows", "*.yml")))
    name = step_name(text, workflows) or f"The step beginning `{text.strip().splitlines()[0] if text.strip() else ''}`"
    folders = [SESSIONS] + [f for f in os.environ.get("BCS_STEP_LOGS", "").split(os.pathsep) if f] + args.logs
    title, lines = failure(name, code, last, commands, log_lines(folders, started))
    print(f"{title}\n" + "\n".join(lines), flush=True)
    annotate("error", title, lines)
    summarize([f"### {title}", ""] + [line if line.startswith("    ") else line + "  " for line in lines])
    return code


if __name__ == "__main__":
    sys.exit(main())
