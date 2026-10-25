using System.Numerics;
using Bevy.Interop;

namespace Bevy;

public static partial class Render
{
    /// <summary>
    /// Draws clouds in a camera's sky, replacing any it had, or with <see langword="null"/> takes
    /// them off. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The clouds are marched through a shell of the sky above the planet at half the picture's size
    /// across and down, after the prepass, and laid over the sky before tonemapping, behind every
    /// piece of geometry and in front of the sky's fog. Their light is the sun's through the
    /// camera's atmosphere (<see cref="SetAtmosphere"/>) and the sky's from the camera's environment
    /// map (<see cref="SetSkyLighting"/>), and without either they are lit by the sun alone, white.
    /// Each frame blends with the last where the clouds stood, so they settle over a few frames and
    /// move smoothly on the wind.
    /// </para>
    /// <para>
    /// The noise they are cut from is made once an app on the GPU, the first time a camera asks,
    /// and the clouds show the frame after it is made. They need a camera drawn once a pixel
    /// (<see cref="PostSettings.Msaa"/> of one), whose depth says where the sky is. The setting
    /// lasts while the app runs.
    /// </para>
    /// <para>
    /// With <see cref="CloudSettings.ShadowLight"/> the clouds shadow the sun, how much of its light
    /// gets through worked out over a square about the camera each frame and laid over the light as
    /// its picture, which Bevy's lighting reads, so the clouds' shadows drift over the ground with
    /// them, and the sky's fog in froxels darkens in them too.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="settings">The clouds, or null to take them off.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A setting is not a number, the layer's top is not above its base, or a size, the density or
    /// the distance is not above none.
    /// </exception>
    /// <exception cref="BevyNativeException">The settings name a shadow light that is not a directional light.</exception>
    /// <example>
    /// <code>
    /// Render.SetAtmosphere(camera, new AtmosphereSettings { Medium = ScatteringTerm.EarthArc() });
    /// Render.SetSkyLighting(camera);
    /// Render.SetClouds(camera, new CloudSettings { Coverage = 0.5f });
    /// </code>
    /// </example>
    public static void SetClouds(Entity camera, CloudSettings? settings)
    {
        if (settings is null)
        {
            Clouds.Clear(camera);
            return;
        }

        float[] numbers =
        [
            settings.Coverage, settings.Stratus, settings.Bottom, settings.Top, settings.Density, settings.ShapeSize, settings.DetailSize,
            settings.Erosion, settings.WeatherSize, settings.Wind.X, settings.Wind.Y, settings.Wind.Z, settings.Distance, settings.Ambient,
            settings.ShadowSize, settings.ShadowStrength,
        ];
        if (!numbers.All(float.IsFinite))
            throw new ArgumentOutOfRangeException(nameof(settings), "A setting of the clouds is not a number.");
        if (!(settings.Top > settings.Bottom) || settings.Bottom < 0f)
            throw new ArgumentOutOfRangeException(nameof(settings), "The clouds' top is above their base, which is above the ground.");
        if (!(settings.Density > 0f && settings.ShapeSize > 0f && settings.DetailSize > 0f && settings.WeatherSize > 0f && settings.Distance > 0f && settings.ShadowSize > 0f))
            throw new ArgumentOutOfRangeException(nameof(settings), "The clouds' density, sizes and distance are above none.");

        Clouds.Set(camera, settings);
    }
}

/// <summary>The clouds <see cref="Render.SetClouds"/> puts on cameras, the noise an app's and the march and draw a camera's.</summary>
internal static class Clouds
{
    /// <summary>The name the clouds' image, dispatch, draw and prepass are kept under on a camera beside the engine's other parts.</summary>
    internal const string Owner = "clouds";

    // The planet's radius the shell lies over where the camera has no atmosphere to say, earth's.
    private const float EarthRadius = 6_360_000f;

    // The texels across the clouds' shadow.
    private const uint ShadowTexels = 512;

    private static App? _app;
    private static Noise? _noise;
    private static readonly Dictionary<ulong, Camera> Cameras = [];

    // The noise volumes, made once an app and again for another seed, and whether the dispatches
    // that make them have run.
    private sealed class Noise
    {
        internal required uint Seed { get; init; }
        internal required AssetHandle Shape { get; init; }
        internal required AssetHandle Detail { get; init; }
        internal required AssetHandle Weather { get; init; }
        internal required ShaderInstance[] Makers { get; init; }
        internal bool Made { get; set; }
    }

