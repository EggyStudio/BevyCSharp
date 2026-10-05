# Examples

Bevy 0.19.1 has 421 examples, 408 of them in the list its `examples/README.md` keeps and 13 kept out of it. Each is a row here, made by `build/examples-table.py` from Bevy's own metadata, so a row is a feature of Bevy and the table is how much of Bevy a C# game can reach.

An example written here is a program in `BevyCSharp.Examples`, under Bevy's name, opened by `dotnet run --project BevyCSharp.Examples -- <name>` or `./bcs open --example <name>`. One `written in part` leaves out a feature of Bevy's the bridge lacks and names it. One that `can be written` uses only what is bridged and waits for its turn. One that is `missing` names what the bridge lacks, and one that `does not apply` says why it is not a thing a C# game does, most often because it is about Rust itself. A difference that is no feature, such as a view sized for another window, is said in a written row and keeps it written.

**142 written, 8 written in part, 101 can be written, 113 missing and 57 do not apply.** Of the 364 that apply, 251 can be written with what is bridged, 8 of them leaving something out.

| Group | Written | Written in part | Can be written | Missing | Does not apply |
|---|---:|---:|---:|---:|---:|
| [2D Rendering](#2d-rendering) | 20 | 1 | 1 | 6 | 1 |
| [3D Rendering](#3d-rendering) | 51 | 4 | 2 | 10 | 0 |
| [Animation](#animation) | 0 | 0 | 6 | 7 | 0 |
| [Application](#application) | 7 | 0 | 3 | 2 | 7 |
| [Assets](#assets) | 0 | 0 | 10 | 2 | 5 |
| [Async Tasks](#async-tasks) | 3 | 0 | 0 | 0 | 0 |
| [Audio](#audio) | 4 | 0 | 1 | 3 | 0 |
| [Camera](#camera) | 4 | 0 | 2 | 3 | 0 |
| [Dev tools](#dev-tools) | 0 | 0 | 1 | 1 | 1 |
| [Diagnostics](#diagnostics) | 0 | 0 | 1 | 2 | 0 |
| [ECS (Entity Component System)](#ecs-entity-component-system) | 14 | 0 | 1 | 11 | 9 |
| [Embedded](#embedded) | 0 | 0 | 0 | 0 | 1 |
| [Games](#games) | 0 | 0 | 6 | 0 | 0 |
| [Gizmos](#gizmos) | 0 | 0 | 4 | 5 | 0 |
| [Helpers](#helpers) | 0 | 0 | 0 | 1 | 0 |
| [Input](#input) | 6 | 1 | 0 | 5 | 0 |
| [Math](#math) | 1 | 0 | 0 | 4 | 1 |
| [Movement](#movement) | 0 | 0 | 1 | 0 | 0 |
| [Picking](#picking) | 0 | 0 | 2 | 3 | 1 |
| [Reflection](#reflection) | 0 | 0 | 0 | 0 | 9 |
| [Remote Protocol](#remote-protocol) | 0 | 0 | 0 | 3 | 1 |
| [Scene](#scene) | 0 | 0 | 1 | 1 | 0 |
| [Shaders](#shaders) | 0 | 0 | 15 | 5 | 5 |
| [Shaders Advanced](#shaders-advanced) | 0 | 0 | 1 | 0 | 1 |
| [State](#state) | 2 | 0 | 0 | 2 | 0 |
| [Stress Tests](#stress-tests) | 0 | 0 | 20 | 0 | 1 |
| [Time](#time) | 2 | 0 | 0 | 0 | 1 |
| [Tools](#tools) | 0 | 0 | 2 | 0 | 0 |
| [Transforms](#transforms) | 5 | 0 | 0 | 0 | 0 |
| [UI (User Interface)](#ui-user-interface) | 23 | 2 | 0 | 35 | 0 |
| [Usage](#usage) | 0 | 0 | 3 | 0 | 0 |
| [Window](#window) | 0 | 0 | 10 | 1 | 0 |
| [glTF](#gltf) | 0 | 0 | 5 | 1 | 3 |
| [Kept out of Bevy's list](#kept-out-of-bevys-list) | 0 | 0 | 3 | 0 | 10 |
| **All** | **142** | **8** | **101** | **113** | **57** |

A row's example links to Bevy's source, at the release the bridge builds. A written one's state links to its program here, and its capture is in `.github/assets/examples`, a picture of what it draws or, for one with nothing to draw, the text it prints. Every picture is drawn at Bevy's window of 1280 by 720, or the size the example asks for, and kept at that size as WebP, lossless for a 2D or interface example and at quality 85 for a 3D one, so a label reads as Bevy draws it and a sky does not band. Where Bevy's site runs the example in a browser, the row links it live, which is Bevy's Rust original rather than the C# one here.

## 2D Rendering

| Example | What it shows | State |
|---|---|---|
| [`2d_shapes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/2d_shapes.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/2d-shapes/) | Renders simple 2D primitive shapes like circles and polygons | [written](../BevyCSharp.Examples/2d/2d_shapes.cs), through Bevy's reflected Wireframe2d, put on each mesh where Bevy's sets its global one |
| [`2d_viewport_to_world`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/2d_viewport_to_world.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/2d-viewport-to-world/) | Demonstrates how to use the `Camera::viewport_to_world_2d` method with a dynamic viewport and camera. | [written](../BevyCSharp.Examples/2d/2d_viewport_to_world.cs), zoomed by the camera's scale where Bevy's sets its projection's, which sits beside a scaling mode no wrapper types |
| [`bloom_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/bloom_2d.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/bloom-2d/) | Illustrates bloom post-processing in 2d | [written](../BevyCSharp.Examples/2d/bloom_2d.cs), through Bevy's reflected Bloom |
| [`cpu_draw`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/cpu_draw.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/cpu-draw/) | Manually read/write the pixels of a texture | [written](../BevyCSharp.Examples/2d/cpu_draw.cs) |
| [`dynamic_mip_generation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/dynamic_mip_generation.rs) | Demonstrates use of the mipmap generation plugin to generate mipmaps for a texture | missing, generating an image's mipmaps on the GPU |
| [`mesh2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/mesh2d.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/mesh2d/) | Renders a 2d mesh | [written](../BevyCSharp.Examples/2d/mesh2d.cs) |
| [`mesh2d_alpha_mode`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/mesh2d_alpha_mode.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/mesh2d-alpha-mode/) | Used to test alpha modes with mesh2d | [written](../BevyCSharp.Examples/2d/mesh2d_alpha_mode.cs) |
| [`mesh2d_arcs`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/mesh2d_arcs.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/mesh2d-arcs/) | Demonstrates UV-mapping of the circular segment and sector primitives | missing, the angle a sector's or a segment's mesh maps its image at |
| [`mesh2d_manual`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/mesh2d_manual.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/mesh2d-manual/) | Renders a custom mesh "manually" with "mid-level" renderer apis | does not apply, writes a render pipeline in Rust with the mid-level render API |
| [`mesh2d_repeated_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/mesh2d_repeated_texture.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/mesh2d-repeated-texture/) | Showcase of using `uv_transform` on the `ColorMaterial` of a `Mesh2d` | [written](../BevyCSharp.Examples/2d/mesh2d_repeated_texture.cs) |
| [`mesh2d_vertex_color_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/mesh2d_vertex_color_texture.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/mesh2d-vertex-color-texture/) | Renders a 2d mesh with vertex color attributes | [written](../BevyCSharp.Examples/2d/mesh2d_vertex_color_texture.cs) |
| [`move_sprite`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/move_sprite.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/move-sprite/) | Changes the transform of a sprite | [written](../BevyCSharp.Examples/2d/move_sprite.cs) |
| [`multi_window_text`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/multi_window_text.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/multi-window-text/) | Renders text to multiple windows with different scale factors using both Text and Text2d | missing, a second window |
| [`pixel_grid_snap`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/pixel_grid_snap.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/pixel-grid-snap/) | Shows how to create graphics that snap to the pixel grid by rendering to a texture in 2D | can be written |
| [`rotate_to_cursor`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/rotate_to_cursor.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/rotate-to-cursor/) | Demonstrates rotating entities in 2D to follow the cursor | [written](../BevyCSharp.Examples/2d/rotate_to_cursor.cs) |
| [`rotation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/rotation.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/rotation/) | Demonstrates rotating entities in 2D with quaternions | [written](../BevyCSharp.Examples/2d/rotation.cs) |
| [`sprite`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/sprite.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/sprite/) | Renders a sprite | [written](../BevyCSharp.Examples/2d/sprite.cs) |
| [`sprite_animation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/sprite_animation.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/sprite-animation/) | Animates a sprite in response to an event | [written](../BevyCSharp.Examples/2d/sprite_animation.cs) |
| [`sprite_flipping`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/sprite_flipping.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/sprite-flipping/) | Renders a sprite flipped along an axis | [written](../BevyCSharp.Examples/2d/sprite_flipping.cs) |
| [`sprite_scale`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/sprite_scale.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/sprite-scale/) | Shows how a sprite can be scaled into a rectangle while keeping the aspect ratio | [written](../BevyCSharp.Examples/2d/sprite_scale.cs) |
| [`sprite_sheet`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/sprite_sheet.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/sprite-sheet/) | Renders an animated sprite | [written](../BevyCSharp.Examples/2d/sprite_sheet.cs) |
| [`sprite_slice`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/sprite_slice.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/sprite-slice/) | Showcases slicing sprites into sections that can be scaled independently via the 9-patch technique | [written](../BevyCSharp.Examples/2d/sprite_slice.cs) |
| [`sprite_tile`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/sprite_tile.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/sprite-tile/) | Renders a sprite tiled in a grid | [written](../BevyCSharp.Examples/2d/sprite_tile.cs) |
| [`text2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/text2d.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/text2d/) | Generates text in 2D | [written in part](../BevyCSharp.Examples/2d/text2d.cs), the underline under the first box's text |
| [`texture_atlas`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/texture_atlas.rs) | Generates a texture atlas (sprite sheet) from individual sprites | missing, an atlas built from a folder of images as the app runs |
| [`tilemap_chunk`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/tilemap_chunk.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/tilemap-chunk/) | Renders a tilemap chunk | missing, an image loaded as an array of layers, which Bevy's TilemapChunk draws its tiles from |
| [`tilemap_chunk_orientation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/tilemap_chunk_orientation.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/tilemap-chunk-orientation/) | Renders a tilemap chunk using tile orientations (mirrored, rotated) | missing, an image loaded as an array of layers, a row of tiles to a layer |
| [`transparency_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/transparency_2d.rs), [live in Bevy](https://bevy.org/examples/2d-rendering/transparency-2d/) | Demonstrates transparency in 2d | [written](../BevyCSharp.Examples/2d/transparency_2d.cs) |
| [`wireframe_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/wireframe_2d.rs) | Showcases wireframes for 2d meshes | [written](../BevyCSharp.Examples/2d/wireframe_2d.cs), through Bevy's reflected Wireframe2d, the global one stood in for by the mesh it reaches |

## 3D Rendering

| Example | What it shows | State |
|---|---|---|
| [`3d_scene`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/3d_scene.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/3d-scene/) | Simple 3D scene with basic shapes and lighting | [written](../BevyCSharp.Examples/3d/3d_scene.cs) |
| [`3d_shapes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/3d_shapes.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/3d-shapes/) | A scene showcasing the built-in 3D shapes | [written in part](../BevyCSharp.Examples/3d/3d_shapes.cs), the segment, the polyline and the extrusions, which are shapes the bridge does not build |
| [`3d_viewport_to_world`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/3d_viewport_to_world.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/3d-viewport-to-world/) | Demonstrates how to use the `Camera::viewport_to_world` method | [written](../BevyCSharp.Examples/3d/3d_viewport_to_world.cs) |
| [`animated_material`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/animated_material.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/animated-material/) | Shows how to animate material properties | [written](../BevyCSharp.Examples/3d/animated_material.cs) |
| [`anisotropy`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/anisotropy.rs) | Displays an example model with anisotropy | [written](../BevyCSharp.Examples/3d/anisotropy.cs) |
| [`anti_aliasing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/anti_aliasing.rs) | Compares different anti-aliasing techniques supported by Bevy | [written](../BevyCSharp.Examples/3d/anti_aliasing.cs) |
| [`atmosphere`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/atmosphere.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/atmosphere/) | A scene showcasing pbr atmospheric scattering | missing, Bevy's Mars scattering medium and its choice of rendering method, and the water's standard material extended by a shader of the example's own |
| [`atmospheric_fog`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/atmospheric_fog.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/atmospheric-fog/) | A scene showcasing the atmospheric fog effect | [written](../BevyCSharp.Examples/3d/atmospheric_fog.cs), through Bevy's reflected DistanceFog |
| [`auto_exposure`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/auto_exposure.rs) | A scene showcasing auto exposure | [written](../BevyCSharp.Examples/3d/auto_exposure.cs) |
| [`blend_modes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/blend_modes.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/blend-modes/) | Showcases different blend modes | [written](../BevyCSharp.Examples/3d/blend_modes.cs) |
| [`bloom_3d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/bloom_3d.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/bloom-3d/) | Illustrates bloom configuration using HDR and emissive materials | [written](../BevyCSharp.Examples/3d/bloom_3d.cs) |
| [`camera_sub_view`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/camera_sub_view.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/camera-sub-view/) | Demonstrates using different sub view effects on a camera | [written](../BevyCSharp.Examples/3d/camera_sub_view.cs) |
| [`clearcoat`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/clearcoat.rs) | Demonstrates the clearcoat PBR feature | [written](../BevyCSharp.Examples/3d/clearcoat.cs) |
| [`clustered_decal_maps`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/clustered_decal_maps.rs) | Demonstrates normal and metallic-roughness maps of decals | [written](../BevyCSharp.Examples/3d/clustered_decal_maps.cs) |
| [`clustered_decals`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/clustered_decals.rs) | Demonstrates clustered decals | missing, a standard material extended by a shader of the example's own, which reads the decal's tag |
| [`color_grading`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/color_grading.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/color-grading/) | Demonstrates color grading | [written](../BevyCSharp.Examples/3d/color_grading.cs) |
| [`contact_shadows`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/contact_shadows.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/contact-shadows/) | Showcases how contact shadows add shadow detail | [written](../BevyCSharp.Examples/3d/contact_shadows.cs), its model spun by a press over it found with a ray, where Bevy observes picking's drag |
| [`decal`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/decal.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/decal/) | Decal rendering | missing, forward decals (ForwardDecal) |
| [`deferred_rendering`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/deferred_rendering.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/deferred-rendering/) | Renders meshes with both forward and deferred pipelines | missing, a material's depth map for parallax mapping, and a material drawn forward while the rest are deferred |
| [`depth_of_field`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/depth_of_field.rs) | Demonstrates depth of field | [written](../BevyCSharp.Examples/3d/depth_of_field.cs) |
| [`fog`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/fog.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/fog/) | A scene showcasing the distance fog effect | [written](../BevyCSharp.Examples/3d/fog.cs), through Bevy's reflected DistanceFog |
| [`fog_volumes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/fog_volumes.rs) | Demonstrates fog volumes | [written](../BevyCSharp.Examples/3d/fog_volumes.cs), through Bevy's reflected FogVolume |
| [`generate_custom_mesh`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/generate_custom_mesh.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/generate-custom-mesh/) | Simple showcase of how to generate a custom mesh with a custom texture | [written](../BevyCSharp.Examples/3d/generate_custom_mesh.cs) |
| [`irradiance_volumes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/irradiance_volumes.rs) | Demonstrates irradiance volumes | missing, Bevy's irradiance volume asset, and a standard material extended by a shader of the example's own to draw its voxels |
| [`light_probe_blending`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/light_probe_blending.rs) | Demonstrates blending between multiple reflection probes | [written](../BevyCSharp.Examples/3d/light_probe_blending.cs) |
| [`light_textures`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/light_textures.rs) | Demonstrates light textures | [written](../BevyCSharp.Examples/3d/light_textures.cs) |
| [`lighting`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/lighting.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/lighting/) | Illustrates various lighting options in a simple scene | [written](../BevyCSharp.Examples/3d/lighting.cs) |
| [`lightmaps`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/lightmaps.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/lightmaps/) | Rendering a scene with baked lightmaps | [written](../BevyCSharp.Examples/3d/lightmaps.cs), through Bevy's reflected Lightmap and a material's LightmapExposure |
| [`lines`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/lines.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/lines/) | Create a custom material to draw 3d lines | [written](../BevyCSharp.Examples/3d/lines.cs) |
| [`mesh_ray_cast`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/mesh_ray_cast.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/mesh-ray-cast/) | Demonstrates ray casting with the `MeshRayCast` system parameter | [written](../BevyCSharp.Examples/3d/mesh_ray_cast.cs) |
| [`meshlet`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/meshlet.rs) | Meshlet rendering for dense high-poly scenes (experimental) | can be written, needs a bridge built with --meshlet |
| [`mirror`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/mirror.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/mirror/) | Demonstrates how to create a mirror with a second camera | missing, the mirror, a standard material extended by a shader of the example's own that samples what a second camera draws in screen space |
| [`mixed_lighting`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/mixed_lighting.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/mixed-lighting/) | Demonstrates how to combine baked and dynamic lighting | [written](../BevyCSharp.Examples/3d/mixed_lighting.cs), through Bevy's reflected Lightmap |
| [`motion_blur`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/motion_blur.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/motion-blur/) | Demonstrates per-pixel motion blur | [written](../BevyCSharp.Examples/3d/motion_blur.cs) |
| [`occlusion_culling`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/occlusion_culling.rs) | Demonstration of Occlusion Culling | missing, the counts of meshes drawn and culled, which it reads back from the render world's indirect draw buffers |
| [`order_independent_transparency`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/order_independent_transparency.rs) | Demonstrates how to use OIT | [written](../BevyCSharp.Examples/3d/order_independent_transparency.cs) |
| [`orthographic`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/orthographic.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/orthographic/) | Shows how to create a 3D orthographic view (for isometric-look in games or CAD applications) | [written](../BevyCSharp.Examples/3d/orthographic.cs) |
| [`parallax_mapping`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/parallax_mapping.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/parallax-mapping/) | Demonstrates use of a normal map and depth map for parallax mapping | missing, a depth map and parallax settings on the standard material |
| [`parenting`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/parenting.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/parenting/) | Demonstrates parent->child relationships and relative transformations | [written](../BevyCSharp.Examples/3d/parenting.cs) |
| [`pbr`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/pbr.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/pbr/) | Demonstrates use of Physically Based Rendering (PBR) properties | [written](../BevyCSharp.Examples/3d/pbr.cs), its view sized for a window of 1280 by 720 at any size |
| [`pccm`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/pccm.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/pccm/) | Demonstrates parallax-corrected cubemap reflections | [written](../BevyCSharp.Examples/3d/pccm.cs) |
| [`pcss`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/pcss.rs) | Demonstrates percentage-closer soft shadows (PCSS) | [written](../BevyCSharp.Examples/3d/pcss.cs) |
| [`post_processing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/post_processing.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/post-processing/) | Demonstrates the built-in postprocessing features | [written](../BevyCSharp.Examples/3d/post_processing.cs) |
| [`rect_light`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/rect_light.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/rect-light/) | Simple 3D scene demonstrating rectangular area lights. | [written](../BevyCSharp.Examples/3d/rect_light.cs), through Bevy's reflected RectLight |
| [`reflection_probes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/reflection_probes.rs) | Demonstrates reflection probes | [written](../BevyCSharp.Examples/3d/reflection_probes.cs) |
| [`render_to_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/render_to_texture.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/render-to-texture/) | Shows how to render to a texture, useful for mirrors, UI, or exporting images | [written](../BevyCSharp.Examples/3d/render_to_texture.cs) |
| [`rotate_environment_map`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/rotate_environment_map.rs) | Demonstrates how to rotate the skybox and the environment map simultaneously | [written](../BevyCSharp.Examples/3d/rotate_environment_map.cs) |
| [`scrolling_fog`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/scrolling_fog.rs) | Demonstrates how to create the effect of fog moving in the wind | [written](../BevyCSharp.Examples/3d/scrolling_fog.cs), through Bevy's reflected FogVolume, its density texture set as a reflected asset |
| [`shadow_biases`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/shadow_biases.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/shadow-biases/) | Demonstrates how shadow biases affect shadows in a 3d scene | [written](../BevyCSharp.Examples/3d/shadow_biases.cs) |
| [`shadow_caster_receiver`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/shadow_caster_receiver.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/shadow-caster-receiver/) | Demonstrates how to prevent meshes from casting/receiving shadows in a 3d scene | [written](../BevyCSharp.Examples/3d/shadow_caster_receiver.cs) |
| [`skybox`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/skybox.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/skybox/) | Load a cubemap texture onto a cube like a skybox and cycle through different compressed texture formats. | [written in part](../BevyCSharp.Examples/3d/skybox.cs), the ASTC and ETC2 cubemaps, since the bridge does not say which compressed formats the GPU decodes |
| [`solari`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/solari.rs) | Demonstrates realtime dynamic raytraced lighting using Bevy Solari. | can be written, needs a bridge built with --solari and an adapter with ray queries |
| [`specular_tint`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/specular_tint.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/specular-tint/) | Demonstrates specular tints and maps | missing, specular tint and the specular maps on the standard material |
| [`spherical_area_lights`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/spherical_area_lights.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/spherical-area-lights/) | Demonstrates how point light radius values affect light behavior | [written](../BevyCSharp.Examples/3d/spherical_area_lights.cs) |
| [`split_screen`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/split_screen.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/split-screen/) | Demonstrates how to render two cameras to the same window to accomplish "split screen" | [written](../BevyCSharp.Examples/3d/split_screen.cs) |
| [`spotlight`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/spotlight.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/spotlight/) | Illustrates spot lights | [written](../BevyCSharp.Examples/3d/spotlight.cs), its cubes scattered by .NET's generator rather than Bevy's |
| [`ssao`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/ssao.rs) | A scene showcasing screen space ambient occlusion | [written](../BevyCSharp.Examples/3d/ssao.cs) |
| [`ssr`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/ssr.rs) | Demonstrates screen space reflections with water ripples | missing, the water, a standard material extended by a shader of the example's own |
| [`texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/texture.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/texture/) | Shows configuration of texture materials | [written](../BevyCSharp.Examples/3d/texture.cs) |
| [`tonemapping`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/tonemapping.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/tonemapping/) | Compares tonemapping options | [written in part](../BevyCSharp.Examples/3d/tonemapping.cs), the image viewer's square sized to the dropped image, since the bridge does not say how large an image is |
| [`transmission`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/transmission.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/transmission/) | Showcases light transmission in the PBR material | [written](../BevyCSharp.Examples/3d/transmission.cs) |
| [`transparency_3d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/transparency_3d.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/transparency-3d/) | Demonstrates transparency in 3d | [written in part](../BevyCSharp.Examples/3d/transparency_3d.cs), its alpha to coverage cube, a mode the bridge's materials do not have |
| [`two_passes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/two_passes.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/two-passes/) | Renders two 3d passes to the same window from different perspectives | [written](../BevyCSharp.Examples/3d/two_passes.cs) |
| [`vertex_colors`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/vertex_colors.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/vertex-colors/) | Shows the use of vertex colors | [written](../BevyCSharp.Examples/3d/vertex_colors.cs) |
| [`visibility_range`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/visibility_range.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/visibility-range/) | Demonstrates visibility ranges | [written](../BevyCSharp.Examples/3d/visibility_range.cs) |
| [`volumetric_fog`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/volumetric_fog.rs), [live in Bevy](https://bevy.org/examples/3d-rendering/volumetric-fog/) | Demonstrates volumetric fog and lighting | [written](../BevyCSharp.Examples/3d/volumetric_fog.cs), through Bevy's reflected VolumetricFog, FogVolume and VolumetricLight |
| [`wireframe`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/wireframe.rs) | Showcases wireframe rendering | [written](../BevyCSharp.Examples/3d/wireframe.cs) |

## Animation

| Example | What it shows | State |
|---|---|---|
| [`animated_mesh`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animated_mesh.rs), [live in Bevy](https://bevy.org/examples/animation/animated-mesh/) | Plays an animation on a skinned glTF model of a fox | can be written |
| [`animated_mesh_control`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animated_mesh_control.rs), [live in Bevy](https://bevy.org/examples/animation/animated-mesh-control/) | Plays an animation from a skinned glTF with keyboard controls | can be written |
| [`animated_mesh_events`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animated_mesh_events.rs), [live in Bevy](https://bevy.org/examples/animation/animated-mesh-events/) | Plays an animation from a skinned glTF with events | missing, events placed on an animation clip |
| [`animated_transform`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animated_transform.rs), [live in Bevy](https://bevy.org/examples/animation/animated-transform/) | Create and play an animation defined by code that operates on the `Transform` component | missing, animation clips built in code (AnimationClip with curves) |
| [`animated_ui`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animated_ui.rs), [live in Bevy](https://bevy.org/examples/animation/animated-ui/) | Shows how to use animation clips to animate UI properties | missing, animation clips built in code that drive interface properties |
| [`animation_events`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animation_events.rs), [live in Bevy](https://bevy.org/examples/animation/animation-events/) | Demonstrate how to use animation events | missing, events placed on an animation clip |
| [`animation_graph`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animation_graph.rs), [live in Bevy](https://bevy.org/examples/animation/animation-graph/) | Blends multiple animations together with a graph | missing, animation graphs that blend clips by weight |
| [`animation_masks`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animation_masks.rs), [live in Bevy](https://bevy.org/examples/animation/animation-masks/) | Demonstrates animation masks | missing, animation masks on a graph |
| [`color_animation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/color_animation.rs), [live in Bevy](https://bevy.org/examples/animation/color-animation/) | Demonstrates how to animate colors using mixing and splines in different color spaces | can be written, mixing and splines over colors, written with the managed math |
| [`custom_skinned_mesh`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/custom_skinned_mesh.rs), [live in Bevy](https://bevy.org/examples/animation/custom-skinned-mesh/) | Skinned mesh example with mesh and joints data defined in code | missing, skinned meshes built in code (joints and weights) |
| [`eased_motion`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/eased_motion.rs), [live in Bevy](https://bevy.org/examples/animation/eased-motion/) | Demonstrates the application of easing curves to animate an object | can be written |
| [`easing_functions`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/easing_functions.rs), [live in Bevy](https://bevy.org/examples/animation/easing-functions/) | Showcases the built-in easing functions | can be written |
| [`morph_targets`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/morph_targets.rs), [live in Bevy](https://bevy.org/examples/animation/morph-targets/) | Plays an animation from a glTF file with meshes with morph targets | can be written, through Bevy's reflected MorphWeights |

## Application

| Example | What it shows | State |
|---|---|---|
| [`custom_loop`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/custom_loop.rs) | Demonstrates how to create a custom runner (to update an app manually) | does not apply, replaces Bevy's runner, which the bridge owns |
| [`drag_and_drop`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/drag_and_drop.rs) | An example that shows how to handle drag and drop in an app | [written](../BevyCSharp.Examples/app/drag_and_drop.cs) |
| [`empty`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/empty.rs) | An empty application (does nothing) | [written](../BevyCSharp.Examples/app/empty.cs), prints [its output](assets/examples/empty.txt) |
| [`empty_defaults`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/empty_defaults.rs), [live in Bevy](https://bevy.org/examples/application/empty-defaults/) | An empty application with default plugins | [written](../BevyCSharp.Examples/app/empty_defaults.cs) |
| [`externally_driven_headless_renderer`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/externally_driven_headless_renderer.rs) | Using bevy with manually driven update to render images | does not apply, drives Bevy's update from Rust code |
| [`headless`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/headless.rs) | An application that runs without default plugins | can be written |
| [`headless_renderer`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/headless_renderer.rs) | An application that runs with no window, but renders into image file | can be written |
| [`log_layers`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/log_layers.rs) | Illustrate how to add custom log layers | does not apply, adds a tracing layer, written in Rust |
| [`log_layers_ecs`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/log_layers_ecs.rs) | Illustrate how to transfer data between log layers and Bevy's ECS | does not apply, adds a tracing layer, written in Rust |
| [`logs`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/logs.rs), [live in Bevy](https://bevy.org/examples/application/logs/) | Illustrate how to use generate log output | missing, Bevy's log written from C# at its levels, and a message logged once |
| [`no_renderer`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/no_renderer.rs) | An application that runs with default plugins and displays an empty window, but without an actual renderer | does not apply, builds Bevy's renderer with no graphics backend, which the bridge sets up |
| [`persisting_window_settings`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/persisting_window_settings.rs) | Demonstrates saving window position settings | can be written |
| [`plugin`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/plugin.rs), [live in Bevy](https://bevy.org/examples/application/plugin/) | Demonstrates the creation and registration of a custom plugin | [written](../BevyCSharp.Examples/app/plugin.cs), prints [its output](assets/examples/plugin.txt) |
| [`plugin_group`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/plugin_group.rs), [live in Bevy](https://bevy.org/examples/application/plugin-group/) | Demonstrates the creation and registration of a custom plugin group | [written](../BevyCSharp.Examples/app/plugin_group.cs), prints [its output](assets/examples/plugin_group.txt) |
| [`render_recovery`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/render_recovery.rs) | Demonstrates how bevy can recover from rendering failures. | missing, recovering from a lost GPU device |
| [`return_after_run`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/return_after_run.rs) | Show how to return to main after the Bevy app has exited | [written](../BevyCSharp.Examples/app/return_after_run.cs), prints [its output](assets/examples/return_after_run.txt) |
| [`settings`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/settings.rs), [live in Bevy](https://bevy.org/examples/application/settings/) | Demonstrates persistence of settings | [written](../BevyCSharp.Examples/app/settings.cs) |
| [`thread_pool_resources`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/thread_pool_resources.rs) | Creates and customizes the internal thread pool | does not apply, tunes Bevy's task pools, which the bridge sets up |
| [`without_winit`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/without_winit.rs) | Create an application without winit (runs single time, no event loop) | does not apply, takes winit out of Bevy's plugins, which the bridge chooses |

## Assets

| Example | What it shows | State |
|---|---|---|
| [`alter_mesh`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/alter_mesh.rs) | Shows how to modify the underlying asset of a Mesh after spawning. | can be written |
| [`alter_sprite`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/alter_sprite.rs) | Shows how to modify texture assets after spawning. | can be written |
| [`asset_decompression`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/asset_decompression.rs) | Demonstrates loading a compressed asset | does not apply, writes a Rust asset loader |
| [`asset_loading`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/asset_loading.rs) | Demonstrates various methods to load assets | can be written |
| [`asset_processing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/processing/asset_processing.rs) | Demonstrates how to process and load custom assets | does not apply, writes Rust asset processors |
| [`asset_saving`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/asset_saving.rs), [live in Bevy](https://bevy.org/examples/assets/asset-saving/) | Demonstrates how to save an asset | does not apply, writes a Rust asset saver |
| [`asset_saving_with_subassets`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/asset_saving_with_subassets.rs), [live in Bevy](https://bevy.org/examples/assets/asset-saving-with-subassets/) | Demonstrates how to save an asset with subassets | does not apply, writes a Rust asset saver |
| [`asset_settings`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/asset_settings.rs) | Demonstrates various methods of applying settings when loading an asset | missing, settings given to a loader per load, such as an image's sampler |
| [`custom_asset`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/custom_asset.rs), [live in Bevy](https://bevy.org/examples/assets/custom-asset/) | Implements a custom asset loader | can be written, as a data asset, which is how a C# game has assets of its own types |
| [`custom_asset_reader`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/custom_asset_reader.rs) | Implements a custom AssetReader | does not apply, writes a Rust AssetReader |
| [`embedded_asset`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/embedded_asset.rs), [live in Bevy](https://bevy.org/examples/assets/embedded-asset/) | Embed an asset in the application binary and load it | can be written |
| [`extra_asset_source`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/extra_source.rs) | Load an asset from a non-standard asset source | can be written |
| [`generated_assets`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/generated_assets.rs), [live in Bevy](https://bevy.org/examples/assets/generated-assets/) | Shows how to generate and store assets at runtime | can be written |
| [`hot_asset_reloading`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/hot_asset_reloading.rs) | Demonstrates automatic reloading of assets when modified on disk | can be written |
| [`multi_asset_sync`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/multi_asset_sync.rs), [live in Bevy](https://bevy.org/examples/assets/multi-asset-sync/) | Demonstrates how to wait for multiple assets to be loaded. | can be written |
| [`repeated_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/repeated_texture.rs), [live in Bevy](https://bevy.org/examples/assets/repeated-texture/) | How to configure the texture to repeat instead of the default clamp to edges | can be written |
| [`web_asset`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/web_asset.rs), [live in Bevy](https://bevy.org/examples/assets/web-asset/) | Load an asset from the web | missing, loading assets over HTTP |

## Async Tasks

| Example | What it shows | State |
|---|---|---|
| [`async_channel_pattern`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/async_tasks/async_channel_pattern.rs), [live in Bevy](https://bevy.org/examples/async-tasks/async-channel-pattern/) | An example showing how to offload work to background async tasks using channels for communication. | [written](../BevyCSharp.Examples/async_tasks/async_channel_pattern.cs), with .NET's tasks and channels |
| [`async_compute`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/async_tasks/async_compute.rs) | How to use `AsyncComputeTaskPool` to complete longer running tasks | [written](../BevyCSharp.Examples/async_tasks/async_compute.cs), with .NET's tasks |
| [`external_source_external_thread`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/async_tasks/external_source_external_thread.rs) | How to use an external thread to run an infinite task and communicate with a channel | [written](../BevyCSharp.Examples/async_tasks/external_source_external_thread.cs), with a .NET thread and channel |

## Audio

| Example | What it shows | State |
|---|---|---|
| [`audio`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/audio.rs), [live in Bevy](https://bevy.org/examples/audio/audio/) | Shows how to load and play an audio file | [written](../BevyCSharp.Examples/audio/audio.cs) |
| [`audio_control`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/audio_control.rs), [live in Bevy](https://bevy.org/examples/audio/audio-control/) | Shows how to load and play an audio file, and control how it's played | missing, a playing sound's speed changed as it plays |
| [`decodable`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/decodable.rs), [live in Bevy](https://bevy.org/examples/audio/decodable/) | Shows how to create and register a custom audio source by implementing the `Decodable` type. | missing, audio sources a game generates (Decodable) |
| [`pitch`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/pitch.rs), [live in Bevy](https://bevy.org/examples/audio/pitch/) | Shows how to directly play a simple pitch | missing, a generated tone (Pitch) |
| [`play_sound_effect`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/play_sound_effect.rs), [live in Bevy](https://bevy.org/examples/audio/play-sound-effect/) | Shows how to play a sound effect in response to an event | [written](../BevyCSharp.Examples/audio/play_sound_effect.cs) |
| [`soundtrack`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/soundtrack.rs), [live in Bevy](https://bevy.org/examples/audio/soundtrack/) | Shows how to play different soundtracks based on game state | [written](../BevyCSharp.Examples/audio/soundtrack.cs) |
| [`spatial_audio_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/spatial_audio_2d.rs), [live in Bevy](https://bevy.org/examples/audio/spatial-audio-2d/) | Shows how to play spatial audio, and moving the emitter in 2D | can be written |
| [`spatial_audio_3d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/spatial_audio_3d.rs), [live in Bevy](https://bevy.org/examples/audio/spatial-audio-3d/) | Shows how to play spatial audio, and moving the emitter in 3D | [written](../BevyCSharp.Examples/audio/spatial_audio_3d.cs) |

## Camera

| Example | What it shows | State |
|---|---|---|
| [`2d_on_ui`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/2d_on_ui.rs), [live in Bevy](https://bevy.org/examples/camera/2d-on-ui/) | Shows how to render 2D objects on top of Bevy UI | [written](../BevyCSharp.Examples/camera/2d_on_ui.cs) |
| [`2d_screen_shake`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/2d_screen_shake.rs), [live in Bevy](https://bevy.org/examples/camera/2d-screen-shake/) | A simple 2D screen shake effect | can be written |
| [`2d_top_down_camera`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/2d_top_down_camera.rs), [live in Bevy](https://bevy.org/examples/camera/2d-top-down-camera/) | A 2D top-down camera smoothly following player movements | can be written, through Bevy's reflected Bloom |
| [`camera_orbit`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/camera_orbit.rs), [live in Bevy](https://bevy.org/examples/camera/camera-orbit/) | Shows how to orbit a static scene using pitch, yaw, and roll. | [written](../BevyCSharp.Examples/camera/camera_orbit.cs) |
| [`custom_projection`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/custom_projection.rs), [live in Bevy](https://bevy.org/examples/camera/custom-projection/) | Shows how to create custom camera projections. | missing, custom camera projections |
| [`first_person_view_model`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/first_person_view_model.rs), [live in Bevy](https://bevy.org/examples/camera/first-person-view-model/) | A first-person camera that uses a world model and a view model with different field of views (FOV) | [written](../BevyCSharp.Examples/camera/first_person_view_model.cs) |
| [`free_camera_controller`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/free_camera_controller.rs), [live in Bevy](https://bevy.org/examples/camera/free-camera-controller/) | Demonstrates the FreeCamera controller for 3D scenes. | missing, Bevy's free camera controller, its plugin and its settings |
| [`pan_camera_controller`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/pan_camera_controller.rs), [live in Bevy](https://bevy.org/examples/camera/pan-camera-controller/) | Example Pan-Camera Styled Camera Controller for 2D scenes | missing, Bevy's pan camera controller, its plugin and its settings |
| [`projection_zoom`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/projection_zoom.rs), [live in Bevy](https://bevy.org/examples/camera/projection-zoom/) | Shows how to zoom orthographic and perspective projection cameras. | [written](../BevyCSharp.Examples/camera/projection_zoom.cs) |

## Dev tools

| Example | What it shows | State |
|---|---|---|
| [`fps_overlay`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/dev_tools/fps_overlay.rs), [live in Bevy](https://bevy.org/examples/dev-tools/fps-overlay/) | Demonstrates FPS overlay | can be written |
| [`infinite_grid`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/dev_tools/infinite_grid.rs), [live in Bevy](https://bevy.org/examples/dev-tools/infinite-grid/) | Demonstrates Bevy's infinite grid, suitable as a ground plane for editors | missing, Bevy's infinite grid |
| [`schedule_data`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/dev_tools/schedule_data.rs) | Extracts the schedule data from a default app and writes it to a file | does not apply, reads Bevy's schedule graphs from Rust |

## Diagnostics

| Example | What it shows | State |
|---|---|---|
| [`custom_diagnostic`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/diagnostics/custom_diagnostic.rs), [live in Bevy](https://bevy.org/examples/diagnostics/custom-diagnostic/) | Shows how to create a custom diagnostic | missing, diagnostics a game registers in Bevy's store |
| [`enabling_disabling_diagnostic`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/diagnostics/enabling_disabling_diagnostic.rs), [live in Bevy](https://bevy.org/examples/diagnostics/enabling-disabling-diagnostic/) | Shows how to disable/re-enable a Diagnostic during runtime | missing, turning one of Bevy's diagnostics on and off |
| [`log_diagnostics`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/diagnostics/log_diagnostics.rs), [live in Bevy](https://bevy.org/examples/diagnostics/log-diagnostics/) | Add a plugin that logs diagnostics, like frames per second (FPS), to the console | can be written, through frame.profile and the frame panel |

## ECS (Entity Component System)

| Example | What it shows | State |
|---|---|---|
| [`callbacks`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/callbacks.rs), [live in Bevy](https://bevy.org/examples/ecs-entity-component-system/callbacks/) | Store arbitrary systems in components and run them on demand | [written](../BevyCSharp.Examples/ecs/callbacks.cs), prints [its output](assets/examples/callbacks.txt) |
| [`change_detection`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/change_detection.rs) | Change detection on components and resources | missing, a resource's change ticks, and when a component was added and by what |
| [`component_hooks`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/component_hooks.rs) | Define component hooks to manage component lifecycle events | missing, a component's add, insert and discard hooks, which only Bevy's remove hook has here, for the generator |
| [`contiguous_query`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/contiguous_query.rs) | Demonstrates contiguous queries | does not apply, about a Rust query's memory layout |
| [`custom_executor`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/custom_executor.rs) | Demonstrates how to make a custom SystemExecutor | does not apply, replaces Bevy's system executor, which is Rust |
| [`custom_query_param`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/custom_query_param.rs) | Groups commonly used compound queries and query filters into a single type | does not apply, derives a Rust query type |
| [`custom_schedule`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/custom_schedule.rs) | Demonstrates how to add custom schedules | missing, schedules a game adds of its own |
| [`delayed_commands`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/delayed_commands.rs), [live in Bevy](https://bevy.org/examples/ecs-entity-component-system/delayed-commands/) | Demonstrates how to schedule ECS commands with a delay | missing, commands queued to run after a delay, and a click observed on a sprite |
| [`dynamic`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/dynamic.rs) | Dynamically create components, spawn entities with those components and query those components | does not apply, builds components from raw layouts in Rust |
| [`ecs_guide`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/ecs_guide.rs) | Full guide to Bevy's ECS | [written](../BevyCSharp.Examples/ecs/ecs_guide.cs), prints [its output](assets/examples/ecs_guide.txt) |
| [`entity_disabling`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/entity_disabling.rs), [live in Bevy](https://bevy.org/examples/ecs-entity-component-system/entity-disabling/) | Demonstrates how to hide entities from the ECS without deleting them | missing, a click observed on a mesh |
| [`error_handling`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/error_handling.rs) | How to return and handle errors across the ECS | missing, an observer of the pointer moving over a mesh, and points sampled over a mesh's surface |
| [`extraction`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/extraction.rs) | Demonstrates different ways of extracting components, copying them from the main world to the render world | does not apply, writes render world extraction in Rust |
| [`fallible_params`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/fallible_params.rs) | Systems are skipped if their parameters cannot be acquired | does not apply, about Rust system parameters that fail validation, where a C# system checks what it needs itself |
| [`fixed_timestep`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/fixed_timestep.rs) | Shows how to create systems that run every fixed timestep, rather than every tick | [written](../BevyCSharp.Examples/ecs/fixed_timestep.cs), prints [its output](assets/examples/fixed_timestep.txt) |
| [`generic_system`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/generic_system.rs) | Shows how to create systems that can be reused with different types | [written](../BevyCSharp.Examples/ecs/generic_system.cs), prints [its output](assets/examples/generic_system.txt) |
| [`hierarchy`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/hierarchy.rs) | Creates a hierarchy of parents and children entities | [written](../BevyCSharp.Examples/ecs/hierarchy.cs) |
| [`hotpatching_systems`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/hotpatching_systems.rs) | Demonstrates how to hotpatch systems | can be written, as scripts reloaded while the app runs |
| [`immutable_components`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/immutable_components.rs) | Demonstrates the creation and utility of immutable components | does not apply, about Rust's component mutability |
| [`iter_combinations`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/iter_combinations.rs), [live in Bevy](https://bevy.org/examples/ecs-entity-component-system/iter-combinations/) | Shows how to iterate over combinations of query results | [written](../BevyCSharp.Examples/ecs/iter_combinations.cs) |
| [`message`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/message.rs) | Illustrates message creation, activation, and reception | missing, a message changed in place by a later system and read by a later one the same frame |
| [`nondeterministic_system_order`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/nondeterministic_system_order.rs) | Systems run in parallel, but their order isn't always deterministic. Here's how to detect and fix this. | missing, the schedule reporting systems whose order is ambiguous |
| [`observer_propagation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/observer_propagation.rs), [live in Bevy](https://bevy.org/examples/ecs-entity-component-system/observer-propagation/) | Demonstrates event propagation with observers | [written](../BevyCSharp.Examples/ecs/observer_propagation.cs), prints [its output](assets/examples/observer_propagation.txt) |
| [`observers`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/observers.rs), [live in Bevy](https://bevy.org/examples/ecs-entity-component-system/observers/) | Demonstrates observers that react to events (both built-in life-cycle events and custom events) | [written](../BevyCSharp.Examples/ecs/observers.cs) |
| [`one_shot_systems`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/one_shot_systems.rs) | Shows how to flexibly run systems without scheduling them | [written](../BevyCSharp.Examples/ecs/one_shot_systems.cs) |
| [`parallel_query`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/parallel_query.rs) | Illustrates parallel queries with `ParallelIterator` | [written](../BevyCSharp.Examples/ecs/parallel_query.cs) |
| [`relationships`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/relationships.rs) | Define and work with custom relationships between entities | missing, relationships of a game's own between entities |
| [`removal_detection`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/removal_detection.rs) | Query for entities that had a specific component removed earlier in the current frame | [written](../BevyCSharp.Examples/ecs/removal_detection.cs) |
| [`run_conditions`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/run_conditions.rs) | Run systems only when one or multiple conditions are met | [written](../BevyCSharp.Examples/ecs/run_conditions.cs), prints [its output](assets/examples/run_conditions.txt) |
| [`startup_system`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/startup_system.rs) | Demonstrates a startup system (one that runs once when the app starts up) | [written](../BevyCSharp.Examples/ecs/startup_system.cs), prints [its output](assets/examples/startup_system.txt) |
| [`state_scoped`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/state_scoped.rs) | Shows how to spawn entities that are automatically despawned either when entering or exiting specific game states. | missing, despawning when a state is entered or by a rule over the transition, and a state holding a value |
| [`system_closure`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/system_closure.rs) | Show how to use closures as systems, and how to configure `Local` variables by capturing external state | [written](../BevyCSharp.Examples/ecs/system_closure.cs), prints [its output](assets/examples/system_closure.txt) |
| [`system_param`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/system_param.rs) | Illustrates creating custom system parameters with `SystemParam` | does not apply, derives a Rust SystemParam |
| [`system_piping`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/system_piping.rs) | Pipe the output of one system into a second, allowing you to handle any errors gracefully | does not apply, pipes Rust system outputs |
| [`system_stepping`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/system_stepping.rs) | Demonstrate stepping through systems in order of execution. | missing, stepping through systems one at a time |

## Embedded

| Example | What it shows | State |
|---|---|---|
| `no_std_library` | Example library compatible with `std` and `no_std` targets | does not apply, a Rust library for no_std targets |

## Games

| Example | What it shows | State |
|---|---|---|
| [`alien_cake_addict`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/showcase/alien_cake_addict.rs), [live in Bevy](https://bevy.org/examples/games/alien-cake-addict/) | Eat the cakes. Eat them all. An example 3D game | can be written |
| [`breakout`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/showcase/breakout.rs), [live in Bevy](https://bevy.org/examples/games/breakout/) | An implementation of the classic game "Breakout". | can be written |
| [`contributors`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/showcase/contributors.rs), [live in Bevy](https://bevy.org/examples/games/contributors/) | Displays each contributor as a bouncy bevy-ball! | can be written |
| [`desk_toy`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/showcase/desk_toy.rs) | Bevy logo as a desk toy using transparent windows! Now with Googly Eyes! | can be written |
| [`game_menu`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/showcase/game_menu.rs), [live in Bevy](https://bevy.org/examples/games/game-menu/) | A simple game menu | can be written |
| [`loading_screen`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/showcase/loading_screen.rs), [live in Bevy](https://bevy.org/examples/games/loading-screen/) | Demonstrates how to create a loading screen that waits for all assets to be loaded and render pipelines to be compiled. | can be written |

## Gizmos

| Example | What it shows | State |
|---|---|---|
| [`2d_gizmos`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/2d_gizmos.rs), [live in Bevy](https://bevy.org/examples/gizmos/2d-gizmos/) | A scene showcasing 2D gizmos | can be written |
| [`2d_text_gizmos`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/2d_text_gizmos.rs), [live in Bevy](https://bevy.org/examples/gizmos/2d-text-gizmos/) | A scene showcasing 2d text gizmos | missing, text gizmos |
| [`3d_gizmos`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/3d_gizmos.rs), [live in Bevy](https://bevy.org/examples/gizmos/3d-gizmos/) | A scene showcasing 3D gizmos | can be written |
| [`3d_text_gizmos`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/3d_text_gizmos.rs), [live in Bevy](https://bevy.org/examples/gizmos/3d-text-gizmos/) | A scene showcasing 3d text gizmos | missing, text gizmos |
| [`anchored_text_gizmos`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/anchored_text_gizmos.rs), [live in Bevy](https://bevy.org/examples/gizmos/anchored-text-gizmos/) | Demonstrates anchored text gizmos | missing, text gizmos |
| [`axes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/axes.rs), [live in Bevy](https://bevy.org/examples/gizmos/axes/) | Demonstrates the function of axes gizmos | can be written |
| [`light_gizmos`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/light_gizmos.rs), [live in Bevy](https://bevy.org/examples/gizmos/light-gizmos/) | A scene showcasing light gizmos | can be written, through Bevy's reflected ShowLightGizmo |
| [`text_gizmos_font`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/text_gizmos_font.rs), [live in Bevy](https://bevy.org/examples/gizmos/text-gizmos-font/) | Example displaying the font used by text gizmos | missing, text gizmos |
| [`transform_gizmo`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/transform_gizmo.rs), [live in Bevy](https://bevy.org/examples/gizmos/transform-gizmo/) | Interactive transform gizmo for translating, rotating, and scaling entities | missing, Bevy's interactive transform gizmo |

## Helpers

| Example | What it shows | State |
|---|---|---|
| [`widgets`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/helpers/widgets.rs) | Example UI Widgets | missing, Bevy's widget helpers (bevy_ui_widgets) |

## Input

| Example | What it shows | State |
|---|---|---|
| [`char_input_events`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/char_input_events.rs) | Prints out all chars as they are inputted | [written](../BevyCSharp.Examples/input/char_input_events.cs), prints [its output](assets/examples/char_input_events.txt) |
| [`gamepad_input`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/gamepad_input.rs) | Shows handling of gamepad input, connections, and disconnections | [written](../BevyCSharp.Examples/input/gamepad_input.cs), prints [its output](assets/examples/gamepad_input.txt) |
| [`gamepad_input_events`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/gamepad_input_events.rs) | Iterates and prints gamepad input and connection events | missing, a gamepad's button and axis changes as messages, in the order they happened |
| [`gamepad_rumble`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/gamepad_rumble.rs) | Shows how to rumble a gamepad using force feedback | [written](../BevyCSharp.Examples/input/gamepad_rumble.cs), prints [its output](assets/examples/gamepad_rumble.txt) |
| [`keyboard_input`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/keyboard_input.rs) | Demonstrates handling a key press/release | missing, logical keys as Bevy's ButtonInput<Key>, such as the key that types '?' |
| [`keyboard_input_events`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/keyboard_input_events.rs) | Prints out all keyboard events | missing, keyboard events as messages, each with its key code, logical key and state |
| [`keyboard_modifiers`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/keyboard_modifiers.rs) | Demonstrates using key modifiers (ctrl, shift) | [written](../BevyCSharp.Examples/input/keyboard_modifiers.cs), prints [its output](assets/examples/keyboard_modifiers.txt) |
| [`mouse_grab`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/mouse_grab.rs) | Demonstrates how to grab the mouse, locking the cursor to the app's screen | [written](../BevyCSharp.Examples/input/mouse_grab.cs) |
| [`mouse_input`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/mouse_input.rs) | Demonstrates handling a mouse button press/release | [written](../BevyCSharp.Examples/input/mouse_input.cs), prints [its output](assets/examples/mouse_input.txt) |
| [`mouse_input_events`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/mouse_input_events.rs) | Prints out all mouse events (buttons, movement, etc.) | missing, mouse button, motion, cursor, wheel and gesture events as messages |
| [`touch_input`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/touch_input.rs) | Displays touch presses, releases, and cancels | [written in part](../BevyCSharp.Examples/input/touch_input.cs), prints [its output](assets/examples/touch_input.txt), a touch the platform cancels, which is not reported |
| [`touch_input_events`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/touch_input_events.rs) | Prints out all touch inputs | missing, touch events as messages |

## Math

| Example | What it shows | State |
|---|---|---|
| [`bounding_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/math/bounding_2d.rs), [live in Bevy](https://bevy.org/examples/math/bounding-2d/) | Showcases bounding volumes and intersection tests | missing, Bevy's bounding volumes, their casts and their intersection tests |
| [`cubic_splines`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/math/cubic_splines.rs), [live in Bevy](https://bevy.org/examples/math/cubic-splines/) | Exhibits different modes of constructing cubic curves using splines | missing, Bevy's cubic curves, Hermite, cardinal and B-spline |
| [`custom_primitives`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/math/custom_primitives.rs), [live in Bevy](https://bevy.org/examples/math/custom-primitives/) | Demonstrates how to add custom primitives and useful traits for them. | does not apply, implements Rust traits for a primitive |
| [`random_sampling`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/math/random_sampling.rs), [live in Bevy](https://bevy.org/examples/math/random-sampling/) | Demonstrates how to sample random points from mathematical primitives | missing, points sampled inside and on the boundary of Bevy's shapes |
| [`render_primitives`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/math/render_primitives.rs), [live in Bevy](https://bevy.org/examples/math/render-primitives/) | Shows off rendering for all math primitives as both Meshes and Gizmos | missing, gizmos of Bevy's primitive shapes |
| [`smooth_follow`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/movement/smooth_follow.rs), [live in Bevy](https://bevy.org/examples/math/smooth-follow/) | Demonstrates how to make an entity smoothly follow another using interpolation | [written](../BevyCSharp.Examples/movement/smooth_follow.cs) |

## Movement

| Example | What it shows | State |
|---|---|---|
| [`physics_in_fixed_timestep`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/movement/physics_in_fixed_timestep.rs), [live in Bevy](https://bevy.org/examples/movement/physics-in-fixed-timestep/) | Handles input, physics, and rendering in an industry-standard way by using a fixed timestep | can be written |

## Picking

| Example | What it shows | State |
|---|---|---|
| [`custom_hit_data`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/picking/custom_hit_data.rs), [live in Bevy](https://bevy.org/examples/picking/custom-hit-data/) | Demonstrates a custom picking backend with custom hit data. | does not apply, writes a Rust picking backend |
| [`debug_picking`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/picking/debug_picking.rs), [live in Bevy](https://bevy.org/examples/picking/debug-picking/) | Demonstrates picking debug overlay | missing, Bevy's picking debug overlay |
| [`dragdrop_picking`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/picking/dragdrop_picking.rs), [live in Bevy](https://bevy.org/examples/picking/dragdrop-picking/) | Demonstrates drag and drop using picking events | missing, drag and drop picking events |
| [`mesh_picking`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/picking/mesh_picking.rs), [live in Bevy](https://bevy.org/examples/picking/mesh-picking/) | Demonstrates picking meshes | can be written, in the editor profile, which carries picking |
| [`simple_picking`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/picking/simple_picking.rs), [live in Bevy](https://bevy.org/examples/picking/simple-picking/) | Demonstrates how to use picking events to spawn simple objects | can be written, in the editor profile, which carries picking |
| [`sprite_picking`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/picking/sprite_picking.rs), [live in Bevy](https://bevy.org/examples/picking/sprite-picking/) | Demonstrates picking sprites and sprite atlases | missing, picking sprites |

## Reflection

| Example | What it shows | State |
|---|---|---|
| `auto_register_static` | Demonstrates how to set up automatic reflect types registration for platforms without `inventory` support | does not apply, about Rust reflection registration |
| [`custom_attributes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/reflection/custom_attributes.rs) | Registering and accessing custom attributes on reflected types | does not apply, about Rust reflection |
| [`dynamic_types`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/reflection/dynamic_types.rs) | How dynamic types are used with reflection | does not apply, about Rust reflection |
| [`function_reflection`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/reflection/function_reflection.rs) | Demonstrates how functions can be called dynamically using reflection | does not apply, about Rust reflection |
| [`generic_reflection`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/reflection/generic_reflection.rs) | Registers concrete instances of generic types that may be used with reflection | does not apply, about Rust reflection |
| [`reflection`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/reflection/reflection.rs) | Demonstrates how reflection in Bevy provides a way to dynamically interact with Rust types | does not apply, about Rust reflection |
| [`reflection_types`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/reflection/reflection_types.rs) | Illustrates the various reflection types available | does not apply, about Rust reflection |
| [`serialization`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/reflection/serialization.rs) | Demonstrates serialization and deserialization using reflection without serde's Serialize/Deserialize traits | does not apply, about Rust reflection |
| [`type_data`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/reflection/type_data.rs) | Demonstrates how to create and use type data | does not apply, about Rust reflection |

## Remote Protocol

| Example | What it shows | State |
|---|---|---|
| [`app_under_test`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/remote/app_under_test.rs) | A Bevy app that you can connect to with the BRP and control | missing, Bevy's remote protocol, where bcs is this engine's own |
| [`client`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/remote/client.rs) | A simple command line client that can control Bevy apps via the BRP | does not apply, a Rust client of Bevy's remote protocol |
| [`integration_test`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/remote/integration_test.rs) | Connects to a running Bevy app via BRP, finds a button, and clicks it | missing, Bevy's remote protocol, where bcs is this engine's own |
| [`server`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/remote/server.rs) | A Bevy app that you can connect to with the BRP and edit | missing, Bevy's remote protocol, where bcs is this engine's own |

## Scene

| Example | What it shows | State |
|---|---|---|
| [`bsn`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/scene/bsn.rs), [live in Bevy](https://bevy.org/examples/scene/bsn/) | Demonstrates how to use BSN to compose scenes | missing, BSN scene notation |
| [`world_serialization`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/scene/world_serialization.rs) | Demonstrates loading from and saving world to files | can be written |

## Shaders

| Example | What it shows | State |
|---|---|---|
| [`animate_shader`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/animate_shader.rs), [live in Bevy](https://bevy.org/examples/shaders/animate-shader/) | A shader that uses dynamic data like the time since startup | can be written |
| [`array_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/array_texture.rs), [live in Bevy](https://bevy.org/examples/shaders/array-texture/) | A shader that shows how to reuse the core bevy PBR shading functionality in a custom material that obtains the base color from an array texture. | can be written |
| [`automatic_instancing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/automatic_instancing.rs), [live in Bevy](https://bevy.org/examples/shaders/automatic-instancing/) | Shows that multiple instances of a cube are automatically instanced in one draw call | can be written |
| [`compute_mesh`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/compute_mesh.rs) | A compute shader that generates a mesh that is controlled by a Handle | can be written |
| [`compute_shader_game_of_life`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/compute_shader_game_of_life.rs) | A compute shader that simulates Conway's Game of Life | can be written |
| [`custom_phase_item`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/custom_phase_item.rs), [live in Bevy](https://bevy.org/examples/shaders/custom-phase-item/) | Demonstrates how to enqueue custom draw commands in a render phase | does not apply, enqueues Rust draw commands in a render phase |
| [`custom_post_processing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/custom_post_processing.rs), [live in Bevy](https://bevy.org/examples/shaders/custom-post-processing/) | A custom post processing effect, using a custom render pass that runs after the main pass | can be written |
| [`custom_render_phase`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/custom_render_phase.rs), [live in Bevy](https://bevy.org/examples/shaders/custom-render-phase/) | Shows how to make a complete render phase | does not apply, builds a render phase in Rust |
| [`custom_shader_instancing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/custom_shader_instancing.rs), [live in Bevy](https://bevy.org/examples/shaders/custom-shader-instancing/) | A shader that renders a mesh multiple times in one draw call using low level rendering api | can be written |
| [`custom_vertex_attribute`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/custom_vertex_attribute.rs), [live in Bevy](https://bevy.org/examples/shaders/custom-vertex-attribute/) | A shader that reads a mesh's custom vertex attribute | missing, vertex attributes a game defines |
| [`extended_material`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/extended_material.rs), [live in Bevy](https://bevy.org/examples/shaders/extended-material/) | A custom shader that builds on the standard material | missing, a standard material extended by a shader of the example's own |
| [`extended_material_bindless`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/extended_material_bindless.rs) | Demonstrates bindless `ExtendedMaterial` | missing, a standard material extended by a shader of the example's own, its data bindless |
| [`gpu_readback`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/gpu_readback.rs) | A very simple compute shader that writes to a buffer that is read by the cpu | can be written |
| [`render_depth_to_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/render_depth_to_texture.rs), [live in Bevy](https://bevy.org/examples/shaders/render-depth-to-texture/) | Demonstrates how to use depth-only cameras | missing, depth-only cameras rendered to a texture |
| [`shader_defs`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_defs.rs), [live in Bevy](https://bevy.org/examples/shaders/shader-defs/) | A shader that uses "shaders defs" (a bevy tool to selectively toggle parts of a shader) | can be written |
| [`shader_material`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_material.rs), [live in Bevy](https://bevy.org/examples/shaders/shader-material/) | A shader and a material that uses it | can be written |
| [`shader_material_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_material_2d.rs), [live in Bevy](https://bevy.org/examples/shaders/shader-material-2d/) | A shader and a material that uses it on a 2d mesh | missing, 2D meshes (Mesh2d) with a shader material |
| [`shader_material_bindless`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_material_bindless.rs), [live in Bevy](https://bevy.org/examples/shaders/shader-material-bindless/) | Demonstrates how to make materials that use bindless textures | can be written |
| [`shader_material_glsl`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_material_glsl.rs), [live in Bevy](https://bevy.org/examples/shaders/shader-material-glsl/) | A shader that uses the GLSL shading language | does not apply, GLSL, where shaders here are Slang |
| [`shader_material_screenspace_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_material_screenspace_texture.rs), [live in Bevy](https://bevy.org/examples/shaders/shader-material-screenspace-texture/) | A shader that samples a texture with view-independent UV coordinates | can be written |
| [`shader_material_wesl`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_material_wesl.rs), [live in Bevy](https://bevy.org/examples/shaders/shader-material-wesl/) | A shader that uses WESL | does not apply, WESL, where shaders here are Slang |
| [`shader_prepass`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_prepass.rs) | A shader that uses the various textures generated by the prepass | can be written |
| [`specialized_mesh_pipeline`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/specialized_mesh_pipeline.rs), [live in Bevy](https://bevy.org/examples/shaders/specialized-mesh-pipeline/) | Demonstrates how to write a specialized mesh pipeline | does not apply, writes a mesh pipeline in Rust |
| [`storage_buffer`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/storage_buffer.rs), [live in Bevy](https://bevy.org/examples/shaders/storage-buffer/) | A shader that shows how to bind a storage buffer using a custom material. | can be written |
| [`texture_binding_array`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/texture_binding_array.rs) | A shader that shows how to bind and sample multiple textures as a binding array (a.k.a. bindless textures). | can be written |

## Shaders Advanced

| Example | What it shows | State |
|---|---|---|
| [`fullscreen_material`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/fullscreen_material.rs), [live in Bevy](https://bevy.org/examples/shaders-advanced/fullscreen-material/) | Demonstrates how to write a fullscreen material | can be written |
| [`manual_material`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/manual_material.rs), [live in Bevy](https://bevy.org/examples/shaders-advanced/manual-material/) | Demonstrates how to implement a material manually using the mid-level render APIs | does not apply, writes a material with Rust's mid-level render API |

## State

| Example | What it shows | State |
|---|---|---|
| [`computed_states`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/state/computed_states.rs) | Advanced state patterns using Computed States. | missing, a state holding values, such as a game that is paused or in turbo |
| [`custom_transitions`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/state/custom_transitions.rs) | Creating and working with custom state transition schedules. | missing, transitions to the same state, run as schedules of a game's own |
| [`states`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/state/states.rs) | Illustrates how to use States to control transitioning from a Menu state to an InGame state. | [written](../BevyCSharp.Examples/state/states.cs) |
| [`sub_states`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/state/sub_states.rs) | Using Sub States for hierarchical state handling. | [written](../BevyCSharp.Examples/state/sub_states.cs) |

## Stress Tests

| Example | What it shows | State |
|---|---|---|
| [`bevymark`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/bevymark.rs), [live in Bevy](https://bevy.org/examples/stress-tests/bevymark/) | A heavy sprite rendering workload to benchmark your system with Bevy | can be written |
| [`bevymark_3d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/bevymark_3d.rs), [live in Bevy](https://bevy.org/examples/stress-tests/bevymark-3d/) | A heavy 3D cube rendering workload to benchmark your system with Bevy | can be written |
| [`many_animated_sprite_meshes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_animated_sprite_meshes.rs) | Displays many animated sprite meshes in a grid arrangement with slight offsets to their animation timers. Used for performance testing. | can be written, through Bevy's reflected SpriteMesh |
| [`many_animated_sprites`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_animated_sprites.rs), [live in Bevy](https://bevy.org/examples/stress-tests/many-animated-sprites/) | Displays many animated sprites in a grid arrangement with slight offsets to their animation timers. Used for performance testing. | can be written |
| [`many_buttons`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_buttons.rs), [live in Bevy](https://bevy.org/examples/stress-tests/many-buttons/) | Test rendering of many UI elements | can be written |
| [`many_cameras_lights`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_cameras_lights.rs), [live in Bevy](https://bevy.org/examples/stress-tests/many-cameras-lights/) | Test rendering of many cameras and lights | can be written |
| [`many_components`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_components.rs) | Test large ECS systems | does not apply, registers components dynamically in Rust |
| [`many_cubes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_cubes.rs), [live in Bevy](https://bevy.org/examples/stress-tests/many-cubes/) | Simple benchmark to test per-entity draw overhead. Run with the `sphere` argument to test frustum culling | can be written |
| [`many_foxes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_foxes.rs), [live in Bevy](https://bevy.org/examples/stress-tests/many-foxes/) | Loads an animated fox model and spawns lots of them. Good for testing skinned mesh performance. Takes an unsigned integer argument for the number of foxes to spawn. Defaults to 1000 | can be written |
| [`many_gizmos`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_gizmos.rs), [live in Bevy](https://bevy.org/examples/stress-tests/many-gizmos/) | Test rendering of many gizmos | can be written |
| [`many_glyphs`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_glyphs.rs), [live in Bevy](https://bevy.org/examples/stress-tests/many-glyphs/) | Simple benchmark to test text rendering. | can be written |
| [`many_gradients`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_gradients.rs), [live in Bevy](https://bevy.org/examples/stress-tests/many-gradients/) | Stress test for gradient rendering performance | can be written, through Bevy's reflected BackgroundGradient |
| [`many_lights`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_lights.rs), [live in Bevy](https://bevy.org/examples/stress-tests/many-lights/) | Simple benchmark to test rendering many point lights. Run with `WGPU_SETTINGS_PRIO=webgl2` to restrict to uniform buffers and max 256 lights | can be written |
| [`many_materials`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_materials.rs), [live in Bevy](https://bevy.org/examples/stress-tests/many-materials/) | Benchmark to test rendering many animated materials | can be written |
| [`many_morph_targets`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_morph_targets.rs), [live in Bevy](https://bevy.org/examples/stress-tests/many-morph-targets/) | Simple benchmark to test rendering many meshes with animated morph targets. | can be written, through Bevy's reflected MorphWeights |
| [`many_sprite_meshes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_sprite_meshes.rs) | Displays many sprite meshes in a grid arrangement! Used for performance testing. Use `--colored` to enable color tinted sprites. | can be written, through Bevy's reflected SpriteMesh |
| [`many_sprites`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_sprites.rs), [live in Bevy](https://bevy.org/examples/stress-tests/many-sprites/) | Displays many sprites in a grid arrangement! Used for performance testing. Use `--colored` to enable color tinted sprites. | can be written |
| [`many_text`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_text.rs), [live in Bevy](https://bevy.org/examples/stress-tests/many-text/) | Displays many UI Text nodes. Used for performance testing. | can be written |
| [`many_text2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_text2d.rs), [live in Bevy](https://bevy.org/examples/stress-tests/many-text2d/) | Displays many Text2d! Used for performance testing. | can be written, through Bevy's reflected Text2d |
| [`text_pipeline`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/text_pipeline.rs) | Text Pipeline benchmark | can be written |
| [`transform_hierarchy`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/transform_hierarchy.rs) | Various test cases for hierarchy and transform propagation performance | can be written |

## Time

| Example | What it shows | State |
|---|---|---|
| [`time`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/time/time.rs) | Explains how Time is handled in ECS | does not apply, replaces Bevy's runner with one stepping the app from the console, and the bridge owns the runner |
| [`timers`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/time/timers.rs) | Illustrates ticking `Timer` resources inside systems and handling their state | [written](../BevyCSharp.Examples/time/timers.cs), prints [its output](assets/examples/timers.txt) |
| [`virtual_time`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/time/virtual_time.rs) | Shows how `Time<Virtual>` can be used to pause, resume, slow down and speed up a game. | [written](../BevyCSharp.Examples/time/virtual_time.cs) |

## Tools

| Example | What it shows | State |
|---|---|---|
| `gamepad_viewer`, [live in Bevy](https://bevy.org/examples/tools/gamepad-viewer/) | Shows a visualization of gamepad buttons, sticks, and triggers | can be written |
| `scene_viewer`, [live in Bevy](https://bevy.org/examples/tools/scene-viewer/) | A simple way to view glTF models with Bevy. Just run `cargo run --release --example scene_viewer /path/to/model.gltf#Scene0`, replacing the path as appropriate. With no arguments it will load the FieldHelmet glTF model from the repository assets subdirectory | can be written |

## Transforms

| Example | What it shows | State |
|---|---|---|
| [`3d_rotation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/transforms/3d_rotation.rs), [live in Bevy](https://bevy.org/examples/transforms/3d-rotation/) | Illustrates how to (constantly) rotate an object around an axis | [written](../BevyCSharp.Examples/transforms/3d_rotation.cs) |
| [`align`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/transforms/align.rs), [live in Bevy](https://bevy.org/examples/transforms/align/) | A demonstration of Transform's axis-alignment feature | [written](../BevyCSharp.Examples/transforms/align.cs) |
| [`scale`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/transforms/scale.rs), [live in Bevy](https://bevy.org/examples/transforms/scale/) | Illustrates how to scale an object in each direction | [written](../BevyCSharp.Examples/transforms/scale.cs) |
| [`transform`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/transforms/transform.rs), [live in Bevy](https://bevy.org/examples/transforms/transform/) | Shows multiple transformations of objects | [written](../BevyCSharp.Examples/transforms/transform.cs) |
| [`translation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/transforms/translation.rs), [live in Bevy](https://bevy.org/examples/transforms/translation/) | Illustrates how to move an object along an axis | [written](../BevyCSharp.Examples/transforms/translation.cs) |

## UI (User Interface)

| Example | What it shows | State |
|---|---|---|
| [`anchor_layout`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/layout/anchor_layout.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/anchor-layout/) | Shows an 'anchor layout' style of ui layout | [written](../BevyCSharp.Examples/ui/anchor_layout.cs) |
| [`borders`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/styling/borders.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/borders/) | Demonstrates how to create a node with a border | [written](../BevyCSharp.Examples/ui/borders.cs) |
| [`box_shadow`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/styling/box_shadow.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/box-shadow/) | Demonstrates how to create a node with a shadow | [written](../BevyCSharp.Examples/ui/box_shadow.cs), through Bevy's reflected BoxShadow |
| [`button`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/button.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/button/) | Illustrates creating and updating a button | [written in part](../BevyCSharp.Examples/ui/button.cs), the input focus given to the button, a resource of Bevy's |
| [`directional_navigation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/navigation/directional_navigation.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/directional-navigation/) | Demonstration of automatic directional navigation based on UI element positions | missing, directional navigation between interface nodes |
| [`directional_navigation_overrides`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/navigation/directional_navigation_overrides.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/directional-navigation-overrides/) | Demonstration of automatic directional navigation between UI elements with manual overrides | missing, directional navigation between interface nodes |
| [`display_and_visibility`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/layout/display_and_visibility.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/display-and-visibility/) | Demonstrates how Display and Visibility work in the UI. | [written](../BevyCSharp.Examples/ui/display_and_visibility.cs) |
| [`drag_to_scroll`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/scroll_and_overflow/drag_to_scroll.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/drag-to-scroll/) | This example tests scale factor, dragging and scrolling | missing, observers of the pointer dragging a scrolled node, and Bevy's UiScale resource |
| [`editable_text_filter`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/editable_text_filter.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/editable-text-filter/) | Demonstrates an 8-character hex input using EditableTextFilter | missing, Bevy's editable text (EditableText) |
| [`feathers_counter`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/feathers_counter.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/feathers-counter/) | Simple counter using feathers | missing, Bevy's Feathers widgets |
| [`feathers_gallery`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/feathers_gallery.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/feathers-gallery/) | Gallery of Feathers Widgets | missing, Bevy's Feathers widgets |
| [`flex_layout`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/layout/flex_layout.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/flex-layout/) | Demonstrates how the AlignItems and JustifyContent properties can be composed to layout nodes and position text | [written](../BevyCSharp.Examples/ui/flex_layout.cs) |
| [`font_atlas_debug`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/font_atlas_debug.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/font-atlas-debug/) | Illustrates how FontAtlases are populated (used to optimize text rendering internally) | missing, reading the font atlases |
| [`font_query`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/font_query.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/font-query/) | Demonstrates font querying | missing, querying fonts |
| [`font_variations`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/font_variations.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/font-variations/) | Demonstrates how to use OpenType font variations. | missing, OpenType font variations |
| [`font_weights`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/font_weights.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/font-weights/) | Demonstrates how to use font weights. | missing, font weights |
| [`generic_font_families`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/generic_font_families.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/generic-font-families/) | Demonstrates how to use generic font families | missing, generic font families |
| [`ghost_nodes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/layout/ghost_nodes.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/ghost-nodes/) | Demonstrates the use of Ghost Nodes to skip entities in the UI layout hierarchy | missing, ghost nodes (GhostNode) |
| [`gradients`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/styling/gradients.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/gradients/) | An example demonstrating gradients | [written](../BevyCSharp.Examples/ui/gradients.cs), through Bevy's reflected BackgroundGradient and BorderGradient |
| [`grid`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/layout/grid.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/grid/) | An example for CSS Grid layout | [written](../BevyCSharp.Examples/ui/grid.cs) |
| [`image_node`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/images/image_node.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/image-node/) | Demonstrates how to create an image node | [written](../BevyCSharp.Examples/ui/image_node.cs) |
| [`image_node_resizing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/images/image_node_resizing.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/image-node-resizing/) | Demonstrates how to resize an image node | missing, Bevy's resources reached through reflection, here GlobalUiDebugOptions |
| [`ime_support`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/ime_support.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/ime-support/) | Demonstrates IME (Input Method Editor) support for text input | missing, Bevy's resources reached through reflection, here InputFocus |
| [`letter_spacing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/letter_spacing.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/letter-spacing/) | Demonstrates the letter spacing feature | [written in part](../BevyCSharp.Examples/ui/letter_spacing.cs), the underline under the heading |
| [`multiline_text_input`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/multiline_text_input.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/multiline-text-input/) | Demonstrates a single multiline EditableText widget | missing, Bevy's editable text (EditableText) |
| [`multiple_text_inputs`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/multiple_text_inputs.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/multiple-text-inputs/) | Demonstrates multiple text inputs | missing, Bevy's editable text (EditableText) |
| [`overflow`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/scroll_and_overflow/overflow.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/overflow/) | Simple example demonstrating overflow behavior | [written](../BevyCSharp.Examples/ui/overflow.cs) |
| [`overflow_clip_margin`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/scroll_and_overflow/overflow_clip_margin.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/overflow-clip-margin/) | Simple example demonstrating the OverflowClipMargin style property | [written](../BevyCSharp.Examples/ui/overflow_clip_margin.cs) |
| [`overflow_debug`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/scroll_and_overflow/overflow_debug.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/overflow-debug/) | An example to debug overflow and clipping behavior | [written](../BevyCSharp.Examples/ui/overflow_debug.cs) |
| [`relative_cursor_position`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/relative_cursor_position.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/relative-cursor-position/) | Showcases the RelativeCursorPosition component | [written](../BevyCSharp.Examples/ui/relative_cursor_position.cs), through Bevy's reflected RelativeCursorPosition |
| [`render_ui_to_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/render_ui_to_texture.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/render-ui-to-texture/) | An example of rendering UI as a part of a 3D world | missing, observers of the pointer pressing and dragging over an interface drawn into a texture |
| [`scroll`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/scroll_and_overflow/scroll.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/scroll/) | Demonstrates scrolling UI containers | missing, observers of the pointer over scrolled nodes, and mouse wheel events as messages |
| [`scrollbars`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/scroll_and_overflow/scrollbars.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/scrollbars/) | Demonstrates use of core scrollbar in Bevy UI | missing, Bevy's core scrollbars |
| [`size_constraints`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/layout/size_constraints.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/size-constraints/) | Demonstrates how the to use the size constraints to control the size of a UI node. | [written](../BevyCSharp.Examples/ui/size_constraints.cs) |
| [`stacked_gradients`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/styling/stacked_gradients.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/stacked-gradients/) | An example demonstrating stacked gradients | [written](../BevyCSharp.Examples/ui/stacked_gradients.cs), through Bevy's reflected BackgroundGradient |
| [`standard_widgets`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/standard_widgets.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/standard-widgets/) | Demonstrates use of core (headless) widgets in Bevy UI | missing, Bevy's core widgets (bevy_ui_widgets) |
| [`standard_widgets_observers`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/standard_widgets_observers.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/standard-widgets-observers/) | Demonstrates use of core (headless) widgets in Bevy UI, with Observers | missing, Bevy's core widgets (bevy_ui_widgets) |
| [`strikethrough_and_underline`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/strikethrough_and_underline.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/strikethrough-and-underline/) | Demonstrates how to display text with strikethrough and underline. | missing, strikethrough and underline on text |
| [`system_fonts`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/system_fonts.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/system-fonts/) | Demonstrates how to use system fonts | missing, system fonts |
| [`tab_navigation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/tab_navigation.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/tab-navigation/) | Demonstration of Tab Navigation between UI elements | missing, tab navigation between interface nodes |
| [`text`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/text.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/text/) | Illustrates creating and updating text | missing, an underline and OpenType font features |
| [`text_background_colors`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/text_background_colors.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/text-background-colors/) | Demonstrates text background colors | missing, text background colors |
| [`text_debug`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/text_debug.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/text-debug/) | An example for debugging text layout | [written](../BevyCSharp.Examples/ui/text_debug.cs) |
| [`text_input`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/text_input.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/text-input/) | Demonstrates a simple, unstyled text input widget | missing, Bevy's editable text (EditableText) |
| [`text_wrap_debug`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/text_wrap_debug.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/text-wrap-debug/) | Demonstrates text wrapping | [written](../BevyCSharp.Examples/ui/text_wrap_debug.cs) |
| [`transparency_ui`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/styling/transparency_ui.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/transparency-ui/) | Demonstrates transparency for UI | [written](../BevyCSharp.Examples/ui/transparency_ui.cs) |
| [`ui_drag_and_drop`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/ui_drag_and_drop.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/ui-drag-and-drop/) | Demonstrates dragging and dropping UI nodes | missing, observers of the pointer dragging and dropping interface nodes |
| [`ui_material`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/ui_material.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/ui-material/) | Demonstrates creating and using custom Ui materials | missing, interface materials (UiMaterial) |
| [`ui_scaling`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/ui_scaling.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/ui-scaling/) | Illustrates how to scale the UI | missing, Bevy's resources reached through reflection, here UiScale |
| [`ui_target_camera`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/ui_target_camera.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/ui-target-camera/) | Demonstrates how to use `UiTargetCamera` and camera ordering. | [written](../BevyCSharp.Examples/ui/ui_target_camera.cs) |
| [`ui_texture_atlas`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/images/ui_texture_atlas.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/ui-texture-atlas/) | Illustrates how to use TextureAtlases in UI | [written](../BevyCSharp.Examples/ui/ui_texture_atlas.cs) |
| [`ui_texture_atlas_slice`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/images/ui_texture_atlas_slice.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/ui-texture-atlas-slice/) | Illustrates how to use 9 Slicing for TextureAtlases in UI | [written](../BevyCSharp.Examples/ui/ui_texture_atlas_slice.cs) |
| [`ui_texture_slice`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/images/ui_texture_slice.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/ui-texture-slice/) | Illustrates how to use 9 Slicing in UI | [written](../BevyCSharp.Examples/ui/ui_texture_slice.cs) |
| [`ui_texture_slice_flip_and_tile`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/images/ui_texture_slice_flip_and_tile.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/ui-texture-slice-flip-and-tile/) | Illustrates how to flip and tile images with 9 Slicing in UI | missing, Bevy's resources reached through reflection, here UiScale |
| [`ui_transform`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/ui_transform.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/ui-transform/) | An example demonstrating how to translate, rotate and scale UI elements. | [written](../BevyCSharp.Examples/ui/ui_transform.cs), through Bevy's reflected UiTransform |
| [`vertical_slider`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/vertical_slider.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/vertical-slider/) | Simple example showing vertical and horizontal slider widgets with snap behavior and value labels | missing, Bevy's core widgets (bevy_ui_widgets) |
| [`viewport_node`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/viewport_node.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/viewport-node/) | Demonstrates how to create a viewport node with picking support | missing, observers of the pointer dragging a mesh and the node that shows it |
| [`virtual_keyboard`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/virtual_keyboard.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/virtual-keyboard/) | Example demonstrating a virtual keyboard widget | missing, Bevy's Feathers widgets |
| [`window_fallthrough`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/window_fallthrough.rs) | Illustrates how to access `winit::window::Window`'s `hittest` functionality. | missing, letting the pointer pass through the window to what is behind it |
| [`z_index`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/layout/z_index.rs), [live in Bevy](https://bevy.org/examples/ui-user-interface/z-index/) | Demonstrates how to control the relative depth (z-position) of UI elements | [written](../BevyCSharp.Examples/ui/z_index.cs), through Bevy's reflected ZIndex and GlobalZIndex |

## Usage

| Example | What it shows | State |
|---|---|---|
| [`context_menu`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/usage/context_menu.rs), [live in Bevy](https://bevy.org/examples/usage/context-menu/) | Example of a context menu | can be written |
| [`cooldown`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/usage/cooldown.rs), [live in Bevy](https://bevy.org/examples/usage/cooldown/) | Example for cooldown on button clicks | can be written |
| [`debug_frustum_culling`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/usage/debug_frustum_culling.rs), [live in Bevy](https://bevy.org/examples/usage/debug-frustum-culling/) | Example demonstrating how to debug frustum culling | can be written |

## Window

| Example | What it shows | State |
|---|---|---|
| [`clear_color`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/clear_color.rs), [live in Bevy](https://bevy.org/examples/window/clear-color/) | Creates a solid color window | can be written |
| [`custom_cursor_image`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/custom_cursor_image.rs), [live in Bevy](https://bevy.org/examples/window/custom-cursor-image/) | Demonstrates creating an animated custom cursor from an image | can be written, through Bevy's reflected CursorIcon, its image set as a reflected asset |
| [`low_power`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/low_power.rs), [live in Bevy](https://bevy.org/examples/window/low-power/) | Demonstrates settings to reduce power use for bevy applications | can be written |
| [`monitor_info`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/monitor_info.rs) | Displays information about available monitors (displays). | can be written |
| [`multiple_windows`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/multiple_windows.rs) | Demonstrates creating multiple windows, and rendering to them | missing, a second window |
| [`scale_factor_override`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/scale_factor_override.rs), [live in Bevy](https://bevy.org/examples/window/scale-factor-override/) | Illustrates how to customize the default window settings | can be written, through the resolution of Bevy's reflected Window |
| [`screenshot`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/screenshot.rs), [live in Bevy](https://bevy.org/examples/window/screenshot/) | Shows how to save screenshots to disk | can be written |
| [`transparent_window`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/transparent_window.rs) | Illustrates making the window transparent and hiding the window decoration | can be written |
| [`window_drag_move`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/window_drag_move.rs) | Demonstrates drag move and drag resize without window decoration | can be written |
| [`window_resizing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/window_resizing.rs), [live in Bevy](https://bevy.org/examples/window/window-resizing/) | Demonstrates resizing and responding to resizing a window | can be written |
| [`window_settings`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/window_settings.rs), [live in Bevy](https://bevy.org/examples/window/window-settings/) | Demonstrates customizing default window settings | can be written |

## glTF

| Example | What it shows | State |
|---|---|---|
| [`custom_gltf_vertex_attribute`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/custom_gltf_vertex_attribute.rs), [live in Bevy](https://bevy.org/examples/gltf/custom-gltf-vertex-attribute/) | Renders a glTF mesh in 2D with a custom vertex attribute | does not apply, maps a glTF attribute to a Rust-defined vertex attribute and 2D material |
| [`edit_material_on_gltf`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/edit_material_on_gltf.rs), [live in Bevy](https://bevy.org/examples/gltf/edit-material-on-gltf/) | Showcases changing materials of a glTF after Scene spawn | can be written |
| [`gltf_extension_animation_graph`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/gltf_extension_animation_graph.rs), [live in Bevy](https://bevy.org/examples/gltf/gltf-extension-animation-graph/) | Uses glTF data to build an AnimationGraph via extension processing | does not apply, writes a Rust glTF extension handler |
| [`gltf_extension_mesh_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/gltf_extension_mesh_2d.rs), [live in Bevy](https://bevy.org/examples/gltf/gltf-extension-mesh-2d/) | Uses glTF extension data to convert incoming Mesh3d/MeshMaterial3d assets to 2d | does not apply, writes a Rust glTF extension handler |
| [`gltf_skinned_mesh`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/gltf_skinned_mesh.rs), [live in Bevy](https://bevy.org/examples/gltf/gltf-skinned-mesh/) | Skinned mesh example with mesh and joints data loaded from a glTF file | can be written |
| [`load_gltf`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/load_gltf.rs), [live in Bevy](https://bevy.org/examples/gltf/load-gltf/) | Loads and renders a glTF file as a scene | can be written |
| [`load_gltf_extras`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/load_gltf_extras.rs), [live in Bevy](https://bevy.org/examples/gltf/load-gltf-extras/) | Loads and renders a glTF file as a scene, including the gltf extras | missing, a glTF node's extras |
| [`query_gltf_primitives`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/query_gltf_primitives.rs), [live in Bevy](https://bevy.org/examples/gltf/query-gltf-primitives/) | Query primitives in a glTF scene | can be written |
| [`update_gltf_scene`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/update_gltf_scene.rs), [live in Bevy](https://bevy.org/examples/gltf/update-gltf-scene/) | Update a scene from a glTF file, either by spawning the scene as a child of another entity, or by accessing the entities of the scene | can be written |

## Kept out of Bevy's list

| Example | What it shows | State |
|---|---|---|
| [`ambiguity_detection`](https://github.com/bevyengine/bevy/blob/v0.19.1/tests/ecs/ambiguity_detection.rs) |   | does not apply, a test of Bevy's schedules, hidden from its list |
| [`desktop_request_redraw`](https://github.com/bevyengine/bevy/blob/v0.19.1/tests/window/desktop_request_redraw.rs) |   | does not apply, a test of winit's redraw requests, hidden from Bevy's list |
| [`fallback_image`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/fallback_image.rs) |   | does not apply, a test of shader fallback images, hidden from Bevy's list |
| [`hello_world`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/hello_world.rs) |   | can be written |
| [`minimizing`](https://github.com/bevyengine/bevy/blob/v0.19.1/tests/window/minimizing.rs) |   | can be written |
| [`no_prepass`](https://github.com/bevyengine/bevy/blob/v0.19.1/tests/3d/no_prepass.rs) |   | does not apply, a test of the prepass, hidden from Bevy's list |
| [`resizing`](https://github.com/bevyengine/bevy/blob/v0.19.1/tests/window/resizing.rs) |   | can be written |
| [`test_invalid_skinned_mesh`](https://github.com/bevyengine/bevy/blob/v0.19.1/tests/3d/test_invalid_skinned_mesh.rs) |   | does not apply, a test of skinned mesh validation, hidden from Bevy's list |
| [`test_skinned_mesh_bounds`](https://github.com/bevyengine/bevy/blob/v0.19.1/tests/3d/test_skinned_mesh_bounds.rs) |   | does not apply, a test of skinned mesh bounds, hidden from Bevy's list |
| [`testbed_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/testbed/2d.rs) |   | does not apply, Bevy's visual regression scenes, hidden from its list |
| [`testbed_3d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/testbed/3d.rs) |   | does not apply, Bevy's visual regression scenes, hidden from its list |
| [`testbed_full_ui`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/testbed/full_ui.rs) |   | does not apply, Bevy's visual regression scenes, hidden from its list |
| [`testbed_ui`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/testbed/ui.rs) |   | does not apply, Bevy's visual regression scenes, hidden from its list |
