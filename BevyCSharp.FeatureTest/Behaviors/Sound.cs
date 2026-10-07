using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// Plays a chime from the panel's audio page, to show that a sound is an entity like anything else.
/// </summary>
/// <remarks>
/// Inert in a headless run, like the rest of the program's presentation, so the same scripts run
/// in both modes. Played on the effects bus, whose volume the audio page sets.
/// </remarks>
[Behavior]
public partial struct Sound
{
    /// <summary>The loaded chime, kept so it is not asked for again on every press.</summary>
    private static AssetHandle _chime;

    /// <summary>Loads the clip once, before anything asks to play it.</summary>
    [OnStartup]
    public static void Load(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        _chime = AssetServer.Load(AssetKind.Audio, "sounds/chime.wav");
    }

    /// <summary>Plays it.</summary>
    /// <remarks>
    /// <see cref="AudioSettings.Effect"/> despawns the entity when the sound ends, so playing it
    /// many times over leaves nothing behind to clean up.
    /// </remarks>
    internal static void Chime(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless || !_chime.IsValid) return;

        var effect = AudioSettings.Effect;
        effect.Bus = "effects";
        Audio.Play(_chime, effect);
    }
}
