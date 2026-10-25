#!/usr/bin/env python3
"""Builds the vegetation's texture collection in BevyCSharp/Vegetation/Textures from the sources the
manifest beside it, textures.json, names, by the recipe it gives each entry.

    build/textures.py                     # import every entry whose sources are here
    build/textures.py NAME ...            # import the entries named
    build/textures.py --out DIR NAME ...  # write the entries under DIR, the collection left as it is
    build/textures.py --sample DIR        # write an entry made from no source under DIR, for the tests

An entry is three KTX2 files with their mipmaps, each level's blocks compressed with zstd, read by
Bevy's loader through the bridge's ktx2 and zstd_rust features:

    albedo.ktx2   BC7, sRGB     the color in RGB and the opacity in A
    normal.ktx2   BC5           the tangent-space normal's X and Y, which Bevy reads as a two-channel map
    surface.ktx2  BC7, linear   occlusion in R, roughness in G, metallic in B and translucency in A,
                                as Bevy's material reads its occlusion, metallic-roughness and
                                diffuse transmission textures, so one file is bound to all three
    depth.ktx2    BC4           a bark's depth for parallax occlusion, white the deepest, as Bevy's
                                material reads its depth map, half a byte a texel

A bark's depth is its source's height turned over where the recipe names one, its grays stretched to
black and white, and otherwise the height its normal map's slopes add up to, integrated over the whole picture at once through its
Fourier transform, which a bark's tiling suits, scaled so its lowest and highest hundredths are
black and white, and softened by a texel, as a steep step in a depth map shows as a jagged edge.

A desktop GPU samples BC7 and BC5 as they are, a byte a texel. The normal and the surface are half the
albedo's size, which a bark or a leaf does not show and which keeps a species under 3 MB. The
encoding is the one function encode() below, so another format, Basis Universal's among them, is a
second branch of it.

The encoders are written here, BC7's mode 6 with each block's endpoints along its principal axis and
fitted again by least squares, and BC4 twice for BC5, so the import needs Python 3.14, whose own
compression module has zstd, and Pillow, and nothing else. Each level is made from the one above by a
box filter, and the color under a cut-out leaf's transparent texels is the color of the leaf nearest,
so no dark rim shows as it shrinks. A leaf atlas's opacity is scaled at each level so the share of
each leaf's box past one half, read between the level's texels as a card samples it, stays the top
level's on average, so a leaf holds in the distance until its box is about a texel across. Keeping
the share of the whole atlas's texels instead loses the leaves a level sooner, where a box two
texels across is averaged with the transparent texels round it.

A source is a folder of .vegetation/, which the repository leaves out, the one the manifest names.
An entry is imported into its folder in the collection, which the library embeds, and one the
manifest marks waiting into waiting/, which the repository and the build leave out, so it is read
where it was imported and never carried, and is named in a textures.json there that
TreeTextures.Add reads, as --out's and --sample's folders are. Each import into the collection
writes the files' sizes, the encoders' error, the mean color and surface of the opaque texels and a
leaf set's boxes back into the manifest. The same sources and recipe make the same bytes, which the
tests hold.
"""

import argparse
import json
import math
import os
import struct
import sys
from compression import zstd

from PIL import Image, ImageChops, ImageEnhance, ImageFilter, ImageMath

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
COLLECTION = os.path.join(ROOT, "BevyCSharp", "Vegetation", "Textures")
MANIFEST = os.path.join(COLLECTION, "textures.json")

# Vulkan's numbers for the formats, which a KTX2 file names its texels by.
BC4_UNORM = 139
BC5_UNORM = 141
BC7_UNORM = 145
BC7_SRGB = 146

# The Khronos data format descriptor's color models and transfer functions for them.
MODEL_BC4 = 131
MODEL_BC5 = 132
MODEL_BC7 = 134
TRANSFER_LINEAR = 1
TRANSFER_SRGB = 2

ZSTD_LEVEL = 19

