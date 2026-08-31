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

Nothing here can be worked around from C#. Without it a project is limited to four primitive
shapes in flat colors.

### Materials and scenes from glTF

Geometry is done: `bevy_gltf` is compiled into the render profile and
`AssetServer.LoadGltfMesh(path, mesh, primitive)` returns an ordinary mesh handle. The API
question the old entry raised is settled, and 0.19 settled it: parts are addressed individually,
by the label Bevy's own asset paths already understand (`ship.gltf#Mesh0/Primitive0`), so no
export was needed at all.

What is still missing from a glTF file:

- **Materials.** `ship.gltf#Material0` loads as a `GltfMaterial`, which describes a material
  rather than being one the renderer draws with. `standard_material_from_gltf_material` does the
  translation, but it lives in `bevy_pbr`'s private `gltf` module and is registered as an
  extension handler by `PbrPlugin`, which a headless-configured app does not install. Either add
  `PbrPlugin` to that path and check the handler produces a labelled `StandardMaterial`, or copy
  the conversion field by field on this side.
- **Scenes.** A glTF scene is a `WorldAsset` in 0.19 rather than the old `Scene` asset, so
  spawning a file's whole hierarchy is its own mechanism and its own bridge.
- **Textures** are the next entry below, and are the reason materials matter.

### More texture formats

`MaterialSettings` binds five maps and PNG and JPEG decode in every build. What is not compiled
in: WebP, and the GPU-compressed formats beyond `ktx2`. Compressed textures also need
`CompressedImageFormatSupport` to carry what the adapter can actually decode, which a windowless
app currently reports as nothing.

Nothing here blocks a game; it is a matter of size on disk and upload cost.

### Texture sampling

A texture is bound and drawn with Bevy's default sampler. Repeat and clamp, filtering, and
anisotropy are not reachable, so a tiling floor cannot be told to tile from C#: the texture is
stretched over whatever UVs the mesh carries. This is `ImagePlugin`'s default sampler and a
per-image override, rather than a material setting.

## Presentation

### Text and UI

`bevy_ui`, `bevy_text` and `default_font` are compiled in, so the types are linked, but the
render halves are not: Bevy 0.19 splits `bevy_ui_render`, `bevy_sprite_render` and
`bevy_gizmos_render` into separate features and the current profile omits all three. Nothing
draws even after a bridge exists.

- features: `bevy/bevy_ui_render`, and `bevy/bevy_ui_widgets` for the built-in widgets
- exports: spawn a UI node, set its layout and style, spawn and update a text node
- managed: enough to build a HUD and a menu
- note: this is the largest single gap for a finished game. Score, health, menus and settings all
  need it.

### 2D rendering

`bevy_sprite` is compiled without `bevy_sprite_render`, so there is no 2D path at all. A 2D game
is not possible today even though the crate is linked.

- features: `bevy/bevy_sprite_render`
- exports: spawn a 2D camera, attach a sprite to an entity, set its atlas rectangle
- managed: a `Render2d` surface alongside `Render`

### Gizmos

Debug drawing rather than a game feature. Once `bevy_gizmos_render` is on, a line, a sphere and
an axis marker cover most uses.

## Audio

`bevy_audio` is not compiled in, so there is no audio support.

- features: `bevy/bevy_audio`, plus formats from `vorbis`, `wav`, `mp3` and `flac`
- exports: load an audio source, play it, stop it, set volume and looping
- managed: `AssetKind.Audio` and an `Audio` surface

## Simulation structure

### State transitions

States themselves are bridged: `App.AddState`, `ctx.State`/`ctx.SetState` and `[InState]` scope a
system to a value. What is missing is the other half, the schedules Bevy runs on the edges.

- exports: register a system into `OnEnter(state)` and `OnExit(state)`
- managed: `[OnEnter(Screen.Playing)]` and `[OnExit(...)]`, which need a dimension beside `Stage`
  in the generator's method model, since an edge is not a stage
- note: today the same thing is expressed by watching for the change in a system scoped with
  `[InState]`, which works but runs every frame rather than once on the edge.

### Reading Bevy's own messages

C#-to-C# broadcast is covered: `ctx.Send` and `ctx.Read` carry a message from one system to any
number of others. What is not bridged is Bevy's own message stream, so nothing the engine reports
is reachable: window resizes, files dropped on the window, asset load failures, and whatever a
Rust plugin sends.

