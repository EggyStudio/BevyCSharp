# TODO

Work outstanding on BevyCSharp, in the order it blocks building a game: getting content in, drawing
it, putting an interface on it, making it behave, reaching the devices a player has, and shipping
the result.

The ECS half of the project is finished and covered by tests: the behavior model, chunked
iteration, change detection, filters, hierarchy, scheduling, commands, input and states. What is
thin is the engine-facing half. Bevy is compiled in far further than it is bridged, so most of what
follows is bridge work over code already linked into the binary rather than a dependency to add.

Rendering for the high end (virtualized geometry, texture streaming, GI, reflections) is planned
in [RENDERING.md](RENDERING.md), which lists what those techniques need from the engine and the
order it is built in. The items below are the gaps closer to hand.

An item says what exists, what is missing, and what the missing part needs. Adding an export means
bumping `ABI_VERSION` in `native/bevy_csharp/src/lib.rs` and `Native.ExpectedAbiVersion`, which
stops a stale bridge loading against new managed code.

## Content

### Bevy's components through reflection

Every component Bevy reflects is read and written through Bevy's reflection, by its type path and
JSON (`ctx.Ecs.GetReflected`, `SetReflected`) or through a typed wrapper generated from a checked-in
description (`ctx.Ecs.Get<PointLightRef>`), and has a schema built from Bevy's own description, so
the inspector and `./bcs entity.get` and `entity.set` cover cameras, lights and the rest with no
code per type. What [COMPONENTS.md](COMPONENTS.md) has left:

- **A variant's fields have no typed property.** The generated wrappers (`Bevy.Reflected`) type
  every field that is not shown as JSON, but a field inside an enum variant, such as a color's red,
  is there only while that variant is held, so it stays on a string path checked when it runs.
- **Bytes in place need a mirror.** Five components (`Transform`, `GlobalTransform` and the three
  visibility types) are mirrored by hand, for systems that read them every frame. Generating
  mirrors and their layout checks is tier 3.

### Collections in components

A component holds a list either inside its own bytes (`InlineList8<T>` and its siblings) or as a
handle into a managed store (`EcsList<T>`), and a dictionary as a handle into the same store
(`EcsMap<K, V>`), which a remove hook frees with the entity. The inspector, the scene writer and
the schemas treat them as lists and maps, and [COMPONENTS.md](COMPONENTS.md) has nothing left of
them.


### Scenes

`SceneFile` writes entities to a `*.scene.json` and spawns them again, with the hierarchy, unnamed
entities, entity references, data references, ids kept across saves, and Bevy's own components
(cameras, lights, tonemapping) as Bevy's JSON, and `scene.save` and `scene.load` reach it from the
console. A type or field renamed with `[FormerName]` or reshaped behind `[DataVersion]` still
reads its old files, and what a build cannot read is kept and written back. Every file a scene
refers to is named by an id that survives a rename in the asset browser. The editor's document is `assets/world.scene.json`, opened at start and written by
Project/Save. `SceneInstances` places a glTF scene or another scene file with overrides a scene
file keeps, one inside another, with children added under its nodes and renames kept, and the
editor records what is edited on one of its nodes, marks a changed field and reverts it, and puts
a model dropped on the view on whatever is under the pointer (SCENES.md §5). What
[SCENES.md](SCENES.md) has left:
- **Saves are JSON and diff whole components.** `SaveGame` saves and loads what play changed over
  the scenes a game started from, with the `Persistent<T>` values a slot carries, and `user://`
  reaches both sides of the bridge. A component changed in one field is written whole, and a save
  has no binary form for when one grows large (SCENES.md §8).
- **A newer file in an older build** is read as far as its fields match and written back at the
  older version, with what was not read kept, so the newer build migrates it a second time
  (SCENES.md §7). Bevy's own components have neither former names nor versions.

### Data assets

A `[DataAsset]` class or struct lives in a `*.data.json` file of its own, with an id in a `.uid`
sidecar beside it, and a component refers to it through a `DataRef<T>`, which keeps working when
the file is renamed. It holds strings, lists and dictionaries, lists of structs and classes among
them, is loaded once and shared, is read again when its file changes while assets are watched,
and is made and edited in the editor, where a reference folds open into the asset it names with
how many entities and data assets share it, a reference inside a list's item among them.
[COMPONENTS.md](COMPONENTS.md) §3 has nothing left.