# BC7's interpolation weights for four-bit indices, in sixty-fourths, and the nearest index for each
# sixty-fourth, so a texel's place between the endpoints is turned into its index by a lookup.
WEIGHTS = (0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64)
NEAREST = [min(range(16), key=lambda i: abs(WEIGHTS[i] - t)) for t in range(65)]


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("names", nargs="*", help="the entries to import, every one whose sources are here where none is named")
    parser.add_argument("--out", help="write the entries under this folder, the collection and its manifest left as they are")
    parser.add_argument("--sample", metavar="DIR", help="write an entry made from no source into DIR and stop")
    arguments = parser.parse_args()

    if arguments.sample:
        write_entry(sample_maps(), os.path.join(arguments.sample, "sample"), "bark")
        list_entry(arguments.sample, "sample", "bark")
        return 0

    with open(MANIFEST, encoding="utf-8") as file:
        manifest = json.load(file)

    named = set(arguments.names)
    unknown = named - {entry["name"] for entry in manifest["entries"]}
    if unknown:
        print(f"textures.py: the manifest has no entry {', '.join(sorted(unknown))}", file=sys.stderr)
        return 2

    failed = False
    for entry in manifest["entries"]:
        if named and entry["name"] not in named:
            continue
        source = os.path.join(ROOT, entry["source"])
        if not os.path.isdir(source):
            if named:
                print(f"textures.py: {entry['name']}'s source {entry['source']} is not here", file=sys.stderr)
                failed = True
            continue

        root = arguments.out or (COLLECTION if ships(entry) else os.path.join(COLLECTION, "waiting"))
        maps = read_entry(entry, source)
        boxes = leaf_boxes(maps["albedo"]) if entry["kind"] == "leaves" else None
        report = write_entry(maps, os.path.join(root, entry["name"]), entry["kind"], boxes)
        if root != COLLECTION:
            list_entry(root, entry["name"], entry["kind"])
        print(f"{entry['name']}: {', '.join(f'{file} {size // 1024} KB, {error:.1f} dB' for file, (size, error) in report.items())}"
              + ("" if ships(entry) else ", waiting"))
        if not arguments.out:
            entry["files"] = {file: size for file, (size, _) in report.items()}
            entry["error"] = {file: round(error, 1) for file, (_, error) in report.items()}
            entry["color"] = mean_color(maps["albedo"])
            entry["surface"] = mean_surface(maps)
            if boxes is not None:
                entry["leaves"] = boxes

    if not arguments.out:
        with open(MANIFEST, "w", encoding="utf-8", newline="\n") as file:
            json.dump(manifest, file, indent=2, ensure_ascii=False)
            file.write("\n")
    return 1 if failed else 0


def list_entry(root, name, kind):
    """
    Names an entry in the textures.json of a folder of entries other than the collection, which
    TreeTextures.Add reads, kept in the order of the names.
    """
    path = os.path.join(root, "textures.json")
    listed = {"entries": []}
    if os.path.exists(path):
        with open(path, encoding="utf-8") as file:
            listed = json.load(file)
    entries = [entry for entry in listed["entries"] if entry["name"] != name] + [{"name": name, "kind": kind}]
    listed["entries"] = sorted(entries, key=lambda entry: entry["name"])
    with open(path, "w", encoding="utf-8", newline="\n") as file:
        json.dump(listed, file, indent=2)
        file.write("\n")


def ships(entry):
    """Whether an entry is carried, which every one is that the manifest does not mark waiting."""
    return not entry.get("waiting")


# The recipe.