- exports: drain a named message type into a byte buffer, the way `bcs_component_id_of` resolves
  a curated list of names rather than anything the type registry holds
- managed: mirrors for the payloads worth reading, and a way to feed them into the same
  `ctx.Read` the managed bus uses, so a reader does not care which side sent it
- note: the managed bus swaps once a frame rather than giving each reader a cursor, so a message
  is readable the frame after it was sent. Matching Bevy's cursor semantics would need a stable
  identity per reader, which a C# system does not have today.

## Rendering control

Cameras, lights and the window take their common parameters now. What is left is narrower.

- **Camera**: no render layers and no viewport, so splitscreen and render-to-texture are out of
  reach. Both are a component and a rectangle rather than new machinery.
- **Lights**: no light probes, no per-light shadow bias, and the spot light has no cookie
  texture. Shadow map resolution is Bevy's default and unreachable.
- **Window**: exclusive fullscreen is not offered, because it needs a video mode to be chosen
  from the monitor's list, which needs the monitor list bridged first. Borderless covers what a
  desktop game wants. Window position, decorations and always-on-top are unbridged.
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
  committed. It also needs the window's `ime_enabled` and `ime_position` set.
- **Touch**: bridged as this frame's list, up to eight at once. Untested: this machine has no
  touchscreen, so only the empty case is covered. Gestures are not derived, and a touch that
  ends is reported once rather than lingering for a frame.

## ECS gaps

### Components Bevy owns

`bcs_component_id_of` resolves seven names by hand: `Transform`, `GlobalTransform`, `ChildOf`,
`Children`, `Visibility`, `InheritedVisibility` and `ViewVisibility`. Anything else is
unreachable. A general lookup is not possible through the type registry alone, since the managed
side also needs a byte-compatible mirror, so this stays a curated list that grows as mirrors are
written.

Candidates, each blocked on being mirrorable rather than on the lookup: `Name` holds a `String`,
and the render components (`Camera`, `PointLight`, `DirectionalLight`, `Mesh3d`,
`MeshMaterial3d`) hold typed asset handles or projection data that raw bytes cannot represent.
Those need named operations of the kind `Render` already provides, or name-only handles if
filtering on them is enough.

## Physics

Not a priority. Recorded here so the approach is settled when it does come up.

Bevy ships no physics engine, and the answer is **not** to bridge Avian or Rapier. Use
[BepuPhysics v2](https://github.com/bepu/bepuphysics2), which is C#, so the simulation lives on
the managed side and needs no bridge surface at all: nothing new crosses the ABI, no Cargo feature
is added, and the only thing that has to reach Bevy is the pose each body ends up with, which is
one `Transform` write through the API that already exists.

`.ref/3DEngine` has a working version of this to follow. Its shape:

- an engine-agnostic façade, `Engine.Physics`: `PhysicsSettings` (gravity, timestep, substeps,
  solver iterations, worker threads), a `PhysicsBody` handle struct that forwards every operation
  to the world that owns it, and `BodyKind` for dynamic, kinematic and static. User code never
  sees a Bepu type.
- the backend, `Engine.Physics.Bepu`: a `PhysicsWorld` owning the `Simulation`, `BufferPool` and
  `ThreadDispatcher`, split across partial files for creation, per-body operations, queries and
  stepping, plus the `INarrowPhaseCallbacks` and `IPoseIntegratorCallbacks` structs Bepu needs.

What differs here:

- **Stepping is already solved.** The reference runs its own accumulator with a
  spiral-of-death guard. This project has `[OnFixedUpdate]`, so a step is one call per fixed step
  and Bevy owns the accumulation. That was the prerequisite, and it now exists.
- **Write-back is a `Transform` write.** Poses go into Bevy's own `Transform`, and propagation
  carries them to `GlobalTransform` and the renderer for free.
- **It belongs in its own package**, so the core does not take the dependency. `BepuPhysics` and
  `BepuUtilities` are separate NuGet packages, on 2.5.0-beta at the time the reference was
  written.
- **Teardown matters.** Bepu is pool-based and its `BufferPool` and `ThreadDispatcher` are
  disposable, so they have to be torn down with the app rather than left to the GC.

## Assets and scenes

- **Scene loading** is blocked upstream. In Bevy 0.19 `Scene` became a trait rather than a
  loadable asset, so there is nothing to load. Revisit when Bevy settles the replacement.
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