### Composing what a glTF file describes

A glTF file's geometry, scenes and materials load, and `ctx.Ecs.SpawnScene` spawns either a glTF
scene or a `.scn`/`.scn.ron` file, both of which are a `WorldAsset` in 0.19. `LoadGltfMaterial`
needs a window, because translating a glTF material into one the renderer draws with belongs to the
renderer, which is the arrangement rather than a limitation.

The division of labor this aims at: glTF carries geometry, materials and animations, because Blender
and every other tool exports them, and composition happens after the scene is spawned by adding
components to what the file defined.

Bevy's own answer to composition is Bevy Scene Notation, and it is **not** reachable from C#.
`bsn!` is a compile-time Rust macro expanding into types that implement the `Scene` trait, so there
is nothing to call at runtime, and Bevy ships no `.bsn` asset format or loader yet. Scenes written
in BSN exist only in Rust source.

What C# has instead is spawn-then-patch through the ECS surface: spawn the scene, walk it with
`ChildrenOf`, add components to the entities it produced, overwrite the `Transform` an artist set.
That reaches the same result with the same last-write-wins rule, because both end as component
inserts. Two things BSN has that it does not:

- **Templates.** A BSN field takes a value turned into a component when the scene spawns, so
  `image: "player.png"` can stand for an `AssetServer::load`. On this side those are separate calls
  that already exist, so what is missing is the convenience rather than the capability.
- **Patching is per component, not per scene.** `ctx.Ecs.Patch` and `PatchTree` change the fields
  they name and leave the rest, as BSN's field-level merge does between two scenes. What has no
  equivalent is stating the patch as data rather than as code, so a second file can overlay the
  first without anything being compiled.

Revisit when `.bsn` ships as a loadable asset. It would load through the same path a glTF scene
does and be authorable without recompiling the bridge, which is the part worth having here.

### Textures and shaders

PNG, JPEG, WebP, BMP and TGA decode in every build. A drawing app reads BCn files through `ktx2` where
the adapter decodes them, and a shader image can be made in BCn and filled a block at a time, but a
windowless app reports no compressed format as decodable, and ASTC and ETC2, which desktop adapters
do not decode, would need transcoding on load. Nothing here blocks a game; it is size on disk and
upload cost.

Shaders are Slang, and a shader declares whatever it needs: numbers, arrays, structs, constant
buffers, any number of textures of any shape, samplers, storage buffers and images. The bridge reads
the layout the compiler reports and builds each material's, pass's and dispatch's bind group from
it, and C# sets every value by name. A camera runs any number of programs over its picture as
full-screen passes, and a program with a compute stage runs over buffers and images that stay on
the GPU and that a material or a pass can draw from. Every file is reloaded when it changes or
anything it imports does, in every profile, and a reload that changes the layout keeps each value
by name. What is left is at the edges of that:

- **Slang cannot see the pipeline's defines.** A Slang entry point is compiled once per program,
  while Bevy compiles a pipeline per mesh layout and per pass with defines saying which vertex
  attributes and prepass outputs exist. The prelude's structs therefore name a fixed set of
  attributes, and the prepass vertex output writes every field any prepass reads. Compiling an
  entry point per mesh layout would lift it, at the cost of a compile per layout.
- **An array of textures needs the adapter to support one.** A shader declaring
  `Texture2D layers[64]` is a binding array, which needs wgpu's texture binding array feature and
  enough of the adapter's per-stage limits for its length. Desktop Vulkan, Metal and DirectX 12
  adapters have both, and the bridge asks for what the adapter offers, but a shader like that fails
  to build its pipeline on one without. Separate globals (`Texture2D a; Texture2D b;`) have no such
  need, up to the ordinary limit of sampled textures a stage may read.
- **A shader's own numbers are read from storage.** wgpu refuses a bind group holding both an
  array of textures and a uniform buffer, so every block of numbers a shader declares for itself is
  bound as a read-only storage buffer with the same layout. The bytes are the same, but a backend
  with no storage buffers, WebGL2 among them, cannot bind one.
- **A texture is filterable where it is sampled.** Which textures a shader samples through a
  sampler decides whether each is bound as filterable, so a 32-bit float image read with `Load`
  binds and one sampled with a linear sampler is replaced by a stand-in with a warning. A float
  image sampled through a nearest sampler would be valid, and is refused the same way.
