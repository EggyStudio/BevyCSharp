# TODO

Work outstanding on BevyCSharp, ordered roughly by how much it blocks building a game.

The ECS half of the project is finished and covered by tests: the behavior model, chunked
iteration, change detection, filters, hierarchy, scheduling, commands and input. What is thin is
the engine-facing half. Bevy is compiled in far further than it is bridged, so several items
below are bridge work over code that is already linked into the binary.

Every item names the Cargo features it needs, the `bcs_*` exports to add, and the managed surface
that goes on top. Adding an export means bumping `ABI_VERSION` in `native/bevy_csharp/src/lib.rs`
and `Native.ExpectedAbiVersion`, which is what stops a stale bridge loading against new managed
code.

## Content pipeline

### Composing what a glTF file describes

A glTF file's geometry, scenes and materials are all reachable. `LoadGltfMaterial` needs a window,
because the translation from a glTF material to the one the renderer draws with belongs to the
renderer, and a windowless run has nothing to draw. That is the arrangement rather than a
limitation: a run with no renderer is running code, not showing a model.

The division of labour this is aiming at: glTF carries the geometry, materials and animations,
because that is what Blender and every other tool exports. Composition on top of it, adding
components to what a file defines, attaching child entities, overriding what an artist set,
happens after the scene is spawned.

Bevy's own answer to that composition is Bevy Scene Notation, new in 0.19, and it is **not**
reachable from C#. `bsn!` is a compile-time Rust macro: it expands into types implementing the
`Scene` trait, so there is nothing to call at runtime, and Bevy ships no `.bsn` asset format or
loader yet, which its own documentation states is intended for a later release. Scenes written in
BSN exist only in Rust source.

What C# has instead is spawn-then-patch, through the ECS surface that already exists: spawn the
scene, walk it with `ChildrenOf`, `Add` components to the entities it produced, overwrite the
`Transform` an artist set. That reaches the same result as BSN's patching, expressed at runtime
rather than in the type system, and with the same last-write-wins rule, because both end as
component inserts.

Two things BSN has that spawn-then-patch does not, worth knowing before anyone tries to close the
gap:

- **Templates.** A BSN field takes a value that is turned into a component when the scene spawns,
  which is what lets `image: "player.png"` stand for an `AssetServer::load` and `asset_value(mesh)`
  for an `AssetServer::add`. On this side those are separate calls that already exist, so the
  convenience is missing rather than the capability.
- **Field-level patching.** Two BSN scenes naming the same component merge field by field. Adding
  a component from C# replaces the whole thing, so overriding one field means reading it, changing
  it and writing it back. A `Patch<T>` helper on the managed side would cover this, and needs no
  bridge.

Revisit when `.bsn` ships as a loadable asset: it would load and spawn through the same
`WorldAssetRoot` export a glTF scene needs, and would then be authorable without recompiling the
bridge, which is the part of BSN actually worth having on this side.

### GPU-compressed textures

PNG, JPEG, WebP, BMP and TGA decode in every build. What is missing is the compressed formats a
GPU can hold without expanding: `ktx2` is compiled in but its payload formats, BCn and ASTC and
ETC2, are not, and `CompressedImageFormatSupport` has to carry what the adapter can decode, which
a windowless app reports as nothing.

Nothing here blocks a game; it is a matter of size on disk and upload cost.

## Presentation

### UI beyond a HUD

`bevy_ui_render` is compiled in and `Ui` spawns nodes and text, sets a node's position, size,
per-side padding, margin and border, direction, justification, alignment, gaps, growth, wrapping,
bounds and overflow, draws an image inside one, scrolls what it clips, rewrites text in place, and
reports the pointer over a node asked to be interactive. That covers a HUD, a button, a menu that
lays itself out, a panel that resizes and a list that scrolls. What is left:

- **Odds and ends of flexbox.** `align_content` spreads the lines a wrapped node produces, the
  way `justify_content` spreads the children within one. `aspect_ratio` sizes the other axis from
  the one that is known. `OverflowClipMargin` clips at the padding or border box rather than at
  the content box, which matters for a scrolling list with a border.
- **Grid.** `GridPlacement` and the row and column tracks are a second layout algorithm rather
  than more fields on this one, and an inventory is what wants it.
- **Image detail.** `ImageNode.texture_atlas` names a frame by index instead of by pixel
  rectangle. The layout asset a sprite uses is the same one, so this is two fields on the image
  config rather than new machinery. A sliced image's centre and sides are stretched, since
  `SliceScaleMode::Tile` is a payload the flat config has no room for.
- **Text layout.** Justification, line breaking and bounds are `TextLayout`, unbridged, so a long
  string runs off the edge.

### 2D beyond a sprite

`bevy_sprite_render` is compiled in, `Render2d` spawns a 2D camera and attaches sprites, and a
sprite can be tinted, resized, mirrored, anchored off its centre, cut down to one rectangle of a
sheet or to one frame of an atlas layout, and drawn sliced or tiled. What a 2D game needs on top
of that:

