#!/usr/bin/env python3
"""Writes the package's release notes, the commits since the package nuget.org lists last was built,
newest first, each commit's sentence a line as the history says them to be read.

    build/release-notes.py [file]

The notes go into the file named, build/release-notes.txt by default, beside build/artifacts rather
than in it, since every folder there is a runtime's and the package check read the notes as a
library.

They start after the commit the latest package of the minor build/version.txt names was built from,
which nuget.org's index of the package and that version's nuspec say, its repository commit naming
where the package was built. So each package's notes are what changed since the one before it, where
the commits since the minor began repeat every earlier package's notes in each one, 119 lines at
0.5.120. Where the feed answers nothing, lists no version of the minor, or names a commit this
checkout does not have, as a shallow clone has none, the notes are the minor's, the commits since
build/version.txt last changed. Either way the script says which commit the notes start from and
why, in what it prints and in the step's summary where GitHub gives one.

A commit whose every file lies under .github/ is left out, since the package carries nothing from
there, which takes the review's records and the workflows' changes alike by what they touch and not
by their words. The README, the guide, the license and the notices are carried, so a commit to them
stays in.

RELEASE_NOTES_FEED names the package's folder of the feed in place of nuget.org's, a file:// one
for a test.
"""

import json
import os
import re
import subprocess
import sys
import urllib.request
import xml.etree.ElementTree as ElementTree

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FEED = os.environ.get("RELEASE_NOTES_FEED", "https://api.nuget.org/v3-flatcontainer/bevycsharp").rstrip("/")


def git(*args):
    return subprocess.run(["git", *args], cwd=ROOT, capture_output=True, text=True, encoding="utf-8")


def fetch(url):
    with urllib.request.urlopen(url, timeout=30) as answer:
        return answer.read()


def published(minor):
    """The commit the latest package of the minor was built from and its version, or why there is none."""
    try:
        versions = json.loads(fetch(f"{FEED}/index.json"))["versions"]
    except Exception as error:
        return None, f"nuget.org answered nothing for the package's versions ({error})"

    ours = [version for version in versions if re.fullmatch(re.escape(minor) + r"\.\d+", version)]
    if not ours:
        return None, f"nuget.org lists no version of {minor}"
    latest = max(ours, key=lambda version: int(version.rsplit(".", 1)[1]))

    try:
        nuspec = ElementTree.fromstring(fetch(f"{FEED}/{latest}/{os.path.basename(FEED)}.nuspec"))
    except Exception as error:
        return None, f"nuget.org answered nothing for the nuspec of {latest} ({error})"

    # The nuspec's own namespace, which changes with its schema, is left out of the match.
    repository = next((element for element in nuspec.iter() if element.tag.rsplit("}", 1)[-1] == "repository"), None)
    commit = repository.get("commit") if repository is not None else None
    if not commit:
        return None, f"the nuspec of {latest} names no commit it was built from"
    if git("cat-file", "-e", f"{commit}^{{commit}}").returncode != 0:
        return None, f"{latest} was built from {commit[:8]}, which this checkout does not have"
    return (commit, latest), None


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "build", "release-notes.txt")
    with open(os.path.join(ROOT, "build", "version.txt"), encoding="utf-8") as version:
        minor = version.read().strip()

    found, why = published(minor)
    if found:
        start, latest = found
        said = f"The release notes start after {start[:8]}, which {latest}, the latest of {minor} on nuget.org, was built from."
    else:
        start = git("log", "-1", "--format=%H", "--", "build/version.txt").stdout.strip()
        since = f"from {start[:8]}, where build/version.txt last changed" if start else "empty, the history holding no change to build/version.txt"
        said = f"The release notes are those of {minor} whole, {since}, since {why}."

    lines = []
    if start:
        for commit in git("rev-list", f"{start}..HEAD").stdout.split():
            files = [name for name in git("show", "--name-only", "--format=", commit).stdout.splitlines() if name]
            if files and all(name.startswith(".github/") for name in files):
                continue
            body = git("log", "-1", "--format=%b", commit).stdout
            lines += [line.rstrip() for line in body.splitlines() if line.strip()]

    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    with open(out, "w", encoding="utf-8") as notes:
        notes.writelines(line + "\n" for line in lines)

    print(f"{len(lines)} lines of release notes in {out}. {said}")
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as page:
            page.write(said + "\n")


if __name__ == "__main__":
    main()
