# Style

Conventions for prose in this repository, which covers code comments, XML documentation, log and
exception messages, MSBuild and YAML comments, and Markdown. They exist so that the codebase reads
consistently regardless of who wrote a given file.

## Punctuation

Em dashes and en dashes are not used. Neither `—` nor `–`, in any file.

The spaced hyphen ` - ` is not sentence punctuation. Replace it according to the relationship
between the clauses.

| instead of | write |
|---|---|
| `the world is loaned - only on the main thread` | `the world is loaned, only on the main thread` |
| `it is two thoughts - here is the second` | `it is two thoughts. Here is the second` |
| `not bridged yet - only C# components are - so this fails` | `not bridged yet (only C# components are), so this fails` |
| `set from elsewhere - a menu, a test` | `set from elsewhere, such as a menu or a test` |

A comma continues a clause, a full stop separates two statements, and parentheses enclose an
aside.

Hyphens within words (`side-agnostic`), arithmetic operators (`count - 1`) and the names of
characters (`the minus key`) are unaffected.

A running sentence holds no colon. A colon stands at the end of a sentence that introduces a
bulleted list, a code block or a command set below it, and after a label in a table. Inside a
sentence it appends an elaboration or a list that a sentence of its own, or the same sentence
recast, says plainly, and once every paragraph takes that shape the reader stops reading the mark
as anything at all.

The test is the text after the mark. A bulleted list, a code block or a command set below the
sentence is correct. Text that continues the sentence is a joint, whatever it says, so
`a tag is cheap: it costs nothing to store` is wrong where
`a tag is cheap, because it costs nothing to store` is right, and a list appended after the mark is
folded into the sentence or given a sentence of its own.

| instead of | write |
|---|---|
| `the wheel never moves the panel: rolling over a list hands it to whatever holds it` | `the wheel never moves the panel, because rolling over a list hands it to whatever holds it` |
| `a click is the engine's to answer: it raycasts the scene` | `a click is the engine's to answer. It raycasts the scene` |
| `not the world: the world is what the project is` | `not the world, which is the project itself` |
| `three files, because they are edited by different hands: the first is` | `three files, because they are edited by different hands. The first is` |
| `it offers three: a menu, a button, a key` | `it offers a menu, a button and a key` |
| `makes the program the README opens with: a window with a turning cube, a behavior that turns it, and the one line that runs the app` | `makes the program the README opens with, a window with a turning cube, a behavior that turns it and the one line that runs the app` |
| `Exit codes: 0 ok, 4 no session` | `The exit code is 0 for success and 4 where no app is serving` |
| `**What is not here.** Despawning is not recorded` | unchanged, because the mark labels rather than joins |
| `Usage:` on a line of its own over a command | unchanged, because a command follows |

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
// Incorrect, since it restates the line below it.
// MinimalPlugins leaves this out, so without it Transform is inert data.
app.add_plugins(TransformPlugin);

