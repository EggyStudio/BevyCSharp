#!/usr/bin/env python3
"""Writes THIRD-PARTY-NOTICES.md, whose work the package carries beside this library's own.

    build/third-party-notices.py            # write the file
    build/third-party-notices.py --check    # fail where the file differs from what would be written

The bridge in the package is built from the crates native/Cargo.lock names, Bevy and what Bevy
stands on, and most of their licenses ask that their notice go with a copy, a compiled one too. The
crates are read through `cargo metadata`, which cargo has without any tool added, and each crate's
own files in the cargo registry give its notices: the lines that name who holds it, and the texts of
its license and notice files, each distinct text written once with the crates that carry it. A
crate whose package holds no such file is given the standard text of each license it names, taken
from the crates that do carry one.

Every crate of the lock is named, so a crate no shipped profile compiles is named too, and N 6.4's
test holds the file to the lock. The library's NuGet packages, Bevy's default font, the examples and
the assets they load are written from what this script knows of them, below.
"""

import collections
import json
import os
import re
import subprocess
import sys
import tomllib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "THIRD-PARTY-NOTICES.md")
MANIFEST = os.path.join(ROOT, "native", "Cargo.toml")

# A file whose name says it holds a license or a notice, at a crate's root or in a folder of its
# own, such as a vendored C library's. Tests, benchmarks and examples are not compiled into the
# bridge, so a font a test of a crate reads is left out.
LICENSE_FILE = re.compile(r"^(LICEN[CS]E|COPYING|NOTICE|COPYRIGHT|UNLICENSE)|[-_.](LICEN[CS]E|COPYING)([-_.]|$)", re.I)
SOURCE_ENDINGS = (".rs", ".toml", ".c", ".h", ".cpp", ".py", ".sh", ".html", ".json")
SKIPPED_FOLDERS = {"tests", "benches", "examples", "target", ".git"}

# A line that names who holds a work, rather than one of a license's own lines about copyright,
# such as Apache's "copyright owner" or a template's "[yyyy]".
HOLDER = re.compile(r"^\W{0,3}(copyright|\(c\)|©)\b", re.I)
NOT_A_HOLDER = re.compile(r"notice|owner|\[yyyy\]|\{yyyy\}|<year>|holders? and contributors|statement|license|the above", re.I)

# What a text is, by words only its license has, for a crate that carries no text of its own.
KINDS = [
    ("Apache-2.0", ["apache license"]),
    ("MPL-2.0", ["mozilla public license"]),
    ("LGPL-2.1", ["gnu lesser general public license"]),
    ("GPL", ["gnu general public license"]),
    ("BSL-1.0", ["boost software license"]),
    ("Unicode-3.0", ["unicode license v3"]),
    ("Unicode-DFS-2016", ["unicode, inc. license agreement"]),
    ("Unlicense", ["free and unencumbered software"]),
    ("CC0-1.0", ["cc0"]),
    ("Zlib", ["provided 'as-is'"]),
    ("Zlib", ['provided "as-is"']),
    ("ISC", ["permission to use, copy, modify, and/or distribute", "provided that the above copyright notice"]),
    ("0BSD", ["permission to use, copy, modify, and/or distribute"]),
    ("BSD-3-Clause", ["neither the name"]),
    ("BSD-2-Clause", ["redistribution and use in source and binary forms"]),
    ("MIT", ["permission is hereby granted, free of charge", "shall be included"]),
    ("MIT-0", ["permission is hereby granted, free of charge"]),
]

# The library's own packages, which a restore fetches with their license files, named here so a
# game shipped on the package can say what it carries.
PACKAGES = [
    ("Twizzle.ImGui-Bundle.NET", "Dear ImGui's bindings and its native build, cimgui, for the editor and a game's tools", "MIT", "https://github.com/JoeTwizzle/ImGui.NET"),
    ("Dear ImGui, inside it", "The interface those tools are drawn with", "MIT", "https://github.com/ocornut/imgui"),
    ("BepuPhysics and BepuUtilities", "Rigid bodies, simulated on the managed side", "Apache-2.0", "https://github.com/bepu/bepuphysics2"),
]

# Bevy's default font, which bevy_text compiles into the bridge, under a license no crate of the
# lock carries the text of. The text is Bevy's own copy, kept beside this script.
FONT_LICENSE = os.path.join(ROOT, "build", "notices", "FiraMono-LICENSE")


def metadata():
    """Every package of the lock, through cargo, with the workspace's own left out."""
    answer = subprocess.run(
        ["cargo", "metadata", "--format-version", "1", "--locked", "--all-features", "--manifest-path", MANIFEST],
        check=True, capture_output=True, text=True)
    meta = json.loads(answer.stdout)
    members = set(meta["workspace_members"])
    return [package for package in meta["packages"] if package["id"] not in members]


