#!/usr/bin/env python3
"""Makes a scene pack from a glTF scene as its makers publish it, for ScenePacks to fetch.

    build/make-scene-pack.py <scene.gltf> <out.pack> [--size 1024] [--name sponza]
        [--extra FILE ...] [--manifest scenes/NAME.json --url URL --title T --source S
         --license L --attribution A]

Every texture the scene's materials use is resized to at most --size on a side, a power of two,
mipmapped down to a texel, and written as KTX2 with the blocks a GPU reads without unpacking: BC7
for a color, base or emitted, which keeps its alpha; BC5 for a normal map, its two channels, Bevy
working out the third; and BC1 for the rest, metalness, roughness and occlusion. Each level is
supercompressed with zstd, which Bevy's KTX2 loader undoes, so the pack is small to fetch and the
GPU is handed blocks. The glTF is written again pointing at the KTX2 files and its buffer, its
cameras taken out, since a camera in a scene a game spawns would draw the game twice. The meshes are
kept as they are. --extra copies a file such as the scene's license into the pack beside the model,
and --no-lights leaves out the scene's punctual lights.

The folder is written as one pack by `bcs scenes pack`, and the pack's size and SHA-256 printed,
or written into a manifest with --manifest, whose address is where the pack will be published.
The same input makes the same pack, byte for byte, so a manifest written here holds the hash of the
file the owner publishes.

The encoders are written here in Python, standard library and Pillow alone, since no encoder is a
dependency of the repository, and each texture is encoded in a process of its own. They are simple:
BC1 and BC4 take the ends of a block along its principal axis and the nearest step for each texel,
and BC7 uses its mode 6 alone, one pair of RGBA endpoints a block with a bit each shared by their
channels, fitted the same way and refined once by least squares. That is a little below what a
dedicated encoder reaches, and plenty for a test scene seen at 1K.
"""
import argparse
import hashlib
import json
import math
import multiprocessing
import os
import shutil
import struct
import subprocess
import sys
import tempfile
from compression import zstd

from PIL import Image

# -- The blocks

# BC7's weights for a four-bit index, out of 64.
WEIGHTS4 = (0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64)


def principal(points, channels):
    """The mean of a block's texels and the direction they spread most along, by a few rounds of
    the power method on their covariance, which settles quickly for sixteen points."""
    n = len(points)
    mean = [sum(p[c] for p in points) / n for c in range(channels)]
    cov = [[0.0] * channels for _ in range(channels)]
    for p in points:
        d = [p[c] - mean[c] for c in range(channels)]
        for i in range(channels):
            di = d[i]
            row = cov[i]
            for j in range(i, channels):
                row[j] += di * d[j]
    for i in range(channels):
        for j in range(i):
            cov[i][j] = cov[j][i]
    # Started from the channel that varies most, so a block grading in one channel finds it.
    axis = [0.0] * channels
    axis[max(range(channels), key=lambda c: cov[c][c])] = 1.0
    for _ in range(6):
        nxt = [sum(cov[i][j] * axis[j] for j in range(channels)) for i in range(channels)]
        length = math.sqrt(sum(v * v for v in nxt))
        if length < 1e-9:
            break
        axis = [v / length for v in nxt]
    return mean, axis


def ends(points, channels):
    """The two ends of a block along its principal axis, as points clamped to the texel range."""
    mean, axis = principal(points, channels)
    ts = [sum((p[c] - mean[c]) * axis[c] for c in range(channels)) for p in points]
    lo, hi = min(ts), max(ts)
    a = [min(255.0, max(0.0, mean[c] + axis[c] * lo)) for c in range(channels)]
    b = [min(255.0, max(0.0, mean[c] + axis[c] * hi)) for c in range(channels)]
    return a, b


