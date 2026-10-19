#!/usr/bin/env python3
"""Writes .github/EXAMPLES.md, a row for each of Bevy's examples and what this engine makes of it.

    build/examples-table.py            # write the table and the README's count
    build/examples-table.py --check    # fail where either is out of date, or an example has no row

The examples are read from the metadata of the Bevy this bridge builds against, the version
native/Cargo.lock pins, in the cargo registry, so none is left out and a new Bevy brings its new
ones in. An example is written when BevyCSharp.Examples holds its file, at <Bevy's folder>/<name>.cs,
and otherwise its state is the line BevyCSharp.Examples/triage.tsv has for it. The groups are
Bevy's own, in the order its examples/README.md lists them, and the examples it keeps out of that
list come last.

The README's gallery shows every written example's capture, each picture opening the C# program
that drew it, so a reader goes from what an example draws to the code that draws it. It is a table
a group under a heading naming it, in the groups' order, each table one row of the group's examples
in the order Bevy's README lists them, which scrolls sideways on GitHub and on nuget.org where it is
wider than the page. Each picture is the capture made small, into .github/assets/gallery, at the
width the gallery shows it, since the tables are Markdown, which nuget.org draws, where it prints
HTML as its text, and a Markdown picture is shown at its own size.

Each written example says at its head which of Bevy's it is written from, at which version and
under which licenses, as THIRD-PARTY-NOTICES.md says of them all. The head is written here, so a new
example and a new Bevy have it without anybody writing it by hand. A head that names an earlier
release than the lock's is kept, since the example follows that release's code until it is written
again from the current one, and its row links Bevy's source at that release. Writing it again
includes taking the old head off, for this to write the current one.
"""

import glob
import hashlib
import os
import re
import shutil
import subprocess
import sys
import textwrap
import tomllib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TABLE = os.path.join(ROOT, ".github", "EXAMPLES.md")
README = os.path.join(ROOT, "README.md")
TRIAGE = os.path.join(ROOT, "BevyCSharp.Examples", "triage.tsv")
EXAMPLES = os.path.join(ROOT, "BevyCSharp.Examples")
CAPTURES = os.path.join(ROOT, ".github", "assets", "examples")
GALLERY = os.path.join(ROOT, ".github", "assets", "gallery")

# The capture each of the gallery's pictures was made from, by its SHA-256, so a check finds a
# picture whose capture was taken again without making it again, which another ImageMagick would
# make in other bytes.
MADE_FROM = os.path.join(GALLERY, "captures.tsv")

# A capture made small to the width the gallery shows it at, and Bevy's window's shape.
THUMBNAIL = (260, 146)

HIDDEN = "Kept out of Bevy's list"

# The README is also the package's page on nuget.org, where a relative link goes nowhere, so what is
# written into it links by full URL.
BLOB = "https://github.com/EggyStudio/BevyCSharp/blob/main/"
RAW = "https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/"
STATES = {"written": "written", "part": "written in part", "can": "can be written", "missing": "missing", "n/a": "does not apply"}
ORDER = ["written", "part", "can", "missing", "n/a"]


def bevy_version():
    """The version of bevy native/Cargo.lock pins."""
    with open(os.path.join(ROOT, "native", "Cargo.lock"), "rb") as lock:
        for package in tomllib.load(lock)["package"]:
            if package["name"] == "bevy":
                return package["version"]
    sys.exit("native/Cargo.lock names no bevy package")


def bevy_source(version):
    """Bevy's crate in the cargo registry, which cargo puts there on the bridge's first build."""
    home = os.environ.get("CARGO_HOME", os.path.join(os.path.expanduser("~"), ".cargo"))
    found = glob.glob(os.path.join(home, "registry", "src", "*", f"bevy-{version}"))
    if not found:
        sys.exit(f"bevy {version} is not in the cargo registry; build the bridge once to fetch it")
    return found[0]


def read_examples(source):
    """Every example Bevy's metadata names, with its group, description and source path."""
    with open(os.path.join(source, "Cargo.toml"), "rb") as manifest:
        cargo = tomllib.load(manifest)

    paths = {entry["name"]: entry.get("path", "") for entry in cargo.get("example", [])}
    examples = []
    for name, meta in cargo["package"]["metadata"]["example"].items():
        group = meta.get("category") or HIDDEN
        examples.append({
            "name": name,
            "group": group,
            "description": meta.get("description", "").strip(),
            "path": paths.get(name, ""),
        })
    return examples


