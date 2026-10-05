using Bevy;

namespace BevyCSharp.Examples.Assets;

// Shows the two ways to change what a sprite shows: Space points the right sprite at a different
// image, swapping bird and logo, and Return changes the left sprite's image itself, inverting every
// byte of it, which every sprite showing that image would show.
internal static class AlterSprite
{
    private static readonly string[] Birds = ["branding/bevy_bird_dark.png", "branding/bevy_logo_dark.png"];

    private static Entity _left, _right;
    private static int _rightBird;

    public static void Build(App app)
    {
        app.Startup(Setup, "alter_sprite.Setup");
        app.Update(AlterHandle, "alter_sprite.AlterHandle");
        app.Update(AlterAsset, "alter_sprite.AlterAsset");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _rightBird = 0;
        Render2d.SpawnCamera2d();

        (_left, _right) = (ecs.Spawn(), ecs.Spawn());
        ecs.Add(_left, Transform.At(-200f, 0f, 0f));
        ecs.Add(_right, Transform.At(200f, 0f, 0f));
        ecs.SetName(_left, "Bird Left");
        ecs.SetName(_right, "Bird Right");
        Render2d.SetSprite(ecs, _left, AssetServer.Load(AssetKind.Image, Birds[0]));
        Render2d.SetSprite(ecs, _right, AssetServer.Load(AssetKind.Image, Birds[0]));

        Ui.SpawnText("Space: swap the right sprite's image handle\nReturn: modify the image Asset of the left sprite, affecting all uses of it",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    private static void AlterHandle(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space)) return;
        _rightBird = 1 - _rightBird;
        Render2d.SetSprite(ctx.Ecs, _right, AssetServer.Load(AssetKind.Image, Birds[_rightBird]));
    }

    // Every byte of the image turned to its opposite, alpha as well, as Bevy's does.
    private static void AlterAsset(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Enter)) return;

        var image = AssetServer.Load(AssetKind.Image, Birds[0]);
        if (!Render.TryReadImage(image, out var pixels)) return;
        Render.WriteImagePixels(image, pixels!.Data.Select(b => (byte)(255 - b)).ToArray());
    }
}