def bc1(block):
    """Eight bytes of BC1 for sixteen RGB texels, in its four-color mode."""
    a, b = ends(block, 3)

    def pack565(c):
        return (int(c[0] * 31 / 255 + 0.5) << 11) | (int(c[1] * 63 / 255 + 0.5) << 5) | int(c[2] * 31 / 255 + 0.5)

    def unpack565(v):
        r, g, bl = (v >> 11) & 31, (v >> 5) & 63, v & 31
        return ((r << 3) | (r >> 2), (g << 2) | (g >> 4), (bl << 3) | (bl >> 2))

    c0, c1 = pack565(b), pack565(a)
    if c0 < c1:
        c0, c1 = c1, c0
    if c0 == c1:
        # One color, which index 0 is in either mode.
        return struct.pack('<HHI', c0, c1, 0)
    e0, e1 = unpack565(c0), unpack565(c1)
    palette = (e0, e1,
               tuple((2 * e0[c] + e1[c]) // 3 for c in range(3)),
               tuple((e0[c] + 2 * e1[c]) // 3 for c in range(3)))
    bits = 0
    for i, p in enumerate(block):
        best, index = 1 << 30, 0
        for k, q in enumerate(palette):
            d = (p[0] - q[0]) ** 2 + (p[1] - q[1]) ** 2 + (p[2] - q[2]) ** 2
            if d < best:
                best, index = d, k
        bits |= index << (2 * i)
    return struct.pack('<HHI', c0, c1, bits)


def bc4(values):
    """Eight bytes of BC4 for sixteen values of one channel, in its eight-step mode."""
    e0, e1 = max(values), min(values)
    if e0 == e1:
        return bytes((e0, e1)) + bytes(6)
    span = e0 - e1
    bits = 0
    for i, v in enumerate(values):
        # Steps counted from e0, 0, to e1, 7, which are indices 0 and 1, the six between 2 to 7.
        k = int((e0 - v) * 7 / span + 0.5)
        index = 0 if k == 0 else 1 if k == 7 else k + 1
        bits |= index << (3 * i)
    return bytes((e0, e1)) + bits.to_bytes(6, 'little')


def bc5(block):
    """Sixteen bytes of BC5, a BC4 block for red and one for green."""
    return bc4([p[0] for p in block]) + bc4([p[1] for p in block])


def quantize7(endpoint):
    """An RGBA endpoint as seven bits a channel and the shared bit that suits it best."""
    best = None
    for pbit in (0, 1):
        q = [min(127, max(0, int((v - pbit) / 2 + 0.5))) for v in endpoint]
        error = sum((endpoint[c] - ((q[c] << 1) | pbit)) ** 2 for c in range(4))
        if best is None or error < best[0]:
            best = (error, q, pbit)
    return best[1], best[2]


def bc7_fit(block, a, b):
    """The quantized ends, shared bits and four-bit indices of a mode 6 block between two ends."""
    qa, pa = quantize7(a)
    qb, pb = quantize7(b)
    e0 = [(v << 1) | pa for v in qa]
    e1 = [(v << 1) | pb for v in qb]
    palette = [tuple(((64 - w) * e0[c] + w * e1[c] + 32) >> 6 for c in range(4)) for w in WEIGHTS4]
    line = [e1[c] - e0[c] for c in range(4)]
    length2 = sum(v * v for v in line) or 1
    indices = []
    error = 0
    for p in block:
        t = sum((p[c] - e0[c]) * line[c] for c in range(4)) / length2
        guess = min(15, max(0, int(t * 15 + 0.5)))
        best, index = 1 << 30, guess
        for k in (guess - 1, guess, guess + 1):
            if 0 <= k <= 15:
                q = palette[k]
                d = (p[0] - q[0]) ** 2 + (p[1] - q[1]) ** 2 + (p[2] - q[2]) ** 2 + (p[3] - q[3]) ** 2
                if d < best:
                    best, index = d, k
        indices.append(index)
        error += best
    return qa, pa, qb, pb, indices, error


def bc7(block):
    """Sixteen bytes of BC7 mode 6 for sixteen RGBA texels."""
    a, b = ends(block, 4)
    fit = bc7_fit(block, a, b)

    # Once by least squares, the ends that best explain the texels at the indices chosen.
    qa, pa, qb, pb, indices, error = fit
    ws = [WEIGHTS4[i] / 64 for i in indices]
    s00 = sum((1 - w) * (1 - w) for w in ws)
    s01 = sum((1 - w) * w for w in ws)
    s11 = sum(w * w for w in ws)
    det = s00 * s11 - s01 * s01
    if abs(det) > 1e-9:
        na, nb = [], []
        for c in range(4):
            r0 = sum((1 - w) * p[c] for w, p in zip(ws, block))
            r1 = sum(w * p[c] for w, p in zip(ws, block))
            na.append(min(255.0, max(0.0, (s11 * r0 - s01 * r1) / det)))
            nb.append(min(255.0, max(0.0, (s00 * r1 - s01 * r0) / det)))
        refined = bc7_fit(block, na, nb)
        if refined[5] < error:
            fit = refined
    qa, pa, qb, pb, indices, _ = fit

    # The first texel's index has its top bit left out, so it must be below eight; where it is not,
    # the ends swap and every index turns over.
    if indices[0] >= 8:
        qa, pa, qb, pb = qb, pb, qa, pa
        indices = [15 - i for i in indices]

    bits = 1 << 6
    shift = 7
    for c in range(4):
        bits |= qa[c] << shift
        bits |= qb[c] << (shift + 7)
        shift += 14
    bits |= pa << shift
    bits |= pb << (shift + 1)
    shift += 2
    for i, index in enumerate(indices):
        width = 3 if i == 0 else 4
        bits |= index << shift
        shift += width
    return bits.to_bytes(16, 'little')


def blocks(image, encode, channels):
    """The blocks of a picture, row after row, the edges of one smaller than a block repeated."""
    width, height = image.size
    data = image.tobytes()
    stride = len(image.getbands())
    out = bytearray()
    for by in range(0, max(height, 4), 4):
        for bx in range(0, max(width, 4), 4):
            block = []
            for y in range(4):
                row = min(by + y, height - 1) * width
                for x in range(4):
                    at = (row + min(bx + x, width - 1)) * stride
                    block.append(tuple(data[at:at + channels]))
            out += encode(block)
    return bytes(out)


# -- KTX2

KTX2_IDENTIFIER = bytes((0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32, 0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A))

# vkFormat, the Khronos data format's color model, the bytes a block and the samples a block holds,
# as (channel, bit offset, bit length).
FORMATS = {
    ('bc7', True): (146, 134, 16, [(0, 0, 128)]),
    ('bc7', False): (145, 134, 16, [(0, 0, 128)]),
    ('bc5', False): (141, 132, 16, [(0, 0, 64), (1, 64, 64)]),
    ('bc1', False): (131, 128, 8, [(0, 0, 64)]),
    ('bc1', True): (132, 128, 8, [(0, 0, 64)]),
}


def dfd(kind, srgb):
    """The data format descriptor KTX2 requires, a basic block for one block-compressed format."""
    _, model, block_bytes, samples = FORMATS[(kind, srgb)]
    size = 24 + 16 * len(samples)
    body = struct.pack('<IHH', 0, 2, size)
    body += struct.pack('<BBBB', model, 1, 2 if srgb else 1, 0)
    body += bytes((3, 3, 0, 0))
    body += bytes((block_bytes, 0, 0, 0, 0, 0, 0, 0))
    for channel, offset, length in samples:
        body += struct.pack('<HBB', offset, length - 1, channel)
        body += bytes(4)
        body += struct.pack('<II', 0, 0xFFFFFFFF)
    return struct.pack('<I', 4 + len(body)) + body


def ktx2(levels, width, height, kind, srgb):
    """A KTX2 file of blocks, the levels largest first, each supercompressed with zstd."""
    vk_format = FORMATS[(kind, srgb)][0]
    count = len(levels)
    descriptor = dfd(kind, srgb)
    header = KTX2_IDENTIFIER + struct.pack('<9I', vk_format, 1, width, height, 0, 0, 1, count, 2)
    index_size = 4 * 4 + 8 * 2
    level_index_size = 24 * count
    dfd_offset = len(header) + index_size + level_index_size
    data_start = dfd_offset + len(descriptor)

    # Stored smallest first, as the format asks, at the alignment of one a supercompressed file has.
    compressed = [zstd.compress(level, level=19) for level in levels]
    offsets = {}
    at = data_start
    for i in reversed(range(count)):
        offsets[i] = at
        at += len(compressed[i])

    out = bytearray(header)
    out += struct.pack('<IIIIQQ', dfd_offset, len(descriptor), 0, 0, 0, 0)
    for i in range(count):
        out += struct.pack('<QQQ', offsets[i], len(compressed[i]), len(levels[i]))
    out += descriptor
    for i in reversed(range(count)):
        out += compressed[i]
    return bytes(out)


# -- Textures

def power_below(n):
    return 1 << (max(1, n).bit_length() - 1)


def convert(job):
    """Writes one texture as KTX2, in a process of its own."""
    source, target, kind, srgb, size = job
    image = Image.open(source)
    mode, channels, encode = {'bc7': ('RGBA', 4, bc7), 'bc5': ('RGB', 3, bc5), 'bc1': ('RGB', 3, bc1)}[kind]
    image = image.convert(mode)
    width = min(power_below(image.width), size)
    height = min(power_below(image.height), size)
    if (width, height) != image.size:
        image = image.resize((width, height), Image.Resampling.LANCZOS)

    levels = []
    level = image
    while True:
        levels.append(blocks(level, encode, channels))
        if level.width == 1 and level.height == 1:
            break
        level = level.resize((max(1, level.width // 2), max(1, level.height // 2)), Image.Resampling.BOX)

    with open(target, 'wb') as file:
        file.write(ktx2(levels, width, height, kind, srgb))
    return target


def kinds(gltf):
    """Each image's encoding by what the materials use it for, a normal map before a color before
    data, where one image serves two."""
    found = {}
    rank = {'bc5': 0, 'bc7': 1, 'bc1': 2}

    def use(texture, kind, srgb):
        if texture is None:
            return
        image = gltf['textures'][texture['index']].get('source')
        if image is None:
            return
        held = found.get(image)
        if held is None or rank[kind] < rank[held[0]]:
            found[image] = (kind, srgb)

    for material in gltf.get('materials', []):
        pbr = material.get('pbrMetallicRoughness', {})
        use(pbr.get('baseColorTexture'), 'bc7', True)
        use(material.get('emissiveTexture'), 'bc7', True)
        use(material.get('normalTexture'), 'bc5', False)
        use(pbr.get('metallicRoughnessTexture'), 'bc1', False)
        use(material.get('occlusionTexture'), 'bc1', False)
    return found


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('gltf')
    parser.add_argument('pack')
    parser.add_argument('--size', type=int, default=1024)
    parser.add_argument('--name', help='what the model is called in the pack, the glTF file\'s name by default')
    parser.add_argument('--extra', nargs='*', default=[])
    parser.add_argument('--manifest')
    parser.add_argument('--url')
    parser.add_argument('--title', default='')
    parser.add_argument('--source', default='')
    parser.add_argument('--license', default='')
    parser.add_argument('--attribution', default='')
    parser.add_argument('--keep', help='a folder to leave the pack\'s files in, to look at')
    parser.add_argument('--no-lights', action='store_true', help='leave out the scene\'s punctual lights')
    args = parser.parse_args()

    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    source_folder = os.path.dirname(os.path.abspath(args.gltf))
    name = args.name or os.path.splitext(os.path.basename(args.gltf))[0]
    with open(args.gltf, encoding='utf-8') as file:
        gltf = json.load(file)

    work = args.keep or tempfile.mkdtemp(prefix='scene-pack-')
    if os.path.exists(work) and args.keep:
        shutil.rmtree(work)
    os.makedirs(os.path.join(work, 'textures'))

    # The textures, each a job, as many at once as there are cores.
    used = kinds(gltf)
    jobs = []
    for index, image in enumerate(gltf.get('images', [])):
        if index not in used or 'uri' not in image:
            continue
        kind, srgb = used[index]
        stem = os.path.splitext(os.path.basename(image['uri']))[0]
        target = os.path.join('textures', stem + '.ktx2')
        jobs.append((os.path.join(source_folder, image['uri']), os.path.join(work, target), kind, srgb, args.size))
        image['uri'] = target.replace(os.sep, '/')
        image.pop('mimeType', None)

    with multiprocessing.Pool() as pool:
        for done, written in enumerate(pool.imap_unordered(convert, jobs), 1):
            print(f'\r{done}/{len(jobs)} textures', end='', file=sys.stderr, flush=True)
    print(file=sys.stderr)

    # The buffer under the model's name, and the cameras out.
    for buffer in gltf.get('buffers', []):
        if 'uri' in buffer and not buffer['uri'].startswith('data:'):
            shutil.copyfile(os.path.join(source_folder, buffer['uri']), os.path.join(work, name + '.bin'))
            buffer['uri'] = name + '.bin'
    gltf.pop('cameras', None)
    for node in gltf.get('nodes', []):
        node.pop('camera', None)

    # Lights out where asked, as Sponza's are, every one of them at an intensity of nothing.
    if args.no_lights:
        for holder in [gltf] + gltf.get('nodes', []):
            extensions = holder.get('extensions', {})
            extensions.pop('KHR_lights_punctual', None)
            if not extensions:
                holder.pop('extensions', None)
        used_extensions = [e for e in gltf.get('extensionsUsed', []) if e != 'KHR_lights_punctual']
        if used_extensions:
            gltf['extensionsUsed'] = used_extensions
        else:
            gltf.pop('extensionsUsed', None)

    with open(os.path.join(work, name + '.gltf'), 'w', encoding='utf-8') as file:
        json.dump(gltf, file, separators=(',', ':'), sort_keys=True)
    for extra in args.extra:
        shutil.copyfile(extra, os.path.join(work, os.path.basename(extra)))

    bcs = os.path.join(root, 'bcs')
    subprocess.run([bcs, 'scenes', 'pack', work, os.path.abspath(args.pack)], check=True)
    if not args.keep:
        shutil.rmtree(work)

    with open(args.pack, 'rb') as file:
        digest = hashlib.sha256(file.read()).hexdigest()
    size = os.path.getsize(args.pack)
    print(f'{args.pack}: {size} bytes, SHA-256 {digest}, model {name}.gltf')

    if args.manifest:
        manifest = {
            'title': args.title or name,
            'source': args.source,
            'license': args.license,
            'attribution': args.attribution,
            'url': args.url or os.path.abspath(args.pack),
            'size': size,
            'sha256': digest,
            'model': name + '.gltf',
        }
        with open(args.manifest, 'w', encoding='utf-8') as file:
            json.dump(manifest, file, indent=2)
            file.write('\n')
        print(f'wrote {args.manifest}')


if __name__ == '__main__':
    main()
