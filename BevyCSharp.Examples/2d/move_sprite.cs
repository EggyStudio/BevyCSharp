// Bevy's move_sprite example, examples/2d/move_sprite.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Renders a sprite moving back and forth, turning at two hundred pixels either side.
internal static class MoveSprite
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        Render2d.SpawnCamera2d();
        var logo = ctx.Ecs.Spawn();
        ctx.Ecs.Add(logo, Transform.Identity);
        Render2d.SetSprite(ctx.Ecs, logo, AssetServer.Load(AssetKind.Image, "branding/icon.png"));
        ctx.Ecs.Add(logo, new Direction { Kind = DirectionKind.Right });
    }, "move_sprite.Setup");
}

/// <summary>A way a sprite can move along X.</summary>
public enum DirectionKind { Left, Right }

/// <summary>The way a sprite is moving, as Bevy's <c>Direction</c> enum keeps it on the sprite.</summary>
[Behavior]
public partial struct Direction
{
    /// <summary>Left or right.</summary>
    public DirectionKind Kind;

    /// <summary>Moved a hundred and fifty pixels a second its way, and turned two hundred either side.</summary>
    [OnUpdate]
    public void SpriteMovement(BehaviorContext ctx, ref Transform transform)
    {
        transform.Translation.X += (Kind == DirectionKind.Right ? 150f : -150f) * ctx.Time.Delta;
        if (transform.Translation.X > 200f) Kind = DirectionKind.Left;
        else if (transform.Translation.X < -200f) Kind = DirectionKind.Right;
    }
}
