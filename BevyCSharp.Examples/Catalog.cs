using BevyCSharp.Examples.Application;
using BevyCSharp.Examples.AsyncTasks;
using BevyCSharp.Examples.Cameras;
using BevyCSharp.Examples.Clocks;
using BevyCSharp.Examples.Ecs;
using BevyCSharp.Examples.Inputs;
using BevyCSharp.Examples.Movement;
using BevyCSharp.Examples.Sound;
using BevyCSharp.Examples.States;
using BevyCSharp.Examples.ThreeD;
using BevyCSharp.Examples.Transforms;
using BevyCSharp.Examples.TwoD;

namespace BevyCSharp.Examples;

/// <summary>Every example written here, by Bevy's name.</summary>
/// <remarks>
/// Kept by hand, in Bevy's groups and in the order of its list, since nothing here reflects to
/// find them. build/examples-table.py finds an example's file under Bevy's folder name, and the
/// captures open each by the name given here, so the two have to agree, which a missing one shows
/// as an example that will not open.
/// </remarks>
internal static class Catalog
{
    public static readonly Example[] All =
    [
        // 2D Rendering
        new("cpu_draw", CpuDraw.Build, CpuDraw.Configure),
        new("move_sprite", MoveSprite.Build),
        new("rotate_to_cursor", RotateToCursor.Build),
        new("rotation", Rotation2d.Build, Rotation2d.Configure),
        new("sprite", SpriteExample.Build),
        new("sprite_animation", SpriteAnimation.Build),
        new("sprite_flipping", SpriteFlipping.Build),
        new("sprite_scale", SpriteScale.Build),
        new("sprite_sheet", SpriteSheet.Build),
        new("sprite_slice", SpriteSlice.Build),
        new("sprite_tile", SpriteTile.Build),
        new("text2d", Text2dExample.Build),
        new("transparency_2d", Transparency2d.Build),

        // 3D Rendering
        new("3d_scene", Example3dScene.Build),
        new("3d_shapes", Example3dShapes.Build),
        new("lighting", Lighting.Build),
        new("spotlight", Spotlight.Build),
        new("pbr", Pbr.Build),
        new("transparency_3d", Transparency3d.Build),
        new("two_passes", TwoPasses.Build),
        new("vertex_colors", VertexColors.Build),
        new("wireframe", Wireframe.Build),
        new("bloom_3d", Bloom3d.Build),
        new("shadow_caster_receiver", ShadowCasterReceiver.Build),
        new("spherical_area_lights", SphericalAreaLights.Build),
        new("animated_material", AnimatedMaterial.Build),
        new("render_to_texture", RenderToTexture.Build),
        new("split_screen", SplitScreen.Build),
        new("3d_viewport_to_world", Example3dViewportToWorld.Build),
        new("generate_custom_mesh", GenerateCustomMesh.Build),
        new("lines", Lines.Build),
        new("texture", Texture.Build),
        new("atmospheric_fog", AtmosphericFog.Build),
        new("fog", Fog.Build),
        new("rect_light", RectLight.Build),
        new("ssao", Ssao.Build),
        new("fog_volumes", FogVolumes.Build),
        new("scrolling_fog", ScrollingFog.Build),
        new("volumetric_fog", VolumetricFog.Build),
        new("rotate_environment_map", RotateEnvironmentMap.Build),
        new("skybox", Skybox.Build),
        new("post_processing", PostProcessing.Build),
        new("auto_exposure", AutoExposure.Build),
        new("clearcoat", Clearcoat.Build),
        new("order_independent_transparency", OrderIndependentTransparency.Build),
        new("motion_blur", MotionBlur.Build),
        new("lightmaps", Lightmaps.Build),
        new("depth_of_field", DepthOfField.Build),
        new("mixed_lighting", MixedLighting.Build),
        new("pcss", Pcss.Build),
        new("orthographic", Orthographic.Build),
        new("parenting", Parenting.Build),

        // Application
        new("empty", Empty.Build, Prints: 1),
        new("drag_and_drop", DragAndDrop.Build),
        new("empty_defaults", EmptyDefaults.Build),
        new("plugin", PluginExample.Build, Prints: 130),
        new("plugin_group", PluginGroupExample.Build, Prints: 3),
        new("return_after_run", ReturnAfterRun.Build, Prints: 3, Returned: ReturnAfterRun.Returned),
        new("settings", Settings.Build),

        // Audio
        new("audio", AudioExample.Build),
        new("play_sound_effect", PlaySoundEffect.Build),
        new("soundtrack", Soundtrack.Build),
        new("spatial_audio_3d", SpatialAudio3d.Build),

        // Camera
        new("2d_on_ui", TwoDOnUi.Build),
        new("camera_orbit", CameraOrbit.Build),
        new("projection_zoom", ProjectionZoom.Build),

        // ECS (Entity Component System)
        new("callbacks", Callbacks.Build, Prints: 2),
        new("fixed_timestep", FixedTimestep.Build, FixedTimestep.Configure, Prints: 64),
        new("generic_system", GenericSystem.Build, Prints: 130),
        new("hierarchy", Hierarchy.Build),
        new("iter_combinations", IterCombinations.Build),
        new("one_shot_systems", OneShotSystems.Build),
        new("parallel_query", ParallelQueryExample.Build),
        new("run_conditions", RunConditions.Build, Prints: 160),
        new("startup_system", StartupSystem.Build, Prints: 1),
        new("system_closure", SystemClosure.Build, Prints: 3),

        // Input
        new("char_input_events", CharInputEvents.Build, Prints: 10, Drive: CharInputEvents.Drive),
        new("gamepad_input", GamepadInput.Build, Prints: 12, Drive: GamepadInput.Drive),
        new("gamepad_rumble", GamepadRumble.Build, Prints: 24, Drive: GamepadRumble.Drive),
        new("keyboard_modifiers", KeyboardModifiers.Build, Prints: 10, Drive: KeyboardModifiers.Drive),
        new("mouse_grab", MouseGrab.Build),
        new("mouse_input", MouseInput.Build, Prints: 3),
        new("touch_input", TouchInput.Build, Prints: 3),

        // Transforms
        new("3d_rotation", Rotation3d.Build),
        new("align", Align.Build),
        new("scale", ScaleExample.Build),
        new("transform", TransformExample.Build),
        new("translation", Translation.Build),

        // Async Tasks
        new("async_channel_pattern", AsyncChannelPattern.Build),
        new("async_compute", AsyncCompute.Build),
        new("external_source_external_thread", ExternalSourceExternalThread.Build, ExternalSourceExternalThread.Configure),

        // Movement
        new("smooth_follow", SmoothFollow.Build),

        // State
        new("states", StatesExample.Build),
        new("sub_states", SubStates.Build),

        // Time
        new("timers", Timers.Build, Prints: 1300),
        new("virtual_time", VirtualTime.Build),
    ];

    public static bool TryFind(string name, out Example example)
    {
        example = All.FirstOrDefault(known => known.Name == name)!;
        return example is not null;
    }
}
