// Bevy's sprite_picking example, examples/picking/sprite_picking.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Pointers;

// Demonstrates picking for sprites and sprite atlases. A grid of Bevy's bird, one for each of the
// nine anchors, each over a black square that shows where its anchor is, drifts about, and a
// running character from a sprite sheet stands beside it. Each turns one color under the pointer,
// another while pressed and back when the pointer leaves, Bevy's sprite picking counting a sprite
// only where the pointer is over one of its opaque pixels.
internal static class SpritePicking
{
    private static Entity _grid;

    public static void Build(App app)
    {
        app.Startup(Setup, "sprite_picking.Setup");
        app.Startup(SetupAtlas, "sprite_picking.SetupAtlas");
        app.Update(MoveSprite, "sprite_picking.MoveSprite");
    }

    // A 3 by 3 grid of the bird, each with an anchor of its own, scaled more to the right and turned
    // more toward the top, over a black square at its anchor.
    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        const float Length = 128f;
        var size = (Length / 2f, Length / 2f);
        var white = Render.CreateImage([255, 255, 255, 255], 1, 1);
        var bird = AssetServer.LoadImage("branding/bevy_bird_dark.png", new TextureSettings());

        _grid = ecs.Spawn();
        ecs.Add(_grid, Transform.Identity);

        Vec2[] anchors =
        [
            new(-0.5f, 0.5f), new(0f, 0.5f), new(0.5f, 0.5f),
            new(-0.5f, 0f), new(0f, 0f), new(0.5f, 0f),
            new(-0.5f, -0.5f), new(0f, -0.5f), new(0.5f, -0.5f),
        ];

        for (var index = 0; index < anchors.Length; index++)
        {
            var (i, j) = (index % 3, index / 3);

            var square = ecs.Spawn();
            ecs.Add(square, Transform.At(i * Length - Length, j * Length - Length, -1f));
            Render2d.SetSprite(ecs, square, white, new SpriteSettings { Color = (0f, 0f, 0f, 1f), Size = size });
            ecs.Insert<PickableRef>(square);
            ecs.SetParent(square, _grid);
            RecolorOn(ecs, square, over: Color.FromSrgb(0f, 1f, 1f), off: Color.FromSrgb(0f, 0f, 0f), pressed: Color.FromSrgb(1f, 1f, 0f));

            var sprite = ecs.Spawn();
            ecs.Add(sprite, new Transform(
                new Vec3(i * Length - Length, j * Length - Length, 0f),
                Quat.FromRotationZ((j - 1) * 0.2f),
                new Vec3(1f + (i - 1) * 0.2f)));
            Render2d.SetSprite(ecs, sprite, bird, new SpriteSettings { Color = (1f, 0f, 0f, 1f), Size = size });
            ecs.Insert<AnchorRef>(sprite).Value = anchors[index];
            ecs.Insert<PickableRef>(sprite);
            ecs.SetParent(sprite, _grid);
            RecolorOn(ecs, sprite, over: Color.FromSrgb(0f, 1f, 0f), off: Color.FromSrgb(1f, 0f, 0f), pressed: Color.FromSrgb(0f, 0f, 1f));
        }
    }

    // The character running in place from the sheet, its frames those of the run.
    private static void SetupAtlas(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var texture = AssetServer.LoadImage("textures/rpg/chars/gabe/gabe-idle-run.png", new TextureSettings());
        var indices = new PickedAnimationIndices { First = 1, Last = 6 };

        var sprite = ecs.Spawn();
        ecs.Add(sprite, new Transform(new Vec3(300f, 0f, 0f), Quat.Identity, new Vec3(6f)));
        Render2d.SetSprite(ecs, sprite, texture, new SpriteSettings { Atlas = Render2d.CreateAtlas(24, 24, 7, 1), Frame = (uint)indices.First });
        ecs.Add(sprite, indices);
        ecs.Add(sprite, new PickedAnimationTimer { Timer = GameTimer.FromSeconds(0.1f, TimerMode.Repeating) });
        ecs.Insert<PickableRef>(sprite);
        RecolorOn(ecs, sprite, over: Color.FromSrgb(0f, 1f, 1f), off: Color.FromSrgb(1f, 1f, 1f), pressed: Color.FromSrgb(1f, 1f, 0f));
    }

    // Bevy's recolor_on, for the pointer coming over, going off, pressing and letting go.
    private static void RecolorOn(EcsWorld ecs, Entity entity, Color over, Color off, Color pressed)
    {
        ecs.Observe<Pointer<Over>>(entity, on => on.Ecs.Wrap<SpriteRef>(on.Entity).Color = over);
        ecs.Observe<Pointer<Out>>(entity, on => on.Ecs.Wrap<SpriteRef>(on.Entity).Color = off);
        ecs.Observe<Pointer<Press>>(entity, on => on.Ecs.Wrap<SpriteRef>(on.Entity).Color = pressed);
        ecs.Observe<Pointer<Release>>(entity, on => on.Ecs.Wrap<SpriteRef>(on.Entity).Color = over);
    }

    // The grid drifting along a figure of eight, a tenth as fast as time.
    private static void MoveSprite(BehaviorContext ctx)
    {
        var t = ctx.Time.Elapsed * 0.1f;
        var transform = ctx.Ecs.GetOrDefault<Transform>(_grid);
        transform.Translation = new Vec3(50f * MathF.Sin(t), 50f * MathF.Sin(t * 2f), transform.Translation.Z);
        ctx.Ecs.Set(_grid, transform);
    }
}

/// <summary>
/// The frames of the character's run on its sheet, Bevy's <c>AnimationIndices</c> under another
/// name since sprite_sheet's runs the same way in another namespace.
/// </summary>
[Behavior]
public partial struct PickedAnimationIndices
{
    /// <summary>The first frame.</summary>
    public int First;

    /// <summary>The last frame.</summary>
    public int Last;
}

/// <summary>
/// The time each frame of the run is shown, Bevy's <c>AnimationTimer</c> under another name.
/// </summary>
[Behavior]
public partial struct PickedAnimationTimer
{
    /// <summary>A tenth of a second, over and over.</summary>
    public GameTimer Timer;

    /// <summary>
    /// On to the next frame each time the timer runs out, from the last back to the first.
    /// </summary>
    [OnUpdate]
    public void AnimateSprite(BehaviorContext ctx, in PickedAnimationIndices indices)
    {
        if (!Timer.Tick(ctx.Time.Delta).JustFinished || ctx.Ecs.Wrap<SpriteRef>(ctx.Entity).TextureAtlas is not { } atlas) return;
        Render2d.SetSpriteFrames([ctx.Entity], [(int)atlas.Index == indices.Last ? (uint)indices.First : (uint)atlas.Index + 1]);
    }
}
