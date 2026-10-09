// Bevy's sprite_sheet example, examples/2d/sprite_sheet.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.TwoD;

// Renders an animated sprite by stepping through a sprite sheet, a character running in place, a
// frame each tenth of a second.
internal static class SpriteSheet
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        // Read at its nearest pixel, as Bevy's ImagePlugin::default_nearest has every image,
        // so the pixel art stays sharp scaled six times.
        var texture = AssetServer.LoadImage("textures/rpg/chars/gabe/gabe-idle-run.png", new TextureSettings());
        var layout = Render2d.CreateAtlas(24, 24, 7, 1);
        var indices = new AnimationIndices { First = 1, Last = 6 };

        var sprite = ecs.Spawn();
        ecs.Add(sprite, new Transform(Vec3.Zero, Quat.Identity, new Vec3(6f)));
        Render2d.SetSprite(ecs, sprite, texture, new SpriteSettings { Atlas = layout, Frame = (uint)indices.First });
        ecs.Add(sprite, indices);
        ecs.Add(sprite, new AnimationTimer { Timer = GameTimer.FromSeconds(0.1f, TimerMode.Repeating) });
    }, "sprite_sheet.Setup");
}

/// <summary>The frames of a sprite's animation on its sheet.</summary>
[Behavior]
public partial struct AnimationIndices
{
    /// <summary>The first frame.</summary>
    public int First;

    /// <summary>The last frame.</summary>
    public int Last;
}

/// <summary>The time each frame of a sprite's animation is shown.</summary>
[Behavior]
public partial struct AnimationTimer
{
    /// <summary>A tenth of a second, over and over.</summary>
    public GameTimer Timer;

    /// <summary>On to the next frame each time the timer runs out, from the last back to the first.</summary>
    [OnUpdate]
    public void AnimateSprite(BehaviorContext ctx, in AnimationIndices indices)
    {
        if (!Timer.Tick(ctx.Time.Delta).JustFinished || ctx.Ecs.Wrap<SpriteRef>(ctx.Entity).TextureAtlas is not { } atlas) return;
        Render2d.SetSpriteFrames([ctx.Entity], [(int)atlas.Index == indices.Last ? (uint)indices.First : (uint)atlas.Index + 1]);
    }
}
