// Bevy's sprite_material example, examples/shader/sprite_material.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;

namespace BevyCSharp.Examples.Shading;

// A sprite drawn with a material of its own, which dissolves the Bevy bird through noise and brings
// it back, a burning edge between, each press of Space turning it round.
internal static class SpriteMaterialExample
{
    public static void Build(App app) => app.Startup(Setup, "sprite_material.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        Ui.SpawnText("Space to dissolve", new UiSettings());

        var program = Shaders.CreateProgram(new ShaderProgramSettings { Fragment2d = "shaders/sprite_material.slang" });

        // No alpha mode, so the sprite blends as a sprite does.
        var dissolve = Shaders.CreateMaterial2d(program)
            .Set("burn_edge_color", new Vector4(1f, 0.66f, 0f, 1f))
            .Set("amount", 0f)
            .Set("burn_edge", 0.05f);

        var bird = ecs.Spawn();
        ecs.Add(bird, Transform.Identity);
        Render2d.SetSprite(ecs, bird, AssetServer.Load(AssetKind.Image, "branding/bevy_bird_dark.png"));
        Render2d.SetMaterial(ecs, bird, dissolve);
        ecs.Add(bird, new DissolveState());
    }
}

/// <summary>A way a sprite's dissolve can stand.</summary>
public enum DissolveKind { Visible, Dissolving, Appearing, Hidden }

/// <summary>Where a sprite's dissolve is, Bevy's example's <c>DissolveState</c>.</summary>
[Behavior]
public partial struct DissolveState
{
    /// <summary>Shown, going, coming back or gone.</summary>
    public DissolveKind Kind;

    /// <summary>How far a dissolve or a return has gone, from 0 to 1.</summary>
    public float Progress;

    /// <summary>
    /// Bevy's dissolve_on_input and update_dissolve, chained as Bevy chains them. Space starts a
    /// dissolve or a return, or turns one round from where it is, and the one going on moves by the
    /// frame's time and sets the sprite's material.
    /// </summary>
    [OnUpdate]
    public void UpdateDissolve(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.Space))
        {
            (Kind, Progress) = Kind switch
            {
                DissolveKind.Visible => (DissolveKind.Dissolving, 0f),
                DissolveKind.Hidden => (DissolveKind.Appearing, 0f),
                DissolveKind.Dissolving => (DissolveKind.Appearing, 1f - Progress),
                _ => (DissolveKind.Dissolving, 1f - Progress),
            };
        }

        if (Kind is DissolveKind.Visible or DissolveKind.Hidden) return;

        var dissolving = Kind == DissolveKind.Dissolving;
        Progress += ctx.Time.Delta;
        if (Progress >= 1f)
        {
            Progress = 1f;
            Kind = dissolving ? DissolveKind.Hidden : DissolveKind.Visible;
        }

        // The amount is the progress one way and its rest the other.
        Shaders.MaterialOn(ctx.Entity).Set("amount", dissolving ? Progress : 1f - Progress);
    }
}
