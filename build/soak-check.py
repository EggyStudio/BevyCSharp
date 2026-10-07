"""Reads what build/soak.sh wrote and fails when something a program holds climbs without leveling
off. The first quarter of the readings, while the program loads and fills its caches, is left out,
and the rest is cut in two halves. The most a value reached in the second half may pass the most it
reached in the first by the slack its kind is given, and no more.

    python3 build/soak-check.py build/soak/<game>.txt [...]

A reading is a line of the seconds into the run and then names and numbers in pairs, as the memory
command answers. A count that only ever grows, as the collections are, is not judged.
"""

import sys

# How far past the first half's most each value may go, as a share of it and an amount.
SLACK = {
    "managed": (0.15, 2 << 20),
    "heap": (0.15, 2 << 20),
    # The process's resident size takes in the graphics driver's memory and the pages the runtime
    # has not given back, which move by megabytes from one reading to the next.
    "process": (0.10, 48 << 20),
    "nativeBytes": (0.10, 4 << 20),
    "nativeBlocks": (0.10, 4000),
    "entities": (0.05, 8),
    "entityIds": (0.05, 8),
    "handles": (0.0, 2),
}

# Every kind of asset, read as asset.<kind>.
ASSET = (0.0, 2)

# Fewer readings than this cannot be split into halves that mean anything.
FEWEST = 6


def read(path):
    rows = []
    with open(path, encoding="utf-8") as lines:
        for line in lines:
            words = line.split()
            pairs = words[1:]
            if len(pairs) < 2 or len(pairs) % 2:
                continue
            rows.append({pairs[i]: int(pairs[i + 1]) for i in range(0, len(pairs), 2)})
    return rows


def judge(path):
    rows = read(path)
    if len(rows) < FEWEST:
        print(f"{path}: {len(rows)} readings, too few to judge")
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
    results = [judge(path) for path in sys.argv[1:]]
    return 0 if all(results) else 1


if __name__ == "__main__":
    sys.exit(main())
