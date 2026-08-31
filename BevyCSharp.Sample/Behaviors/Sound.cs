using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// Plays a chime on F3, to show that a sound is an entity like anything else.
/// </summary>
/// <remarks>
/// Inert in a headless run, like the rest of the sample's presentation, so the same scripts run
/// in both modes.
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

    /// <summary>Plays it on F3.</summary>
    /// <remarks>
    /// <see cref="AudioSettings.Effect"/> despawns the entity when the sound ends, so pressing
    /// this a hundred times leaves nothing behind to clean up.
    /// </remarks>
    [OnUpdate]
    public static void PlayOnKey(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;
        if (!ctx.Input.KeyPressed(Key.F3)) return;

        Audio.Play(_chime, AudioSettings.Effect);
    }
}