- **A validation error stops drawing rather than one material.** A stage input the vertex shader
  never wrote is caught by wgpu when the pipeline is built, which Bevy does not scope, so the error
  is the device's rather than the pipeline's, and `Shaders.KeepRenderingAfterErrors` survives it at
  the cost of every frame drawn while the broken pipeline is in use.
- **A pass reads the prepass only from a camera drawn once a pixel.** A multisampled prepass is a
  multisampled texture, which is a different binding from the plain one the layout names, so a
  camera with `Msaa` above one hands its passes the stand-ins, and the same goes for normals and
  motion vectors. Resolving them first, or a second layout for multisampled cameras, would lift
  it.
- **SPIR-V is checked by nobody.** A compute shader compiled to SPIR-V for ray queries reaches the
  driver untouched, so a declaration that differs from what the bridge binds, or a read past a
  buffer's end, is undefined behavior rather than an error. The layout comes from Slang's
  reflection, which cannot tell a comparison sampler from a plain one, so a shader of this kind
  binds only plain samplers. A ray scene builds a pool mesh again only when asked, and builds
  rather than refits it, which costs more for a mesh that deforms every frame.
- **Geometry drawn out of buffers is lit by its own shader.** A draw casts shadows into every
  light's shadow map and writes the prepass's motion and normals, but Bevy's lighting shades only
  materials on meshes, so something drawn out of buffers and lit the way Bevy lights things is
  still a mesh of as many squares as there are particles, or a visibility buffer resolved by a pass
  that does its own lighting with `bcs_pass`'s lights.

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

- **A compressed cross.** A cubemap is read as a column of six faces, a row of them, a
  horizontal or vertical cross, laid out as a column when its pixels arrive, or six images of
  their own (`Render.CubemapFromFaces`). A compressed cross would be rearranged block by block. A
  cube cameras draw into, a layer each, is a target made with six layers.
- **One medium, which is earth's air.** Density, ground albedo and a quality setting are bridged.
  Mars is the other medium Bevy ships, and its dust phase comes from a texture the caller would have
  to supply, since nothing embeds one. `ScatteringMedium::new` takes arbitrary scattering and
  absorption terms, which an alien planet needs and a flat config cannot describe.
- **DLSS.** `bevy_anti_alias` carries it behind a `dlss` feature pulling in `dlss_wgpu`, which has
  licensing terms of its own and runs only on an NVIDIA RTX card through Vulkan on Windows or Linux.
  It also needs a `DlssProjectId` inserted before `DefaultPlugins` and a runtime check of whether
  the machine supports it, so it is a fourth arm on `AntiAlias` that most machines have to be told
  they cannot have.
- **A directional light's soft shadow shows no penumbra here.** `Render.SetSoftShadows` widens a
  spot light's penumbra visibly. A directional light's showed none over a
  slab hanging above a floor, a few units across, at any size tried from 0.05 to a hundred
  thousand, with temporal shadow filtering, temporal antialiasing and the depth bias Bevy's own
  example uses. Bevy's blocker search divides the size by the cascade's texel size, so the size
  that searches the right distance depends on the cascade, and the example's ten suits a scene
  the size of its palm tree. What is left is reading the cascade a slab like this one falls in
  and the depth range it covers, which decide both the search and the blur.
- **One window.** Position, decorations, resizability, always-on-top and exclusive fullscreen are
  bridged, the monitors are readable by size, name and video mode, `Window.SetVideoMode` takes
  the screen over at one of them, and `Config.RememberWindow` reopens the window where it was
  left, at its size and maximized if it was. What is left is more than one window, since every
  entry point addresses the primary one, and the monitor a window was on, which on Wayland is the
  one part of its place an app could still be told.
- **A capture reads back only what a shader image can be.** `Render.TryReadCaptureAsItIs` hands a
  picture over in the format it was drawn in, a half-float target as half floats, where
  `TryReadCapture` clamps it to eight-bit sRGB. An image in a format no shader image is made in,
  such as a one-channel picture loaded from a file, is refused that way and read only as eight
  bits, where its format allows.

### Gizmos

`Gizmos` draws lines, fading lines, arrows, spheres, circles, arcs, rectangles, boxes, capsules,
cones, cylinders, tori, tetrahedra, grids and axis markers, in either of two groups: one the scene
can hide and one it cannot. Calls are queued and drained by one Bevy system each frame, because a
`Gizmos` parameter cannot be held by an exclusive system, and every C# system is one.
`Gizmos.Configure` sets line width, render layers and whether anything is drawn at all.

