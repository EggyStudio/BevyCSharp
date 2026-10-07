# Building from source

How to build the native bridge and the package. Using BevyCSharp needs none of this, since the
package ships a prebuilt bridge per platform; this is for working on the bridge itself.

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and
[Rust](https://rustup.rs).

```bash
build/build-native.sh          # build the native bridge (headless profile)
dotnet build                   # build the managed side
build/test.py                  # run the bridge's tests and the suite, with a page of what failed
dotnet run --project BevyCSharp.Sample -- --frames 120 --verbose
```

Most of what the bridge does is only observable from managed code, so the suite is where nearly
all of the coverage is. The Rust tests cover what it cannot reach from there: the convention for
returning text through a caller's buffer, the guard that turns a panic into a status code rather
than an unwind into .NET, and the asset registration that has to stay inert when it is asked twice.

`build/test.py` runs three parts, each a process held to a time and a memory: `bridge`, cargo's
tests of the bridge, `renderer`, the same with the renderer's crates, and `suite`, the managed
suite. Naming parts runs those alone, as `build/test.py suite` does. The log ends with a page of at
most 200 lines, the counts and a line for each part, any process lost, and the failures by cause,
which is also written to `BevyCSharp.Tests/TestResults/digest.md`. A suite that is lost, by a crash,
a hang, its time or its memory, runs again in parts, so one crash costs only its own part's tests.
`dotnet test` and `cargo test` still run as they always did. A test during which the engine logs an
error fails unless it says it expects that error with `[ExpectsError]`.

Everything generated lands in `build/`, cargo's target directory, the staged per-RID artifacts,
and the packed `.nupkg`. The repository root stays clean.

```
BevyCSharp/            managed runtime library
BevyCSharp.Generator/  Roslyn source generator
BevyCSharp.Editor/     the editor, and the framework its panels are built on
BevyCSharp.Sample/     runnable example behaviors
BevyCSharp.Examples/   Bevy's examples in C#, by Bevy's names (.github/EXAMPLES.md)
docs/                  the guide, a page an area, which the README's Guide lists
BevyCSharp.Tests/      test suite, run against a real Bevy app
native/                Rust sources for the bridge
build/                 the native build scripts, and everything they generate
.github/workflows/     CI: builds every runtime identifier, then packs them together
```

## Warnings

A build has no warnings, and a warning fails the workflow (NORM.md, N 6.1). The test job builds the
solution with `-warnaserror`, the game job builds Courtyard the same way, and every cargo build in
the workflow runs with `CARGO_BUILD_WARNINGS=deny`, cargo's own setting, which fails a build that
printed a warning, the warnings of a crate it replays from its cache included. A warning that is
right to keep is turned off where it arises, with its reason beside it.

The library is held further. A public member with no documentation is an error in
`BevyCSharp.csproj`, and so is a fault in the documentation that is there, such as a `param` for
nothing or a `cref` naming nothing (N 2.2), since the summary is the help a game's author sees at
the call. The wrappers the generator writes for Bevy's components count as the library's, and the
generator documents each member it writes.

Before a commit the same check runs here:

```bash
dotnet build BevyCSharp.slnx --no-incremental -warnaserror
for f in headless render editor; do
  cargo check --manifest-path native/Cargo.toml --no-default-features --features $f --all-targets \
    --config 'build.warnings="deny"'
done
```

`--no-incremental` matters. An incremental build passes over a project an earlier build left up to
date and prints none of its warnings again, so a strict build after a plain one passes with a
warning standing, which is how one reached 3DEngine's `main`. Cargo replays a cached crate's
warnings, so its check needs no such flag.

## Native profiles

The bridge builds in two profiles:

| Profile    | What it includes                                                       |
|------------|------------------------------------------------------------------------|
| `headless` | App, ECS, time, input, transform, assets. No window, no GPU. The default. |
| `render`   | The above plus windowing, the renderer, post processing, UI, 2D, gizmos and audio. |
| `editor`   | The above plus Dear ImGui, entity introspection and picking, for `BevyCSharp.Editor`. |

The editor profile costs about a hundred crates and several minutes of build time over the render
one, which is why it is a profile of its own rather than part of it, since the test suite and the
per-platform package builds have no use for it.

```bash
build/build-native.sh --render          # bash
build/build-native.ps1 -Render          # PowerShell, same output
```

The `render` profile is assembled feature by feature rather than taking Bevy's
`default_platform`, which links Wayland at build time. Everything graphical resolves at runtime,
X11 through `x11-dl`, Wayland through `wayland-dlopen` and Vulkan through the loader. It takes
several minutes to compile and produces a much larger library.

Audio and gamepads are the exceptions, and the only system dependencies in the tree. Bevy's audio
sits on cpal, which links against ALSA on Linux, and its gamepads on gilrs, which reads them through
libudev, so a `render` build there needs `libasound2-dev` and `libudev-dev`, or the equivalents for
the distribution (`alsa-lib-devel` and `systemd-devel` on Fedora). `build-native.sh` checks for
both and names the packages if they are missing, and installs them into the container on the
`--portable` path. The `headless` profile has neither and builds with nothing but a C compiler,
though it carries Bevy's gamepad input, which a pad pretended by a script or a test feeds. Neither
affects anyone consuming the NuGet package, which ships the native prebuilt for each runtime
identifier.

`Config.Headless` forces the windowless path even on a render build, which is how the tests and
a dedicated server run the same behavior code without a display.

Bevy's meshlets and its ray-traced lighting are additions to a profile rather than profiles of their
own:

```bash
build/build-native.sh --editor --meshlet --solari    # bash
build/build-native.ps1 -Editor -Meshlet -Solari      # PowerShell
./bcs build --editor --meshlet --solari              # the bridge, then the managed side
```

They compile meshoptimizer and METIS, a C++ and a C library that cut a mesh into clusters, which
nothing else in the tree needs, so no profile carries them by default. A build with them behaves
as one without until an app sets `Config.MeshletClusters`, and even then only on a GPU with 64-bit
texture atomics, which the bridge checks before turning them on. Ray-traced lighting adds no build
dependency, and is kept out of the profiles because adding it makes every material deferred; an
app turns it on with `Config.RayTracedLighting`, on an adapter with ray queries.

A game's assets can be compiled into its own assembly, with `-p:BevyCSharpEmbedAssets=true` on the
build or publish (`BevyCSharp/build/BevyCSharp.Embed.targets`), as the Play tab's export does when
it embeds. The managed side reads them there after the disk and hands the bridge a reader over
them, so the shared bridge serves them to Bevy and what a player is given is the executable and
the library with no folder beside them beyond the scripts and shaders the game compiles from their
files ([PLAY.md](PLAY.md) §4).

A game too large to hold in its assembly ships its assets in one pack file, `assets.pack` beside
the executable, which the Play tab's export writes when asked to pack and which an app reads after
the folder and before its assembly (`AssetPack`, [SCENES.md](SCENES.md) §1).

They can be compiled into the bridge instead, which then serves one game:

```bash
build/build-native.sh --render --embed BevyCSharp.Sample/assets    # bash
build/build-native.ps1 -Render -Embed BevyCSharp.Sample/assets      # PowerShell
```

`bevy_embedded_assets` reads the folder as the library compiles and serves it in place of the asset
root, so every path loads as before and `App.HasEmbeddedAssets` says so. A bridge built so serves
one game, so it is staged under `build/embedded/<rid>/` rather than where the projects here copy
the bridge from. Scenes, data assets and the other files the managed side reads for itself are
still read from the folder or the game's assembly, which a bridge cannot serve.

`--game` (`-Game`) stages a bridge apart in the same way, under `build/game/<rid>/`, which is the
render bridge the Play tab's export ships, so a checkout whose projects run on the editor's bridge
still exports a game without one.

## Bevy's components after an upgrade

`BevyCSharp/Generated/bevy-components.tsv` describes every component Bevy reflects, and the
generator turns it into the typed wrappers in `Bevy.Reflected` (`PointLightRef` and the rest). It
is read out of Bevy's registry in a running app, an editor build because that profile reflects the
most, so it is written again whenever Bevy is upgraded:

```bash
./bcs build --editor
./bcs open --editor --offscreen
./bcs command schema.dump "$PWD/BevyCSharp/Generated/bevy-components.tsv"
./bcs stop
dotnet build
```

The file's diff is the list of what Bevy changed in its components. A field a game used that Bevy
renamed or removed stops compiling at the property that named it, rather than failing on the day
the line runs.

## Platforms

The managed assembly is portable. The bridge is a cdylib, so it has to be compiled once per
runtime identifier, and a package can only contain the platforms someone actually built.

| Runtime identifier                   | Windowing      | Graphics          |
|--------------------------------------|----------------|-------------------|
| `linux-x64`, `linux-arm64`           | X11 or Wayland | Vulkan            |
| `linux-musl-x64`, `linux-musl-arm64` | X11 or Wayland | Vulkan            |
| `win-x64`, `win-arm64`               | Win32          | Vulkan or DX12    |
| `osx-x64`, `osx-arm64`               | Cocoa          | Metal             |

Each is built by the same command, on a machine that can target it:

```bash
build/build-native.sh --render --target aarch64-apple-darwin
```

`.github/workflows/build.yml` does this across a runner matrix and packs every RID into one
package. Locally you get whichever platform you built; `dotnet pack` skips the slots you have
no binary for rather than failing. Each bridge goes under `runtimes/<rid>/native/`, where .NET
looks for the platform it runs on, and the source generator under `analyzers/dotnet/cs/`, where
the compiler finds it.

Three platform notes worth knowing:

- **A Linux build only runs on a glibc at least as new as the one it was built on.** Building on
  a current Fedora and running on an Ubuntu LTS fails at load with a `GLIBC_x.yz not found`
  error, and so does building in one container and running in another. Pass `--portable` to
  build inside a Debian container instead, which lowers the floor to glibc 2.35 and covers every
  supported distribution:

  ```bash
  build/build-native.sh --render --portable
  ```

  It needs podman or docker, and prints the resulting floor either way. The workflow builds on
  `ubuntu-latest`, so packaged binaries are already portable; this is for local builds.

- **macOS requires the window event loop to own the main thread.** `App.Run` checks this and throws
  a clear error rather than letting it crash inside AppKit. The check applies only when a window is
  actually going to be opened (`App.WillOpenWindow`), because the constraint belongs to windowing
  rather than to the engine. A headless run has no event loop and works from any thread, so a test
  runner can drive it from its own worker threads.
- **The one Linux binary serves both X11 and Wayland.** Which is used is decided at runtime, so
  there is no separate build for each.

Beyond the desktop, Bevy also targets Android, iOS and the web. Those need a different .NET
story entirely (a different app model and, for the web, a different runtime), so they are out of
scope here rather than merely unbuilt.

## Packing

```bash
build/build-native.sh          # stage the native bridge first
dotnet pack BevyCSharp/BevyCSharp.csproj -c Release
```

Packing fails with `BCS101` if the staged bridge is older than the Rust sources, because shipping
a stale one produces an `EntryPointNotFoundException` far from its cause.

The package carries `THIRD-PARTY-NOTICES.md`, which names every crate of `native/Cargo.lock` with
its license and the notices its own files give. `build/third-party-notices.py` writes it through
`cargo metadata`, and a change to the lock is followed by running it again, which the pack
workflow checks with `--check` and NormTests' N 6.5 holds to the lock.

To ship more than one platform, run `build-native.sh --target <triple>` for each; every staged RID
slot is picked up at pack time and missing ones are skipped.

## Packages

Every package the library and its generator reference and every crate the bridge names, with what
each is used for. A dependency is surface the engine answers for, so adding one is the owner's
decision, and NormTests' N 2.8 fails for one the project files reference and this list does not
name, and for a row that names nothing they reference. The versions are the project files' own.

The library's, in `BevyCSharp/BevyCSharp.csproj`:

| Package | Used for |
|---|---|
| `Twizzle.ImGui-Bundle.NET` | Dear ImGui, which the interface and the editor are drawn with, carrying its native library for every platform the bridge builds for. |
| `BepuPhysics` | The rigid bodies of `Bevy.Physics`, simulated in C# on this side of the bridge. On a beta line, pinned so an update is a decision. |

The generator's, in `BevyCSharp.Generator/BevyCSharp.Generator.csproj`, which the package carries as
an analyzer. Both are private to the build, since the compiler that runs the generator brings its
own Roslyn.

| Package | Used for |
|---|---|
| `Microsoft.CodeAnalysis.CSharp` | Roslyn, through which the generators read a game's code and write its behaviors, schemas and commands. |
| `Microsoft.CodeAnalysis.Analyzers` | The rules Roslyn holds a generator to while it is built, such as the calls a generator may not make. |

The bridge's, in `native/bevy_csharp/Cargo.toml`. All but Bevy and the embedding are in the tree
through Bevy already, at the version it builds, so naming them adds nothing to the build.

| Crate | Used for |
|---|---|
| `bevy` | The engine, its features chosen by the profile. |
| `nonmax` | Building the row number of a table, for reading a component's column directly. |
| `bytemuck` | Casting the interface's vertices to bytes for the GPU. |
| `naga` | Reading the WGSL a Slang shader compiles to, to check its bindings before a pipeline is built with them (render). |
| `serde_json` | Components read and written through Bevy's reflection, which cross as JSON, and the reflection slangc writes beside a compile. |
| `serde` | The trait a reflected value is deserialized through, which Bevy does not re-export. |
| `wgpu` | Asking a window's surface and the GPU what they support before Bevy is given something they do not (render). |
| `bevy_embedded_assets` | A game's assets compiled into the library (embed). |
| `winit` | Loading Wayland when the program runs rather than linking it, on Linux and the BSDs (render). |

## Publishing

A push or a pull request runs the test suite on Linux and Windows and stops there. It builds no
per-platform bridge and packs nothing, and nothing a commit message says changes that.

A package is made by the **pack** workflow, run by hand from the Actions tab ("Run workflow"). It
builds the bridge for all six platforms, runs the tests on Linux and Windows, plays Courtyard and
walks the README's install in a container (`build/readme-walk.sh`), and packs only once all of
them pass. The package is kept as the run's artifact, to download and upload to nuget.org by hand.
Ticking its **publish** box pushes it to nuget.org from the run instead, which needs the
`NUGET_API_KEY` secret.

The jobs that play, capture, walk and pack run each step through `build/step.py`, as the shell
GitHub runs the step's script in. A step that fails having said nothing is given an error naming
the step, the command that failed with its line and exit code, the step's last lines, and the last
lines at a warning or worse of each log written while it ran, those `bcs open` keeps under
`build/sessions` and those in the folders `BCS_STEP_LOGS` names, such as the game's own log
(NORM.md, N 6.7). The error is an annotation and the job's summary, both of which a reader who is
not signed in to GitHub sees, where the log needs signing in. `build/page.py` holds what it and
`build/test.py` share in saying so.

Courtyard's play holds each frame to a sixtieth of a second of the game with `app.frametime`,
since the play plans each walk in frames and the runner draws with Mesa's software Vulkan, a few
frames a second, which on the machine's clock carried the runner past its coins.

The version is `build/version.sh`'s. Its major and minor are `build/version.txt`'s, set by hand,
and its patch is the number of commits since that file last changed, so each commit raises it by
one and a new minor starts it again at 0. To move to `0.4.x`, change `build/version.txt` and
commit. The script counts commits, so it needs the whole history, which the workflow fetches.

```bash
build/version.sh            # 0.3.12, say
```

---

## Examples

`BevyCSharp.Examples` holds Bevy's examples written in C#, one program picking an example by
Bevy's name. Some load files from Bevy's own assets folder, which stay out of this repository and
are downloaded at the Bevy release the bridge builds by `build/fetch-bevy-assets.sh`, from the list
in `BevyCSharp.Examples/bevy-assets.txt`.

```bash
build/fetch-bevy-assets.sh
dotnet build BevyCSharp.Examples
./bcs open --example 3d_scene              # or dotnet run --project BevyCSharp.Examples -- 3d_scene
build/capture-example.sh 3d_scene          # its picture in .github/assets/examples
build/capture-examples.sh                  # every one, failing on one that does not start or is blank
build/examples-table.py                    # .github/EXAMPLES.md, the README's count and each example's head
build/measure-stress.sh many_sprites       # a stress test beside Bevy's own program, as PERFORMANCE.md has them
```

`build/examples-table.py` reads Bevy's example metadata from the cargo registry, which holds the
bridge's Bevy once the bridge has been built, and `BevyCSharp.Examples/triage.tsv`, where each
example not yet written has its state and what it waits on. It also writes the comment each
written example opens with, naming the example of Bevy's it is written from, at which version and
under Bevy's licenses. The package workflow captures every example on the Linux bridge it built.

An example with nothing to draw, as most of Bevy's ECS examples are, prints instead. It runs
headless for the frames its line in `Catalog.cs` gives, needing no renderer, and its capture is
what it printed, as `<name>.txt` beside the pictures, which its row in EXAMPLES.md links. `--window`
opens an empty window for one that reads keys, as Bevy's does, and `--printing` lists them.

## The guide

`docs/` holds the guide, a page an area for somebody using the engine, and the README is for
somebody deciding whether to, so it links to each page by its full URL, since it is also the
package's page on nuget.org. `build/check-docs.py` follows every link in the README and the guide,
`--external` the ones off this repository too, and the workflow run on every push runs it.

`CHEATSHEET.md` at the root lists every public method a line each, and is written from the library
and its XML documentation by `build/cheatsheet`, after a build of the library:

```bash
dotnet build BevyCSharp
dotnet run --project build/cheatsheet -- .
```

`CheatsheetTests` holds the page to the library by the rules the generator follows, so a public
method added without its line fails the suite until the page is written again.
