# BevyCSharp devlog

What landed in BevyCSharp, by day and newest first, with the pictures taken on the way, the captures
the repository keeps under `.github/assets`. A line under Done names the item of .github/REVIEW.md
or .github/REVIEW-ARCHIVE.md and the commit that closed it.

## 2026-10-24

![](.github/assets/denoiser.webp)
![](.github/assets/readings/2026-10-20-hub-particles.webp)
![](.github/assets/readings/2026-10-21-edit-mesh-primitives.webp)
![](.github/assets/readings/2026-10-21-grid-room.webp)
![](.github/assets/readings/2026-10-21-modeling-window.webp)
![](.github/assets/readings/2026-10-21-overlay-f3.webp)
![](.github/assets/readings/2026-10-21-swarm-waves.webp)
![](.github/assets/readings/2026-10-21-terminal-room.webp)
![](.github/assets/readings/2026-10-21-texture-coordinates.webp)
![](.github/assets/readings/2026-10-21-textured-cube-editor.webp)
![](.github/assets/readings/2026-10-23-field-pair.webp)
![](.github/assets/readings/2026-10-23-field-window.webp)
![](.github/assets/readings/2026-10-24-denoiser-checker-crops.webp)
Item 51's second part adds global illumination's light to Bevy's surfaces through a pass after the
opaque geometry that takes back Bevy's ambient diffuse from the G-buffer and adds the surface's
diffuse color times the probes' light, with a sky over a ground past the last cascade, the probes'
sky at the 0.45 of an ambient light that Bevy's ambient term returns, Render.SetGlobalIllumination
public, a row on the feature test's graphics page, tests of a red wall tinting a white block's side
and an open floor reading as Bevy's ambient light leaves it, the costs read in a Cornell box, and
the denoiser's picture in the ray tracing guide beside the review's readings. (`07863068`)

![](.github/assets/denoiser.webp)
The feature test's Cornell box under Solari's ray-traced lighting, one grainy frame on the left and
the denoiser's frame on the right, item 54's node over the same frame.

![](.github/assets/readings/2026-10-24-denoiser-checker-crops.webp)
The checkered ground far off in three crops, the reference mean, the denoiser's frame and a grainy
one, the reading that moved the demodulation to the diffuse color plus the specular reflectance
(item 54).

Done that day

- Item 54 at `97440a1d`. A denoiser over Solari's lit result, so the ray-traced tier's picture is
  clean (Decision 52).
- Item 59 at `750c9cbc`. The denoiser's node comes down from 3.6 ms at 1080p, read stage by stage
  first.

## 2026-10-23

![](.github/assets/scene-field.webp)
The scene's distance field darkens each directional light where it closes a surface off, past the
shadow map by default, by taking back the light Bevy gave from the camera's G-buffer in a picture of
floats, and draws its settings and a slice across a cascade in a window of its own, which the
feature test shows at its gallery. (`d2e6ef81`)

![](.github/assets/readings/2026-10-23-field-pair.webp)
The scene's distance field read as a pair, a box's field mapped and traced (item 50).

![](.github/assets/readings/2026-10-23-field-window.webp)
The field's window in the feature test, the cascades' cells over the room (item 50).

Done that day

- Item 43 at `cd05d9a2`. Modeling operations on faces and edges, Blender's kinds (Decision 46).
- Item 44 at `cd05d9a2`. Modifiers, a stack applied at the build and never baked until asked
  (Decision 46).
- Item 45 at `ba8111fb`. What is modeled is exported for Blender and imported from it (Decision 46).
- Item 49 at `e1b08124`. F1 hides the interface, F3 shows a debug overlay like the newest
  Minecraft's with sections a program adds, and Shift+F3 the profiler with it (Decision 50).
- Item 50 at `d2e6ef81`. The scene's distance field, 3DEngine's first phase of its global
  illumination, through the bridge (Decision 51).
- Item 55 at `135e43b5`. The README's animated capture of Swarm goes.
- Item 56 at `fda2f62e`. The bridge's Slang modules give their names plainly, as 3DEngine's do, with
  no `bcs_*::` before them (Decision 56).
- Item 58 at `66e2b067`. The cheat sheet's headers are 3DEngine's, plain group headings and no sub-
  heading a type (Decision 58).

## 2026-10-21

![](.github/assets/blockout.webp)
Blockout, a level of three rooms, a corridor and stairs between two floors blocked out from one
recipe, is walked through the character controller and reshaped from the modeling window on Tab, and
the booleans take a solid primitive's line as the cut does. (`09fc95dd`)

![](.github/assets/readings/2026-10-21-swarm-waves.webp)
Swarm's arena across three waves, the game played by its script.

![](.github/assets/readings/2026-10-21-edit-mesh-primitives.webp)
The edit mesh's primitives in the drive, a box, a sphere, a cylinder, a cone, a wedge and an arch
(item 38).

![](.github/assets/readings/2026-10-21-grid-room.webp)
A room blocked out in the prototype materials with the grid image of a meter's cell (item 39).

![](.github/assets/readings/2026-10-21-terminal-room.webp)
The room built through the model commands from the terminal alone (item 40).

![](.github/assets/readings/2026-10-21-modeling-window.webp)
The modeling window in the editor with its picking, its drag and the Model tab (item 40).

![](.github/assets/readings/2026-10-21-texture-coordinates.webp)
Texture coordinates by projection on a checker and a wood grain (item 41).

![](.github/assets/readings/2026-10-21-textured-cube-editor.webp)
A textured cube in the modeling window after the texture coordinates landed (item 41).

![](.github/assets/readings/2026-10-21-overlay-f3.webp)
The F3 overlay over the feature test, the frame's strip and its sections (item 49).

Done that day

- Item 38 at `08d03b4d`. An edit mesh a program shapes by operations, with a recipe that rebuilds
  it, built into what the engine draws and collides with (Decision 46).
- Item 39 at `bfe1fc63`. Grid prototyping, materials that show a meter, texture coordinates aligned
  to the world, and openings cut by booleans (Decision 46).