- **Two groups, not many.** `Gizmos.Configure` takes a `GizmoGroup`, so the shapes the scene can
  hide are settable apart from the ones it cannot, the split a game usually needs. A third category
  needs a third `GizmoConfigGroup`, and a group is a Rust type rather than a value, so it is added
  where the two are and rebuilt.
- **A batch is one call, not one buffer.** `Gizmos.Batch` gathers every shape and hands them over
  together, and the bridge still records each into its queue and draws it with its own Bevy call.
  Bevy's gizmos cost that anyway, so the saving is the crossing and not the drawing.

### 2D

`Render2d` spawns a 2D camera and attaches sprites, and a sprite can be tinted, resized, mirrored,
anchored off its center, cut down to one rectangle of a sheet or one frame of an atlas layout, and
drawn sliced, tiled or fitted inside its size the way a video player letterboxes.

- **Animation is a sample, not a feature.** `SpriteAnimation` in `BevyCSharp.Sample` steps a sheet
  and is there to be copied. A game adds frame events, a queue of clips and animation driven by the
  state machine, and each would be the wrong shape shipped in the library while still costing a
  query a frame.

## Interface

### Layout and text

`Ui` spawns nodes and text, sets a node's position, size, per-side padding, margin and border,
direction, justification, alignment, gaps, growth, wrapping, bounds, overflow, aspect ratio, where
its clipping falls and which camera draws it, draws an image inside one, scrolls what it clips,
draws one frame of a sheet rather than a whole picture, breaks and aligns a run of text in a font of
its own at a spacing and smoothing it chooses, casts a shadow behind it, rewrites it in place, and
reports the pointer over a node asked to be interactive. Naming the camera lets a screen be drawn
with no window, so the layout is asserted on pixel by pixel in an offscreen run like the rest of the
renderer. That covers a HUD, a button, a menu that lays itself out, a panel that resizes, a list
that scrolls and a paragraph that fits its box.

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
which draws them over what the cameras drew. Immediate mode is the point, because an inspector is a
call per field per frame, so there is no widget tree to keep in step with the world.

- **The bundle is ImGui 1.91.5.** From 1.92 Dear ImGui embeds a scalable version of its classic
  font, which the native theme should use rather than the ProggyClean bitmap it has. It arrives when
  the bundle updates and needs no change here.
- **No icon font.** The icons are PNGs the editor ships, loaded through the asset server and drawn
  with `ImGui.Image`. More can be rasterized from SVG when they are wanted.
- **Composed text arrives whole.** The input method is turned on while a field has the focus, with
  its candidate list at the caret ImGui reports, and committed text is typed into the field. What
  ImGui 1.91 cannot do is show a candidate inside the field before it is committed, so the
  platform's own window shows it instead. This path runs only with a window, and no test covers it,
  since an offscreen run has no input method to turn on.
- **The interface is redrawn every frame**, as immediate mode does. At editor scale that is a few
  thousand triangles and one buffer write; if it ever matters, a frame where nothing moved can be
  drawn again instead of rebuilt.

### The editor

`BevyCSharp.Editor` runs. One panel on the right holds the world with a picture per row above the
details of whatever is selected, the tools float in the scene's corners, and the rest is behind a
hamburger whose contents are a table of paths. A component is a card that opens and shuts, a field
is a row drawn as its kind and attributes say, and a component with nothing to show is a tag. The
bottom left is a strip of tabs opening into a card: the console, the asset browser, the shader
programs, the images the scene camera's shaders keep, the settings and the style. Gizmos draw the selection, its handles, the ground and the camera's orientation, and a
drag on a handle moves, turns or stretches what is selected. [EDITOR.md](EDITOR.md) has the design
language.

- **The Material card offers the settings `MaterialSettings` has.** The editor saves a scene file
  holding every entity, its components and its mesh and material, by file or, made in memory, as a
  primitive's recipe, a mesh's geometry or a material's settings, and either can be saved as a file
  of its own (`*.mesh.json`, `*.material.json`). The cards edit a primitive's measures and a
  material's colors, surface, clearcoat, transmission and texture slots, and leave out what a
  standard material has beyond `MaterialSettings`, such as anisotropy, attenuation and the maps
  for clearcoat and transmission.
