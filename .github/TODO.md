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

### Load meshes from glTF

`bevy_gltf` is not compiled in at all, so a scene can only use `Cuboid`, `Sphere`, `Plane` and
`Capsule`.

- features: `bevy/bevy_gltf`
- exports: extend `bcs_asset_load` to accept a glTF kind, plus a way to name a sub-asset, since
  one file holds many meshes, materials and nodes
- managed: `AssetKind.Gltf`, and a call that attaches a loaded mesh to an entity the way
  `Render.SetMesh` attaches a built one
- note: a glTF file produces a hierarchy rather than a single mesh. Decide whether the bridge
  spawns that hierarchy or exposes the parts individually, because it changes the API shape.

### Bind textures to materials

`AssetKind.Image` already loads and returns a valid handle, and nothing consumes it.
`bcs_material_create` takes six floats and has no parameter for a texture.

- features: `bevy/jpeg` and the other formats worth supporting alongside the current `png` and
  `ktx2`
- exports: a material builder that accepts image handles for base color, normal, metallic and
  roughness, and emissive
- managed: replace the flat `Render.CreateMaterial` argument list with a description type, since
  the parameter count is already at six and every map adds one

### Widen material parameters

Emissive, alpha blending mode, double-sided and unlit are all `StandardMaterial` fields that no
call reaches. Transparency is therefore not expressible.

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

### States

`bevy_state` is compiled in and unbridged. There is no way to express menu, playing and paused,
which means no way to scope a system to one of them.

- exports: define a state type, read it, request a transition, and register a system to run only
  in a given state
- managed: a state surface plus a run condition, alongside the existing `[RunIf]`

### Messages between systems

Systems communicate only through components and resources. Bevy's message API is not bridged, so
there is no way to broadcast something like a collision or a button press to several readers
without inventing a component to carry it.

## Rendering control

The renderer is reachable but barely configurable. Each of these is a small widening of an
existing call rather than new machinery.

- **Camera**: `bcs_render_spawn_camera_3d` takes no arguments. No field of view, no orthographic
  projection, no clear color, no render layers, no viewport.
- **Lights**: `bcs_render_spawn_light` takes a kind and an intensity. No color, no spot lights, no
  range or falloff, and no way to turn shadows off per light.
- **Window at runtime**: title, size, fullscreen, and cursor grab and visibility are fixed at
  startup through `Config`. Cursor lock is a requirement for a first-person camera, so the work
  is small but blocks that camera entirely.

## Input

- **Gamepad**: excluded deliberately. `bevy_gilrs` needs libudev development headers at build
  time on Linux, which the current profile avoids so the bridge builds with nothing but a C
  compiler. Adding it means either accepting that build dependency or gating the feature per
  platform.
- **Text input**: no character or IME events, so a name entry field is not possible.
- **Touch**: not bridged.

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

Bevy ships no physics engine, so this means bridging Avian or Rapier. It is comparable in size to
everything built so far. The fixed timestep it depends on now exists, so the prerequisite is met,
but this is still worth treating as a separate project rather than an item here.

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