def kind_of(text):
    flat = re.sub(r"\s+", " ", text).lower()
    for kind, words in KINDS:
        if all(word in flat for word in words):
            return kind
    return None


def license_files(folder):
    """The license and notice files of a crate, its root's first, each as a path from the crate."""
    found = []
    for root, folders, files in os.walk(folder):
        folders[:] = sorted(name for name in folders if name not in SKIPPED_FOLDERS)
        for name in sorted(files):
            if LICENSE_FILE.search(name) and not name.endswith(SOURCE_ENDINGS):
                found.append(os.path.relpath(os.path.join(root, name), folder).replace(os.sep, "/"))
    return sorted(found, key=lambda path: (path.count("/"), path.lower()))


def split(text):
    """A file's lines naming who holds the work, and the rest of it as the text to show."""
    holders, rest = [], []
    for line in text.splitlines():
        if HOLDER.match(line) and not NOT_A_HOLDER.search(line):
            holders.append(line.strip())
        else:
            rest.append(line.rstrip())
    return holders, "\n".join(rest).strip("\n")


def cell(text):
    """Text safe in a table's cell."""
    return text.replace("|", "\\|").replace("<", "&lt;").replace(">", "&gt;")


def license_ids(expression):
    """The licenses an expression names, in its order, Cargo's older slash read as OR."""
    return [word for word in re.findall(r"[A-Za-z0-9.+-]+", expression.replace("/", " OR "))
            if word not in ("OR", "AND", "WITH") and not word.endswith("exception")]


def build(packages):
    packages = sorted(packages, key=lambda p: (p["name"], [(0, int(n)) if n.isdigit() else (1, n) for n in re.split(r"[.+-]", p["version"])]))

    # Each distinct text once, numbered in the order the crates first carry it, keyed by its words
    # with the spacing evened out, so a text wrapped differently is still the one text.
    texts = {}
    order = []

    def text_number(text, kind):
        key = re.sub(r"\s+", " ", text).strip()
        if key not in texts:
            texts[key] = {"number": len(order) + 1, "text": text, "kind": kind, "crates": [], "taken": [], "from": None}
            order.append(key)
        return texts[key]

    rows = []
    without = []
    for package in packages:
        folder = os.path.dirname(package["manifest_path"])
        holders, carried = [], []
        for path in license_files(folder):
            with open(os.path.join(folder, path), encoding="utf-8", errors="replace") as file:
                found, text = split(file.read())
            holders += [line for line in found if line not in holders]
            if text.strip():
                entry = text_number(text, kind_of(text))
                name = f"{package['name']} {package['version']}"
                if name not in entry["crates"]:
                    entry["crates"].append(name)
                if entry["number"] not in carried:
                    carried.append(entry["number"])
        row = {"package": package, "holders": holders, "texts": carried}
        rows.append(row)
        if not carried:
            without.append(row)

    # The standard text of each license, the one most crates carry, for a crate that carries none.
    standard = {}
    for key in order:
        entry = texts[key]
        if entry["kind"] and len(entry["crates"]) > len(standard.get(entry["kind"], {"crates": []})["crates"]):
            standard[entry["kind"]] = entry
    with open(os.path.join(ROOT, "LICENSE"), encoding="utf-8") as file:
        ours = text_number(file.read().strip("\n"), "MPL-2.0")
    ours["from"] = "this repository's own LICENSE"
    standard.setdefault("MPL-2.0", ours)

    for row in without:
        package = row["package"]
        for license in license_ids(package.get("license") or ""):
            if license in standard and standard[license]["number"] not in row["texts"]:
                row["texts"].append(standard[license]["number"])
                standard[license]["taken"].append(f"{package['name']} {package['version']}")

    with open(FONT_LICENSE, encoding="utf-8") as file:
        font = text_number(file.read().strip("\n"), "OFL-1.1")
    font["crates"].append("Fira Mono, Bevy's default font, inside bevy_text")

    out = []
    out.append("# Third-party notices\n")
    out.append("BevyCSharp is under the Mozilla Public License 2.0 (`LICENSE`). Its package carries the library,")
    out.append("its generator and the bridge, a native library built from Bevy and the Rust crates Bevy stands on,")
    out.append("so a game shipped on the package carries them too. Each is named here with its license and the")
    out.append("notices its own files give, so the game can say what it carries.\n")
    out.append("`build/third-party-notices.py` writes this file from `native/Cargo.lock` through `cargo metadata`,")
    out.append("and NormTests holds it to the lock, so it is written again rather than edited.\n")

    out.append("## The library's packages\n")
    out.append("| Package | Used for | License | Source |")
    out.append("|---|---|---|---|")
    for name, use, license, source in PACKAGES:
        out.append(f"| {name} | {use} | {license} | {source} |")
    out.append("")
    out.append("A restore fetches them with their own license files. A shader written in Slang is compiled by")
    out.append("`slangc`, which the package does not carry.\n")

    out.append("## The bridge's crates\n")
    out.append("Every crate `native/Cargo.lock` names, which is every crate a profile of the bridge compiles for any")
    out.append("system the package is built for, and some that no shipped profile compiles. A crate offered under a")
    out.append("choice of licenses is offered so here as its authors offer it. The copyright column gives the")
    out.append("holders its files name, or else the authors its manifest names, and the last column the texts")
    out.append("below that its files hold, or for a crate whose package holds none, the standard text of each")
    out.append("license it names.\n")
    out.append("| Crate | Version | License | Copyright | Texts |")
    out.append("|---|---|---|---|---|")
    for row in rows:
        package = row["package"]
        if row["holders"]:
            holders = "<br>".join(cell(line) for line in row["holders"])
        elif package.get("authors"):
            holders = "by " + cell(", ".join(package["authors"]))
        else:
            holders = "none given"
        links = ", ".join(f"[{number}](#text-{number})" for number in row["texts"]) or "none"
        out.append(f"| {package['name']} | {package['version']} | {cell(package.get('license') or 'see its texts')} | {holders} | {links} |")
    out.append("")
    out.append("Bevy's default font, a subset of Fira Mono that bevy_text compiles into the bridge, is under the")
    out.append(f"SIL Open Font License 1.1, whose text with its holders is [text {font['number']}](#text-{font['number']}).\n")
    mpl = [f"{row['package']['name']}" for row in rows if "MPL-2.0" in license_ids(row["package"].get("license") or "")]
    if mpl:
        names = ", ".join(mpl[:-1]) + " and " + mpl[-1] if len(mpl) > 1 else mpl[0]
        out.append("The crates under the Mozilla Public License 2.0 are compiled in unchanged, and their source is")
        out.append(f"on crates.io at the versions above: {names}.\n")

    out.append("## The examples\n")
    out.append("The programs in `BevyCSharp.Examples` named for Bevy's examples are Bevy's")
    out.append(f"(https://github.com/bevyengine/bevy/tree/v{bevy_version()}/examples), by Bevy's contributors under")
    out.append("MIT or Apache-2.0, written again in C#, each naming at its head the example of Bevy's it is written")
    out.append("from. They are not in the package. The files of Bevy's they load are fetched from Bevy's repository")
    out.append("by `build/fetch-bevy-assets.sh` and are not kept in this one. Bevy credits them so:\n")
    for line in bevy_credits():
        out.append(f"> {line}".rstrip())
    out.append("")

    out.append("## Texts\n")
    for key in order:
        entry = texts[key]
        out.append(f"### Text {entry['number']}\n")
        out.append(describe(entry) + "\n")
        out.append("~~~~")
        out.append(entry["text"])
        out.append("~~~~\n")

    return "\n".join(out).rstrip("\n") + "\n"