def readme_places(source, examples):
    """Each example's place in Bevy's examples/README.md, found by the source path its row links."""
    by_path = {example["path"]: example["name"] for example in examples if example["path"]}
    places = {}
    with open(os.path.join(source, "examples", "README.md"), encoding="utf-8") as readme:
        for line in readme:
            match = re.match(r"\[[^\]]*\]\(\.\./(examples/[^)]+)\)", line)
            if match and by_path.get(match.group(1)) not in (None, *places):
                places[by_path[match.group(1)]] = len(places)
    return places


def group_order(source):
    """The groups in the order Bevy's examples/README.md lists them."""
    order = []
    with open(os.path.join(source, "examples", "README.md"), encoding="utf-8") as readme:
        for line in readme:
            if line.startswith("## Platform-Specific"):
                break
            match = re.match(r"### (.+)", line)
            if match and match.group(1).strip() not in order:
                order.append(match.group(1).strip())
    return order


def read_triage():
    """The state and note of each example not written yet, by name."""
    triage = {}
    with open(TRIAGE, encoding="utf-8") as lines:
        for number, line in enumerate(lines, 1):
            line = line.rstrip("\n")
            if not line or line.startswith("#"):
                continue
            parts = line.split("\t")
            if len(parts) < 2 or parts[1] not in ("can", "part", "missing", "n/a"):
                sys.exit(f"triage.tsv:{number}: '{line}' is not a name, a state and a note")
            if parts[1] in ("part", "missing", "n/a") and (len(parts) < 3 or not parts[2]):
                sys.exit(f"triage.tsv:{number}: {parts[0]} is {parts[1]} and says nothing about why")
            triage[parts[0]] = (parts[1], parts[2] if len(parts) > 2 else "")
    return triage


def written_examples():
    """Each example written here, by name, with its file relative to the checkout."""
    written = {}
    for path in glob.glob(os.path.join(EXAMPLES, "*", "*.cs")):
        name = os.path.splitext(os.path.basename(path))[0]
        written[name] = os.path.relpath(path, ROOT).replace(os.sep, "/")
    return written


def keeps_components(source, example):
    """Whether Bevy's example keeps state on an entity, in a component of its own."""
    path = os.path.join(source, example["path"]) if example["path"] else ""
    if not path or not os.path.exists(path):
        return False
    with open(path, encoding="utf-8") as rust:
        return re.search(r"#\[derive\([^)]*\bComponent\b", rust.read()) is not None


def on_behaviors(path):
    """Whether a written example keeps what is on its entities in a behavior."""
    with open(os.path.join(ROOT, path), encoding="utf-8") as program:
        return "[Behavior]" in program.read()


