// Bevy's virtual_time example, examples/time/virtual_time.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Clocks;

// Shows the game's clock beside the wall's, two logos swinging by each, the gold one by the game's
// clock, which Space pauses and the arrow keys speed up and slow down.
internal static class VirtualTimeExample
{
    // Bevy's Time<Real>, the wall's clock summed from the raw delta, which pausing and speed leave
    // alone, and the timer on it its texts are written by, four times a second.
    internal static float RealElapsed;
    internal static GameTimer TextTimer;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (RealElapsed, TextTimer) = (0f, GameTimer.FromSeconds(0.25f, TimerMode.Repeating));
            ctx.Time.SetSpeed(2f);
            Render2d.SpawnCamera2d();

            var gold = Color.FromSrgb8(255, 215, 0);
            var texture = AssetServer.Load(AssetKind.Image, "branding/icon.png");
            var real = ecs.Spawn();
            ecs.Add(real, new Transform(Vec3.Zero, Quat.Identity, new Vec3(0.5f, 0.5f, 1f)));
            ecs.Add(real, new RealTime());
            Render2d.SetSprite(ecs, real, texture);
            var game = ecs.Spawn();
            ecs.Add(game, new Transform(new Vec3(0f, -160f, 0f), Quat.Identity, new Vec3(0.5f, 0.5f, 1f)));
            ecs.Add(game, new VirtualTime());
            Render2d.SetSprite(ecs, game, texture, new SpriteSettings { Color = gold });

            var row = Ui.SpawnNode(new UiSettings { Absolute = true, Top = Length.Px(0f), Width = Length.Percent(100f), Justify = UiJustify.SpaceBetween, Padding = Sides.All(Length.Px(20f)) });
            var realText = Ui.SpawnText(string.Empty, new UiSettings(), 33f);
            ecs.Add(realText, new RealTime());
            ecs.SetParent(realText, row);
            ecs.SetParent(Ui.SpawnText("CONTROLS\n(Un)pause: Space\nSpeed+: Up\nSpeed-: Down", new UiSettings { Color = Color.FromSrgb(0.85f, 0.85f, 0.85f) }, new UiTextSettings { FontSize = 33f, Justify = TextJustify.Center }), row);
            var gameText = Ui.SpawnText(string.Empty, new UiSettings { Color = gold }, new UiTextSettings { FontSize = 33f, Justify = TextJustify.Right });
            ecs.Add(gameText, new VirtualTime());
            ecs.SetParent(gameText, row);
        }, "virtual_time.Setup");

        app.Update(ctx =>
        {
            var time = ctx.Time;
            RealElapsed += (float)time.RawDeltaSeconds;
            TextTimer.Tick((float)time.RawDeltaSeconds);

            var input = ctx.Input;
            if (input.KeyPressed(Key.Space))
            {
                if (time.Paused) time.Resume();
                else time.Pause();
            }

            var change = (input.KeyPressed(Key.ArrowUp) ? 1f : 0f) - (input.KeyPressed(Key.ArrowDown) ? 1f : 0f);
            if (change != 0f) time.SetSpeed(Math.Clamp(MathF.Round(time.Speed + change), 0.25f, 5f));
        }, "virtual_time.Clocks");
    }

    // Bevy's get_sprite_translation_x.
    internal static float SpriteX(float elapsed) => MathF.Sin(elapsed) * 500f;
}

/// <summary>A sprite that swings, or a text that says, by the wall's clock.</summary>
[Behavior]
public partial struct RealTime
{
    /// <summary>The sprite swung by the wall's clock.</summary>
    [OnUpdate]
    public void MoveRealTimeSprites(BehaviorContext ctx, ref Transform transform) =>
        transform.Translation = transform.Translation with { X = VirtualTimeExample.SpriteX(VirtualTimeExample.RealElapsed) };

    /// <summary>The text written with the wall's clock, four times a second by that clock.</summary>
    [OnUpdate]
    public void UpdateRealTimeInfoText(BehaviorContext ctx)
    {
        // A text has no Transform, as a sprite has, which is Bevy's With<Text>.
        if (!VirtualTimeExample.TextTimer.JustFinished || ctx.Ecs.Has<Transform>(ctx.Entity)) return;
        Ui.SetText(ctx.Entity, FormattableString.Invariant($"REAL TIME\nElapsed: {VirtualTimeExample.RealElapsed:0.0}\nDelta: {ctx.Time.RawDeltaSeconds:0.00000}\n"));
    }
}

/// <summary>A sprite that swings, or a text that says, by the game's clock.</summary>
[Behavior]
public partial struct VirtualTime
{
    /// <summary>The sprite swung by the game's clock.</summary>
    [OnUpdate]
    public void MoveVirtualTimeSprites(BehaviorContext ctx, ref Transform transform) =>
        transform.Translation = transform.Translation with { X = VirtualTimeExample.SpriteX(ctx.Time.Elapsed) };

    /// <summary>The text written with the game's clock, four times a second by the wall's.</summary>
    [OnUpdate]
    public void UpdateVirtualTimeInfoText(BehaviorContext ctx)
    {
        if (!VirtualTimeExample.TextTimer.JustFinished || ctx.Ecs.Has<Transform>(ctx.Entity)) return;
        var time = ctx.Time;
        Ui.SetText(ctx.Entity, FormattableString.Invariant($"VIRTUAL TIME\nElapsed: {time.Elapsed:0.0}\nDelta: {time.Delta:0.00000}\nSpeed: {time.Speed:0.00}"));
    }
}
