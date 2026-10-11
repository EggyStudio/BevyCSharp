// Bevy's pitch example, examples/audio/pitch.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Sound;

// A tone of one frequency, a second long, played at each press of Space, from 220 hertz, the up and
// down arrows raising and lowering it a semitone, each tone logged with how many have been made.
internal static class PitchExample
{
    private static readonly float Semitone = MathF.Pow(2f, 1f / 12f);

    private static float _frequency = 220f;
    private static int _made;

    public static void Build(App app) =>
        app.Update(KeyboardInputSystem, "pitch.KeyboardInputSystem");

    // Bevy's keyboard_input_system and play_pitch, the tone made and played as it is asked for.
    private static void KeyboardInputSystem(BehaviorContext ctx)
    {
        var input = ctx.Input;
        if (input.KeyPressed(Key.ArrowUp)) _frequency *= Semitone;
        if (input.KeyPressed(Key.ArrowDown)) _frequency /= Semitone;
        if (!input.KeyPressed(Key.Space)) return;

        Log.Info($"playing pitch with frequency: {_frequency}");
        Audio.Play(Audio.CreatePitch(_frequency, TimeSpan.FromSeconds(1)), AudioSettings.Effect);
        Log.Info($"number of pitch assets: {++_made}");
    }
}
