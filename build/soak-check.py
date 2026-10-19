"""Reads what build/soak.sh wrote and fails when something a program holds climbs without leveling
off. The first quarter of the readings, while the program loads and fills its caches, is left out,
and the rest is cut in two halves. The most a value reached in the second half may pass the most it
reached in the first by the slack its kind is given, and no more.

    python3 build/soak-check.py build/soak/<game>.txt [...]
    python3 build/soak-check.py --fewest      # the readings it judges no fewer than

A reading is a line of the seconds into the run and then names and numbers in pairs, as the memory
command answers. A count that only ever grows, as the collections are, is not judged.
"""

import sys

# How far past the first half's most each value may go, as a share of it and an amount.
#
# The live figure is the bytes the managed heap holds alive, those kept by the full compacting
# collection memory.collect makes, as build/soak.sh reads it after each round, and the heap's size
# is its size after that collection, what is alive and the free space pinned objects hold apart.
# A compacting collection leaves no other free space, so a heap that climbs while the live bytes
# stand still is space held by a pin that should have let go. On a ten-minute soak of Courtyard,
# the stress program and Swarm on lavapipe before memory.collect compacted, the heap moved by as
# much as 3,726,064 bytes, the stress program's, with nothing climbing, and compacting alone left
# it moving by 3,330,864, held by the buffer a blocking accept of the command line's server kept
# pinned in the newest generation, which the server's polling before it accepts lets go of.
SLACK = {
    # A ten-minute soak of Courtyard, the stress program and Swarm on lavapipe held what each kept
    # alive within 60,712 bytes of where it settled, Courtyard's 2,191,664 to 2,253,432, so a
    # quarter of a megabyte past the first half's most is four times that.
    "live": (0.05, 256 << 10),
    # The same soak after both mends held each heap within 8,672 bytes of its live bytes, and
    # each moved by 23,416 bytes at most, Courtyard's, so an eighth of a megabyte past the first
    # half's most is five times that.
    "heap": (0.05, 128 << 10),
    # The rest are each twice the most the figure moved, from its least to its most, over a soak of
    # Courtyard, the stress program and Swarm (build/soaks.sh 600 courtyard stress swarm) and one of
    # the feature test (build/soaks.sh 900 feature-test), run on Mesa 26.2.2's lavapipe with
    # VK_ICD_FILENAMES naming its driver. The process's figure is its anonymous resident pages,
    # which take in the runtime's pages not given back and a software device's memory and moved by
    # 22,401,024 bytes, Swarm's. The bridge's bytes moved by 2,076,749 and its blocks by 12,011,
    # Courtyard's, and the entities, the indices and the handles not at all, so they are given the
    # two an asset of each kind is.
    "process": (0.0, 44 << 20),
    "nativeBytes": (0.0, 4 << 20),
    "nativeBlocks": (0.0, 25000),
    "entities": (0.0, 2),
    "entityIds": (0.0, 2),
    "handles": (0.0, 2),
}

# Every kind of asset, read as asset.<kind>.
ASSET = (0.0, 2)

# Fewer readings than this cannot be split into halves that mean anything.
FEWEST = 6


def read(path):
    """The seconds into the run of each reading, and its names and numbers."""
    seconds, rows = [], []
    with open(path, encoding="utf-8") as lines:
        for line in lines:
            words = line.split()
            pairs = words[1:]
            if len(pairs) < 2 or len(pairs) % 2:
                continue
            seconds.append(int(words[0]))
            rows.append({pairs[i]: int(pairs[i + 1]) for i in range(0, len(pairs), 2)})
    return seconds, rows


def judge(path):
    seconds, rows = read(path)
    if len(rows) < FEWEST:
        # How long a round took, the time between readings, so the soak that reads this few is
        # given the seconds its rounds need.
        apart = f", a round taking some {(seconds[-1] - seconds[0]) // (len(rows) - 1)} seconds" if len(rows) > 1 else ""
        print(f"{path}: {len(rows)} readings{apart}, too few to judge where {FEWEST} are")
        return False

    kept = rows[len(rows) // 4:]
    first, second = kept[: len(kept) // 2], kept[len(kept) // 2:]
    names = sorted({name for row in kept for name in row if name in SLACK or name.startswith("asset.")})

    held = True
    for name in names:
        share, amount = SLACK.get(name, ASSET)
        before = max(row.get(name, 0) for row in first)
        after = max(row.get(name, 0) for row in second)
        bound = before * (1 + share) + amount
        climbs = after > bound
        held &= not climbs
        print(f"{path}: {name:24} {before:>12} then {after:>12} (bound {int(bound)}) {'CLIMBS' if climbs else 'ok'}")
    return held


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    if sys.argv[1] == "--fewest":
        print(FEWEST)
        return 0
    results = [judge(path) for path in sys.argv[1:]]
    return 0 if all(results) else 1


if __name__ == "__main__":
    sys.exit(main())
