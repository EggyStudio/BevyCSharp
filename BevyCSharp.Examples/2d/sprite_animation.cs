using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Animates two sprites once each time an arrow key is pressed, the left one at ten frames a second
// and the right one at twenty.
internal static class SpriteAnimation
{
    private const uint First = 1;
    private const uint Last = 6;

    private sealed class Animation(Entity sprite, float fps)
    {
        public Entity Sprite { get; } = sprite;
        public float FrameTime { get; } = 1f / fps;
        public uint Index { get; set; } = First;

        // Bevy's one-shot frame timer, which runs out once and is set going again for each frame.
        public float? Remaining { get; set; } = 1f / fps;
    }

    private static Animation? _left, _right;
    private static AssetHandle _texture, _layout;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render2d.SpawnCamera2d();
            Ui.SpawnText("Left Arrow: Animate Left Sprite\nRight Arrow: Animate Right Sprite",
                new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });

            _texture = AssetServer.LoadImage("textures/rpg/chars/gabe/gabe-idle-run.png", new TextureSettings());
            _layout = Render2d.CreateAtlas(24, 24, 7, 1);
            _left = new Animation(Spawn(ecs, -70f), 10f);
            _right = new Animation(Spawn(ecs, 70f), 20f);
        }, "sprite_animation.Setup");

        app.Update(ctx =>
        {
            if (ctx.Input.KeyPressed(Key.ArrowLeft)) _left!.Remaining = _left.FrameTime;
            if (ctx.Input.KeyPressed(Key.ArrowRight)) _right!.Remaining = _right.FrameTime;

            foreach (var animation in new[] { _left!, _right! })
            {
                if (animation.Remaining is not { } remaining) continue;
                remaining -= ctx.Time.Delta;
                if (remaining > 0f)
                {
                    animation.Remaining = remaining;
                    continue;
                }

                // At the last frame it goes back to the first and stops until a key starts it.
                if (animation.Index == Last)
                {
                    animation.Index = First;
                    animation.Remaining = null;
                }
                else
                {
                    animation.Index++;
                    animation.Remaining = animation.FrameTime;
                }

                Render2d.SetSprite(ctx.Ecs, animation.Sprite, _texture, new SpriteSettings { Atlas = _layout, Frame = animation.Index });
            }
        }, "sprite_animation.ExecuteAnimations");
    }

    private static Entity Spawn(EcsWorld ecs, float x)
    {
        var sprite = ecs.Spawn();
        ecs.Add(sprite, new Transform(new Vec3(x, 0f, 0f), Quat.Identity, new Vec3(6f)));
        Render2d.SetSprite(ecs, sprite, _texture, new SpriteSettings { Atlas = _layout, Frame = First });
        return sprite;
    }
}