def build(version, examples, order, triage, written, source, followed):
    # Read before the rows below take the name for each row's link.
    places = readme_places(source, examples)

    unknown = sorted(set(triage) - {example["name"] for example in examples})
    if unknown:
        sys.exit("triage.tsv names examples Bevy does not have: " + ", ".join(unknown))

    for example in examples:
        name = example["name"]
        if name in written:
            # A line kept for a written example says how it differs from Bevy's, and one marked
            # part names the feature it leaves out.
            state, note = triage.get(name, ("can", ""))
            example["state"], example["note"] = ("part" if state == "part" else "written"), note
        elif name in triage:
            example["state"], example["note"] = triage[name]
        else:
            sys.exit(f"{name} ({example['group']}) has no line in triage.tsv and is not written")

    groups = [group for group in order if any(example["group"] == group for example in examples)]
    groups += sorted({example["group"] for example in examples} - set(groups) - {HIDDEN})
    if any(example["group"] == HIDDEN for example in examples):
        groups.append(HIDDEN)

    def counts(rows):
        return {state: sum(1 for row in rows if row["state"] == state) for state in ORDER}

    # B 4 of NORM.md, an example keeping on an entity what Bevy's example keeps on one, in a
    # behavior. Bevy's keeps state on an entity where it declares a component of its own, and the
    # program here does where it declares a behavior, which a reviewer reads further.
    for example in examples:
        example["components"] = example["state"] in ("written", "part") and keeps_components(source, example)
        example["behaviors"] = example["components"] and on_behaviors(written[example["name"]])

    def kept(rows):
        rows = list(rows)
        return sum(1 for row in rows if row["behaviors"]), sum(1 for row in rows if row["components"])

    total = counts(examples)
    # A missing row declined rather than waiting says so first in its note.
    declined = sum(1 for example in examples if example["state"] == "missing" and example["note"].startswith("declined"))
    listed = [example for example in examples if example["group"] != HIDDEN]
    applies = len(examples) - total["n/a"]

    out = []
    out.append("# Examples")
    out.append("")
    out.append(
        f"Bevy {version} has {len(examples)} examples, {len(listed)} of them in the list its "
        f"`examples/README.md` keeps and {len(examples) - len(listed)} kept out of it. Each is a "
        "row here, made by `build/examples-table.py` from Bevy's own metadata, so a row is a "
        "feature of Bevy and the table is how much of Bevy a C# game can reach.")
    out.append("")
    out.append(
        "An example written here is a program in `BevyCSharp.Examples`, under Bevy's name, opened "
        "by `dotnet run --project BevyCSharp.Examples -- <name>` or `./bcs open --example <name>`. "
        "One `written in part` leaves out a feature of Bevy's the bridge lacks and names it. One that "
        "`can be written` uses only what is bridged and waits for its turn. One that is `missing` "
        "names what the bridge lacks, or why the crate it would bring is declined, and one that "
        "`does not apply` says why it is not a thing a C# "
        "game does, most often because it is about Rust itself. A difference that is no feature, "
        "such as a view sized for another window, is said in a written row and keeps it written.")
    out.append("")
    out.append(
        f"**{total['written']} written, {total['part']} written in part, {total['can']} can be written, "
        f"{total['missing']} missing, {declined} of them declined, and {total['n/a']} do not apply.** Of the {applies} that apply, "
        f"{total['written'] + total['part'] + total['can']} can be written with what is bridged, "
        f"{total['part']} of them leaving something out.")
    out.append("")
    on, of = kept(examples)
    out.append(
        f"**{on} of the {of} written whose Bevy example keeps state on an entity, in a component of "
        "its own, keep it on the entity in a behavior here,** as B 4 of NORM.md has it, the rest "
        "keeping it in static fields until their group is brought over. The last column counts them "
        "by group.")
    out.append("")
    out.append("| Group | Written | Written in part | Can be written | Missing | Does not apply | In behaviors |")
    out.append("|---|---:|---:|---:|---:|---:|---:|")
    for group in groups:
        c = counts([example for example in examples if example["group"] == group])
        anchor = re.sub(r"[^a-z0-9 -]", "", group.lower()).replace(" ", "-")
        group_on, group_of = kept(example for example in examples if example["group"] == group)
        behaviors = f"{group_on} of {group_of}" if group_of else ""
        out.append(f"| [{group}](#{anchor}) | {c['written']} | {c['part']} | {c['can']} | {c['missing']} | {c['n/a']} | {behaviors} |")
    out.append(f"| **All** | **{total['written']}** | **{total['part']}** | **{total['can']}** | **{total['missing']}** | **{total['n/a']}** | **{on} of {of}** |")
    out.append("")
    out.append(
        "A row's example links to Bevy's source, at the release the bridge builds. A written "
        "one's state links to its program here, and its capture is in `.github/assets/examples`, a "
        "picture of what it draws or, for one with nothing to draw, the text it prints. Every "
        "picture is drawn at Bevy's window of 1280 by 720, or the size the example asks for, and "
        "kept at that size as WebP, lossless for a 2D or interface example and at quality 85 for a "
        "3D one, so a label reads as Bevy draws it and a sky does not band. A picture of another "
        "shape than sixteen by nine is scaled to fill 1280 by 720 and cut around its middle, so the "
        "README shows every picture whole in a box of one shape.")

    for group in groups:
        out.append("")
        out.append(f"## {group}")
        out.append("")
        out.append("| Example | What it shows | State |")
        out.append("|---|---|---|")
        for example in sorted((e for e in examples if e["group"] == group), key=lambda e: e["name"]):
            release = followed.get(example["name"], version)
            source = f"https://github.com/bevyengine/bevy/blob/v{release}/{example['path']}" if example["path"] else ""
            name = f"[`{example['name']}`]({source})" if source else f"`{example['name']}`"
            state = STATES[example["state"]]
            if example["state"] in ("written", "part"):
                state = f"[{STATES[example['state']]}](../{written[example['name']]})"
                # One with nothing to draw is captured as what it prints, which the row links.
                if os.path.exists(os.path.join(CAPTURES, example["name"] + ".txt")):
                    state += f", prints [its output](assets/examples/{example['name']}.txt)"
            if example["note"]:
                state += f", {example['note']}"
            description = example["description"].replace("|", "\\|") or " "
            out.append(f"| {name} | {description} | {state} |")

    table = "\n".join(out) + "\n"

    # Every written example's capture for the README, a table a group in the order of the groups
    # above, each one row of the group's examples in the order Bevy's examples/README.md lists them,
    # so a group wider than the page scrolls sideways rather than wrapping onto rows of its own. Each
    # picture opens the C# file that drew it. The tables are Markdown, since nuget.org prints an
    # HTML table as its text, where GitHub and nuget.org each draw a Markdown one as a block that
    # scrolls sideways, measured on both on 2026-10-19.
    sections = []
    shown = []
    for group in groups:
        pictured = sorted(
            (example for example in examples if example["group"] == group and example["state"] in ("written", "part")
             and os.path.exists(os.path.join(CAPTURES, example["name"] + ".webp"))),
            key=lambda example: (places.get(example["name"], len(places)), example["name"]))
        if not pictured:
            continue
        heads, captions = [], []
        for example in pictured:
            name, link = example["name"], BLOB + written[example["name"]]
            shown.append(name)
            heads.append(f"[![{name}]({RAW}.github/assets/gallery/{name}.webp)]({link})")
            # The caption sets each column's width, its name padded with spaces that do not break to
            # thirty-two characters, about the picture's width in code, so a column is as wide as
            # its picture whatever the name. A browser laying out a table wider than the page shrinks
            # each column to the widest thing in it that cannot break, and a picture that may be no
            # wider than its column, as both sites have it, shrank to its name in a column without.
            captions.append(f"[`{name}" + "\u00a0" * max(0, 32 - len(name)) + f"`]({link})")
        # The heading says how many of the group's examples are written, and how many Bevy's group
        # holds where that is more, so a reader sees how far each group has come without counting
        # pictures across a row wider than the page. Written ones rather than the pictures in the
        # row, since one with nothing to draw prints instead and has no picture there, and a group
        # of those would read as barely begun.
        rows = [example for example in examples if example["group"] == group]
        done = sum(1 for example in rows if example["state"] == "written")
        count = f"{done} of {len(rows)}" if len(rows) > done or done == 1 else f"all {done}"
        # The pictures are the table's head, so each name stands under its picture.
        sections.append(f"### {group}, {count}\n| " + " | ".join(heads) + " |\n|" + "---|" * len(heads)
                        + "\n| " + " | ".join(captions) + " |")
    gallery = "\n\n".join(sections)
    status = (
        f"Of Bevy's {len(examples)} examples, {total['written']} are written in C# here and "
        f"{total['part']} more in part, {total['can']} more can be with what is bridged, {total['missing'] - declined} wait on something "
        f"the bridge lacks, {declined} are declined and {total['n/a']} are about Rust itself ([EXAMPLES.md]({BLOB}.github/EXAMPLES.md)).")
    return table, status, gallery, shown


