using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Builds renderable assets and attaches them to entities.
/// </summary>
/// <remarks>
/// <para>
/// Everything here needs a native build with the renderer compiled in. On a headless build the
/// calls report that rather than failing obscurely, so the same behavior code runs either way and
/// draws nothing.
/// </para>
/// <para>
/// Meshes and materials are Rust values that have to be constructed rather than described by a
/// layout, and the components carrying them hold a typed handle that raw bytes cannot represent.
/// That is why these are named operations rather than a component written through
/// <see cref="EcsWorld.Add{T}"/>, the way <see cref="Transform"/> is.
/// </para>
/// </remarks>
public static unsafe class Render
{
    /// <summary>
    /// Builds a mesh primitive and returns a handle to it.
    /// </summary>
    /// <param name="shape">One of the constants on <see cref="MeshShape"/>.</param>
    /// <param name="a">Width for a cuboid or plane, radius for a sphere or capsule.</param>
    /// <param name="b">Height for a cuboid, depth for a plane, length for a capsule.</param>
    /// <param name="c">Depth, for a cuboid.</param>
    public static AssetHandle CreateMesh(string shape, float a = 1f, float b = 1f, float c = 1f)
    {
        ArgumentException.ThrowIfNullOrEmpty(shape);

        var key = Native.bcs_mesh_create(shape, a, b, c);
        if (key == NativeStatus.Unsupported) throw NoRenderer("Building a mesh");
        if (key == NativeStatus.NoComponent)
            throw new BevyNativeException(
                NativeStatus.NoComponent,
                $"'{shape}' is not a mesh primitive the engine can build. Use one of the "
                + "constants on MeshShape.");

        Native.Check(key, $"building a {shape} mesh");
        return new AssetHandle(key);
    }

    /// <summary>
    /// Builds a physically based material and returns a handle to it.
    /// </summary>
    /// <param name="red">Linear sRGB red, from zero to one.</param>
    /// <param name="green">Linear sRGB green, from zero to one.</param>
    /// <param name="blue">Linear sRGB blue, from zero to one.</param>
    /// <param name="alpha">Opacity, from zero to one.</param>
    /// <param name="metallic">Zero for a dielectric, one for a metal.</param>
    /// <param name="roughness">Near zero for a mirror, one for a matte surface.</param>
    public static AssetHandle CreateMaterial(
        float red,
        float green,
        float blue,
        float alpha = 1f,
        float metallic = 0f,
        float roughness = 0.5f) =>
        CreateMaterial(new MaterialSettings
        {
            BaseColor = (red, green, blue, alpha),
            Metallic = metallic,
            Roughness = roughness,
        });

    /// <summary>Builds a material from <paramref name="settings"/> and returns a handle to it.</summary>
    /// <remarks>
    /// A texture the settings leave at <see cref="AssetHandle.None"/> is one the material does
    /// without. A handle that names nothing, which is what a released one becomes, is refused
    /// instead, because drawing the surface untextured and reporting success would leave the caller
    /// with a wrong picture and nothing pointing at why.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// A texture handle names nothing, or this build has no renderer.
    /// </exception>
    public static AssetHandle CreateMaterial(MaterialSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeMaterialConfig
        {
            BaseR = settings.BaseColor.R,
            BaseG = settings.BaseColor.G,
            BaseB = settings.BaseColor.B,
            BaseA = settings.BaseColor.A,
            Metallic = settings.Metallic,
            Roughness = settings.Roughness,
            EmissiveR = settings.Emissive.R,
            EmissiveG = settings.Emissive.G,
            EmissiveB = settings.Emissive.B,
            EmissiveA = settings.Emissive.A,
            AlphaMode = (int)settings.AlphaMode,
            AlphaCutoff = settings.AlphaCutoff,
            DoubleSided = settings.DoubleSided ? 1 : 0,
            Unlit = settings.Unlit ? 1 : 0,
            BaseColorTexture = Key(settings.BaseColorTexture),
            NormalMap = Key(settings.NormalMap),
            MetallicRoughnessTexture = Key(settings.MetallicRoughnessTexture),
            EmissiveTexture = Key(settings.EmissiveTexture),
            OcclusionTexture = Key(settings.OcclusionTexture),
            UvScaleX = settings.UvScale.U,
            UvScaleY = settings.UvScale.V,
            UvRotation = settings.UvRotation,
            UvOffsetX = settings.UvOffset.U,
            UvOffsetY = settings.UvOffset.V,
        };

        var key = Native.bcs_material_create(&native);
        if (key == NativeStatus.Unsupported) throw NoRenderer("Building a material");

        Native.Check(key, "building a material");
        return new AssetHandle(key);

        // An unset handle is -1, which is what the bridge reads as "no texture here".
        static int Key(AssetHandle handle) => handle.IsValid ? handle.Key : -1;
    }

