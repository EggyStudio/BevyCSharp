# Building from source

How to build the native bridge and the package. Using BevyCSharp needs none of this, since the
package ships a prebuilt bridge per platform; this is for working on the bridge itself.

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and
[Rust](https://rustup.rs).

```bash
build/build-native.sh          # build the native bridge (headless profile)
dotnet build                   # build the managed side
dotnet test                    # run the suite
cargo test --manifest-path native/Cargo.toml    # and the bridge's own
dotnet run --project BevyCSharp.Sample -- --frames 120 --verbose
```

Most of what the bridge does is only observable from managed code, so `dotnet test` is where
nearly all of the coverage is. The Rust tests cover what it cannot reach from there: the
convention for returning text through a caller's buffer, the guard that turns a panic into a
status code rather than an unwind into .NET, and the asset registration that has to stay inert
when it is asked twice.

Everything generated lands in `build/`, cargo's target directory, the staged per-RID artifacts,
and the packed `.nupkg`. The repository root stays clean.

```
BevyCSharp/            managed runtime library
BevyCSharp.Generator/  Roslyn source generator
BevyCSharp.Editor/     the editor, and the framework its panels are built on
BevyCSharp.Sample/     runnable example behaviors
BevyCSharp.Examples/   Bevy's examples in C#, by Bevy's names (.github/EXAMPLES.md)
BevyCSharp.Tests/      test suite, run against a real Bevy app
native/                Rust sources for the bridge
build/                 the native build scripts, and everything they generate
.github/workflows/     CI: builds every runtime identifier, then packs them together
```

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
no binary for rather than failing.

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

To ship more than one platform, run `build-native.sh --target <triple>` for each; every staged RID
slot is picked up at pack time and missing ones are skipped.

## Publishing

A push or a pull request runs the test suite on Linux and Windows and stops there. It builds no
per-platform bridge and packs nothing, and nothing a commit message says changes that.

A package is made by the **pack** workflow, run by hand from the Actions tab ("Run workflow"). It
builds the bridge for all six platforms, runs the tests on Linux and Windows, plays Courtyard and
walks the README's install in a container, and packs only once all of them pass. The package is
kept as the run's artifact, to download and upload to nuget.org by hand. Ticking its **publish**
box pushes it to nuget.org from the run instead, which needs the `NUGET_API_KEY` secret.

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
build/examples-table.py                    # .github/EXAMPLES.md and the README's count, from Bevy's metadata
```

`build/examples-table.py` reads Bevy's example metadata from the cargo registry, which holds the
bridge's Bevy once the bridge has been built, and `BevyCSharp.Examples/triage.tsv`, where each
example not yet written has its state and what it waits on. The package workflow captures every
example on the Linux bridge it built.
