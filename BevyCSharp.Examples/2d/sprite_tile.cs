// Bevy's sprite_tile example, examples/2d/sprite_tile.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Displays a sprite tiled across a size that grows and shrinks, the logo repeating every 128
// pixels rather than stretching.
internal static class SpriteTile
{
    private const float Min = 128f;
    private const float Max = 512f;

    private static Entity _sprite;
    private static AssetHandle _logo;
    private static float _current, _speed;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            Render2d.SpawnCamera2d();
            (_current, _speed) = (Min, 50f);
            _logo = AssetServer.Load(AssetKind.Image, "branding/icon.png");
            _sprite = ctx.Ecs.Spawn();
            ctx.Ecs.Add(_sprite, Transform.Identity);
        }, "sprite_tile.Setup");

        app.Update(ctx =>
        {
            if (_current >= Max || _current <= Min) _speed = -_speed;
            _current += _speed * ctx.Time.Delta;

            // Tiled at half its size, so the 256-pixel logo repeats every 128.
            Render2d.SetSprite(ctx.Ecs, _sprite, _logo, new SpriteSettings { Mode = SpriteImageMode.Tiled, TileStretch = 0.5f, Size = (_current, _current) });
        }, "sprite_tile.Animate");
    }
}
