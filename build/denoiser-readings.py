#!/usr/bin/env python3
"""Takes the readings of the bridge's denoiser over ray-traced lighting that docs/ray-tracing.md
gives, from the feature test drawn offscreen, on a machine whose GPU traces rays.

    python3 build/denoiser-readings.py [--frames 256] [--keep DIR]

Each of the feature test's views, its zones as a teleport shows them and one into the Cornell box,
the room the rays light, is drawn with the denoiser off many times over standing still, and the
mean of those frames is the view's reference, the picture the grain averages out to. One frame
with the denoiser off and one with it on, after the view has stood a few seconds, are each read as
their root mean square distance from the reference, in levels of eight bits, the mean of eight such
frames apiece. Then the camera strafes past the Cornell box's tall block, a tenth of a meter a frame
for thirty frames, four times over, and each frame it stops on is read against the reference of
where it stopped over the pixels the block hid in the strafe's last frames and has left, where a
denoiser that takes a history from the wrong surface leaves a trail. Those pixels are found by
casting each one's ray into the room from where the camera stopped and cutting the segment from
there to each earlier eye by the block.

The feature test runs on a set clock held between commands, so a view stands still while it is read,
as many frames drawn as the machine draws in that time, and `glide` moves the camera the same
distance in each frame of the strafe. Its settings are kept in a folder of their own for the run,
ray-traced lighting on, the weather, bloom and the anti-aliasing off, which would filter the grain
as well, and the sun stopped, and its HUD, whose count of frames changes in every frame, is hidden.
A bridge built with --solari, a feature test built on it, and ImageMagick are needed, and a session
serving before it starts is refused rather than stopped. --keep keeps the frames.
"""

import argparse
import json
import math
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BCS = os.path.join(ROOT, "bcs")
SESSION = "BevyCSharp.FeatureTest"

# The picture the feature test draws offscreen.
WIDTH, HEIGHT = 1280, 720

# The views, each the words of a command that puts the camera there.
VIEWS = [
    ("hub", ["teleport", "hub"]),
    ("course", ["teleport", "course"]),
    ("render gallery", ["teleport", "render gallery"]),
    ("lights", ["teleport", "lights"]),
    ("vegetation", ["teleport", "vegetation"]),
    ("scenes", ["teleport", "scenes"]),
    ("Cornell box", ["look", "-64", "1", "-12.5", "-73", "1", "-12.5"]),
]

# The strafe, from where it starts, facing west into the Cornell box, toward the north, to the
# camera's right, a tenth of a meter a frame, six meters a second on a clock of sixty frames a
# second, as fast as the free camera flies.
STRAFE_FROM = (-64.0, 1.0, -11.0)
STRAFE_FRAMES = 30
STRAFE_STEP = 0.1
STRAFE_TIMES = 4

# The Cornell box's tall block, as games/FeatureTest/Behaviors/Gallery.cs stands it, its middle, its
# half sizes and its turn about the vertical.
BLOCK_MIDDLE = (-71.4, -1.18 + 1.5, -13.8)
BLOCK_HALF = (0.7, 1.5, 0.7)
BLOCK_TURN = 0.35

# How many frames of each kind are read against a reference, and how long a view stands before.
SINGLES = 8
SETTLE = 3.0

# The feature test's camera's upright field of view, as games/FeatureTest/Behaviors/Scene.cs gives
# it.
FOV = math.radians(55.0)

# Whether the frames a reference is the mean of are kept, which --keep asks for.
KEEP = False


def bcs(*arguments, check=True):
    """Runs a bcs verb and gives what it printed, which is where it says why it failed."""
    done = subprocess.run([BCS, *arguments], cwd=ROOT, capture_output=True, text=True)
    if check and done.returncode != 0:
        sys.exit(f"bcs {' '.join(arguments)} failed: {done.stdout.strip()} {done.stderr.strip()}")
    return done.stdout.strip()


def command(*words):
    return bcs("command", *words)


def shot(path, frame=None):
    bcs("shot", path, *([str(frame)] if frame is not None else []))
    if not os.path.exists(path):
        sys.exit(f"bcs shot wrote no {path}")


def magick(*arguments):
    done = subprocess.run(["magick", *arguments], capture_output=True, text=True)
    # compare answers 1 where the pictures differ, as they do here.
    if done.returncode not in (0, 1):
        sys.exit(f"magick {' '.join(arguments)} failed: {done.stderr.strip()}")
    return (done.stdout + done.stderr).strip()


