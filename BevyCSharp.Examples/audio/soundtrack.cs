// Bevy's soundtrack example, examples/audio/soundtrack.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Sound;

// Shows how to play a soundtrack that follows the game's state, a calm track and a battle track
// swapped every ten seconds, the new one fading in over two while the old one fades out.
internal static class Soundtrack
{
    private enum GameState { Peaceful, Battle }

    internal const float FadeTime = 2f;
    private const float StateTime = 10f;

    // Bevy's GameStateTimer resource, the time since the state last changed, which both fades
    // follow, and its SoundtrackPlayer resource, the two tracks.
    internal static GameTimer StateTimer;
    private static AssetHandle[] _tracks = [];
    private static GameState _state;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            (_state, StateTimer) = (GameState.Peaceful, GameTimer.FromSeconds(StateTime, TimerMode.Repeating));
            _tracks =
            [
                AssetServer.Load(AssetKind.Audio, "sounds/Mysterious acoustic guitar.ogg"),
                AssetServer.Load(AssetKind.Audio, "sounds/Epic orchestra music.ogg"),
            ];
            ChangeTrack(ctx);
        }, "soundtrack.Setup");

        // The state changes each time ten seconds pass, and with it the track.
        app.Update(ctx =>
        {
            if (!StateTimer.Tick(ctx.Time.Delta).JustFinished) return;
            _state = _state == GameState.Battle ? GameState.Peaceful : GameState.Battle;
            ChangeTrack(ctx);
        }, "soundtrack.CycleGameState");
    }

    // Whatever is playing fades out, and the state's track starts silent and fades in, as Bevy's
    // change_track does on entering a state.
    private static void ChangeTrack(BehaviorContext ctx)
    {
        foreach (var track in ctx.Ecs.Query<FadeIn>(markChanged: false))
        {
            ctx.Cmd.Remove<FadeIn>(track.Entity);
            ctx.Cmd.Add(track.Entity, new FadeOut());
        }

        var playing = Audio.Play(_tracks[(int)_state], new AudioSettings { Mode = PlaybackMode.Loop, Volume = 0f });
        ctx.Ecs.Add(playing, new FadeIn());
    }
}

/// <summary>The track of the state the game is in, fading in, and kept playing once it has.</summary>
[Behavior]
public partial struct FadeIn
{
    /// <summary>Whether it has faded all the way in.</summary>
    public bool Done;

    /// <summary>Louder with the time since the state changed, to full once the fade's two seconds are over.</summary>
    [OnUpdate]
    public void Fade(BehaviorContext ctx)
    {
        if (Done) return;
        var elapsed = Soundtrack.StateTimer.Elapsed;
        Audio.SetVolume(ctx.Entity, Math.Min(elapsed / Soundtrack.FadeTime, 1f));
        Done = elapsed >= Soundtrack.FadeTime;
    }
}

/// <summary>The track of the state the game left, fading out, and gone once it has.</summary>
[Behavior]
public partial struct FadeOut
{
    /// <summary>Quieter with the time since the state changed, and despawned once the fade's two seconds are over.</summary>
    [OnUpdate]
    public void Fade(BehaviorContext ctx)
    {
        var elapsed = Soundtrack.StateTimer.Elapsed;
        Audio.SetVolume(ctx.Entity, Math.Max(1f - elapsed / Soundtrack.FadeTime, 0f));
        if (elapsed >= Soundtrack.FadeTime) ctx.Cmd.Despawn(ctx.Entity);
    }
}