- **A mesh and a material are shown by where they came from.** They are Bevy components holding
  typed handles, so the panel draws them as their own section with a card each, and a picker of
  shapes, what the scene uses and each mesh or material of each model file. An OBJ lists no parts,
  so the picker offers it whole.
- **The hierarchy names what it can see and the stats panel counts it.** Both go through
  `EditorKinds`, so a camera in the tree and a camera in the count are one question asked once.
  Neither can see a component the bridge does not name, so an entity whose components are all
  engine-side reads as plain. Naming more of them is a bridge job.
- **The inspector draws a field as one arm of one switch**, told how by the attributes the
  generator carried through, or by Bevy's reflection for an engine component, folds included. Editing several things at once writes to all of them
  and dims the name of a field they disagree about. What it cannot do is show a value none of them
  holds, so the box beside a dimmed name is the one entity's rather than blank.
- **A selection is remembered by name**, so what a reloaded script respawns is found again. All of
  them or none, since half a selection coming back is worse than none. Two entities sharing a name
  still resolve to the first.
- **Undo covers what can be reversed exactly**, which is a field edited in the inspector, a
  rename, a new entity, a duplicate, something hidden with its eye, and a component put on or taken
  off, keeping what it held. Despawning is deliberately not recorded. An entity's mesh and material
  can be named where they were loaded from, so a despawn could be reversed for one drawn with
  files, and not for one drawn with a mesh built in memory. Half a despawn coming back is worse than
  none.
- **Settings are the editor's, not the project's.** `EditorSettings` saves to
  `user://settings.json` with the layout, in the editor's own directory, and everything on it
  belongs to the person using this editor. A project setting worth the
  name (a startup scene, a physics step, a build target) needs somewhere to live that is part of
  the project, which the scene file could carry and does not yet.
- **The scene is the camera's viewport rather than a texture.** Docked, `Render.SetViewport` gives
  the camera the rectangle the panels left. A texture would make the scene a panel of its own,
  dockable and tabbable, as a second view needs.
- **A theme is a file, and only the running build has it.** `assets/theme.txt` is written beside the
  binary, so a look dialed in has to be copied back into the project by hand to be shipped.
- **Most lists draw every row every frame.** The world panel draws only the rows on screen,
  through ImGui's list clipper, so a world of thousands of named entities costs what the few dozen
  in view cost. The asset browser's tiles, the console and the inspector's rows still draw all of
  theirs, which at editor scale is nothing, and each would take the same change to its loop.

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
- **A computed state reads one source.** `app.AddComputedState` maps values of one state onto
  values of another, by a table or by a rule of the game's own, which the bridge calls back into
  through one function pointer with the computed state's number. Deriving from two states at once
  needs a computed state whose source is a pair, which is a slot type per pair of axes.
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

- **An asset type Bevy does not reflect has no name.** `AssetLoadFailed.Kind` names an asset by
  the curated list `AssetServer.Load` takes, and any other by the short name Bevy's type registry
  gives it, so an asset the engine loaded for itself as part of something else is named too. A
  type nobody registered for reflection answers an empty string.

### Components Bevy owns, by id

`bcs_component_id_of` resolves the short names of the mirrored and name-only components by hand,
and any other component Bevy reflects by its full type path through the registry. So any reflected
component can be filtered on, counted and removed by id. Reading its bytes in place still needs a
mirror, which only a type of plain numbers can have. `Name` holds a `String`, and the render
components (`Camera`, `PointLight`, `DirectionalLight`, `Mesh3d`, `MeshMaterial3d`) hold handles
or projection data, so those stay on reflection.

### Physics

`Bevy.Physics` simulates rigid bodies with BepuPhysics v2 on the managed side, in the library
beside the interface. `PhysicsWorld` is the whole surface, with bodies by entity
and no Bepu type in it, so the engine underneath can be replaced without a game changing. It owns
the simulation, its buffer pool and thread dispatcher, and the callbacks Bepu requires, steps once
per fixed step, and writes each dynamic body back through `Transform`. Each body can have a
material of its own, friction and a bounce Bepu's stiff contacts do not give, which the world
gives back after the step, and a body that tumbles in an odd shape is a convex hull of its points,
turning about the hull's center while its entity keeps its origin. Ball joints, hinges, welds and
distance ranges hold bodies together, a hinge can turn itself and stop at an angle, and contacts
are reported, a sensor making a trigger volume. What is left:

