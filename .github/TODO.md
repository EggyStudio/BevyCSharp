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

A program names the files that draw a material, WGSL or Slang, and a material carries sixty-four
floats, a storage buffer of any size, eight textures with samplers, and two each of cubemaps, array
textures and 3D textures. A camera runs any number of programs over its picture as full-screen
passes, and a program with a compute stage runs over buffers that stay on the GPU and that a
material or a pass can draw from. Every file is reloaded when it changes, in every profile, and a
Slang file is recompiled when anything it imports changes. What is left is at the edges of that:

- **The bind group is the same for every material.** Bevy lays a material's bind group out per
  type, and there is one type, so what a shader can be handed is what the table in the README
  lists. It is sized generously rather than described by the caller, which keeps it a shader to
  write rather than a layout to learn, but a storage texture a shader writes to, a depth texture,
  a comparison sampler or a ninth sampled texture has nowhere to go. A texture format that cannot
  be filtered, such as a 32-bit float heightmap, is replaced by the fallback for the same reason.
- **Slang cannot see the pipeline's defines.** A Slang entry point is compiled once per program,
  while Bevy compiles a pipeline per mesh layout and per pass with defines saying which vertex
  attributes and prepass outputs exist. The prelude's structs therefore name a fixed set of
  attributes, and the prepass vertex output writes every field any prepass reads. A WGSL shader
  reads the defines with `#ifdef` and has no such limit.
- **A validation error stops drawing rather than one material.** A Slang shader's bindings are
  checked against the bridge's layouts before a pipeline is built from it, so a binding of the
  wrong kind is a failed compile with the binding named. A WGSL shader is composed by naga_oil
  inside Bevy's pipeline cache, where nothing of the bridge's sees the result, and a stage input
  the vertex shader never wrote is not checked in either language. Both are caught by wgpu when
  the pipeline is built, which Bevy does not scope, so the error is the device's rather than the
  pipeline's, and `Shaders.KeepRenderingAfterErrors` survives it at the cost of every frame drawn
  while the broken pipeline is in use.
- **A pass reads depth only from a camera drawn once a pixel.** A multisampled prepass is a
  multisampled texture, which is a different binding from the plain one the layout names, so a
  camera with `Msaa` above one hands its passes the stand-ins. Resolving the depth first, or a
  second layout for multisampled cameras, would lift it. Motion vectors are not bound at all.
- **A compute shader writes two image formats.** A storage texture's format is part of its
  binding, and the layout is fixed, so a dispatch writes one eight-bit image and one half-float one,
  write-only. Reading and writing one image in the same dispatch needs a format wgpu allows that
  for, which is a single 32-bit channel, and a simulation that wants its last state reads it from a
  second image instead.
- **A buffer is drawn by one mesh.** A material reads a buffer, but what it draws is still one
  entity's mesh, so ten thousand particles are a mesh of ten thousand squares, built with
  `Render.CreateMesh(MeshData)`, whose vertex shader places each from the buffer. Bevy's indirect
  and instanced drawing is what would let the buffer say how many there are, and buffers are made
  with the usage it needs.

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
- **A light probe is the whole scene's.** `Render.SetImageLighting` and `SetEnvironmentMap` put
  the map on a camera, so everything it draws is lit by one environment. Bevy's `LightProbe` is a
  volume that lights what is inside it, which is what a room lit differently from the corridor
  outside it needs, and what makes a reflection change as something walks between them.
- **A cubemap of anything else.** The reinterpretation is a column of six faces stacked
  vertically, which is what a file holds. A cubemap rendered into, which is what a reflection probe
  or a point light's shadow would want, needs an image created with six layers rather than one
  reinterpreted after loading.
- **One medium, which is earth's air.** Density, ground albedo and a quality setting are bridged.
  Mars is the other medium Bevy ships, and its dust phase comes from a texture the caller would
  have to supply, since nothing embeds one. `ScatteringMedium::new` takes arbitrary scattering and
  absorption terms, which is what an alien planet wants and what a flat config cannot describe.
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