- Item 40 at `bee54e98`. The modeling commands, so a level is blocked out from the terminal, and a
  window in the game to shape by hand (Decision 46).
- Item 41 at `fc63b7ba`. Texture coordinates shaped and textures given to faces (Decision 46).
- Item 42 at `09fc95dd`. A game that is a level blocked out with the tools, Blockout (Decision 46).
- Item 46 at `585497f1`. An animated capture of a run, frames taken at an interval and written as
  one file, and one made from captures on disk (Decision 48).
- Item 47 at `3d045110`. F2 takes a screenshot in every program, to a screenshots folder beside it
  (Decision 49).
- Item 48 at `5f051299`. The render recovery test reads its error count until it is one or a bound
  of frames.
- Item 53 at `45526e4b`. ImGui's pass moves from the editor profile to the render profile, so a game
  on the package draws ImGui.

## 2026-10-20

![](.github/assets/readings/2026-10-20-hub-particles.webp)
The feature test's hub under its particles, driven offscreen for the readings.

Done that day

- Item 2 at `5e9235ce`. The norm's lists are paid down.
- Item 29 at `0f8e89b4`. What was mended for the time being is mended for good (Decision 38), after
  item 4.
- Item 34 at `20f3d5c0`. A behavior's write after a call that moves its entity's table row lands in
  another entity's row.
- Item 35 at `41ebc700`. The build workflow's jobs are named as 3DEngine's are, by their ids, with
  no expression in a name.
- Item 36 at `d56e7cb7`. A program reads the force a joint held its bodies with over the last step,
  from 3DEngine's `GetPhysicsJointForce`.
- Item 37 at `cf70c650`. The feature test moves into the games folder, as 3DEngine's lives (Decision
  45).

## 2026-10-19

![](.github/assets/examples/monitor_info.webp)
monitor_info is captured on four virtual monitors of sway in a container by build/capture-monitor-
info.sh, so it is written whole and opens no window on a machine's own screens, and the keyboard
bitset's size is named in ulongs. (`142dc131`)

![](.github/assets/examples/debug_picking.webp)
![](.github/assets/examples/fps_overlay.webp)
![](.github/assets/examples/infinite_grid.webp)
![](.github/assets/examples/scene_viewer.webp)
Bevy's dev tools come in, the FPS overlay, the infinite grid and picking's debug overlay asked for
through Config.DevTools with the gizmos' frusta and skinned bounds beside them, and fps_overlay,
infinite_grid, debug_picking and scene_viewer are written. (`64c1b25d`)

![](.github/assets/examples/feathers_gallery.webp)
Feathers' menus, selects, list views, color controls, text input, panes, groups and dialogs come in
with feathers_gallery written on them, and a Color crosses each of Bevy's color spaces as Bevy
converts it. (`1e8c4750`)

![](.github/assets/examples/feathers_number_input.webp)
![](.github/assets/examples/virtual_keyboard.webp)
Feathers' number input and virtual keyboard come in with feathers_number_input and virtual_keyboard
written on them, and Ui.EditText queues any of Bevy's text edits on a field. (`5452a595`)

![](.github/assets/examples/feathers_counter.webp)
Feathers comes in with Bevy's dark theme, its buttons, checkboxes, radio buttons, toggle switches,
sliders and labels spawned from C# and feathers_counter written on them, the lock gaining
bevy_feathers alone. (`edee9686`)

![](.github/assets/examples/free_camera_controller.webp)
![](.github/assets/examples/pan_camera_controller.webp)
![](.github/assets/examples/pan_orbit_camera_cad.webp)
![](.github/assets/examples/pan_orbit_camera_minimal.webp)
![](.github/assets/examples/pan_orbit_camera_ortho.webp)
Bevy's free, pan and pan-orbit camera controllers are bridged with their five examples, the lock
gaining bevy_camera_controller alone, and Bevy's free camera takes the place of the examples' own in
the sixteen that flew one. (`670ecf2c`)

Done that day

- Item 4 at `142dc131`. The gaps, by how many rows each holds.
- Item 31 at `8fe38f60`. The README's centered parts are markdown inside one centering block
  (Decision 40), and nuget.org draws them.
- Item 32 at `d482caa3`. A package's release notes are the commits since the package last published
  (Decision 41).
- Item 33 at `e1f516fa`. The pack run publishes through nuget.org's trusted publishing and no stored
  key (Decision 42).

## 2026-10-18

Done that day

- Item 22 at `5e750133`. Swarm builds on the package version its project names.
- Item 26 at `c3bd5349`. The reflected variants holding a tuple, an instant or a graph's index get
  typed properties, TODO.md's open line under the components through reflection, item 4's remainder.
- Item 27 at `1a6c5d22`. Byte mirrors for the components that are plain data, COMPONENTS.md's third
  tier, TODO.md's next open line, in steps.
- Item 28 at `c5f19669`. An atmosphere takes a medium of the game's own, TODO.md's rendering line,
  one commit.

## 2026-10-17

Done that day

- Item 23 at `b178ae3f`. Two workflows an engine, `build.yml` on a push and `pack.yml` by hand
  (Decision 34).
- Item 24 at `1d246e9f`. The docs job leaves the build workflow (Decision 36); the repository's own
  links are a test of the suite.
- Item 25 at `b4d45938`. The register, STYLE.md's rule and the sweep (Decision 37).

## 2026-10-12

![](.github/assets/examples/font_atlas_debug.webp)
Bevy's font_atlas_debug example is written in C#, the pictures of the font atlases read through a
call that answers each with the size of the font drawn into it. (`75024f77`)

![](.github/assets/examples/solari_reflections.webp)
Bevy's solari_reflections example is written in C#, a room of mirrors and a test material whose
roughness a slider sets, drawn by Solari's realtime lighting or by the reference path tracer at a
key. (`b326b127`)

![](.github/assets/examples/render_recovery.webp)
Bevy's render_recovery example is written in C#, with a policy for what a render error does, a count
of the errors and a call that causes each kind, and an app that closes while its pipelines compile
no longer aborts on its way out. (`0da017e7`)

