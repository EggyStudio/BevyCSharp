// Bevy's play_sound_effect example, examples/audio/play_sound_effect.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Sound;

// Shows how to play a sound effect in response to an event, a knock each time Space is pressed,
// loaded once and played as many times as asked.
internal static class PlaySoundEffect
{
    private static AssetHandle _effect;

    public static void Build(App app)
    {
        app.Startup(_ =>
        {
            _effect = AssetServer.Load(AssetKind.Audio, "sounds/breakout_collision.ogg");
            Render2d.SpawnCamera2d();
            Ui.SpawnText("Press Space to play the sound effect.", new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
        }, "play_sound_effect.Setup");

        // A sound played once goes when it ends, as Bevy's PlaybackSettings::DESPAWN has it.
        app.Update(ctx =>
        {
            if (ctx.Input.KeyPressed(Key.Space)) Audio.Play(_effect, new AudioSettings { Mode = PlaybackMode.Despawn });
        }, "play_sound_effect.KeyboardEvent");
    }
}