def mean(paths, into, scratch):
    """The mean of the frames, sixteen at a time and then the means, in sixteen bits a channel. The
    frames go after, unless they are kept, since a few hundred of them are half a gigabyte."""
    groups = [paths[start:start + 16] for start in range(0, len(paths), 16)]
    means = []
    for index, group in enumerate(groups):
        means.append(os.path.join(scratch, f"group-{index:02}.png"))
        magick(*group, "-alpha", "off", "-evaluate-sequence", "mean", "-depth", "16", means[-1])
    magick(*means, "-evaluate-sequence", "mean", "-depth", "16", into)
    for path in means + ([] if KEEP else paths):
        os.remove(path)


def distance(picture, reference, mask=None):
    """The root mean square distance of a picture from its reference, in levels of eight bits,
    over the pixels a mask holds white, or over the whole picture."""
    squared = [picture, reference, "-alpha", "off", "-compose", "difference", "-composite", "-evaluate", "pow", "2"]
    if mask is None:
        return math.sqrt(float(magick(*squared, "-format", "%[fx:mean]", "info:"))) * 255.0
    held = float(magick(mask, "-format", "%[fx:mean]", "info:"))
    if held <= 0.0:
        sys.exit(f"{mask} holds no pixels")
    masked = float(magick(*squared, mask, "-compose", "multiply", "-composite", "-format", "%[fx:mean]", "info:"))
    return math.sqrt(masked / held) * 255.0


def clock():
    """The frame the set clock is at, held still between commands."""
    status = command("app.status")
    found = re.search(r"\((\d+) of the clock\)", status) or re.search(r"frame (\d+)", status)
    return int(found.group(1))


def frames(prefix, count, scratch):
    paths = []
    for index in range(count):
        paths.append(os.path.join(scratch, f"{prefix}-{index:03}.png"))
        shot(paths[-1])
    return paths


def standing(name, scratch, count):
    """Reads a view standing still, its reference and one frame of each kind against it."""
    command("feature.denoiser", "off")
    time.sleep(SETTLE)
    reference = os.path.join(scratch, f"{name}-reference.png")
    mean(frames(f"{name}-noisy-many", count, scratch), reference, scratch)
    noisy = [distance(path, reference) for path in frames(f"{name}-noisy", SINGLES, scratch)]
    command("feature.denoiser", "on")
    time.sleep(SETTLE)
    denoised = [distance(path, reference) for path in frames(f"{name}-denoised", SINGLES, scratch)]
    return reference, sum(noisy) / len(noisy), sum(denoised) / len(denoised)


# The inside of the Cornell box, each face as the axis it faces along, where it lies on that axis,
# and its bounds along the other two, as Gallery.cs builds the room, five meters a side around
# (-70.5, -1.18, -13), the floor's top at that height.
ROOM = [
    (0, -73.0, ((1, -1.18, 3.82), (2, -15.5, -10.5))),
    (2, -15.5, ((0, -73.0, -68.0), (1, -1.18, 3.82))),
    (2, -10.5, ((0, -73.0, -68.0), (1, -1.18, 3.82))),
    (1, -1.18, ((0, -73.0, -68.0), (2, -15.5, -10.5))),
    (1, 3.82, ((0, -73.0, -68.0), (2, -15.5, -10.5))),
]

# How far a pixel is from the picture's middle per unit of the way across it.
FOCAL = (HEIGHT / 2.0) / math.tan(FOV / 2.0)


def project(eye, point):
    """Where a point falls on the picture of a camera at eye facing west, level."""
    # Facing west, along -x, the picture's right is north, along -z, and its up is up.
    depth = eye[0] - point[0]
    return (WIDTH / 2.0 - FOCAL * (point[2] - eye[2]) / depth, HEIGHT / 2.0 - FOCAL * (point[1] - eye[1]) / depth)


def corners():
    """The tall block's eight corners, turned about the vertical as Quat.FromRotationY turns them."""
    cos, sin = math.cos(BLOCK_TURN), math.sin(BLOCK_TURN)
    for x in (-1, 1):
        for y in (-1, 1):
            for z in (-1, 1):
                local = (x * BLOCK_HALF[0], y * BLOCK_HALF[1], z * BLOCK_HALF[2])
                turned = (local[0] * cos + local[2] * sin, local[1], -local[0] * sin + local[2] * cos)
                yield tuple(BLOCK_MIDDLE[i] + turned[i] for i in range(3))