def describe(entry):
    """Which license a text is and who carries it, or whose standard text it is taken as."""
    parts = []
    if entry["from"]:
        parts.append(f"from {entry['from']}")
    if entry["crates"]:
        parts.append(f"as {', '.join(entry['crates'])} {'carries' if len(entry['crates']) == 1 else 'carry'} it")
    if entry["taken"]:
        parts.append(f"{'and ' if parts else ''}the text taken for {', '.join(entry['taken'])}, whose packages hold none")
    return f"{entry['kind'] or 'A notice'}, {', '.join(parts)}."


def bevy_version():
    with open(os.path.join(ROOT, "native", "Cargo.lock"), "rb") as lock:
        for package in tomllib.load(lock)["package"]:
            if package["name"] == "bevy":
                return package["version"]
    sys.exit("native/Cargo.lock names no bevy package")


def bevy_credits():
    """The Assets part of Bevy's CREDITS.md, from Bevy's crate in the cargo registry."""
    home = os.environ.get("CARGO_HOME", os.path.join(os.path.expanduser("~"), ".cargo"))
    registry = os.path.join(home, "registry", "src")
    for index in sorted(os.listdir(registry)):
        path = os.path.join(registry, index, f"bevy-{bevy_version()}", "CREDITS.md")
        if os.path.exists(path):
            with open(path, encoding="utf-8") as file:
                lines = file.read().splitlines()
            start = lines.index("## Assets") + 1
            return "\n".join(lines[start:]).strip("\n").splitlines()
    sys.exit(f"bevy {bevy_version()} is not in the cargo registry; build the bridge once to fetch it")


def main():
    check = "--check" in sys.argv[1:]
    written = build(metadata())
    current = open(OUT, encoding="utf-8").read() if os.path.exists(OUT) else ""
    if check:
        if current != written:
            sys.exit("out of date, run build/third-party-notices.py: THIRD-PARTY-NOTICES.md")
        return
    if current != written:
        with open(OUT, "w", encoding="utf-8") as file:
            file.write(written)


if __name__ == "__main__":
    main()
