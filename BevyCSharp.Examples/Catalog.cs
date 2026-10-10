using BevyCSharp.Examples.Animations;
using BevyCSharp.Examples.Application;
using BevyCSharp.Examples.Assets;
using BevyCSharp.Examples.AsyncTasks;
using BevyCSharp.Examples.Cameras;
using BevyCSharp.Examples.Clocks;
using BevyCSharp.Examples.Ecs;
using BevyCSharp.Examples.Games;
using BevyCSharp.Examples.Gltf;
using BevyCSharp.Examples.Gizmo;
using BevyCSharp.Examples.Inputs;
using BevyCSharp.Examples.Interface;
using BevyCSharp.Examples.Maths;
using BevyCSharp.Examples.Movement;
using BevyCSharp.Examples.Pointers;
using BevyCSharp.Examples.Shading;
using BevyCSharp.Examples.Sound;
using BevyCSharp.Examples.States;
using BevyCSharp.Examples.StressTests;
using BevyCSharp.Examples.ThreeD;
using BevyCSharp.Examples.Tools;
using BevyCSharp.Examples.Transforms;
using BevyCSharp.Examples.TwoD;
using BevyCSharp.Examples.Usage;
using BevyCSharp.Examples.Windowing;

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
        new("2d_shapes", Shapes2d.Build, Shapes2d.Configure),
        new("2d_viewport_to_world", Example2dViewportToWorld.Build),
        new("bloom_2d", Bloom2d.Build),
        new("cpu_draw", CpuDraw.Build, CpuDraw.Configure),
        new("mesh2d", Mesh2dExample.Build),
        new("mesh2d_alpha_mode", Mesh2dAlphaMode.Build),
        new("mesh2d_arcs", Mesh2dArcs.Build),
        new("mesh2d_repeated_texture", Mesh2dRepeatedTexture.Build),
        new("mesh2d_vertex_color_texture", Mesh2dVertexColorTexture.Build),
        new("move_sprite", MoveSprite.Build),
        new("pixel_grid_snap", PixelGridSnap.Build),
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
        new("tilemap_chunk", TilemapChunkExample.Build),
        new("tilemap_chunk_orientation", TilemapChunkOrientation.Build),
        new("transparency_2d", Transparency2d.Build),
        new("wireframe_2d", Wireframe2dExample.Build, Wireframe2dExample.Configure),

        // 3D Rendering
        new("3d_scene", Example3dScene.Build),
        new("3d_shapes", Example3dShapes.Build, Example3dShapes.Configure),
        new("lighting", Lighting.Build),
        new("spotlight", Spotlight.Build),
        new("pbr", Pbr.Build),
        new("transparency_3d", Transparency3d.Build),
        new("two_passes", TwoPasses.Build),
        new("vertex_colors", VertexColors.Build),
        new("wireframe", Wireframe.Build, Wireframe.Configure),
        new("bloom_3d", Bloom3d.Build),
        new("shadow_caster_receiver", ShadowCasterReceiver.Build),
        new("anisotropy", Anisotropy.Build),
        new("anti_aliasing", AntiAliasing.Build),
        new("blend_modes", BlendModes.Build),
        new("camera_sub_view", CameraSubView.Build),
        new("clustered_decal_maps", ClusteredDecalMaps.Build),
        new("clustered_decals", ClusteredDecals.Build),
        new("irradiance_volumes", IrradianceVolumes.Build),
        new("color_grading", ColorGrading.Build),
        new("contact_shadows", ContactShadows.Build),
        new("pccm", Pccm.Build),
        new("shadow_biases", ShadowBiases.Build),
        new("tonemapping", TonemappingExample.Build),
        new("transmission", Transmission.Build),
        new("visibility_range", VisibilityRange.Build),
        new("spherical_area_lights", SphericalAreaLights.Build),
        new("animated_material", AnimatedMaterial.Build),
        new("render_to_texture", RenderToTexture.Build),
        new("split_screen", SplitScreen.Build),
        new("3d_viewport_to_world", Example3dViewportToWorld.Build),
        new("generate_custom_mesh", GenerateCustomMesh.Build),
        new("light_probe_blending", LightProbeBlending.Build),
        new("meshlet", Meshlet.Build, Meshlet.Configure),
        new("mirror", MirrorExample.Build),
        new("solari", Solari.Build, Solari.Configure),
        new("light_textures", LightTextures.Build),
        new("lines", Lines.Build),
        new("mesh_ray_cast", MeshRayCast.Build),
        new("texture", Texture.Build),
        new("atmospheric_fog", AtmosphericFog.Build),
        new("fog", Fog.Build),
        new("rect_light", RectLight.Build),
        new("reflection_probes", ReflectionProbes.Build),
        new("ssao", Ssao.Build),
        new("ssr", Ssr.Build),
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

        // Animation
        new("animated_mesh", AnimatedMesh.Build),
        new("animated_mesh_events", AnimatedMeshEvents.Build),
        new("animated_mesh_control", AnimatedMeshControl.Build),
        new("color_animation", ColorAnimation.Build),
        new("animated_transform", AnimatedTransform.Build),
        new("animated_ui", AnimatedUi.Build),
        new("animation_events", AnimationEvents.Build),
        new("animation_graph", AnimationGraphExample.Build),
        new("animation_masks", AnimationMasks.Build),
        new("custom_skinned_mesh", CustomSkinnedMesh.Build),
        new("eased_motion", EasedMotion.Build),
        new("easing_functions", EasingFunctions.Build),
        new("morph_targets", MorphTargets.Build),

        // Application
        new("empty", Empty.Build, Prints: 1),
        new("drag_and_drop", DragAndDrop.Build),
        new("empty_defaults", EmptyDefaults.Build),
        new("hello_world", HelloWorld.Build, Prints: 1),
        new("logs", Logs.Build),
        new("headless", Headless.Build, Prints: 1, Returned: Headless.Returned),
        new("headless_renderer", HeadlessRenderer.Build, HeadlessRenderer.Configure),
        new("plugin", PluginExample.Build, Prints: 130),
        new("plugin_group", PluginGroupExample.Build, Prints: 3),
        new("return_after_run", ReturnAfterRun.Build, Prints: 3, Returned: ReturnAfterRun.Returned),
        new("settings", Settings.Build),

        // Assets
        new("alter_mesh", AlterMesh.Build),
        new("alter_sprite", AlterSprite.Build),
        new("asset_loading", AssetLoading.Build),
        new("custom_asset", CustomAssetExample.Build, Prints: 30),
        new("embedded_asset", EmbeddedAsset.Build),
        new("extra_asset_source", ExtraSource.Build, ExtraSource.Configure),
        new("generated_assets", GeneratedAssets.Build),
        new("hot_asset_reloading", HotAssetReloading.Build),
        new("multi_asset_sync", MultiAssetSync.Build),
        new("repeated_texture", RepeatedTexture.Build),

        // Audio
        new("audio", AudioExample.Build),
        new("audio_control", AudioControl.Build),
        new("play_sound_effect", PlaySoundEffect.Build),
        new("soundtrack", Soundtrack.Build),
        new("spatial_audio_2d", SpatialAudio2d.Build, SpatialAudio2d.Configure),
        new("spatial_audio_3d", SpatialAudio3d.Build),

        // Camera
        new("2d_on_ui", TwoDOnUi.Build),
        new("2d_screen_shake", ScreenShake2d.Build),
        new("2d_top_down_camera", TopDownCamera2d.Build),
        new("camera_orbit", CameraOrbit.Build),
        new("first_person_view_model", FirstPersonViewModel.Build),
        new("projection_zoom", ProjectionZoom.Build),

        // ECS (Entity Component System)
        new("callbacks", Callbacks.Build, Prints: 2),
        new("component_hooks", ComponentHooksExample.Build, Prints: 12, Drive: ComponentHooksExample.Drive),
        new("delayed_commands", DelayedCommands.Build),
        new("ecs_guide", EcsGuide.Build, Prints: 20),
        new("fixed_timestep", FixedTimestep.Build, FixedTimestep.Configure, Prints: 64),
        new("generic_system", GenericSystem.Build, Prints: 130),
        new("hierarchy", Hierarchy.Build),
        new("hotpatching_systems", HotpatchingSystems.Build),
        new("iter_combinations", IterCombinations.Build),
        new("observer_propagation", ObserverPropagation.Build, Prints: 1800),
        new("observers", Observers.Build),
        new("one_shot_systems", OneShotSystems.Build),
        new("parallel_query", ParallelQueryExample.Build),
        new("removal_detection", RemovalDetection.Build),
        new("entity_disabling", EntityDisabling.Build, EntityDisabling.Configure),
        new("run_conditions", RunConditions.Build, Prints: 160),
        new("startup_system", StartupSystem.Build, Prints: 1),
        new("state_scoped", StateScoped.Build),
        new("system_closure", SystemClosure.Build, Prints: 3),

        // Games
        new("alien_cake_addict", AlienCakeAddict.Build),
        new("breakout", Breakout.Build),
        new("contributors", Contributors.Build),
        new("desk_toy", DeskToy.Build, DeskToy.Configure),
        new("game_menu", GameMenu.Build),
        new("loading_screen", LoadingScreenExample.Build),

        // glTF
        new("edit_material_on_gltf", EditMaterialOnGltf.Build),
        new("gltf_skinned_mesh", GltfSkinnedMesh.Build),
        new("load_gltf", LoadGltf.Build),
        new("query_gltf_primitives", QueryGltfPrimitives.Build),
        new("update_gltf_scene", UpdateGltfScene.Build),

        // Gizmos
        new("2d_gizmos", Gizmos2d.Build),
        new("2d_text_gizmos", TextGizmos2d.Build),
        new("3d_gizmos", Gizmos3d.Build),
        new("3d_text_gizmos", TextGizmos3d.Build),
        new("anchored_text_gizmos", AnchoredTextGizmos.Build),
        new("axes", Axes.Build),
        new("light_gizmos", LightGizmos.Build),
        new("text_gizmos_font", TextGizmosFont.Build),

        // Input
        new("char_input_events", CharInputEvents.Build, Prints: 10, Drive: CharInputEvents.Drive),
        new("gamepad_input", GamepadInput.Build, Prints: 12, Drive: GamepadInput.Drive),
        new("gamepad_input_events", GamepadInputEvents.Build, Prints: 12, Drive: GamepadInputEvents.Drive),
        new("gamepad_rumble", GamepadRumble.Build, Prints: 24, Drive: GamepadRumble.Drive),
        new("keyboard_input", KeyboardInputExample.Build, Prints: 10, Drive: KeyboardInputExample.Drive),
        new("keyboard_input_events", KeyboardInputEvents.Build, Prints: 10, Drive: KeyboardInputEvents.Drive),
        new("keyboard_modifiers", KeyboardModifiers.Build, Prints: 10, Drive: KeyboardModifiers.Drive),
        new("mouse_grab", MouseGrab.Build),
        new("mouse_input", MouseInput.Build, Prints: 3),
        new("mouse_input_events", MouseInputEvents.Build, Prints: 10, Drive: MouseInputEvents.Drive),
        new("touch_input", TouchInput.Build, Prints: 3),
        new("touch_input_events", TouchInputEvents.Build, Prints: 3),

        // Math
        new("bounding_2d", Bounding2d.Build),
        new("cubic_splines", CubicSplines.Build),
        new("random_sampling", RandomSampling.Build),

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
        new("physics_in_fixed_timestep", PhysicsInFixedTimestep.Build),
        new("smooth_follow", SmoothFollow.Build),

        // Picking
        new("mesh_picking", MeshPicking.Build, MeshPicking.Configure),
        new("simple_picking", SimplePicking.Build, SimplePicking.Configure),
        new("sprite_picking", SpritePicking.Build),
        new("dragdrop_picking", DragdropPicking.Build, DragdropPicking.Configure),
        new("draggable_slider", DraggableSlider.Build),

        // State
        new("computed_states", ComputedStatesExample.Build),
        new("custom_transitions", CustomTransitions.Build),
        new("states", StatesExample.Build),
        new("sub_states", SubStates.Build),

        // Stress Tests
        new("bevymark", Bevymark.Build, Bevymark.Configure),
        new("bevymark_3d", Bevymark3d.Build, Bevymark3d.Configure),
        new("many_animated_sprites", ManyAnimatedSprites.Build, ManyAnimatedSprites.Configure),
        new("many_buttons", ManyButtons.Build, ManyButtons.Configure),
        new("many_cameras_lights", ManyCamerasLights.Build, ManyCamerasLights.Configure),
        new("many_cubes", ManyCubes.Build, ManyCubes.Configure),
        new("many_foxes", ManyFoxes.Build, ManyFoxes.Configure),
        new("many_gizmos", ManyGizmos.Build, ManyGizmos.Configure),
        new("many_glyphs", ManyGlyphs.Build, ManyGlyphs.Configure),
        new("many_gradients", ManyGradients.Build, ManyGradients.Configure),
        new("many_lights", ManyLights.Build, ManyLights.Configure),
        new("many_materials", ManyMaterials.Build, ManyMaterials.Configure),
        new("many_morph_targets", ManyMorphTargets.Build, ManyMorphTargets.Configure),
        new("many_sprites", ManySprites.Build, ManySprites.Configure),
        new("many_text", ManyText.Build, ManyText.Configure),
        new("many_text2d", ManyText2d.Build, ManyText2d.Configure),
        new("text_pipeline", TextPipeline.Build, TextPipeline.Configure),
        new("transform_hierarchy", TransformHierarchy.Build, TransformHierarchy.Configure),

        // Time
        new("timers", Timers.Build, Prints: 1300),
        new("virtual_time", VirtualTimeExample.Build),

        // Tools
        new("gamepad_viewer", GamepadViewer.Build),

        // Shaders
        new("animate_shader", AnimateShader.Build),
        new("array_texture", ArrayTexture.Build),
        new("automatic_instancing", AutomaticInstancing.Build),
        new("compute_shader_game_of_life", ComputeShaderGameOfLife.Build, ComputeShaderGameOfLife.Configure),
        new("extended_material", ExtendedMaterial.Build),
        new("extended_material_bindless", ExtendedMaterialBindless.Build),
        new("gpu_readback", GpuReadback.Build),
        new("shader_defs", ShaderDefs.Build),
        new("shader_material", ShaderMaterialExample.Build),
        new("shader_material_2d", ShaderMaterial2dExample.Build),
        new("shader_material_bindless", ShaderMaterialBindless.Build),
        new("shader_material_screenspace_texture", ShaderMaterialScreenspaceTexture.Build),
        new("shader_prepass", ShaderPrepass.Build),
        new("storage_buffer", StorageBuffer.Build),

        // Shaders - Advanced
        new("compute_mesh", ComputeMesh.Build),
        new("custom_post_processing", CustomPostProcessing.Build),
        new("custom_shader_instancing", CustomShaderInstancing.Build),
        new("deferred_raymarch", DeferredRaymarch.Build),
        new("fullscreen_material", FullscreenMaterial.Build),
        new("texture_binding_array", TextureBindingArray.Build),

        // UI (User Interface)
        new("anchor_layout", AnchorLayout.Build),
        new("borders", Borders.Build),
        new("box_shadow", BoxShadowExample.Build),
        new("button", ButtonExample.Build),
        new("directional_navigation", DirectionalNavigationExample.Build),
        new("directional_navigation_overrides", DirectionalNavigationOverrides.Build),
        new("display_and_visibility", DisplayAndVisibility.Build),
        new("editable_text_filter", EditableTextFilter.Build),
        new("flex_layout", FlexLayout.Build),
        new("grid", Grid.Build, Grid.Configure),
        new("ui_drag_and_drop", UiDragAndDrop.Build),
        new("drag_to_scroll", DragToScroll.Build),
        new("scroll", ScrollExample.Build),
        new("viewport_node", ViewportNode.Build, ViewportNode.Configure),
        new("window_fallthrough", WindowFallthrough.Build, WindowFallthrough.Configure),
        new("render_ui_to_texture", RenderUiToTexture.Build),
        new("gradients", Gradients.Build),
        new("image_node", ImageNode.Build),
        new("letter_spacing", LetterSpacingExample.Build),
        new("overflow", OverflowExample.Build),
        new("overflow_clip_margin", OverflowClipMargin.Build),
        new("overflow_debug", OverflowDebug.Build),
        new("relative_cursor_position", RelativeCursorPosition.Build),
        new("scrollbars", Scrollbars.Build),
        new("size_constraints", SizeConstraints.Build),
        new("stacked_gradients", StackedGradients.Build),
        new("tab_navigation", TabNavigation.Build),
        new("text_debug", TextDebug.Build),
        new("text", TextExample.Build),
        new("strikethrough_and_underline", StrikethroughAndUnderline.Build),
        new("text_background_colors", TextBackgroundColors.Build),
        new("font_weights", FontWeights.Build),
        new("font_variations", FontVariationsExample.Build),
        new("font_query", FontQuery.Build),
        new("text_input", TextInput.Build),
        new("multiline_text_input", MultilineTextInput.Build),
        new("multiple_text_inputs", MultipleTextInputs.Build),
        new("text_wrap_debug", TextWrapDebug.Build),
        new("transparency_ui", TransparencyUi.Build),
        new("ui_target_camera", UiTargetCamera.Build),
        new("ui_scaling", UiScaling.Build),
        new("ui_transform", UiTransformExample.Build),
        new("ui_texture_atlas", UiTextureAtlas.Build),
        new("ui_texture_atlas_slice", UiTextureAtlasSlice.Build),
        new("ui_texture_slice", UiTextureSlice.Build),
        new("ui_texture_slice_flip_and_tile", UiTextureSliceFlipAndTile.Build),
        new("vertical_slider", VerticalSliderExample.Build),
        new("standard_widgets", StandardWidgets.Build),
        new("standard_widgets_observers", StandardWidgetsObservers.Build),
        new("headless_tabs", HeadlessTabs.Build),
        new("z_index", ZIndex.Build),

        // Usage
        new("character_creation", CharacterCreation.Build),
        new("context_menu", ContextMenuExample.Build),
        new("cooldown", CooldownExample.Build),
        new("debug_frustum_culling", DebugFrustumCulling.Build),

        // Window
        new("clear_color", ClearColor.Build),
        new("multi_window_text", MultiWindowText.Build, MultiWindowText.Configure),
        new("multiple_windows", MultipleWindows.Build),
        new("persisting_window_settings", PersistingWindowSettings.Build, PersistingWindowSettings.Configure),
        new("scale_factor_override", ScaleFactorOverride.Build, ScaleFactorOverride.Configure),
        new("screenshot", Screenshot.Build),
        new("transparent_window", TransparentWindow.Build, TransparentWindow.Configure),
        new("window_drag_move", WindowDragMove.Build),
        new("window_resizing", WindowResizing.Build),
        new("window_settings", WindowSettings.Build, WindowSettings.Configure),

        // Kept out of Bevy's list
        new("minimizing", Minimizing.Build, Minimizing.Configure),
        new("resizing", Resizing.Build, Resizing.Configure),
    ];

    public static bool TryFind(string name, out Example example)
    {
        example = All.FirstOrDefault(known => known.Name == name)!;
        return example is not null;
    }
}
