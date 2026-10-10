using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bevy.Interop;

internal static unsafe partial class Native
{
    // -- HTML and CSS UI (editor builds only)

    /// <summary>Reports whether the HTML and CSS surface is compiled in.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_has_editor();

    /// <summary>Pauses or resumes the game's clock and sets its speed, a negative speed leaving it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_time_set_virtual(int paused, float speed);

    /// <summary>Writes whether the game's clock is paused and how fast it runs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_time_virtual(int* paused, float* speed);

    /// <summary>Sets the seconds each frame advances the clock by, zero for the machine's clock.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_time_set_frame_seconds(double seconds);

    /// <summary>Writes the seconds each frame advances the clock by, zero for the machine's clock.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_time_frame_seconds(double* seconds);

    /// <summary>Writes how far the fixed clock has run past its last step, as a share of one.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_time_fixed_overstep(float* fraction);

    /// <summary>Reports whether a game's assets are compiled in.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_has_embedded_assets();

    /// <summary>Reports whether the running app installed the interface.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_has_interface();

    /// <summary>Copies the scene entities clicked since the last call.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pick_events(ulong* buffer, int capacity);

    /// <summary>Casts a ray at the scene's meshes and writes the nearest it meets.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pick_ray(float* origin, float* direction, ulong* entity, float* point, float* normal, float* uv);

    /// <summary>Spawns a pointer of the game's own and writes its number.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pointer_spawn(ulong* number);

    /// <summary>Gives an entity the input focus, as Bevy's <c>InputFocus::set</c>.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_focus(ulong entity, int cause);

    /// <summary>Writes where the focus would move along the tab order, answering one where there is somewhere.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_navigate(int action, ulong* next);

    /// <summary>Puts a line under or through a run of text, or takes it off, each 1, 0 or -1 to leave it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_text_lines(ulong entity, int underline, int strikethrough);

    /// <summary>Sets a run's OpenType features (kind 0) or variable axes (kind 1) from tagged values.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ui_font_tags(ulong entity, int kind, NativeFontTag* tags, int count);

    /// <summary>Moves, presses or releases a pointer of the game's own on an image.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pointer_input(ulong number, int image, float x, float y, int action, int button);