![](.github/assets/examples/atmosphere.webp)
Bevy's atmosphere example is written in C#, with Mars's air, the choice of how the sky is worked
out, and the disk of the sun a light stands for. (`a76577ac`)

![](.github/assets/examples/world_serialization.webp)
Bevy's world_serialization is written in C#, with text a component holds, a field a scene file
leaves out, and the game's own resources saved and loaded beside a scene's entities. (`c9b8728f`)

![](.github/assets/examples/decodable.webp)
A game plays samples it writes as it plays, a stream that plays silence when it runs out, and Bevy's
decodable example is written with it. (`2aaaef58`)

![](.github/assets/examples/pitch.webp)
A tone of one frequency is made and played as a clip is, as Bevy's pitch example plays one, and a
mesh may be left without the normals a shader of its own never reads. (`7f42cf40`)

Done that day

- Item 16 at `4e6eab36`. An offscreen click lands only where the pointer has been.
- Item 19 at `c9b8728f`. A world a scene file saves whole, a component's text, a field left out and
  a resource of the game's own, Bevy's `world_serialization` reached from C#.
- Item 20 at `7f42cf40`. A mesh without normals on purpose, `MeshData.ComputeNormals` (Decision 32).
- Item 21 at `c8849d71`. A behavior's `[Changed]` filter on its own type fires every frame.

## 2026-10-11

![](.github/assets/examples/transform_gizmo.webp)
An app may ask for Bevy's interactive transform gizmo, and Bevy's transform_gizmo example is written
with it. (`a612ab27`)

![](.github/assets/examples/occlusion_culling.webp)
The meshes the GPU drew in the opaque pass are read back on request, as Bevy's occlusion_culling
example reads them, and that example is written with it. (`7bc0af69`)

![](.github/assets/examples/custom_vertex_attribute.webp)
A mesh carries vertex streams of the game's own that a vertex shader of the program's own reads at
their locations, and Bevy's custom_vertex_attribute example is written with them. (`32ae29fb`)

![](.github/assets/examples/dynamic_mip_generation.webp)
The GPU makes an image's mipmaps from its top level each frame on request, as Bevy's
MipGenerationJobs does, and Bevy's dynamic_mip_generation example is written with it. (`6c491f7c`)

![](.github/assets/examples/ui_material.webp)
An interface node can be drawn by a Slang program of the game's own, as Bevy draws a UiMaterial, and
Bevy's ui_material example is written with it. (`2fbd2860`)

![](.github/assets/examples/render_depth_to_texture.webp)
Bevy's render_depth_to_texture is written with a camera drawing its depth alone into no target and a
watch of that depth shown on a plane, which the engine reached already, so its triage line was
stale. (`b134dd4d`)

![](.github/assets/examples/custom_projection.webp)
Render.SetAdjustedPerspective gives a camera Bevy's perspective with entries of its matrix written
over it, a projection of the game's own the bridge declares since C# cannot, and Bevy's
custom_projection is written with it. (`6e28c3cc`)

![](.github/assets/examples/error_handling.webp)
Bevy's error_handling is written, its points sampled over the sphere from its triangles read back
and its errors met as a C# system meets them, logged by the engine or caught by the system, and the
guide says how that answers to Bevy's error handlers. (`6641dbcc`)

![](.github/assets/examples/load_gltf_extras.webp)
Bevy's load_gltf_extras is written through the extras wrappers glTF's four kinds of extras already
had, so its triage line was stale, and the guide says where a model's extras arrive. (`7a6cbf16`)

![](.github/assets/examples/decal.webp)
Render.CreateDecalMaterial makes Bevy's forward decal material and Render.SetForwardDecal makes an
entity a decal drawn with it, Bevy's decal is written with them, and the bridge's comment on the
path tracer names the development machine's GPU in plain words. (`c16f67e5`)

![](.github/assets/examples/ghost_nodes.webp)
Bevy's ghost_nodes feature is built in, adding no crate, so GhostNodeRef puts a node the layout
passes through on an entity, and Bevy's ghost_nodes is written with it. (`d91665d9`)

![](.github/assets/examples/asset_settings.webp)
Bevy's asset_settings is written, its sampler asked for by a .meta file beside one image and where
another is loaded, with its images fetched from beside Bevy's source, since the loader settings it
needs were reached already. (`4ea63186`)

![](.github/assets/examples/low_power.webp)
Window.SetUpdateSettings sets winit's update modes, focused and not, from Bevy's presets or a mode
of a game's own, Window.RequestRedraw and Window.WakeUp ask for one more update, and Bevy's
low_power is written with them. (`ea5e1532`)

![](.github/assets/examples/custom_cursor_image.webp)
Window.SetCursorAtlas cuts the pointer's image to a frame of an atlas with that frame's hotspot, a
wrapper writing an enum switches its variant only where it holds another so a field the record
cannot type is kept, and Bevy's custom_cursor_image is written with both. (`69fca4bb`)

![](.github/assets/examples/image_node_resizing.webp)
image_node_resizing is written with Bevy's outlines of interface nodes, which the bridge always
built and which are turned on through the resource a plugin made, since one inserted from nothing
has a line width of zero, as InsertResource's remarks say. (`68aa5dd5`)

![](.github/assets/examples/texture_atlas.webp)
Render2d.PackAtlas packs loaded images into one atlas as the app runs, as Bevy's TextureAtlasBuilder
does, with the padding between them, so texture_atlas packs Bevy's folder of sprites four ways and
its rpg folder is fetched whole. (`c11c63d4`)

![](.github/assets/examples/render_primitives.webp)
Gizmos draw Bevy's planes, endless lines and flat primitives as its primitive_2d and primitive_3d
draw them, the round solids take a resolution and a cylinder its half height as named, so
render_primitives is written. (`b3ac2a0e`)

Done that day

