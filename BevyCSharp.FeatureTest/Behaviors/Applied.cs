using Bevy;
using Bevy.Physics;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// Applies the settings to the world, as the program starts and each time one changes.
/// </summary>
/// <remarks>
/// <para>
/// The panel changes a setting through <see cref="Settings.Change"/>, which writes the file and
/// marks it here, and the next update applies what changed, since a setting reaches the world and
/// the panel is drawn where the world is lent. Vsync is the window's from its start, so it is read
/// as the program builds its config and waits for the next start.
/// </para>
/// <para>
/// The picture's settings go on the scene's camera as one <see cref="PostSettings"/>, built from
/// the anti-aliasing, the bloom, the tonemapper and the sharpening, and the lens's as one
/// <see cref="EffectSettings"/>, the depth of field, the motion blur, the chromatic aberration,
/// the vignette and the auto exposure. The sun is made again with or without shadows, since a
/// light's settings are given as it is made.
/// </para>
/// <para>
/// Ambient occlusion and screen-space reflections both need the picture drawn once a pixel, so
/// MSAA gives way to either. The occlusion is left to <see cref="RayTracedOcclusion"/> while that
/// traces its own into the same texture. The reflections draw every material deferred, which is
/// put back to forward as they go unless Solari keeps it deferred. The backdrop is the
/// atmosphere the scene starts with or a dusk sky drawn in code as a cubemap, made the first time
/// it is asked for.
/// </para>
/// </remarks>
[Behavior]
public partial struct Applied
{
    private static bool _due = true;
    private static readonly HashSet<Entity> Wired = [];
    private static bool _reflections;
    private static Backdrop _backdrop;
    private static AssetHandle _dusk = AssetHandle.None;

    /// <summary>Where the depth of field is focused, eased toward what the view rests on.</summary>
    private static float _focus = 10f;

    /// <summary>Marks the settings to be applied at the next update.</summary>
    internal static void Due() => _due = true;

    /// <summary>Hears of each change from the settings.</summary>
    [OnStartup]
    public static void Listen(BehaviorContext ctx)
    {
        Settings.Changed -= OnChanged;
        Settings.Changed += OnChanged;
        _due = true;
        Wired.Clear();
        _reflections = false;
        _backdrop = Backdrop.Atmosphere;
        _dusk = AssetHandle.None;
        _focus = 10f;
    }

    private static void OnChanged(FeatureSettings was) => _due = true;

    /// <summary>
    /// Applies what is due, and keeps new meshes drawn as their edges while that is on.
    /// </summary>
    [OnUpdate]
    public static void Apply(BehaviorContext ctx)
    {
        var drawn = App.HasRenderer && !ctx.Res<Config>().Headless;
        if (drawn && Settings.Current.Wireframe) Wire(ctx.Ecs, true);

        if (!_due) return;
        _due = false;

        var settings = Settings.Current;
        ctx.Time.SetSpeed(settings.TimeScale);

        Bevy.Audio.SetGlobalVolume(settings.Master);
        Bevy.Audio.SetBusVolume("music", settings.Music);
        Bevy.Audio.SetBusVolume("effects", settings.Effects);

        if (!drawn) return;

        if (!ctx.Res<Config>().Offscreen) Window.SetMode(settings.Window);
        if (Scene.Camera is { } camera && ctx.Ecs.IsAlive(camera)) Camera(camera, settings);
        Scene.Light(ctx.Ecs, settings.Shadows);
        if (!settings.Wireframe) Wire(ctx.Ecs, false);
    }

    /// <summary>
    /// Keeps the depth of field focused on what the middle of the view rests on, as a camera's
    /// autofocus does, while the setting is on.
    /// </summary>
    /// <remarks>
    /// A ray from the camera along its view finds the nearest body, past the player's own in
    /// first person, where the eye is inside it, and the focus eases toward it over a few frames
    /// rather than snapping. The lens is set again only when the focus has moved by more than a
    /// hundredth, so a still view sets nothing.
    /// </remarks>
    [OnUpdate]
    public static void Focus(BehaviorContext ctx)
    {
        var settings = Settings.Current;
        if (settings.DepthOfField == DepthOfFieldMode.None || !App.HasRenderer || ctx.Res<Config>().Headless) return;
        if (Scene.Camera is not { } camera || !ctx.Ecs.IsAlive(camera)) return;
        if (!ctx.World.TryGetResource<PhysicsWorld>(out var physics)) return;

        const float Farthest = 200f;
        var view = ctx.Ecs.GetOrDefault<Transform>(camera);
        var forward = view.Rotation * new Vec3(0f, 0f, -1f);
        var inside = Player.Mode != PlayerMode.Spectator && Player.View == PlayerView.FirstPerson && physics.Has(Player.Entity);
        var hit = inside
            ? physics.Raycast(view.Translation, forward, Farthest, Player.Entity)
            : physics.Raycast(view.Translation, forward, Farthest);

        var wanted = hit?.Distance ?? Farthest;
        var eased = _focus + ((wanted - _focus) * MathF.Min(1f, ctx.Time.Delta * 6f));
        if (MathF.Abs(eased - _focus) < _focus * 0.01f) return;

        _focus = eased;
        Render.SetEffects(camera, Lens(settings));
    }