    /// <summary>Locks a pointer to an entity, as Bevy's <c>PointerCaptureMap::capture</c>, with the hit it reports.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pointer_capture(
        int kind, ulong number, ulong entity, ulong camera, float depth, float* position, float* normal);

    /// <summary>Releases what a pointer was locked to, as Bevy's <c>PointerCaptureMap::release</c>.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_pointer_release_capture(int kind, ulong number);

    /// <summary>Projects a world point onto a camera's viewport, in logical pixels.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_world_to_viewport(
        ulong camera, float x, float y, float z, float* point);

    /// <summary>Turns a viewport point into a ray: origin then direction.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_viewport_to_world(
        ulong camera, float x, float y, float* ray);

    /// <summary>Writes an entity's world-space bounds: min x, y, z then max x, y, z.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_bounds(ulong entity, float* bounds);

    /// <summary>Draws an entity's mesh as its edges, or stops.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_wireframe(
        ulong entity, int on, float red, float green, float blue, float alpha);

    /// <summary>Sets the lens effects a camera draws through.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_effects(ulong entity, NativeEffectsConfig* config);

    /// <summary>Draws the sky the air scatters, seen from a camera.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_atmosphere(
        ulong camera, NativeAtmosphereConfig* config);

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

    /// <summary>Grades the picture a camera drew, after tonemapping.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_grading(ulong camera, NativeGradingConfig* config);

    /// <summary>Sets the exposure a camera meters the scene at, in EV-100.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_exposure(ulong camera, float ev100);

    /// <summary>Lights the scene from a pair of cubemaps somebody baked.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_environment_map(
        ulong camera, int diffuse, int specular, float intensity, float* rotation);

    /// <summary>Copies out the names a watch on a camera would find, one a line.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_drawn_view_image_names(ulong camera, byte* output, int capacity);

    /// <summary>Copies the last frame's render timings out as text.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_timings(byte* output, int capacity);

    /// <summary>Writes how many pipelines the renderer is still compiling.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_pipelines_waiting(uint* waiting);

    /// <summary>Makes an empty geometry pool, writing its three buffer keys.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_geometry_pool_create(int* keys);

    /// <summary>Adds a mesh to a geometry pool, answering its number there.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_geometry_pool_add(int pool, int mesh);

    /// <summary>Whether the device can build ray scenes and trace rays through them.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_ray_queries_supported();

    /// <summary>Whether this device runs mesh shaders, one or zero.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_mesh_shaders_supported();

    /// <summary>Makes a ray scene over a geometry pool, answering its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_ray_scene_create(int pool, int capacity);

    /// <summary>Puts an entity made of a pool mesh in a slot of a ray scene, or empties the slot.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_ray_scene_set(int scene, int slot, ulong entity, int mesh);

    /// <summary>Builds a ray scene's pool mesh again from what the pool holds, or all with a negative mesh.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_ray_scene_rebuild(int scene, int mesh);

    /// <summary>Puts a ray scene under a name, or takes it off with a key of zero.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_set_ray_scene(int kind, long id, byte* name, int scene);

    /// <summary>Makes a buffer the engine fills with the materials of entities put in its slots.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_shader_material_buffer_create(int capacity);

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

    /// <summary>Whether Bevy's meshlets are running in this app.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_meshlets_active();

    /// <summary>Whether the weather is running in this app.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_weather_active();

    /// <summary>Sets the weather to a preset, eased into or at once.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_weather_set_preset(int preset, int immediately);

    /// <summary>Makes a material that colors each cluster of a meshlet mesh.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_cluster_material_create();

    /// <summary>Starts making a meshlet mesh from a mesh, answering its key at once.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_create_meshlet_mesh(int mesh, uint quantization, string? saveTo);

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

    /// <summary>Writes what a camera drew to a PNG file.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_screenshot(string path, int target);

    /// <summary>Copies a mesh's positions and normals out, or answers how many vertices there are.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_mesh_normals(int mesh, float* positions, float* normals, int capacity, int* count);

    /// <summary>Copies a mesh's positions and triangle indices out, or answers their counts.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_mesh_triangles(
        int mesh, float* positions, int positionCapacity, uint* indices, int indexCapacity, int* counts);

    /// <summary>Creates an empty image a camera can draw into.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_create_target(uint width, uint height, int format, uint layers);

    /// <summary>Asks for a picture to be read back into memory.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_capture(int target);

    /// <summary>Reads a capture, and forgets it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_capture_read(
        int id, uint* width, uint* height, byte* buffer, int capacity);

    /// <summary>The entities whose component changed after a tick, and the tick it was read at.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_changed_since(int component, uint since, ulong* entities, int capacity, uint* now);

    /// <summary>For each of a list of entities, whether it carries a component, or changed it after a tick.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_ecs_has_many(int component, uint since, ulong* entities, int count, byte* answers);

    /// <summary>Turns the bridge's frame counting on or off, clearing what was counted.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_profile_enable(int on);

    /// <summary>The render schedule's phases' times since the last read, and their names a line each.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_profile_phases(ulong* nanos, int count, byte* names, int capacity);

    /// <summary>Does nothing, through the guard every entry point takes, for timing a crossing alone.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_profile_noop();

    /// <summary>Reads what the bridge counted since the last read, and starts the count over.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_profile_take(NativeProfile* profile);

    /// <summary>Names a folder as an asset source of its own for the next app, or forgets them all with a null root.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_source_add(string? name, string? root);

    /// <summary>The names of a scene's clips, a line each.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_clips(ulong root, byte* buffer, int capacity);

    /// <summary>Plays a scene's clip by its number.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_play(ulong root, int clip, int repeat, float speed, float blend);

    /// <summary>Stops every clip on a scene.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_stop(ulong root);

    /// <summary>Makes an empty clip, answering its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_clip_create();

    /// <summary>Writes the target at the end of a path of names, given one a line.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_target(string names, ulong* high, ulong* low);

    /// <summary>Adds a described curve to a clip, aimed at a target.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_clip_add_curve(int clip, ulong high, ulong low, NativeAnimationCurve* curve, float* times, float* values, int valueCount);

    /// <summary>Makes a graph of one clip, writing its key and the clip's node.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_graph_from_clip(int clip, int* graph, uint* node);

    /// <summary>Makes an entity a player of a graph, playing a node.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_play_graph(ulong entity, int graph, uint node, int repeat);

    /// <summary>Makes an entity a target of a clip's curves, moved by a player.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_animate(ulong entity, ulong high, ulong low, ulong player);

    /// <summary>Has the clips' events call back into C#, through the handle given.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_observe_clip_events(IntPtr app, delegate* unmanaged[Cdecl]<uint, ulong, IntPtr, void> callback, IntPtr user);

    /// <summary>Sets how long a clip lasts.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_clip_set_duration(int clip, float seconds);

    /// <summary>Places an event on a clip, at its player or at a target.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_clip_add_event(int clip, float time, uint number, int targeted, ulong high, ulong low);

    /// <summary>Makes an empty graph, writing its key and its root's node.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_graph_create(int* graph, uint* root);

    /// <summary>Adds a blend to a graph, writing its node.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_graph_add_blend(int graph, float weight, uint parent, int additive, uint* node);

    /// <summary>Writes the target an entity is aimed at by, where it has one.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_target_of(ulong entity, ulong* high, ulong* low);

    /// <summary>Adds a clip to a graph, with the mask groups it leaves out, writing its node.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_graph_add_clip(int graph, int clip, ulong mask, float weight, uint parent, uint* node);

    /// <summary>Puts a target into one of a graph's mask groups.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_graph_add_to_mask_group(int graph, ulong high, ulong low, uint group);

    /// <summary>Sets the mask groups a graph's node leaves out.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_graph_set_mask(int graph, uint node, ulong mask);

    /// <summary>Gives an entity a graph to play from.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_set_graph(ulong entity, int graph);

    /// <summary>Starts a node of an entity's graph playing beside the others.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_play_node(ulong entity, uint node, int repeat);

    /// <summary>Sets the weight a playing node is mixed in at.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_node_weight(ulong entity, uint node, float weight);

    /// <summary>Copies Bevy's input messages since the last call, answering how many.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_input_messages(NativeInputMessage* messages, int capacity);

    /// <summary>Moves the input focus by direction, writing where it moved, answering 1 where it did.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_nav_move(int direction, ulong* moved);

    /// <summary>Draws or blocks an edge of the navigation map, one way or both.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_nav_edge(ulong from, ulong to, int direction, int kind);

    /// <summary>Draws edges between nodes in their order, looping where asked.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_nav_edges(ulong* entities, int count, int direction, int looping);

    /// <summary>Takes a node's edges out of the navigation map, or all of them for none.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_nav_forget(ulong entity);

    /// <summary>Gives a mesh the joints each vertex follows and their weights, four of each a vertex.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_mesh_set_joints(int mesh, ushort* joints, float* weights, int count);

    /// <summary>Makes a skin's inverse bindposes from transforms of ten floats each, writing its key.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_skin_create(float* transforms, int count, int* key);

    /// <summary>Skins an entity's mesh with a skin and the joint entities that move it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_skin_set(ulong entity, int skin, ulong* joints, int count);

    /// <summary>Changes how many times the playing clip plays.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_set_repeat(ulong root, uint times);

    /// <summary>Holds or lets go of every clip on a scene.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_pause(ulong root, int paused);

    /// <summary>Moves or changes the speed of the clip a scene plays.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_adjust(ulong root, float seconds, float speed);

    /// <summary>What a scene plays.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_state(ulong root, NativeAnimationState* state);

    /// <summary>Takes the clips that reached their end.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_animation_finished(ulong* roots, int* clips, int capacity);

    /// <summary>Reads a capture in the format it arrived in, and forgets it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_capture_read_raw(
        int id, uint* width, uint* height, int* format, byte* buffer, int capacity);

    /// <summary>Forgets a capture that will not be read.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_capture_release(int id);

    /// <summary>Points a camera at an image, or back at the window.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_camera_target(ulong entity, int image, int layer);

    /// <summary>Points a camera at a window a game spawned.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_camera_window(ulong camera, ulong window);

    /// <summary>Writes what a window a game spawned shows to a PNG file.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_window_screenshot(string path, ulong window);

    /// <summary>Sets the shadow map size for each kind of light.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_shadow_maps(uint directional, uint point);

    /// <summary>Puts an entity on a set of render layers.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_layers(ulong entity, uint mask);

    /// <summary>Attaches a sprite to an entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_set_sprite(ulong entity, NativeSpriteConfig* config);

    /// <summary>Moves each sprite or sprite mesh to the atlas frame beside it, answering how many it moved.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_sprite_frames(ulong* entities, uint* frames, int count);
}
