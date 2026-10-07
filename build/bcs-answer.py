#!/usr/bin/env python3
"""Says what a ./bcs command that failed answered, where a reader of the workflow's run sees it.

    answer=$(./bcs open --feature-test --json) || code=$?
    printf '%s' "$answer" | build/bcs-answer.py "./bcs open --feature-test" "$code" BevyCSharp.FeatureTest

A step that runs ./bcs bare ends with bcs's exit code and nothing else on the page, its answer,
the JSON envelope with the code and the sentence that exit code stands for, going to a log nobody
reads, as the Windows jobs' opening of the sample did for three runs (NORM.md, N 6.7). This reads
the answer the step kept and says, as an error annotation and on the job's summary, the command,
its exit code, the answer's code and sentence, and the last lines of the program's log, at the path
the answer names or the one bcs keeps under build/sessions for the program named, as 3DEngine's
drive-game.sh says a game that did not open. It exits with the command's exit code.
"""

import json
import os
import re
import sys
from collections import deque

sys.dont_write_bytecode = True
from page import annotate, fit, summarize

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOG_LINES = 15

# Bevy colors its lines and stamps them with the time, neither of which a page needs.
ANSI = re.compile(r"\x1b\[[0-9;]*m")
STAMP = re.compile(r"^\d{4}-\d\d-\d\dT[\d:.]+Z\s+")


def said(text):
    """The answer's code and sentence, and the log it names, or what the answer was instead."""
    try:
        answer = json.loads(text)
    except json.JSONDecodeError:
        flat = " ".join(text.split())
        return (f"an answer that is not JSON, {flat[:300]}" if flat else "nothing"), None

    if not isinstance(answer, dict):
        return f"an answer that is not an envelope, {text.strip()[:300]}", None

    errors = answer.get("errors") or [{}]
    error = errors[0] if isinstance(errors[0], dict) else {}
    sentence = str(error.get("message") or "no sentence").rstrip(".")
    data = answer.get("data") if isinstance(answer.get("data"), dict) else {}
    return f"{error.get('code') or 'no code'}, {sentence}", data.get("log")


def system():
    """The system by the name a reader knows it by, as the runner names it."""
    return os.environ.get("RUNNER_OS") or {"win32": "Windows", "darwin": "macOS"}.get(sys.platform, "Linux")


def main():
    if len(sys.argv) < 3:
        print(__doc__.strip().splitlines()[0], file=sys.stderr)
        return 2

    command, code = sys.argv[1], sys.argv[2]
    program = sys.argv[3] if len(sys.argv) > 3 else None
    answer, log = said(sys.stdin.read())
    if not log and program:
        log = os.path.join(ROOT, "build", "sessions", f"{program}.log")

    lines = [f"{command} ended with exit code {code}, and bcs said {answer}."]
    if log and os.path.isfile(log):
        with open(log, encoding="utf-8", errors="replace") as f:
            tail = [STAMP.sub("", ANSI.sub("", line.rstrip())) for line in deque(f, LOG_LINES)]
        lines += [f"The last lines of {log}:"] + [f"    {line}" for line in tail if line.strip()]
    elif log:
        lines.append(f"There is no log at {log}.")

    lines = fit(lines)
    title = f"{command} on {system()}"
    annotate("error", title, lines)
    summarize([f"### {title}", "", "```", *lines, "```"])
    print("\n".join(lines))

    try:
        return int(code) or 1
    except ValueError:
        return 1


if __name__ == "__main__":
    sys.exit(main())