- Item 11 at `c3aaacac`. The CLI server writes an answered request before it closes.
- Item 12 at `e1a32283`. A resource inserted from nothing replaced the plugin's with a zeroed one.
- Item 13 at `83b371cc`. The README's gallery as one scrolling row of pictures a group, done through
  `83b371cc` (Decision 27).
- Item 14 at `53fd0c3f`. The bcs skill left the `.claude` folder.
- Item 15 at `c447a370`. The development machine's GPU is named nowhere a user reads.
- Item 17 at `02598971`. The examples' counts under the Examples heading, and a count on each
  group's heading.
- Item 18 at `32ae29fb`. A vertex stream of the game's own on a mesh, Bevy's
  `custom_vertex_attribute` reached from C# (Decision 31).

## 2026-10-10

![](.github/assets/examples/log_diagnostics.webp)
Bevy's diagnostics store is reached from C#, a game's own measures registered, measured, read and
turned off beside Bevy's frame, entity and render ones, and custom_diagnostic,
enabling_disabling_diagnostic and log_diagnostics are written with it. (`90075005`)

![](.github/assets/examples/deferred_rendering.webp)
![](.github/assets/examples/parallax_mapping.webp)
![](.github/assets/examples/specular_tint.webp)
The standard material takes a depth map for parallax mapping, a specular tint with its two maps, and
whether it is drawn forward or deferred apart from the rest, and parallax_mapping, specular_tint and
deferred_rendering are written with them. (`e09a2ed8`)

![](.github/assets/examples/many_meshlet_materials.webp)
![](.github/assets/examples/mines.webp)
![](.github/assets/examples/overflow_transform.webp)
![](.github/assets/examples/test_meshlet.webp)
The four examples that could be written with what is bridged are written, the meshlet test, the
meshlet materials stress test, transformed overflow and the minesweeper game. (`af4242ae`)

![](.github/assets/examples/mesh_shader_intro.webp)
A program draws on a camera with Slang task and mesh shaders compiled to SPIR-V, counted in
workgroups, where the device has them, and mesh_shader_intro is written with it. (`e9a8508e`)

![](.github/assets/examples/gpu_component_array_buffer.webp)
App.AddComponentArray keeps every entity's component in one buffer a shader reads at the entity's
mesh tag, as Bevy's GpuComponentArrayBuffer does, and gpu_component_array_buffer is written with it.
(`2359425d`)

![](.github/assets/examples/pipeline_constants.webp)
A Slang pipeline constant is set on a material by name as any value is and compiled into pipelines
of its own for each set of values, and pipeline_constants is written with it. (`4e3dd60a`)

![](.github/assets/examples/fixed_node.webp)
![](.github/assets/examples/inline_image.webp)
inline_image and fixed_node are written through Bevy's reflected InlineImage and FixedNode, each
drawing as Bevy's does and the fixed node's parent turning red under the pointer. (`095bccd7`)

![](.github/assets/examples/shader_material_2d_bindless.webp)
![](.github/assets/examples/sprite_material.webp)
A 2D shader material given to a sprite draws it as Bevy's SpriteMaterial does, its fragment shader
reading the sprite through bcs_sprite, and sprite_material and shader_material_2d_bindless are
written with it. (`855c4b7e`)

![](.github/assets/examples/shader_material_2d.webp)
A Slang program draws a 2D mesh through 2D stages of its own as Bevy draws a Material2d, its values
set by name as a 3D material's are, and shader_material_2d is written with it. (`467efee0`)

![](.github/assets/examples/character_creation.webp)
![](.github/assets/examples/draggable_slider.webp)
![](.github/assets/examples/headless_tabs.webp)
A tab list's choice is observed as Bevy's ValueChange of an optional entity and kept by
Ui.SelfUpdate, an entity holds a pointer through a drag with Picking.CapturePointer, and
headless_tabs, draggable_slider and character_creation are written with them. (`01b5ac3e`)

![](.github/assets/examples/deferred_raymarch.webp)
A draw on a camera writes Bevy's G-buffer from a new point inside the prepass and casts shadows from
a shadow stage of its own, so a surface a shader finds by marching a ray is lit and shadowed by Bevy
as a standard material is, and deferred_raymarch is written with it. (`7849ecf6`)

Done that day

- Item 5 at `f12a5879`. Every method native code calls catches every exception.
- Item 6 at `fef6bbeb`. A template package, so `dotnet new` starts a game.
- Item 7 at `7a2a99ac`. The entities that lost a component since a system last ran.
- Item 8 at `5d955562`. Every example compiles on the package alone.
- Item 9 at `8f02d976`. A script that more than one system runs is read for the forms only GNU's
  tools or a later bash read.
- Item 10 at `d328fdc3`. Fixes for the generator's diagnostics offered in an editor.

## 2026-10-09

![](.github/assets/examples/ssr.webp)
A shader program can draw its material into the deferred buffers through a deferred stage, lit by
the deferred lighting pass and read by screen-space reflections, and the ssr example is written with
its water in Slang. (`2ed99011`)

## 2026-10-07

![](.github/assets/first-game/01.webp)
![](.github/assets/first-game/02.webp)
![](.github/assets/first-game/03.webp)
![](.github/assets/first-game/04.webp)
![](.github/assets/first-game/05.webp)
![](.github/assets/first-game/06.webp)
![](.github/assets/first-game/07.webp)
![](.github/assets/first-game/08.webp)
![](.github/assets/first-game/09.webp)
![](.github/assets/first-game/10.webp)
![](.github/assets/first-game/11.webp)
![](.github/assets/first-game/12.webp)
A first game is told from an empty folder in twelve steps, each a whole program the pack workflow
builds and runs and the page is held to line for line. (`7072511d`)

![](.github/assets/examples/delayed_commands.webp)
Commands can be queued to land after a delay, as Bevy's delayed commands do, and delayed_commands is
written. (`7f92ed03`)

![](.github/assets/examples/audio_control.webp)
![](.github/assets/examples/window_fallthrough.webp)
A playing sound's speed and mute are bridged with a way to ask for its sink, and audio_control and
window_fallthrough are written. (`1b8b2823`)

