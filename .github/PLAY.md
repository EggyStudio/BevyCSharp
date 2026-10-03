# Playing and shipping a game

How a game made in the editor is run from it and turned into something a player installs. The first
step exists and the rest is planned, in the order each depends on the last.

## What exists

**Play runs the project in a window of its own.** The Play tab along the bottom (`PlayTab`), `F5`
and `Project/Play` start the project through `dotnet run --project <file> -- --window`, and the same
places stop it (`EditorPlay`). The tab's Build, and `Project/Build`, run `dotnet build` instead,
which says whether the project compiles without starting it. One runs at a time. It runs the sample
unless the tab's field or the settings name another project file. The scene's toolbars carry no play
button, so the viewport holds the scene and the tools that act on it, and the game, which changes
nothing in the view, is started from the tab where its output is read. `dotnet run` builds the
project first when it is out of date, so the first press takes as long as a build. What the game
writes appears in the editor's console, each line marked `[game]`, and in the tab, which keeps the
last lines of the game or the build on their own, with the terminal's color codes taken out.
Stopping kills the whole process tree, since `dotnet run` starts the game as a child of its own, and
closing the editor stops the game with it.

A separate process rather than a mode of the editor's own world, as Godot does it, because a game
that runs inside the editor shares its state, its crashes and its frame. Its window is the one it
ships in, sized and framed as a player sees it.

**A game's window can take the desktop's own title bar on GNOME** (`Config.DesktopTitleBar`, which
the sample sets). Under Wayland, GNOME leaves an app to draw its own title bar, and winit draws one
that imitates an older GNOME rather than the libadwaita one every other window has. GNOME draws its
own only for an X11 window, so with the option set on a GNOME Wayland session that also offers X11,
the bridge takes `WAYLAND_DISPLAY` out of its environment before winit starts, and the window opens
through XWayland. The cost is that an XWayland window is drawn at a whole scale and stretched on a
display at a fractional one, which is softer. So the option is off unless asked for, and it does
nothing on any other desktop or platform.

## 1. Playing the scene being edited

What is played is the project's own `Main`, which builds its own world. Godot plays the scene open
in the editor, so a change made there is seen without being written into code.

- **A player.** A small entry point the library provides (`BevyCSharp.Player`, or a `--play <file>`
  argument any app built on the library understands) that starts an app, loads a scene file
  ([SCENES.md](SCENES.md)) and spawns what it holds, and compiles the scripts under `assets/scripts` through the
  script host the editor uses. The script host lives in the editor, so it moves to a package of its
  own that the player and the editor both reference, and a shipped game leaves it out and compiles
  its scripts ahead of time.
- **Saving before playing.** Play writes the edited scene to a file under `build/play/` rather than
  over the scene's own file, so playing never changes what is saved, and passes that file to the
  player.
- **Two ways to play,** as Godot's "Run Project" and "Run Current Scene": the project's `Main`, as
  it is now, and the scene being edited through the player. The tab offers both, and `F5` takes
  the second.

## 2. Seeing the running game from the editor

Godot's remote scene tree shows the running game's world in the editor, beside the edited one. The
`./bcs` socket already answers `entity.list`, `entity.get` and `entity.set` from a running app, so
the editor can be the client.

- The game is started with `config.Serve` on, and the editor reads the session file it writes to
  find it, as `./bcs` does, by process id.
- A Remote view in the world panel lists the running game's entities by asking `entity.list` a few
  times a second, and the details show `entity.get` for the one picked. A field changed there is an
  `entity.set`, which lasts until the game stops and is not recorded in the editor's history.
- Pausing and stepping the game need commands of their own (`app.pause`, `app.step`), which are
  `[Command]` methods over Bevy's `Time<Virtual>`.
- The game's picture can be shown in a tab through `shot`, but a capture a frame is too slow for
  anything past a thumbnail, so the game keeps its own window.

## 3. Exporting from the Play tab

The Play tab grows a section that turns the project into something a player installs, where Play
and Build only run and compile it on this machine.

- **Targets.** One row per runtime identifier (`linux-x64`, `linux-arm64`, `win-x64`, `win-arm64`,
  `osx-x64`, `osx-arm64`), each with whether a bridge for it is present, since a bridge is built per
  platform and packing on one machine produces a package for one platform (see `TODO.md`).
- **What a build does.**
  1. `dotnet publish -c Release -r <rid> --self-contained` into `build/export/<rid>/`, with
     `PublishTrimmed`, since nothing in the library reflects at runtime. Native AOT is a later step,
     because the script host compiles at runtime and has to be left out of a shipped game first.
  2. The bridge for that identifier, built with the profile the game needs (`render`, never
     `editor`), copied beside the executable.
  3. The assets, either as a folder beside the executable or embedded (below).
- **Output.** Each step's output goes to the tab as the game's does to the console, with the
  failing step named. A finished build shows its folder and its size and offers to run it.
