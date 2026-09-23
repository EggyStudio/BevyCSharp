# TODO

Work outstanding on BevyCSharp, in the order it blocks building a game: getting content in, drawing
it, putting an interface on it, making it behave, reaching the devices a player has, and shipping
the result.

The ECS half of the project is finished and covered by tests: the behavior model, chunked
iteration, change detection, filters, hierarchy, scheduling, commands, input and states. What is
thin is the engine-facing half. Bevy is compiled in far further than it is bridged, so most of what
follows is bridge work over code already linked into the binary rather than a dependency to add.

An item says what exists, what is missing, and what the missing part needs. Adding an export means
bumping `ABI_VERSION` in `native/bevy_csharp/src/lib.rs` and `Native.ExpectedAbiVersion`, which is
what stops a stale bridge loading against new managed code.

## Content

### Composing what a glTF file describes

A glTF file's geometry, scenes and materials load, and `ctx.Ecs.SpawnScene` spawns either a glTF
scene or a `.scn`/`.scn.ron` file, both of which are a `WorldAsset` in 0.19. `LoadGltfMaterial`
needs a window, because translating a glTF material into one the renderer draws with belongs to the
renderer, which is the arrangement rather than a limitation.

The division of labour this aims at: glTF carries geometry, materials and animations, because that
is what Blender and every other tool exports, and composition happens after the scene is spawned by
adding components to what the file defined.

Bevy's own answer to composition is Bevy Scene Notation, and it is **not** reachable from C#.
`bsn!` is a compile-time Rust macro expanding into types that implement the `Scene` trait, so there
is nothing to call at runtime, and Bevy ships no `.bsn` asset format or loader yet. Scenes written
in BSN exist only in Rust source.

What C# has instead is spawn-then-patch through the ECS surface: spawn the scene, walk it with
`ChildrenOf`, add components to the entities it produced, overwrite the `Transform` an artist set.
That reaches the same result with the same last-write-wins rule, because both end as component
inserts. Two things BSN has that it does not:

- **Templates.** A BSN field takes a value turned into a component when the scene spawns, which is
  what lets `image: "player.png"` stand for an `AssetServer::load`. On this side those are separate
  calls that already exist, so what is missing is the convenience rather than the capability.
- **Patching is per component, not per scene.** `ctx.Ecs.Patch` and `PatchTree` change the fields
  they name and leave the rest, which is what BSN's field-level merge does between two scenes. What
  has no equivalent is stating the patch as data rather than as code, so a second file can overlay
  the first without anything being compiled.

Revisit when `.bsn` ships as a loadable asset. It would load through the same path a glTF scene
does and be authorable without recompiling the bridge, which is the part worth having here.

### Textures and shaders

PNG, JPEG, WebP, BMP and TGA decode in every build. The GPU-compressed formats do not: `ktx2` is
compiled in but its payload formats, BCn and ASTC and ETC2, are not, and
`CompressedImageFormatSupport` has to carry what the adapter can decode, which a windowless app
reports as nothing. Nothing here blocks a game; it is size on disk and upload cost.

`AssetKind.Shader` loads and returns a handle nothing consumes. Either give it a custom material
path or remove the kind.

### Scripts a game can load

`BehaviorsPlugin.ScriptsDirectory` is reserved and does nothing. The engine half exists,
`App.EnableDynamicSystems` and `App.RemoveSystemsBySource`, and `BevyCSharp.Editor` drives Roslyn
through them. What is left is deciding whether the core should carry a compiler at all, which is
what a game loading a script without the editor would take.

## Rendering

### Cameras, lights and the window

Cameras, lights and the window take their common parameters, and a camera tonemaps, dithers,
multisamples, antialiases, sharpens, blooms and scatters a sky over what it draws, pulls focus,
smears what moves, fringes and warps its lens, darkens its corners, finds its own exposure, meters
at an exposure it is given, and grades the finished picture in three tonal ranges.
Render layers, viewports and render-to-texture are bridged, so splitscreen, a minimap, a portal and
a run that draws with no window at all are expressible, and a cubemap can be drawn behind the scene
as a skybox or used to light the scene through the atmosphere. `bevy_post_process` and
`bevy_anti_alias` are compiled into the render profile, so most of what is left is bridge work over
code already in the binary.

- **A lens is described twice.** `Render.SetLensExposure` takes an aperture, a shutter speed and a
  sensitivity, and `Render.SetDepthOfField` takes an aperture and a focal length of its own.
  `PhysicalCameraParameters` is one struct behind both, so a camera could be written down once and
  have the exposure and the blur read from it.