    /// <summary>
    /// Gives an entity a mesh to draw.
    /// </summary>
    /// <remarks>
    /// Inserting this also pulls in the components Bevy requires alongside it, such as
    /// <see cref="Transform"/> and visibility, so an entity needs nothing else to be drawable
    /// beyond a material.
    /// </remarks>
    public static void SetMesh(EcsWorld world, Entity entity, AssetHandle mesh) =>
        Attach(world, entity, "Mesh3d", mesh, "a mesh");

    /// <summary>Gives an entity a material to draw its mesh with.</summary>
    public static void SetMaterial(EcsWorld world, Entity entity, AssetHandle material) =>
        Attach(world, entity, "MeshMaterial3d", material, "a material");

    /// <summary>
    /// Where an entity's mesh was loaded from, or empty when it was not loaded from anywhere.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A mesh and a material are Bevy's own components holding typed handles, so nothing on this
    /// side can read them the way it reads a component of its own. What they can be asked is where
    /// they came from, which is what a tool showing an entity has something to say about and what a
    /// person can point at a different file.
    /// </para>
    /// <para>
    /// Empty for anything built in memory, which is everything <see cref="CreateMesh"/> makes, and
    /// for an entity carrying no mesh at all. The two are told apart by
    /// <see cref="EcsWorld.Has{T}"/> on <see cref="Bevy.Mesh3d"/> where a mirror exists, and by
    /// nothing where one does not, which is the honest limit.
    /// </para>
    /// </remarks>
    /// <param name="entity">The entity to ask about.</param>
    public static string MeshPathOf(Entity entity) => AssetPathOf(entity, 0);

    /// <summary>Where an entity's material was loaded from, or empty when it was not.</summary>
    /// <remarks>
    /// Only a standard material answers, since a material drawn by a shader slot is a different
    /// type and one of several. <see cref="MeshPathOf"/> covers the rest of the reasoning.
    /// </remarks>
    /// <param name="entity">The entity to ask about.</param>
    public static string MaterialPathOf(Entity entity) => AssetPathOf(entity, 1);

    /// <summary>
    /// Whether an entity carries a mesh the renderer draws.
    /// </summary>
    /// <remarks>
    /// The question <see cref="MeshPathOf"/> cannot answer, since a mesh made in memory has no
    /// path and neither does an entity with no mesh at all. A tool showing what something is drawn
    /// with has to tell those apart.
    /// </remarks>
    /// <param name="entity">The entity to ask about.</param>
    public static bool IsDrawn(Entity entity) =>
        Native.bcs_render_asset_path(entity.Bits, 0, null, 0) >= 0;