def surface(eye, px, py):
    """Where the ray through a pixel's middle first meets the inside of the Cornell box, or None
    where it leaves the box."""
    way = (-1.0, -(py + 0.5 - HEIGHT / 2.0) / FOCAL, -(px + 0.5 - WIDTH / 2.0) / FOCAL)
    nearest = None
    for axis, at, bounds in ROOM:
        t = (at - eye[axis]) / way[axis] if way[axis] != 0.0 else -1.0
        if t <= 0.0 or (nearest is not None and t >= nearest):
            continue
        if all(low - 1e-4 <= eye[other] + way[other] * t <= high + 1e-4 for other, low, high in bounds):
            nearest = t
    return None if nearest is None else tuple(eye[i] + way[i] * nearest for i in range(3))


def hidden(eye, point):
    """Whether the tall block stands between an eye and a point, the segment between them turned
    back into the block's own frame and cut by its sides."""
    cos, sin = math.cos(BLOCK_TURN), math.sin(BLOCK_TURN)

    def local(p):
        v = (p[0] - BLOCK_MIDDLE[0], p[1] - BLOCK_MIDDLE[1], p[2] - BLOCK_MIDDLE[2])
        return (v[0] * cos - v[2] * sin, v[1], v[0] * sin + v[2] * cos)

    a, b = local(eye), local(point)
    low, high = 0.0, 1.0 - 1e-4
    for i in range(3):
        d = b[i] - a[i]
        if abs(d) < 1e-12:
            if abs(a[i]) > BLOCK_HALF[i]:
                return False
            continue
        near, far = (-BLOCK_HALF[i] - a[i]) / d, (BLOCK_HALF[i] - a[i]) / d
        low, high = max(low, min(near, far)), min(high, max(near, far))
        if low > high:
            return False
    return True


def uncovered(paths):
    """Writes masks of the pixels whose surface, where the strafe stops, the tall block hid in one
    of the strafe's last frames, each path keyed by how many frames before the end it reaches back.

    A pixel is found by its ray into the room from where the camera stopped, and is in a mask where
    the block stood between that point and the camera in a frame before, and not in the last. The
    room behind the block moves across the picture as well, slower than the block, so the pixels
    the block left are a band beside it as wide as the two differ, not all it passed over."""
    eyes = [(STRAFE_FROM[0], STRAFE_FROM[1], STRAFE_FROM[2] - STRAFE_STEP * k) for k in range(STRAFE_FRAMES + 1)]
    final = eyes[-1]
    # Only where the block falls on the picture in some frame can a pixel's surface have been
    # behind it.
    fallen = [project(eye, corner) for eye in eyes for corner in corners()]
    left, right = max(0, int(min(x for x, _ in fallen))), min(WIDTH, int(max(x for x, _ in fallen)) + 1)
    top, bottom = max(0, int(min(y for _, y in fallen))), min(HEIGHT, int(max(y for _, y in fallen)) + 1)

    # For each pixel, how many frames before the end the block last hid its surface, or nought.
    ago = bytearray(WIDTH * HEIGHT)
    for py in range(top, bottom):
        for px in range(left, right):
            point = surface(final, px, py)
            if point is None or hidden(final, point):
                continue
            for k in range(STRAFE_FRAMES - 1, -1, -1):
                if hidden(eyes[k], point):
                    ago[py * WIDTH + px] = STRAFE_FRAMES - k
                    break

    for since, path in paths.items():
        with open(path, "wb") as mask:
            mask.write(f"P5 {WIDTH} {HEIGHT} 255\n".encode("ascii"))
            mask.write(bytes(255 if 0 < frames <= since else 0 for frames in ago))