- **A prebaked environment map.** `Render.SetImageLighting` filters a cubemap on the GPU every
  time the app starts, which costs a moment of startup and needs the faces square and a power of
  two. `EnvironmentMapLight` takes the diffuse and specular cubemaps a tool baked earlier instead,
  which is what a shipped game wants and what a large environment cannot afford to redo.
- **A cubemap of anything else.** The reinterpretation is a column of six faces stacked
  vertically, which is what a file holds. A cubemap rendered into, which is what a reflection probe
  or a point light's shadow would want, needs an image created with six layers rather than one
  reinterpreted after loading.
- **The rest of the sky.** Earth's air is bridged; Mars is the other medium Bevy ships and its dust
  phase comes from a texture. `ScatteringMedium::new` takes arbitrary scattering terms, which is
  what an alien planet wants and what a flat config cannot describe. The LUT sizes and sample
  counts on `AtmosphereSettings` are Bevy's defaults, and they are the quality knob.
- **DLSS.** `bevy_anti_alias` carries it behind a `dlss` feature pulling in `dlss_wgpu`, which has
  licensing terms of its own and runs only on an NVIDIA RTX card through Vulkan on Windows or
  Linux. It also wants a `DlssProjectId` inserted before `DefaultPlugins` and a runtime check of
  whether the machine supports it, so it is a fourth arm on `AntiAlias` that most machines have to
  be told they cannot have.
- **Lights.** Shadow bias, map size, cascades and a spot light's cookie are all settable. What is
  left is the experimental half of Bevy's own lighting: soft shadows sit behind the
  `experimental_pbr_pcss` feature, and contact shadows need a camera component to go with the flag
  on the light.
- **One window.** Position, decorations, resizability, always-on-top and exclusive fullscreen are
  bridged, the monitors are readable by size, name and video mode, and `Window.SetVideoMode` takes
  the screen over at one of them. What is left is more than one window, since every entry point
  addresses the primary one.
- **A picture is always RGBA and always eight bits a channel.** `Render.CreateImage` takes bytes
  and `Render.BeginCapture` hands them back, both in that one format. A heightmap of floats, a
  single-channel mask or a compressed texture would each need the format to be a parameter rather
  than an assumption.

### Gizmos

`Gizmos` draws lines, fading lines, arrows, spheres, circles, arcs, rectangles, boxes, capsules,
cones, cylinders, tori, grids and axis markers, in either of two groups: one the scene can hide and
one it cannot. Calls are queued
and drained by one Bevy system each frame, because a `Gizmos` parameter cannot be held by an
exclusive system, which is what every C# system is. `Gizmos.Configure` sets line width, render
layers and whether anything is drawn at all.

- **The rest of the primitives.** `primitive_3d` draws any shape in `bevy_math`. What the bridge
  does not reach are the ones described by a list of points rather than by numbers, which is a
  triangle, a polyline and a tetrahedron. All three are runs of lines, so `Gizmos.Lines` draws them
  today at the cost of naming the corners; a call of their own would only save that.
- **Two groups, not many.** `Gizmos.Configure` takes a `GizmoGroup`, so the shapes the scene can
  hide are settable apart from the ones it cannot, which is the split a game usually wants. A third
  category needs a third `GizmoConfigGroup`, and a group is a Rust type rather than a value, so it
  is added where the two are and rebuilt.
- **Only lines batch.** `Gizmos.Lines` hands a whole run over at once, which is what the editor's
  fading grid uses and what a wireframe or a path wants. Every other shape still crosses the
  boundary on its own, and a scene drawing thousands of spheres or boxes a frame would want the
  same treatment.

### 2D

`Render2d` spawns a 2D camera and attaches sprites, and a sprite can be tinted, resized, mirrored,
anchored off its centre, cut down to one rectangle of a sheet or one frame of an atlas layout, and
drawn sliced, tiled or fitted inside its size the way a video player letterboxes.

- **Animation is a sample, not a feature.** `SpriteAnimation` in `BevyCSharp.Sample` steps a sheet
  and is there to be copied. Frame events, a queue of clips and animation driven by the state
  machine are what a game adds, and each would be the wrong shape shipped in the library while
  still costing a query a frame.

## Interface

### Layout and text

`Ui` spawns nodes and text, sets a node's position, size, per-side padding, margin and border,
direction, justification, alignment, gaps, growth, wrapping, bounds, overflow, aspect ratio, where
its clipping falls and which camera draws it, draws an image inside one, scrolls what it clips,
draws one frame of a sheet rather than a whole picture, breaks and aligns a run of text in a font of
its own at a spacing and smoothing it chooses, casts a shadow behind it, rewrites it in place, and
reports the pointer over a node asked to be interactive. Naming the camera is what lets a screen be drawn with no
window, so the layout is asserted on pixel by pixel in an offscreen run like the rest of the
renderer. That covers a HUD, a button,
a menu that lays itself out, a panel that resizes, a list that scrolls and a paragraph that fits its
box.