![](.github/assets/examples/logs.webp)
C# writes into Bevy's log at its levels and once a line of code, a run ends with a failure code, and
Bevy's logs example is written. (`ee229278`)

![](.github/assets/examples/tilemap_chunk.webp)
![](.github/assets/examples/tilemap_chunk_orientation.webp)
A tilemap chunk is made with its tiles and has them written and read from C#, and both of Bevy's
tilemap chunk examples are written. (`6bdfd85e`)

![](.github/assets/examples/mesh2d_arcs.webp)
Bevy's arc, sector and segment are in the math with their bounds, a sector or a segment mesh maps
its image at an angle, and mesh2d_arcs is written. (`ad9cc846`)

![](.github/assets/examples/multi_window_text.webp)
![](.github/assets/examples/multiple_windows.webp)
A camera is aimed at a window the game spawned, an offscreen run draws each such window into an
image of its own at its scale, and two of Bevy's window examples are written. (`0c50c042`)

![](.github/assets/examples/computed_states.webp)
![](.github/assets/examples/custom_transitions.webp)
![](.github/assets/examples/state_scoped.webp)
A state's transitions reach C# as Bevy's messages, an entity goes as a state is entered or by a rule
over the transition, a joint reads a computed state, and three of Bevy's state examples are written.
(`ea00b975`)

![](.github/assets/examples/cubic_splines.webp)
Hermite, cardinal, B-spline and Bezier splines are made into cubic curves sampled along as Bevy
makes them, and cubic_splines is written with them. (`9f5537a3`)

![](.github/assets/examples/bounding_2d.webp)
Shapes in the plane give boxes and circles about them as Bevy bounds them, rays and swept volumes
meet those as Bevy casts them, and bounding_2d is written with them. (`cbe9a1f6`)

## 2026-10-06

![](.github/assets/examples/random_sampling.webp)
A box and a ball are shapes as values that points are sampled in and on as Bevy samples them, and
random_sampling is written with them. (`a31e3b32`)

![](.github/assets/examples/directional_navigation.webp)
![](.github/assets/examples/directional_navigation_overrides.webp)
The input focus moves between interface nodes by direction with edges a game draws before the
nearest node, and directional_navigation and directional_navigation_overrides are written with it.
(`f127b1f9`)

![](.github/assets/examples/custom_skinned_mesh.webp)
A mesh made in code is skinned to joint entities with its joints and weights and a skin of inverse
bindposes, and custom_skinned_mesh is written with it. (`3208c707`)

![](.github/assets/examples/animation_graph.webp)
![](.github/assets/examples/animation_masks.webp)
An animation graph is built in code from blends and clips, played several nodes at once at their
weights and masked by groups of bones, and animation_graph and animation_masks are written with it.
(`5d5a982d`)

![](.github/assets/examples/animated_mesh_events.webp)
![](.github/assets/examples/animation_events.webp)
A game places its own events on an animation clip, made in code or loaded from a model, heard at the
player or at a target as the clip reaches them, and animation_events and animated_mesh_events are
written with them. (`2158ee21`)

![](.github/assets/examples/animated_transform.webp)
![](.github/assets/examples/animated_ui.webp)
An animation clip is built in code from curves sampled or eased, aimed at entities by name and
played from a graph, and animated_transform, animated_ui and the whole of eased_motion are written
with it. (`421d4e15`)

![](.github/assets/examples/font_query.webp)
![](.github/assets/examples/font_variations.webp)
![](.github/assets/examples/font_weights.webp)
![](.github/assets/examples/strikethrough_and_underline.webp)
![](.github/assets/examples/text.webp)
![](.github/assets/examples/text_background_colors.webp)
Text is drawn with a line under or through it and with a font's OpenType features and variable axes
from C#, and eight of Bevy's text examples are written or made whole with them. (`f0be6dab`)

![](.github/assets/examples/multiline_text_input.webp)
![](.github/assets/examples/multiple_text_inputs.webp)
A key reaching the focused entity is observed as Bevy's FocusedInput, an offscreen run's fields take
keys, the focus is given and moved from C#, and the two text input examples are written with them.
(`f46edecf`)

![](.github/assets/examples/standard_widgets_observers.webp)
A game observes Bevy's own components coming and going through their wrappers as it does its own,
and standard_widgets_observers is written on them. (`569ea59a`)

![](.github/assets/examples/standard_widgets.webp)
What Bevy's widgets report reaches C# as their own Activate, ValueChange and MenuEvent, and
standard_widgets is written on them. (`2597c127`)

![](.github/assets/examples/scroll.webp)
The pretend wheel is written into Bevy as a real wheel is, so a game, the interface and picking all
see it, and scroll is written with its own event carried up from picking's scroll. (`a683c3ee`)

![](.github/assets/examples/render_ui_to_texture.webp)
A game spawns pointers of its own and puts them on an image an interface is drawn into, a ray cast
answers the texture coordinate it met, and render_ui_to_texture is written with them. (`d59365fa`)

![](.github/assets/examples/drag_to_scroll.webp)
![](.github/assets/examples/viewport_node.webp)
drag_to_scroll and viewport_node are written on the pointer's events, a board scrolled by a drag and
a cube picked through the widget that shows it. (`4034f919`)

![](.github/assets/examples/dragdrop_picking.webp)
![](.github/assets/examples/entity_disabling.webp)
![](.github/assets/examples/sprite_picking.webp)
![](.github/assets/examples/ui_drag_and_drop.webp)
A game observes what a pointer does to an entity as Bevy's Pointer events, picking moves into the
render profile at 3 seconds and 0.6 percent more for a clean build, an offscreen run is pointed at
through its image, and four of Bevy's picking examples are written with it. (`b548987d`)

![](.github/assets/examples/irradiance_volumes.webp)
A Slang material reads the light an irradiance volume gives through Bevy's own function, an image
says how large it is, and irradiance_volumes and the whole of tonemapping are written with them.
(`1a4b8211`)

