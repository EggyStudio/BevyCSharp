# Examples

Bevy 0.19.1 has 421 examples, 408 of them in the list its `examples/README.md` keeps and 13 kept out of it. Each is a row here, made by `build/examples-table.py` from Bevy's own metadata, so a row is a feature of Bevy and the table is how much of Bevy a C# game can reach.

An example written here is a program in `BevyCSharp.Examples`, under Bevy's name, opened by `dotnet run --project BevyCSharp.Examples -- <name>` or `./bcs open --example <name>`. One `written in part` leaves out a feature of Bevy's the bridge lacks and names it. One that `can be written` uses only what is bridged and waits for its turn. One that is `missing` names what the bridge lacks, and one that `does not apply` says why it is not a thing a C# game does, most often because it is about Rust itself. A difference that is no feature, such as a view sized for another window, is said in a written row and keeps it written.

**31 written, 3 written in part, 260 can be written, 74 missing and 53 do not apply.** Of the 368 that apply, 294 can be written with what is bridged, 3 of them leaving something out.

| Group | Written | Written in part | Can be written | Missing | Does not apply |
|---|---:|---:|---:|---:|---:|
| [2D Rendering](#2d-rendering) | 0 | 0 | 20 | 8 | 1 |
| [3D Rendering](#3d-rendering) | 31 | 3 | 30 | 3 | 0 |
| [Animation](#animation) | 0 | 0 | 6 | 7 | 0 |
| [Application](#application) | 0 | 0 | 13 | 1 | 5 |
| [Assets](#assets) | 0 | 0 | 10 | 2 | 5 |
| [Async Tasks](#async-tasks) | 0 | 0 | 3 | 0 | 0 |
| [Audio](#audio) | 0 | 0 | 6 | 2 | 0 |
| [Camera](#camera) | 0 | 0 | 8 | 1 | 0 |
| [Dev tools](#dev-tools) | 0 | 0 | 1 | 1 | 1 |
| [Diagnostics](#diagnostics) | 0 | 0 | 1 | 2 | 0 |
| [ECS (Entity Component System)](#ecs-entity-component-system) | 0 | 0 | 24 | 3 | 8 |
| [Embedded](#embedded) | 0 | 0 | 0 | 0 | 1 |
| [Games](#games) | 0 | 0 | 6 | 0 | 0 |
| [Gizmos](#gizmos) | 0 | 0 | 4 | 5 | 0 |
| [Helpers](#helpers) | 0 | 0 | 0 | 1 | 0 |
| [Input](#input) | 0 | 0 | 12 | 0 | 0 |
| [Math](#math) | 0 | 0 | 5 | 0 | 1 |
| [Movement](#movement) | 0 | 0 | 1 | 0 | 0 |
| [Picking](#picking) | 0 | 0 | 2 | 3 | 1 |
| [Reflection](#reflection) | 0 | 0 | 0 | 0 | 9 |
| [Remote Protocol](#remote-protocol) | 0 | 0 | 0 | 3 | 1 |
| [Scene](#scene) | 0 | 0 | 1 | 1 | 0 |
| [Shaders](#shaders) | 0 | 0 | 17 | 3 | 5 |
| [Shaders Advanced](#shaders-advanced) | 0 | 0 | 1 | 0 | 1 |
| [State](#state) | 0 | 0 | 4 | 0 | 0 |
| [Stress Tests](#stress-tests) | 0 | 0 | 18 | 2 | 1 |
| [Time](#time) | 0 | 0 | 3 | 0 | 0 |
| [Tools](#tools) | 0 | 0 | 2 | 0 | 0 |
| [Transforms](#transforms) | 0 | 0 | 5 | 0 | 0 |
| [UI (User Interface)](#ui-user-interface) | 0 | 0 | 36 | 24 | 0 |
| [Usage](#usage) | 0 | 0 | 3 | 0 | 0 |
| [Window](#window) | 0 | 0 | 10 | 1 | 0 |
| [glTF](#gltf) | 0 | 0 | 5 | 1 | 3 |
| [Kept out of Bevy's list](#kept-out-of-bevys-list) | 0 | 0 | 3 | 0 | 10 |
| **All** | **31** | **3** | **260** | **74** | **53** |

A row's example links to Bevy's source, at the release the bridge builds. A written one's state links to its program here, and its capture is in `.github/assets/examples`.

## 2D Rendering

| Example | What it shows | State |
|---|---|---|
| [`2d_shapes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/2d_shapes.rs) | Renders simple 2D primitive shapes like circles and polygons | can be written |
| [`2d_viewport_to_world`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/2d_viewport_to_world.rs) | Demonstrates how to use the `Camera::viewport_to_world_2d` method with a dynamic viewport and camera. | can be written |
| [`bloom_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/bloom_2d.rs) | Illustrates bloom post-processing in 2d | can be written |
| [`cpu_draw`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/cpu_draw.rs) | Manually read/write the pixels of a texture | can be written |
| [`dynamic_mip_generation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/dynamic_mip_generation.rs) | Demonstrates use of the mipmap generation plugin to generate mipmaps for a texture | missing, generating an image's mipmaps on the GPU |
| [`mesh2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/mesh2d.rs) | Renders a 2d mesh | missing, 2D meshes (Mesh2d with ColorMaterial) |
| [`mesh2d_alpha_mode`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/mesh2d_alpha_mode.rs) | Used to test alpha modes with mesh2d | missing, 2D meshes (Mesh2d with ColorMaterial) |
| [`mesh2d_arcs`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/mesh2d_arcs.rs) | Demonstrates UV-mapping of the circular segment and sector primitives | missing, 2D meshes (Mesh2d with ColorMaterial) and the arc primitives |
| [`mesh2d_manual`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/mesh2d_manual.rs) | Renders a custom mesh "manually" with "mid-level" renderer apis | does not apply, writes a render pipeline in Rust with the mid-level render API |
| [`mesh2d_repeated_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/mesh2d_repeated_texture.rs) | Showcase of using `uv_transform` on the `ColorMaterial` of a `Mesh2d` | missing, 2D meshes (Mesh2d with ColorMaterial) and its uv transform |
| [`mesh2d_vertex_color_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/mesh2d_vertex_color_texture.rs) | Renders a 2d mesh with vertex color attributes | missing, 2D meshes (Mesh2d with ColorMaterial) |
| [`move_sprite`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/move_sprite.rs) | Changes the transform of a sprite | can be written |
| [`multi_window_text`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/multi_window_text.rs) | Renders text to multiple windows with different scale factors using both Text and Text2d | missing, a second window |
| [`pixel_grid_snap`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/pixel_grid_snap.rs) | Shows how to create graphics that snap to the pixel grid by rendering to a texture in 2D | can be written |
| [`rotate_to_cursor`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/rotate_to_cursor.rs) | Demonstrates rotating entities in 2D to follow the cursor | can be written |
| [`rotation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/rotation.rs) | Demonstrates rotating entities in 2D with quaternions | can be written |
| [`sprite`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/sprite.rs) | Renders a sprite | can be written |
| [`sprite_animation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/sprite_animation.rs) | Animates a sprite in response to an event | can be written |
| [`sprite_flipping`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/sprite_flipping.rs) | Renders a sprite flipped along an axis | can be written |
| [`sprite_scale`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/sprite_scale.rs) | Shows how a sprite can be scaled into a rectangle while keeping the aspect ratio | can be written |
| [`sprite_sheet`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/sprite_sheet.rs) | Renders an animated sprite | can be written |
| [`sprite_slice`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/sprite_slice.rs) | Showcases slicing sprites into sections that can be scaled independently via the 9-patch technique | can be written |
| [`sprite_tile`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/sprite_tile.rs) | Renders a sprite tiled in a grid | can be written |
| [`text2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/text2d.rs) | Generates text in 2D | can be written, through Bevy's reflected Text2d |
| [`texture_atlas`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/texture_atlas.rs) | Generates a texture atlas (sprite sheet) from individual sprites | can be written |
| [`tilemap_chunk`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/tilemap_chunk.rs) | Renders a tilemap chunk | can be written, through Bevy's reflected TilemapChunk and its tile data |
| [`tilemap_chunk_orientation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/tilemap_chunk_orientation.rs) | Renders a tilemap chunk using tile orientations (mirrored, rotated) | can be written, through Bevy's reflected TilemapChunk and its tile data |
| [`transparency_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/transparency_2d.rs) | Demonstrates transparency in 2d | can be written |
| [`wireframe_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/2d/wireframe_2d.rs) | Showcases wireframes for 2d meshes | missing, 2D meshes (Mesh2d) and their wireframes |

## 3D Rendering

| Example | What it shows | State |
|---|---|---|
| [`3d_scene`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/3d_scene.rs) | Simple 3D scene with basic shapes and lighting | [written](../BevyCSharp.Examples/3d/3d_scene.cs) |
| [`3d_shapes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/3d_shapes.rs) | A scene showcasing the built-in 3D shapes | [written in part](../BevyCSharp.Examples/3d/3d_shapes.cs), the segment, the polyline and the extrusions, which are shapes the bridge does not build |
| [`3d_viewport_to_world`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/3d_viewport_to_world.rs) | Demonstrates how to use the `Camera::viewport_to_world` method | [written](../BevyCSharp.Examples/3d/3d_viewport_to_world.cs) |
| [`animated_material`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/animated_material.rs) | Shows how to animate material properties | [written](../BevyCSharp.Examples/3d/animated_material.cs) |
| [`anisotropy`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/anisotropy.rs) | Displays an example model with anisotropy | can be written |
| [`anti_aliasing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/anti_aliasing.rs) | Compares different anti-aliasing techniques supported by Bevy | can be written |
| [`atmosphere`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/atmosphere.rs) | A scene showcasing pbr atmospheric scattering | can be written |
| [`atmospheric_fog`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/atmospheric_fog.rs) | A scene showcasing the atmospheric fog effect | [written](../BevyCSharp.Examples/3d/atmospheric_fog.cs), through Bevy's reflected DistanceFog |
| [`auto_exposure`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/auto_exposure.rs) | A scene showcasing auto exposure | [written](../BevyCSharp.Examples/3d/auto_exposure.cs) |
| [`blend_modes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/blend_modes.rs) | Showcases different blend modes | can be written |
| [`bloom_3d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/bloom_3d.rs) | Illustrates bloom configuration using HDR and emissive materials | [written](../BevyCSharp.Examples/3d/bloom_3d.cs) |
| [`camera_sub_view`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/camera_sub_view.rs) | Demonstrates using different sub view effects on a camera | can be written, through the sub view of Bevy's reflected Camera |
| [`clearcoat`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/clearcoat.rs) | Demonstrates the clearcoat PBR feature | [written](../BevyCSharp.Examples/3d/clearcoat.cs) |
| [`clustered_decal_maps`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/clustered_decal_maps.rs) | Demonstrates normal and metallic-roughness maps of decals | can be written, through Bevy's reflected ClusteredDecal |
| [`clustered_decals`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/clustered_decals.rs) | Demonstrates clustered decals | can be written, through Bevy's reflected ClusteredDecal |
| [`color_grading`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/color_grading.rs) | Demonstrates color grading | can be written |
| [`contact_shadows`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/contact_shadows.rs) | Showcases how contact shadows add shadow detail | can be written |
| [`decal`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/decal.rs) | Decal rendering | missing, forward decals (ForwardDecal) |
| [`deferred_rendering`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/deferred_rendering.rs) | Renders meshes with both forward and deferred pipelines | can be written |
| [`depth_of_field`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/depth_of_field.rs) | Demonstrates depth of field | can be written |
| [`fog`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/fog.rs) | A scene showcasing the distance fog effect | [written](../BevyCSharp.Examples/3d/fog.cs), through Bevy's reflected DistanceFog |
| [`fog_volumes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/fog_volumes.rs) | Demonstrates fog volumes | [written](../BevyCSharp.Examples/3d/fog_volumes.cs), through Bevy's reflected FogVolume |
| [`generate_custom_mesh`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/generate_custom_mesh.rs) | Simple showcase of how to generate a custom mesh with a custom texture | [written](../BevyCSharp.Examples/3d/generate_custom_mesh.cs) |
| [`irradiance_volumes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/irradiance_volumes.rs) | Demonstrates irradiance volumes | can be written, needs Bevy's irradiance volume asset |
| [`light_probe_blending`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/light_probe_blending.rs) | Demonstrates blending between multiple reflection probes | can be written |
| [`light_textures`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/light_textures.rs) | Demonstrates light textures | can be written |
| [`lighting`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/lighting.rs) | Illustrates various lighting options in a simple scene | [written](../BevyCSharp.Examples/3d/lighting.cs) |
| [`lightmaps`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/lightmaps.rs) | Rendering a scene with baked lightmaps | can be written, through Bevy's reflected Lightmap |
| [`lines`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/lines.rs) | Create a custom material to draw 3d lines | [written](../BevyCSharp.Examples/3d/lines.cs) |
| [`mesh_ray_cast`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/mesh_ray_cast.rs) | Demonstrates ray casting with the `MeshRayCast` system parameter | can be written, casts through Picking, which the editor profile carries |
| [`meshlet`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/meshlet.rs) | Meshlet rendering for dense high-poly scenes (experimental) | can be written, needs a bridge built with --meshlet |
| [`mirror`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/mirror.rs) | Demonstrates how to create a mirror with a second camera | can be written |
| [`mixed_lighting`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/mixed_lighting.rs) | Demonstrates how to combine baked and dynamic lighting | can be written, through Bevy's reflected Lightmap |
| [`motion_blur`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/motion_blur.rs) | Demonstrates per-pixel motion blur | can be written |
| [`occlusion_culling`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/occlusion_culling.rs) | Demonstration of Occlusion Culling | can be written, through Bevy's reflected OcclusionCulling |
| [`order_independent_transparency`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/order_independent_transparency.rs) | Demonstrates how to use OIT | [written](../BevyCSharp.Examples/3d/order_independent_transparency.cs) |
| [`orthographic`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/orthographic.rs) | Shows how to create a 3D orthographic view (for isometric-look in games or CAD applications) | [written](../BevyCSharp.Examples/3d/orthographic.cs) |
| [`parallax_mapping`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/parallax_mapping.rs) | Demonstrates use of a normal map and depth map for parallax mapping | missing, a depth map and parallax settings on the standard material |
| [`parenting`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/parenting.rs) | Demonstrates parent->child relationships and relative transformations | [written](../BevyCSharp.Examples/3d/parenting.cs) |
| [`pbr`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/pbr.rs) | Demonstrates use of Physically Based Rendering (PBR) properties | [written](../BevyCSharp.Examples/3d/pbr.cs), its view sized for a window of 1280 by 720 at any size |
| [`pccm`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/pccm.rs) | Demonstrates parallax-corrected cubemap reflections | can be written, through Bevy's reflected ParallaxCorrection |
| [`pcss`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/pcss.rs) | Demonstrates percentage-closer soft shadows (PCSS) | can be written |
| [`post_processing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/post_processing.rs) | Demonstrates the built-in postprocessing features | [written](../BevyCSharp.Examples/3d/post_processing.cs) |
| [`rect_light`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/rect_light.rs) | Simple 3D scene demonstrating rectangular area lights. | [written](../BevyCSharp.Examples/3d/rect_light.cs), through Bevy's reflected RectLight |
| [`reflection_probes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/reflection_probes.rs) | Demonstrates reflection probes | can be written |
| [`render_to_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/render_to_texture.rs) | Shows how to render to a texture, useful for mirrors, UI, or exporting images | [written](../BevyCSharp.Examples/3d/render_to_texture.cs) |
| [`rotate_environment_map`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/rotate_environment_map.rs) | Demonstrates how to rotate the skybox and the environment map simultaneously | [written](../BevyCSharp.Examples/3d/rotate_environment_map.cs) |
| [`scrolling_fog`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/scrolling_fog.rs) | Demonstrates how to create the effect of fog moving in the wind | [written](../BevyCSharp.Examples/3d/scrolling_fog.cs), through Bevy's reflected FogVolume, its density texture set as a reflected asset |
| [`shadow_biases`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/shadow_biases.rs) | Demonstrates how shadow biases affect shadows in a 3d scene | can be written |
| [`shadow_caster_receiver`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/shadow_caster_receiver.rs) | Demonstrates how to prevent meshes from casting/receiving shadows in a 3d scene | [written](../BevyCSharp.Examples/3d/shadow_caster_receiver.cs) |
| [`skybox`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/skybox.rs) | Load a cubemap texture onto a cube like a skybox and cycle through different compressed texture formats. | [written in part](../BevyCSharp.Examples/3d/skybox.cs), the ASTC and ETC2 cubemaps, since the bridge does not say which compressed formats the GPU decodes |
| [`solari`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/solari.rs) | Demonstrates realtime dynamic raytraced lighting using Bevy Solari. | can be written, needs a bridge built with --solari and an adapter with ray queries |
| [`specular_tint`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/specular_tint.rs) | Demonstrates specular tints and maps | missing, specular tint and the specular maps on the standard material |
| [`spherical_area_lights`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/spherical_area_lights.rs) | Demonstrates how point light radius values affect light behavior | [written](../BevyCSharp.Examples/3d/spherical_area_lights.cs) |
| [`split_screen`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/split_screen.rs) | Demonstrates how to render two cameras to the same window to accomplish "split screen" | [written](../BevyCSharp.Examples/3d/split_screen.cs) |
| [`spotlight`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/spotlight.rs) | Illustrates spot lights | [written](../BevyCSharp.Examples/3d/spotlight.cs), its cubes scattered by .NET's generator rather than Bevy's |
| [`ssao`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/ssao.rs) | A scene showcasing screen space ambient occlusion | [written](../BevyCSharp.Examples/3d/ssao.cs) |
| [`ssr`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/ssr.rs) | Demonstrates screen space reflections with water ripples | can be written |
| [`texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/texture.rs) | Shows configuration of texture materials | [written](../BevyCSharp.Examples/3d/texture.cs) |
| [`tonemapping`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/tonemapping.rs) | Compares tonemapping options | can be written |
| [`transmission`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/transmission.rs) | Showcases light transmission in the PBR material | can be written |
| [`transparency_3d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/transparency_3d.rs) | Demonstrates transparency in 3d | [written in part](../BevyCSharp.Examples/3d/transparency_3d.cs), its alpha to coverage cube, a mode the bridge's materials do not have |
| [`two_passes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/two_passes.rs) | Renders two 3d passes to the same window from different perspectives | [written](../BevyCSharp.Examples/3d/two_passes.cs) |
| [`vertex_colors`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/vertex_colors.rs) | Shows the use of vertex colors | [written](../BevyCSharp.Examples/3d/vertex_colors.cs) |
| [`visibility_range`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/visibility_range.rs) | Demonstrates visibility ranges | can be written, through Bevy's reflected VisibilityRange |
| [`volumetric_fog`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/volumetric_fog.rs) | Demonstrates volumetric fog and lighting | [written](../BevyCSharp.Examples/3d/volumetric_fog.cs), through Bevy's reflected VolumetricFog, FogVolume and VolumetricLight |
| [`wireframe`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/3d/wireframe.rs) | Showcases wireframe rendering | [written](../BevyCSharp.Examples/3d/wireframe.cs) |

## Animation

| Example | What it shows | State |
|---|---|---|
| [`animated_mesh`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animated_mesh.rs) | Plays an animation on a skinned glTF model of a fox | can be written |
| [`animated_mesh_control`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animated_mesh_control.rs) | Plays an animation from a skinned glTF with keyboard controls | can be written |
| [`animated_mesh_events`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animated_mesh_events.rs) | Plays an animation from a skinned glTF with events | missing, events placed on an animation clip |
| [`animated_transform`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animated_transform.rs) | Create and play an animation defined by code that operates on the `Transform` component | missing, animation clips built in code (AnimationClip with curves) |
| [`animated_ui`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animated_ui.rs) | Shows how to use animation clips to animate UI properties | missing, animation clips built in code that drive interface properties |
| [`animation_events`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animation_events.rs) | Demonstrate how to use animation events | missing, events placed on an animation clip |
| [`animation_graph`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animation_graph.rs) | Blends multiple animations together with a graph | missing, animation graphs that blend clips by weight |
| [`animation_masks`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/animation_masks.rs) | Demonstrates animation masks | missing, animation masks on a graph |
| [`color_animation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/color_animation.rs) | Demonstrates how to animate colors using mixing and splines in different color spaces | can be written, mixing and splines over colors, written with the managed math |
| [`custom_skinned_mesh`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/custom_skinned_mesh.rs) | Skinned mesh example with mesh and joints data defined in code | missing, skinned meshes built in code (joints and weights) |
| [`eased_motion`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/eased_motion.rs) | Demonstrates the application of easing curves to animate an object | can be written |
| [`easing_functions`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/easing_functions.rs) | Showcases the built-in easing functions | can be written |
| [`morph_targets`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/animation/morph_targets.rs) | Plays an animation from a glTF file with meshes with morph targets | can be written, through Bevy's reflected MorphWeights |

## Application

| Example | What it shows | State |
|---|---|---|
| [`custom_loop`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/custom_loop.rs) | Demonstrates how to create a custom runner (to update an app manually) | does not apply, replaces Bevy's runner, which the bridge owns |
| [`drag_and_drop`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/drag_and_drop.rs) | An example that shows how to handle drag and drop in an app | can be written |
| [`empty`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/empty.rs) | An empty application (does nothing) | can be written |
| [`empty_defaults`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/empty_defaults.rs) | An empty application with default plugins | can be written |
| [`externally_driven_headless_renderer`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/externally_driven_headless_renderer.rs) | Using bevy with manually driven update to render images | does not apply, drives Bevy's update from Rust code |
| [`headless`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/headless.rs) | An application that runs without default plugins | can be written |
| [`headless_renderer`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/headless_renderer.rs) | An application that runs with no window, but renders into image file | can be written |
| [`log_layers`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/log_layers.rs) | Illustrate how to add custom log layers | does not apply, adds a tracing layer, written in Rust |
| [`log_layers_ecs`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/log_layers_ecs.rs) | Illustrate how to transfer data between log layers and Bevy's ECS | does not apply, adds a tracing layer, written in Rust |
| [`logs`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/logs.rs) | Illustrate how to use generate log output | can be written |
| [`no_renderer`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/no_renderer.rs) | An application that runs with default plugins and displays an empty window, but without an actual renderer | can be written |
| [`persisting_window_settings`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/persisting_window_settings.rs) | Demonstrates saving window position settings | can be written |
| [`plugin`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/plugin.rs) | Demonstrates the creation and registration of a custom plugin | can be written |
| [`plugin_group`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/plugin_group.rs) | Demonstrates the creation and registration of a custom plugin group | can be written |
| [`render_recovery`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/render_recovery.rs) | Demonstrates how bevy can recover from rendering failures. | missing, recovering from a lost GPU device |
| [`return_after_run`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/return_after_run.rs) | Show how to return to main after the Bevy app has exited | can be written |
| [`settings`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/settings.rs) | Demonstrates persistence of settings | can be written |
| [`thread_pool_resources`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/thread_pool_resources.rs) | Creates and customizes the internal thread pool | does not apply, tunes Bevy's task pools, which the bridge sets up |
| [`without_winit`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/app/without_winit.rs) | Create an application without winit (runs single time, no event loop) | can be written |

## Assets

| Example | What it shows | State |
|---|---|---|
| [`alter_mesh`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/alter_mesh.rs) | Shows how to modify the underlying asset of a Mesh after spawning. | can be written |
| [`alter_sprite`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/alter_sprite.rs) | Shows how to modify texture assets after spawning. | can be written |
| [`asset_decompression`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/asset_decompression.rs) | Demonstrates loading a compressed asset | does not apply, writes a Rust asset loader |
| [`asset_loading`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/asset_loading.rs) | Demonstrates various methods to load assets | can be written |
| [`asset_processing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/processing/asset_processing.rs) | Demonstrates how to process and load custom assets | does not apply, writes Rust asset processors |
| [`asset_saving`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/asset_saving.rs) | Demonstrates how to save an asset | does not apply, writes a Rust asset saver |
| [`asset_saving_with_subassets`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/asset_saving_with_subassets.rs) | Demonstrates how to save an asset with subassets | does not apply, writes a Rust asset saver |
| [`asset_settings`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/asset_settings.rs) | Demonstrates various methods of applying settings when loading an asset | missing, settings given to a loader per load, such as an image's sampler |
| [`custom_asset`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/custom_asset.rs) | Implements a custom asset loader | can be written, as a data asset, which is how a C# game has assets of its own types |
| [`custom_asset_reader`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/custom_asset_reader.rs) | Implements a custom AssetReader | does not apply, writes a Rust AssetReader |
| [`embedded_asset`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/embedded_asset.rs) | Embed an asset in the application binary and load it | can be written |
| [`extra_asset_source`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/extra_source.rs) | Load an asset from a non-standard asset source | can be written |
| [`generated_assets`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/generated_assets.rs) | Shows how to generate and store assets at runtime | can be written |
| [`hot_asset_reloading`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/hot_asset_reloading.rs) | Demonstrates automatic reloading of assets when modified on disk | can be written |
| [`multi_asset_sync`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/multi_asset_sync.rs) | Demonstrates how to wait for multiple assets to be loaded. | can be written |
| [`repeated_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/repeated_texture.rs) | How to configure the texture to repeat instead of the default clamp to edges | can be written |
| [`web_asset`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/asset/web_asset.rs) | Load an asset from the web | missing, loading assets over HTTP |

## Async Tasks

| Example | What it shows | State |
|---|---|---|
| [`async_channel_pattern`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/async_tasks/async_channel_pattern.rs) | An example showing how to offload work to background async tasks using channels for communication. | can be written, with .NET's tasks and channels |
| [`async_compute`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/async_tasks/async_compute.rs) | How to use `AsyncComputeTaskPool` to complete longer running tasks | can be written, with .NET's tasks |
| [`external_source_external_thread`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/async_tasks/external_source_external_thread.rs) | How to use an external thread to run an infinite task and communicate with a channel | can be written, with a .NET thread and channel |

## Audio

| Example | What it shows | State |
|---|---|---|
| [`audio`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/audio.rs) | Shows how to load and play an audio file | can be written |
| [`audio_control`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/audio_control.rs) | Shows how to load and play an audio file, and control how it's played | can be written |
| [`decodable`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/decodable.rs) | Shows how to create and register a custom audio source by implementing the `Decodable` type. | missing, audio sources a game generates (Decodable) |
| [`pitch`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/pitch.rs) | Shows how to directly play a simple pitch | missing, a generated tone (Pitch) |
| [`play_sound_effect`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/play_sound_effect.rs) | Shows how to play a sound effect in response to an event | can be written |
| [`soundtrack`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/soundtrack.rs) | Shows how to play different soundtracks based on game state | can be written |
| [`spatial_audio_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/spatial_audio_2d.rs) | Shows how to play spatial audio, and moving the emitter in 2D | can be written |
| [`spatial_audio_3d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/audio/spatial_audio_3d.rs) | Shows how to play spatial audio, and moving the emitter in 3D | can be written |

## Camera

| Example | What it shows | State |
|---|---|---|
| [`2d_on_ui`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/2d_on_ui.rs) | Shows how to render 2D objects on top of Bevy UI | can be written |
| [`2d_screen_shake`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/2d_screen_shake.rs) | A simple 2D screen shake effect | can be written |
| [`2d_top_down_camera`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/2d_top_down_camera.rs) | A 2D top-down camera smoothly following player movements | can be written |
| [`camera_orbit`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/camera_orbit.rs) | Shows how to orbit a static scene using pitch, yaw, and roll. | can be written |
| [`custom_projection`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/custom_projection.rs) | Shows how to create custom camera projections. | missing, custom camera projections |
| [`first_person_view_model`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/first_person_view_model.rs) | A first-person camera that uses a world model and a view model with different field of views (FOV) | can be written |
| [`free_camera_controller`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/free_camera_controller.rs) | Demonstrates the FreeCamera controller for 3D scenes. | can be written |
| [`pan_camera_controller`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/pan_camera_controller.rs) | Example Pan-Camera Styled Camera Controller for 2D scenes | can be written |
| [`projection_zoom`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/camera/projection_zoom.rs) | Shows how to zoom orthographic and perspective projection cameras. | can be written |

## Dev tools

| Example | What it shows | State |
|---|---|---|
| [`fps_overlay`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/dev_tools/fps_overlay.rs) | Demonstrates FPS overlay | can be written |
| [`infinite_grid`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/dev_tools/infinite_grid.rs) | Demonstrates Bevy's infinite grid, suitable as a ground plane for editors | missing, Bevy's infinite grid |
| [`schedule_data`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/dev_tools/schedule_data.rs) | Extracts the schedule data from a default app and writes it to a file | does not apply, reads Bevy's schedule graphs from Rust |

## Diagnostics

| Example | What it shows | State |
|---|---|---|
| [`custom_diagnostic`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/diagnostics/custom_diagnostic.rs) | Shows how to create a custom diagnostic | missing, diagnostics a game registers in Bevy's store |
| [`enabling_disabling_diagnostic`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/diagnostics/enabling_disabling_diagnostic.rs) | Shows how to disable/re-enable a Diagnostic during runtime | missing, turning one of Bevy's diagnostics on and off |
| [`log_diagnostics`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/diagnostics/log_diagnostics.rs) | Add a plugin that logs diagnostics, like frames per second (FPS), to the console | can be written, through frame.profile and the frame panel |

## ECS (Entity Component System)

| Example | What it shows | State |
|---|---|---|
| [`callbacks`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/callbacks.rs) | Store arbitrary systems in components and run them on demand | can be written |
| [`change_detection`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/change_detection.rs) | Change detection on components and resources | can be written |
| [`component_hooks`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/component_hooks.rs) | Define component hooks to manage component lifecycle events | can be written |
| [`contiguous_query`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/contiguous_query.rs) | Demonstrates contiguous queries | does not apply, about a Rust query's memory layout |
| [`custom_executor`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/custom_executor.rs) | Demonstrates how to make a custom SystemExecutor | does not apply, replaces Bevy's system executor, which is Rust |
| [`custom_query_param`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/custom_query_param.rs) | Groups commonly used compound queries and query filters into a single type | does not apply, derives a Rust query type |
| [`custom_schedule`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/custom_schedule.rs) | Demonstrates how to add custom schedules | missing, schedules a game adds of its own |
| [`delayed_commands`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/delayed_commands.rs) | Demonstrates how to schedule ECS commands with a delay | can be written |
| [`dynamic`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/dynamic.rs) | Dynamically create components, spawn entities with those components and query those components | does not apply, builds components from raw layouts in Rust |
| [`ecs_guide`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/ecs_guide.rs) | Full guide to Bevy's ECS | can be written |
| [`entity_disabling`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/entity_disabling.rs) | Demonstrates how to hide entities from the ECS without deleting them | can be written, through Bevy's reflected Disabled |
| [`error_handling`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/error_handling.rs) | How to return and handle errors across the ECS | can be written, with .NET exceptions |
| [`extraction`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/extraction.rs) | Demonstrates different ways of extracting components, copying them from the main world to the render world | does not apply, writes render world extraction in Rust |
| [`fallible_params`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/fallible_params.rs) | Systems are skipped if their parameters cannot be acquired | can be written |
| [`fixed_timestep`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/fixed_timestep.rs) | Shows how to create systems that run every fixed timestep, rather than every tick | can be written |
| [`generic_system`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/generic_system.rs) | Shows how to create systems that can be reused with different types | can be written |
| [`hierarchy`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/hierarchy.rs) | Creates a hierarchy of parents and children entities | can be written |
| [`hotpatching_systems`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/hotpatching_systems.rs) | Demonstrates how to hotpatch systems | can be written, as scripts reloaded while the app runs |
| [`immutable_components`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/immutable_components.rs) | Demonstrates the creation and utility of immutable components | does not apply, about Rust's component mutability |
| [`iter_combinations`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/iter_combinations.rs) | Shows how to iterate over combinations of query results | can be written |
| [`message`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/message.rs) | Illustrates message creation, activation, and reception | can be written |
| [`nondeterministic_system_order`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/nondeterministic_system_order.rs) | Systems run in parallel, but their order isn't always deterministic. Here's how to detect and fix this. | can be written |
| [`observer_propagation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/observer_propagation.rs) | Demonstrates event propagation with observers | missing, events that propagate through a hierarchy to observers |
| [`observers`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/observers.rs) | Demonstrates observers that react to events (both built-in life-cycle events and custom events) | can be written |
| [`one_shot_systems`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/one_shot_systems.rs) | Shows how to flexibly run systems without scheduling them | can be written |
| [`parallel_query`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/parallel_query.rs) | Illustrates parallel queries with `ParallelIterator` | can be written |
| [`relationships`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/relationships.rs) | Define and work with custom relationships between entities | can be written |
| [`removal_detection`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/removal_detection.rs) | Query for entities that had a specific component removed earlier in the current frame | can be written |
| [`run_conditions`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/run_conditions.rs) | Run systems only when one or multiple conditions are met | can be written |
| [`startup_system`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/startup_system.rs) | Demonstrates a startup system (one that runs once when the app starts up) | can be written |
| [`state_scoped`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/state_scoped.rs) | Shows how to spawn entities that are automatically despawned either when entering or exiting specific game states. | can be written |
| [`system_closure`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ecs/system_closure.rs) | Show how to use closures as systems, and how to configure `Local` variables by capturing external state | can be written |
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
| [`alien_cake_addict`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/showcase/alien_cake_addict.rs) | Eat the cakes. Eat them all. An example 3D game | can be written |
| [`breakout`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/showcase/breakout.rs) | An implementation of the classic game "Breakout". | can be written |
| [`contributors`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/showcase/contributors.rs) | Displays each contributor as a bouncy bevy-ball! | can be written |
| [`desk_toy`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/showcase/desk_toy.rs) | Bevy logo as a desk toy using transparent windows! Now with Googly Eyes! | can be written |
| [`game_menu`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/showcase/game_menu.rs) | A simple game menu | can be written |
| [`loading_screen`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/showcase/loading_screen.rs) | Demonstrates how to create a loading screen that waits for all assets to be loaded and render pipelines to be compiled. | can be written |

## Gizmos

| Example | What it shows | State |
|---|---|---|
| [`2d_gizmos`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/2d_gizmos.rs) | A scene showcasing 2D gizmos | can be written |
| [`2d_text_gizmos`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/2d_text_gizmos.rs) | A scene showcasing 2d text gizmos | missing, text gizmos |
| [`3d_gizmos`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/3d_gizmos.rs) | A scene showcasing 3D gizmos | can be written |
| [`3d_text_gizmos`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/3d_text_gizmos.rs) | A scene showcasing 3d text gizmos | missing, text gizmos |
| [`anchored_text_gizmos`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/anchored_text_gizmos.rs) | Demonstrates anchored text gizmos | missing, text gizmos |
| [`axes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/axes.rs) | Demonstrates the function of axes gizmos | can be written |
| [`light_gizmos`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/light_gizmos.rs) | A scene showcasing light gizmos | can be written, through Bevy's reflected ShowLightGizmo |
| [`text_gizmos_font`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/text_gizmos_font.rs) | Example displaying the font used by text gizmos | missing, text gizmos |
| [`transform_gizmo`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gizmos/transform_gizmo.rs) | Interactive transform gizmo for translating, rotating, and scaling entities | missing, Bevy's interactive transform gizmo |

## Helpers

| Example | What it shows | State |
|---|---|---|
| [`widgets`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/helpers/widgets.rs) | Example UI Widgets | missing, Bevy's widget helpers (bevy_ui_widgets) |

## Input

| Example | What it shows | State |
|---|---|---|
| [`char_input_events`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/char_input_events.rs) | Prints out all chars as they are inputted | can be written |
| [`gamepad_input`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/gamepad_input.rs) | Shows handling of gamepad input, connections, and disconnections | can be written |
| [`gamepad_input_events`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/gamepad_input_events.rs) | Iterates and prints gamepad input and connection events | can be written |
| [`gamepad_rumble`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/gamepad_rumble.rs) | Shows how to rumble a gamepad using force feedback | can be written |
| [`keyboard_input`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/keyboard_input.rs) | Demonstrates handling a key press/release | can be written |
| [`keyboard_input_events`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/keyboard_input_events.rs) | Prints out all keyboard events | can be written |
| [`keyboard_modifiers`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/keyboard_modifiers.rs) | Demonstrates using key modifiers (ctrl, shift) | can be written |
| [`mouse_grab`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/mouse_grab.rs) | Demonstrates how to grab the mouse, locking the cursor to the app's screen | can be written |
| [`mouse_input`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/mouse_input.rs) | Demonstrates handling a mouse button press/release | can be written |
| [`mouse_input_events`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/mouse_input_events.rs) | Prints out all mouse events (buttons, movement, etc.) | can be written |
| [`touch_input`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/touch_input.rs) | Displays touch presses, releases, and cancels | can be written |
| [`touch_input_events`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/input/touch_input_events.rs) | Prints out all touch inputs | can be written |

## Math

| Example | What it shows | State |
|---|---|---|
| [`bounding_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/math/bounding_2d.rs) | Showcases bounding volumes and intersection tests | can be written, with the managed math |
| [`cubic_splines`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/math/cubic_splines.rs) | Exhibits different modes of constructing cubic curves using splines | can be written, with the managed math |
| [`custom_primitives`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/math/custom_primitives.rs) | Demonstrates how to add custom primitives and useful traits for them. | does not apply, implements Rust traits for a primitive |
| [`random_sampling`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/math/random_sampling.rs) | Demonstrates how to sample random points from mathematical primitives | can be written, with the managed math |
| [`render_primitives`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/math/render_primitives.rs) | Shows off rendering for all math primitives as both Meshes and Gizmos | can be written |
| [`smooth_follow`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/movement/smooth_follow.rs) | Demonstrates how to make an entity smoothly follow another using interpolation | can be written |

## Movement

| Example | What it shows | State |
|---|---|---|
| [`physics_in_fixed_timestep`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/movement/physics_in_fixed_timestep.rs) | Handles input, physics, and rendering in an industry-standard way by using a fixed timestep | can be written |

## Picking

| Example | What it shows | State |
|---|---|---|
| [`custom_hit_data`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/picking/custom_hit_data.rs) | Demonstrates a custom picking backend with custom hit data. | does not apply, writes a Rust picking backend |
| [`debug_picking`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/picking/debug_picking.rs) | Demonstrates picking debug overlay | missing, Bevy's picking debug overlay |
| [`dragdrop_picking`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/picking/dragdrop_picking.rs) | Demonstrates drag and drop using picking events | missing, drag and drop picking events |
| [`mesh_picking`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/picking/mesh_picking.rs) | Demonstrates picking meshes | can be written, in the editor profile, which carries picking |
| [`simple_picking`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/picking/simple_picking.rs) | Demonstrates how to use picking events to spawn simple objects | can be written, in the editor profile, which carries picking |
| [`sprite_picking`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/picking/sprite_picking.rs) | Demonstrates picking sprites and sprite atlases | missing, picking sprites |

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
| [`bsn`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/scene/bsn.rs) | Demonstrates how to use BSN to compose scenes | missing, BSN scene notation |
| [`world_serialization`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/scene/world_serialization.rs) | Demonstrates loading from and saving world to files | can be written |

## Shaders

| Example | What it shows | State |
|---|---|---|
| [`animate_shader`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/animate_shader.rs) | A shader that uses dynamic data like the time since startup | can be written |
| [`array_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/array_texture.rs) | A shader that shows how to reuse the core bevy PBR shading functionality in a custom material that obtains the base color from an array texture. | can be written |
| [`automatic_instancing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/automatic_instancing.rs) | Shows that multiple instances of a cube are automatically instanced in one draw call | can be written |
| [`compute_mesh`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/compute_mesh.rs) | A compute shader that generates a mesh that is controlled by a Handle | can be written |
| [`compute_shader_game_of_life`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/compute_shader_game_of_life.rs) | A compute shader that simulates Conway's Game of Life | can be written |
| [`custom_phase_item`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/custom_phase_item.rs) | Demonstrates how to enqueue custom draw commands in a render phase | does not apply, enqueues Rust draw commands in a render phase |
| [`custom_post_processing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/custom_post_processing.rs) | A custom post processing effect, using a custom render pass that runs after the main pass | can be written |
| [`custom_render_phase`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/custom_render_phase.rs) | Shows how to make a complete render phase | does not apply, builds a render phase in Rust |
| [`custom_shader_instancing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/custom_shader_instancing.rs) | A shader that renders a mesh multiple times in one draw call using low level rendering api | can be written |
| [`custom_vertex_attribute`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/custom_vertex_attribute.rs) | A shader that reads a mesh's custom vertex attribute | missing, vertex attributes a game defines |
| [`extended_material`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/extended_material.rs) | A custom shader that builds on the standard material | can be written |
| [`extended_material_bindless`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/extended_material_bindless.rs) | Demonstrates bindless `ExtendedMaterial` | can be written |
| [`gpu_readback`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/gpu_readback.rs) | A very simple compute shader that writes to a buffer that is read by the cpu | can be written |
| [`render_depth_to_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/render_depth_to_texture.rs) | Demonstrates how to use depth-only cameras | missing, depth-only cameras rendered to a texture |
| [`shader_defs`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_defs.rs) | A shader that uses "shaders defs" (a bevy tool to selectively toggle parts of a shader) | can be written |
| [`shader_material`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_material.rs) | A shader and a material that uses it | can be written |
| [`shader_material_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_material_2d.rs) | A shader and a material that uses it on a 2d mesh | missing, 2D meshes (Mesh2d) with a shader material |
| [`shader_material_bindless`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_material_bindless.rs) | Demonstrates how to make materials that use bindless textures | can be written |
| [`shader_material_glsl`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_material_glsl.rs) | A shader that uses the GLSL shading language | does not apply, GLSL, where shaders here are Slang |
| [`shader_material_screenspace_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_material_screenspace_texture.rs) | A shader that samples a texture with view-independent UV coordinates | can be written |
| [`shader_material_wesl`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_material_wesl.rs) | A shader that uses WESL | does not apply, WESL, where shaders here are Slang |
| [`shader_prepass`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/shader_prepass.rs) | A shader that uses the various textures generated by the prepass | can be written |
| [`specialized_mesh_pipeline`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/specialized_mesh_pipeline.rs) | Demonstrates how to write a specialized mesh pipeline | does not apply, writes a mesh pipeline in Rust |
| [`storage_buffer`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader/storage_buffer.rs) | A shader that shows how to bind a storage buffer using a custom material. | can be written |
| [`texture_binding_array`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/texture_binding_array.rs) | A shader that shows how to bind and sample multiple textures as a binding array (a.k.a. bindless textures). | can be written |

## Shaders Advanced

| Example | What it shows | State |
|---|---|---|
| [`fullscreen_material`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/fullscreen_material.rs) | Demonstrates how to write a fullscreen material | can be written |
| [`manual_material`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/shader_advanced/manual_material.rs) | Demonstrates how to implement a material manually using the mid-level render APIs | does not apply, writes a material with Rust's mid-level render API |

## State

| Example | What it shows | State |
|---|---|---|
| [`computed_states`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/state/computed_states.rs) | Advanced state patterns using Computed States. | can be written |
| [`custom_transitions`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/state/custom_transitions.rs) | Creating and working with custom state transition schedules. | can be written |
| [`states`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/state/states.rs) | Illustrates how to use States to control transitioning from a Menu state to an InGame state. | can be written |
| [`sub_states`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/state/sub_states.rs) | Using Sub States for hierarchical state handling. | can be written |

## Stress Tests

| Example | What it shows | State |
|---|---|---|
| [`bevymark`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/bevymark.rs) | A heavy sprite rendering workload to benchmark your system with Bevy | can be written |
| [`bevymark_3d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/bevymark_3d.rs) | A heavy 3D cube rendering workload to benchmark your system with Bevy | can be written |
| [`many_animated_sprite_meshes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_animated_sprite_meshes.rs) | Displays many animated sprite meshes in a grid arrangement with slight offsets to their animation timers. Used for performance testing. | missing, 2D meshes (Mesh2d) for sprites |
| [`many_animated_sprites`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_animated_sprites.rs) | Displays many animated sprites in a grid arrangement with slight offsets to their animation timers. Used for performance testing. | can be written |
| [`many_buttons`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_buttons.rs) | Test rendering of many UI elements | can be written |
| [`many_cameras_lights`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_cameras_lights.rs) | Test rendering of many cameras and lights | can be written |
| [`many_components`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_components.rs) | Test large ECS systems | does not apply, registers components dynamically in Rust |
| [`many_cubes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_cubes.rs) | Simple benchmark to test per-entity draw overhead. Run with the `sphere` argument to test frustum culling | can be written |
| [`many_foxes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_foxes.rs) | Loads an animated fox model and spawns lots of them. Good for testing skinned mesh performance. Takes an unsigned integer argument for the number of foxes to spawn. Defaults to 1000 | can be written |
| [`many_gizmos`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_gizmos.rs) | Test rendering of many gizmos | can be written |
| [`many_glyphs`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_glyphs.rs) | Simple benchmark to test text rendering. | can be written |
| [`many_gradients`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_gradients.rs) | Stress test for gradient rendering performance | can be written, through Bevy's reflected BackgroundGradient |
| [`many_lights`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_lights.rs) | Simple benchmark to test rendering many point lights. Run with `WGPU_SETTINGS_PRIO=webgl2` to restrict to uniform buffers and max 256 lights | can be written |
| [`many_materials`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_materials.rs) | Benchmark to test rendering many animated materials | can be written |
| [`many_morph_targets`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_morph_targets.rs) | Simple benchmark to test rendering many meshes with animated morph targets. | can be written, through Bevy's reflected MorphWeights |
| [`many_sprite_meshes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_sprite_meshes.rs) | Displays many sprite meshes in a grid arrangement! Used for performance testing. Use `--colored` to enable color tinted sprites. | missing, 2D meshes (Mesh2d) for sprites |
| [`many_sprites`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_sprites.rs) | Displays many sprites in a grid arrangement! Used for performance testing. Use `--colored` to enable color tinted sprites. | can be written |
| [`many_text`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_text.rs) | Displays many UI Text nodes. Used for performance testing. | can be written |
| [`many_text2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/many_text2d.rs) | Displays many Text2d! Used for performance testing. | can be written, through Bevy's reflected Text2d |
| [`text_pipeline`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/text_pipeline.rs) | Text Pipeline benchmark | can be written |
| [`transform_hierarchy`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/stress_tests/transform_hierarchy.rs) | Various test cases for hierarchy and transform propagation performance | can be written |

## Time

| Example | What it shows | State |
|---|---|---|
| [`time`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/time/time.rs) | Explains how Time is handled in ECS | can be written |
| [`timers`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/time/timers.rs) | Illustrates ticking `Timer` resources inside systems and handling their state | can be written |
| [`virtual_time`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/time/virtual_time.rs) | Shows how `Time<Virtual>` can be used to pause, resume, slow down and speed up a game. | can be written |

## Tools

| Example | What it shows | State |
|---|---|---|
| `gamepad_viewer` | Shows a visualization of gamepad buttons, sticks, and triggers | can be written |
| `scene_viewer` | A simple way to view glTF models with Bevy. Just run `cargo run --release --example scene_viewer /path/to/model.gltf#Scene0`, replacing the path as appropriate. With no arguments it will load the FieldHelmet glTF model from the repository assets subdirectory | can be written |

## Transforms

| Example | What it shows | State |
|---|---|---|
| [`3d_rotation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/transforms/3d_rotation.rs) | Illustrates how to (constantly) rotate an object around an axis | can be written |
| [`align`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/transforms/align.rs) | A demonstration of Transform's axis-alignment feature | can be written |
| [`scale`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/transforms/scale.rs) | Illustrates how to scale an object in each direction | can be written |
| [`transform`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/transforms/transform.rs) | Shows multiple transformations of objects | can be written |
| [`translation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/transforms/translation.rs) | Illustrates how to move an object along an axis | can be written |

## UI (User Interface)

| Example | What it shows | State |
|---|---|---|
| [`anchor_layout`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/layout/anchor_layout.rs) | Shows an 'anchor layout' style of ui layout | can be written |
| [`borders`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/styling/borders.rs) | Demonstrates how to create a node with a border | can be written |
| [`box_shadow`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/styling/box_shadow.rs) | Demonstrates how to create a node with a shadow | can be written, through Bevy's reflected BoxShadow |
| [`button`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/button.rs) | Illustrates creating and updating a button | can be written |
| [`directional_navigation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/navigation/directional_navigation.rs) | Demonstration of automatic directional navigation based on UI element positions | missing, directional navigation between interface nodes |
| [`directional_navigation_overrides`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/navigation/directional_navigation_overrides.rs) | Demonstration of automatic directional navigation between UI elements with manual overrides | missing, directional navigation between interface nodes |
| [`display_and_visibility`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/layout/display_and_visibility.rs) | Demonstrates how Display and Visibility work in the UI. | can be written |
| [`drag_to_scroll`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/scroll_and_overflow/drag_to_scroll.rs) | This example tests scale factor, dragging and scrolling | can be written |
| [`editable_text_filter`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/editable_text_filter.rs) | Demonstrates an 8-character hex input using EditableTextFilter | missing, Bevy's editable text (EditableText) |
| [`feathers_counter`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/feathers_counter.rs) | Simple counter using feathers | missing, Bevy's Feathers widgets |
| [`feathers_gallery`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/feathers_gallery.rs) | Gallery of Feathers Widgets | missing, Bevy's Feathers widgets |
| [`flex_layout`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/layout/flex_layout.rs) | Demonstrates how the AlignItems and JustifyContent properties can be composed to layout nodes and position text | can be written |
| [`font_atlas_debug`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/font_atlas_debug.rs) | Illustrates how FontAtlases are populated (used to optimize text rendering internally) | missing, reading the font atlases |
| [`font_query`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/font_query.rs) | Demonstrates font querying | missing, querying fonts |
| [`font_variations`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/font_variations.rs) | Demonstrates how to use OpenType font variations. | missing, OpenType font variations |
| [`font_weights`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/font_weights.rs) | Demonstrates how to use font weights. | missing, font weights |
| [`generic_font_families`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/generic_font_families.rs) | Demonstrates how to use generic font families | missing, generic font families |
| [`ghost_nodes`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/layout/ghost_nodes.rs) | Demonstrates the use of Ghost Nodes to skip entities in the UI layout hierarchy | missing, ghost nodes (GhostNode) |
| [`gradients`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/styling/gradients.rs) | An example demonstrating gradients | can be written, through Bevy's reflected BackgroundGradient and BorderGradient |
| [`grid`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/layout/grid.rs) | An example for CSS Grid layout | can be written |
| [`image_node`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/images/image_node.rs) | Demonstrates how to create an image node | can be written |
| [`image_node_resizing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/images/image_node_resizing.rs) | Demonstrates how to resize an image node | can be written |
| [`ime_support`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/ime_support.rs) | Demonstrates IME (Input Method Editor) support for text input | can be written |
| [`letter_spacing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/letter_spacing.rs) | Demonstrates the letter spacing feature | can be written |
| [`multiline_text_input`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/multiline_text_input.rs) | Demonstrates a single multiline EditableText widget | missing, Bevy's editable text (EditableText) |
| [`multiple_text_inputs`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/multiple_text_inputs.rs) | Demonstrates multiple text inputs | missing, Bevy's editable text (EditableText) |
| [`overflow`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/scroll_and_overflow/overflow.rs) | Simple example demonstrating overflow behavior | can be written |
| [`overflow_clip_margin`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/scroll_and_overflow/overflow_clip_margin.rs) | Simple example demonstrating the OverflowClipMargin style property | can be written |
| [`overflow_debug`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/scroll_and_overflow/overflow_debug.rs) | An example to debug overflow and clipping behavior | can be written |
| [`relative_cursor_position`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/relative_cursor_position.rs) | Showcases the RelativeCursorPosition component | can be written, through Bevy's reflected RelativeCursorPosition |
| [`render_ui_to_texture`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/render_ui_to_texture.rs) | An example of rendering UI as a part of a 3D world | can be written |
| [`scroll`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/scroll_and_overflow/scroll.rs) | Demonstrates scrolling UI containers | can be written |
| [`scrollbars`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/scroll_and_overflow/scrollbars.rs) | Demonstrates use of core scrollbar in Bevy UI | missing, Bevy's core scrollbars |
| [`size_constraints`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/layout/size_constraints.rs) | Demonstrates how the to use the size constraints to control the size of a UI node. | can be written |
| [`stacked_gradients`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/styling/stacked_gradients.rs) | An example demonstrating stacked gradients | can be written, through Bevy's reflected BackgroundGradient |
| [`standard_widgets`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/standard_widgets.rs) | Demonstrates use of core (headless) widgets in Bevy UI | missing, Bevy's core widgets (bevy_ui_widgets) |
| [`standard_widgets_observers`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/standard_widgets_observers.rs) | Demonstrates use of core (headless) widgets in Bevy UI, with Observers | missing, Bevy's core widgets (bevy_ui_widgets) |
| [`strikethrough_and_underline`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/strikethrough_and_underline.rs) | Demonstrates how to display text with strikethrough and underline. | missing, strikethrough and underline on text |
| [`system_fonts`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/system_fonts.rs) | Demonstrates how to use system fonts | missing, system fonts |
| [`tab_navigation`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/tab_navigation.rs) | Demonstration of Tab Navigation between UI elements | missing, tab navigation between interface nodes |
| [`text`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/text.rs) | Illustrates creating and updating text | can be written |
| [`text_background_colors`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/text_background_colors.rs) | Demonstrates text background colors | missing, text background colors |
| [`text_debug`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/text_debug.rs) | An example for debugging text layout | can be written |
| [`text_input`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/text_input.rs) | Demonstrates a simple, unstyled text input widget | missing, Bevy's editable text (EditableText) |
| [`text_wrap_debug`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/text/text_wrap_debug.rs) | Demonstrates text wrapping | can be written |
| [`transparency_ui`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/styling/transparency_ui.rs) | Demonstrates transparency for UI | can be written |
| [`ui_drag_and_drop`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/ui_drag_and_drop.rs) | Demonstrates dragging and dropping UI nodes | can be written |
| [`ui_material`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/ui_material.rs) | Demonstrates creating and using custom Ui materials | missing, interface materials (UiMaterial) |
| [`ui_scaling`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/ui_scaling.rs) | Illustrates how to scale the UI | can be written, through Bevy's reflected UiScale |
| [`ui_target_camera`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/ui_target_camera.rs) | Demonstrates how to use `UiTargetCamera` and camera ordering. | can be written |
| [`ui_texture_atlas`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/images/ui_texture_atlas.rs) | Illustrates how to use TextureAtlases in UI | can be written |
| [`ui_texture_atlas_slice`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/images/ui_texture_atlas_slice.rs) | Illustrates how to use 9 Slicing for TextureAtlases in UI | can be written |
| [`ui_texture_slice`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/images/ui_texture_slice.rs) | Illustrates how to use 9 Slicing in UI | can be written |
| [`ui_texture_slice_flip_and_tile`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/images/ui_texture_slice_flip_and_tile.rs) | Illustrates how to flip and tile images with 9 Slicing in UI | can be written |
| [`ui_transform`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/ui_transform.rs) | An example demonstrating how to translate, rotate and scale UI elements. | can be written, through Bevy's reflected UiTransform |
| [`vertical_slider`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/vertical_slider.rs) | Simple example showing vertical and horizontal slider widgets with snap behavior and value labels | missing, Bevy's core widgets (bevy_ui_widgets) |
| [`viewport_node`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/viewport_node.rs) | Demonstrates how to create a viewport node with picking support | can be written, through Bevy's reflected ViewportNode |
| [`virtual_keyboard`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/widgets/virtual_keyboard.rs) | Example demonstrating a virtual keyboard widget | missing, Bevy's Feathers widgets |
| [`window_fallthrough`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/window_fallthrough.rs) | Illustrates how to access `winit::window::Window`'s `hittest` functionality. | can be written |
| [`z_index`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/ui/layout/z_index.rs) | Demonstrates how to control the relative depth (z-position) of UI elements | can be written, through Bevy's reflected ZIndex and GlobalZIndex |

## Usage

| Example | What it shows | State |
|---|---|---|
| [`context_menu`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/usage/context_menu.rs) | Example of a context menu | can be written |
| [`cooldown`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/usage/cooldown.rs) | Example for cooldown on button clicks | can be written |
| [`debug_frustum_culling`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/usage/debug_frustum_culling.rs) | Example demonstrating how to debug frustum culling | can be written |

## Window

| Example | What it shows | State |
|---|---|---|
| [`clear_color`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/clear_color.rs) | Creates a solid color window | can be written |
| [`custom_cursor_image`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/custom_cursor_image.rs) | Demonstrates creating an animated custom cursor from an image | can be written, through Bevy's reflected CursorIcon, its image set as a reflected asset |
| [`low_power`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/low_power.rs) | Demonstrates settings to reduce power use for bevy applications | can be written |
| [`monitor_info`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/monitor_info.rs) | Displays information about available monitors (displays). | can be written |
| [`multiple_windows`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/multiple_windows.rs) | Demonstrates creating multiple windows, and rendering to them | missing, a second window |
| [`scale_factor_override`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/scale_factor_override.rs) | Illustrates how to customize the default window settings | can be written, through the resolution of Bevy's reflected Window |
| [`screenshot`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/screenshot.rs) | Shows how to save screenshots to disk | can be written |
| [`transparent_window`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/transparent_window.rs) | Illustrates making the window transparent and hiding the window decoration | can be written |
| [`window_drag_move`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/window_drag_move.rs) | Demonstrates drag move and drag resize without window decoration | can be written |
| [`window_resizing`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/window_resizing.rs) | Demonstrates resizing and responding to resizing a window | can be written |
| [`window_settings`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/window/window_settings.rs) | Demonstrates customizing default window settings | can be written |

## glTF

| Example | What it shows | State |
|---|---|---|
| [`custom_gltf_vertex_attribute`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/custom_gltf_vertex_attribute.rs) | Renders a glTF mesh in 2D with a custom vertex attribute | does not apply, maps a glTF attribute to a Rust-defined vertex attribute and 2D material |
| [`edit_material_on_gltf`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/edit_material_on_gltf.rs) | Showcases changing materials of a glTF after Scene spawn | can be written |
| [`gltf_extension_animation_graph`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/gltf_extension_animation_graph.rs) | Uses glTF data to build an AnimationGraph via extension processing | does not apply, writes a Rust glTF extension handler |
| [`gltf_extension_mesh_2d`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/gltf_extension_mesh_2d.rs) | Uses glTF extension data to convert incoming Mesh3d/MeshMaterial3d assets to 2d | does not apply, writes a Rust glTF extension handler |
| [`gltf_skinned_mesh`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/gltf_skinned_mesh.rs) | Skinned mesh example with mesh and joints data loaded from a glTF file | can be written |
| [`load_gltf`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/load_gltf.rs) | Loads and renders a glTF file as a scene | can be written |
| [`load_gltf_extras`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/load_gltf_extras.rs) | Loads and renders a glTF file as a scene, including the gltf extras | missing, a glTF node's extras |
| [`query_gltf_primitives`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/query_gltf_primitives.rs) | Query primitives in a glTF scene | can be written |
| [`update_gltf_scene`](https://github.com/bevyengine/bevy/blob/v0.19.1/examples/gltf/update_gltf_scene.rs) | Update a scene from a glTF file, either by spawning the scene as a child of another entity, or by accessing the entities of the scene | can be written |

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
