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
dotnet build                      # this moves the .so where apps will find it
```

`./bcs build --editor` does both in that order, and [.github/BUILDING.md](.github/BUILDING.md) has
the rest. `--meshlet` and `--solari` add Bevy's meshlets and ray-traced lighting to any profile; without
them their tests are skipped, and a run's summary counts them. The three profiles are cumulative: `headless` < `render` (window, audio) < `editor`
(asset watcher, picking). `App.HasRenderer` and `App.HasEditor`
report which one is loaded; `Native.ExpectedAbiVersion` must match what the bridge reports, and a
mismatch means the bridge needs rebuilding.

This checkout builds the bridge inside a container (`build/build-native.local` sets `PORTABLE=1`),
so the first build is slow and later ones are incremental.

## Tests

```bash
dotnet test BevyCSharp.Tests/BevyCSharp.Tests.csproj   # or ./bcs test
cargo test --manifest-path native/Cargo.toml

# After touching native/, all three profiles, because most of that code is behind a feature and
# only the smallest profile compiles the paths the other two leave out.
for f in headless render editor; do
  cargo check --manifest-path native/Cargo.toml --no-default-features --features $f
done
```

`cargo check` with no feature at all fails by design, so a profile is always named.

The suite runs real headless engines through `EngineHarness` (`BevyCSharp.Tests/EngineFixture.cs`),
which inverts assertions into systems, because everything ECS-touching needs a world on loan from
a running system and there is no way to assert from outside the loop. Engine tests share the
`"engine"` collection, which disables parallelization, because two native apps at once is not
allowed.
Stop any serving session before running the suite.

## Where things are

| Path | Holds |
|---|---|
| `BevyCSharp/Core` | `App`, `Config`, `World`, stages, plugins |
| `BevyCSharp/Ecs` | `EcsWorld`, the component registry, `ComponentSchemas` (generated, reflection-free) |
| `BevyCSharp/Interop` | `Native` (the `bcs_*` entry points), the loader and the ABI check |
| `BevyCSharp/Physics` | Rigid bodies over BepuPhysics, simulated on the managed side |
| `BevyCSharp/Diagnostics` | The console log ring and the `[Command]` catalog |
| `BevyCSharp/Cli` | The server side of `./bcs`: session file, socket, request queue |
| `BevyCSharp.Generator` | Behavior, schema and command generators |
| `BevyCSharp.Editor/Framework` | The editor's panels, history and theming |
| `BevyCSharp.Scripting` | The script host that compiles behavior scripts while an app runs |
| `BevyCSharp.Player` | Plays a scene file with its scripts, which the editor's Play scene runs |
| `native/bevy_csharp/src` | The Rust bridge, one module per subsystem |
| `BevyCSharp/Assets` | Assets and what draws them, in `AssetServer`, `Render`, `Render2d`, `Shaders`, `Ui`, `Gizmos`, `Audio` and `Animation` |
| `BevyCSharp/Behaviors` | The `[Behavior]` attributes, run conditions, `BehaviorContext` and the runner behaviors are registered with |
| `BevyCSharp/Generated` | `bevy-components.tsv`, Bevy's components as the wrappers in `Bevy.Reflected` are generated from |
| `BevyCSharp/Input` | `Input`, `Key`, gamepads and `SyntheticInput` |
| `BevyCSharp/Math` | `Vec2`, `Vec3`, `Quat`, `Transform` and `GlobalTransform` |
| `BevyCSharp/Scenes` | Scene files, saved games and `Persistent<T>` settings |
| `BevyCSharp/Time` | `Time`, the frame's clock and the fixed one |
| `BevyCSharp/Ui` | ImGui's runtime, input and textures |
| `BevyCSharp/build` | The props and targets the package adds to a game's build |
| `BevyCSharp.Cli` | `./bcs`, the command line that drives a running app |
| `BevyCSharp.Examples` | Bevy's examples written in C#, opened by name, and `triage.tsv` for the rest |
| `BevyCSharp.FeatureTest` | Every feature on one map, with an admin panel, an overlay and a console, run with a window, offscreen or headless |
| `BevyCSharp.Tests` | The test suite, run on real engines through `EngineHarness` |
| `build/cheatsheet` | Writes CHEATSHEET.md from the built library |
| `docs` | The guide for somebody using the engine, a page an area, which the README links |
| `games/Courtyard` | A game built from the packed package, which the workflow plays |
| `games/Stress` | The engine under load, for the measurements of PERFORMANCE.md |
| `scenes` | The manifests of the scene packs, well-known graphics scenes fetched as packs on demand |
| `templates` | The templates `dotnet new bevycsharp` and `bevycsharp-empty` make a game from, packed beside the engine by `build/pack-templates.sh` |

## Conventions

- Comments explain **why**, at length, in prose. Match the surrounding density rather than trimming
  them; a file here usually carries more explanation than code.
- Public API carries XML docs with a `<remarks>` section covering the reasoning and the traps.
- Nothing in the library reflects at runtime, because the generators emit registrations that
  module initializers run, so everything survives trimming and AOT.
- A `[Command]` method is a console command, a CLI verb and an editor console entry all at once.
  Adding one is writing one.
- Prose in this repository follows `.github/STYLE.md`, which governs comments, XML documentation,
  messages and Markdown. Read it before writing any of them.
- Each finished batch of work is committed on `main` and never pushed, with a message whose subject
  is three invisible marks and whose description is one plain sentence. Before each commit,
  `.github/STYLE.md` is read and applied to what is staged, the message included.
  `.github/COMMITS.md` has the exact form, that pass, and how to split a batch that shares a file
  with another.
- `.github/REVIEW.md` is read before a batch is started and before each commit. A second session
  writes it after reading the code and the history, and its Now list comes before TODO.md's order.
  Only its Replies section is edited here, for an item that is disputed or blocked, and the file is
  committed with whichever batch comes next.
- `.github/SHARED.md` records what this engine and its sibling (BevyCSharp and 3DEngine) have in
  common and which of them has solved what. It is the same file in both repositories and is
  written by the session that writes REVIEW.md. A batch that touches a shared area is offered
  with a line under Replies in REVIEW.md beginning `Shared:`, and the other repository, checked
  out beside this one, may be read for a model and is never edited from here.
- `.github/NORM.md` holds the rules this engine and its sibling keep, each with a number, a reason
  and what checks it. It is the same file in both repositories and is written by the session that
  writes REVIEW.md. `dotnet test BevyCSharp.Tests/BevyCSharp.Tests.csproj --filter NormTests` says
  whether the rules hold. A rule read as wrong, or a fault of a kind no rule names, is said with a
  line under Replies in REVIEW.md beginning `Rule:`. A row that the table under Where things are
  lacks is added here when N 1.5 asks for it, which the owner allowed on 2026-10-05, and nothing
  else in this file changes without the owner's word.
- `.github/ASKS.md` holds what the games ask of the engine. A session making a game, in `games` or
  beside it, writes an entry there where the engine falls short, a cost it cannot afford or a thing
  it cannot do, with what it measured and how (`./bcs command frame.profile`), and the session that
  writes REVIEW.md turns the entry into an item of the Now list by its weight and writes the item's
  number under it, which the owner allowed on 2026-10-10. This session reads REVIEW.md as before and
  writes nothing in ASKS.md.