def strafe(scratch, count):
    """Strafes past the tall block a few times and reads where each stops against its reference,
    over the pixels the block left. The rest of the picture is not read, since the clock moved
    while the camera did and the planet orbiting over the hub moved with it."""
    looking = [f"{value:g}" for value in (*STRAFE_FROM, -73.0, STRAFE_FROM[1], STRAFE_FROM[2])]
    stops = []
    for attempt in range(STRAFE_TIMES):
        command("look", *looking)
        command("feature.denoiser", "on")
        time.sleep(SETTLE)
        at = clock() + STRAFE_FRAMES
        # The glide is asked for first, since the clock moves while any command waits, and the shot
        # waits for the frame it stops on.
        gliding = subprocess.Popen([BCS, "command", "glide", "0", "0", f"{-STRAFE_STEP:g}", str(STRAFE_FRAMES)],
                                   cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        stops.append(os.path.join(scratch, f"strafe-{attempt}.png"))
        shot(stops[-1], at)
        said, _ = gliding.communicate()
        if gliding.returncode != 0:
            sys.exit(f"glide failed: {said.strip()}")

    where = re.search(r"^Transform\.Translation = (.*)$", command("entity.get", "Scene camera"), re.MULTILINE).group(1)
    command("feature.denoiser", "off")
    time.sleep(SETTLE)
    reference = os.path.join(scratch, "strafe-reference.png")
    mean(frames("strafe-noisy-many", count, scratch), reference, scratch)
    noisy = frames("strafe-noisy", SINGLES, scratch)
    command("feature.denoiser", "on")

    masks = {since: os.path.join(scratch, f"strafe-uncovered-{since}.pgm") for since in (STRAFE_FRAMES, 8)}
    uncovered(masks)

    def read(paths, mask):
        return sum(distance(path, reference, mask) for path in paths) / len(paths)

    return where, {f"uncovered in the last {since} frames": (read(noisy, mask), read(stops, mask))
                   for since, mask in masks.items()}


def main(arguments):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--frames", type=int, default=256, help="the frames a reference is the mean of")
    parser.add_argument("--keep", help="a folder to keep the frames and the references in")
    options = parser.parse_args(arguments)
    global KEEP
    KEEP = options.keep is not None

    if shutil.which("magick") is None:
        sys.exit("ImageMagick's magick is needed for the means and the distances")
    if subprocess.run([BCS, "status"], cwd=ROOT, capture_output=True).returncode == 0:
        sys.exit("a session is serving; stop it first, since this run opens its own")

    data = tempfile.mkdtemp(prefix="bcs-denoiser-data-")
    scratch = options.keep or tempfile.mkdtemp(prefix="bcs-denoiser-")
    os.makedirs(scratch, exist_ok=True)
    folder = os.path.join(data, "BevyCSharp Feature Test")
    os.makedirs(folder)
    with open(os.path.join(folder, "feature-test.json"), "w", encoding="utf-8") as settings:
        json.dump({"RayTraced": True, "Weather": False, "Bloom": False, "Smoothing": "None", "DaySpeed": 0}, settings)

    # The session is written under the same folder, where every bcs after this one looks for it.
    os.environ["XDG_DATA_HOME"] = data
    opened = subprocess.run([BCS, "open", "--feature-test", "--offscreen", "--frames", "0",
                             "--frame-time", "0.0166667", "--hold", "loads,commands"],
                            cwd=ROOT, capture_output=True, text=True)
    if opened.returncode != 0:
        sys.exit(f"the feature test did not open: {opened.stdout.strip()}")

    try:
        command("pipelines.wait")
        command("frames.wait", "60")
        said = command("feature.denoiser", "on")
        if "not running" in said:
            sys.exit(f"the feature test says {said}; it needs a bridge built with --solari and a GPU that traces rays")
        command("mode", "spectator")
        # The HUD's frame count changes in every frame, and is taken off the picture.
        command("input.key", "F1")
        command("frames.wait", "2")

        print(f"{'view':<16} {'noisy':>7} {'denoised':>9}")
        for name, words in VIEWS:
            command(*words)
            _, noisy, denoised = standing(name.replace(" ", "-"), scratch, options.frames)
            print(f"{name:<16} {noisy:7.2f} {denoised:9.2f}")

        where, read = strafe(scratch, options.frames)
        print(f"\nthe strafe, {STRAFE_FRAMES} frames past the tall block, stopped at {where}")
        for name, (noisy, denoised) in read.items():
            print(f"{name:<34} {noisy:7.2f} {denoised:9.2f}")
    finally:
        bcs("stop", "--name", SESSION, check=False)
        shutil.rmtree(data, ignore_errors=True)
        if not options.keep:
            shutil.rmtree(scratch, ignore_errors=True)


if __name__ == "__main__":
    main(sys.argv[1:])
