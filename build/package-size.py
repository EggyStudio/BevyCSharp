#!/usr/bin/env python3
"""Says the size of each package of a version in build/package against the size nuget.org takes, and
fails where one is over, so a pack run stops before its push and not at it.

    build/package-size.py <version>

The engine's package carries the bridge for every system, nearly all of its bytes, and stood at
249,913,716 bytes at 0.6.91. A push nuget.org refuses would fail the run after every game and
capture of it passed, so the pack job measures first, before anything is kept or pushed, and says
each package's room in the run's summary, where a shrinking margin is seen before it runs out.

nuget.org's documents give the size as 250 MB. The gallery itself refuses a push whose request is
over 262,144,000 bytes, ASP.NET's maxRequestLength of 256,000 KB in its Web.config, with IIS's
maxAllowedContentLength the same, and answers 413, the package file exceeding the size limit. The
request is the package in a form of one part, as dotnet nuget push sends it, a few hundred bytes of
framing, and the check leaves 4,096 bytes for it.

Beside each package it says the last version nuget.org lists of the same package and that one's
size and room, read from the gallery's flat container, so a run shows what its package weighs
against the one published before it, and a reader of a later run sees which version it is
compared with.

PACKAGE_SIZE_LIMIT gives the request's limit in bytes in place of the gallery's, and
PACKAGE_SIZE_PUBLISHED the version and size published before as `<version>=<bytes>`, or `none`, in
place of the gallery's, for a test.
"""

import glob
import json
import os
import re
import sys
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FOLDER = os.path.join(ROOT, "build", "package")
REQUEST_LIMIT = int(os.environ.get("PACKAGE_SIZE_LIMIT", "262144000"))
FRAMING = 4096
LIMIT = REQUEST_LIMIT - FRAMING


def say(line, summary):
    print(line, flush=True)
    path = os.environ.get("GITHUB_STEP_SUMMARY")
    if path:
        with open(path, "a", encoding="utf-8") as page:
            page.write(summary + "\n")


def published(package_id, version):
    """The last version nuget.org lists of the package other than this one, and its size in bytes,
    or None where the gallery says none or cannot be read."""
    given = os.environ.get("PACKAGE_SIZE_PUBLISHED")
    if given is not None:
        if given == "none":
            return None
        last, size = given.split("=")
        return last, int(size)
    flat = f"https://api.nuget.org/v3-flatcontainer/{package_id.lower()}"
    try:
        with urllib.request.urlopen(f"{flat}/index.json", timeout=30) as answer:
            versions = [v for v in json.load(answer)["versions"] if v != version.lower()]
        if not versions:
            return None
        last = versions[-1]
        asked = urllib.request.Request(f"{flat}/{last}/{package_id.lower()}.{last}.nupkg", method="HEAD")
        with urllib.request.urlopen(asked, timeout=30) as answer:
            return last, int(answer.headers["Content-Length"])
    except (OSError, ValueError, KeyError):
        return None


def main():
    if len(sys.argv) != 2:
        sys.exit("usage: build/package-size.py <version>")
    version = sys.argv[1]

    files = sorted(glob.glob(os.path.join(FOLDER, f"*.{version}.nupkg")))
    if not files:
        sys.exit(f"no package of {version} in {FOLDER}")

    say(f"{len(files)} packages of {version}, each against {LIMIT:,} bytes, nuget.org's {REQUEST_LIMIT:,} of a request less {FRAMING:,} for its framing",
        f"### The packages against nuget.org's size\n\nEach against {LIMIT:,} bytes, nuget.org's {REQUEST_LIMIT:,} of a request less "
        f"{FRAMING:,} for its framing, beside the last version nuget.org lists of it.\n\n"
        f"| Package | Bytes | Room | Published before | Its bytes | Its room |\n|---|---:|---:|---|---:|---:|")
    over = []
    for path in files:
        name = os.path.basename(path)
        size = os.path.getsize(path)
        room = LIMIT - size
        if room < 0:
            over.append(name)
        package_id = re.sub(r"\." + re.escape(version) + r"\.nupkg$", "", name)
        before = published(package_id, version)
        if before is None:
            was, was_row = ", nothing published before it read", "| none read | | |"
        else:
            last, last_size = before
            was = f", where {last} on nuget.org was {last_size:,} bytes with {LIMIT - last_size:,} of room"
            was_row = f"| {last} | {last_size:,} | {LIMIT - last_size:,} ({(LIMIT - last_size) / LIMIT:.2%}) |"
        say(f"  {name} {size:,} bytes, {room:,} bytes of room ({room / LIMIT:.2%})" + (", over" if room < 0 else "") + was,
            f"| {name} | {size:,} | {room:,} ({room / LIMIT:.2%}) {was_row}")
    if over:
        sys.exit(f"over the size nuget.org takes, so nothing is pushed: {', '.join(over)}")


if __name__ == "__main__":
    main()