def made_from():
    """The capture each picture of the gallery was made from, as its SHA-256 by the example's name."""
    if not os.path.exists(MADE_FROM):
        return {}
    with open(MADE_FROM, encoding="utf-8") as listed:
        return dict(line.rstrip("\n").split("\t") for line in listed if line.strip() and not line.startswith("#"))


def digest(path):
    with open(path, "rb") as file:
        return hashlib.sha256(file.read()).hexdigest()


def make_pictures(shown, check):
    """Makes the gallery's pictures whose captures changed, or names them where only checking.

    A picture is made again where it is missing or its capture's SHA-256 is not the one it was made
    from, and one no example shows any longer is taken away. ImageMagick makes it where there is
    one, and libwebp's cwebp where ImageMagick was built without WebP, as the captures are written.
    """
    listed = made_from()
    wanted = {name: digest(os.path.join(CAPTURES, name + ".webp")) for name in shown}
    stale = [name for name, sha in wanted.items()
             if listed.get(name) != sha or not os.path.exists(os.path.join(GALLERY, name + ".webp"))]
    present = {os.path.basename(path)[:-5] for path in glob.glob(os.path.join(GALLERY, "*.webp"))}
    gone = sorted((present | set(listed)) - set(wanted))
    if check:
        return [os.path.join(GALLERY, name + ".webp") for name in sorted(stale) + gone]

    os.makedirs(GALLERY, exist_ok=True)
    magick = shutil.which("magick") or shutil.which("convert")
    width, height = THUMBNAIL
    for name in stale:
        source, picture = os.path.join(CAPTURES, name + ".webp"), os.path.join(GALLERY, name + ".webp")
        if magick:
            command = [magick, source, "-resize", f"{width}x{height}!", "-quality", "85", "-strip", picture]
        elif shutil.which("cwebp"):
            command = ["cwebp", "-quiet", "-q", "85", "-resize", str(width), str(height), source, "-o", picture]
        else:
            sys.exit("ImageMagick or cwebp is needed to make the gallery's pictures")
        subprocess.run(command, check=True)
    for name in gone:
        if os.path.exists(os.path.join(GALLERY, name + ".webp")):
            os.remove(os.path.join(GALLERY, name + ".webp"))
    with open(MADE_FROM, "w", encoding="utf-8") as out:
        out.write("# The capture each picture here was made from, by its SHA-256, which build/examples-table.py\n"
                  "# reads to make a picture again where its capture was taken again.\n")
        out.writelines(f"{name}\t{wanted[name]}\n" for name in sorted(wanted))
    return []


