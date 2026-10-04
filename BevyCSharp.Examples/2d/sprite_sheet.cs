using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Renders an animated sprite by stepping through a sprite sheet, a character running in place, a
// frame each tenth of a second.
internal static class SpriteSheet
{
    private const uint First = 1;
    private const uint Last = 6;

    private static Entity _sprite;
    private static AssetHandle _texture, _layout;
    private static uint _index;
    private static float _timer;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            Render2d.SpawnCamera2d();
            (_index, _timer) = (First, 0f);

            // Read at its nearest pixel, as Bevy's ImagePlugin::default_nearest has every image,
            // so the pixel art stays sharp scaled six times.
            _texture = AssetServer.LoadImage("textures/rpg/chars/gabe/gabe-idle-run.png", new TextureSettings());
            _layout = Render2d.CreateAtlas(24, 24, 7, 1);
            _sprite = ctx.Ecs.Spawn();
            ctx.Ecs.Add(_sprite, new Transform(Vec3.Zero, Quat.Identity, new Vec3(6f)));
            Show(ctx.Ecs);
        }, "sprite_sheet.Setup");

        app.Update(ctx =>
        {
            _timer += ctx.Time.Delta;
            if (_timer < 0.1f) return;
            _timer -= 0.1f;
            _index = _index == Last ? First : _index + 1;
            Show(ctx.Ecs);
        }, "sprite_sheet.AnimateSprite");
    }

    private static void Show(EcsWorld ecs) =>
        Render2d.SetSprite(ecs, _sprite, _texture, new SpriteSettings { Atlas = _layout, Frame = _index });
}
