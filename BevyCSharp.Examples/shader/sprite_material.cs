// Bevy's sprite_material example, examples/shader/sprite_material.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;

namespace BevyCSharp.Examples.Shading;

// A sprite drawn with a material of its own, which dissolves the Bevy bird through noise and brings
// it back, a burning edge between, each press of Space turning it round.
internal static class SpriteMaterialExample
{
    // Where the dissolve is, as Bevy's example keeps it in a component of the one sprite.
    private enum State { Visible, Dissolving, Appearing, Hidden }

    private static State _state = State.Visible;
    private static float _progress;
    private static ShaderMaterial _dissolve;

    public static void Build(App app)
    {
        app.Startup(Setup, "sprite_material.Setup");
        app.Update(DissolveOnInput, "sprite_material.DissolveOnInput");
        app.Update(UpdateDissolve, "sprite_material.UpdateDissolve");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        (_state, _progress) = (State.Visible, 0f);
        Render2d.SpawnCamera2d();
        Ui.SpawnText("Space to dissolve", new UiSettings());

        var program = Shaders.CreateProgram(new ShaderProgramSettings { Fragment2d = "shaders/sprite_material.slang" });

        // No alpha mode, so the sprite blends as a sprite does.
        _dissolve = Shaders.CreateMaterial2d(program)
            .Set("burn_edge_color", new Vector4(1f, 0.66f, 0f, 1f))
            .Set("amount", 0f)
            .Set("burn_edge", 0.05f);

        var bird = ecs.Spawn();
        ecs.Add(bird, Transform.Identity);
        Render2d.SetSprite(ecs, bird, AssetServer.Load(AssetKind.Image, "branding/bevy_bird_dark.png"));
        Render2d.SetMaterial(ecs, bird, _dissolve);
    }

    // Space starts a dissolve or a return, or turns one round from where it is.
    private static void DissolveOnInput(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space)) return;

        (_state, _progress) = _state switch
        {
            State.Visible => (State.Dissolving, 0f),
            State.Hidden => (State.Appearing, 0f),
            State.Dissolving => (State.Appearing, 1f - _progress),
            _ => (State.Dissolving, 1f - _progress),
        };
    }

    private static void UpdateDissolve(BehaviorContext ctx)
    {
        if (_state is State.Visible or State.Hidden) return;

        _progress += ctx.Time.Delta;
        if (_progress >= 1f)
        {
            _progress = 1f;
            _state = _state == State.Dissolving ? State.Hidden : State.Visible;
        }

        // Hidden once a dissolve ends, so the amount is the progress one way and its rest the other.
        var dissolving = _state is State.Dissolving or State.Hidden;
        _dissolve.Set("amount", dissolving ? _progress : 1f - _progress);
    }
}
