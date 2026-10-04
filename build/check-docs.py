#!/usr/bin/env python3
"""Checks the README and the guide under docs/ as a reader follows them.

    build/check-docs.py                       # every link in README.md and docs/ goes somewhere
    build/check-docs.py --external            # and the ones off this repository answer
    build/check-docs.py --headings <commit>   # every heading README.md had at a commit is still here

A link inside the repository, relative or by its full GitHub URL, has to name a file that is here,
and an anchor a heading that file has. The README links by full URL alone, since it is also the
package's page on nuget.org, where a relative link goes nowhere, and a relative link left in it
fails. The headings check is for a move of the README's reference into docs/, which is to drop
nothing, so each heading the README had at the commit before is found in it or in a page.
"""

import glob
import os
import re
import subprocess
import sys
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REPOSITORY = ("https://github.com/EggyStudio/BevyCSharp/blob/main/",
              "https://github.com/EggyStudio/BevyCSharp/tree/main/",
              "https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/",
              "https://raw.githubusercontent.com/EggyStudio/BevyCSharp/refs/heads/main/")

# Headings the move renamed rather than carried, by what they became.
RENAMED = {"What is where": "Guide", "The engine": "Guide"}


def headings(text):
    """The headings of a Markdown text, outside code blocks."""
    found, fence = [], False
    for line in text.split("\n"):
        if line.startswith("```"):
            fence = not fence
        elif not fence and (match := re.match(r"#{1,6} (.+)", line)):
            found.append(match.group(1).strip())
    return found


def anchor(text):
    """The anchor GitHub gives a heading."""
    return re.sub(r"[^\w\- ]", "", text.lower()).replace(" ", "-")


def links(text):
    """Every link target of a Markdown text, outside code blocks, with images' sources."""
    found, fence = [], False
    for line in text.split("\n"):
        if line.startswith("```"):
            fence = not fence
            continue
        if fence:
            continue
        line = re.sub(r"`[^`]*`", "", line)
        found += re.findall(r"\]\(([^)\s]+)\)", line)
        found += re.findall(r'src="([^"]+)"', line)
    return found


def check_links(external):
    problems = []
    pages = [os.path.join(ROOT, "README.md"), os.path.join(ROOT, "CHEATSHEET.md")] + sorted(glob.glob(os.path.join(ROOT, "docs", "*.md")))
    anchors = {}
    outside = set()

    for page in pages:
        text = open(page, encoding="utf-8").read()
        name = os.path.relpath(page, ROOT)

        for target in links(text):
            if target.startswith(REPOSITORY):
                prefix = next(p for p in REPOSITORY if target.startswith(p))
                path, _, heading = target[len(prefix):].partition("#")
                local = os.path.join(ROOT, path)
            elif re.match(r"(https?:|mailto:)", target):
                outside.add(target)
                continue
            else:
                if name in ("README.md", "CHEATSHEET.md") and not target.startswith("#"):
                    problems.append(f"{name}: '{target}' is relative, which goes nowhere on nuget.org")
                    continue
                path, _, heading = target.partition("#")
                local = os.path.normpath(os.path.join(os.path.dirname(page), path)) if path else page

            if not os.path.exists(local):
                problems.append(f"{name}: '{target}' names nothing here")
                continue

            if heading and local.endswith(".md"):
                if local not in anchors:
                    anchors[local] = {anchor(h) for h in headings(open(local, encoding="utf-8").read())}
                if heading not in anchors[local]:
                    problems.append(f"{name}: '{target}' names a heading {os.path.relpath(local, ROOT)} does not have")

    if external:
        # Bevy's live examples are listed by build/examples-table.py --live, which asked the site
        # for each, and are a few hundred requests to one host that this would only repeat.
        outside = {target for target in outside if not target.startswith("https://bevy.org/examples/")}
        for target in sorted(outside):
            try:
                request = urllib.request.Request(target, method="HEAD", headers={"User-Agent": "check-docs"})
                urllib.request.urlopen(request, timeout=20)
            except Exception as error:
                # Some sites refuse a HEAD, so a refusal is asked again as a GET before it counts.
                try:
                    urllib.request.urlopen(urllib.request.Request(target, headers={"User-Agent": "check-docs"}), timeout=20)
                except Exception:
                    problems.append(f"'{target}' did not answer: {error}")

    return problems


def check_headings(commit):
    before = subprocess.run(["git", "show", f"{commit}:README.md"], cwd=ROOT, capture_output=True, text=True, check=True).stdout
    now = set(headings(open(os.path.join(ROOT, "README.md"), encoding="utf-8").read()))
    for page in glob.glob(os.path.join(ROOT, "docs", "*.md")):
        now |= set(headings(open(page, encoding="utf-8").read()))

    return [f"'{heading}' is in neither README.md nor a page under docs/"
            for heading in headings(before)
            if heading not in now and RENAMED.get(heading) not in now]


def main():
    arguments = sys.argv[1:]
    problems = []
    if "--headings" in arguments:
        problems += check_headings(arguments[arguments.index("--headings") + 1])
    problems += check_links("--external" in arguments)

    for problem in problems:
        print(problem, file=sys.stderr)
    if problems:
        sys.exit(1)
    print("the README and the guide link only where something is")


if __name__ == "__main__":
    main()
