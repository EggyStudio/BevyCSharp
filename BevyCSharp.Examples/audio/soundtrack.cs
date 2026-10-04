using Bevy;

namespace BevyCSharp.Examples.Sound;

// Shows how to play a soundtrack that follows the game's state, a calm track and a battle track
// swapped every ten seconds, the new one fading in over two while the old one fades out.
internal static class Soundtrack
{
    private enum GameState { Peaceful, Battle }

    private const float FadeTime = 2f;
    private const float StateTime = 10f;

    private static readonly List<Entity> FadingIn = [];
    private static readonly List<Entity> FadingOut = [];
    private static AssetHandle[] _tracks = [];
    private static GameState _state;
    private static float _timer;
    private static bool _changed;

    public static void Build(App app)
    {
        app.Startup(_ =>
        {
            FadingIn.Clear();
            FadingOut.Clear();
            (_state, _timer, _changed) = (GameState.Peaceful, 0f, true);
            _tracks =
            [
                AssetServer.Load(AssetKind.Audio, "sounds/Mysterious acoustic guitar.ogg"),
                AssetServer.Load(AssetKind.Audio, "sounds/Epic orchestra music.ogg"),
            ];
        }, "soundtrack.Setup");

        app.Update(ctx =>
        {
            // The state changes each time ten seconds pass.
            _timer += ctx.Time.Delta;
            if (_timer >= StateTime)
            {
                _timer -= StateTime;
                _state = _state == GameState.Battle ? GameState.Peaceful : GameState.Battle;
                _changed = true;
            }

            // A change fades out whatever is playing and starts the state's track, silent.
            if (_changed)
            {
                _changed = false;
                FadingOut.AddRange(FadingIn);
                FadingIn.Clear();
                FadingIn.Add(Audio.Play(_tracks[(int)_state], new AudioSettings { Mode = PlaybackMode.Loop, Volume = 0f }));
            }

            // Both fades follow the time since the state changed, as Bevy's follow its timer.
            var faded = Math.Min(_timer / FadeTime, 1f);
            foreach (var track in FadingIn) Audio.SetVolume(track, faded);
            foreach (var track in FadingOut) Audio.SetVolume(track, 1f - faded);
            if (_timer < FadeTime) return;

            foreach (var track in FadingOut) Audio.Stop(track);
            FadingOut.Clear();
        }, "soundtrack.ChangeTrackAndFade");
    }
}