    /// <summary>One of the two paths, or empty when there is nothing to say.</summary>
    private static string AssetPathOf(Entity entity, int which)
    {
        // Absent rather than refused, because a tool asks this about whatever is selected and most
        // things are not drawn at all.
        var probe = Native.bcs_render_asset_path(entity.Bits, which, null, 0);
        if (probe < 0) return string.Empty;

        return Native.ReadText(
            (buffer, capacity) =>
                Native.bcs_render_asset_path(entity.Bits, which, buffer, capacity),
            $"reading what {entity} is drawn with");
    }

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
            Shadows = settings.Shadows ? 1 : 0,
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
    /// Where the first cascade ends. The ones after it are spaced out towards
    /// <paramref name="maximum"/>, so this is the knob for how much detail the near ground gets.
    /// </param>
    /// <param name="overlap">
    /// How much of each cascade is blended into the next, as a proportion, which is what keeps the
    /// join between two of them from showing as a line across the ground.
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
    /// Beside <see cref="SetPostProcessing"/>: that call is the pipeline a settings screen owns,
    /// and this is what a scene does for a moment. The whole set in one call either way, so an
    /// effect these settings leave off is taken off the camera.
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
    /// Lights the scene from a cubemap, filtered on the GPU.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The other way to light a scene from its surroundings. <see cref="SetSkyLighting"/> derives
    /// the map from the atmosphere, which covers an outdoor scene; this takes a picture, which is
    /// what an indoor one, or a scene lit from a photograph, needs.
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
    /// and is what a shipped game wants, especially for an environment too large to filter again.
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
    /// <param name="intensity">How bright, in candelas per square metre.</param>
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
    /// Lights the scene from the sky this camera is already scattering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What makes a surface pick up the color of what is around it rather than only what a lamp
    /// points at it. The environment map is derived from the atmosphere each frame, so it follows
    /// the sun: a scene lit this way goes warm at dusk without anything being animated.
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
    /// <paramref name="brightness"/> scales the samples into the units the rest of the scene is
    /// lit in, which are candelas per square metre, so the useful numbers are in the hundreds or
    /// thousands. A brightness of one is a night sky and comes out black, which reads as a skybox
    /// that failed rather than one that is very dark. The skybox is what is seen behind the scene
    /// and does not light it; lighting from a sky is an environment map, which has no bridge yet.
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
    /// This is the base an auto exposure pass corrects rather than an alternative to it: with
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
    /// Asks for a picture to be read back into memory rather than written to a file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same capture <see cref="Screenshot(string)"/> takes, delivered as bytes instead of as a
    /// PNG. A file is for a person to look at; this is for a program to inspect, which is what
    /// asserting on what was drawn needs.
    /// </para>
    /// <para>
    /// The picture arrives a frame or two later, because it has to come back off the GPU, so this
    /// answers with a ticket rather than with pixels. Poll
    /// <see cref="TryReadCapture(Capture, out CapturedImage?)"/> until it says yes, from a later
    /// frame rather than in a loop, because the frames are what the picture is waiting on.
    /// </para>
    /// <para>
    /// A capture of the first frames of a run is a picture of a window that has been cleared and
    /// not yet drawn into. A material's render pipeline is compiled the first time something asks
    /// to be drawn with it, and until it is ready the renderer skips the mesh and clears the frame
    /// anyway, so an early capture shows the clear color with nothing in it. That is indistinguishable
    /// from a mesh that never arrived, so give a fresh run a hundred frames before believing an
    /// empty picture.
    /// </para>
    /// </remarks>
    /// <param name="target">
    /// The image to capture, from <see cref="CreateTarget"/>, or
    /// <see cref="AssetHandle.None"/> for whatever this run is drawing into.
    /// </param>
    /// <returns>A ticket naming the capture.</returns>
    /// <exception cref="BevyNativeException">
    /// This build has no renderer, or the handle names no image.
    /// </exception>
    public static Capture BeginCapture(AssetHandle target)
    {
        var id = Native.bcs_render_capture(target.Key);
        if (id == NativeStatus.Unsupported) throw NoRenderer("Capturing a picture");

        Native.Check(id, $"asking for a capture of {target}");
        return new Capture(id);
    }

    /// <summary>Asks for a picture of whatever this run is drawing into.</summary>
    public static Capture BeginCapture() => BeginCapture(AssetHandle.None);