    private sealed class Camera
    {
        internal required ShaderInstance March { get; init; }
        internal required ShaderInstance Draw { get; init; }
        internal required CloudSettings Settings { get; set; }
        internal int Frames { get; set; }
        internal Vec3 Carried { get; set; }

        // The shadow's dispatch and image, made the first time the camera's clouds shadow a light,
        // the light given the picture and the square's middle last given it.
        internal ShaderInstance Shadow { get; set; }
        internal AssetHandle ShadowImage { get; set; } = AssetHandle.None;
        internal Entity Shadowed { get; set; } = Entity.None;
        internal Vec3 Middle { get; set; }
        internal (Vec3 Across, Vec3 Up) Axes { get; set; }
    }

    internal static void Set(Entity camera, CloudSettings settings)
    {
        ForgetOtherApps();
        var noise = NoiseFor(settings.Seed);
        var had = Cameras.TryGetValue(camera.Bits, out var found);
        var clouds = found!;
        if (!had)
        {
            clouds = new Camera
            {
                March = Shaders.CreateInstance(SceneField.KernelProgram("clouds.slang")),
                Draw = SceneField.Draw("clouds_draw.slang"),
                Settings = settings,
            };
            Cameras[camera.Bits] = clouds;
        }
        // A camera whose clouds stand as they are takes the numbers alone, so the weather can give
        // them every frame, its image, dispatches and draw left as they were.
        var standing = had && clouds.Settings.ShadowLight == settings.ShadowLight && clouds.Shadow.IsValid == !settings.ShadowLight.IsNone;
        if (!clouds.Shadowed.IsNone && clouds.Shadowed != settings.ShadowLight) Uncookie(clouds);
        clouds.Settings = settings;
        if (!settings.ShadowLight.IsNone && !clouds.Shadow.IsValid)
        {
            clouds.Shadow = Shaders.CreateInstance(SceneField.KernelProgram("clouds.slang", entry: "shadows"));
            clouds.ShadowImage = Shaders.CreateImage(ShadowTexels, ShadowTexels, ShaderImageFormat.Rgba8);
            clouds.Shadow.SetTexture("cloud_shadow", clouds.ShadowImage);
        }

        Feed(clouds, clouds.March, noise);
        if (clouds.Shadow.IsValid) Feed(clouds, clouds.Shadow, noise);
        clouds.Draw.SetSampler("clouds_sampler", SamplerSettings.Clamped);
        Turn(camera, clouds, 0f);
        if (standing) return;

        Shaders.SetEngineViewImages(camera, Owner, new ViewImage("clouds", ShaderImageFormat.Rgba16Float, Scale: 0.5f, History: true));
        Shaders.SetEnginePrepass(camera, Owner, 1u);
        var march = ViewDispatch.PerPixel(clouds.March, FramePoint.AfterPrepass, 8, 8, 0.5f);
        if (settings.ShadowLight.IsNone) Shaders.SetEngineViewDispatches(camera, Owner, march);
        else Shaders.SetEngineViewDispatches(camera, Owner, ViewDispatch.Fixed(clouds.Shadow, FramePoint.AfterPrepass, ShadowTexels / 8, ShadowTexels / 8), march);
        Shaders.SetEngineViewDraws(camera, Owner, ViewDraw.Fixed(clouds.Draw, FramePoint.BeforeTonemapping, 3, 1, DrawBlend.Alpha, writesDepth: false));
    }

    // The numbers and the noise both the march and the shadow read.
    private static void Feed(Camera clouds, ShaderInstance instance, Noise noise)
    {
        var settings = clouds.Settings;
        instance.Set("shape", new Vector4(Math.Clamp(settings.Coverage, 0f, 1f), settings.Density, 1f / settings.ShapeSize, 1f / settings.DetailSize))
            .Set("lobes", new Vector4(0.8f, -0.3f, 0.3f, Math.Max(0f, settings.Ambient)))
            .Set("stratus", new Vector4(Math.Clamp(settings.Stratus, 0f, 1f), 0f, 0f, 0f))
            .SetTexture("cloud_shape", noise.Shape)
            .SetTexture("cloud_detail", noise.Detail)
            .SetTexture("cloud_weather", noise.Weather)
            .SetSampler("cloud_sampler", SamplerSettings.Linear);

        // Last frame's clouds are the march's alone, which the shadow does not declare, and a
        // value set for what a compiled shader does not declare is refused.
        if (instance == clouds.March) instance.SetSampler("clouds_previous_sampler", SamplerSettings.Clamped);
    }

