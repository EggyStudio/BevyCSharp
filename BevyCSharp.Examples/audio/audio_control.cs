// Bevy's audio_control example, examples/audio/audio_control.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Sound;

// This example illustrates how to load and play an audio file, and control how it's played. The
// music's speed sweeps between a tenth and twice as fast, with its pitch, Space pauses and resumes
// it, M mutes it, and - and = turn it down and up by a tenth, while the text says how far it has
// played.
internal static class AudioControl
{
    public static void Build(App app) => app.Startup(Setup, "audio_control.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var music = Audio.Play(AssetServer.Load(AssetKind.Audio, "sounds/Windless Slopes.ogg"));
        ctx.Ecs.Add(music, new MyMusic());

        var progress = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        ctx.Ecs.Add(progress, new ProgressText { Music = music });

        // The example's instructions.
        Ui.SpawnText("-/=: Volume Down/Up\nSpace: Toggle Playback\nM: Toggle Mute", new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });

        Render.SpawnCamera3d();
    }
}

/// <summary>The music the keys control, Bevy's <c>MyMusic</c>.</summary>
[Behavior]
public partial struct MyMusic
{
    /// <summary>
    /// Bevy's <c>update_speed</c>, <c>pause</c>, <c>mute</c> and <c>volume</c>, each skipped until
    /// the music has its sink, as Bevy's queries for one are.
    /// </summary>
    [OnUpdate]
    public readonly void Control(BehaviorContext ctx)
    {
        var music = ctx.Entity;
        if (!Audio.HasStarted(music)) return;

        if (!Audio.IsPaused(music))
            Audio.SetSpeed(music, MathF.Max(MathF.Sin(ctx.Time.Elapsed / 5f) + 1f, 0.1f));

        var input = ctx.Input;
        var volume = Audio.VolumeOf(music);
        if (input.KeyPressed(Key.Space))
        {
            if (Audio.IsPaused(music)) Audio.Resume(music, volume);
            else Audio.Pause(music, volume);
        }

        if (input.KeyPressed(Key.M)) Audio.SetMuted(music, !Audio.IsMuted(music));

        // Bevy's increase_by_percentage, ten percent of the volume up or down.
        if (input.KeyPressed(Key.Equal)) Audio.SetVolume(music, volume * 1.1f);
        else if (input.KeyPressed(Key.Minus)) Audio.SetVolume(music, volume * 0.9f);
    }
}

/// <summary>The text that says how far the music has played, Bevy's <c>ProgressText</c>.</summary>
[Behavior]
public partial struct ProgressText
{
    /// <summary>The music it reads, the one Bevy's <c>Single</c> finds.</summary>
    public Entity Music;

    /// <summary>Bevy's <c>update_progress_text</c>.</summary>
    [OnUpdate]
    public readonly void UpdateProgressText(BehaviorContext ctx)
    {
        if (!Audio.HasStarted(Music)) return;
        Ui.SetText(ctx.Entity, $"Progress: {Audio.PositionOf(Music)}s");
    }
}