- **Not published yet.** The workflow packs the core alone, so the package is reached by project
  reference inside this repository. Packing it is adding it to the pack step.

## Platform

### Input

- **Gamepad.** Excluded deliberately. `bevy_gilrs` needs libudev development headers at build time
  on Linux, which the current profile avoids so the bridge builds with nothing but a C compiler.
  Adding it means accepting that build dependency or gating the feature per platform.
- **IME reaches the bus and the editor, not Bevy's UI.** `Window.SetIme` turns the input method on,
  and `ImeComposing` and `ImeCommit` arrive as messages, so a field of the game's own can show a
  candidate before it is committed, and the editor's fields take what is committed. Bevy's UI has
  no text field of its own to hand them to.
- **A synthetic pointer clicks only the interface without a window.** `SyntheticInput` writes a
  window message and the interface's own event, and an offscreen run, which has no window, takes
  the interface's half, so `bcs` clicks an offscreen editor's panels, buttons and fields. Picking
  and the camera read the window's pointer, so the scene is not clicked there, which is the other
  half of the path a hand takes.
- **Touch.** Bridged as this frame's list, up to eight at once, and untested, because this machine
  has no touchscreen and only the empty case is covered. Gestures are not derived, and a touch that
  ends is reported once rather than lingering for a frame.

### Audio

`bevy_audio` is compiled into the render profile with Ogg Vorbis, WAV, FLAC and MP3, and `Audio`
plays, stops, pauses, sets volume per sound, per bus, and over everything at once, plays a window
out of a clip rather than all of it, places a sound in the world for a nominated listener, and reads
and moves the point a clip has reached. A playing sound is an entity.

It is the one part of the bridge that takes a system library, because cpal links against ALSA on
Linux, so a render build needs `libasound2-dev` or the equivalent. `build-native.sh` installs it
into the container on the portable path and checks for it before a local build, naming the package
per distribution. The minimal profile still builds with nothing but a C compiler.

- **Seeking a looping sound.** Looping is rodio's `Repeat` over a `Buffered` source, which keeps
  the decoded samples so the clip can start again and refuses to move within them, so a seek
  reports `INVALID_STATE`. Nothing here can work around it, so music that has to resume where it
  left off is played once and restarted. Revisit if rodio makes a buffered source seekable.
- **A bus is only a volume.** `AudioSettings.Bus` and `Audio.SetBusVolume` group sounds under a
  volume, kept on the managed side since Bevy has no mixer. A bus carries no effects, since rodio
  mixes each sink straight to the device and there is nowhere between the two to put a filter or a
  reverb.

## Project

### Testing

What reaches the render world is drawn and looked at rather than taken on trust. Every registration
goes through `assets::init_asset_once`, whose two halves the crate's own tests pin, and the step
after is covered by drawing into an offscreen target and asserting on the pixels: a mesh with an
unlit material made in code (`DrawnTests`), a glTF file's own material (`GltfMaterialDrawnTests`), a
lit surface through the sky and environment map tests, a sprite through the overlay one, and text
and the interface through the layout's pixel tests. Every lens effect is checked by the shape of
its change, depth of field included (`DepthOfFieldTests`), which measures a checkered surface
focused far past, since its blur is capped in pixels, needs a lens far out of focus to show at all,
and leaves silhouettes alone. These need a GPU, so they are skipped on the headless bridge the
test workflow builds, each through `Needs` in the tests with the profile, tool or device it lacks
as the reason, so a run's summary counts what did not run there rather than passing it.

### Build and release

- **The portable build is not cached.** `Swatinem/rust-cache` now names the directory
  `build-native.sh` writes to, so the bridge and the crate's own tests both come back from cache.
  What is not cached is the container path `PORTABLE=1` takes, which writes to
  `build/target-portable` and serves a local checkout on a newer glibc.
- **Packing on one machine produces a package for one platform.** Use the CI workflow, or run
  `build-native.sh` on each target, to produce a package covering all six runtime identifiers.
- **An export is built for one platform at a time, on that platform.** The Play tab exports a
  project with `dotnet publish`, self-contained, for a chosen platform, with a render bridge built
  for exports or one carrying its assets. The bridge is built on this machine, so an export for
  another platform needs that platform's toolchain, as packing does. [PLAY.md](PLAY.md) plans the
  rest, with playing the scene being edited.
- **Publishing is manual by choice.** The workflow builds and uploads; the upload to nuget.org is
  done by hand.
