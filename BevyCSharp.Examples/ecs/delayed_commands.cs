// Bevy's delayed_commands example, examples/ecs/delayed_commands.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Ecs;

// This example demonstrates how to send commands which will take effect after a period of time,
// through a grid of clickable squares with ripples created when you click. Each square turns white
// and then black again after a delay that grows with its distance from the click, so a ring runs out
// from where the pointer was.
internal static class DelayedCommands
{
    private const float SquareSize = 45f;

    private static Entity _camera;

    public static void Build(App app) => app.Startup(Spawn, "delayed_commands.Spawn");

    private static void Spawn(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _camera = Render2d.SpawnCamera2d();

        for (var x = -5; x <= 5; x++)
        {
            for (var y = -5; y <= 5; y++)
            {
                var square = ecs.Spawn();
                ecs.Add(square, Transform.At(x * 50f, y * 50f, 0f));
                // Bevy's Sprite::from_color, a sprite of one color and a size.
                var sprite = ecs.Insert<SpriteRef>(square);
                (sprite.Color, sprite.CustomSize) = (Color.Black, new Vec2(SquareSize, SquareSize));
                ecs.Add(square, new BlinkySquare());
            }
        }

        // Bevy's click observer, over whatever was clicked.
        ecs.Observe<Pointer<Click>>(on => Click(on.Ecs, on.World.Resource<EcsCommands>(), on.Event.Position));
    }

    // Each square's blink queued with a delay of its distance from the click, a thousandth of a
    // second a unit.
    private static void Click(EcsWorld ecs, EcsCommands commands, Vec2 pointer)
    {
        if (!Render.TryRay(_camera, pointer.X, pointer.Y, out var origin, out _)) return;
        var clicked = new Vec2(origin.X, origin.Y);

        foreach (var square in ecs.All())
        {
            if (!ecs.Has<BlinkySquare>(square) || !ecs.TryGet<Transform>(square, out var transform)) continue;

            var delay = (clicked - new Vec2(transform.Translation.X, transform.Translation.Y)).Length / 1000f;
            commands.Delayed(delay).Run(world => world.Wrap<SpriteRef>(square).Color = Color.White);
            commands.Delayed(delay + 0.1f).Run(world => world.Wrap<SpriteRef>(square).Color = Color.Black);
        }
    }
}

/// <summary>A square of the grid, which blinks as the ripple passes, Bevy's <c>BlinkySquare</c>.</summary>
[Behavior]
public partial struct BlinkySquare;