def read_entry(entry, source):
    """The entry's albedo, normal and surface at their sizes, from its parts as the recipe places them."""
    width, height = entry["size"]
    albedo = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    normal = Image.new("RGB", (max(width // 2, 4), max(height // 2, 4)), (128, 128, 255))
    surface = Image.new("RGBA", normal.size, (255, 255, 0, 0))

    for part in entry["parts"]:
        place = part.get("place", [0, 0, 1, 1])
        box = placed(place, albedo.size)
        small = placed(place, normal.size)

        color = read_map(source, part["albedo"], "RGB")
        opacity = read_channel(source, part.get("opacity", 1.0), color.size)
        if part.get("fit"):
            box = fitted(box, color.size)
            small = fitted(small, color.size)
        piece = color.resize(size_of(box), Image.Resampling.LANCZOS)
        piece.putalpha(opacity.resize(size_of(box), Image.Resampling.LANCZOS))
        albedo.paste(piece, box[:2])

        if part.get("normal"):
            bumps = read_map(source, part["normal"], "RGB")
            if part["normal"].get("flip_green"):
                r, g, b = bumps.split()
                bumps = Image.merge("RGB", (r, ImageChops.invert(g), b))
            normal.paste(bumps.resize(size_of(small), Image.Resampling.LANCZOS), small[:2])

        size = color.size
        occlusion = read_channel(source, part.get("occlusion", 1.0), size)
        roughness = read_channel(source, part.get("roughness", 0.8), size)
        metallic = read_channel(source, 0.0, size)
        if part.get("translucency") == "albedo":
            translucency = color.convert("L").point(lambda value: min(255, int(value * part.get("translucency_gain", 1.0))))
        else:
            translucency = read_channel(source, part.get("translucency", 0.0), size)
        layers = Image.merge("RGBA", (occlusion, roughness, metallic, translucency))
        piece = resized(layers, size_of(small), Image.Resampling.LANCZOS)
        mask = opacity.resize(size_of(small), Image.Resampling.LANCZOS)
        surface.paste(piece, small[:2], mask if part.get("opacity") is not None else None)

    maps = {"albedo": albedo, "normal": normal, "surface": surface}
    if entry["kind"] == "bark":
        part = entry["parts"][0]
        if "depth" in part:
            height = stretched(read_channel(source, part["depth"], normal.size))
        else:
            height = height_of(normal)
        maps["depth"] = ImageChops.invert(height).filter(ImageFilter.GaussianBlur(1))
    return maps


def stretched(image):
    """A one-channel image with its lowest and highest hundredths made black and white, as a source's height is often a narrow band of grays."""
    histogram = image.histogram()
    total = sum(histogram)
    counted, low, high = 0, 0, 255
    for value, count in enumerate(histogram):
        counted += count
        if counted <= total // 100:
            low = value + 1
        if counted < total - total // 100:
            high = value + 1
    span = max(high - low, 1)
    return image.point(lambda value: max(0, min(255, round(255 * (value - low) / span))))


def height_of(normal):
    """
    The height a tangent-space normal map's slopes add up to, the slope along a row minus its X over
    its Z and down a column its Y over its Z, as an OpenGL map's Y points up the picture, solved for
    over the whole map at once in the Fourier domain as Frankot and Chellappa solve it, the waves
    longer than a quarter of it faded out, its lowest and highest hundredths black and white.
    """
    width, height = normal.size
    data = normal.convert("RGB").tobytes()
    along, down = [], []
    for i in range(width * height):
        x, y, z = (data[3 * i + c] / 127.5 - 1.0 for c in range(3))
        z = max(z, 0.1)
        along.append(complex(-x / z))
        down.append(complex(y / z))
    first, second = fourier(along, width, height), fourier(down, width, height)
    solved = [0j] * (width * height)

    # The waves longer than a quarter of the picture faded out, as a normal map's slopes drift a
    # little over its whole width, which summed would tilt the bark's depth across it.
    cutoff = 2 * math.pi * 4 / min(width, height)
    for row in range(height):
        v = (row if row < height // 2 else row - height) * 2 * math.pi / height
        for column in range(width):
            u = (column if column < width // 2 else column - width) * 2 * math.pi / width
            if u or v:
                at = column + width * row
                kept = 1.0 - math.exp(-(u * u + v * v) / (cutoff * cutoff))
                solved[at] = kept * (-1j * u * first[at] - 1j * v * second[at]) / (u * u + v * v)
    values = [value.real for value in fourier(solved, width, height, invert=True)]
    ordered = sorted(values)
    low, high = ordered[len(ordered) // 100], ordered[-1 - len(ordered) // 100]
    span = high - low or 1.0
    image = Image.new("L", (width, height))
    image.putdata([max(0, min(255, round(255 * (value - low) / span))) for value in values])
    return image


def fourier(values, width, height, invert=False):
    """A picture's two-dimensional discrete Fourier transform, or its inverse, its rows and then its columns."""
    rows = [transform(values[row * width:(row + 1) * width], invert) for row in range(height)]
    out = [0j] * (width * height)
    for column in range(width):
        transformed = transform([rows[row][column] for row in range(height)], invert)
        for row in range(height):
            out[column + width * row] = transformed[row]
    return out


def transform(values, invert):
    """The Fourier transform of a power of two of numbers, by Cooley and Tukey's halving."""
    count = len(values)
    out = list(values)
    j = 0
    for i in range(1, count):
        bit = count >> 1
        while j & bit:
            j ^= bit
            bit >>= 1
        j |= bit
        if i < j:
            out[i], out[j] = out[j], out[i]
    length = 2
    while length <= count:
        angle = (2 * math.pi / length) * (1 if invert else -1)
        step = complex(math.cos(angle), math.sin(angle))
        half = length // 2
        for start in range(0, count, length):
            turn = 1 + 0j
            for k in range(half):
                a = out[start + k]
                b = out[start + k + half] * turn
                out[start + k] = a + b
                out[start + k + half] = a - b
                turn *= step
        length <<= 1
    return [value / count for value in out] if invert else out


def read_map(source, spec, mode):
    """A map of a recipe, its file read, cropped and changed in color as it says."""
    image = Image.open(os.path.join(source, spec["file"]))
    image.load()
    if "crop" in spec:
        x, y, w, h = spec["crop"]
        image = image.crop((x, y, x + w, y + h))
    image = image.convert(mode)
    if mode == "RGB" and "color" in spec:
        image = recolor(image, spec["color"])
    return image


def read_channel(source, spec, size):
    """One channel of a recipe, a number from zero to one over the whole size or a channel of a file."""
    if isinstance(spec, (int, float)):
        return Image.new("L", size, round(255 * spec))
    image = Image.open(os.path.join(source, spec["file"]))
    image.load()
    if "crop" in spec:
        x, y, w, h = spec["crop"]
        image = image.crop((x, y, x + w, y + h))
    channel = spec.get("channel", "l")
    image = image.convert("L") if channel == "l" else image.convert("RGBA").getchannel(channel.upper())
    if spec.get("invert"):
        image = ImageChops.invert(image)
    if "gain" in spec or "bias" in spec:
        gain, bias = spec.get("gain", 1.0), spec.get("bias", 0.0)
        image = image.point(lambda value: max(0, min(255, round(value * gain + 255 * bias))))
    return image.resize(size, Image.Resampling.LANCZOS) if image.size != size else image


def recolor(image, color):
    """A color map with its brightness, saturation and contrast changed, each a factor about one."""
    for name, enhancer in (("brightness", ImageEnhance.Brightness), ("saturation", ImageEnhance.Color), ("contrast", ImageEnhance.Contrast)):
        if name in color:
            image = enhancer(image).enhance(color[name])
    return image


def resized(image, size, method):
    """
    An image resized a channel at a time, since Pillow resizes RGBA through premultiplied alpha,
    which turns a transparent texel's color black and the surface's channels, whose alpha is the
    translucency, to nothing where it is.
    """
    return Image.merge(image.mode, [band.resize(size, method) for band in image.split()])


def placed(place, size):
    x, y, w, h = place
    return (round(x * size[0]), round(y * size[1]), round((x + w) * size[0]), round((y + h) * size[1]))


def fitted(box, size):
    """A box shrunk to a size's proportions, centered in it."""
    left, top, right, bottom = box
    scale = min((right - left) / size[0], (bottom - top) / size[1])
    w, h = max(1, round(size[0] * scale)), max(1, round(size[1] * scale))
    x, y = left + (right - left - w) // 2, top + (bottom - top - h) // 2
    return (x, y, x + w, y + h)


def size_of(box):
    return (box[2] - box[0], box[3] - box[1])


def sample_maps():
    """An entry from no source, a ringed bark of 64 by 64 with a hole cut in it and its depth from its normal, for the tests."""
    albedo = Image.new("RGBA", (64, 64))
    normal = Image.new("RGB", (32, 32))
    surface = Image.new("RGBA", (32, 32))
    for y in range(64):
        for x in range(64):
            ring = int(128 + 100 * math.sin(x * 0.4) * math.cos(y * 0.2))
            hole = (x - 40) ** 2 + (y - 24) ** 2 < 100
            albedo.putpixel((x, y), (ring, 90, 255 - ring, 0 if hole else 255))
    for y in range(32):
        for x in range(32):
            normal.putpixel((x, y), (int(128 + 60 * math.cos(x * 0.8)), int(128 + 60 * math.sin(y * 0.4)), 255))
            surface.putpixel((x, y), (255, 200, 0, 8 * x))
    return {"albedo": albedo, "normal": normal, "surface": surface, "depth": ImageChops.invert(height_of(normal)).filter(ImageFilter.GaussianBlur(1))}


def mean_color(albedo):
    """
    The mean of the albedo's opaque texels as sRGB from zero to one, the leaves' summer color a
    fall tint is worked out against.
    """
    alpha = albedo.getchannel("A").point(lambda value: 255 if value > 127 else 0)
    color = albedo.convert("RGB")
    count = alpha.histogram()[255]
    if count == 0:
        return [0.0, 0.0, 0.0]
    sums = [sum(value * n for value, n in enumerate(Image.composite(band, Image.new("L", band.size), alpha).histogram())) for band in color.split()]
    return [round(total / count / 255, 4) for total in sums]


def mean_surface(maps):
    """
    The mean of the surface's occlusion, roughness, metallic and translucency over the albedo's
    opaque texels, from zero to one as the material reads them, which a picture of a whole branch,
    as a tree's far card is, takes for the leaves it holds.
    """
    surface = maps["surface"]
    alpha = maps["albedo"].getchannel("A").resize(surface.size, Image.Resampling.BOX).point(lambda value: 255 if value > 127 else 0)
    count = alpha.histogram()[255]
    if count == 0:
        return [1.0, 1.0, 0.0, 0.0]
    sums = [sum(value * n for value, n in enumerate(Image.composite(band, Image.new("L", band.size), alpha).histogram())) for band in surface.split()]
    return [round(total / count / 255, 4) for total in sums]


def leaf_boxes(albedo):
    """
    Where each leaf of an atlas is, as the share of its width and height from its left and top
    and the share it spans, the opaque regions of its opacity read texel by texel, so a frond's thin
    stalk holds it together, a region under a tenth the largest's area left out as a scrap of a stem
    or a cut, in rows from the top and each row from the left, so a card shows one leaf.
    """
    small = albedo.getchannel("A")
    width, height = small.size
    solid = [value > 127 for value in small.tobytes()]
    seen = [False] * len(solid)
    boxes = []
    for start in range(len(solid)):
        if not solid[start] or seen[start]:
            continue
        stack = [start]
        seen[start] = True
        left, top, right, bottom, area = width, height, 0, 0, 0
        while stack:
            at = stack.pop()
            x, y = at % width, at // width
            left, right, top, bottom = min(left, x), max(right, x), min(top, y), max(bottom, y)
            area += 1
            for near in (at - 1 if x > 0 else -1, at + 1 if x < width - 1 else -1, at - width, at + width):
                if 0 <= near < len(solid) and solid[near] and not seen[near]:
                    seen[near] = True
                    stack.append(near)
        boxes.append((area, left, top, right + 1, bottom + 1))

    if not boxes:
        return []
    largest = max(box[0] for box in boxes)
    kept = sorted((box for box in boxes if box[0] * 10 >= largest), key=lambda box: box[2] + box[4])

    # A row is the leaves whose middles stand within half a typical leaf's height of the first's.
    tall = sorted(box[4] - box[2] for box in kept)[len(kept) // 2]
    rows = []
    for box in kept:
        if rows and (box[2] + box[4]) - (rows[-1][0][2] + rows[-1][0][4]) <= tall:
            rows[-1].append(box)
        else:
            rows.append([box])
    ordered = [box for row in rows for box in sorted(row, key=lambda box: box[1])]
    return [[round(left / width, 4), round(top / height, 4), round((right - left) / width, 4), round((bottom - top) / height, 4)]
            for _, left, top, right, bottom in ordered]


# The levels.

def write_entry(maps, folder, kind, boxes=None):
    """
    Writes an entry's files into a folder, and says each one's size and its encoding's error, a
    leaf atlas's levels keeping its leaves by the boxes leaf_boxes() finds.
    """
    os.makedirs(folder, exist_ok=True)
    report = {}
    for name, image in maps.items():
        levels = mip_chain(image, boxes if name == "albedo" and kind == "leaves" else None)
        format, blocks, error = encode(name, levels)
        data = ktx2(format, levels, blocks)
        path = os.path.join(folder, f"{name}.ktx2")
        with open(path + ".part", "wb") as file:
            file.write(data)
        os.replace(path + ".part", path)
        report[f"{name}.ktx2"] = (len(data), error)
    return report


def mip_chain(image, boxes=None):
    """
    The image and each level under it, half the size of the one above, down to a texel, a leaf
    atlas's opacity scaled at each level to keep the share of its leaves' boxes past one half.
    """
    if image.mode == "RGBA":
        image = bleed(image)
    levels = [image]
    rects = [placed(box, image.size) for box in boxes] if boxes else None
    share = sampled_share(image.getchannel("A"), rects, image.size) if rects else None
    while levels[-1].size != (1, 1):
        above = levels[-1]
        size = (max(above.width // 2, 1), max(above.height // 2, 1))
        level = resized(above, size, Image.Resampling.BOX)
        if share is not None:
            alpha = level.getchannel("A")
            scale = sampled_scale(alpha, rects, image.size, share)
            level.putalpha(alpha.point(lambda value: min(255, round(value * scale))))
        levels.append(level)
    return levels


def bleed(image):
    """
    An image whose transparent texels take the color of the opaque ones nearest them, the
    opaque texels' colors averaged at each size from a texel up to the whole image and the
    finest average that reaches a texel taken for it.
    """
    alpha = image.getchannel("A")
    if alpha.getextrema()[0] == 255:
        return image
    color = image.convert("RGB")
    mask = alpha.point(lambda value: 255 if value > 0 else 0)
    premultiplied = Image.composite(color, Image.new("RGB", color.size), mask)

    sizes = []
    size = color.size
    while size != (1, 1):
        size = (max(size[0] // 2, 1), max(size[1] // 2, 1))
        sizes.append(size)

    filled = color
    for size in reversed(sizes):
        weight = mask.resize(size, Image.Resampling.BOX).resize(color.size, Image.Resampling.BILINEAR)
        summed = premultiplied.resize(size, Image.Resampling.BOX).resize(color.size, Image.Resampling.BILINEAR)
        average = Image.merge("RGB", [
            ImageMath.lambda_eval(lambda image: image["c"] * 255.0 / (image["w"] + 0.001), c=channel.convert("F"), w=weight.convert("F")).convert("L")
            for channel in summed.split()])
        reached = weight.point(lambda value: 255 if value > 0 else 0)
        filled = Image.composite(average, filled, reached)

    result = Image.composite(color, filled, mask)
    result.putalpha(alpha)
    return result


def sampled_share(alpha, rects, size):
    """
    The leaves' mean share of their boxes past one half, a level's opacity read between its texels
    at the top level's size, as a card drawing the box samples it.
    """
    between = alpha.resize(size, Image.Resampling.BILINEAR)
    shares = []
    for rect in rects:
        histogram = between.crop(rect).histogram()
        shares.append(sum(histogram[128:]) / max(1, sum(histogram)))
    return sum(shares) / len(shares)


def sampled_scale(alpha, rects, size, share):
    """The scale of a level's opacity that keeps the leaves' sampled share nearest the top level's."""
    low, high = 0.25, 16.0
    for _ in range(20):
        middle = (low + high) / 2
        if sampled_share(alpha.point(lambda value: min(255, round(value * middle))), rects, size) < share:
            low = middle
        else:
            high = middle
    return high


# The encoding.

def encode(name, levels):
    """
    The format of a map and its levels' blocks, with the encoding's error in decibels at the top
    level. The one place a map's format is chosen, so another, Basis Universal's, is a branch here.
    """
    if name == "normal":
        blocks = [bc5(level) for level in levels]
        return BC5_UNORM, blocks, psnr_bc5(levels[0], blocks[0])
    if name == "depth":
        blocks = [bc4(level) for level in levels]
        return BC4_UNORM, blocks, psnr_bc4(levels[0], blocks[0])
    blocks = [bc7(level) for level in levels]
    return BC7_SRGB if name == "albedo" else BC7_UNORM, blocks, psnr_bc7(levels[0], blocks[0])


def texel_blocks(image, channels):
    """The image's 4 by 4 blocks, each its sixteen texels of the channels, a level smaller than a block repeating its edge."""
    width, height = image.size
    data = image.tobytes()
    stride = len(image.getbands())
    for by in range(0, max(height, 4), 4):
        for bx in range(0, max(width, 4), 4):
            block = []
            for y in range(by, by + 4):
                row = min(y, height - 1) * width
                for x in range(bx, bx + 4):
                    at = (row + min(x, width - 1)) * stride
                    block.append(tuple(data[at:at + channels]))
            yield block


def bc7(image):
    """The image in BC7, every block in mode 6, its RGBA endpoints with a bit each shared by their channels."""
    out = bytearray()
    for block in texel_blocks(image.convert("RGBA"), 4):
        out += bc7_block(block)
    return bytes(out)


def bc7_block(texels):
    # The endpoints along the principal axis of the block's colors, by a few rounds of the power
    # method on their covariance.
    count = 16.0
    mean = [sum(texel[c] for texel in texels) / count for c in range(4)]
    covariance = [[0.0] * 4 for _ in range(4)]
    for texel in texels:
        d = [texel[c] - mean[c] for c in range(4)]
        for i in range(4):
            for j in range(i, 4):
                covariance[i][j] += d[i] * d[j]
    for i in range(4):
        for j in range(i):
            covariance[i][j] = covariance[j][i]
    axis = [1.0, 1.0, 1.0, 1.0]
    for _ in range(6):
        axis = [sum(covariance[i][j] * axis[j] for j in range(4)) for i in range(4)]
        largest = max(abs(value) for value in axis)
        if largest == 0.0:
            break
        axis = [value / largest for value in axis]
    length = sum(value * value for value in axis)
    if length == 0.0:
        low = high = mean
    else:
        places = [sum((texel[c] - mean[c]) * axis[c] for c in range(4)) / length for texel in texels]
        low = [mean[c] + axis[c] * min(places) for c in range(4)]
        high = [mean[c] + axis[c] * max(places) for c in range(4)]

    endpoints = quantize(low), quantize(high)
    indices = bc7_indices(texels, endpoints)

    # Fitted again by least squares to the indices found, kept where it lessens the error.
    refit = least_squares(texels, indices)
    if refit is not None:
        others = quantize(refit[0]), quantize(refit[1])
        other_indices = bc7_indices(texels, others)
        if bc7_error(texels, others, other_indices) < bc7_error(texels, endpoints, indices):
            endpoints, indices = others, other_indices

    (q0, p0), (q1, p1) = endpoints
    if indices[0] >= 8:
        q0, q1, p0, p1 = q1, q0, p1, p0
        indices = [15 - index for index in indices]

    bits = 1 << 6
    shift = 7
    for c in range(4):
        bits |= q0[c] << shift
        bits |= q1[c] << (shift + 7)
        shift += 14
    bits |= p0 << shift | p1 << (shift + 1)
    shift += 2
    bits |= indices[0] << shift
    shift += 3
    for index in indices[1:]:
        bits |= index << shift
        shift += 4
    return bits.to_bytes(16, "little")


def quantize(endpoint):
    """An endpoint's seven bits a channel and the bit they share, the pair nearest it."""
    best = None
    for bit in (0, 1):
        values = [max(0, min(127, int((max(0.0, min(255.0, c)) - bit) / 2 + 0.5))) for c in endpoint]
        error = sum(((value << 1 | bit) - c) ** 2 for value, c in zip(values, endpoint))
        if best is None or error < best[0]:
            best = (error, values, bit)
    return best[1], best[2]


def expand(endpoint):
    values, bit = endpoint
    return [value << 1 | bit for value in values]


def bc7_indices(texels, endpoints):
    c0, c1 = expand(endpoints[0]), expand(endpoints[1])
    d = [b - a for a, b in zip(c0, c1)]
    dd = sum(value * value for value in d)
    if dd == 0:
        return [0] * 16
    return [NEAREST[max(0, min(64, int(sum((texel[c] - c0[c]) * d[c] for c in range(4)) * 64 / dd + 0.5)))] for texel in texels]


def bc7_error(texels, endpoints, indices):
    c0, c1 = expand(endpoints[0]), expand(endpoints[1])
    error = 0
    for texel, index in zip(texels, indices):
        w = WEIGHTS[index]
        for c in range(4):
            error += (((64 - w) * c0[c] + w * c1[c] + 32 >> 6) - texel[c]) ** 2
    return error


def least_squares(texels, indices):
    """The endpoints that best give the texels at the weights the indices name, or nothing where they are all one."""
    a00 = a01 = a11 = 0.0
    b0 = [0.0] * 4
    b1 = [0.0] * 4
    for texel, index in zip(texels, indices):
        t = WEIGHTS[index] / 64.0
        s = 1.0 - t
        a00 += s * s
        a01 += s * t
        a11 += t * t
        for c in range(4):
            b0[c] += s * texel[c]
            b1[c] += t * texel[c]
    determinant = a00 * a11 - a01 * a01
    if abs(determinant) < 1e-9:
        return None
    return ([(a11 * b0[c] - a01 * b1[c]) / determinant for c in range(4)],
            [(a00 * b1[c] - a01 * b0[c]) / determinant for c in range(4)])


def bc5(image):
    """The image's red and green in BC5, each a BC4 block of its own."""
    out = bytearray()
    for block in texel_blocks(image.convert("RGB"), 2):
        out += bc4_block([texel[0] for texel in block])
        out += bc4_block([texel[1] for texel in block])
    return bytes(out)


def bc4(image):
    """The image's one channel in BC4."""
    out = bytearray()
    for block in texel_blocks(image.convert("L"), 1):
        out += bc4_block([texel[0] for texel in block])
    return bytes(out)


def psnr_bc4(image, blocks):
    total = 0
    count = 0
    for at, texels in enumerate(texel_blocks(image.convert("L"), 1)):
        block = blocks[8 * at:8 * at + 8]
        palette = bc4_palette(block)
        bits = int.from_bytes(block, "little") >> 16
        for i, texel in enumerate(texels):
            total += (palette[bits >> (3 * i) & 7] - texel[0]) ** 2
            count += 1
    return psnr(total, count)


def bc4_block(values):
    """One channel's block in BC4's eight-step mode, its highest value first."""
    high, low = max(values), min(values)
    bits = high | low << 8
    if high != low:
        for i, value in enumerate(values):
            step = int((value - low) * 7 / (high - low) + 0.5)
            index = 0 if step == 7 else 1 if step == 0 else 8 - step
            bits |= index << (16 + 3 * i)
    return bits.to_bytes(8, "little")


def bc4_palette(block):
    high, low = block[0], block[1]
    return [high, low] + [((8 - i) * high + (i - 1) * low + 3) // 7 for i in range(2, 8)]


def psnr(errors, count):
    return 99.0 if errors == 0 else 10 * math.log10(255 * 255 * count / errors)


def psnr_bc7(image, blocks):
    """The top level's error against its blocks, decoded as mode 6 decodes, in decibels."""
    total = 0
    count = 0
    for at, texels in enumerate(texel_blocks(image.convert("RGBA"), 4)):
        bits = int.from_bytes(blocks[16 * at:16 * at + 16], "little")
        shift = 7
        c0, c1 = [], []
        for _ in range(4):
            c0.append(bits >> shift & 127)
            c1.append(bits >> (shift + 7) & 127)
            shift += 14
        p0, p1 = bits >> shift & 1, bits >> (shift + 1) & 1
        shift += 2
        c0 = [value << 1 | p0 for value in c0]
        c1 = [value << 1 | p1 for value in c1]
        indices = [bits >> shift & 7]
        shift += 3
        for _ in range(15):
            indices.append(bits >> shift & 15)
            shift += 4
        for texel, index in zip(texels, indices):
            w = WEIGHTS[index]
            for c in range(4):
                total += (((64 - w) * c0[c] + w * c1[c] + 32 >> 6) - texel[c]) ** 2
            count += 4
    return psnr(total, count)


def psnr_bc5(image, blocks):
    total = 0
    count = 0
    for at, texels in enumerate(texel_blocks(image.convert("RGB"), 2)):
        for channel in range(2):
            block = blocks[16 * at + 8 * channel:16 * at + 8 * channel + 8]
            palette = bc4_palette(block)
            bits = int.from_bytes(block, "little") >> 16
            for i, texel in enumerate(texels):
                total += (palette[bits >> (3 * i) & 7] - texel[channel]) ** 2
                count += 1
    return psnr(total, count)


# The file.

def ktx2(format, levels, blocks):
    """A KTX2 file of the levels' blocks, each level compressed with zstd, the largest first in the index and last in the file."""
    width, height = levels[0].size
    compressed = [zstd.compress(level, level=ZSTD_LEVEL) for level in blocks]

    descriptor = data_format(format)
    pairs = [(b"KTXwriter", b"build/textures.py")]
    key_values = b""
    for key, value in pairs:
        entry = key + b"\0" + value + b"\0"
        key_values += struct.pack("<I", len(entry)) + entry
        key_values += b"\0" * (-len(key_values) % 4)

    header_size = 12 + 9 * 4 + 4 * 4 + 2 * 8
    index_size = 24 * len(levels)
    descriptor_at = header_size + index_size
    key_values_at = descriptor_at + len(descriptor)
    data_at = key_values_at + len(key_values)

    # The levels' bytes from the smallest to the largest, as the format lays them out.
    offsets = [0] * len(levels)
    at = data_at
    for level in reversed(range(len(levels))):
        offsets[level] = at
        at += len(compressed[level])

    out = bytearray(b"\xabKTX 20\xbb\r\n\x1a\n")
    out += struct.pack("<9I", format, 1, width, height, 0, 0, 1, len(levels), 2)
    out += struct.pack("<4I", descriptor_at, len(descriptor), key_values_at, len(key_values))
    out += struct.pack("<2Q", 0, 0)
    for level in range(len(levels)):
        out += struct.pack("<3Q", offsets[level], len(compressed[level]), len(blocks[level]))
    out += descriptor
    out += key_values
    for level in reversed(range(len(levels))):
        out += compressed[level]
    return bytes(out)


def data_format(format):
    """The data format descriptor of a block format, its total size first and its one basic block."""
    if format == BC4_UNORM:
        model, transfer = MODEL_BC4, TRANSFER_LINEAR
        samples = [(0, 63, 0)]
    elif format == BC5_UNORM:
        model, transfer = MODEL_BC5, TRANSFER_LINEAR
        samples = [(0, 63, 0), (64, 63, 1)]
    else:
        model, transfer = MODEL_BC7, TRANSFER_SRGB if format == BC7_SRGB else TRANSFER_LINEAR
        samples = [(0, 127, 0)]
    block_size = 24 + 16 * len(samples)
    block = struct.pack("<II", 0, 2 | block_size << 16)
    block += struct.pack("<4B", model, 1, transfer, 0)
    block += struct.pack("<4B", 3, 3, 0, 0)
    block += struct.pack("<8B", 8 if format == BC4_UNORM else 16, 0, 0, 0, 0, 0, 0, 0)
    for offset, length, channel in samples:
        block += struct.pack("<HBB4BII", offset, length, channel, 0, 0, 0, 0, 0, 0xFFFFFFFF)
    return struct.pack("<I", 4 + len(block)) + block


if __name__ == "__main__":
    sys.exit(main())
