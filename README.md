# Bevy C# 

Write [Bevy](https://bevy.org) games in C#.

![Showcase](https://raw.githubusercontent.com/EggyStudio/BevyCSharp/refs/heads/main/.github/assets/showcase_3.gif)

<sup>`BevyCSharp.Sample`, `dotnet run --project BevyCSharp.Sample`</sup>

Mark a struct `[Behavior]`, give it methods with stage attributes, and a Roslyn source generator
wires it into Bevy's schedule, as a component and a system at the same time. Bevy is the engine
underneath, with its ECS, its scheduler, its timing, its input and its renderer.

```csharp
using Bevy;

[Behavior]
public partial struct Spin
{
    [Range(0f, 5f)] public float Speed;
    private float _angle;

    // static: one plain system
    [OnStartup]
    public static void Scene(BehaviorContext ctx)
    {
        ctx.Ecs.Add(Render.SpawnCamera3d(), Transform.LookingAt(new Vec3(3f, 3f, 5f), Vec3.Zero, Vec3.UnitY));
        Render.SpawnLight(LightKind.Directional, 10_000f);

        var cube = ctx.Ecs.Spawn();
        ctx.Ecs.Add(cube, new Spin { Speed = 1.2f });
        Render.SetMesh(ctx.Ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f));
        Render.SetMaterial(ctx.Ecs, cube, Render.CreateMaterial(0.25f, 0.55f, 0.85f));
    }

    // instance: once per entity that has one, handed the entity's transform
    [OnUpdate]
    public void Tick(BehaviorContext ctx, ref Transform transform)
    {
        _angle += Speed * ctx.Time.Delta;
        transform.Rotation = Quat.FromAxisAngle(Vec3.UnitY, _angle);
    }
}
```

In Program.cs
```csharp
using Bevy;

BevyApp.Run();
```

Behaviors are discovered automatically, so a consuming project needs no registration code.

## Contents

- [Examples](#examples)
- [Install](#install)
- [Guide](#guide)
- [Driving a running app](#driving-a-running-app)
- [Status and limitations](#status-and-limitations)
- [Building from source](#building-from-source)
- [Contributing](#contributing)
- [License](#license)

## Examples

Bevy's own examples are written in C# under Bevy's names in `BevyCSharp.Examples`, each opened by
name and captured beside Bevy's picture of it, and [EXAMPLES.md](https://github.com/EggyStudio/BevyCSharp/blob/main/.github/EXAMPLES.md) has a
row for every one of Bevy's, saying whether it is written, can be, or waits on something the bridge
lacks.

<!-- example-gallery -->
<table>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/move_sprite.png" width="200"/><br><code>move_sprite</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/rotate_to_cursor.png" width="200"/><br><code>rotate_to_cursor</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/rotation.png" width="200"/><br><code>rotation</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/cpu_draw.png" width="200"/><br><code>cpu_draw</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sprite.png" width="200"/><br><code>sprite</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sprite_animation.png" width="200"/><br><code>sprite_animation</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sprite_scale.png" width="200"/><br><code>sprite_scale</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sprite_flipping.png" width="200"/><br><code>sprite_flipping</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sprite_sheet.png" width="200"/><br><code>sprite_sheet</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sprite_tile.png" width="200"/><br><code>sprite_tile</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sprite_slice.png" width="200"/><br><code>sprite_slice</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/text2d.png" width="200"/><br><code>text2d</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/transparency_2d.png" width="200"/><br><code>transparency_2d</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/3d_scene.png" width="200"/><br><code>3d_scene</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/rect_light.png" width="200"/><br><code>rect_light</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/3d_shapes.png" width="200"/><br><code>3d_shapes</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/3d_viewport_to_world.png" width="200"/><br><code>3d_viewport_to_world</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/animated_material.png" width="200"/><br><code>animated_material</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/generate_custom_mesh.png" width="200"/><br><code>generate_custom_mesh</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/atmospheric_fog.png" width="200"/><br><code>atmospheric_fog</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/fog.png" width="200"/><br><code>fog</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/auto_exposure.png" width="200"/><br><code>auto_exposure</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/lighting.png" width="200"/><br><code>lighting</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/lines.png" width="200"/><br><code>lines</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/ssao.png" width="200"/><br><code>ssao</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/spotlight.png" width="200"/><br><code>spotlight</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/bloom_3d.png" width="200"/><br><code>bloom_3d</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/motion_blur.png" width="200"/><br><code>motion_blur</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/order_independent_transparency.png" width="200"/><br><code>order_independent_transparency</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/orthographic.png" width="200"/><br><code>orthographic</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/parenting.png" width="200"/><br><code>parenting</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/pbr.png" width="200"/><br><code>pbr</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/render_to_texture.png" width="200"/><br><code>render_to_texture</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/shadow_caster_receiver.png" width="200"/><br><code>shadow_caster_receiver</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/skybox.png" width="200"/><br><code>skybox</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/spherical_area_lights.png" width="200"/><br><code>spherical_area_lights</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/split_screen.png" width="200"/><br><code>split_screen</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/texture.png" width="200"/><br><code>texture</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/transparency_3d.png" width="200"/><br><code>transparency_3d</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/two_passes.png" width="200"/><br><code>two_passes</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/vertex_colors.png" width="200"/><br><code>vertex_colors</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/wireframe.png" width="200"/><br><code>wireframe</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/lightmaps.png" width="200"/><br><code>lightmaps</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/drag_and_drop.png" width="200"/><br><code>drag_and_drop</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/empty_defaults.png" width="200"/><br><code>empty_defaults</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/async_compute.png" width="200"/><br><code>async_compute</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/async_channel_pattern.png" width="200"/><br><code>async_channel_pattern</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/external_source_external_thread.png" width="200"/><br><code>external_source_external_thread</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/audio.png" width="200"/><br><code>audio</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/soundtrack.png" width="200"/><br><code>soundtrack</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/spatial_audio_3d.png" width="200"/><br><code>spatial_audio_3d</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/play_sound_effect.png" width="200"/><br><code>play_sound_effect</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/hierarchy.png" width="200"/><br><code>hierarchy</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/iter_combinations.png" width="200"/><br><code>iter_combinations</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/one_shot_systems.png" width="200"/><br><code>one_shot_systems</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/parallel_query.png" width="200"/><br><code>parallel_query</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/states.png" width="200"/><br><code>states</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sub_states.png" width="200"/><br><code>sub_states</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/virtual_time.png" width="200"/><br><code>virtual_time</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/mouse_grab.png" width="200"/><br><code>mouse_grab</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/3d_rotation.png" width="200"/><br><code>3d_rotation</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/align.png" width="200"/><br><code>align</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/scale.png" width="200"/><br><code>scale</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/transform.png" width="200"/><br><code>transform</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/translation.png" width="200"/><br><code>translation</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/anchor_layout.png" width="200"/><br><code>anchor_layout</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/borders.png" width="200"/><br><code>borders</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/button.png" width="200"/><br><code>button</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/display_and_visibility.png" width="200"/><br><code>display_and_visibility</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/overflow.png" width="200"/><br><code>overflow</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/overflow_clip_margin.png" width="200"/><br><code>overflow_clip_margin</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/overflow_debug.png" width="200"/><br><code>overflow_debug</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/relative_cursor_position.png" width="200"/><br><code>relative_cursor_position</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/size_constraints.png" width="200"/><br><code>size_constraints</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/text_debug.png" width="200"/><br><code>text_debug</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/flex_layout.png" width="200"/><br><code>flex_layout</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/text_wrap_debug.png" width="200"/><br><code>text_wrap_debug</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/letter_spacing.png" width="200"/><br><code>letter_spacing</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/gradients.png" width="200"/><br><code>gradients</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/stacked_gradients.png" width="200"/><br><code>stacked_gradients</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/transparency_ui.png" width="200"/><br><code>transparency_ui</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/z_index.png" width="200"/><br><code>z_index</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/ui_texture_atlas.png" width="200"/><br><code>ui_texture_atlas</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/ui_texture_slice.png" width="200"/><br><code>ui_texture_slice</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/ui_texture_atlas_slice.png" width="200"/><br><code>ui_texture_atlas_slice</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/ui_transform.png" width="200"/><br><code>ui_transform</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/ui_target_camera.png" width="200"/><br><code>ui_target_camera</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/image_node.png" width="200"/><br><code>image_node</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/smooth_follow.png" width="200"/><br><code>smooth_follow</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/projection_zoom.png" width="200"/><br><code>projection_zoom</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/camera_orbit.png" width="200"/><br><code>camera_orbit</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/2d_on_ui.png" width="200"/><br><code>2d_on_ui</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/clearcoat.png" width="200"/><br><code>clearcoat</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/depth_of_field.png" width="200"/><br><code>depth_of_field</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/volumetric_fog.png" width="200"/><br><code>volumetric_fog</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/fog_volumes.png" width="200"/><br><code>fog_volumes</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/scrolling_fog.png" width="200"/><br><code>scrolling_fog</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/post_processing.png" width="200"/><br><code>post_processing</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/rotate_environment_map.png" width="200"/><br><code>rotate_environment_map</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/pcss.png" width="200"/><br><code>pcss</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/mixed_lighting.png" width="200"/><br><code>mixed_lighting</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/settings.png" width="200"/><br><code>settings</code></td></tr>
</table>
<!-- /example-gallery -->

```bash
dotnet run --project BevyCSharp.Examples -- 3d_scene
./bcs open --example lighting --offscreen     # serving, for ./bcs to drive and capture
```

## Install

```
dotnet add package BevyCSharp
```

The package carries three things: the managed library, the source generator (in the analyzer
slot), and a prebuilt native bridge per runtime identifier under `runtimes/`.

The program above, in a new console project with the package added, is a game, and `dotnet run`
opens its window:

```bash
dotnet new console -o MyGame && cd MyGame
dotnet add package BevyCSharp
# Spin.cs and Program.cs as above
dotnet run
```

On Linux the bridge's audio links ALSA, so the bridge needs ALSA's library to load, `libasound2`
(`libasound2t64` on Ubuntu 24.04 and Debian 13) or `alsa-lib` on Fedora and Arch, which a desktop
has and a container usually does not, and drawing needs a Vulkan driver. Where there is no display,
as in a container or on a build server, `BCS_OFFSCREEN=1` draws into an image instead of a window
and `BCS_FRAMES` ends the run after that many frames. Mesa's software Vulkan (`mesa-vulkan-drivers`)
draws where there is no graphics card:

```bash
BCS_OFFSCREEN=1 BCS_FRAMES=120 dotnet run
```

`build/readme-walk.sh` follows these steps in a container holding the .NET SDK and a packed package
alone, and the package workflow runs it.

## Guide

The guide in [`docs/`](https://github.com/EggyStudio/BevyCSharp/blob/main/docs) is for somebody using the engine, a page an area. Everything in
it is Bevy's own, bridged rather than reimplemented, so a `Transform` written from C# is the
transform the renderer reads and a sound played from C# is an entity in the same world.

| | |
|---|---|
| [CHEATSHEET.md](https://github.com/EggyStudio/BevyCSharp/blob/main/CHEATSHEET.md) | Every public method of the library on one line, grouped as the pages are |
| [Behaviors](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/behaviors.md) | Systems and components, stages, the fixed timestep, filters, conditions and threads |
| [States](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/states.md) | A game's modes, systems scoped to them, and code run as they change |
| [Messages and the hierarchy](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/messages-and-hierarchy.md) | One system telling another, and entities parented |
| [Components](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/components.md) | Bevy's components from C#, the rest through reflection, lists and maps, visibility |
| [Scenes and saves](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/scenes-and-saves.md) | Data assets, scene files, instances, saved games and types that change |
| [Assets and models](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/assets-and-models.md) | Loading from a folder, an assembly or a pack, and glTF models |
| [Drawing](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/drawing.md) | Meshes and materials made in code, and meshlets |
| [Materials and textures](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/materials.md) | Bevy's physically based material and its images |
| [Shaders](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/shaders.md) | Materials and passes written in Slang, compiled and reloaded as the game runs |
| [Compute](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/compute.md) | Compute shaders on their own and on a camera |
| [Cameras and light](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/cameras-and-light.md) | Cameras, shadows, the picture a camera makes and its lens |
| [Reflections and the sky](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/sky-and-reflections.md) | Screen-space and probe reflections, the sky and light probes |
| [Ray tracing](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/ray-tracing.md) | Bevy's ray-traced lighting and rays traced by a game |
| [Images and the window](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/window.md) | Drawing into an image, and the window |
| [2D](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/2d.md) | Sprites and flat cameras |
| [Gizmos](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/gizmos.md) | Lines and shapes drawn for a frame |
| [The interface](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/ui.md) | Bevy's nodes and text, and Dear ImGui for tools |
| [Audio](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/audio.md) | Sounds and music, placed and mixed |
| [Physics](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/physics.md) | Bodies, colliders, joints, contacts and characters over BepuPhysics |
| [Input](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/input.md) | Typed text, the input method, touches and gamepads |
| [Running a game](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/running-a-game.md) | In a window, headless or offscreen, and behaviors reloaded while it runs |
| [Making a game](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/making-a-game.md) | Courtyard, a small game from an empty project to an export |
| [The tools](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/tools.md) | The editor, its console, and driving a running app |
| [How it works](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/how-it-works.md) | Inside, between C# and Bevy |

## Driving a running app

`./bcs` drives a running app from a terminal, the editor, a game or an example, answering in JSON,
which is how a change is checked without a person at the window:

```bash
./bcs open --editor --offscreen        # start one, detached, with no display needed
./bcs command entity.set Cube Transform.Translation 0,2.5,0
./bcs command input.hold W,D 12        # keys held for twelve frames, inside the app
./bcs shot after.png                   # the next frame, as a PNG
./bcs stop
```

A game adds commands of its own with `[Command]` on a static method, and
[the tools](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/tools.md) page has the rest.

## Status and limitations

Early. The behavior system, the ECS bridge and the schedule work and are covered by tests that
run against a real Bevy app. Bevy's own examples are the measure of how much of it is reached:

<!-- examples -->
Of Bevy's 421 examples, 112 are written in C# here and 7 more in part, 123 more can be with what is bridged, 122 wait on something the bridge lacks and 57 are about Rust itself ([EXAMPLES.md](https://github.com/EggyStudio/BevyCSharp/blob/main/.github/EXAMPLES.md)).
<!-- /examples -->

Known gaps:

- A locally built package contains only the platform you built it on. Use the CI workflow, or
  run `build-native.sh` on each target platform, to produce a package covering all of them.
- A render build draws. Mesh primitives, textured physically based materials, cameras, lights,
  sprites, gizmos, UI nodes and text are reachable from a behavior script, verified on Vulkan. glTF
  files and `.scn` scenes load and spawn, audio plays, and a camera tonemaps, blooms, multisamples,
  antialiases, scatters a sky over what it draws, pulls focus and finds its own exposure. What is
  thin is the layer above that. A model's clips play one at a time, sprites step through no frames
  of their own, and a compressed texture a desktop GPU cannot decode is not transcoded.
  [.github/TODO.md](https://github.com/EggyStudio/BevyCSharp/blob/main/.github/TODO.md) lists what each gap needs.
- The library compiles no C# at runtime, so a game carries no compiler. Behavior scripts loaded
  while an app runs go through `BevyCSharp.Scripting`, which the editor and the player reference
  and a game references only if it loads scripts itself, over `App.EnableDynamicSystems` and
  `App.RemoveSystemsBySource`.
- The editor's document is a scene file, with the hierarchy, entity references and Bevy's own
  components kept, and primitives and materials made in memory written as how to make them again.
  A mesh built vertex by vertex is written as its geometry, and a material can be kept in a
  `*.material.json` of its own. [.github/SCENES.md](https://github.com/EggyStudio/BevyCSharp/blob/main/.github/SCENES.md) has what is left.
- Five of Bevy's components are mirrored by hand, and the rest are reached through Bevy's
  reflection, by type path and JSON or through generated typed wrappers, at the cost of a
  serialization a call. A component holds a list, inline or in a managed store, and a dictionary
  in the same store, and shared values live in data assets of their own.
  [.github/COMPONENTS.md](https://github.com/EggyStudio/BevyCSharp/blob/main/.github/COMPONENTS.md) plans generated mirrors.
- Component filters must be table-stored components, which is everything C# registers. A filter
  naming a Bevy-side sparse-set component is rejected rather than silently wrong.
- A cubemap comes from a file, as six square faces in a column, a row or a cross, or from a
  reflection probe that captures itself. One a game renders into with its own cameras, a layer at a time, has no
  bridge.
- Slang shaders compile with `slangc`, which the build fetches. A machine without it draws what
  was compiled and cached on one that had it, and cannot compile an edit.
- The renderer is open at fewer points than virtualized geometry, texture streaming, screen-space
  and world-space GI or reflections need. [.github/RENDERING.md](https://github.com/EggyStudio/BevyCSharp/blob/main/.github/RENDERING.md) lists what
  each of those asks of an engine, what is here, what is missing, and the order it is built in.

## Building from source

Using the package needs none of this, since it ships a prebuilt bridge for every supported
platform. This
is for working on the bridge itself.

```bash
build/build-native.sh          # the headless bridge
dotnet build                   # what copies it beside each project's binaries
dotnet test BevyCSharp.Tests/BevyCSharp.Tests.csproj
```

The two halves are built separately, and a rebuilt bridge is invisible until a managed build copies
it, which is why the second line is not optional. `build/build-native.sh --render` adds the renderer
and `--editor` adds the interface on top of it.

[.github/BUILDING.md](https://github.com/EggyStudio/BevyCSharp/blob/main/.github/BUILDING.md) has the rest: the three native profiles and what each
costs, the platforms and their prerequisites, how the package is assembled for six runtime
identifiers, and how a release is published.

## Contributing

Prose in this repository follows [.github/STYLE.md](https://github.com/EggyStudio/BevyCSharp/blob/main/.github/STYLE.md): no em dashes, no spaced
hyphens as punctuation, no colon joining two clauses where a full stop or a "because" belongs, no
padded section banners, and comments that explain why rather than restate the code.

## License

Mozilla Public License 2.0. The full text is in [LICENSE](https://github.com/EggyStudio/BevyCSharp/blob/main/LICENSE), and it ships inside the
package.

MPL-2.0 is file-level copyleft, meaning changes to files that are part of this project have to stay
under it and be made available in source form, while anything you build *around* it, including a
game that references the package, is yours under whatever terms you like. Bevy itself is MIT and
Apache-2.0, which this can incorporate freely.