    /// <summary>
    /// Reads a capture once it has arrived, and forgets it.
    /// </summary>
    /// <remarks>
    /// False means the picture is still on its way, which is the ordinary answer for the first
    /// frame or two. True hands it over and drops the engine's copy, because it is a megabyte or
    /// two and nothing on that side knows when the caller would be done with it.
    /// </remarks>
    /// <param name="capture">The ticket from <see cref="BeginCapture(AssetHandle)"/>.</param>
    /// <param name="picture">The pixels, when this returns true.</param>
    /// <returns>Whether the picture had arrived.</returns>
    /// <exception cref="BevyNativeException">The ticket names no capture.</exception>
    public static bool TryReadCapture(Capture capture, out CapturedImage? picture)
    {
        picture = null;

        uint width;
        uint height;

        var needed = Native.bcs_render_capture_read(capture.Id, &width, &height, null, 0);

        // Still coming back off the GPU. Asking again next frame is the whole of the protocol.
        if (needed == NativeStatus.InvalidState) return false;
        if (needed == NativeStatus.Unsupported) throw NoRenderer("Reading a capture");

        Native.Check(needed, $"asking how large {capture} is");

        var pixels = new byte[needed];

        fixed (byte* buffer = pixels)
        {
            Native.Check(
                Native.bcs_render_capture_read(capture.Id, &width, &height, buffer, needed),
                $"reading {capture}");
        }

        picture = new CapturedImage(width, height, pixels);
        return true;
    }

    /// <summary>Forgets a capture that will not be read.</summary>
    /// <remarks>
    /// For a caller that stopped waiting. A capture that has arrived holds its pixels until
    /// something drops them, and one that never arrives holds nothing.
    /// </remarks>
    public static void ReleaseCapture(Capture capture) =>
        Native.Check(Native.bcs_render_capture_release(capture.Id), $"releasing {capture}");

    /// <summary>
    /// Creates an empty image a camera can draw into.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What a portal, a security monitor, a minimap or a second viewport is built from. Point a
    /// camera at it with <see cref="SetCameraTarget"/>, and give the same handle to a material as
    /// its <see cref="MaterialSettings.BaseColorTexture"/>, and the surface carrying that material
    /// shows what the camera sees.
    /// </para>
    /// <para>
    /// The image is empty until something draws into it, and nothing loads, so the handle is
    /// usable on the frame it is returned. It is sized in pixels rather than in logical units,
    /// because nothing about it is scaled by a desktop.
    /// </para>
    /// </remarks>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <returns>A handle to the image.</returns>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static AssetHandle CreateTarget(uint width, uint height)
    {
        var key = Native.bcs_render_create_target(width, height);
        if (key == NativeStatus.Unsupported) throw NoRenderer("Creating a render target");

        Native.Check(key, $"creating a {width}x{height} render target");
        return new AssetHandle(key);
    }

