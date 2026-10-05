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

This is Bevy, run unchanged underneath, so what a game draws and how its
systems are scheduled are Bevy's. What it adds is C#, behaviors reloaded while the game runs, an
editor, scene files and saves, physics, and a package with no Rust in it. What it costs is a call
across to the bridge each time C# reaches Bevy, and only what the bridge has opened, which reaches
two thirds of the Bevy examples about a game rather than Rust. [Compared with Bevy](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/compared-with-bevy.md) has each, with what was measured.

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
lacks. A picture opens Bevy's own Rust example running live in the browser on bevy.org, where
Bevy's site has one, to set beside the C# one here.

<!-- example-gallery -->
<table>
<tr><td><a href="https://bevy.org/examples/2d-rendering/bloom-2d/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/bloom_2d.webp" width="200"/></a><br><code>bloom_2d</code></td><td><a href="https://bevy.org/examples/2d-rendering/move-sprite/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/move_sprite.webp" width="200"/></a><br><code>move_sprite</code></td><td><a href="https://bevy.org/examples/2d-rendering/2d-viewport-to-world/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/2d_viewport_to_world.webp" width="200"/></a><br><code>2d_viewport_to_world</code></td><td><a href="https://bevy.org/examples/2d-rendering/rotate-to-cursor/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/rotate_to_cursor.webp" width="200"/></a><br><code>rotate_to_cursor</code></td></tr>
<tr><td><a href="https://bevy.org/examples/2d-rendering/rotation/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/rotation.webp" width="200"/></a><br><code>rotation</code></td><td><a href="https://bevy.org/examples/2d-rendering/mesh2d/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/mesh2d.webp" width="200"/></a><br><code>mesh2d</code></td><td><a href="https://bevy.org/examples/2d-rendering/mesh2d-vertex-color-texture/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/mesh2d_vertex_color_texture.webp" width="200"/></a><br><code>mesh2d_vertex_color_texture</code></td><td><a href="https://bevy.org/examples/2d-rendering/2d-shapes/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/2d_shapes.webp" width="200"/></a><br><code>2d_shapes</code></td></tr>
<tr><td><a href="https://bevy.org/examples/2d-rendering/cpu-draw/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/cpu_draw.webp" width="200"/></a><br><code>cpu_draw</code></td><td><a href="https://bevy.org/examples/2d-rendering/sprite/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sprite.webp" width="200"/></a><br><code>sprite</code></td><td><a href="https://bevy.org/examples/2d-rendering/sprite-animation/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sprite_animation.webp" width="200"/></a><br><code>sprite_animation</code></td><td><a href="https://bevy.org/examples/2d-rendering/sprite-scale/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sprite_scale.webp" width="200"/></a><br><code>sprite_scale</code></td></tr>
<tr><td><a href="https://bevy.org/examples/2d-rendering/sprite-flipping/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sprite_flipping.webp" width="200"/></a><br><code>sprite_flipping</code></td><td><a href="https://bevy.org/examples/2d-rendering/sprite-sheet/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sprite_sheet.webp" width="200"/></a><br><code>sprite_sheet</code></td><td><a href="https://bevy.org/examples/2d-rendering/sprite-tile/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sprite_tile.webp" width="200"/></a><br><code>sprite_tile</code></td><td><a href="https://bevy.org/examples/2d-rendering/sprite-slice/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sprite_slice.webp" width="200"/></a><br><code>sprite_slice</code></td></tr>
<tr><td><a href="https://bevy.org/examples/2d-rendering/text2d/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/text2d.webp" width="200"/></a><br><code>text2d</code></td><td><a href="https://bevy.org/examples/2d-rendering/transparency-2d/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/transparency_2d.webp" width="200"/></a><br><code>transparency_2d</code></td><td><a href="https://bevy.org/examples/2d-rendering/mesh2d-alpha-mode/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/mesh2d_alpha_mode.webp" width="200"/></a><br><code>mesh2d_alpha_mode</code></td><td><a href="https://bevy.org/examples/2d-rendering/mesh2d-repeated-texture/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/mesh2d_repeated_texture.webp" width="200"/></a><br><code>mesh2d_repeated_texture</code></td></tr>
<tr><td><a href="https://bevy.org/examples/2d-rendering/pixel-grid-snap/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/pixel_grid_snap.webp" width="200"/></a><br><code>pixel_grid_snap</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/wireframe_2d.webp" width="200"/><br><code>wireframe_2d</code></td><td><a href="https://bevy.org/examples/3d-rendering/3d-scene/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/3d_scene.webp" width="200"/></a><br><code>3d_scene</code></td><td><a href="https://bevy.org/examples/3d-rendering/rect-light/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/rect_light.webp" width="200"/></a><br><code>rect_light</code></td></tr>
<tr><td><a href="https://bevy.org/examples/3d-rendering/3d-shapes/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/3d_shapes.webp" width="200"/></a><br><code>3d_shapes</code></td><td><a href="https://bevy.org/examples/3d-rendering/3d-viewport-to-world/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/3d_viewport_to_world.webp" width="200"/></a><br><code>3d_viewport_to_world</code></td><td><a href="https://bevy.org/examples/3d-rendering/animated-material/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/animated_material.webp" width="200"/></a><br><code>animated_material</code></td><td><a href="https://bevy.org/examples/3d-rendering/generate-custom-mesh/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/generate_custom_mesh.webp" width="200"/></a><br><code>generate_custom_mesh</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/anti_aliasing.webp" width="200"/><br><code>anti_aliasing</code></td><td><a href="https://bevy.org/examples/3d-rendering/atmospheric-fog/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/atmospheric_fog.webp" width="200"/></a><br><code>atmospheric_fog</code></td><td><a href="https://bevy.org/examples/3d-rendering/fog/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/fog.webp" width="200"/></a><br><code>fog</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/auto_exposure.webp" width="200"/><br><code>auto_exposure</code></td></tr>
<tr><td><a href="https://bevy.org/examples/3d-rendering/blend-modes/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/blend_modes.webp" width="200"/></a><br><code>blend_modes</code></td><td><a href="https://bevy.org/examples/3d-rendering/contact-shadows/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/contact_shadows.webp" width="200"/></a><br><code>contact_shadows</code></td><td><a href="https://bevy.org/examples/3d-rendering/lighting/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/lighting.webp" width="200"/></a><br><code>lighting</code></td><td><a href="https://bevy.org/examples/3d-rendering/lines/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/lines.webp" width="200"/></a><br><code>lines</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/ssao.webp" width="200"/><br><code>ssao</code></td><td><a href="https://bevy.org/examples/3d-rendering/spotlight/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/spotlight.webp" width="200"/></a><br><code>spotlight</code></td><td><a href="https://bevy.org/examples/3d-rendering/bloom-3d/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/bloom_3d.webp" width="200"/></a><br><code>bloom_3d</code></td><td><a href="https://bevy.org/examples/3d-rendering/motion-blur/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/motion_blur.webp" width="200"/></a><br><code>motion_blur</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/order_independent_transparency.webp" width="200"/><br><code>order_independent_transparency</code></td><td><a href="https://bevy.org/examples/3d-rendering/tonemapping/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/tonemapping.webp" width="200"/></a><br><code>tonemapping</code></td><td><a href="https://bevy.org/examples/3d-rendering/orthographic/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/orthographic.webp" width="200"/></a><br><code>orthographic</code></td><td><a href="https://bevy.org/examples/3d-rendering/parenting/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/parenting.webp" width="200"/></a><br><code>parenting</code></td></tr>
<tr><td><a href="https://bevy.org/examples/3d-rendering/pbr/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/pbr.webp" width="200"/></a><br><code>pbr</code></td><td><a href="https://bevy.org/examples/3d-rendering/render-to-texture/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/render_to_texture.webp" width="200"/></a><br><code>render_to_texture</code></td><td><a href="https://bevy.org/examples/3d-rendering/shadow-biases/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/shadow_biases.webp" width="200"/></a><br><code>shadow_biases</code></td><td><a href="https://bevy.org/examples/3d-rendering/shadow-caster-receiver/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/shadow_caster_receiver.webp" width="200"/></a><br><code>shadow_caster_receiver</code></td></tr>
<tr><td><a href="https://bevy.org/examples/3d-rendering/skybox/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/skybox.webp" width="200"/></a><br><code>skybox</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/solari.webp" width="200"/><br><code>solari</code></td><td><a href="https://bevy.org/examples/3d-rendering/spherical-area-lights/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/spherical_area_lights.webp" width="200"/></a><br><code>spherical_area_lights</code></td><td><a href="https://bevy.org/examples/3d-rendering/split-screen/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/split_screen.webp" width="200"/></a><br><code>split_screen</code></td></tr>
<tr><td><a href="https://bevy.org/examples/3d-rendering/texture/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/texture.webp" width="200"/></a><br><code>texture</code></td><td><a href="https://bevy.org/examples/3d-rendering/transparency-3d/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/transparency_3d.webp" width="200"/></a><br><code>transparency_3d</code></td><td><a href="https://bevy.org/examples/3d-rendering/transmission/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/transmission.webp" width="200"/></a><br><code>transmission</code></td><td><a href="https://bevy.org/examples/3d-rendering/two-passes/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/two_passes.webp" width="200"/></a><br><code>two_passes</code></td></tr>
<tr><td><a href="https://bevy.org/examples/3d-rendering/vertex-colors/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/vertex_colors.webp" width="200"/></a><br><code>vertex_colors</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/wireframe.webp" width="200"/><br><code>wireframe</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/meshlet.webp" width="200"/><br><code>meshlet</code></td><td><a href="https://bevy.org/examples/3d-rendering/mesh-ray-cast/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/mesh_ray_cast.webp" width="200"/></a><br><code>mesh_ray_cast</code></td></tr>
<tr><td><a href="https://bevy.org/examples/3d-rendering/lightmaps/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/lightmaps.webp" width="200"/></a><br><code>lightmaps</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/drag_and_drop.webp" width="200"/><br><code>drag_and_drop</code></td><td><a href="https://bevy.org/examples/application/empty-defaults/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/empty_defaults.webp" width="200"/></a><br><code>empty_defaults</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/async_compute.webp" width="200"/><br><code>async_compute</code></td></tr>
<tr><td><a href="https://bevy.org/examples/async-tasks/async-channel-pattern/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/async_channel_pattern.webp" width="200"/></a><br><code>async_channel_pattern</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/external_source_external_thread.webp" width="200"/><br><code>external_source_external_thread</code></td><td><a href="https://bevy.org/examples/audio/audio/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/audio.webp" width="200"/></a><br><code>audio</code></td><td><a href="https://bevy.org/examples/audio/soundtrack/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/soundtrack.webp" width="200"/></a><br><code>soundtrack</code></td></tr>
<tr><td><a href="https://bevy.org/examples/audio/spatial-audio-2d/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/spatial_audio_2d.webp" width="200"/></a><br><code>spatial_audio_2d</code></td><td><a href="https://bevy.org/examples/audio/spatial-audio-3d/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/spatial_audio_3d.webp" width="200"/></a><br><code>spatial_audio_3d</code></td><td><a href="https://bevy.org/examples/audio/play-sound-effect/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/play_sound_effect.webp" width="200"/></a><br><code>play_sound_effect</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/hierarchy.webp" width="200"/><br><code>hierarchy</code></td></tr>
<tr><td><a href="https://bevy.org/examples/ecs-entity-component-system/iter-combinations/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/iter_combinations.webp" width="200"/></a><br><code>iter_combinations</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/one_shot_systems.webp" width="200"/><br><code>one_shot_systems</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/parallel_query.webp" width="200"/><br><code>parallel_query</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/removal_detection.webp" width="200"/><br><code>removal_detection</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/states.webp" width="200"/><br><code>states</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/sub_states.webp" width="200"/><br><code>sub_states</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/virtual_time.webp" width="200"/><br><code>virtual_time</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/mouse_grab.webp" width="200"/><br><code>mouse_grab</code></td></tr>
<tr><td><a href="https://bevy.org/examples/shaders/extended-material/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/extended_material.webp" width="200"/></a><br><code>extended_material</code></td><td><a href="https://bevy.org/examples/ecs-entity-component-system/observers/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/observers.webp" width="200"/></a><br><code>observers</code></td><td><a href="https://bevy.org/examples/transforms/3d-rotation/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/3d_rotation.webp" width="200"/></a><br><code>3d_rotation</code></td><td><a href="https://bevy.org/examples/transforms/align/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/align.webp" width="200"/></a><br><code>align</code></td></tr>
<tr><td><a href="https://bevy.org/examples/transforms/scale/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/scale.webp" width="200"/></a><br><code>scale</code></td><td><a href="https://bevy.org/examples/transforms/transform/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/transform.webp" width="200"/></a><br><code>transform</code></td><td><a href="https://bevy.org/examples/transforms/translation/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/translation.webp" width="200"/></a><br><code>translation</code></td><td><a href="https://bevy.org/examples/ui-user-interface/anchor-layout/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/anchor_layout.webp" width="200"/></a><br><code>anchor_layout</code></td></tr>
<tr><td><a href="https://bevy.org/examples/ui-user-interface/borders/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/borders.webp" width="200"/></a><br><code>borders</code></td><td><a href="https://bevy.org/examples/ui-user-interface/box-shadow/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/box_shadow.webp" width="200"/></a><br><code>box_shadow</code></td><td><a href="https://bevy.org/examples/ui-user-interface/button/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/button.webp" width="200"/></a><br><code>button</code></td><td><a href="https://bevy.org/examples/ui-user-interface/display-and-visibility/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/display_and_visibility.webp" width="200"/></a><br><code>display_and_visibility</code></td></tr>
<tr><td><a href="https://bevy.org/examples/ui-user-interface/overflow/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/overflow.webp" width="200"/></a><br><code>overflow</code></td><td><a href="https://bevy.org/examples/ui-user-interface/overflow-clip-margin/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/overflow_clip_margin.webp" width="200"/></a><br><code>overflow_clip_margin</code></td><td><a href="https://bevy.org/examples/ui-user-interface/overflow-debug/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/overflow_debug.webp" width="200"/></a><br><code>overflow_debug</code></td><td><a href="https://bevy.org/examples/ui-user-interface/relative-cursor-position/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/relative_cursor_position.webp" width="200"/></a><br><code>relative_cursor_position</code></td></tr>
<tr><td><a href="https://bevy.org/examples/ui-user-interface/size-constraints/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/size_constraints.webp" width="200"/></a><br><code>size_constraints</code></td><td><a href="https://bevy.org/examples/ui-user-interface/text-debug/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/text_debug.webp" width="200"/></a><br><code>text_debug</code></td><td><a href="https://bevy.org/examples/ui-user-interface/flex-layout/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/flex_layout.webp" width="200"/></a><br><code>flex_layout</code></td><td><a href="https://bevy.org/examples/ui-user-interface/text-wrap-debug/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/text_wrap_debug.webp" width="200"/></a><br><code>text_wrap_debug</code></td></tr>
<tr><td><a href="https://bevy.org/examples/ui-user-interface/letter-spacing/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/letter_spacing.webp" width="200"/></a><br><code>letter_spacing</code></td><td><a href="https://bevy.org/examples/ui-user-interface/grid/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/grid.webp" width="200"/></a><br><code>grid</code></td><td><a href="https://bevy.org/examples/ui-user-interface/gradients/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/gradients.webp" width="200"/></a><br><code>gradients</code></td><td><a href="https://bevy.org/examples/ui-user-interface/stacked-gradients/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/stacked_gradients.webp" width="200"/></a><br><code>stacked_gradients</code></td></tr>
<tr><td><a href="https://bevy.org/examples/ui-user-interface/transparency-ui/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/transparency_ui.webp" width="200"/></a><br><code>transparency_ui</code></td><td><a href="https://bevy.org/examples/ui-user-interface/z-index/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/z_index.webp" width="200"/></a><br><code>z_index</code></td><td><a href="https://bevy.org/examples/ui-user-interface/ui-scaling/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/ui_scaling.webp" width="200"/></a><br><code>ui_scaling</code></td><td><a href="https://bevy.org/examples/ui-user-interface/ui-texture-atlas/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/ui_texture_atlas.webp" width="200"/></a><br><code>ui_texture_atlas</code></td></tr>
<tr><td><a href="https://bevy.org/examples/ui-user-interface/ui-texture-slice/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/ui_texture_slice.webp" width="200"/></a><br><code>ui_texture_slice</code></td><td><a href="https://bevy.org/examples/ui-user-interface/ui-texture-slice-flip-and-tile/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/ui_texture_slice_flip_and_tile.webp" width="200"/></a><br><code>ui_texture_slice_flip_and_tile</code></td><td><a href="https://bevy.org/examples/ui-user-interface/ui-texture-atlas-slice/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/ui_texture_atlas_slice.webp" width="200"/></a><br><code>ui_texture_atlas_slice</code></td><td><a href="https://bevy.org/examples/ui-user-interface/ui-transform/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/ui_transform.webp" width="200"/></a><br><code>ui_transform</code></td></tr>
<tr><td><a href="https://bevy.org/examples/ui-user-interface/ui-target-camera/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/ui_target_camera.webp" width="200"/></a><br><code>ui_target_camera</code></td><td><a href="https://bevy.org/examples/ui-user-interface/image-node/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/image_node.webp" width="200"/></a><br><code>image_node</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/reflection_probes.webp" width="200"/><br><code>reflection_probes</code></td><td><a href="https://bevy.org/examples/math/smooth-follow/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/smooth_follow.webp" width="200"/></a><br><code>smooth_follow</code></td></tr>
<tr><td><a href="https://bevy.org/examples/camera/2d-top-down-camera/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/2d_top_down_camera.webp" width="200"/></a><br><code>2d_top_down_camera</code></td><td><a href="https://bevy.org/examples/camera/first-person-view-model/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/first_person_view_model.webp" width="200"/></a><br><code>first_person_view_model</code></td><td><a href="https://bevy.org/examples/camera/projection-zoom/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/projection_zoom.webp" width="200"/></a><br><code>projection_zoom</code></td><td><a href="https://bevy.org/examples/camera/camera-orbit/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/camera_orbit.webp" width="200"/></a><br><code>camera_orbit</code></td></tr>
<tr><td><a href="https://bevy.org/examples/camera/2d-screen-shake/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/2d_screen_shake.webp" width="200"/></a><br><code>2d_screen_shake</code></td><td><a href="https://bevy.org/examples/camera/2d-on-ui/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/2d_on_ui.webp" width="200"/></a><br><code>2d_on_ui</code></td><td><a href="https://bevy.org/examples/3d-rendering/visibility-range/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/visibility_range.webp" width="200"/></a><br><code>visibility_range</code></td><td><a href="https://bevy.org/examples/3d-rendering/camera-sub-view/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/camera_sub_view.webp" width="200"/></a><br><code>camera_sub_view</code></td></tr>
<tr><td><a href="https://bevy.org/examples/3d-rendering/color-grading/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/color_grading.webp" width="200"/></a><br><code>color_grading</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/clearcoat.webp" width="200"/><br><code>clearcoat</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/depth_of_field.webp" width="200"/><br><code>depth_of_field</code></td><td><a href="https://bevy.org/examples/3d-rendering/volumetric-fog/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/volumetric_fog.webp" width="200"/></a><br><code>volumetric_fog</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/anisotropy.webp" width="200"/><br><code>anisotropy</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/fog_volumes.webp" width="200"/><br><code>fog_volumes</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/scrolling_fog.webp" width="200"/><br><code>scrolling_fog</code></td><td><a href="https://bevy.org/examples/3d-rendering/post-processing/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/post_processing.webp" width="200"/></a><br><code>post_processing</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/rotate_environment_map.webp" width="200"/><br><code>rotate_environment_map</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/pcss.webp" width="200"/><br><code>pcss</code></td><td><a href="https://bevy.org/examples/3d-rendering/mixed-lighting/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/mixed_lighting.webp" width="200"/></a><br><code>mixed_lighting</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/light_textures.webp" width="200"/><br><code>light_textures</code></td></tr>
<tr><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/extended_material_bindless.webp" width="200"/><br><code>extended_material_bindless</code></td><td><a href="https://bevy.org/examples/application/settings/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/settings.webp" width="200"/></a><br><code>settings</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/clustered_decal_maps.webp" width="200"/><br><code>clustered_decal_maps</code></td><td><a href="https://bevy.org/examples/3d-rendering/mirror/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/mirror.webp" width="200"/></a><br><code>mirror</code></td></tr>
<tr><td><a href="https://bevy.org/examples/3d-rendering/pccm/"><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/pccm.webp" width="200"/></a><br><code>pccm</code></td><td><img src="https://raw.githubusercontent.com/EggyStudio/BevyCSharp/main/.github/assets/examples/light_probe_blending.webp" width="200"/><br><code>light_probe_blending</code></td></tr>
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
| [Messages and the hierarchy](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/messages-and-hierarchy.md) | One system telling another, code run the moment something happens, and entities parented |
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
| [Compared with Bevy](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/compared-with-bevy.md) | What is the same, what it adds, what it costs, and what was measured |

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
Of Bevy's 421 examples, 153 are written in C# here and 8 more in part, 95 more can be with what is bridged, 108 wait on something the bridge lacks and 57 are about Rust itself ([EXAMPLES.md](https://github.com/EggyStudio/BevyCSharp/blob/main/.github/EXAMPLES.md)).
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
