"""Compares each example's capture with the one checked in, and fails where too much of it
differs, so an example that draws or prints differently from when its capture was taken is named in
the run that changed it.

    python3 build/compare-captures.py <captures folder> [--against .github/assets/examples]

A pixel differs where one of its channels is further than STEP from the checked-in picture's, and a
picture differs where more than SHARE of its pixels do, which the edges, text and filtering of two
devices take between them, as 3DEngine's reference frames are held. A failure is said with its
share and the box its differing pixels lie in. An example that prints is compared as text, line by
line with every number set apart, since what it prints holds frame times and counts measured as it
ran, and its share is of its lines.

An example allowed more is a line of build/captures-differ.txt, its name and the share it was
measured at, then a tab and why. One with nothing checked in is said and passed over, since a new
example's capture is taken in the commit that writes it. Pictures are decoded to PPM by
ImageMagick, or by libwebp's dwebp where ImageMagick was built without WebP, as
build/capture-examples.sh decodes them.
"""

import os
import re
import shutil
import subprocess
import sys

# How far a channel may move before its pixel counts as differing, out of 255.
STEP = 24

# The share of pixels, or of lines, that may differ before the capture does.
SHARE = 0.02

ALLOWED = os.path.join(os.path.dirname(os.path.abspath(__file__)), "captures-differ.txt")

# A number as an example prints one, a sign, digits and a fraction or an exponent.
NUMBER = re.compile(r"[-+]?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?")


def decode(path):
    """The picture's width, height and RGB bytes."""
    magick = shutil.which("magick") or shutil.which("convert")
    data = b""
    if magick:
        data = subprocess.run([magick, path, "ppm:-"], capture_output=True).stdout
    if not data and shutil.which("dwebp"):
        data = subprocess.run(["dwebp", "-quiet", path, "-ppm", "-o", "-"], capture_output=True).stdout
    if not data:
        raise ValueError(f"{path} could not be decoded")

    fields, at = [], 0
    while len(fields) < 4:
        while data[at:at + 1].isspace():
            at += 1
        if data[at:at + 1] == b"#":
            at = data.index(b"\n", at)
            continue
        end = at
        while not data[end:end + 1].isspace():
            end += 1
        fields.append(data[at:end])
        at = end

    width, height = int(fields[1]), int(fields[2])
    return width, height, data[at + 1:at + 1 + width * height * 3]


def differing(width, first, second):
    """How many pixels of two pictures of one size differ past STEP in a channel, and the box they
    lie in as left, top, right and bottom, or None where none do."""
    count, box = 0, None
    for at in range(0, len(first), 3):
        if (abs(first[at] - second[at]) > STEP
                or abs(first[at + 1] - second[at + 1]) > STEP
                or abs(first[at + 2] - second[at + 2]) > STEP):
            count += 1
            x, y = (at // 3) % width, (at // 3) // width
            box = (x, y, x, y) if box is None else (min(box[0], x), min(box[1], y), max(box[2], x), max(box[3], y))
    return count, box


def lines(path):
    """A printed capture's lines, every number in them set apart as a mark."""
    with open(path, encoding="utf-8", errors="replace") as text:
        return [NUMBER.sub("#", line.rstrip()) for line in text]


def allowed():
    """The examples allowed more than SHARE, by name."""
    shares = {}
    if not os.path.exists(ALLOWED):
        return shares
    with open(ALLOWED, encoding="utf-8") as listed:
        for line in listed:
            if not line.strip() or line.startswith("#"):
                continue
            name, share = line.split("\t", 1)[0].split()
            shares[name] = float(share)
    return shares


def compare_picture(example, capture, reference, limit):
    """Why a picture fails, or None where it passes."""
    width, height, pixels = decode(capture)
    reference_width, reference_height, reference_pixels = decode(reference)
    if (width, height) != (reference_width, reference_height):
        return f"{example} is {width}x{height} where its picture is {reference_width}x{reference_height}"

    count, box = differing(width, pixels, reference_pixels)
    share = count / (width * height)
    if share <= limit:
        return None
    left, top, right, bottom = box
    return (f"{example} differs in {share:.1%} of its pixels, past the {limit:.1%} it is allowed, "
            f"within x {left} to {right} and y {top} to {bottom}")


def compare_text(example, capture, reference, limit):
    """Why a printed capture fails, or None where it passes."""
    printed, kept = lines(capture), lines(reference)
    longest = max(len(printed), len(kept), 1)
    changed = [i for i in range(longest) if (printed[i] if i < len(printed) else None) != (kept[i] if i < len(kept) else None)]
    share = len(changed) / longest
    if share <= limit:
        return None
    return (f"{example} prints {len(changed)} of {longest} lines otherwise, {share:.0%}, past the {limit:.0%} it is "
            f"allowed, the first being line {changed[0] + 1}")


def main(arguments):
    if not arguments or arguments[0].startswith("-"):
        print(__doc__, file=sys.stderr)
        return 2

    captures = arguments[0]
    against = arguments[arguments.index("--against") + 1] if "--against" in arguments else ".github/assets/examples"
    shares = allowed()
    failed, compared, missing = [], 0, []

    for name in sorted(os.listdir(captures)):
        example, kind = os.path.splitext(name)
        if kind not in (".webp", ".txt"):
            continue
        reference = os.path.join(against, name)
        if not os.path.exists(reference):
            missing.append(example)
            continue

        compare = compare_picture if kind == ".webp" else compare_text
        compared += 1
        why = compare(example, os.path.join(captures, name), reference, shares.get(example, SHARE))
        if why is not None:
            failed.append(why)

    for example in missing:
        print(f"{example} has nothing checked in to compare with")

    if failed:
        print(f"{len(failed)} of {compared} captures differ from theirs:", file=sys.stderr)
        for line in failed:
            print(f"  {line}", file=sys.stderr)
        # Every one in the workflow's error, which a reader not signed in sees.
        if os.environ.get("GITHUB_ACTIONS") == "true":
            print(f"::error title={len(failed)} captures differ::" + "%0A".join(failed))
        return 1

    print(f"{compared} captures match theirs")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
