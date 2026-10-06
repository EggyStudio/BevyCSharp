// Bevy's alter_sprite example, examples/asset/alter_sprite.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Assets;

// Shows the two ways to change what a sprite shows: Space points the right sprite at a different
// image, swapping bird and logo, and Return changes the left sprite's image itself, inverting every
// byte of it, which every sprite showing that image would show.
internal static class AlterSprite
{
    public static void Build(App app) => app.Startup(Setup, "alter_sprite.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        // Both sprites carry which bird they show, and the left one is marked so the two can be
        // told apart, as Bevy's are.
        var (left, right) = (ecs.Spawn(), ecs.Spawn());
        ecs.Add(left, Transform.At(-200f, 0f, 0f));
        ecs.Add(right, Transform.At(200f, 0f, 0f));
        ecs.SetName(left, "Bird Left");
        ecs.SetName(right, "Bird Right");
        ecs.Add(left, new Bird());
        ecs.Add(right, new Bird());
        ecs.Add(left, new Left());
        Render2d.SetSprite(ecs, left, AssetServer.Load(AssetKind.Image, Bird.TexturePath(BirdKind.Normal)));
        Render2d.SetSprite(ecs, right, AssetServer.Load(AssetKind.Image, Bird.TexturePath(BirdKind.Normal)));

        Ui.SpawnText("Space: swap the right sprite's image handle\nReturn: modify the image Asset of the left sprite, affecting all uses of it",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }
}

/// <summary>Which of the two images a bird shows.</summary>
public enum BirdKind
{
    /// <summary>Bevy's bird.</summary>
    Normal,

    /// <summary>Bevy's logo.</summary>
    Logo,
}

/// <summary>A sprite showing a bird, and which one, as Bevy's <c>Bird</c> keeps it.</summary>
[Behavior]
public partial struct Bird
{
    /// <summary>Which image it shows.</summary>
    public BirdKind Kind;

    /// <summary>The image a kind of bird is drawn from.</summary>
    public static string TexturePath(BirdKind kind) =>
        kind == BirdKind.Normal ? "branding/bevy_bird_dark.png" : "branding/bevy_logo_dark.png";

    /// <summary>Space points the right bird at the other image, as Bevy's <c>alter_handle</c> does.</summary>
    [OnUpdate]
    [Without(typeof(Left))]
    public void AlterHandle(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space)) return;
        Kind = Kind == BirdKind.Normal ? BirdKind.Logo : BirdKind.Normal;
        Render2d.SetSprite(ctx.Ecs, ctx.Entity, AssetServer.Load(AssetKind.Image, TexturePath(Kind)));
    }
}

/// <summary>The bird on the left, whose image itself is changed.</summary>
[Behavior]
public partial struct Left
{
    /// <summary>
    /// Return turns every byte of the left bird's image to its opposite, alpha as well, as Bevy's
    /// <c>alter_asset</c> does, which every sprite showing that image shows.
    /// </summary>
    [OnUpdate]
    public void AlterAsset(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Enter)) return;

        var image = AssetServer.Load(AssetKind.Image, Bird.TexturePath(ctx.Ecs.GetOrDefault<Bird>(ctx.Entity).Kind));
        if (!Render.TryReadImage(image, out var pixels)) return;
        Render.WriteImagePixels(image, pixels!.Data.Select(b => (byte)(255 - b)).ToArray());
    }
}
