using Bevy;

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
/// the anti-aliasing, the bloom and what the camera always has. The sun is made again with or
/// without shadows, since a light's settings are given as it is made.
/// </para>
/// </remarks>
[Behavior]
public partial struct Applied
{
    private static bool _due = true;
    private static readonly HashSet<Entity> Wired = [];

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
        if (Scene.Camera is { } camera && ctx.Ecs.IsAlive(camera)) Render.SetPostProcessing(camera, Picture(settings));
        Scene.Light(ctx.Ecs, settings.Shadows);
        if (!settings.Wireframe) Wire(ctx.Ecs, false);
    }

    /// <summary>What the camera does with the picture, from the settings.</summary>
    internal static PostSettings Picture(FeatureSettings settings) => new()
    {
        Hdr = true,
        Tonemapper = Tonemapper.TonyMcMapface,
        Bloom = settings.Bloom,
        BloomIntensity = 0.3f,
        AntiAlias = settings.Smoothing switch
        {
            Smoothing.Fxaa => AntiAliasPass.Fxaa,
            Smoothing.Smaa => AntiAliasPass.Smaa,
            Smoothing.Taa => AntiAliasPass.Temporal,
            _ => AntiAliasPass.None,
        },
        Msaa = settings.Smoothing == Smoothing.Msaa ? 4 : 1,
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
}