- **Animation.** Nothing steps a sprite through its frames. That is a component holding a frame
  range and a timer, and it needs no bridge: `Frame` names a frame by number, so a behavior that
  counts and calls `SetSprite` is the whole of it. Worth writing once in the sample rather than
  in every game built on top of it.
- **Scaled fitting.** `SpriteImageMode::Scale` fits a picture inside `Size` the way a video
  player letterboxes, keeping its proportions. Its payload is another enum, which the flat config
  has no room for, the same limit the sliced modes hit.

### Gizmos beyond three shapes

`bevy_gizmos_render` is compiled in and `Gizmos` draws lines, spheres and axis markers. Calls are
queued and drained by one Bevy system each frame, because a `Gizmos` parameter cannot be held by
an exclusive system, which is what every C# system is.

What is not bridged:

- **The other shapes.** Bevy draws rectangles, circles, arcs, arrows, grids and any 2D or 3D
  primitive through `primitive_3d`. Each is another arm on the same queue.
- **Configuration.** `GizmoConfig` sets line width, whether gizmos draw on top of the scene or
  are occluded by it, and which render layers they appear on. All of it is Bevy's default.
- **Gizmo groups.** A second `GizmoConfigGroup` lets one category be toggled or styled apart from
  another, which is what a project with several kinds of debug drawing wants.

## Audio

`bevy_audio` is compiled into the render profile with Ogg Vorbis, WAV, FLAC and MP3, and `Audio`
plays, stops, pauses, sets volume per sound and over everything at once, places a sound in the
world for a nominated listener, and reads and moves the point a clip has reached. A playing sound
is an entity.

It is the one part of the bridge that takes a system library: cpal links against ALSA on Linux,
so a render build needs `libasound2-dev` or the equivalent. `build-native.sh` installs it into the
container on the `--portable` path and checks for it before a local build, naming the package per
distribution. The minimal profile is untouched and still builds with nothing but a C compiler.

What is not bridged:

- **Seeking a looping sound.** Looping is rodio's `Repeat` over a `Buffered` source, which keeps
  the decoded samples so the clip can start again and refuses to move within them, so a seek
  reports `INVALID_STATE`. Nothing on this side can work around it: music that has to resume
  where it left off is played once and restarted. Revisit if rodio makes a buffered source
  seekable.
- **Where the sound goes.** `PlaybackSettings.start_position` and `duration` play part of a clip
  without seeking afterwards, which is how one file holds several effects. `SpatialScale` is per
  sound, while `AudioPlugin::default_spatial_scale` sets it once for the whole app.
- **Ear geometry.** `SpatialListener` takes the two ear offsets separately; the bridge takes one
  gap and places them on the x axis, which is what Bevy's own constructor does.

## Simulation structure

### Sub-states and scoped entities

States carry their edges: `[OnEnter]` and `[OnExit]` run once per transition, beside `[InState]`
for every frame a state is held. Two pieces of Bevy's state machinery are still unbridged, and
both are about what a state owns rather than when it changes:

- **Sub-states.** `SubStates` exists only while a parent state holds a value, so a pause menu's
  own state disappears with the run it belongs to. Bridging it means a slot knowing its parent,
  which the fixed slot table does not express.
- **State-scoped entities.** `DespawnOnExit` despawns an entity when a state is left, which is
  what removes a level without a teardown system listing everything it spawned. It is a component
  holding a state value, so it needs the same slot plumbing rather than a new export.

### Engine messages that carry text

The window's messages are drained onto the managed bus each frame, so `ctx.Read<WindowResized>()`
reads an engine message the same way it reads one another system sent. Nine are bridged: six
whose payload is numbers, and the three file drag-and-drop messages.

Text crosses the boundary by the caller owning the buffer: the entry point writes into it and
returns the length in bytes, so a caller probes with a small buffer and calls again only when the
answer did not fit. Anything else carrying text follows that convention.

What is left:

- **Asset load failures.** `AssetEvent` reports which asset failed and why, where the handle only
  says that it did. The message is generic over the asset type, so it needs the curated-name
  treatment as well.
- **IME**, which the Input section covers.

## Rendering control

Cameras, lights and the window take their common parameters. What is left is narrower.

- **Camera**: render layers and viewports are bridged, so splitscreen and a minimap are
  expressible. Render-to-texture is not: `RenderTarget::Image` points a camera at a texture
  instead of the window, which is what a security monitor, a portal or a reflection needs, and
  what an editor viewport is built on. It needs an image created empty at a given size, which
  `Render.CreateMaterial` and the asset surface have no way to ask for.
- **Lights**: shadow bias is per light and shadow map size is settable. What is left is optical
  rather than structural: a spot light has no cookie texture to shape its beam, cascade
  configuration for a directional light's shadow distance is Bevy's default, and light probes,
  which is how a room gets ambient light that matches it, need cubemap assets the asset surface
  cannot load.
- **Window**: position, decorations, resizability, always-on-top and exclusive fullscreen are
  bridged, and the monitors are readable by size and by name. What is left is choosing a mode: a
  monitor's list of video modes is a list of structs, so exclusive fullscreen takes the monitor's
  current mode rather than offering a resolution to pick from. Multiple windows are also
  unbridged: every entry point here addresses the primary one.
