// Bevy's sprite_scale example, examples/2d/sprite_scale.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.TwoD;

// Shows the ways a sprite's picture meets a size it does not fit, stretched, filled from the
// middle, the start or the end, and fitted the same three ways, for still pictures and for frames
// of a running character.
internal static class SpriteScale
{

    private static readonly (float W, float H, string Text, float X, float Y, bool Banner, SpriteScaling? Scaling)[] Pictures =
    [
        (100f, 225f, "Stretched", -570f, 230f, false, null),
        (100f, 225f, "Fill Center", -450f, 230f, false, SpriteScaling.FillCenter),
        (100f, 225f, "Fill Start", -330f, 230f, false, SpriteScaling.FillStart),
        (100f, 225f, "Fill End", -210f, 230f, false, SpriteScaling.FillEnd),
        (300f, 100f, "Fill Start Horizontal", 10f, 290f, false, SpriteScaling.FillStart),
        (300f, 100f, "Fill End Horizontal", 10f, 155f, false, SpriteScaling.FillEnd),
        (200f, 200f, "Fill Center", 280f, 230f, true, SpriteScaling.FillCenter),
        (200f, 100f, "Fill Center", 500f, 230f, false, SpriteScaling.FillCenter),
        (100f, 100f, "Stretched", -570f, -40f, true, null),
        (200f, 200f, "Fit Center", -400f, -40f, true, SpriteScaling.FitCenter),
        (200f, 200f, "Fit Start", -180f, -40f, true, SpriteScaling.FitStart),
        (200f, 200f, "Fit End", 40f, -40f, true, SpriteScaling.FitEnd),
        (100f, 200f, "Fit Center", 210f, -40f, true, SpriteScaling.FitCenter),
    ];

    private static readonly (float W, float H, string Text, float X, float Y, SpriteScaling? Scaling)[] Sheets =
    [
        (120f, 50f, "Stretched", -570f, -200f, null),
        (120f, 50f, "Fill Center", -570f, -300f, SpriteScaling.FillCenter),
        (120f, 50f, "Fill Start", -430f, -200f, SpriteScaling.FillStart),
        (120f, 50f, "Fill End", -430f, -300f, SpriteScaling.FillEnd),
        (50f, 120f, "Fill Center", -300f, -250f, SpriteScaling.FillCenter),
        (50f, 120f, "Fill Start", -190f, -250f, SpriteScaling.FillStart),
        (50f, 120f, "Fill End", -90f, -250f, SpriteScaling.FillEnd),
        (120f, 50f, "Fit Center", 20f, -200f, SpriteScaling.FitCenter),
        (120f, 50f, "Fit Start", 20f, -300f, SpriteScaling.FitStart),
        (120f, 50f, "Fit End", 160f, -200f, SpriteScaling.FitEnd),
    ];

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var square = AssetServer.Load(AssetKind.Image, "textures/slice_square_2.png");
        var banner = AssetServer.Load(AssetKind.Image, "branding/banner.png");
        foreach (var (w, h, text, x, y, isBanner, scaling) in Pictures)
            Labeled(ecs, isBanner ? banner : square, Settings(w, h, scaling), text, x, y, h);

        // Each running character with the frames of its sheet and a tenth of a second for each.
        var gabe = AssetServer.Load(AssetKind.Image, "textures/rpg/chars/gabe/gabe-idle-run.png");
        var layout = Render2d.CreateAtlas(24, 24, 7, 1);
        foreach (var (w, h, text, x, y, scaling) in Sheets)
        {
            var settings = Settings(w, h, scaling);
            settings.Atlas = layout;
            var sprite = Labeled(ecs, gabe, settings, text, x, y, h);
            ecs.Add(sprite, new ScaleAnimationIndices { First = 0, Last = 6 });
            ecs.Add(sprite, new ScaleAnimationTimer { Timer = GameTimer.FromSeconds(0.1f, TimerMode.Repeating) });
        }
    }, "sprite_scale.Setup");

    private static SpriteSettings Settings(float width, float height, SpriteScaling? scaling) => new()
    {
        Size = (width, height),
        Mode = scaling is null ? SpriteImageMode.Auto : SpriteImageMode.Scaled,
        Scaling = scaling ?? SpriteScaling.FitCenter,
    };

    // A sprite with its label in small text under it, hung by the label's top middle.
    private static Entity Labeled(EcsWorld ecs, AssetHandle image, SpriteSettings settings, string text, float x, float y, float height)
    {
        var sprite = ecs.Spawn();
        ecs.Add(sprite, Transform.At(x, y, 0f));
        Render2d.SetSprite(ecs, sprite, image, settings);

        var label = ecs.Spawn();
        ecs.Add(label, Transform.At(0f, -0.5f * height - 10f, 0f));
        ecs.Insert<Text2dRef>(label).Value = text;
        ecs.Insert<TextFontRef>(label).FontSize = new FontSize.Px(15f);
        ecs.Insert<TextLayoutRef>(label).Justify = TextLayoutRef.JustifyVariant.Center;
        ecs.Insert<AnchorRef>(label).Value = new Vec2(0f, 0.5f);
        ecs.SetParent(label, sprite);
        return sprite;
    }
}

/// <summary>
/// The frames of a sprite's animation on its sheet, Bevy's <c>AnimationIndices</c> under another
/// name since sprite_sheet's shares the namespace.
/// </summary>
[Behavior]
public partial struct ScaleAnimationIndices
{
    /// <summary>The first frame.</summary>
    public int First;

    /// <summary>The last frame.</summary>
    public int Last;
}

/// <summary>
/// The time each frame of a sprite's animation is shown, Bevy's <c>AnimationTimer</c> under another
/// name since sprite_sheet's shares the namespace.
/// </summary>
[Behavior]
public partial struct ScaleAnimationTimer
{
    /// <summary>A tenth of a second, over and over.</summary>
    public GameTimer Timer;

    /// <summary>On to the next frame each time the timer runs out, from the last back to the first.</summary>
    [OnUpdate]
    public void AnimateSprite(BehaviorContext ctx, in ScaleAnimationIndices indices)
    {
        if (!Timer.Tick(ctx.Time.Delta).JustFinished || ctx.Ecs.Wrap<SpriteRef>(ctx.Entity).TextureAtlas is not { } atlas) return;
        Render2d.SetSpriteFrames([ctx.Entity], [(int)atlas.Index == indices.Last ? (uint)indices.First : (uint)atlas.Index + 1]);
    }
}
