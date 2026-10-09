// Bevy's ui_scaling example, examples/ui/ui_scaling.rs at v0.20.0, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows how Bevy's UiScale resource scales the interface, the arrow keys doubling and halving it,
// eased toward each new scale over a short time.
internal static class UiScaling
{
    private const float ScaleTime = 0.4f;

    private static float _startScale, _targetScale, _elapsed;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_startScale, _targetScale, _elapsed) = (1f, 1f, ScaleTime);
            Render2d.SpawnCamera2d();

            // A box half the window's size in its middle, holding a red square of fixed size with a
            // word in it, a blue one sized as a share of the box, and the icon.
            var box = Ui.SpawnNode(new UiSettings
            {
                Absolute = true,
                Width = Length.Percent(50f),
                Height = Length.Percent(50f),
                Left = Length.Percent(25f),
                Top = Length.Percent(25f),
                Justify = UiJustify.SpaceAround,
                Align = UiAlign.Center,
                Color = Color.FromSrgb8(250, 235, 215),
            });

            var red = Ui.SpawnNode(new UiSettings { Width = Length.Px(40f), Height = Length.Px(40f), Color = (1f, 0f, 0f, 1f) });
            ecs.SetParent(red, box);
            ecs.SetParent(Ui.SpawnText("Size!", new UiSettings { Color = (0f, 0f, 0f, 1f) }, new UiTextSettings { FontSize = 13f }), red);

            ecs.SetParent(Ui.SpawnNode(new UiSettings { Width = Length.Percent(15f), Height = Length.Percent(15f), Color = (0f, 0f, 1f, 1f) }), box);

            var icon = Ui.SpawnNode(new UiSettings { Width = Length.Px(30f), Height = Length.Px(30f) });
            Ui.SetImage(icon, AssetServer.Load(AssetKind.Image, "branding/icon.png"));
            ecs.SetParent(icon, box);
        }, "ui_scaling.Setup");

        app.Update(ctx =>
        {
            var input = ctx.Input;
            if (input.KeyPressed(Key.ArrowUp)) SetScale(MathF.Min(_targetScale * 2f, 8f), "up");
            if (input.KeyPressed(Key.ArrowDown)) SetScale(MathF.Max(_targetScale / 2f, 1f / 8f), "down");

            // Written while easing and once more as it ends, and left alone after.
            if (_elapsed >= ScaleTime) return;
            _elapsed = MathF.Min(_elapsed + ctx.Time.Delta, ScaleTime);
            if (ctx.Ecs.Resource<UiScaleRef>() is { } scale) scale.Value = CurrentScale();
        }, "ui_scaling.ApplyScaling");
    }

    private static void SetScale(float scale, string direction)
    {
        _startScale = CurrentScale();
        (_targetScale, _elapsed) = (scale, 0f);
        Console.WriteLine(FormattableString.Invariant($"Scaling {direction}! Scale: {scale}"));
    }

    // Eased in exponentially, slow to leave the old scale and quick to reach the new one.
    private static float CurrentScale()
    {
        var x = _elapsed / ScaleTime;
        var t = x == 0f ? 0f : MathF.Pow(2f, 5f * x - 5f);
        return _startScale + (_targetScale - _startScale) * t;
    }
}
