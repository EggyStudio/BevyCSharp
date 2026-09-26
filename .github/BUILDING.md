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
`default_platform`, which drags in gamepad support and links Wayland at build time. Everything
graphical resolves at runtime: X11 comes through `x11-dl`, Wayland through `wayland-dlopen`, and
Vulkan through the loader. It takes several minutes to compile and produces a much larger library.

Audio is the exception, and the only system dependency in the tree. Bevy's audio sits on cpal, which
links against ALSA on Linux, so a `render` build there needs `libasound2-dev` or the equivalent for
the distribution. `build-native.sh` checks for it and names the package if it is missing, and
installs it into the container on the `--portable` path. The `headless` profile has no such
dependency and builds with nothing but a C compiler. Neither affects anyone consuming the NuGet
package, which ships the native prebuilt for each runtime identifier.

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

An ordinary push is cheap, running the test suite on Linux and stopping there. It does not build the
per-platform bridges and does not pack, since neither is used unless a package is published. Two
things change that.

**Changed the readme, the icon or the project metadata.** None of that affects the binaries, so
pushing to the default branch republishes on its own. It reuses the native binaries from the last
full build and only repacks around them, which takes a couple of minutes instead of an hour.

**Changed the code.** Put `[publish]` anywhere in a commit message. That builds all six platforms,
tests on all three operating systems, and publishes:

```bash
git commit -m "add the thing [publish]"
```

The marker is a plain substring, so it works alongside any other text and in any commit of the
push, not only the last one. Either route can also be started by hand from the Actions tab.

Versions are `MAJOR.MINOR.<commit count>`: the first two from `VersionPrefix` in
`Directory.Build.props`, the last from `git rev-list --count HEAD`. One counter that only grows,
shared by both routes so they can never disagree, and nothing stored anywhere. To move to
`0.2.x`, change `VersionPrefix` and push.

Republishing needs artifacts from a full build to still exist, and they expire after two weeks.
If none survive, the run fails and says to push a `[publish]` commit first. Only a `[publish]`
run uploads any, so the reuse step walks back past the ordinary pushes to find one that built
every platform.

---