- **Verification**: none of this is checked by eye. The tests assert that settings are accepted
  and that a windowless run refuses, which is what can go wrong silently; whether the picture is
  right is confirmed by running the sample, which uses a custom clear colour, a tinted sun and a
  spot light, and binds F11 to fullscreen and Tab to cursor lock.

## Input

- **Gamepad**: excluded deliberately. `bevy_gilrs` needs libudev development headers at build
  time on Linux, which the current profile avoids so the bridge builds with nothing but a C
  compiler. Adding it means either accepting that build dependency or gating the feature per
  platform.
- **IME**: `Input.Text` covers typing, including dead keys, so a name field works. What is not
  bridged is composition: Bevy's `Ime` messages report a candidate string being assembled, which
  is what a Japanese or Chinese input method needs to show underlined text before it is
  committed. The text convention the file drop messages use is what carries the candidate string;
  what is left is the messages themselves and the window's `ime_enabled` and `ime_position`.
- **Touch**: bridged as this frame's list, up to eight at once. Untested: this machine has no
  touchscreen, so only the empty case is covered. Gestures are not derived, and a touch that
  ends is reported once rather than lingering for a frame.

## ECS gaps

### Components Bevy owns

`bcs_component_id_of` resolves nine names by hand: `Transform`, `GlobalTransform`, `ChildOf`,
`Children`, `Visibility`, `InheritedVisibility`, `ViewVisibility`, `WorldInstance` and
`Interaction`. Anything else is unreachable. A general lookup is not possible through the type registry alone, since the managed
side also needs a byte-compatible mirror, so this stays a curated list that grows as mirrors are
written.

Candidates, each blocked on being mirrorable rather than on the lookup: `Name` holds a `String`,
and the render components (`Camera`, `PointLight`, `DirectionalLight`, `Mesh3d`,
`MeshMaterial3d`) hold typed asset handles or projection data that raw bytes cannot represent.
Those need named operations of the kind `Render` already provides, or name-only handles if
filtering on them is enough.

## Physics

Not a priority.

Bevy ships no physics engine, and the answer is **not** to bridge Avian or Rapier. Use
[BepuPhysics v2](https://github.com/bepu/bepuphysics2), which is C#, so the simulation lives on
the managed side and needs no bridge surface at all: nothing new crosses the ABI, no Cargo feature
is added, and the only thing that has to reach Bevy is the pose each body ends up with, which is
one `Transform` write through the API that already exists.

Two layers, so that no Bepu type reaches user code and the backend can be replaced:

- a façade: settings (gravity, timestep, substeps, solver iterations, worker threads), a body
  handle struct that forwards every operation to the world owning it, and a body kind for
  dynamic, kinematic and static.
- the backend: a physics world owning Bepu's `Simulation`, `BufferPool` and `ThreadDispatcher`,
  plus the `INarrowPhaseCallbacks` and `IPoseIntegratorCallbacks` structs Bepu requires. Worth
  splitting across partial files, one each for creation, per-body operations, queries and
  stepping, because it grows large.

Points to get right:

- **Stepping.** A physics integration usually carries its own accumulator and a guard against the
  spiral of death, where a slow frame asks for more steps than the next frame has time for.
  Neither is needed here: `[OnFixedUpdate]` means one step per fixed step, with Bevy owning the
  accumulation.
- **Write-back is a `Transform` write.** Poses go into Bevy's own `Transform`, and propagation
  carries them to `GlobalTransform` and the renderer for free.
- **It belongs in its own package**, so the core does not take the dependency. `BepuPhysics` and
  `BepuUtilities` are separate NuGet packages, published on a 2.5.0-beta line.
- **Teardown matters.** Bepu is pool-based and its `BufferPool` and `ThreadDispatcher` are
  disposable, so they have to be torn down with the app rather than left to the GC.

## Assets and scenes

- **Scene loading** works. `Scene` is a trait in 0.19 and the loadable asset is `WorldAsset`:
  `.scn` and `.scn.ron` load into one through `WorldAssetLoader`, and a glTF file's scenes are
  handles to the same type. `ctx.Ecs.SpawnScene` spawns either.
- **Shaders**: `AssetKind.Shader` loads and returns a handle that nothing consumes. Either give
  it a custom material path or remove the kind.
- **Hot reload**: `BehaviorsPlugin.ScriptsDirectory` is reserved and does nothing.

## Maintenance

- **Regression test for asset double-registration.** `init_asset` is not idempotent: it replaces
  `Assets<A>`, registers a second handle provider and duplicates the per-frame asset systems.
  Calling it on a windowed build broke all rendering with no error reported. No test caught this,
  because the failure needs a real GPU to appear. The missing check is that the render world
  receives the meshes and materials the bridge creates.
- **Packing on one machine produces a package for one platform.** Use the CI workflow, or run
  `build-native.sh` on each target, to produce a package covering all six runtime identifiers.
- **Publishing is manual by choice.** The workflow builds and uploads; the upload to nuget.org is
  done by hand.