def head(example, version):
    """The comment an example opens with, naming the example of Bevy's it is written from."""
    words = (f"Bevy's {example['name']} example, {example['path']} at v{version}, by Bevy's contributors "
             "under MIT or Apache-2.0, written again in C#.")
    return ["// " + line for line in textwrap.wrap(words, 97)]


def release_of(version):
    """A release's number as a tuple, to tell an earlier one from a later one."""
    return tuple(int(part) for part in version.split("."))


def followed_releases(written, version):
    """The release each written example follows where its head names one earlier than the lock's."""
    followed = {}
    for name, path in written.items():
        with open(os.path.join(ROOT, path), encoding="utf-8") as file:
            head = " ".join(line[3:] for line in file.read().split("\n\n")[0].split("\n"))
        found = re.match(rf"Bevy's {re.escape(name)} example, \S+ at v(\d+\.\d+\.\d+),", head)
        if found and release_of(found.group(1)) < release_of(version):
            followed[name] = found.group(1)
    return followed


def heads(examples, written, version, followed):
    """Each written example's file as it reads with its head, by its path, where that differs."""
    changed = {}
    for example in examples:
        path = written.get(example["name"])
        if not path or not example["path"]:
            continue
        with open(os.path.join(ROOT, path), encoding="utf-8") as file:
            lines = file.read().split("\n")

        # The head written before, up to the blank line after it, is replaced rather than kept.
        if lines[0].startswith(f"// Bevy's {example['name']} example,") and "" in lines:
            lines = lines[lines.index("") + 1:]

        text = "\n".join(head(example, followed.get(example["name"], version)) + [""] + lines)
        with open(os.path.join(ROOT, path), encoding="utf-8") as file:
            if file.read() != text:
                changed[path] = text
    return changed


def main():
    check = "--check" in sys.argv[1:]
    version = bevy_version()
    source = bevy_source(version)
    examples = read_examples(source)
    followed = followed_releases(written_examples(), version)
    table, status, gallery, shown = build(version, examples, group_order(source), read_triage(), written_examples(), source, followed)

    with open(README, encoding="utf-8") as readme:
        text = readme.read()
    marked = re.compile(r"(<!-- examples -->\n)(?:.*?\n)?(<!-- /examples -->)", re.S)
    if not marked.search(text):
        sys.exit("README.md has no <!-- examples --> and <!-- /examples --> around the examples' count")
    readme_text = marked.sub(lambda match: match.group(1) + status + "\n" + match.group(2), text, count=1)

    pictures = re.compile(r"(<!-- example-gallery -->\n)(?:.*?\n)?(<!-- /example-gallery -->)", re.S)
    if not pictures.search(readme_text):
        sys.exit("README.md has no <!-- example-gallery --> and <!-- /example-gallery --> around the captures")
    readme_text = pictures.sub(lambda match: match.group(1) + (gallery + "\n" if gallery else "") + match.group(2), readme_text, count=1)

    current = open(TABLE, encoding="utf-8").read() if os.path.exists(TABLE) else ""
    changed = heads(examples, written_examples(), version, followed)
    if check:
        stale = [path for path, old, new in ((TABLE, current, table), (README, text, readme_text)) if old != new]
        stale += [os.path.join(ROOT, path) for path in sorted(changed)]
        stale += make_pictures(shown, check=True)
        if stale:
            sys.exit("out of date, run build/examples-table.py: " + ", ".join(os.path.relpath(p, ROOT) for p in stale))
        return

    make_pictures(shown, check=False)
    with open(TABLE, "w", encoding="utf-8") as out:
        out.write(table)
    with open(README, "w", encoding="utf-8") as out:
        out.write(readme_text)
    for path, text in changed.items():
        with open(os.path.join(ROOT, path), "w", encoding="utf-8") as out:
            out.write(text)
    print(status)


if __name__ == "__main__":
    main()