    /// <summary>
    /// Makes an image out of pixels held here, and hands back a handle to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The other end of <see cref="TryReadCapture"/>. Reading gives back what was drawn; this takes
    /// a picture that was never in a file, which is what a texture worked out at startup, a mask
    /// built from a heightmap, or a capture handed on to a material needs.
    /// </para>
    /// <para>
    /// Nothing loads, so the handle is usable on the frame it is returned, and the pixels are
    /// copied rather than kept, so the array is the caller's again afterwards.
    /// </para>
    /// </remarks>
    /// <param name="pixels">
    /// <paramref name="width"/> by <paramref name="height"/> pixels of RGBA, a row at a time from
    /// the top, which is the layout <see cref="CapturedImage.Pixels"/> comes back in.
    /// </param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="srgb">
    /// Whether the numbers are a color somebody chose, which is what a picture usually is. False
    /// reads them as they are, for a picture whose numbers mean something else, such as a normal
    /// map or a roughness mask.
    /// </param>
    /// <returns>A handle to the image.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="pixels"/> is not four bytes per pixel of the size given.
    /// </exception>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static AssetHandle CreateImage(
        ReadOnlySpan<byte> pixels,
        uint width,
        uint height,
        bool srgb = true)
    {
        var wanted = (long)width * height * 4;

        if (width == 0 || height == 0)
            throw new ArgumentException("An image needs a width and a height.", nameof(width));

        if (pixels.Length != wanted)
        {
            throw new ArgumentException(
                $"A {width}x{height} image is {wanted} bytes of RGBA, and {pixels.Length} were "
                + "given.",
                nameof(pixels));
        }

        int key;

        fixed (byte* at = pixels)
        {
            key = Native.bcs_render_create_image(at, width, height, srgb ? 1 : 0);
        }

        if (key < 0) throw NoRenderer($"Creating a {width}x{height} image");

        return new AssetHandle(key);
    }

    /// <summary>
    /// Points a camera at an image instead of at the window.
    /// </summary>
    /// <remarks>
    /// <see cref="AssetHandle.None"/> puts it back on the window, which is where a camera starts.
    /// The camera keeps everything else it was given, because its projection, its layers, its
    /// order and its post-processing are about what it draws rather than about where the result
    /// goes.
    /// </remarks>
    /// <param name="camera">The camera, from <see cref="SpawnCamera3d(CameraSettings)"/>.</param>
    /// <param name="target">The image to draw into, or <see cref="AssetHandle.None"/> for the window.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is not a camera, or the handle names no image.
    /// </exception>
    public static void SetCameraTarget(Entity camera, AssetHandle target) => Native.Check(
        Native.bcs_render_set_camera_target(camera.Bits, target.Key),
        $"pointing {camera} at {target}");

    /// <summary>
    /// Where a world point lands on a camera's viewport, in logical pixels.
    /// </summary>
    /// <remarks>
    /// The same coordinates the cursor is reported in, which is what lets something drawn in the
    /// world be hit-tested against the pointer. A point behind the camera answers
    /// <see langword="false"/> rather than a number that would be off the screen in the wrong
    /// direction.
    /// </remarks>
    public static bool TryProject(Entity camera, Vec3 point, out float x, out float y)
    {
        var screen = stackalloc float[2];

        if (Native.bcs_render_world_to_viewport(camera.Bits, point.X, point.Y, point.Z, screen) < 0)
        {
            x = 0f;
            y = 0f;
            return false;
        }

        x = screen[0];
        y = screen[1];
        return true;
    }

    /// <summary>
    /// The ray through a point on a camera's viewport.
    /// </summary>
    /// <remarks>
    /// The other half of <see cref="TryProject"/>. Where a drag is taking something is a question
    /// about this ray and the axis being dragged along, rather than about pixels.
    /// </remarks>
    public static bool TryRay(Entity camera, float x, float y, out Vec3 origin, out Vec3 direction)
    {
        var ray = stackalloc float[6];

        if (Native.bcs_render_viewport_to_world(camera.Bits, x, y, ray) < 0)
        {
            origin = default;
            direction = default;
            return false;
        }

        origin = new Vec3(ray[0], ray[1], ray[2]);
        direction = new Vec3(ray[3], ray[4], ray[5]);
        return true;
    }

    /// <summary>
    /// The box an entity occupies in the world, or <see langword="false"/> when it has none.
    /// </summary>
    /// <remarks>
    /// Bevy computes bounds for everything it draws, in the mesh's own space; what comes back
    /// here is those bounds put through the entity's global transform, so a rotated object gets
    /// the box around its corners rather than its corners moved. Anything not drawn, a camera or
    /// a bare entity, has no bounds and answers <see langword="false"/>.
    /// </remarks>
    public static bool TryGetBounds(Entity entity, out Vec3 min, out Vec3 max)
    {
        var bounds = stackalloc float[6];

        if (Native.bcs_render_bounds(entity.Bits, bounds) < 0)
        {
            min = default;
            max = default;
            return false;
        }

        min = new Vec3(bounds[0], bounds[1], bounds[2]);
        max = new Vec3(bounds[3], bounds[4], bounds[5]);
        return true;
    }

    /// <summary>
    /// Draws an entity's mesh as its own edges, or stops drawing them.
    /// </summary>
    /// <remarks>
    /// The shape itself rather than a box round it, which is what an editor outlines a selection
    /// with when the box is not enough. The line pipeline it needs is a desktop one. Where a
    /// backend cannot draw lines, this is accepted and nothing appears.
    /// </remarks>
    /// <param name="entity">What to draw, or stop drawing.</param>
    /// <param name="on">Whether to draw it.</param>
    /// <param name="color">Linear RGBA for the lines.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static void SetWireframe(
        Entity entity, bool on, (float R, float G, float B, float A) color = default)
    {
        var status = Native.bcs_render_wireframe(
            entity.Bits, on ? 1 : 0, color.R, color.G, color.B, color.A);

        if (status == NativeStatus.Unsupported) throw NoRenderer("Drawing a wireframe");

        // An entity that has gone is not an error to stop drawing, because a selection outlives
        // what it pointed at by a frame, and asking after that is how it is cleaned up.
        if (status == NativeStatus.NoEntity) return;

        Native.Check(status, "drawing a wireframe");
    }

    /// <summary>
    /// Sets how large a shadow map each kind of light gets, in pixels on a side.
    /// </summary>
    /// <remarks>
    /// One size for every directional light and one for every point and spot light, because Bevy
    /// keeps these globally rather than per light. Larger is sharper and costs memory and fill
    /// rate on every shadow-casting light at once. Bevy's defaults are 2048 and 1024. Zero leaves
    /// that kind as it is, so one can be changed without knowing the other.
    /// </remarks>
    /// <param name="directional">Size for directional lights, or 0 to leave it.</param>
    /// <param name="point">Size for point and spot lights, or 0 to leave it.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static void SetShadowMapSize(uint directional = 0, uint point = 0)
    {
        var status = Native.bcs_render_set_shadow_maps(directional, point);
        if (status == NativeStatus.Unsupported) throw NoRenderer("Setting the shadow map size");

        Native.Check(status, "setting the shadow map size");
    }

    /// <summary>
    /// Puts an entity on a set of render layers, as a bit per layer.
    /// </summary>
    /// <remarks>
    /// A camera draws an entity only where their layers overlap. Zero takes the entity back to
    /// Bevy's default layer, which is what every camera sees unless it says otherwise.
    /// </remarks>
    /// <example>
    /// <code>
    /// const uint Minimap = 1u &lt;&lt; 1;
    ///
    /// Render.SetLayers(ctx.Ecs, marker, Minimap);          // only the minimap camera sees it
    /// Render.SetLayers(ctx.Ecs, player, 1u | Minimap);     // both cameras do
    /// </code>
    /// </example>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static void SetLayers(EcsWorld world, Entity entity, uint layers)
    {
        ArgumentNullException.ThrowIfNull(world);

        var status = Native.bcs_render_set_layers(entity.Bits, layers);
        if (status == NativeStatus.Unsupported) throw NoRenderer("Setting render layers");

        Native.Check(status, $"setting the render layers of {entity}");
    }

    /// <summary>Attaches a handle through one of the components that carry one.</summary>
    private static void Attach(
        EcsWorld world,
        Entity entity,
        string component,
        AssetHandle handle,
        string described)
    {
        ArgumentNullException.ThrowIfNull(world);

        var status = Native.bcs_ecs_insert_asset(entity.Bits, component, handle.Key);
        if (status == NativeStatus.Unsupported) throw NoRenderer($"Attaching {described}");
        if (status == NativeStatus.NoEntity)
            throw new BevyNativeException(
                NativeStatus.NoEntity,
                $"Cannot attach {described} to {entity}: either the entity is no longer alive, "
                + "or the handle has been released.");
        if (status == NativeStatus.NoComponent)
            throw new BevyNativeException(
                NativeStatus.NoComponent,
                $"That handle does not point at {described}. Check that the handle came from the "
                + "matching Create call.");

        Native.Check(status, $"attaching {described}");
    }

    /// <summary>The error for asking a headless build to do something graphical.</summary>
    private static BevyNativeException NoRenderer(string attempted) =>
        new(NativeStatus.Unsupported,
            $"{attempted} needs a native build with the renderer compiled in. Rebuild the bridge "
            + "with build/build-native.sh --render, or guard the call with App.HasRenderer.");
}
