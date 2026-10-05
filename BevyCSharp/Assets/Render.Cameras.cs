using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Render
{
    /// <summary>
    /// Spawns a 3D camera and returns it.
    /// </summary>
    /// <remarks>
    /// Bevy draws nothing without a camera. The new one sits at the origin looking down negative
    /// Z; position it by writing its <see cref="Transform"/> like any other entity.
    /// </remarks>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnCamera3d() => new(Native.bcs_render_spawn_camera_3d(null));

    /// <summary>
    /// Gives a camera part of the window to draw into, or the whole of it.
    /// </summary>
    /// <remarks>
    /// What a docked panel needs. The interface takes the right of the window and the scene is told
    /// to draw into what is left, so the picture is the shape of the space rather than the shape of
    /// the window with something over it. A width or height of zero means the whole window again.
    /// </remarks>
    /// <param name="camera">The camera to place.</param>
    /// <param name="x">Left edge, in physical pixels.</param>
    /// <param name="y">Top edge, in physical pixels.</param>
    /// <param name="width">How wide, in physical pixels, or zero for the whole window.</param>
    /// <param name="height">How tall, in physical pixels, or zero for the whole window.</param>
    public static void SetViewport(Entity camera, uint x, uint y, uint width, uint height) =>
        Native.Check(
            Native.bcs_render_set_viewport(camera.Bits, x, y, width, height),
            "Render.SetViewport");

    /// <summary>
    /// Sets a camera's field of view and how near and how far it sees, making it a perspective
    /// camera if it was not one. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// For a camera tuned while it runs, such as an editor's view of the scene, since
    /// <see cref="CameraSettings"/> only decides these when the camera is made. The near distance is
    /// where depth precision is spent, mostly close to it, so a very small one makes far surfaces
    /// flicker against each other; raise it before lowering the far one.
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="fieldOfView">Degrees across the picture's height, above nothing and below 180.</param>
    /// <param name="near">The nearest distance seen, above nothing.</param>
    /// <param name="far">The furthest distance seen, beyond the nearest.</param>
    /// <exception cref="ArgumentOutOfRangeException">A value outside those ranges.</exception>
    public static void SetPerspective(Entity camera, float fieldOfView, float near, float far)
    {
        if (!(fieldOfView > 0f && fieldOfView < 180f)) throw new ArgumentOutOfRangeException(nameof(fieldOfView), fieldOfView, "A field of view is above nothing and below 180 degrees.");
        if (!(near > 0f)) throw new ArgumentOutOfRangeException(nameof(near), near, "The near distance has to be above nothing.");
        if (!(far > near)) throw new ArgumentOutOfRangeException(nameof(far), far, "The far distance has to be beyond the near one.");

        Native.Check(Native.bcs_render_set_perspective(camera.Bits, fieldOfView, near, far), "Render.SetPerspective");
    }

    /// <summary>
    /// Rounds the corners of a camera's picture, showing <paramref name="fill"/> outside them, which
    /// is clear unless given. A radius of nothing squares them again. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The corners of the camera's viewport where it has one (<see cref="SetViewport"/>), and of
    /// its whole picture otherwise. What the camera drew is multiplied by how much of each pixel
    /// lies inside the rounded rectangle, color and alpha alike, and the fill is added by how much
    /// lies outside, after the camera's own passes and before its picture reaches the window. So
    /// the edge is antialiased, and what the corner shows can be clear or partly clear, which
    /// nothing drawn over the picture can make it.
    /// </para>
    /// <para>
    /// A camera given part of a window wants the fill to be the window's clear color round it
    /// (<see cref="SetClearColor"/>), so its corners match what surrounds them. A camera filling a
    /// window whose own corners are being rounded wants them clear, which is only seen through on
    /// a window made with <see cref="Config.Transparent"/> and is black on any other.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="radius">How round, in physical pixels.</param>
    /// <param name="fill">What shows outside the corners, in straight linear RGBA.</param>
    /// <exception cref="ArgumentOutOfRangeException">The radius is negative or not a number.</exception>
    public static void SetRoundedCorners(
        Entity camera,
        float radius,
        (float R, float G, float B, float A) fill = default)
    {
        if (!float.IsFinite(radius) || radius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "A radius is a length, so it cannot be negative.");
        }

        Native.Check(
            Native.bcs_render_set_rounded_corners(camera.Bits, radius, fill.R, fill.G, fill.B, fill.A),
            "Render.SetRoundedCorners");
    }

    /// <summary>
    /// Sets the world's clear color, in linear RGBA, which a camera clearing to
    /// <see cref="ClearMode.World"/> clears to. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It also fills the part of a window no camera's viewport covers, since Bevy clears the whole
    /// window before putting a camera's picture on its part of it. An editor whose scene is given
    /// part of the window (<see cref="SetViewport"/>) and whose window is see-through
    /// (<see cref="Config.Transparent"/>) clears to nothing here, and gives the scene camera a
    /// color of its own (<see cref="ClearMode.Custom"/>), so the scene has a background and the
    /// rest of the window does not.
    /// </para>
    /// </remarks>
    /// <param name="color">The color, straight rather than premultiplied.</param>
    public static void SetClearColor((float R, float G, float B, float A) color) =>
        Native.Check(
            Native.bcs_render_set_clear_color(color.R, color.G, color.B, color.A),
            "Render.SetClearColor");


    /// <summary>Spawns a 3D camera set up by <paramref name="settings"/>.</summary>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnCamera3d(CameraSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeCameraConfig
        {
            Projection = (int)settings.Projection,
            FovDegrees = settings.FieldOfView,
            OrthoHeight = settings.Height,
            Near = settings.Near,
            Far = settings.Far,
            ClearMode = (int)settings.Clear,
            ClearR = settings.ClearColor.R,
            ClearG = settings.ClearColor.G,
            ClearB = settings.ClearColor.B,
            ClearA = settings.ClearColor.A,
            Order = settings.Order,
            HasViewport = settings.Viewport is null ? 0 : 1,
            ViewportX = settings.Viewport?.X ?? 0,
            ViewportY = settings.Viewport?.Y ?? 0,
            ViewportWidth = settings.Viewport?.Width ?? 0,
            ViewportHeight = settings.Viewport?.Height ?? 0,
            Layers = settings.Layers,
        };

        return new Entity(Native.bcs_render_spawn_camera_3d(&native));
    }

    /// <summary>
    /// Spawns a light and returns it.
    /// </summary>
    /// <param name="kind">Which light to spawn.</param>
    /// <param name="intensity">
    /// Illuminance in lux for a directional light, luminous power in lumens for a point light.
    /// </param>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnLight(LightKind kind, float intensity) =>
        SpawnLight(new LightSettings { Kind = kind, Intensity = intensity });

    /// <summary>Spawns a light set up by <paramref name="settings"/>.</summary>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnLight(LightSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeLightConfig
        {
            Kind = (int)settings.Kind,
            Intensity = settings.Intensity,
            ColorR = settings.Color.R,
            ColorG = settings.Color.G,
            ColorB = settings.Color.B,
            Range = settings.Range,
            Radius = settings.Radius,
            Shadows = (settings.Shadows ? 1 : 0) | (settings.ContactShadows ? 2 : 0),
            InnerAngle = settings.InnerAngle,
            OuterAngle = settings.OuterAngle,
            ShadowDepthBias = settings.ShadowDepthBias,
            ShadowNormalBias = settings.ShadowNormalBias,
        };

        return new Entity(Native.bcs_render_spawn_light(&native));
    }

    /// <summary>
    /// Sets how a directional light divides its shadows across the distance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A directional light covers the whole scene, so one shadow map stretched over all of it is
    /// coarse near the camera, which is where it is looked at closest. Cascades split the range
    /// into a few maps, each covering a nearer and smaller slice of it. A shadow that looks blocky
    /// at arm's length is this rather than a resolution.
    /// </para>
    /// <para>
    /// Every number here keeps Bevy's own when it is left at zero, so a call setting one of them
    /// says one thing rather than restating the rest.
    /// </para>
    /// </remarks>
    /// <param name="light">A directional light from <see cref="SpawnLight(LightSettings)"/>.</param>
    /// <param name="cascades">How many maps to split the range into, up to four.</param>
    /// <param name="minimum">The nearest distance that receives a shadow.</param>
    /// <param name="maximum">
    /// The furthest. The cost of a large number is quality rather than time, since the same maps
    /// are stretched over more ground.
    /// </param>
    /// <param name="firstBound">
    /// Where the first cascade ends. The ones after it are spaced out toward
    /// <paramref name="maximum"/>, so this is the knob for how much detail the near ground gets.
    /// </param>
    /// <param name="overlap">
    /// How much of each cascade is blended into the next, as a proportion, which keeps the join
    /// between two of them from showing as a line across the ground.
    /// </param>
    /// <exception cref="BevyNativeException">The entity is not a directional light.</exception>
    public static void SetShadowCascades(
        Entity light,
        int cascades = 0,
        float minimum = 0f,
        float maximum = 0f,
        float firstBound = 0f,
        float overlap = 0f) =>
        Native.Check(
            Native.bcs_render_set_shadow_cascades(
                light.Bits, cascades, minimum, maximum, firstBound, overlap),
            $"setting the shadow cascades of {light}");

    /// <summary>
    /// Shapes a spot light's beam with a picture, the way a gobo shapes a stage light.
    /// </summary>
    /// <remarks>
    /// What puts the shadow of a window frame on the floor without a window being there, or breaks
    /// a torch beam up so it does not read as a cone of paint. Only the red channel is read, so the
    /// picture says how much light gets through rather than what color it is, and its border should
    /// be black or the light leaks past the edge of it.
    /// </remarks>
    /// <param name="light">A spot light from <see cref="SpawnLight(LightSettings)"/>.</param>
    /// <param name="cookie">
    /// The picture, or <see cref="AssetHandle.None"/> to take the shaping off and leave a plain
    /// cone.
    /// </param>
    /// <exception cref="BevyNativeException">The entity is not a spot light.</exception>
    public static void SetLightCookie(Entity light, AssetHandle cookie) =>
        Native.Check(
            Native.bcs_render_set_light_cookie(light.Bits, cookie.Key),
            $"shaping the beam of {light}");

    /// <summary>
    /// Softens a light's shadow the farther it falls from what casts it, as a light
    /// <paramref name="size"/> world units across does. Zero makes it hard again. Only valid inside
    /// a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A real light has a size, so its shadow is sharp where an object touches the ground and
    /// blurs as it stretches away, which a blur of one fixed width cannot do. This is Bevy's
    /// percentage-closer soft shadows, which search the shadow map around each point for how far
    /// the casters are and widen the penumbra to match.
    /// </para>
    /// <para>
    /// For a point or a spot light the size is its <see cref="LightSettings.Radius"/>, which this
    /// sets, and a penumbra shows from a few units up. A directional light's penumbra is worked
    /// out in its shadow map's depth, and Bevy's own example gives one a size of ten. It is noisy
    /// with the default filtering, so it suits a camera given
    /// <see cref="ShadowFiltering.Temporal"/> with temporal antialiasing on, and it costs a good
    /// deal more than a hard shadow.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">The entity is not a light.</exception>
    public static void SetSoftShadows(Entity light, float size) =>
        Native.Check(Native.bcs_render_set_soft_shadows(light.Bits, size), $"softening {light}'s shadows");

    /// <summary>
    /// Sets how a camera filters the shadow maps it reads. Only valid inside a system.
    /// </summary>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetShadowFiltering(Entity camera, ShadowFiltering filtering) =>
        Native.Check(Native.bcs_render_set_shadow_filtering(camera.Bits, (int)filtering), $"filtering {camera}'s shadows");

    /// <summary>
    /// Sets what a camera does to the picture after the scene has been drawn.
    /// </summary>
    /// <remarks>
    /// The whole pipeline in one call. An effect the settings leave off is removed from the
    /// camera, so the same call turns something on and off again. Only a camera can be given
    /// these, since it is the camera's render graph that reads them.
    /// </remarks>
    /// <param name="camera">A camera entity from <see cref="SpawnCamera3d()"/> or
    /// <see cref="Render2d.SpawnCamera2d"/>.</param>
    /// <param name="settings">What the camera should do.</param>
    /// <exception cref="ArgumentException">
    /// <see cref="AntiAliasPass.Temporal"/> is asked for together with multisampling.
    /// </exception>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// var camera = Render.SpawnCamera3d();
    ///
    /// Render.SetPostProcessing(camera, new PostSettings
    /// {
    ///     Hdr = true,
    ///     Bloom = true,
    ///     BloomIntensity = 0.2f,
    ///     Tonemapper = Tonemapper.AgX,
    ///     AntiAlias = AntiAliasPass.Fxaa,
    ///     Msaa = 1,
    /// });
    /// </code>
    /// </example>
    public static void SetPostProcessing(Entity camera, PostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Temporal antialiasing resolves the picture from the frames before it, and a
        // multisampled target has no such history. Bevy answers the pair by warning once a frame
        // and drawing nothing, which is the kind of quiet failure worth refusing outright.
        if (settings.AntiAlias == AntiAliasPass.Temporal && settings.Msaa is 2 or 4 or 8)
        {
            throw new ArgumentException(
                "temporal antialiasing cannot run on a multisampled target, and Msaa is "
                    + $"{settings.Msaa}; set Msaa to 1 to use it",
                nameof(settings));
        }

        var native = new NativePostConfig
        {
            Tonemapping = (int)settings.Tonemapper,
            Dither = settings.Dither ? 1 : 0,
            Hdr = settings.Hdr ? 1 : 0,
            Msaa = settings.Msaa,
            AntiAlias = (int)settings.AntiAlias,
            AntiAliasQuality = (int)settings.Quality,
            Sharpen = settings.Sharpen,
            Bloom = settings.Bloom ? 1 : 0,
            BloomIntensity = settings.BloomIntensity,
            BloomThreshold = settings.BloomThreshold,
            BloomThresholdSoftness = settings.BloomThresholdSoftness,
            BloomMode = (int)settings.BloomMode,
        };

        Native.Check(
            Native.bcs_render_set_post(camera.Bits, &native),
            $"setting the post processing on {camera}");
    }

    /// <summary>
    /// Sets the lens a camera draws through.
    /// </summary>
    /// <remarks>
    /// Beside <see cref="SetPostProcessing"/>, which is the pipeline a settings screen owns, a
    /// scene sets this for a moment. The whole set in one call either way, so an effect these
    /// settings leave off is taken off the camera.
    /// </remarks>
    /// <param name="camera">A camera entity from <see cref="SpawnCamera3d()"/> or
    /// <see cref="Render2d.SpawnCamera2d"/>.</param>
    /// <param name="settings">The lens to draw through.</param>
    /// <exception cref="ArgumentException">
    /// <see cref="EffectSettings.ExposureCompensation"/> does not rise in luminance.
    /// </exception>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, an image named in the settings is not loaded, or
    /// this build has no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// Render.SetEffects(camera, new EffectSettings
    /// {
    ///     DepthOfField = DepthOfFieldMode.Bokeh,
    ///     FocalDistance = 8f,
    ///     Aperture = 1.4f,
    ///     ShutterAngle = 0.5f,
    ///     Vignette = 0.4f,
    /// });
    /// </code>
    /// </example>
    public static void SetEffects(Entity camera, EffectSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeEffectsConfig
        {
            DofMode = (int)settings.DepthOfField,
            FocalDistance = settings.FocalDistance,
            ApertureFStops = settings.Aperture,
            SensorHeight = settings.SensorHeight,
            MaxBlurDiameter = settings.MaxBlurDiameter,
            MaxDepth = settings.MaxDepth,
            ShutterAngle = settings.ShutterAngle,
            MotionBlurSamples = settings.MotionBlurSamples,
            Aberration = settings.Aberration,
            AberrationSamples = settings.AberrationSamples,
            AberrationLut = Key(settings.AberrationColors),
            Distortion = settings.Distortion,
            DistortionScale = settings.DistortionScale,
            DistortionAxisX = settings.DistortionAxes.X,
            DistortionAxisY = settings.DistortionAxes.Y,
            DistortionCenterX = settings.DistortionCenter.X,
            DistortionCenterY = settings.DistortionCenter.Y,
            DistortionEdgeCurvature = settings.DistortionEdgeCurvature,
            Vignette = settings.Vignette,
            VignetteRadius = settings.VignetteRadius,
            VignetteSmoothness = settings.VignetteSmoothness,
            VignetteRoundness = settings.VignetteRoundness,
            VignetteCenterX = settings.VignetteCenter.X,
            VignetteCenterY = settings.VignetteCenter.Y,
            VignetteEdgeCompensation = settings.VignetteEdgeCompensation,
            VignetteColorR = settings.VignetteColor.R,
            VignetteColorG = settings.VignetteColor.G,
            VignetteColorB = settings.VignetteColor.B,
            VignetteColorA = settings.VignetteColor.A,
            AutoExposure = settings.AutoExposure ? 1 : 0,
            MeteringMin = settings.MeteringRange.Min,
            MeteringMax = settings.MeteringRange.Max,
            MeteringLow = settings.MeteringFilter.Low,
            MeteringHigh = settings.MeteringFilter.High,
            SpeedBrighten = settings.SpeedBrighten,
            SpeedDarken = settings.SpeedDarken,
            ExposureTransition = settings.ExposureTransition,
            MeteringMask = Key(settings.MeteringMask),
        };

        var curve = settings.ExposureCompensation;
        if (curve is not null)
        {
            var count = Math.Min(curve.Count, NativeEffectsConfig.CompensationPoints);
            native.CompensationCount = (uint)count;

            for (var i = 0; i < count; i++)
            {
                // The curve is read by looking a measured brightness up in it, so it has to rise
                // in luminance. Caught here rather than on the far side, where the only answer
                // the bridge can give is a status code.
                if (i > 0 && curve[i].Luminance <= curve[i - 1].Luminance)
                {
                    throw new ArgumentException(
                        $"exposure compensation point {i} is at luminance {curve[i].Luminance}, "
                            + $"which is not above the {curve[i - 1].Luminance} before it",
                        nameof(settings));
                }

                native.CompensationCurve[i * 2] = curve[i].Luminance;
                native.CompensationCurve[(i * 2) + 1] = curve[i].Compensation;
            }
        }

        Native.Check(
            Native.bcs_render_set_effects(camera.Bits, &native),
            $"setting the effects on {camera}");

        static int Key(AssetHandle handle) => handle.IsValid ? handle.Key : -1;
    }

    /// <summary>
    /// Draws the sky the air scatters, seen from a camera.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two things make a sky: a planet, which is an entity the size of a world, and a camera told
    /// to sample it. This call keeps at most one planet in the world and points the camera at it,
    /// so calling it for a second camera adds a viewer rather than a second sky.
    /// </para>
    /// <para>
    /// The camera is given a high dynamic range target, which the sky needs, because a sun
    /// scattered through air is far brighter than white. Pair it with
    /// <see cref="SetPostProcessing"/> for a tonemapper to bring that range back down.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera that should see the sky.</param>
    /// <param name="settings">How thick the air is, and at what scale.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// var camera = Render.SpawnCamera3d();
    /// var sun = Render.SpawnLight(LightKind.Directional, 10_000f);
    /// ctx.Ecs.Add(sun, Transform.LookingAt(new Vec3(1f, 0.6f, 0f), Vec3.Zero, Vec3.UnitY));
    ///
    /// Render.SetAtmosphere(camera, new AtmosphereSettings());
    /// Render.SetPostProcessing(camera, new PostSettings { Hdr = true });
    /// </code>
    /// </example>
    public static void SetAtmosphere(Entity camera, AtmosphereSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeAtmosphereConfig
        {
            Enabled = 1,
            Density = settings.Density,
            Scale = settings.Scale,
            HazeDistance = settings.HazeDistance,
            Quality = (int)settings.Quality,
            GroundAlbedo = settings.GroundAlbedo,
        };

        Native.Check(
            Native.bcs_render_set_atmosphere(camera.Bits, &native),
            $"setting the atmosphere on {camera}");
    }

    /// <summary>
    /// Stops a camera drawing the sky.
    /// </summary>
    /// <remarks>
    /// The planet stays where it is. Nothing is computed for it until a camera asks again, so
    /// leaving it costs a component and no work.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    public static void ClearAtmosphere(Entity camera)
    {
        var native = new NativeAtmosphereConfig();

        Native.Check(
            Native.bcs_render_set_atmosphere(camera.Bits, &native),
            $"clearing the atmosphere on {camera}");
    }
}
