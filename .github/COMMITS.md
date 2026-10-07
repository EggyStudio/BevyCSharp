# Commits

How work is committed in this repository, by hand or by an agent working through the TODOs.

## When

Each logical batch of work is committed on its own once it is finished, its tests pass and its
documentation says what it does. Commits are made on `main` and are never pushed, since pushing is
the owner's to do.

A batch is one thing somebody would want to read, revert or cherry-pick alone, such as a fix, a
feature with its tests and its docs, or a refactor. Work that is still half done stays uncommitted
until it is whole.

## Before committing

Before a commit is made, [STYLE.md](STYLE.md) is read and applied to everything the commit adds:
comments, XML documentation, messages, Markdown and the commit message itself. The checks at the
end of STYLE.md are run over what is staged rather than the whole tree, so a hit is in the work
being committed and is fixed before it goes in:

```bash
# The added lines alone, read rather than counted, since arithmetic matches the spaced hyphen.
git diff --cached | grep '^+' | grep -nE '[—–]| - |\b(is|are|was|were) what\b|which is what|\bwants?\b|\bjust\b|\bsimply\b|!$'
```

The grep finds the mechanical faults. The rest of STYLE.md (colons joining clauses, tone, prose
about earlier revisions, American spelling) is checked by reading the staged diff, since no pattern
tells a label from a joint. A fix found here is made and staged, and the commit follows.

The build is checked for warnings too, from nothing, with the commands under Warnings in
[BUILDING.md](BUILDING.md), since a build that reuses what an earlier one left up to date prints
none of that earlier build's warnings and passes with them standing. A warning found here is
mended before the commit, as a fault of style is.

## The message

Three lines, of which the first reads as nothing and the third says what the commit does.

1. **Three invisible marks**, U+200E (the left-to-right mark) three times with a space between each
   (`‎ ‎ ‎`), so the subject line looks empty in a log.
2. **A blank line**, which tells Git the subject has ended and a description begins.
3. **One plain sentence** saying what the commit does and what it changed, as somebody would say it
   out loud.

The sentence has no colons, no headings, no lists, no prefixes such as `feat` or `fix`, and no
trailers. It names no tool, model or assistant, and carries no `Co-Authored-By` line. It gives the
reason for a change and names no one who asked for it or decided it, since the release notes are
made from the messages (NORM.md, N 4.7). It is short, and it follows [STYLE.md](STYLE.md) like the
rest of the prose here.

A commit that changes the library's public surface carries `BevyCSharp/PublicApi.txt` written again
by `build/api.sh`, which the suite holds the library to, so the change is in the diff where it is
read.

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