![](.github/assets/examples/clustered_decals.webp)
A Slang material reaches the clustered decals over it through Bevy's own iterator, laying them on as
the standard material does or reading each one's tag and textures, and clustered_decals is written
with it. (`4bbcec08`)

![](.github/assets/examples/bevymark.webp)
![](.github/assets/examples/bevymark_3d.webp)
![](.github/assets/examples/many_buttons.webp)
![](.github/assets/examples/many_cubes.webp)
![](.github/assets/examples/many_foxes.webp)
![](.github/assets/examples/many_morph_targets.webp)
![](.github/assets/examples/transform_hierarchy.webp)
The last seven of Bevy's stress tests are written and measured beside Bevy's programs, which empties
the column of examples that can be written. (`d71ff6a3`)

## 2026-10-05

![](.github/assets/examples/many_animated_sprites.webp)
![](.github/assets/examples/many_cameras_lights.webp)
![](.github/assets/examples/many_gizmos.webp)
![](.github/assets/examples/many_glyphs.webp)
![](.github/assets/examples/many_gradients.webp)
![](.github/assets/examples/many_lights.webp)
![](.github/assets/examples/many_materials.webp)
![](.github/assets/examples/many_sprites.webp)
![](.github/assets/examples/many_text.webp)
![](.github/assets/examples/many_text2d.webp)
![](.github/assets/examples/text_pipeline.webp)
Thirteen of Bevy's stress tests are written and measured beside Bevy's own programs run offscreen
through the bridge's harness, with what measuring found named in PERFORMANCE.md and two of its finds
fixed. (`1fc9c9c4`)

![](.github/assets/examples/gamepad_viewer.webp)
![](.github/assets/examples/hotpatching_systems.webp)
gamepad_viewer and hotpatching_systems are written, and log_diagnostics, world_serialization and
scene_viewer become rows of what is missing, which leaves the stress tests as the only rows that can
be written. (`687af0a6`)

![](.github/assets/examples/physics_in_fixed_timestep.webp)
physics_in_fixed_timestep is written with its player as a behavior, and Time.FixedOverstep says how
far the fixed clock has run past its last step. (`3e694f06`)

![](.github/assets/examples/clear_color.webp)
![](.github/assets/examples/minimizing.webp)
![](.github/assets/examples/persisting_window_settings.webp)
![](.github/assets/examples/resizing.webp)
![](.github/assets/examples/scale_factor_override.webp)
![](.github/assets/examples/screenshot.webp)
![](.github/assets/examples/transparent_window.webp)
![](.github/assets/examples/window_drag_move.webp)
![](.github/assets/examples/window_resizing.webp)
![](.github/assets/examples/window_settings.webp)
The window examples and Bevy's two window tests are written with wait and progress cursors added,
and three window rows are marked missing. (`13eb5211`)

![](.github/assets/examples/edit_material_on_gltf.webp)
![](.github/assets/examples/gltf_skinned_mesh.webp)
![](.github/assets/examples/load_gltf.webp)
![](.github/assets/examples/query_gltf_primitives.webp)
![](.github/assets/examples/update_gltf_scene.webp)
The five glTF examples and hello_world are written, and fps_overlay is marked missing for want of
Bevy's dev tools. (`bb440b9a`)

![](.github/assets/examples/context_menu.webp)
![](.github/assets/examples/cooldown.webp)
![](.github/assets/examples/debug_frustum_culling.webp)
![](.github/assets/examples/headless_renderer.webp)
![](.github/assets/examples/mesh_picking.webp)
![](.github/assets/examples/simple_picking.webp)
The application, usage and picking examples are written, with flat shapes extruded into solids and
the extrusion row of 3d_shapes. (`6b94ca28`)

![](.github/assets/examples/compute_mesh.webp)
![](.github/assets/examples/compute_shader_game_of_life.webp)
![](.github/assets/examples/custom_post_processing.webp)
![](.github/assets/examples/custom_shader_instancing.webp)
![](.github/assets/examples/fullscreen_material.webp)
![](.github/assets/examples/gpu_readback.webp)
![](.github/assets/examples/shader_prepass.webp)
The last seven shader examples are written, compute, passes and a draw on the camera among them,
with an image read back from the GPU. (`966f607c`)

![](.github/assets/examples/animate_shader.webp)
![](.github/assets/examples/array_texture.webp)
![](.github/assets/examples/automatic_instancing.webp)
![](.github/assets/examples/shader_defs.webp)
![](.github/assets/examples/shader_material.webp)
![](.github/assets/examples/shader_material_bindless.webp)
![](.github/assets/examples/shader_material_screenspace_texture.webp)
![](.github/assets/examples/storage_buffer.webp)
![](.github/assets/examples/texture_binding_array.webp)
Nine of the shader examples are written in Slang, with a mesh tag read through the instance index
Bevy hands a fragment shader and images loaded as array textures. (`e454755e`)

![](.github/assets/examples/2d_gizmos.webp)
![](.github/assets/examples/3d_gizmos.webp)
![](.github/assets/examples/axes.webp)
![](.github/assets/examples/light_gizmos.webp)
The four gizmo examples are written, with retained gizmos, depth bias and line style by group,
Bevy's light and bounding box gizmos, and the ellipses, rounded shapes and arrow options they draw.
(`75bf3eec`)

![](.github/assets/examples/alien_cake_addict.webp)
![](.github/assets/examples/breakout.webp)
![](.github/assets/examples/contributors.webp)
![](.github/assets/examples/desk_toy.webp)
![](.github/assets/examples/game_menu.webp)
![](.github/assets/examples/loading_screen.webp)
The six games of Bevy are written, with the primary window entity, a load state that counts
dependencies and pipeline readiness bridged for them, and an offscreen camera spawned after startup
now drawing. (`1ad2179c`)

