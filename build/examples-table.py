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
"""

import glob
import os
import re
import sys
import tomllib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TABLE = os.path.join(ROOT, ".github", "EXAMPLES.md")
README = os.path.join(ROOT, "README.md")
TRIAGE = os.path.join(ROOT, "BevyCSharp.Examples", "triage.tsv")
EXAMPLES = os.path.join(ROOT, "BevyCSharp.Examples")
CAPTURES = os.path.join(ROOT, ".github", "assets", "examples")

HIDDEN = "Kept out of Bevy's list"
STATES = {"written": "written", "can": "can be written", "missing": "missing", "n/a": "does not apply"}
ORDER = ["written", "can", "missing", "n/a"]


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
            if len(parts) < 2 or parts[1] not in ("can", "missing", "n/a"):
                sys.exit(f"triage.tsv:{number}: '{line}' is not a name, a state and a note")
            if parts[1] in ("missing", "n/a") and (len(parts) < 3 or not parts[2]):
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


def build(version, examples, order, triage, written):
    unknown = sorted(set(triage) - {example["name"] for example in examples})
    if unknown:
        sys.exit("triage.tsv names examples Bevy does not have: " + ", ".join(unknown))

    for example in examples:
        name = example["name"]
        if name in written:
            # A line kept for a written example says how it differs from Bevy's.
            example["state"], example["note"] = "written", triage.get(name, ("", ""))[1]
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

    total = counts(examples)
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
        "One that `can be written` uses only what is bridged and waits for its turn. One that is "
        "`missing` names what the bridge lacks, and one that `does not apply` says why it is not "
        "a thing a C# game does, most often because it is about Rust itself.")
    out.append("")
    out.append(
        f"**{total['written']} written, {total['can']} can be written, {total['missing']} missing "
        f"and {total['n/a']} do not apply.** Of the {applies} that apply, "
        f"{total['written'] + total['can']} can be written with what is bridged.")
    out.append("")
    out.append("| Group | Written | Can be written | Missing | Does not apply |")
    out.append("|---|---:|---:|---:|---:|")
    for group in groups:
        c = counts([example for example in examples if example["group"] == group])
        anchor = re.sub(r"[^a-z0-9 -]", "", group.lower()).replace(" ", "-")
        out.append(f"| [{group}](#{anchor}) | {c['written']} | {c['can']} | {c['missing']} | {c['n/a']} |")
    out.append(f"| **All** | **{total['written']}** | **{total['can']}** | **{total['missing']}** | **{total['n/a']}** |")
    out.append("")
    out.append(
        "A row's example links to Bevy's source, at the release the bridge builds. A written "
        "one's state links to its program here, and its capture is in `.github/assets/examples`.")

    for group in groups:
        out.append("")
        out.append(f"## {group}")
        out.append("")
        out.append("| Example | What it shows | State |")
        out.append("|---|---|---|")
        for example in sorted((e for e in examples if e["group"] == group), key=lambda e: e["name"]):
            source = f"https://github.com/bevyengine/bevy/blob/v{version}/{example['path']}" if example["path"] else ""
            name = f"[`{example['name']}`]({source})" if source else f"`{example['name']}`"
            state = STATES[example["state"]]
            if example["state"] == "written":
                state = f"[written](../{written[example['name']]})"
            if example["note"]:
                state += f", {example['note']}"
            description = example["description"].replace("|", "\\|") or " "
            out.append(f"| {name} | {description} | {state} |")

    table = "\n".join(out) + "\n"

    # Every written example's capture, four to a row, for the README.
    shown = [example for example in examples if example["state"] == "written"
             and os.path.exists(os.path.join(CAPTURES, example["name"] + ".png"))]
    gallery = []
    for start in range(0, len(shown), 4):
        cells = [
            f'<td><img src=".github/assets/examples/{example["name"]}.png" width="200"/><br><code>{example["name"]}</code></td>'
            for example in shown[start:start + 4]
        ]
        gallery.append("<tr>" + "".join(cells) + "</tr>")
    gallery = "<table>\n" + "\n".join(gallery) + "\n</table>" if gallery else ""
    status = (
        f"Of Bevy's {len(examples)} examples, {total['written']} are written in C# here, "
        f"{total['can']} more can be with what is bridged, {total['missing']} wait on something "
        f"the bridge lacks and {total['n/a']} are about Rust itself ([EXAMPLES.md](.github/EXAMPLES.md)).")
    return table, status, gallery


def main():
    check = "--check" in sys.argv[1:]
    version = bevy_version()
    source = bevy_source(version)
    table, status, gallery = build(version, read_examples(source), group_order(source), read_triage(), written_examples())

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
    if check:
        stale = [path for path, old, new in ((TABLE, current, table), (README, text, readme_text)) if old != new]
        if stale:
            sys.exit("out of date, run build/examples-table.py: " + ", ".join(os.path.relpath(p, ROOT) for p in stale))
        return

    with open(TABLE, "w", encoding="utf-8") as out:
        out.write(table)
    with open(README, "w", encoding="utf-8") as out:
        out.write(readme_text)
    print(status)


if __name__ == "__main__":
    main()