    /// <summary>Puts the picture's, the lens's and the sky's settings on the camera.</summary>
    private static void Camera(Entity camera, FeatureSettings settings)
    {
        Render.SetPostProcessing(camera, Picture(settings));
        Render.SetEffects(camera, Lens(settings));

        if (!RayTracedOcclusion.Tracing)
            Render.SetAmbientOcclusion(camera, settings.AmbientOcclusion ? AmbientOcclusionQuality.High : null);

        if (settings.Reflections != _reflections)
        {
            Render.SetScreenSpaceReflections(camera, settings.Reflections ? new ReflectionSettings() : null);
            if (!settings.Reflections && !Render.RayTracingActive) Render.SetDeferredRendering(false);
            _reflections = settings.Reflections;
        }

        if (settings.Backdrop != _backdrop)
        {
            if (settings.Backdrop == Backdrop.Dusk)
            {
                if (!_dusk.IsValid) _dusk = Dusk();
                Render.ClearAtmosphere(camera);
                Render.SetSkybox(camera, _dusk, brightness: 600f);
            }
            else
            {
                Render.SetSkybox(camera, AssetHandle.None);
                Render.SetAtmosphere(camera, new AtmosphereSettings());
            }

            _backdrop = settings.Backdrop;
        }
    }

    /// <summary>What the camera does with the picture, from the settings.</summary>
    internal static PostSettings Picture(FeatureSettings settings) => new()
    {
        Hdr = true,
        Tonemapper = settings.Tonemapper,
        Bloom = settings.Bloom,
        BloomIntensity = 0.3f,
        Sharpen = settings.Sharpen,
        AntiAlias = settings.Smoothing switch
        {
            Smoothing.Fxaa => AntiAliasPass.Fxaa,
            Smoothing.Smaa => AntiAliasPass.Smaa,
            Smoothing.Taa => AntiAliasPass.Temporal,
            _ => AntiAliasPass.None,
        },
        Msaa = settings.Smoothing == Smoothing.Msaa && !settings.AmbientOcclusion && !settings.Reflections ? 4 : 1,
    };

    /// <summary>The lens the camera draws through, from the settings.</summary>
    /// <remarks>
    /// No depth of field focuses at a fixed distance over a map this size, which would blur every
    /// zone but one, so it follows <see cref="Focus"/>. A shutter open half a frame smears as
    /// film does at 24 frames a second.
    /// </remarks>
    internal static EffectSettings Lens(FeatureSettings settings) => new()
    {
        DepthOfField = settings.DepthOfField,
        FocalDistance = _focus,
        Aperture = 2f,
        ShutterAngle = settings.MotionBlur ? 0.5f : 0f,
        MotionBlurSamples = settings.MotionBlur ? 4u : 1u,
        Aberration = settings.Aberration ? 0.03f : 0f,
        AberrationSamples = settings.Aberration ? 8u : 0u,
        Vignette = settings.Vignette ? 0.45f : 0f,
        VignetteRadius = 0.7f,
        AutoExposure = settings.AutoExposure,
    };

    /// <summary>Draws every mesh as its edges, or stops, the ones spawned since included.</summary>
    private static void Wire(EcsWorld ecs, bool on)
    {
        if (!on)
        {
            foreach (var entity in Wired)
                if (ecs.IsAlive(entity)) Render.SetWireframe(entity, false);

            Wired.Clear();
            return;
        }

        foreach (var entity in ecs.All())
        {
            if (Wired.Contains(entity) || !Render.MeshOf(ecs, entity).IsValid) continue;

            Render.SetWireframe(entity, true, (0.9f, 0.9f, 0.9f, 1f));
            Wired.Add(entity);
        }
    }

    /// <summary>
    /// A dusk sky as a cubemap, drawn here a pixel at a time, a deep blue overhead going to an
    /// orange band at the horizon with stars in the dark, for the effects page's backdrop.
    /// </summary>
    /// <remarks>
    /// Each face's pixel is turned into the direction it is seen along, by the layout every
    /// cubemap shares, and colored by how high that direction looks, so the faces meet without a
    /// seam. The stars come from a hash of the pixel, which is the same each run.
    /// </remarks>
    private static AssetHandle Dusk()
    {
        const int Size = 128;
        var pixels = new byte[Size * Size * 6 * 4];
        for (var face = 0; face < 6; face++)
        {
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var u = (2f * (x + 0.5f) / Size) - 1f;
                    var v = (2f * (y + 0.5f) / Size) - 1f;
                    var direction = face switch
                    {
                        0 => new Vec3(1f, -v, -u),
                        1 => new Vec3(-1f, -v, u),
                        2 => new Vec3(u, 1f, v),
                        3 => new Vec3(u, -1f, -v),
                        4 => new Vec3(u, -v, 1f),
                        _ => new Vec3(-u, -v, -1f),
                    };
                    var height = direction.Normalized.Y;

                    var (r, g, b) = height < 0f
                        ? Mix((0.35f, 0.18f, 0.12f), (0.04f, 0.035f, 0.045f), MathF.Min(1f, -height * 4f))
                        : Mix((0.95f, 0.45f, 0.2f), (0.02f, 0.03f, 0.12f), MathF.Pow(MathF.Min(1f, height * 2.2f), 0.6f));

                    var hash = (uint)((face * 73_856_093) ^ (y * 19_349_663) ^ (x * 83_492_791));
                    hash = (hash ^ (hash >> 13)) * 0x5bd1e995u;
                    if (height > 0.25f && (hash ^ (hash >> 15)) % 700 == 0) (r, g, b) = (1f, 1f, 0.95f);

                    var at = ((((face * Size) + y) * Size) + x) * 4;
                    (pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3]) =
                        ((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), 255);
                }
            }
        }

        var image = Render.CreateImage(pixels, Size, Size * 6);
        Render.MakeCubemap(image);
        return image;
    }

    private static (float, float, float) Mix((float R, float G, float B) from, (float R, float G, float B) to, float amount) =>
        (from.R + ((to.R - from.R) * amount), from.G + ((to.G - from.G) * amount), from.B + ((to.B - from.B) * amount));
}
