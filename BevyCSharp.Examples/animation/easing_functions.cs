// Bevy's easing_functions example, examples/animation/easing_functions.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Animations;

// Plots every one of Bevy's easing functions, each with a dot climbing its curve and another along
// its right edge at the height the curve has reached, all moving together as the progress runs from
// nought to one.
internal static class EasingFunctions
{
    private const int Columns = 12;
    private static readonly Vec2 Extent = new(1172f, 520f);
    private const float PlotSize = 80f;

    // Bevy's functions in its order, each by the name Bevy prints it with.
    private static readonly (string Name, Func<float, float> Function)[] Functions =
    [
        ("SineIn", Ease.SineIn), ("QuadraticIn", Ease.QuadraticIn), ("CubicIn", Ease.CubicIn), ("QuarticIn", Ease.QuarticIn),
        ("QuinticIn", Ease.QuinticIn), ("SmoothStepIn", Ease.SmoothStepIn), ("SmootherStepIn", Ease.SmootherStepIn), ("CircularIn", Ease.CircularIn),
        ("ExponentialIn", Ease.ExponentialIn), ("ElasticIn", Ease.ElasticIn), ("BackIn", Ease.BackIn), ("BounceIn", Ease.BounceIn),
        ("SineOut", Ease.SineOut), ("QuadraticOut", Ease.QuadraticOut), ("CubicOut", Ease.CubicOut), ("QuarticOut", Ease.QuarticOut),
        ("QuinticOut", Ease.QuinticOut), ("SmoothStepOut", Ease.SmoothStepOut), ("SmootherStepOut", Ease.SmootherStepOut), ("CircularOut", Ease.CircularOut),
        ("ExponentialOut", Ease.ExponentialOut), ("ElasticOut", Ease.ElasticOut), ("BackOut", Ease.BackOut), ("BounceOut", Ease.BounceOut),
        ("SineInOut", Ease.SineInOut), ("QuadraticInOut", Ease.QuadraticInOut), ("CubicInOut", Ease.CubicInOut), ("QuarticInOut", Ease.QuarticInOut),
        ("QuinticInOut", Ease.QuinticInOut), ("SmoothStep", Ease.SmoothStep), ("SmootherStep", Ease.SmootherStep), ("CircularInOut", Ease.CircularInOut),
        ("ExponentialInOut", Ease.ExponentialInOut), ("ElasticInOut", Ease.ElasticInOut), ("BackInOut", Ease.BackInOut), ("BounceInOut", Ease.BounceInOut),
        ("Linear", Ease.Linear),
        ("Steps(4, End)", t => Ease.Steps(4, Ease.JumpAt.End, t)), ("Steps(4, Start)", t => Ease.Steps(4, Ease.JumpAt.Start, t)),
        ("Steps(4, Both)", t => Ease.Steps(4, Ease.JumpAt.Both, t)), ("Steps(4, None)", t => Ease.Steps(4, Ease.JumpAt.None, t)),
        ("Elastic(50.0)", t => Ease.Elastic(50f, t)),
    ];

    private static readonly List<(Vec2 Center, int Column, Func<float, float> Function, Entity Edge, Entity Climber)> Plots = [];
    private static Entity _progress;

    public static void Build(App app)
    {
        app.Startup(Setup, "easing_functions.Setup");
        app.Update(DisplayCurves, "easing_functions.DisplayCurves");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Plots.Clear();
        Render2d.SpawnCamera2d();

        var white = Render.CreateImage([255, 255, 255, 255], 1, 1);
        var rows = (Functions.Length + Columns - 1) / Columns;
        var (halfExtent, half) = (Extent * 0.5f, PlotSize / 2f);

        for (var i = 0; i < Functions.Length; i++)
        {
            var (row, column) = (i / Columns, i % Columns);
            var color = Color.FromHsl(column / (float)Columns * 360f, 0.8f, 0.75f);
            var center = new Vec2(-halfExtent.X + Extent.X / (Columns - 1) * column, halfExtent.Y - Extent.Y / (rows - 1) * row);

            Entity Dot(float size, Vec2 at)
            {
                var dot = ecs.Spawn();
                ecs.Add(dot, Transform.At(at.X, at.Y, 0f));
                Render2d.SetSprite(ecs, dot, white, new SpriteSettings { Color = color, Size = (size, size) });
                return dot;
            }

            var edge = Dot(5f, center + new Vec2(half + 5f, -half));
            var climber = Dot(4f, center + new Vec2(-half, -half));

            var label = ecs.Spawn();
            ecs.Add(label, Transform.At(center.X, center.Y - half - 15f, 0f));
            ecs.Insert<Text2dRef>(label).Value = Functions[i].Name;
            ecs.Wrap<TextFontRef>(label).FontSize = new FontSize.Px(10f);
            ecs.Wrap<TextColorRef>(label).Value = new Color(color.R, color.G, color.B, color.A);

            Plots.Add((center, column, Functions[i].Function, edge, climber));
        }

        _progress = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    private static void DisplayCurves(BehaviorContext ctx)
    {
        const int Samples = 100;
        const float Duration = 2.5f, Margin = 0.5f;

        var now = Math.Clamp((ctx.Time.Elapsed % (Duration + Margin * 2f) - Margin) / Duration, 0f, 1f);
        Ui.SetText(_progress, FormattableString.Invariant($"Progress: {now:0.00}"));

        var half = PlotSize / 2f;
        Span<Vec3> curve = stackalloc Vec3[Samples];
        using var batch = Gizmos.Batch();
        foreach (var (center, column, function, edge, climber) in Plots)
        {
            var hue = column / (float)Columns * 360f;

            // The plot's frame, darker than its curve.
            Gizmos.Polyline(
                [new Vec3(center.X + half, center.Y + half, 0f), new Vec3(center.X - half, center.Y + half, 0f),
                 new Vec3(center.X - half, center.Y - half, 0f), new Vec3(center.X + half, center.Y - half, 0f)],
                Color.FromHsl(hue, 0.8f, 0.35f), closed: true, inFront: true);

            for (var i = 0; i < Samples; i++)
            {
                var x = i / (Samples - 1f);
                curve[i] = new Vec3(center.X - half + x * PlotSize, center.Y - half + function(x) * PlotSize, 0f);
            }
            Gizmos.Polyline(curve, Color.FromHsl(hue, 0.8f, 0.75f), inFront: true);

            var y = function(now) * PlotSize;
            ctx.Ecs.Set(edge, Transform.At(center.X + half + 5f, center.Y - half + y, 0f));
            ctx.Ecs.Set(climber, Transform.At(center.X - half + now * PlotSize, center.Y - half + y, 0f));
            Gizmos.Line2d((center.X - half, center.Y - half + y), (center.X + half, center.Y - half + y), Color.FromHsl(hue, 0.8f, 0.55f));
        }
    }
}
