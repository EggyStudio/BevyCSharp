// Bevy's 2d_on_ui example, examples/camera/2d_on_ui.rs at v0.19.1, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Cameras;

// Shows how to draw 2D over the interface, a sprite turning in front of a panel because a second
// camera, drawn after the first and clearing nothing, sees only the sprite's layer.
internal static class TwoDOnUi
{
    private static Entity _sprite;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;

            // The interface draws with the first camera, and the second draws layer one over it.
            var under = Render2d.SpawnCamera2d();
            ecs.Insert<IsDefaultUiCameraRef>(under);
            var over = Render2d.SpawnCamera2d(order: 1);
            ecs.Wrap<CameraRef>(over).ClearColor = new ClearColorConfig.None();
            Render.SetLayers(ecs, over, 1u << 1);

            var panel = Ui.SpawnNode(new UiSettings
            {
                Width = Length.Percent(100f),
                Height = Length.Percent(100f),
                Justify = UiJustify.Center,
                Align = UiAlign.Center,
                Color = Color.FromSrgb8(251, 113, 133),
            });
            var frame = Ui.SpawnNode(new UiSettings
            {
                Height = Length.Percent(30f),
                Width = Length.Percent(20f),
                MinHeight = Length.Px(150f),
                MinWidth = Length.Px(150f),
                Border = Sides.All(Length.Px(2f)),
                BorderColor = (1f, 1f, 1f, 1f),
                Corners = Corners.All(Length.Percent(25f)),
            });
            ecs.SetParent(frame, panel);

            _sprite = ecs.Spawn();
            ecs.Add(_sprite, Transform.Identity);
            Render2d.SetSprite(ecs, _sprite, AssetServer.Load(AssetKind.Image, "textures/rpg/chars/sensei/sensei.png"), new SpriteSettings { Size = (100f, 100f) });
            Render.SetLayers(ecs, _sprite, 1u << 1);
        }, "2d_on_ui.Setup");

        app.Update(ctx =>
        {
            var transform = ctx.Ecs.GetOrDefault<Transform>(_sprite);
            transform.Rotation *= Quat.FromRotationZ(ctx.Time.Delta * 0.5f) * Quat.FromRotationY(ctx.Time.Delta);
            ctx.Ecs.Set(_sprite, transform);
        }, "2d_on_ui.RotateSprite");
    }
}