    internal static void Clear(Entity camera)
    {
        ForgetOtherApps();
        if (!Cameras.Remove(camera.Bits, out var clouds)) return;
        Uncookie(clouds);
        Shaders.SetEngineViewDraws(camera, Owner);
        Shaders.SetEngineViewDispatches(camera, Owner);
        Shaders.SetEnginePrepass(camera, Owner, 0);
        Shaders.SetEngineViewImages(camera, Owner);
    }

    /// <summary>The cameras with clouds in the running app, with the settings each was given.</summary>
    internal static IEnumerable<(Entity Camera, CloudSettings Settings)> All()
    {
        ForgetOtherApps();
        return Cameras.Select(clouds => (new Entity(clouds.Key), clouds.Value.Settings));
    }

    /// <summary>Whether the noise is made and every camera's march and draw have compiled, so a frame draws the clouds.</summary>
    internal static bool Ready()
    {
        ForgetOtherApps();
        return (_noise?.Made ?? false) && Cameras.Values.All(c =>
            c.March.Program.State == ShaderProgramState.Ready && c.Draw.Program.State == ShaderProgramState.Ready);
    }

    /// <summary>
    /// Makes the noise once its program has compiled, and moves every camera's clouds on a frame,
    /// the wind carrying them and the history read from the third frame on.
    /// </summary>
    internal static void Tick(World world)
    {
        ForgetOtherApps();
        if (_noise is { Made: false } noise && noise.Makers.All(m => m.Program.State == ShaderProgramState.Ready))
        {
            Shaders.Dispatch(noise.Makers[0], 32, 32, 32);
            Shaders.Dispatch(noise.Makers[1], 8, 8, 8);
            Shaders.Dispatch(noise.Makers[2], 128, 128, 1);
            noise.Made = true;
        }

        if (Cameras.Count == 0) return;
        var delta = world.Resource<Time>().Delta;
        var ecs = world.Resource<EcsWorld>();
        foreach (var (camera, clouds) in Cameras)
        {
            Turn(new Entity(camera), clouds, delta);
            Cookie(ecs, new Entity(camera), clouds);
        }
    }

    /// <summary>The clouds' shadow on a camera, its image and the square it lies over, for the fog to read, or none.</summary>
    internal static (AssetHandle Image, Vec3 Middle, float Half, Vec3 Across, Vec3 Up)? ShadowFor(Entity camera)
    {
        ForgetOtherApps();
        if (!Cameras.TryGetValue(camera.Bits, out var clouds) || clouds.Shadowed.IsNone) return null;
        return (clouds.ShadowImage, clouds.Middle, clouds.Settings.ShadowSize / 2f, clouds.Axes.Across, clouds.Axes.Up);
    }

    // Lays the shadow over its light about the camera, the square's middle moved a texel at a time
    // across the sun's own axes so the shadow does not crawl as the camera moves, and hands the
    // shadow's dispatch the square.
    private static void Cookie(EcsWorld ecs, Entity camera, Camera clouds)
    {
        var light = clouds.Settings.ShadowLight;
        if (light.IsNone || !clouds.Shadow.IsValid || !ecs.IsAlive(light)) return;

        var turn = ecs.GetOrDefault<Transform>(light).Rotation;
        var (across, up, along) = (turn * new Vec3(1f, 0f, 0f), turn * new Vec3(0f, 1f, 0f), turn * new Vec3(0f, 0f, 1f));
        var eye = ecs.GetOrDefault<GlobalTransform>(camera).Translation;
        var texel = clouds.Settings.ShadowSize / ShadowTexels;
        float Snap(float value) => MathF.Round(value / texel) * texel;
        var middle = (across * Snap(Vec3.Dot(eye, across))) + (up * Snap(Vec3.Dot(eye, up))) + (along * Vec3.Dot(eye, along));
        var half = clouds.Settings.ShadowSize / 2f;

        clouds.Shadow.Set("shadow_frame", new Vector4(middle.X, middle.Y, middle.Z, half))
            .Set("shadow_across", new Vector4(across.X, across.Y, across.Z, 0f))
            .Set("shadow_up", new Vector4(up.X, up.Y, up.Z, Math.Clamp(clouds.Settings.ShadowStrength, 0f, 1f)));
        if (clouds.Shadowed != light || clouds.Middle != middle)
        {
            Native.Check(Native.bcs_render_set_directional_cookie(light.Bits, clouds.ShadowImage.Key, half, middle.X, middle.Y, middle.Z),
                $"laying the clouds' shadow over {light}");
            clouds.Shadowed = light;
        }
        clouds.Middle = middle;
        clouds.Axes = (across, up);
    }