- **Settings per target,** saved with the project rather than the editor: the name, the icon,
  the window's title, whether to embed assets, and `DesktopTitleBar`.

The same build is a `[Command]` (`project.build <rid>`), so `./bcs` and CI build a game the way the
tab does.

## 4. Embedding assets

A shipped game carries its assets either as a folder beside it, which a player can open and change,
or inside the binary, which is one file to ship and nothing to lose.

`bevy_embedded_assets` 0.16 targets Bevy 0.19, which the bridge uses. It replaces the default asset
source with one that reads from bytes compiled into the binary, so every `asset_server.load` finds
the embedded file with no change to the paths. The catch is where it embeds from. It includes a
directory at the time the Rust crate is compiled, and the bridge is one prebuilt library shared by
every game (and by the NuGet package), so embedding a game's assets there means building the bridge
once per game.

That fits a build step that already builds the bridge per target, so the plan takes it in two parts:

- **Per game, through the bridge.** A `--embed <dir>` option on `build-native.sh` and the export section
  turns on a bridge feature that adds `EmbeddedAssetPlugin` in `ReplaceDefault` mode over that
  directory. It is the smallest change, and each game pays a bridge build for it.
- **Shared bridge, managed bytes.** The same reader serves the managed files as well (scenes, data
  assets and the rest in [SCENES.md](SCENES.md)), so one embedded copy of the assets feeds both
  sides. An asset source the bridge registers whose reader asks the
  managed side for a path's bytes, which C# serves from embedded resources in the game's assembly
  (`<EmbeddedResource Include="assets/**" />`). The bridge stays shared, and a single-file publish
  carries everything. It needs a new export pair (a reader callback and a free), so it is the
  second step.

The first is enough to ship; the second removes the per-game bridge build.

## 5. Saving settings and progress

`bevy-persistent` 0.11 also targets Bevy 0.19. It wraps a resource in `Persistent<R>`, which loads
it from a file under the platform's data or config directory, writes it back when asked, and falls
back to a default when the file is missing or unreadable, in JSON, TOML, RON, YAML, INI or bincode.

The resource it persists is a Rust type that implements `serde`, and a game's settings and save
data are C# types the bridge cannot name. So it would fit only what the bridge owns and C# does
not, and the one such thing, the window's place, needs so little from the bridge that it is kept
on the managed side as well:

- **The window's size, place and monitor.** A game reopening where it was closed is expected on
  the desktop. `bevy-persistent-windows` does this over `bevy-persistent`, but its 0.9 release is
  on Bevy 0.17. Built instead on the managed side, which needs no crate: `Config.RememberWindow`
  keeps a `WindowPlace` in `user://window.json` through `Persistent<T>`, read before the window
  opens and passed to the bridge as where to open it, and written when `Window.Place`
  (`bcs_window_place`, ABI 160) says the window has moved, been resized or been maximized. A
  maximized window keeps the size it goes back to. Wayland never tells an app where its window is,
  so there it keeps the size and not the place. Not built: the monitor, which matters on Wayland,
  where it is the one part of the place a compositor takes.

For what C# owns, the same design on the managed side:

- **`Persistent<T>`** in the library, with the same shape (a name, a format, a path under the
  platform's directory, `Get`, `Update`, `Persist`, `Revert`), over `System.Text.Json` with a
  source-generated context, so it stays reflection-free and survives trimming and AOT. The directory
  is `$XDG_DATA_HOME` or `~/.local/share` on Linux, `%APPDATA%` on Windows and
  `~/Library/Application Support` on macOS, under the game's name. Built in
  `BevyCSharp/Scenes/Persistent.cs`, as `Persistent<T>` (`Value`, `Set`, `Update`, `Persist`,
  `Revert`, `Reset`, and `Problem` for a file that would not read) over `UserData`, whose name is
  `Config.GameName` or the title.
- **Writes are atomic,** to a file beside the target and then renamed over it, so a crash while
  saving leaves the last save rather than half of one.
- **Save games are built on it** as a diff over the scenes they name, which [SCENES.md](SCENES.md)
  sets out with the rest of the scene format.
- **`EditorSettings` is kept in it,** since it is the same thing with a format of its own, so a
  game's settings screen and the editor's store their values the same way. Built:
  `EditorProject` writes the settings to `user://settings.json` as a `Persistent<T>`, and reads a
  `settings.txt` left in the assets by an older build when there is no such file yet.

## Order

1. Per-game embedding (`--embed`), since it is a feature flag and a plugin.
2. The window's place over `bevy-persistent`, and `EditorSettings` moved onto `Persistent<T>`.
3. Export in the Play tab over `dotnet publish`, with embedding as an option.
4. Scene files, as [SCENES.md](SCENES.md) orders them, which the player loads.
5. The player and playing the edited scene.
6. The remote view.
7. Embedding through a shared bridge.