// Correct, with no comment.
app.add_plugins(TransformPlugin);
```

A comment is warranted when the reader would otherwise ask why, and the answer is not visible in
the surrounding code, as in these cases:

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

Marketing register is not used. This includes the rhetorical triple.

> No registration call, no partial-class list, no startup boilerplate. Add the package, write
> behavior scripts, compile, run.

State what the software does instead.

> Behaviors are discovered automatically, so a consuming project needs no registration code.

The words `simply`, `just`, `powerful` and `blazing` are not used, nor are exclamation marks.
`easy` is not used to describe working with the software. It is acceptable in a warning, as in
"easy to get subtly wrong", where it tells the reader something they need.

Antithesis is not used for emphasis. A claim paired with the negation of something nobody
proposed reads as a slogan, and the negated half carries nothing.

> Discovery is generated, not scanned.

State the claim on its own.

> Registration happens at assembly load.

The construction is warranted where a reader would otherwise assume the negated alternative, and
correcting that assumption is the point of the sentence, as in "that is Bevy's real `Transform`, not
a copy kept in sync".

Limitations are stated directly. "Not implemented yet" is preferable to "coming soon".

### Saying what things are for

A sentence names its subject and says what it does. The cleft forms `X is what Y needs`,
`which is what Y wants`, `that is what Y is for` and `this is what makes it work` route a plain
statement through a relative clause and read as a mannerism once they recur, which they do, because
every explanation can be forced into that shape. Code and features do not want things either, so
`a camera wants Msaa of one` is written as the requirement it is.

| instead of | write |
|---|---|
| `a prepass is what an outline is made of` | `an outline is drawn from the prepass` |
| `which is what screen-space GI needs` | `which screen-space GI needs` |
| `that is what the timestamp queries are for` | `the timestamp queries measure that` |
| `this is what lets a reload keep its values` | `so a reload keeps its values` |
| `a scene of dense meshes wants meshlets` | `a scene of dense meshes benefits from meshlets` |
| `a camera read this way wants Msaa of one` | `a camera read this way needs Msaa of one` |
| `what the caller wanted` | unchanged, because a person wants things |

The same applies to `what X does is Y`, `the point is that`, and `the reason is that`, which delay
the statement they introduce.

## Register

Prose uses the technical and generic term for a thing, the engine's own where it has one, and no
figure of speech or casual paraphrase in its place. A paraphrase reads as an explanation the first
time and as a mannerism after, and a reader who knows the term translates it back each time.

A likeness is not a description and a figure of speech is not a term, so a thing is said by what it
is and does. Another product is named only where a rule or a number is taken from it, as the
editor's measures are taken from Unity's and the examples from Bevy's. A description holds no plan,
so no line says what a thing is the start of or grows toward.

| instead of | write |
|---|---|
| `the light that bounces`, `the bounce` | `global illumination`, or `indirect light` where its effect is meant |
| `a material that gives off light`, `a glowing screen` | `an emissive material`, `an emissive screen` |
| `drives the running app`, `driving a session` | `controls the running app through the command line`, `scripting a session` |
| `a drive of the feature test`, `the drive through the course` | `a scripted run of the feature test`, `the run through the course` |
| `A voxel game in the manner of Minecraft, and the ground floor of a larger one` | `A voxel game set in an endless generated world whose blocks are placed and broken, with its light, its water, its mobs and the items they drop` |

The idioms in the manner of, the ground floor of, out of the box, under the hood, at a glance, a
handful of, a great deal of, in the wild, the knob, the plumbing, the wiring, an escape hatch, for
free and a long way are replaced in the same way, each by the term or the number it stands for. A
word in its literal sense stays, as by hand does for work done without a tool or a program and a
knob does on a gamepad's stick.

A word in its technical sense keeps it, as a GPU's driver, a joint's drive, which is the motor of a
slider, a body that bounces off another and a path tracer's bounces do. A name of the engine's API
keeps its spelling, such as `PhysicsMaterial.Bounce` and `SolariLightingRef.MaxBounces`, and an
identifier keeps its name, as `build/drive-feature-test.sh` and the workflow's `drive` job do.

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

A list of gaps is a claim about the software in the same way the rest of the prose is, so closing
a gap means rewriting the entry around what is left of it. `TODO.md` and the README's status
section describe the same software and are kept consistent with each other.

## References

Prose refers only to what a reader of the repository can open. An ignored directory, a path that
exists on one machine, a private branch or an internal ticket tells them nothing.

`.ref/` is ignored by git, so it is not cited. Where something in it informed a decision, state
the decision and the reasoning, which is the part worth keeping:

| instead of | write |
|---|---|
| `.ref/3DEngine has a working version to follow. Its shape:` | `Two layers, so that no backend type reaches user code:` |
| `as the reference engine does it` | describe the approach |

Public sources are citable, such as a crate on docs.rs, a type in a dependency or an upstream
issue. So is anything checked in, by repository-relative path.

## Spelling

American spelling is used everywhere, in prose as well as in code, as in `behavior`, `color`,
`center`, `gray`, `meter`, `neighbor`, `initialize`, `serialize`, `canceled`, `modeled` and
`toward`. The public
API is spelled that way, and so are Bevy, .NET and wgpu, and a word spelled one way in a type name
and another in the sentence describing it reads as two words.

A name that comes from outside is kept as it is, such as .NET's `StringMarshalling`, a file name in
a third-party icon set, or the text of a license.

## Scope

- `//`, `///` and `//!` in C# and Rust, and `#` comments in shell, Python and YAML
- exception messages, log output, what a program or a script prints, and `GITHUB_STEP_SUMMARY`
  content
- MSBuild `<!-- -->` comments and analyzer `messageFormat` strings
- every Markdown file, the README, the cheatsheet, the guide under `docs/`, the templates' READMEs,
  `AGENTS.md` and the documents under `.github`

## Checks

```bash
# Em and en dashes.
grep -rn "[—–]" --include=*.cs --include=*.rs --include=*.md --include=*.yml .

# Spaced hyphens in comments and strings.
grep -rn "^\s*\(///\|//\|//!\) .* - \|\"[^\"]* - [^\"]*\"" --include=*.cs --include=*.rs .

# Colons inside a running sentence, in comments and scripts and in the prose of the Markdown files
# outside their code. A label is correct, so the hits are read rather than counted.
grep -rn "^\s*\(///\|//\|//!\|#\) .*[a-z)\`]: " --include=*.cs --include=*.rs --include=*.sh --include=*.py --include=*.yml .
awk 'FNR == 1 { code = 0 } /^ *```/ { code = !code; next } !code && /[a-z)`]: /' README.md CHEATSHEET.md docs/*.md templates/README.md AGENTS.md .github/*.md

# The casual paraphrases the register replaces. A word in its technical sense, and a name, are
# left, so each hit is read.
git grep -n -i -E "light that bounces|the bounce|bounc(e|es|ing) (no )?light|give[sn]? off|gave off|glowing|\bdriv(e|en|es|ing)\b" -- ':!.github/REVIEW.md'

# The likenesses and idioms the register replaces. A word in its literal sense is left, so each hit
# is read, and the vendored weather crate and the third-party notices are their authors' text.
git grep -n -i -E "in the manner of|ground floor|out of the box|under the hood|at a glance|a handful of|a great deal of|in the wild|\bknobs?\b|plumbing|wiring|wired up|escape hatch|for free|a long way" -- ':!.github/REVIEW.md' ':!native/bevy_weather' ':!THIRD-PARTY-NOTICES.md'

# Padded section banners.
grep -rn "// -- .*--" --include=*.cs --include=*.rs .

# Cleft sentences and things that want. People want things, so each hit is read.
grep -rniE "\b(is|are|was|were) what\b|which is what|that is what|this is what|\bwants?\b" --include=*.cs --include=*.rs --include=*.md .

# Prose about earlier revisions.
grep -rniE "contrary to what|this (file|document|entry) (said|used to)|the (old|previous) (entry|version)|at the time .* was written" --include=*.md .

# References to paths that are not checked in.
grep -rn "\.ref/" --include=*.cs --include=*.rs --include=*.md . | grep -v "^\./\.ref/"
```

Arithmetic such as `counts[i - 1]` is the expected false positive in the second command.
