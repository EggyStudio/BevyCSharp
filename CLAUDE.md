# BevyCSharp

A C# binding over the Rust Bevy engine: a managed library (`BevyCSharp`), a source generator that
turns `[Behavior]` structs into systems, an ImGui editor that is itself an ordinary BevyCSharp app,
and a native bridge (`native/bevy_csharp`, a Rust cdylib) that the managed side calls through a C
ABI.

## Driving the engine with `./bcs`

`./bcs` talks to a running app over a local socket and answers in JSON. **Before asking anything
about a running app (what is in the world, what a click does, what the window looks like), check
`./bcs status` and drive the live session instead of launching a process per question.**

```bash
./bcs status          # is anything serving?
./bcs open --editor   # start one, detached, and wait until it answers
                      # add --offscreen where there is no display to open a window on
./bcs list            # what that app can be asked
./bcs command app.status
./bcs shot /tmp/x.png
```

The full workflow, the envelope, the exit codes and the failure modes are in
`.claude/skills/bcs-cli/SKILL.md`. Read it before driving a session.

## Build order matters

The native bridge and the managed side are built separately, and **a native rebuild is invisible
until a managed build copies the library into each project's `bin`**:

```bash
build/build-native.sh --editor    # or --render, or nothing for headless
dotnet build                      # this is what moves the .so where apps will find it
```

`./bcs build --editor` does both in that order. The three profiles are cumulative: `headless` <
`render` (window, audio) < `editor` (asset watcher, picking). `App.HasRenderer` and `App.HasEditor`
report which one is loaded; `Native.ExpectedAbiVersion` must match what the bridge reports, and a
mismatch means the bridge needs rebuilding.

This checkout builds the bridge inside a container (`build/build-native.local` sets `PORTABLE=1`),
so the first build is slow and later ones are incremental.

## Tests

```bash
dotnet test BevyCSharp.Tests/BevyCSharp.Tests.csproj   # or ./bcs test
cargo test --manifest-path native/Cargo.toml
```

The suite runs real headless engines through `EngineHarness` (`BevyCSharp.Tests/EngineFixture.cs`),
which inverts assertions into systems, because everything ECS-touching needs a world on loan from
a running system and there is no way to assert from outside the loop. Engine tests share the
`"engine"` collection, which disables parallelisation, because two native apps at once is not
allowed.
Stop any serving session before running the suite.

## Where things are

| Path | Holds |
|---|---|
| `BevyCSharp/Core` | `App`, `Config`, `World`, stages, plugins |
| `BevyCSharp/Ecs` | `EcsWorld`, the component registry, `ComponentSchemas` (generated, reflection-free) |
| `BevyCSharp/Interop` | `Native` (the `bcs_*` entry points), the loader and the ABI check |
| `BevyCSharp/Diagnostics` | The console log ring and the `[Command]` catalog |
| `BevyCSharp/Cli` | The server side of `./bcs`: session file, socket, request queue |
| `BevyCSharp.Generator` | Behavior, schema and command generators |
| `BevyCSharp.Editor/Framework` | The editor's panels, history, theming and script host |
| `native/bevy_csharp/src` | The Rust bridge, one module per subsystem |

## Conventions

- Comments explain **why**, at length, in prose. Match the surrounding density rather than trimming
  them; a file here usually carries more explanation than code.
- Public API carries XML docs with a `<remarks>` section covering the reasoning and the traps.
- Nothing in the library reflects at runtime, because the generators emit registrations that
  module initialisers run, so everything survives trimming and AOT.
- A `[Command]` method is a console command, a CLI verb and an editor console entry all at once.
  Adding one is writing one.
- Prose in this repository follows `.github/STYLE.md`, which governs comments, XML documentation,
  messages and Markdown. Read it before writing any of them.
