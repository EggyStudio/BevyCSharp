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

    // Bevy's functions in its order, each labeled as Bevy prints it.
    private static readonly EaseFunction[] Functions =
    [
        EaseFunction.SineIn, EaseFunction.QuadraticIn, EaseFunction.CubicIn, EaseFunction.QuarticIn,
        EaseFunction.QuinticIn, EaseFunction.SmoothStepIn, EaseFunction.SmootherStepIn, EaseFunction.CircularIn,
        EaseFunction.ExponentialIn, EaseFunction.ElasticIn, EaseFunction.BackIn, EaseFunction.BounceIn,
        EaseFunction.SineOut, EaseFunction.QuadraticOut, EaseFunction.CubicOut, EaseFunction.QuarticOut,
        EaseFunction.QuinticOut, EaseFunction.SmoothStepOut, EaseFunction.SmootherStepOut, EaseFunction.CircularOut,
        EaseFunction.ExponentialOut, EaseFunction.ElasticOut, EaseFunction.BackOut, EaseFunction.BounceOut,
        EaseFunction.SineInOut, EaseFunction.QuadraticInOut, EaseFunction.CubicInOut, EaseFunction.QuarticInOut,
        EaseFunction.QuinticInOut, EaseFunction.SmoothStep, EaseFunction.SmootherStep, EaseFunction.CircularInOut,
        EaseFunction.ExponentialInOut, EaseFunction.ElasticInOut, EaseFunction.BackInOut, EaseFunction.BounceInOut,
        EaseFunction.Linear,
        EaseFunction.Steps(4, JumpAt.End), EaseFunction.Steps(4, JumpAt.Start),
        EaseFunction.Steps(4, JumpAt.Both), EaseFunction.Steps(4, JumpAt.None),
        EaseFunction.Elastic(50f),
    ];

    private static readonly List<(Vec2 Center, int Column, EaseFunction Function, Entity Edge, Entity Climber)> Plots = [];
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
            ecs.Insert<Text2dRef>(label).Value = Functions[i].ToString();
            ecs.Wrap<TextFontRef>(label).FontSize = new FontSize.Px(10f);
            ecs.Wrap<TextColorRef>(label).Value = new Color(color.R, color.G, color.B, color.A);

            Plots.Add((center, column, Functions[i], edge, climber));
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
                curve[i] = new Vec3(center.X - half + x * PlotSize, center.Y - half + function.Sample(x) * PlotSize, 0f);
            }
            Gizmos.Polyline(curve, Color.FromHsl(hue, 0.8f, 0.75f), inFront: true);

            var y = function.Sample(now) * PlotSize;
            ctx.Ecs.Set(edge, Transform.At(center.X + half + 5f, center.Y - half + y, 0f));
            ctx.Ecs.Set(climber, Transform.At(center.X - half + now * PlotSize, center.Y - half + y, 0f));
            Gizmos.Line2d((center.X - half, center.Y - half + y), (center.X + half, center.Y - half + y), Color.FromHsl(hue, 0.8f, 0.55f));
        }
    }
}