    // Takes the shadow off the light it was laid over.
    private static void Uncookie(Camera clouds)
    {
        if (clouds.Shadowed.IsNone) return;
        Native.bcs_render_set_directional_cookie(clouds.Shadowed.Bits, -1, 0f, 0f, 0f, 0f);
        clouds.Shadowed = Entity.None;
    }

    // Carries the clouds on a frame and hands the march its steps, last frame's clouds where they
    // may be read, and the glow of a strike flashing in them, which last frame's would smear, so
    // none is read while it flashes.
    private static void Turn(Entity camera, Camera clouds, float delta)
    {
        clouds.Frames++;
        clouds.Carried += clouds.Settings.Wind * delta;
        var settings = clouds.Settings;
        var glow = SkyThunder.GlowFor(camera);
        clouds.March.Set("flash", glow is { } lit ? new Vector4(lit.Middle.X, lit.Middle.Y, lit.Middle.Z, lit.Light) : Vector4.Zero)
            .Set("flash_color", glow is { } tint ? new Vector4(tint.Color, tint.Radius) : Vector4.Zero);

        // The steps along a way and toward the sun.
        var (steps, toward) = settings.Quality switch
        {
            CloudQuality.Low => (48f, 4f),
            CloudQuality.High => (128f, 6f),
            _ => (72f, 6f),
        };
        var history = clouds.Frames > 2 && (_noise?.Made ?? false) && glow is null ? 1f : 0f;
        foreach (var instance in clouds.Shadow.IsValid ? [clouds.March, clouds.Shadow] : new[] { clouds.March })
        {
            instance.Set("layer", new Vector4(settings.Bottom, settings.Top, EarthRadius, settings.Distance))
                .Set("wind", new Vector4(clouds.Carried.X, clouds.Carried.Y, clouds.Carried.Z, 1f / settings.WeatherSize))
                .Set("march", new Vector4(steps, toward, Math.Max(0f, settings.Erosion), history));
        }
    }

    // The noise for a seed, made again where the seed differs from the one the app has.
    private static Noise NoiseFor(uint seed)
    {
        if (_noise is { } noise && noise.Seed == seed) return noise;
        ShaderInstance Maker(uint size, uint which, uint depth) =>
            Shaders.CreateInstance(SceneField.KernelProgram("cloud_noise.slang")).SetNumbers("size", 4, (ReadOnlySpan<uint>)[size, which, depth, seed]);
        var shape = Shaders.CreateImage(128, 128, ShaderImageFormat.Rgba8, depth: 128);
        var detail = Shaders.CreateImage(32, 32, ShaderImageFormat.Rgba8, depth: 32);
        // Two slices deep, so it is a 3D image as the march reads it, the second a copy of the first.
        var weather = Shaders.CreateImage(512, 512, ShaderImageFormat.Rgba8, depth: 2);
        ShaderInstance[] makers = [Maker(128, 0, 128), Maker(32, 1, 32), Maker(512, 2, 2)];
        makers[0].SetTexture("cloud_noise", shape);
        makers[1].SetTexture("cloud_noise", detail);
        makers[2].SetTexture("cloud_noise", weather);
        _noise = new Noise { Seed = seed, Shape = shape, Detail = detail, Weather = weather, Makers = makers };
        return _noise;
    }

    private static void ForgetOtherApps()
    {
        if (ReferenceEquals(_app, App.Running)) return;
        _app = App.Running;
        _noise = null;
        Cameras.Clear();
    }
}
