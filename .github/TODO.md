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

### glTF materials in a windowless app

A glTF file's geometry, scenes and materials are all reachable. `LoadGltfMaterial` asks for
`ship.gltf#Material0/std`, the label `PbrPlugin` publishes its translated `StandardMaterial`
under, so it works wherever that plugin is installed, which means a windowed run.

A windowless app does not install it, so the same call fails there. Two routes were considered
and rejected:

- **Add `PbrPlugin` to the windowless path.** It builds, and then breaks the app: 108 of 129 tests
  fail against a bridge that does this. It pulls in the render plugin chain, and the pieces that
  need a render sub-app do not tolerate its absence.
- **Copy the conversion.** `standard_material_from_gltf_material` is private to `bevy_pbr`, and
  reproducing it means 60 field assignments behind 16 feature gates that would have to track
  upstream. The divergence would be invisible until a material looked wrong.

What is left is a narrower version of the first: register a `GltfExtensionHandler` of our own on
the windowless path only. The trait and the `GltfExtensionHandlers` resource are both public, and
the handler would publish the same `/std` label, so nothing above it would change. It still needs
the conversion, so it is worth doing only alongside a way to keep that honest.

This only matters for a windowless run, which draws nothing. It is a gap in testability rather
than in what a game can do.

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

`bevy_ui_render` is compiled in and `Ui` spawns nodes and text, sets a node's position, size and
padding, and rewrites text in place. That covers a HUD. A menu needs more:

- **Interaction.** Nothing reports a click or a hover. Bevy's `Interaction` component changes as
  the pointer moves over a node, and is mirrorable as a name-only handle plus a byte, which would
  make a button possible without bridging `bevy_ui_widgets`.
- **Layout.** `Node` carries two dozen fields and six are bridged. Flex direction, justify and
  align, gaps, margins and borders are what turn a stack of nodes into a laid-out screen.
- **Images.** A node can hold a texture through `ImageNode`, which is how an icon or a nine-slice
  panel is drawn. The handle plumbing for it already exists.
- **Text layout.** Justification, line breaking and bounds are `TextLayout`, unbridged, so a long
  string runs off the edge.

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

Cameras, lights and the window take their common parameters. What is left is narrower.

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

`bcs_component_id_of` resolves eight names by hand: `Transform`, `GlobalTransform`, `ChildOf`,
`Children`, `Visibility`, `InheritedVisibility`, `ViewVisibility` and `WorldInstance`. Anything
else is unreachable. A general lookup is not possible through the type registry alone, since the managed
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
