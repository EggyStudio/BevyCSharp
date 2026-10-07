using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Render
{
    /// <summary>
    /// Writes what is being drawn to a PNG file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The window, or the image an offscreen run draws into instead. Which one is not the caller's
    /// to choose, because a run has one thing it is drawing and a capture is a picture of that. A
    /// headless run draws nothing and captures nothing.
    /// </para>
    /// <para>
    /// The capture happens on the frame after this call, because the picture has to come back off
    /// the GPU, and the file appears once it has. Watch for the file rather than assuming it is
    /// there when this returns.
    /// </para>
    /// <para>
    /// What this is for is checking the picture without a person looking at it. A test can assert
    /// that a setting was accepted; only the picture says whether anything was drawn.
    /// </para>
    /// </remarks>
    /// <param name="path">Where to write the PNG. Relative paths are resolved by the process.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    /// <seealso cref="Config.Offscreen"/>
    public static void Screenshot(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        Native.Check(
            Native.bcs_render_screenshot(path, AssetHandle.None.Key),
            $"capturing what is being drawn to {path}");
    }

    /// <summary>
    /// Writes what a camera drew into an image to a PNG file.
    /// </summary>
    /// <remarks>
    /// The other end of <see cref="CreateTarget"/>. A camera pointed at a texture draws a picture
    /// nothing on screen shows, and this is how it is read back: a minimap, a portal, or a second
    /// viewport, checked without a person looking at the window it is not in.
    /// </remarks>
    /// <param name="path">Where to write the PNG. Relative paths are resolved by the process.</param>
    /// <param name="target">The image to capture, from <see cref="CreateTarget"/>.</param>
    /// <exception cref="BevyNativeException">
    /// This build has no renderer, or the handle names no image.
    /// </exception>
    public static void Screenshot(string path, AssetHandle target)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        Native.Check(
            Native.bcs_render_screenshot(path, target.Key), $"capturing {target} to {path}");
    }

    /// <summary>
    /// Writes what a window the game spawned shows to a PNG file.
    /// </summary>
    /// <remarks>
    /// The window's own picture, or in an offscreen run the image standing for it, which it is
    /// given the frame after it is spawned (<see cref="SetCameraTarget(Entity, Entity)"/>). The
    /// file appears a frame or two later, as with any capture.
    /// </remarks>
    /// <param name="path">Where to write the PNG. Relative paths are resolved by the process.</param>
    /// <param name="window">An entity carrying Bevy's <c>Window</c>.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is not a window, an offscreen run has not given it its image yet, or there is no
    /// renderer.
    /// </exception>
    public static void Screenshot(string path, Entity window)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        Native.Check(Native.bcs_window_screenshot(path, window.Bits), $"capturing window {window} to {path}");
    }

    /// <summary>
    /// Lights the scene from a cubemap, filtered on the GPU.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The other way to light a scene from its surroundings. <see cref="SetSkyLighting"/> derives
    /// the map from the atmosphere, which covers an outdoor scene; this takes a picture, for an
    /// indoor one or a scene lit from a photograph.
    /// </para>
    /// <para>
    /// One cubemap rather than the two a baked environment map carries, because Bevy filters it
    /// into its diffuse and specular halves itself. It is the same column of six faces
    /// <see cref="SetSkybox"/> takes, so the same file can be seen behind the scene and be the
    /// light in it.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera whose view is lit.</param>
    /// <param name="cubemap">
    /// Six square faces stacked vertically, or <see cref="AssetHandle.None"/> to take the lighting
    /// off.
    /// </param>
    /// <param name="intensity">How bright the lighting is.</param>
    /// <param name="rotation">Which way the map is turned, for one authored with another axis up.</param>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetImageLighting(
        Entity camera,
        AssetHandle cubemap,
        float intensity = 1000f,
        Quat? rotation = null)
    {
        if (rotation is not { } turn)
        {
            Native.Check(
                Native.bcs_render_set_image_lighting(camera.Bits, cubemap.Key, intensity, null),
                $"lighting {camera} from an image");

            return;
        }

        var parts = stackalloc float[4] { turn.X, turn.Y, turn.Z, turn.W };

        Native.Check(
            Native.bcs_render_set_image_lighting(camera.Bits, cubemap.Key, intensity, parts),
            $"lighting {camera} from an image");
    }

    /// <summary>
    /// Lights the scene from a pair of cubemaps somebody baked earlier.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The other end of <see cref="SetImageLighting"/>, which filters one cubemap on the GPU every
    /// time the app starts. This takes the two maps a tool produced, which costs nothing at startup
    /// and suits a shipped game, especially for an environment too large to filter again.
    /// </para>
    /// <para>
    /// <paramref name="diffuse"/> is the blurred map a rough surface reflects and
    /// <paramref name="specular"/> is the sharp one a polished surface reflects. Both are a column
    /// of six square faces like a skybox, and both are turned into cubes before the light is
    /// applied, so a handle asked for in the same frame the camera is spawned works.
    /// </para>
    /// <para>
    /// Passing <see cref="AssetHandle.None"/> for either takes the lighting off, since a baked map
    /// is the pair and half of one is not a weaker version of it.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera whose view is lit.</param>
    /// <param name="diffuse">The blurred map.</param>
    /// <param name="specular">The sharp one.</param>
    /// <param name="intensity">How bright, in candelas per square meter.</param>
    /// <param name="rotation">Which way the maps are turned, or null for not at all.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is not a camera, or a handle names no image.
    /// </exception>
    public static void SetEnvironmentMap(
        Entity camera,
        AssetHandle diffuse,
        AssetHandle specular,
        float intensity = 1000f,
        Quat? rotation = null)
    {
        if (rotation is not { } turn)
        {
            Native.Check(
                Native.bcs_render_set_environment_map(
                    camera.Bits, diffuse.Key, specular.Key, intensity, null),
                $"lighting {camera} from a baked environment map");

            return;
        }

        var parts = stackalloc float[4] { turn.X, turn.Y, turn.Z, turn.W };

        Native.Check(
            Native.bcs_render_set_environment_map(
                camera.Bits, diffuse.Key, specular.Key, intensity, parts),
            $"lighting {camera} from a baked environment map");
    }

    /// <summary>
    /// Makes an entity a reflection probe: a box inside which surfaces reflect a pair of baked
    /// cubemaps rather than the camera's environment. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The box is the entity's <see cref="Transform"/>, a unit cube before its scale, so a probe
    /// covering a room is placed at the room's center and scaled to its size. The pair is the one
    /// <see cref="SetEnvironmentMap"/> takes, captured from inside the room, and a surface inside
    /// the box picks it in place of what the camera is lit by, so a room stops reflecting the sky
    /// outside it. Reflections are corrected for where in the box the surface is, so a mirror near
    /// a wall shows the wall close.
    /// </para>
    /// <para>
    /// <paramref name="falloff"/> is how much of the box, on each axis from nothing to all of it,
    /// the probe's influence fades across. With none the box has a hard edge; with some, a surface
    /// moving between two overlapping probes blends from one to the other. Bevy uses the nearest
    /// few probes to each view, which is plenty for a building and why a city is split into
    /// probes a street long.
    /// </para>
    /// <para>
    /// <see cref="AssetHandle.None"/> for either map takes the reflection off. A camera is refused,
    /// since a camera is lit by <see cref="SetEnvironmentMap"/>.
    /// </para>
    /// </remarks>
    /// <param name="probe">The entity whose transform is the box.</param>
    /// <param name="diffuse">The blurred map, a column of six square faces.</param>
    /// <param name="specular">The sharp map, the same shape.</param>
    /// <param name="intensity">How bright, in candelas per square meter.</param>
    /// <param name="falloff">The fraction of the box faded across on each axis, or null for none.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is a camera or is gone, or a handle names no image.
    /// </exception>
    public static void SetReflectionProbe(
        Entity probe,
        AssetHandle diffuse,
        AssetHandle specular,
        float intensity = 1000f,
        Vec3? falloff = null)
    {
        if (falloff is not { } fade)
        {
            Native.Check(
                Native.bcs_render_set_reflection_probe(probe.Bits, diffuse.Key, specular.Key, intensity, null),
                $"making {probe} a reflection probe");

            return;
        }

        var parts = stackalloc float[3] { fade.X, fade.Y, fade.Z };

        Native.Check(
            Native.bcs_render_set_reflection_probe(probe.Bits, diffuse.Key, specular.Key, intensity, parts),
            $"making {probe} a reflection probe");
    }

    /// <summary>
    /// Makes an entity an irradiance volume: a box inside which surfaces take their diffuse
    /// indirect light from a grid of points held in a 3D image. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each point of the grid holds the light a surface facing each of the six axis directions
    /// receives there, and a surface between points blends the nearest ones by its normal. So a
    /// wall beside a red carpet picks up red low down and less higher up, which an environment map
    /// the same everywhere cannot do. Bevy ranks it above a reflection probe and the camera's
    /// environment for diffuse light, and ambient light is added on top.
    /// </para>
    /// <para>
    /// The image is how a global illumination technique reaches Bevy's materials. It is an
    /// ordinary 3D image, so one made with <see cref="Shaders.CreateImage(uint, uint, ShaderImageFormat, uint, uint)"/>
    /// and written by a compute shader every frame lights everything drawn with a
    /// material from <see cref="CreateMaterial(MaterialSettings)"/> by whatever the shader worked out, with no change to how
    /// those materials are drawn. A grid <c>x</c> by <c>y</c> by <c>z</c> points is an image
    /// <c>x</c> wide, <c>2y</c> high and <c>3z</c> deep, and <c>bcs_scene</c>'s
    /// <c>irradiance_texel</c> and <c>irradiance_point_in_box</c> address it by point and direction,
    /// so a shader never deals with the packing. It is sampled filtered, so it is made
    /// <see cref="ShaderImageFormat.Rgba16Float"/>. A baked one can come from a file as well.
    /// </para>
    /// <para>
    /// The box is the entity's <see cref="Transform"/>, and <paramref name="falloff"/> is as for
    /// <see cref="SetReflectionProbe"/>. <see cref="AssetHandle.None"/> takes the volume off.
    /// </para>
    /// </remarks>
    /// <param name="probe">The entity whose transform is the box.</param>
    /// <param name="voxels">The 3D image.</param>
    /// <param name="intensity">
    /// What the image's values are multiplied by, in candelas per square meter, so an image holding
    /// light in its own units is brought into the scene's.
    /// </param>
    /// <param name="falloff">The fraction of the box faded across on each axis, or null for none.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is a camera or is gone, the handle names no image, or the image is not 3D.
    /// </exception>
    public static void SetIrradianceVolume(
        Entity probe,
        AssetHandle voxels,
        float intensity = 1000f,
        Vec3? falloff = null)
    {
        if (falloff is not { } fade)
        {
            Native.Check(
                Native.bcs_render_set_irradiance_volume(probe.Bits, voxels.Key, intensity, null),
                $"making {probe} an irradiance volume");

            return;
        }

        var parts = stackalloc float[3] { fade.X, fade.Y, fade.Z };

        Native.Check(
            Native.bcs_render_set_irradiance_volume(probe.Bits, voxels.Key, intensity, parts),
            $"making {probe} an irradiance volume");
    }

    /// <summary>
    /// Makes an entity a reflection probe that renders what is around it, or with
    /// <see langword="null"/> stops. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Six cameras at the probe's center draw the scene into the faces of a cube, and Bevy filters
    /// the cube into the probe's light on the GPU, so the room is reflected as it is rather than as
    /// somebody baked it. The box is the entity's <see cref="Transform"/> as for
    /// <see cref="SetReflectionProbe"/>, and the cameras follow the probe's center as it moves.
    /// </para>
    /// <para>
    /// A live probe draws the scene six more times every frame, which a reflection of something
    /// moving needs and a large one cannot afford. One that is not live captures once, over the
    /// first few frames after nothing is left compiling, so asking at startup captures the scene as
    /// it will be drawn rather than while its materials are still missing.
    /// <see cref="RecaptureProbe"/> captures again when the room changes, or once a texture that
    /// loaded late has arrived. Either way the light is a frame behind what the cameras drew.
    /// </para>
    /// <para>
    /// The cameras see the probe's own light, so each capture reflects the one before, and light
    /// bounces a little further every frame. That settles as long as the intensity only undoes the
    /// exposure the faces were drawn at, as the default does; much more makes the room brighter
    /// each frame. A camera is refused, as for the other probes.
    /// </para>
    /// </remarks>
    /// <param name="probe">The entity whose transform is the box.</param>
    /// <param name="settings">How to capture, or null to stop.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is a camera or is gone, or the size is not a power of two.
    /// </exception>
    public static void SetProbeCapture(Entity probe, ProbeCaptureSettings? settings)
    {
        if (settings is null)
        {
            Native.Check(
                Native.bcs_render_set_probe_capture(probe.Bits, 0, 0f, null, 0, 0f),
                $"stopping the capture of {probe}");

            return;
        }

        var parts = stackalloc float[3] { settings.Falloff.X, settings.Falloff.Y, settings.Falloff.Z };

        Native.Check(
            Native.bcs_render_set_probe_capture(
                probe.Bits,
                settings.Size,
                settings.Intensity,
                parts,
                settings.Live ? 1 : 0,
                settings.Near),
            $"capturing {probe} as a reflection probe");
    }

    /// <summary>
    /// Captures a probe that is not live again, after what is around it has changed. Only valid
    /// inside a system.
    /// </summary>
    /// <exception cref="BevyNativeException">The entity has no capture.</exception>
    public static void RecaptureProbe(Entity probe) =>
        Native.Check(Native.bcs_render_recapture_probe(probe.Bits), $"capturing {probe} again");

    /// <summary>
    /// Lights the scene from the sky this camera is already scattering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What makes a surface pick up the color of what is around it rather than only what a lamp
    /// points at it. The environment map is derived from the atmosphere each frame, so it follows
    /// the sun, and a scene lit this way goes warm at dusk without anything being animated.
    /// </para>
    /// <para>
    /// Needs <see cref="SetAtmosphere"/> on the same camera, because what it filters is the sky
    /// being drawn. A camera with no atmosphere has nothing to derive a map from.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera whose view is lit.</param>
    /// <param name="intensity">How bright the lighting is. One matches the sky's own.</param>
    /// <param name="size">
    /// The square resolution of the cubemap it generates, which has to be a power of two.
    /// </param>
    /// <exception cref="BevyNativeException">
    /// The entity is not a camera, or the size is not a power of two.
    /// </exception>
    public static void SetSkyLighting(Entity camera, float intensity = 1f, uint size = 512) =>
        Native.Check(
            Native.bcs_render_set_sky_lighting(camera.Bits, 1, intensity, size),
            $"lighting {camera} from the sky");

    /// <summary>Stops lighting the scene from the sky.</summary>
    public static void ClearSkyLighting(Entity camera) => Native.Check(
        Native.bcs_render_set_sky_lighting(camera.Bits, 0, 0f, 0),
        $"taking the sky lighting off {camera}");

    /// <summary>
    /// Draws a cubemap behind everything a camera draws.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The image is a column of six square faces, which is the layout every cubemap texture ships
    /// in, and it is turned into a cube once it has loaded. An image of any other shape draws
    /// nothing and says so on the log rather than failing here, because whether the file is the
    /// right shape is not known until it has been decoded.
    /// </para>
    /// <para>
    /// <paramref name="brightness"/> scales the samples into the units the rest of the scene is lit
    /// in, which are candelas per square meter, so the useful numbers are in the hundreds or
    /// thousands. A brightness of one is a night sky and comes out black, which reads as a skybox
    /// that failed rather than one that is very dark. The skybox is seen behind the scene and does
    /// not light it; <see cref="SetImageLighting"/> lights a scene from the same file.
    /// </para>
    /// <para>
    /// Pass <see cref="AssetHandle.None"/> to take the skybox off.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera to draw it behind.</param>
    /// <param name="cubemap">Six square faces stacked vertically, from the asset server.</param>
    /// <param name="brightness">How much to scale the samples by.</param>
    /// <param name="rotation">Which way the cube is turned, for a cubemap authored Z-up.</param>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetSkybox(
        Entity camera,
        AssetHandle cubemap,
        float brightness = 1000f,
        Quat? rotation = null)
    {
        if (rotation is not { } turn)
        {
            Native.Check(
                Native.bcs_render_set_skybox(camera.Bits, cubemap.Key, brightness, null),
                $"putting a skybox on {camera}");

            return;
        }

        var parts = stackalloc float[4] { turn.X, turn.Y, turn.Z, turn.W };

        Native.Check(
            Native.bcs_render_set_skybox(camera.Bits, cubemap.Key, brightness, parts),
            $"putting a skybox on {camera}");
    }

    /// <summary>
    /// Grades the picture a camera drew, after tonemapping.
    /// </summary>
    /// <remarks>
    /// What a look is made of once the scene is drawn. Passing <see langword="null"/> puts the
    /// camera back to the engine's own grading, which changes nothing about the picture.
    /// </remarks>
    /// <param name="camera">The camera to grade.</param>
    /// <param name="settings">The grade, or null for none.</param>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetColorGrading(Entity camera, GradingSettings? settings)
    {
        if (settings is null)
        {
            Native.Check(
                Native.bcs_render_set_grading(camera.Bits, null),
                $"clearing the color grade on {camera}");

            return;
        }

        var native = settings.ToNative();

        Native.Check(
            Native.bcs_render_set_grading(camera.Bits, &native),
            $"grading {camera}");
    }

    /// <summary>
    /// Sets the exposure a camera meters the scene at, in EV-100.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The number a photographer would set. Sunlight is around 15, an overcast day around 12 and
    /// an interior around 7, so a scene lit in physical units and metered wrongly comes out far too
    /// bright or far too dark rather than subtly off.
    /// </para>
    /// <para>
    /// This is the base an auto exposure pass corrects rather than an alternative to it. With
    /// <see cref="EffectSettings.AutoExposure"/> on, what is set here is where it starts from.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera to meter.</param>
    /// <param name="ev100">The exposure value at ISO 100.</param>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetExposure(Entity camera, float ev100) => Native.Check(
        Native.bcs_render_set_exposure(camera.Bits, ev100), $"metering {camera}");

    /// <summary>
    /// Sets a camera's exposure from the lens it stands in for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same three numbers a photographer sets, and the same ones a real lens is described by,
    /// so a camera can be written down once as a lens and metered from that rather than from an
    /// exposure value worked out by hand. This and <see cref="SetExposure"/> set the same thing.
    /// </para>
    /// <para>
    /// Each number keeps Bevy's own when it is left at zero, which is f/1 at a hundred and
    /// twenty-fifth of a second and ISO 100.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera to meter.</param>
    /// <param name="aperture">The f-stop. A larger number lets less light in.</param>
    /// <param name="shutter">How long the shutter is open, in seconds.</param>
    /// <param name="sensitivity">The ISO.</param>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetLensExposure(
        Entity camera,
        float aperture = 0f,
        float shutter = 0f,
        float sensitivity = 0f) =>
        Native.Check(
            Native.bcs_render_set_lens_exposure(camera.Bits, aperture, shutter, sensitivity),
            $"metering {camera} from a lens");

    /// <summary>Meters a camera from a lens written down once (<see cref="PhysicalLens"/>).</summary>
    /// <remarks>
    /// Its aperture, shutter and sensitivity, as <see cref="SetLensExposure"/> takes them. Pass the
    /// same lens to <see cref="EffectSettings.Through"/> for a depth of field through it.
    /// </remarks>
    /// <param name="camera">The camera to meter.</param>
    /// <param name="lens">The lens it stands in for.</param>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetLens(Entity camera, PhysicalLens lens)
    {
        ArgumentNullException.ThrowIfNull(lens);
        SetLensExposure(camera, lens.Aperture, lens.Shutter, lens.Sensitivity);
    }

    /// <summary>
    /// Sorts transparent fragments rather than whole objects, for one camera.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What fixes transparent surfaces drawn in the wrong order. Ordinary alpha blending sorts
    /// objects by distance, so two panes of glass crossing each other, or one mesh whose own faces
    /// overlap, come out right from some angles and wrong from others, and no amount of reordering
    /// the scene fixes both. This sorts each pixel's fragments instead.
    /// </para>
    /// <para>
    /// It costs a buffer the size of the screen times <paramref name="layers"/>, which is why it
    /// is per camera and off unless asked for. Every number keeps Bevy's own when it is left at
    /// zero.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera to sort for.</param>
    /// <param name="on">Whether to sort at all. False takes it off again.</param>
    /// <param name="layers">
    /// How many fragments a pixel sorts exactly before the rest are merged approximately. More is
    /// more accurate and slower.
    /// </param>
    /// <param name="average">How many fragments a pixel budgets for on average.</param>
    /// <param name="threshold">The alpha below which a fragment is dropped rather than stored.</param>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetSortedTransparency(
        Entity camera,
        bool on = true,
        uint layers = 0,
        float average = 0f,
        float threshold = 0f) =>
        Native.Check(
            Native.bcs_render_set_sorted_transparency(
                camera.Bits, on ? 1 : 0, layers, average, threshold),
            $"sorting transparency for {camera}");

    /// <summary>
    /// Sets the ambient light every camera without its own is lit by. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// Light arriving from every direction at once, which is the cheapest stand-in for all the
    /// light bounced around a scene, and what ambient occlusion darkens where a surface is hemmed
    /// in. <paramref name="brightness"/> is in candela per square meter, the unit Bevy's lights
    /// use, and Bevy's own is eighty.
    /// </remarks>
    public static void SetAmbientLight((float R, float G, float B) color, float brightness) =>
        Native.Check(
            Native.bcs_render_set_ambient_light(0, color.R, color.G, color.B, Math.Max(0f, brightness)),
            "setting the ambient light");

    /// <summary>
    /// Gives a camera an ambient light of its own, or with <see langword="null"/> takes it away so
    /// the camera is lit by everyone's again. Only valid inside a system.
    /// </summary>
    public static void SetAmbientLight(Entity camera, (float R, float G, float B)? color, float brightness = 80f)
    {
        var (r, g, b) = color ?? (1f, 1f, 1f);

        Native.Check(
            Native.bcs_render_set_ambient_light(camera.Bits, r, g, b, color is null ? -1f : Math.Max(0f, brightness)),
            $"setting the ambient light of {camera}");
    }

    /// <summary>
    /// Turns Bevy's screen-space ambient occlusion on for a camera at a quality, or with
    /// <see langword="null"/> off. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Occlusion darkens the ambient and environment light Bevy's own materials receive where a
    /// surface is hemmed in, in corners, creases and under things, rather than darkening the
    /// finished picture, which would take direct light with it. It needs depth and normals, which it
    /// asks for itself, and a camera drawn once a pixel (<see cref="PostSettings.Msaa"/> of one);
    /// on a multisampled camera Bevy leaves it off with a warning.
    /// </para>
    /// <para>
    /// It is also the way in for occlusion of your own. While it is on, a compute shader on the
    /// camera sees the texture Bevy's materials read under the name <c>ambient_occlusion</c>, and
    /// one run at <see cref="FramePoint.AfterPrepass"/> writing it replaces Bevy's answer with its
    /// own before anything is lit. Bevy still computes its own first, so a camera replacing it asks
    /// for <see cref="AmbientOcclusionQuality.Low"/>.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="quality">How many samples a pixel takes, or null to turn it off.</param>
    /// <param name="thickness">
    /// How thick Bevy assumes what it sees to be, in world units, which decides how far behind a
    /// surface something has to be before it stops occluding. Zero keeps Bevy's own.
    /// </param>
    public static void SetAmbientOcclusion(Entity camera, AmbientOcclusionQuality? quality, float thickness = 0f) =>
        Native.Check(
            Native.bcs_render_set_ambient_occlusion(camera.Bits, quality is { } level ? (int)level : -1, thickness),
            $"setting the ambient occlusion of {camera}");

    /// <summary>
    /// Draws Bevy's own materials deferred, into a G-buffer lit afterward, or forward, lit as they
    /// are drawn, which is the default. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// Screen-space reflections read the deferred G-buffer, and deferred makes many lights cheap.
    /// It needs cameras drawn once a pixel (<see cref="PostSettings.Msaa"/> of one), and applies to
    /// Bevy's own materials, since one a Slang program draws is always forward, since it writes a
    /// color rather than a surface description. Every Bevy material is prepared again when it
    /// changes.
    /// </remarks>
    public static void SetDeferredRendering(bool on) =>
        Native.Check(Native.bcs_render_set_deferred(on ? 1 : 0), "switching between forward and deferred");

    /// <summary>
    /// Draws contact shadows on a camera, or with <see langword="null"/> stops. Only valid inside a
    /// system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A shadow map is drawn from the light at a resolution of its own, so the shadow right where
    /// a foot meets a floor or a cup meets a table is lost in a texel or two. A contact shadow is
    /// traced from each pixel toward each light that casts one
    /// (<see cref="LightSettings.ContactShadows"/>), a short way through the depth buffer, and
    /// fills in exactly that.
    /// </para>
    /// <para>
    /// The camera draws depth before the scene for it. Only what is on screen can cast one, and a
    /// thin object seen edge on casts less than it should, since the depth buffer has no idea how
    /// thick anything is.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetContactShadows(Entity camera, ContactShadowSettings? settings) =>
        Native.Check(
            settings is { } given
                ? Native.bcs_render_set_contact_shadows(camera.Bits, Math.Max(1u, given.Steps), given.Thickness, given.Length)
                : Native.bcs_render_set_contact_shadows(camera.Bits, 0, 0f, 0f),
            $"setting contact shadows on {camera}");

    /// <summary>
    /// Turns Bevy's screen-space reflections on for a camera, or with <see langword="null"/> off.
    /// Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reflections are traced against the depth buffer and read the lit picture, so they show what
    /// is on screen, fading out at its edges, on surfaces smoother than the settings' roughness
    /// ranges. They read Bevy's G-buffer, so turning them on also turns on
    /// <see cref="SetDeferredRendering"/> and asks the camera for the depth and deferred prepasses.
    /// </para>
    /// <para>
    /// What leaves the screen leaves its reflection too, which is the limit of any screen-space
    /// technique. A reflection probe or a traced reflection fills that in.
    /// </para>
    /// </remarks>
    public static void SetScreenSpaceReflections(Entity camera, ReflectionSettings? settings)
    {
        if (settings is null)
        {
            Native.Check(
                Native.bcs_render_set_screen_space_reflections(camera.Bits, null),
                $"taking reflections off {camera}");
            return;
        }

        var config = new NativeReflectionConfig
        {
            MinRoughnessStart = settings.FadeInRoughness.Start,
            MinRoughnessFull = settings.FadeInRoughness.Full,
            MaxRoughnessStart = settings.FadeOutRoughness.Start,
            MaxRoughnessEnd = settings.FadeOutRoughness.Gone,
            EdgeGone = settings.EdgeFade.Gone,
            EdgeFull = settings.EdgeFade.Full,
            Thickness = settings.Thickness,
            LinearSteps = Math.Max(1u, settings.Steps),
            LinearExponent = settings.StepExponent,
            BisectionSteps = settings.RefineSteps,
            UseSecant = settings.Secant ? 1 : 0,
        };

        Native.Check(
            Native.bcs_render_set_screen_space_reflections(camera.Bits, &config),
            $"turning reflections on for {camera}");
    }
}
