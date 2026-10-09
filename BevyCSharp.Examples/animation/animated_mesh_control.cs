// Bevy's animated_mesh_control example, examples/animation/animated_mesh_control.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Animations;

// Plays the fox's run, walk and survey, controlled from the keyboard: paused and resumed, sped up
// and slowed, moved back and forward, played a number of times or for ever, and changed for the
// next with a quarter second's blend.
internal static class AnimatedMeshControl
{
    private static readonly string[] Clips = ["Run", "Walk", "Survey"];

    private static Entity _fox;
    private static bool _started;
    private static int _current;

    public static void Build(App app)
    {
        (_fox, _started, _current) = (Entity.None, false, 0);
        app.Startup(ctx =>
        {
            FoxScene.Setup(ctx.Ecs);
            Ui.SpawnText("space: play / pause\nup / down: playback speed\nleft / right: seek\n1-3: play N times\nL: loop forever\nreturn: change animation\n",
                new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        }, "animated_mesh_control.Setup");
        app.SpawnGltf("models/animated/Fox.glb", (_, root) => _fox = root);
        app.Update(KeyboardControl, "animated_mesh_control.KeyboardControl");
    }

    private static void KeyboardControl(BehaviorContext ctx)
    {
        if (_fox == Entity.None) return;
        if (!_started)
        {
            _started = Animation.Play(_fox, Clips[0], new AnimationSettings { Repeat = true });
            return;
        }

        var input = ctx.Input;
        if (Animation.StateOf(_fox) is not { } state) return;

        if (input.KeyPressed(Key.Space))
        {
            if (state.Paused) Animation.Resume(_fox);
            else Animation.Pause(_fox);
        }

        if (input.KeyPressed(Key.ArrowUp)) Animation.SetSpeed(_fox, state.Speed * 1.2f);
        if (input.KeyPressed(Key.ArrowDown)) Animation.SetSpeed(_fox, state.Speed * 0.8f);
        if (input.KeyPressed(Key.ArrowLeft)) Animation.Seek(_fox, MathF.Max(state.Seconds - 0.1f, 0f));
        if (input.KeyPressed(Key.ArrowRight)) Animation.Seek(_fox, state.Seconds + 0.1f);

        if (input.KeyPressed(Key.Enter))
        {
            _current = (_current + 1) % Clips.Length;
            Animation.Play(_fox, Clips[_current], new AnimationSettings { Repeat = true, Blend = 0.25f });
        }

        // Played that many times from its start, as Bevy's set_repeat and replay do.
        foreach (var (key, times) in new[] { (Key.Digit1, 1u), (Key.Digit2, 2u), (Key.Digit3, 3u) })
        {
            if (input.KeyPressed(key)) Animation.Play(_fox, Clips[_current], new AnimationSettings { Times = times, Speed = state.Speed });
        }

        if (input.KeyPressed(Key.L)) Animation.SetRepeat(_fox, 0);
    }
}
