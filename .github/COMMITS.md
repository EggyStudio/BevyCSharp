# Commits

How work is committed in this repository, by hand or by an agent working through the TODOs.

## When

Each logical batch of work is committed on its own once it is finished, its tests pass and its
documentation says what it does. Commits are made on `main` and are never pushed, since pushing is
the owner's to do.

A batch is one thing somebody would want to read, revert or cherry-pick alone, such as a fix, a
feature with its tests and its docs, or a refactor. Work that is still half done stays uncommitted
until it is whole.

## The message

Three lines, of which the first reads as nothing and the third says what the commit does.

1. **Three invisible marks**, U+200E (the left-to-right mark) three times with a space between each
   (`‎ ‎ ‎`), so the subject line looks empty in a log.
2. **A blank line**, which tells Git the subject has ended and a description begins.
3. **One plain sentence** saying what the commit does and what it changed, as somebody would say it
   out loud.

The sentence has no colons, no headings, no lists, no prefixes such as `feat` or `fix`, and no
trailers. It names no tool, model or assistant, and carries no `Co-Authored-By` line. It is short,
and it follows [STYLE.md](STYLE.md) like the rest of the prose here.

Written from a shell, so the marks are exact rather than pasted:

```bash
printf '‎ ‎ ‎\n\nThumbnails are drawn on a transparent background unless a color is set under Assets.\n' > /tmp/msg
git commit -F /tmp/msg
```

A message that reads right in `git log --format='%B' | cat -v` shows `M-bM-^@M-^N` three times on
the first line, an empty second line and the sentence on the third.

## A batch that shares a file with another

When two batches touch one file, only the first batch's hunks are staged, so each commit holds one
thing. `git add -p` asks questions interactively and cannot be driven by an agent, so hunks are
picked from the diff and applied to the index instead:

```bash
# Stage hunks 1 and 3 of a file, numbered in the order git diff -U1 lists them.
git diff -U1 -- path/to/File.cs > /tmp/all.patch
# Keep the file header and the chosen @@ hunks, drop the rest, then:
git apply --cached --unidiff-zero /tmp/chosen.patch
```

Hunks are listed with `git diff -U1 -- path | grep -n '^@@'`. Order the commits so that a hunk
which mentions two batches goes in the later of them, after the code it describes exists.

`git diff --cached --stat` before each commit confirms what is in it, and `git status --short`
after the last one confirms nothing was left behind.