- **Scrollbar width.** `scrollbar_width` is the one `Node` field left unbridged, and it reserves
  room at the edge of a scrolling node for a scrollbar. Nothing here draws one, so the room would
  be a gap.
- **Fonts by family name.** Excluded deliberately, the way gamepads are. `FontSource` can name
  `SansSerif`, `Monospace` or the system interface font, but Bevy resolves those through
  `system_font_discovery`, whose Linux backend links against fontconfig at build time. Without the
  feature such text renders nothing and logs why, so the bridge offers a loaded font or Bevy's own
  and nothing in between. Revisit if the feature becomes dlopen-based, as the Wayland backend is.

### Dear ImGui

The editor runs on Dear ImGui. The C# side owns the context through `Twizzle.ImGui-Bundle.NET`,
builds the windows the way ImGui is built anywhere, and hands the triangles to `bcs_imgui_frame`,
which draws them over what the cameras drew. Immediate mode is the point: an inspector is a call per
field per frame, so there is no widget tree to keep in step with the world.

- **The bundle is ImGui 1.91.5.** From 1.92 Dear ImGui embeds a scalable version of its classic
  font, which is what the native theme should use rather than the ProggyClean bitmap it has. It
  arrives when the bundle updates and needs no change here.
- **No icon font.** The icons are PNGs the editor ships, loaded through the asset server and drawn
  with `ImGui.Image`. More can be rasterised from SVG when they are wanted.
- **IME is not forwarded.** Keys, characters, the pointer and the wheel are.
- **The interface is redrawn every frame**, which is what immediate mode means. At editor scale
  that is a few thousand triangles and one buffer write; if it ever matters, a frame where nothing
  moved can be drawn again instead of rebuilt.

### The editor

`BevyCSharp.Editor` runs. One panel on the right holds the world with a picture per row above the
details of whatever is selected, the tools float in the scene's corners, and the rest is behind a
hamburger whose contents are a table of paths. A component is a card that opens and shuts, a field
is a row drawn as its kind and attributes say, and a component with nothing to show is a tag. The
bottom left is a strip of tabs opening into a card: the console, the asset browser, the settings and
the style. Gizmos draw the selection, its handles, the ground and the camera's orientation, and a
drag on a handle moves, turns or stretches what is selected. [EDITOR.md](EDITOR.md) has the design
language.

- **Half of the world is saved.** `assets/world.json` keeps every named entity's name and every
  component with a schema, which is what the editor can change. What it cannot write is the
  engine's own components: a mesh handle, a material, a camera's projection.
  `bevy_world_serialization` would write exactly those and can see no C# component at all, because
  those are bytes registered at runtime with no Rust type behind them. A world asset worth the name
  is both files, or one format holding both halves.
- **An image tile shows itself; nothing else does.** A picture is drawn from its path, fitted to
  the tile by the size `ImGuiTextures.SizeOf` reports. What a model or a sound looks like needs a
  camera pointed at a render target, which every piece exists for. `Render.CreateTarget` and
  `Render.SetCameraTarget` point a camera at an image and `ImGuiTextures.Of` hands that image to a
  draw call, so a model thumbnail, a material preview and an orientation widget drawn as a small
  scene are all editor work rather than bridge work.
- **A field can hold an asset, and the engine's own cannot.** A game's component holding an
  `AssetHandle` is drawn by name, and pressing it offers the files under the asset root that suit
  it. The mesh and the material on an entity are Rust components with no schema, so the editor
  cannot point an entity at a different mesh.
- **The hierarchy names what it can see and the stats panel counts it.** Both go through
  `EditorKinds`, so a camera in the tree and a camera in the count are one question asked once.
  Neither can see a component the bridge does not name, so an entity whose components are all
  engine-side reads as plain. Naming more of them is a bridge job.
- **The inspector draws a field as one arm of one switch**, told how by the attributes the
  generator carried through, folds included. What is left is editing several things at once, where
  the panel shows the last one picked with a count beside it rather than what they agree and
  disagree about.
- **A selection is remembered by name**, so what a reloaded script respawns is found again. All of
  them or none, since half a selection coming back is worse than none. Two entities sharing a name
  still resolve to the first.