- **What is saved is what can be named.** `assets/world.json` keeps every named entity's name,
  every component with a schema, and where its mesh and material were loaded from. What it cannot
  write is anything built in memory, since a set of numbers has no name, nor a camera's projection
  or a light's settings, which are engine components with no schema and no path either.
  `bevy_world_serialization` would write exactly those and can see no C# component at all, because
  those are bytes registered at runtime with no Rust type behind them. A world asset worth the name
  is both files, or one format holding both halves.
- **One preview, not a thumbnail each.** An image tile shows itself, and a selected model is drawn
  by a camera of its own into a render target beside the tiles, framed by its bounds and kept on a
  render layer nothing else is on. One scene rather than one per tile, because a camera drawing
  into an image costs a pass a frame and forty tiles would cost forty. A thumbnail on every tile
  wants a pass that draws once and is kept, which nothing here does.
- **A material has no preview.** The pieces are the same ones the model preview uses, and what is
  missing is a material to point at. The editor can name the material an entity is drawn with but
  not hand it to a second mesh, because a handle read back from an entity is a path rather than a
  handle.
- **A mesh and a material are shown by where they came from.** They are Bevy components holding
  typed handles, so they have no schema and the panel draws them as their own section, reading the
  asset path and offering the files that suit. What it cannot do is name a part of a file other
  than the first, since a glTF holds many meshes and nothing here can list them without loading it,
  so the picker takes `Mesh0/Primitive0` and a file with several needs the label written by hand.
- **The hierarchy names what it can see and the stats panel counts it.** Both go through
  `EditorKinds`, so a camera in the tree and a camera in the count are one question asked once.
  Neither can see a component the bridge does not name, so an entity whose components are all
  engine-side reads as plain. Naming more of them is a bridge job.
- **The inspector draws a field as one arm of one switch**, told how by the attributes the
  generator carried through, folds included. Editing several things at once writes to all of them
  and dims the name of a field they disagree about. What it cannot do is show a value none of them
  holds, so the box beside a dimmed name is the one entity's rather than blank.
- **A selection is remembered by name**, so what a reloaded script respawns is found again. All of
  them or none, since half a selection coming back is worse than none. Two entities sharing a name
  still resolve to the first.
- **Undo covers what can be reversed exactly**, which is a field edited in the inspector, a
  rename, a new entity, something hidden with its eye, and a component put on or taken off, keeping
  what it held. Despawning is deliberately not recorded. An entity's mesh and material can now be
  named where they were loaded from, so a despawn could be reversed for one drawn with files, and
  not for one drawn with a mesh built in memory. Half a despawn coming back is worse than none.
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
  the test workflow builds. A lit surface is covered by the sky and environment map tests, a sprite
  by the overlay one, and text and the interface by the pixel tests over the layout. What is
  unchecked that way is a glTF file's own materials, which need a file with one in it.
- **Depth of field is the one lens effect with no test.** `LensTests` draws the same scene twice
  for the vignette, the chromatic fringe and the lens distortion, and asserts the shape of the
  change. Depth of field resists it for three reasons worth knowing before trying again. The blur
  is capped in pixels rather than scaled, so `MaxBlurDiameter` decides it and the aperture
  saturates against that cap. A short lens focused far away has an enormous depth of field, so
  the physically obvious settings produce under a pixel of blur. And the pass keeps a silhouette
  from smearing into what is behind it, which is what the depth buffer is for, so the strongest
  edge in a simple scene is the one edge the effect is built not to touch. A test wants a textured
  surface filling the frame at a focus it misses, measured inside the shape.

### Build and release

- **The portable build is not cached.** `Swatinem/rust-cache` now names the directory
  `build-native.sh` writes to, so the bridge and the crate's own tests both come back from cache.
  What is not cached is the container path `PORTABLE=1` takes, which writes to
  `build/target-portable` and is what a local checkout on a newer glibc uses.
- **Packing on one machine produces a package for one platform.** Use the CI workflow, or run
  `build-native.sh` on each target, to produce a package covering all six runtime identifiers.
- **Publishing is manual by choice.** The workflow builds and uploads; the upload to nuget.org is
  done by hand.
