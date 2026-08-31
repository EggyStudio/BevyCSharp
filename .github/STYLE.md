# Style

Conventions for prose in this repository: code comments, XML documentation, log and exception
messages, MSBuild and YAML comments, and Markdown. They exist so that the codebase reads
consistently regardless of who wrote a given file.

## Punctuation

Em dashes and en dashes are not used. Neither `—` nor `–`, in any file.

The spaced hyphen ` - ` is not sentence punctuation. Replace it according to the relationship
between the clauses:

| instead of | write |
|---|---|
| `the world is loaned - only on the main thread` | `the world is loaned, only on the main thread` |
| `it is two thoughts - here is the second` | `it is two thoughts. Here is the second` |
| `not bridged yet - only C# components are - so this fails` | `not bridged yet (only C# components are), so this fails` |
| `driven from elsewhere - a menu, a test` | `driven from elsewhere: a menu, a test` |

A comma continues a clause, a full stop separates two statements, parentheses enclose an aside,
and a colon introduces a list.

Hyphens within words (`side-agnostic`), arithmetic operators (`count - 1`) and the names of
characters (`the minus key`) are unaffected.

## Section banners

A banner names its section and ends:

```rust
// -- Hierarchy
```

Trailing runs of dashes are not used. They carry no information, and their length is not
reproducible across edits.

## Comments

A comment that restates the code is a maintenance liability, because it has to be kept true as
the code changes. Remove it.

```rust
// Incorrect: restates the line below it.
// MinimalPlugins leaves this out, so without it Transform is inert data.
app.add_plugins(TransformPlugin);

// Correct: no comment.
app.add_plugins(TransformPlugin);
```

A comment is warranted when the reader would otherwise ask why, and the answer is not visible in
the surrounding code:

- a constraint that is not apparent locally
- a decision, together with the alternative that was rejected
- a hazard
- a safety argument for an `unsafe` block

Where a comment would be needed to explain what the code does, renaming is usually the better
correction.

## Tone

Prose is factual and plain.

Headings and rule names are descriptive rather than metaphorical or aphoristic. Use "Comments",
not "Comments earn their place".

Marketing register is not used. This includes the rhetorical triple:

> No registration call, no partial-class list, no startup boilerplate. Add the package, write
> behavior scripts, compile, run.

State what the software does instead:

> Behaviors are discovered automatically, so a consuming project needs no registration code.

The words `simply`, `just`, `powerful` and `blazing` are not used, nor are exclamation marks.
`easy` is not used to describe working with the software. It is acceptable in a warning, as in
"easy to get subtly wrong", where it tells the reader something they need.

Limitations are stated directly. "Not implemented yet" is preferable to "coming soon".

## Self-reference

Prose states what is true. It does not describe its own history, and it does not narrate the work
that produced it.

| instead of | write |
|---|---|
| `Scene loading is not blocked, contrary to what this file said before` | `Scene loading works` |
| `The question the old entry raised is settled: parts are addressed individually` | `Parts are addressed individually` |
| `Recorded here so the approach is settled when it comes up` | delete the sentence |
| `Cameras take their common parameters now` | `Cameras take their common parameters` |
| `The bug this was written to explain is fixed now` | delete the sentence |

A reader has no access to the previous version of a file, so a correction phrased against it says
nothing. Rewrite the passage as a plain statement and let the diff carry the change.

The words `now`, `already` and `no longer` are the usual signals. Each is fine where it
distinguishes two states the reader can see, as in "the handle reports `Loading` until the file
has been read", and wrong where it only means "since the last edit".

The same applies to `TODO.md`. An item is a description of outstanding work, so completed work is
removed from it or the entry is rewritten around what remains. It is not annotated as done, struck
through, or kept for the record.

## References

Prose refers only to what a reader of the repository can open. An ignored directory, a path that
exists on one machine, a private branch or an internal ticket tells them nothing.

`.ref/` is ignored by git, so it is not cited. Where something in it informed a decision, state
the decision and the reasoning, which is the part worth keeping:

| instead of | write |
|---|---|
| `.ref/3DEngine has a working version to follow. Its shape:` | `Two layers, so that no backend type reaches user code:` |
| `as the reference engine does it` | describe the approach |

Public sources are citable: a crate on docs.rs, a type in a dependency, an upstream issue. So is
anything checked in, by repository-relative path.

## Spelling

`behavior` rather than `behaviour`, in prose as well as in code, matching the public API.

Otherwise either British or American spelling is acceptable, provided a single file is
internally consistent.

## Scope

- `//`, `///` and `//!` in C# and Rust
- exception messages, log output and `GITHUB_STEP_SUMMARY` content
- MSBuild `<!-- -->` comments and analyzer `messageFormat` strings
- YAML comments under `.github/workflows`
- `README.md`, `.github/TODO.md` and this file

## Checks

```bash
# Em and en dashes.
grep -rn "[—–]" --include=*.cs --include=*.rs --include=*.md --include=*.yml .

# Spaced hyphens in comments and strings.
grep -rn "^\s*\(///\|//\|//!\) .* - \|\"[^\"]* - [^\"]*\"" --include=*.cs --include=*.rs .

# Padded section banners.
grep -rn "// -- .*--" --include=*.cs --include=*.rs .

# Prose about earlier revisions.
grep -rniE "contrary to what|this (file|document|entry) (said|used to)|the (old|previous) (entry|version)|at the time .* was written" --include=*.md .

# References to paths that are not checked in.
grep -rn "\.ref/" --include=*.cs --include=*.rs --include=*.md . | grep -v "^\./\.ref/"
```

Arithmetic such as `counts[i - 1]` is the expected false positive in the second command.
