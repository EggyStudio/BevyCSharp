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

## Spelling

`behavior` rather than `behaviour`, in prose as well as in code, matching the public API.

Otherwise either British or American spelling is acceptable, provided a single file is
internally consistent.

## Scope

- `//`, `///` and `//!` in C# and Rust
- exception messages, log output and `GITHUB_STEP_SUMMARY` content
- MSBuild `<!-- -->` comments and analyzer `messageFormat` strings
- YAML comments under `.github/workflows`
- `README.md` and this file

## Checks

```bash
# Em and en dashes.
grep -rn "[—–]" --include=*.cs --include=*.rs --include=*.md --include=*.yml .

# Spaced hyphens in comments and strings.
grep -rn "^\s*\(///\|//\|//!\) .* - \|\"[^\"]* - [^\"]*\"" --include=*.cs --include=*.rs .

# Padded section banners.
grep -rn "// -- .*--" --include=*.cs --include=*.rs .
```

Arithmetic such as `counts[i - 1]` is the expected false positive in the second command.
