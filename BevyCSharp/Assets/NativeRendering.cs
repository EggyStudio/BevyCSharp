using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's entry points for the rendering, cameras and their controllers, lights and shadows,
/// the look, the environment, meshes, materials, images, captures, picking and the shader programs.
/// </summary>
internal static unsafe partial class NativeRendering
{
    // The bridge every entry point here is imported from, named once in Native.
    private const string Library = Native.Library;

    // Runs the loader's check before this class's first call, as Native's remarks say.
    static NativeRendering() => NativeLoader.Initialize();

    // -- Cameras

    /// <summary>Says how a camera draws beyond what it looks at.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_camera_flags(ulong camera, uint flags);

    /// <summary>Spawns a 3D camera and returns its entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_render_spawn_camera_3d(NativeCameraConfig* config);

    /// <summary>Sets a camera's field of view and clip distances.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_perspective(ulong camera, float fovDegrees, float near, float far);

    /// <summary>Rounds the corners of a camera's picture, showing a fill outside them, or squares them.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_rounded_corners(ulong camera, float radius, float r, float g, float b, float a);

    /// <summary>Sets the world's clear color.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_clear_color(float r, float g, float b, float a);

    /// <summary>Gives a camera part of the window to draw into, or all of it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_viewport(
        ulong camera, uint x, uint y, uint width, uint height);

    /// <summary>Projects a world point onto a camera's viewport, in logical pixels.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_world_to_viewport(
        ulong camera, float x, float y, float z, float* point);

    /// <summary>Turns a viewport point into a ray, its origin then its direction.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_viewport_to_world(
        ulong camera, float x, float y, float* ray);

    /// <summary>Points a camera at an image, or back at the window.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_camera_target(ulong entity, int image, int layer);

    /// <summary>Gives a camera Bevy's perspective with entries of its matrix written over it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_adjusted_perspective(ulong camera, float fov, float near, float far, float* entries, int count);

    /// <summary>Points a camera at a window a game spawned.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_camera_window(ulong camera, ulong window);

    /// <summary>Puts an entity on a set of render layers.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_layers(ulong entity, uint mask);

    // -- Camera controllers

    /// <summary>Writes Bevy's default free camera settings.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_free_camera_default(NativeFreeCamera* settings);

    /// <summary>Puts the free camera on a camera, or changes its settings.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_free_camera_set(ulong camera, NativeFreeCamera* settings);

    /// <summary>Reads the free camera's settings off a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_free_camera_get(ulong camera, NativeFreeCamera* settings);

    /// <summary>Takes the free camera off a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_free_camera_remove(ulong camera);

    /// <summary>Reads what the free camera holds as it runs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_free_camera_state(ulong camera, NativeFreeCameraState* state);

    /// <summary>Writes what the free camera holds as it runs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_free_camera_set_state(ulong camera, NativeFreeCameraState* state);

    /// <summary>Writes Bevy's description of the free camera's controls.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_free_camera_text(ulong camera, byte* buffer, int capacity);

    /// <summary>Writes Bevy's default pan camera settings.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pan_camera_default(NativePanCamera* settings);

    /// <summary>Puts the pan camera on a camera, or changes its settings.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pan_camera_set(ulong camera, NativePanCamera* settings);

    /// <summary>Reads the pan camera's settings off a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pan_camera_get(ulong camera, NativePanCamera* settings);

    /// <summary>Takes the pan camera off a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pan_camera_remove(ulong camera);

    /// <summary>Writes Bevy's description of the pan camera's controls.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pan_camera_text(ulong camera, byte* buffer, int capacity);

    /// <summary>Moves a pan-orbit camera to another projection over Bevy's dolly zoom.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pan_orbit_dolly_zoom(ulong camera, int orthographic);

    /// <summary>Turns a pan-orbit camera to face a direction over Bevy's look-to animation.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pan_orbit_look_to(ulong camera, float x, float y, float z);

    // -- Lights and shadows

    /// <summary>Sets the ambient light, everywhere or on one camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_ambient_light(ulong camera, float r, float g, float b, float brightness);

    /// <summary>Draws contact shadows on a camera, or with zero steps stops.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_contact_shadows(ulong camera, uint steps, float thickness, float length);

    /// <summary>Spawns a light and returns its entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_render_spawn_light(NativeLightConfig* config);

    /// <summary>Writes the point lights the render world held and drew in its last frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_point_lights_drawn(uint* counts);

    /// <summary>Sets how a directional light divides its shadows across the distance.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_shadow_cascades(
        ulong light, int cascades, float minimum, float maximum, float firstBound, float overlap);

    /// <summary>Shapes a spot light's beam with a picture.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_light_cookie(ulong light, int image);

    /// <summary>Softens a light's shadows as a light of a size, or hardens them with zero.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_soft_shadows(ulong light, float size);

    /// <summary>Sets how a camera filters the shadow maps it reads.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_shadow_filtering(ulong camera, int method);

    /// <summary>Sets the shadow map size for each kind of light.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_shadow_maps(uint directional, uint point);

    // -- The look

    /// <summary>Turns Bevy's screen-space ambient occlusion on or off for a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_ambient_occlusion(ulong camera, int quality, float thickness);

    /// <summary>Draws Bevy's own materials deferred or forward.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_deferred(int on);

    /// <summary>Turns screen-space reflections on or off for a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_screen_space_reflections(ulong camera, NativeReflectionConfig* config);

    /// <summary>Sets what a camera does to the picture after the scene is drawn.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_post(ulong entity, NativePostConfig* config);

    /// <summary>Draws an entity's mesh as its edges, or stops.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_wireframe(
        ulong entity, int on, float red, float green, float blue, float alpha);

    /// <summary>Sets the lens effects a camera draws through.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_effects(ulong entity, NativeEffectsConfig* config);

    /// <summary>Grades the picture a camera drew, after tonemapping.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_grading(ulong camera, NativeGradingConfig* config);

    /// <summary>Sets the exposure a camera meters the scene at, in EV-100.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_exposure(ulong camera, float ev100);

    /// <summary>Sets a camera's exposure from the lens it stands in for.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_lens_exposure(
        ulong camera, float aperture, float shutter, float sensitivity);

    /// <summary>Turns order-independent transparency on or off for a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_sorted_transparency(
        ulong camera, int on, uint layers, float average, float threshold);

    // -- The environment

    /// <summary>Draws the sky the air scatters, seen from a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_atmosphere(
        ulong camera, NativeAtmosphereConfig* config);

    /// <summary>Gives a directional light the disk of the sun an atmosphere draws for it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_sun_disk(ulong light, float angularSize, float intensity);

    /// <summary>Lights the scene from a cubemap, filtered on the GPU.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_image_lighting(
        ulong camera, int image, float intensity, float* rotation);

    /// <summary>Lights the scene from the sky the camera is scattering.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_sky_lighting(
        ulong camera, int on, float intensity, uint size);

    /// <summary>Draws a cubemap behind everything a camera draws.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_skybox(
        ulong camera, int image, float brightness, float* rotation);

    /// <summary>Lights the scene from a pair of cubemaps somebody baked.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_environment_map(
        ulong camera, int diffuse, int specular, float intensity, float* rotation);

    /// <summary>Makes an entity a reflection probe lit by a pair of baked cubemaps.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_reflection_probe(
        ulong entity, int diffuse, int specular, float intensity, float* falloff);

    /// <summary>Makes an entity an irradiance volume lit from a 3D image.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_irradiance_volume(
        ulong entity, int voxels, float intensity, float* falloff);

    /// <summary>Makes an entity a reflection probe that renders what is around it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_probe_capture(
        ulong entity, uint size, float intensity, float* falloff, int live, float near);

    /// <summary>Captures a probe that is not live again.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_recapture_probe(ulong entity);

    // -- Ray-traced lighting and meshlets

    /// <summary>Whether Bevy's ray-traced lighting is running in this app.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_ray_tracing_active();

    /// <summary>Lights a camera with Bevy's ray tracing, or the usual way again.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_ray_traced_lighting(ulong camera, int on);

    /// <summary>Makes an entity's mesh take part in ray tracing.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_ray_traced(ulong entity, int mesh);

    /// <summary>Turns the bridge's denoiser over ray-traced lighting on or off, with what it filters by.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_ray_traced_denoiser(int on, NativeDenoiserSettings* settings);

    /// <summary>Whether the bridge's denoiser is in this app and on.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_ray_traced_denoiser_active();

    /// <summary>Whether Bevy's meshlets are running in this app.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_meshlets_active();
}