![](.github/assets/examples/animated_mesh.webp)
![](.github/assets/examples/animated_mesh_control.webp)
![](.github/assets/examples/color_animation.webp)
![](.github/assets/examples/eased_motion.webp)
![](.github/assets/examples/easing_functions.webp)
![](.github/assets/examples/morph_targets.webp)
Writes the six animation examples, with a clip played a number of times and that number changed as
it plays. (`7b1e48f7`)

![](.github/assets/examples/alter_mesh.webp)
![](.github/assets/examples/alter_sprite.webp)
![](.github/assets/examples/asset_loading.webp)
![](.github/assets/examples/embedded_asset.webp)
![](.github/assets/examples/extra_asset_source.webp)
![](.github/assets/examples/generated_assets.webp)
![](.github/assets/examples/hot_asset_reloading.webp)
![](.github/assets/examples/multi_asset_sync.webp)
![](.github/assets/examples/repeated_texture.webp)
Writes the ten asset examples, with meshes and images changed in place from C#. (`fbc50452`)

![](.github/assets/examples/scrollbars.webp)
![](.github/assets/examples/vertical_slider.webp)
Reaches Bevy's widgets through wrappers and keeps their own state, writes immutable components by
reinserting them, and writes vertical_slider and scrollbars. (`bb508d9f`)

![](.github/assets/examples/editable_text_filter.webp)
![](.github/assets/examples/tab_navigation.webp)
![](.github/assets/examples/text_input.webp)
Builds Bevy's widgets and interface picking into the render profile, makes a node a text field from
C#, and writes editable_text_filter, text_input and tab_navigation. (`327f86a0`)

![](.github/assets/examples/2d_text_gizmos.webp)
![](.github/assets/examples/3d_text_gizmos.webp)
![](.github/assets/examples/anchored_text_gizmos.webp)
![](.github/assets/examples/text_gizmos_font.webp)
Draws text as gizmos in Bevy's stroke font, and writes the four text gizmo examples with it.
(`862d8e86`)

![](.github/assets/examples/mirror.webp)
Writes mirror with a lit Slang shader, culling the other face of a shader material under a camera
that inverts culling. (`f75836af`)

![](.github/assets/examples/extended_material.webp)
![](.github/assets/examples/extended_material_bindless.webp)
Lights a Slang shader with Bevy's own lighting through bcs::light and bcs::finish, and writes
extended_material and its bindless twin with it. (`6e036913`)

![](.github/assets/examples/meshlet.webp)
![](.github/assets/examples/solari.webp)
Writes meshlet and solari against a bridge built with them, adding the material that colors each
cluster and tracing rays against a mesh with no texture coordinates. (`7abb7ab9`)

![](.github/assets/examples/ui_scaling.webp)
![](.github/assets/examples/ui_texture_slice_flip_and_tile.webp)
Reaches Bevy's resources through the wrappers its components have, and writes ui_scaling and
ui_texture_slice_flip_and_tile, button's input focus and the 2D wireframe examples' global setting
with them. (`6a2e78aa`)

![](.github/assets/examples/2d_screen_shake.webp)
![](.github/assets/examples/2d_top_down_camera.webp)
![](.github/assets/examples/pixel_grid_snap.webp)
![](.github/assets/examples/spatial_audio_2d.webp)
Writes pixel_grid_snap, 2d_screen_shake, 2d_top_down_camera and spatial_audio_2d on the 2D meshes.
(`d1cfbdad`)

![](.github/assets/examples/2d_shapes.webp)
![](.github/assets/examples/2d_viewport_to_world.webp)
![](.github/assets/examples/bloom_2d.webp)
![](.github/assets/examples/mesh2d.webp)
![](.github/assets/examples/mesh2d_alpha_mode.webp)
![](.github/assets/examples/mesh2d_repeated_texture.webp)
![](.github/assets/examples/mesh2d_vertex_color_texture.webp)
![](.github/assets/examples/wireframe_2d.webp)
Draws 2D meshes with Bevy's color materials, rings and wireframes, writes eight of Bevy's 2D
examples with them, and gives an offscreen run its size from the first startup system. (`8e64d042`)

![](.github/assets/examples/tonemapping.webp)
Bevy's tonemapping example is written in C#, its test pattern shader in Slang. (`6766c94f`)

![](.github/assets/examples/anti_aliasing.webp)
Bevy's anti_aliasing example is written in C#. (`a72991c5`)

![](.github/assets/examples/light_probe_blending.webp)
Bevy's light_probe_blending example is written in C#. (`43037d16`)

![](.github/assets/examples/clustered_decal_maps.webp)
Bevy's clustered_decal_maps example is written in C#. (`39d40a4a`)

![](.github/assets/examples/light_textures.webp)
![](.github/assets/examples/reflection_probes.webp)
Light textures are drawn now that the bridge builds them, and Bevy's light_textures and
reflection_probes examples are written in C#. (`7618f24e`)

## 2026-10-04

![](.github/assets/examples/transmission.webp)
Bevy's transmission example is written in C#. (`0d30866e`)

![](.github/assets/examples/contact_shadows.webp)
Bevy's contact_shadows example is written in C#, its model spun by a press found under the cursor
with a ray. (`10978a66`)

![](.github/assets/examples/anisotropy.webp)
Bevy's anisotropy example is written in C#. (`5b74efe3`)

![](.github/assets/examples/color_grading.webp)
Bevy's color_grading example is written in C#. (`60ce9835`)

![](.github/assets/examples/camera_sub_view.webp)
Bevy's camera_sub_view example is written in C#, and atmosphere and deferred_rendering are marked as
waiting on what the bridge lacks. (`99fecf46`)

![](.github/assets/examples/blend_modes.webp)
Bevy's blend_modes example is written in C#. (`d9bb9ed1`)

![](.github/assets/examples/pccm.webp)
Bevy's pccm example is written in C#, with the files it loads from Bevy's asset_files repository
fetched beside the others. (`9508f00b`)

![](.github/assets/examples/visibility_range.webp)
A component holding a range of numbers can be written from C#, and Bevy's visibility_range example
is written with it. (`d951f507`)