- **Undo covers what can be reversed exactly**: a field edited in the inspector, a rename, a new
  entity, something hidden with its eye, and a component put on or taken off, which keeps what it
  held. Despawning is deliberately not recorded, because an entity's mesh and material are
  engine-side components with no mirror and what came back would be a name with nothing to draw.
  That is the world file's gap, and closing one closes both.
- **Settings are the editor's, not the project's.** `EditorSettings` saves to `assets/settings.txt`
  beside the layout, and everything on it belongs to this editor build. A project setting worth the
  name (a startup scene, a physics step, a build target) needs somewhere to live that is part of
  the project, which is the world file's gap again.
- **The scene is the camera's viewport rather than a texture.** Docked, `Render.SetViewport` gives
  the camera the rectangle the panels left. A texture would make the scene a panel of its own,
  dockable and tabbable, which is what a second view wants.
- **A theme is a file, and only the running build has it.** `assets/theme.txt` is written beside the
  binary, so a look dialled in has to be copied back into the project by hand to be shipped.
- **Every row is drawn every frame.** A list is a call per row inside a scrolling region, which is
  what immediate mode means. At editor scale that is nothing; a list of ten thousand entities would
  want ImGui's own clipper, which asks only for the rows on screen and is a change to the loops
  rather than to what they draw.

## Simulation

### States

States carry their edges and their entities: `[OnEnter]` and `[OnExit]` run once per transition,
`[InState]` every frame a state is held, `DespawnOnExit` ties an entity's life to a value, and
`[SubStateOf]` declares a state that exists only while another holds a value, so a pause disappears
with the run it belongs to, and `[ComputedFrom]` declares one whose value follows from another's.
The bridge sets aside a fixed block of each per state slot, because both name their source as an
associated type and the types exist when the crate is built.

- **Sub-states of sub-states.** A state carries two sub-states, and a third is refused because
  every axis is given the same fixed block of them when the bridge is built. A chain is the harder
  one, since a sub-state of a sub-state has nowhere to live in a layout that is one block per
  axis.
- **A computed state reads one source, and by a table.** `app.AddComputedState` maps values of one
  state onto values of another, which covers "the interface is up on these three screens". Bevy's
  `compute` is an arbitrary function over a `StateSet`, so deriving from two states at once, or by
  any rule a table cannot state, needs a way to call back into managed code from a static function
  with no world in hand.
- **A sub-state over more than one parent.** `SourceStates` can be a tuple, so a state can exist
  only while two others hold values. The pairing is per parent slot, so this needs a different
  arrangement rather than another pair.

### Engine messages

The window's messages are drained onto the managed bus each frame, so `ctx.Read<WindowResized>()`
reads an engine message the way it reads one another system sent. Ten are bridged: six whose
payload is numbers, the three file drag-and-drop messages, and `AssetLoadFailed`, which carries the
path, the reason and the kind for an asset that would not load. Text crosses the boundary by the caller
owning the buffer, so a caller probes with a small buffer and calls again only when the answer did
not fit.

- **Only the asset types the bridge loads are named.** `AssetLoadFailed.Kind` matches the type id
  against the same curated list `AssetServer.Load` takes, so an asset type the engine loaded for
  itself as part of something else answers an empty string. A general answer needs Bevy's registry
  to carry every asset type's name, which it does not.

### Components Bevy owns

`bcs_component_id_of` resolves nine names by hand: `Transform`, `GlobalTransform`, `ChildOf`,
`Children`, `Visibility`, `InheritedVisibility`, `ViewVisibility`, `WorldInstance` and
`Interaction`. Anything else is unreachable. A general lookup is not possible through the type
registry alone, since the managed side also needs a byte-compatible mirror, so this stays a curated
list that grows as mirrors are written.

Each candidate is blocked on being mirrorable rather than on the lookup. `Name` holds a `String`,
and the render components (`Camera`, `PointLight`, `DirectionalLight`, `Mesh3d`, `MeshMaterial3d`)
hold typed asset handles or projection data that raw bytes cannot represent. Those need named
operations of the kind `Render` provides, or name-only handles if filtering on them is enough.

### Physics

Not a priority.

