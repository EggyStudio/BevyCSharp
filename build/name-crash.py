#!/usr/bin/env python3
"""Names the frames a crash file writes as offsets into the bridge, from the full library kept for
its build.

    build/name-crash.py <crash file> <symbols folder>...
    build/name-crash.py --build-id <library>

A crash file's panic carries, after std's stack, a line naming the bridge's format and build id and
a line for each frame, a frame in the bridge as `bridge+0x` and its offset. A pack's bridge carries
no symbol table, so std's stack names none of its frames, and each pack run keeps the full library
for each system under build/symbols/<rid>, with its build id in a `.build-id` file beside it, as an
artifact of the run (.github/BUILDING.md). This finds the library whose build id the crash names
among the folders given, a run's downloaded artifact or this checkout's build/symbols, and writes
each frame again with the function it lies in, read from the library's symbol table by nm.

An offset is a frame's return address, which lies past the call it returns from, so every frame
but the first is looked up a byte before its offset, inside the call. A Windows library carries no
symbol table and its symbols lie in a PDB, which nm does not read, so its frames keep their offsets
and its build id says which PDB holds them.

With --build-id it prints a library's build id as the crash file writes it, an ELF's GNU build id
note or a Mach-O's UUID in hex, or a PE's CodeView GUID and age as a symbol server names a PDB.
"""

import os
import re
import shutil
import struct
import subprocess
import sys


def build_id(path):
    """The library's build id as the bridge writes it, or None where it carries none."""
    data = open(path, "rb").read()
    if data[:4] == b"\x7fELF":
        return elf_build_id(data)
    if struct.unpack_from("<I", data, 0)[0] == 0xFEEDFACF:
        return macho_uuid(data)
    if data[:2] == b"MZ":
        return pe_codeview(data)
    return None


def elf_build_id(data):
    headers, = struct.unpack_from("<Q", data, 0x20)
    size, count = struct.unpack_from("<HH", data, 0x36)
    for index in range(count):
        at = headers + index * size
        kind, = struct.unpack_from("<I", data, at)
        if kind != 4:
            continue
        offset, = struct.unpack_from("<Q", data, at + 0x08)
        length, = struct.unpack_from("<Q", data, at + 0x20)
        note = offset
        while note + 12 <= offset + length:
            name, described, note_kind = struct.unpack_from("<III", data, note)
            named = note + 12
            description = named + (name + 3) // 4 * 4
            if note_kind == 3 and data[named:named + name] == b"GNU\0":
                return data[description:description + described].hex()
            note = description + (described + 3) // 4 * 4
    return None


def macho_uuid(data):
    count, = struct.unpack_from("<I", data, 16)
    command = 32
    for _ in range(count):
        kind, size = struct.unpack_from("<II", data, command)
        if kind == 0x1B:
            return data[command + 8:command + 24].hex()
        command += size
    return None


def pe_codeview(data):
    headers, = struct.unpack_from("<I", data, 0x3C)
    if data[headers:headers + 4] != b"PE\0\0":
        return None
    sections, optional_size = struct.unpack_from("<HxxxxxxxxxxxxH", data, headers + 6)
    optional = headers + 24
    directory, length = struct.unpack_from("<II", data, optional + 112 + 6 * 8)
    table = optional + optional_size
    for index in range(sections):
        at = table + index * 40
        size, address, _, raw = struct.unpack_from("<IIII", data, at + 8)
        if address <= directory < address + size:
            directory = directory - address + raw
            break
    for entry in range(directory, directory + length, 28):
        kind, = struct.unpack_from("<I", data, entry + 12)
        raw, = struct.unpack_from("<I", data, entry + 24)
        if kind == 2 and data[raw:raw + 4] == b"RSDS":
            first, second, third = struct.unpack_from("<IHH", data, raw + 4)
            rest = data[raw + 12:raw + 20]
            age, = struct.unpack_from("<I", data, raw + 20)
            return f"{first:08X}{second:04X}{third:04X}{rest.hex().upper()}{age:X}"
    return None


def libraries(folders):
    """Every library under the folders, with its build id, read from the file beside it or from it."""
    for folder in folders:
        for top, _, files in os.walk(folder):
            for name in files:
                if not re.search(r"\.(so|dylib|dll)$", name):
                    continue
                path = os.path.join(top, name)
                kept = path + ".build-id"
                yield path, open(kept).read().strip() if os.path.exists(kept) else build_id(path)


def symbols(path):
    """The library's functions by address, read by nm, in order."""
    nm = shutil.which("nm") or shutil.which("llvm-nm")
    if nm is None:
        sys.exit("no nm to read the library's symbols with")
    listed = subprocess.run([nm, "-n", "-C", "--defined-only", path], capture_output=True, text=True)
    found = []
    for line in listed.stdout.splitlines():
        parts = line.split(" ", 2)
        if len(parts) == 3 and parts[1] in "TtWw" and parts[0].strip():
            found.append((int(parts[0], 16), parts[2]))
    return found


def named(table, offset):
    """The function in `table` that `offset` lies in, the last one at or before it."""
    low, high = 0, len(table)
    while low < high:
        middle = (low + high) // 2
        if table[middle][0] <= offset:
            low = middle + 1
        else:
            high = middle
    return table[low - 1][1] if low > 0 else None


def name(crash, folders):
    """The crash file again, each of its bridge frames with the function it lies in."""
    found = list(libraries(folders))
    out, table = [], None
    for line in open(crash, encoding="utf-8", errors="replace").read().splitlines():
        bridge = re.match(r"bridge (\S+) (\S+)$", line)
        if bridge:
            form, build = bridge.groups()
            match = next((path for path, kept in found if kept == build), None)
            table = symbols(match) if match and form != "pe" else None
            if table:
                out.append(f"{line}, named from {match}")
            elif match is None:
                out.append(f"{line}, no library of that build among the folders given")
            elif form == "pe":
                out.append(f"{line}, not named, its symbols in the PDB beside {match}")
            else:
                out.append(f"{line}, not named, {match} carrying no symbol table")
            continue
        frame = re.match(r"(\s+)(\d+): bridge\+0x([0-9a-f]+)$", line)
        if frame and table is not None:
            index, offset = int(frame.group(2)), int(frame.group(3), 16)
            function = named(table, offset if index == 0 else offset - 1)
            out.append(f"{line}  {function or '?'}")
            continue
        out.append(line)
    return out


def main():
    if len(sys.argv) == 3 and sys.argv[1] == "--build-id":
        found = build_id(sys.argv[2])
        if found is None:
            sys.exit(f"{sys.argv[2]} carries no build id")
        print(found)
        return
    if len(sys.argv) < 3:
        sys.exit(__doc__.strip().split("\n\n")[1])
    print("\n".join(name(sys.argv[1], sys.argv[2:])))


if __name__ == "__main__":
    main()
