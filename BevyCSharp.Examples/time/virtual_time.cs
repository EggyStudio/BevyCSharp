// Bevy's virtual_time example, examples/time/virtual_time.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Clocks;

// Shows the game's clock beside the wall's, two logos swinging by each, the gold one by the game's
// clock, which Space pauses and the arrow keys speed up and slow down.
internal static class VirtualTime
{
    private static Entity _real, _virtual, _realText, _virtualText;
    private static float _realElapsed, _sinceText;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_realElapsed, _sinceText) = (0f, 0f);
            ctx.Time.SetSpeed(2f);
            Render2d.SpawnCamera2d();

            var gold = Scene.Srgb8(255, 215, 0);
            var texture = AssetServer.Load(AssetKind.Image, "branding/icon.png");
            _real = ecs.Spawn();
            ecs.Add(_real, new Transform(Vec3.Zero, Quat.Identity, new Vec3(0.5f, 0.5f, 1f)));
            Render2d.SetSprite(ecs, _real, texture);
            _virtual = ecs.Spawn();
            ecs.Add(_virtual, new Transform(new Vec3(0f, -160f, 0f), Quat.Identity, new Vec3(0.5f, 0.5f, 1f)));
            Render2d.SetSprite(ecs, _virtual, texture, new SpriteSettings { Color = gold });

            var row = Ui.SpawnNode(new UiSettings { Absolute = true, Top = Length.Px(0f), Width = Length.Percent(100f), Justify = UiJustify.SpaceBetween, Padding = Sides.All(Length.Px(20f)) });
            _realText = Ui.SpawnText(string.Empty, new UiSettings(), 33f);
            ecs.SetParent(_realText, row);
            ecs.SetParent(Ui.SpawnText("CONTROLS\n(Un)pause: Space\nSpeed+: Up\nSpeed-: Down", new UiSettings { Color = Scene.Srgb(0.85f, 0.85f, 0.85f) }, new UiTextSettings { FontSize = 33f, Justify = TextJustify.Center }), row);
            _virtualText = Ui.SpawnText(string.Empty, new UiSettings { Color = gold }, new UiTextSettings { FontSize = 33f, Justify = TextJustify.Right });
            ecs.SetParent(_virtualText, row);
        }, "virtual_time.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var time = ctx.Time;

            // The wall's clock is the raw delta summed, which pausing and speed leave alone.
            _realElapsed += (float)time.RawDeltaSeconds;
            Swing(ecs, _real, _realElapsed);
            Swing(ecs, _virtual, time.Elapsed);

            var input = ctx.Input;
            if (input.KeyPressed(Key.Space))
            {
                if (time.Paused) time.Resume();
                else time.Pause();
            }

            var change = (input.KeyPressed(Key.ArrowUp) ? 1f : 0f) - (input.KeyPressed(Key.ArrowDown) ? 1f : 0f);
            if (change != 0f) time.SetSpeed(Math.Clamp(MathF.Round(time.Speed + change), 0.25f, 5f));

            // Four times a second by the wall's clock.
            _sinceText += (float)time.RawDeltaSeconds;
            if (_sinceText < 0.25f) return;
            _sinceText = 0f;
            Ui.SetText(_realText, FormattableString.Invariant($"REAL TIME\nElapsed: {_realElapsed:0.0}\nDelta: {time.RawDeltaSeconds:0.00000}\n"));
            Ui.SetText(_virtualText, FormattableString.Invariant($"VIRTUAL TIME\nElapsed: {time.Elapsed:0.0}\nDelta: {time.Delta:0.00000}\nSpeed: {time.Speed:0.00}"));
        }, "virtual_time.Update");
    }

    private static void Swing(EcsWorld ecs, Entity sprite, float elapsed)
    {
        var transform = ecs.GetOrDefault<Transform>(sprite);
        transform.Translation = transform.Translation with { X = MathF.Sin(elapsed) * 500f };
        ecs.Set(sprite, transform);
    }
}