Bevy ships no physics engine, and the answer is **not** to bridge Avian or Rapier. Use
[BepuPhysics v2](https://github.com/bepu/bepuphysics2), which is C#, so the simulation lives on the
managed side and needs no bridge surface at all. Nothing new crosses the ABI, no Cargo feature is
added, and the only thing that has to reach Bevy is the pose each body ends up with, which is one
`Transform` write through the API that already exists.

Two layers, so that no Bepu type reaches user code and the backend can be replaced. A façade
carrying settings, a body handle that forwards to the world owning it, and a body kind for dynamic,
kinematic and static; and a backend owning Bepu's `Simulation`, `BufferPool` and `ThreadDispatcher`
plus the callback structs it requires.

- **Stepping.** A physics integration usually carries its own accumulator and a guard against the
  spiral of death. Neither is needed here, because `[OnFixedUpdate]` means one step per fixed step
  with Bevy owning the accumulation.
- **Write-back is a `Transform` write.** Propagation carries it to `GlobalTransform` and the
  renderer for free.
- **It belongs in its own package**, so the core does not take the dependency. `BepuPhysics` and
  `BepuUtilities` are separate NuGet packages on a 2.5.0-beta line.
- **Teardown matters.** Bepu is pool-based, and its `BufferPool` and `ThreadDispatcher` are
  disposable, so they have to be torn down with the app rather than left to the GC.

## Platform

### Input

- **Gamepad.** Excluded deliberately. `bevy_gilrs` needs libudev development headers at build time
  on Linux, which the current profile avoids so the bridge builds with nothing but a C compiler.
  Adding it means accepting that build dependency or gating the feature per platform.
- **IME.** `Input.Text` covers typing, including dead keys, so a name field works. What is missing
  is composition: Bevy's `Ime` messages report a candidate string being assembled, which is what a
  Japanese or Chinese input method needs to show underlined text before it is committed. The text
  convention the file drop messages use is what carries the candidate string; what is left is the
  messages themselves and the window's `ime_enabled` and `ime_position`.
- **A synthetic pointer needs a window.** `SyntheticInput` writes window messages, so a headless
  or offscreen run has nowhere to send one and says so. That is the one thing `bcs` cannot drive in
  an offscreen editor, where the interface is drawn and laid out but cannot be clicked. Feeding the
  interface's own queue without a window would cover the panels and still leave picking and the
  camera untouched, which is half the path a hand takes.
- **Touch.** Bridged as this frame's list, up to eight at once, and untested, because this machine
  has no touchscreen and only the empty case is covered. Gestures are not derived, and a touch that
  ends is reported once rather than lingering for a frame.

### Audio

`bevy_audio` is compiled into the render profile with Ogg Vorbis, WAV, FLAC and MP3, and `Audio`
plays, stops, pauses, sets volume per sound and over everything at once, plays a window out of a
clip rather than all of it, places a sound in the world for a nominated listener, and reads and
moves the point a clip has reached. A playing sound is an
entity.

It is the one part of the bridge that takes a system library: cpal links against ALSA on Linux, so
a render build needs `libasound2-dev` or the equivalent. `build-native.sh` installs it into the
container on the portable path and checks for it before a local build, naming the package per
distribution. The minimal profile still builds with nothing but a C compiler.

- **Seeking a looping sound.** Looping is rodio's `Repeat` over a `Buffered` source, which keeps
  the decoded samples so the clip can start again and refuses to move within them, so a seek
  reports `INVALID_STATE`. Nothing here can work around it, so music that has to resume where it
  left off is played once and restarted. Revisit if rodio makes a buffered source seekable.
- **No mixer.** Every sound carries its own volume, so a music and an effects slider are a
  multiplication the game does itself before it plays anything. Bevy has no bus to hang them off
  either, so a mixer would be a managed layer over the volumes rather than a bridge.

## Project

### Testing

- **What reaches the render world is checked for one shape.** Every registration goes through
  `assets::init_asset_once`, and the crate's own tests pin both halves of that guard. `DrawnTests`
  covers the step after by drawing a primitive mesh with an unlit material into an offscreen target
  and asserting on the pixels, so a mesh or material that never reaches the render world fails a
  test rather than producing an empty picture. It needs a GPU, so it skips on the headless bridge
  the test workflow builds. Unchecked that way is everything with more than one moving part: a lit
  surface, a glTF file's own materials, a sprite, and text.
- **The lens effects have not been compared.** Whether a whole scene is right is confirmed by
  running the sample, and an effect is worth checking against a second run with it turned off.
  Bloom was confirmed that way, since a halo is obvious beside the same frame without one and easy
  to imagine without the comparison. The lens effects have not been through it.

### Build and release

- **The Rust build is not cached in CI.** `Swatinem/rust-cache` is configured with
  `workspaces: native`, so it caches `native/target`, while `build-native.sh` writes to
  `build/target`. The bridge is rebuilt from nothing on every run. The crate's own tests do use
  `native/target` and are cached. Either point the script at the cached directory or the cache at
  the script's.
- **Packing on one machine produces a package for one platform.** Use the CI workflow, or run
  `build-native.sh` on each target, to produce a package covering all six runtime identifiers.
- **Publishing is manual by choice.** The workflow builds and uploads; the upload to nuget.org is
  done by hand.