![](.github/assets/examples/mesh_ray_cast.webp)
![](.github/assets/examples/shadow_biases.webp)
Bevy's mesh_ray_cast and shadow_biases examples are written in C#. (`f54a2848`)

![](.github/assets/examples/2d_on_ui.webp)
![](.github/assets/examples/3d_rotation.webp)
![](.github/assets/examples/3d_scene.webp)
![](.github/assets/examples/3d_shapes.webp)
![](.github/assets/examples/3d_viewport_to_world.webp)
![](.github/assets/examples/align.webp)
![](.github/assets/examples/anchor_layout.webp)
![](.github/assets/examples/animated_material.webp)
![](.github/assets/examples/async_channel_pattern.webp)
![](.github/assets/examples/async_compute.webp)
![](.github/assets/examples/atmospheric_fog.webp)
![](.github/assets/examples/audio.webp)
![](.github/assets/examples/auto_exposure.webp)
![](.github/assets/examples/bloom_3d.webp)
![](.github/assets/examples/borders.webp)
![](.github/assets/examples/box_shadow.webp)
![](.github/assets/examples/button.webp)
![](.github/assets/examples/camera_orbit.webp)
![](.github/assets/examples/clearcoat.webp)
![](.github/assets/examples/cpu_draw.webp)
![](.github/assets/examples/depth_of_field.webp)
![](.github/assets/examples/display_and_visibility.webp)
![](.github/assets/examples/drag_and_drop.webp)
![](.github/assets/examples/empty_defaults.webp)
![](.github/assets/examples/external_source_external_thread.webp)
![](.github/assets/examples/first_person_view_model.webp)
![](.github/assets/examples/flex_layout.webp)
![](.github/assets/examples/fog.webp)
![](.github/assets/examples/fog_volumes.webp)
![](.github/assets/examples/generate_custom_mesh.webp)
![](.github/assets/examples/gradients.webp)
![](.github/assets/examples/grid.webp)
![](.github/assets/examples/hierarchy.webp)
![](.github/assets/examples/image_node.webp)
![](.github/assets/examples/iter_combinations.webp)
![](.github/assets/examples/letter_spacing.webp)
![](.github/assets/examples/lighting.webp)
![](.github/assets/examples/lightmaps.webp)
![](.github/assets/examples/lines.webp)
![](.github/assets/examples/mixed_lighting.webp)
![](.github/assets/examples/motion_blur.webp)
![](.github/assets/examples/mouse_grab.webp)
![](.github/assets/examples/move_sprite.webp)
![](.github/assets/examples/observers.webp)
![](.github/assets/examples/one_shot_systems.webp)
![](.github/assets/examples/order_independent_transparency.webp)
![](.github/assets/examples/orthographic.webp)
![](.github/assets/examples/overflow.webp)
![](.github/assets/examples/overflow_clip_margin.webp)
![](.github/assets/examples/overflow_debug.webp)
![](.github/assets/examples/parallel_query.webp)
![](.github/assets/examples/parenting.webp)
![](.github/assets/examples/pbr.webp)
![](.github/assets/examples/pcss.webp)
![](.github/assets/examples/play_sound_effect.webp)
![](.github/assets/examples/post_processing.webp)
![](.github/assets/examples/projection_zoom.webp)
![](.github/assets/examples/rect_light.webp)
![](.github/assets/examples/relative_cursor_position.webp)
![](.github/assets/examples/removal_detection.webp)
![](.github/assets/examples/render_to_texture.webp)
![](.github/assets/examples/rotate_environment_map.webp)
![](.github/assets/examples/rotate_to_cursor.webp)
![](.github/assets/examples/rotation.webp)
![](.github/assets/examples/scale.webp)
![](.github/assets/examples/scrolling_fog.webp)
![](.github/assets/examples/settings.webp)
![](.github/assets/examples/shadow_caster_receiver.webp)
![](.github/assets/examples/size_constraints.webp)
![](.github/assets/examples/skybox.webp)
![](.github/assets/examples/smooth_follow.webp)
![](.github/assets/examples/soundtrack.webp)
![](.github/assets/examples/spatial_audio_3d.webp)
![](.github/assets/examples/spherical_area_lights.webp)
![](.github/assets/examples/split_screen.webp)
![](.github/assets/examples/spotlight.webp)
![](.github/assets/examples/sprite.webp)
![](.github/assets/examples/sprite_animation.webp)
![](.github/assets/examples/sprite_flipping.webp)
![](.github/assets/examples/sprite_scale.webp)
![](.github/assets/examples/sprite_sheet.webp)
![](.github/assets/examples/sprite_slice.webp)
![](.github/assets/examples/sprite_tile.webp)
![](.github/assets/examples/ssao.webp)
![](.github/assets/examples/stacked_gradients.webp)
![](.github/assets/examples/states.webp)
![](.github/assets/examples/sub_states.webp)
![](.github/assets/examples/text2d.webp)
![](.github/assets/examples/text_debug.webp)
![](.github/assets/examples/text_wrap_debug.webp)
![](.github/assets/examples/texture.webp)
![](.github/assets/examples/transform.webp)
![](.github/assets/examples/translation.webp)
![](.github/assets/examples/transparency_2d.webp)
![](.github/assets/examples/transparency_3d.webp)
![](.github/assets/examples/transparency_ui.webp)
![](.github/assets/examples/two_passes.webp)
![](.github/assets/examples/ui_target_camera.webp)
![](.github/assets/examples/ui_texture_atlas.webp)
![](.github/assets/examples/ui_texture_atlas_slice.webp)
![](.github/assets/examples/ui_texture_slice.webp)
![](.github/assets/examples/ui_transform.webp)
![](.github/assets/examples/vertex_colors.webp)
![](.github/assets/examples/virtual_time.webp)
![](.github/assets/examples/volumetric_fog.webp)
![](.github/assets/examples/wireframe.webp)
![](.github/assets/examples/z_index.webp)
Every example is captured at the size Bevy draws it and kept as WebP, lossless for flat examples and
lossy for 3D ones. (`9f798158`)
