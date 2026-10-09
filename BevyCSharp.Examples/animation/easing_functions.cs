// Bevy's easing_functions example, examples/animation/easing_functions.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Animations;

// Plots every one of Bevy's easing functions, each with a dot climbing its curve and another along
// its right edge at the height the curve has reached, all moving together as the progress runs from
// zero to one.
internal static class EasingFunctions
{
    private const int Columns = 12;
    private static readonly Vec2 Extent = new(1172f, 520f);
    internal const float PlotSize = 80f;

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

    private static Entity _progress;

    public static void Build(App app)
    {
        app.Startup(Setup, "easing_functions.Setup");
        app.Update(ShowProgress, "easing_functions.ShowProgress");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var white = Render.CreateImage([255, 255, 255, 255], 1, 1);
        var rows = (Functions.Length + Columns - 1) / Columns;
        var (halfExtent, half) = (Extent * 0.5f, PlotSize / 2f);

        for (var i = 0; i < Functions.Length; i++)
        {
            var (row, column) = (i / Columns, i % Columns);
            var hue = column / (float)Columns * 360f;
            var color = Color.FromHsl(hue, 0.8f, 0.75f);

            // The plot at its place, drawn by its behavior, with what moves along it and its label
            // as its children, placed from its middle.
            var plot = ecs.Spawn();
            ecs.Add(plot, Transform.At(-halfExtent.X + Extent.X / (Columns - 1) * column, halfExtent.Y - Extent.Y / (rows - 1) * row, 0f));
            ecs.Insert<VisibilityRef>(plot);
            ecs.Add(plot, new EaseFunctionPlot { Function = Functions[i], Hue = hue });

            void Child(Entity child, Vec2 at)
            {
                ecs.Add(child, Transform.At(at.X, at.Y, 0f));
                ecs.SetParent(child, plot);
            }

            foreach (var (size, at) in new[] { (5f, new Vec2(half + 5f, -half)), (4f, new Vec2(-half, -half)) })
            {
                var dot = ecs.Spawn();
                Render2d.SetSprite(ecs, dot, white, new SpriteSettings { Color = color, Size = (size, size) });
                Child(dot, at);
            }

            var label = ecs.Spawn();
            ecs.Insert<Text2dRef>(label).Value = Functions[i].ToString();
            ecs.Wrap<TextFontRef>(label).FontSize = new FontSize.Px(10f);
            ecs.Wrap<TextColorRef>(label).Value = new Color(color.R, color.G, color.B, color.A);
            Child(label, new Vec2(0f, -half - 15f));
        }

        _progress = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    // How far the progress has run, over two and a half seconds held half a second at each end.
    internal static float Now(float elapsed)
    {
        const float Duration = 2.5f, Margin = 0.5f;
        return Math.Clamp((elapsed % (Duration + Margin * 2f) - Margin) / Duration, 0f, 1f);
    }

    private static void ShowProgress(BehaviorContext ctx) =>
        Ui.SetText(_progress, FormattableString.Invariant($"Progress: {Now(ctx.Time.Elapsed):0.00}"));
}

/// <summary>
/// An easing function's plot, its curve drawn in its frame and two dots, its children, moving with
/// the progress.
/// </summary>
[Behavior]
public partial struct EaseFunctionPlot
{
    /// <summary>The function plotted.</summary>
    public EaseFunction Function;

    /// <summary>
    /// The hue it is drawn in, in degrees. Bevy keeps an HSL color and darkens its lightness for the
    /// frame and the line, so the hue is kept here and each shade made from it.
    /// </summary>
    public float Hue;

    /// <summary>
    /// The frame and the curve drawn, the dot at the edge raised to the height the curve has
    /// reached, the other moved along the curve, and a line drawn across at that height.
    /// </summary>
    [OnUpdate]
    public void DisplayCurves(BehaviorContext ctx, in Transform transform)
    {
        const int Samples = 100;
        var now = EasingFunctions.Now(ctx.Time.Elapsed);
        var (center, half) = (new Vec2(transform.Translation.X, transform.Translation.Y), EasingFunctions.PlotSize / 2f);

        using var batch = Gizmos.Batch();
        Gizmos.Polyline(
            [new Vec3(center.X + half, center.Y + half, 0f), new Vec3(center.X - half, center.Y + half, 0f),
             new Vec3(center.X - half, center.Y - half, 0f), new Vec3(center.X + half, center.Y - half, 0f)],
            Color.FromHsl(Hue, 0.8f, 0.35f), closed: true, inFront: true);

        Span<Vec3> curve = stackalloc Vec3[Samples];
        for (var i = 0; i < Samples; i++)
        {
            var x = i / (Samples - 1f);
            curve[i] = new Vec3(center.X - half + x * EasingFunctions.PlotSize, center.Y - half + Function.Sample(x) * EasingFunctions.PlotSize, 0f);
        }
        Gizmos.Polyline(curve, Color.FromHsl(Hue, 0.8f, 0.75f), inFront: true);

        var y = Function.Sample(now) * EasingFunctions.PlotSize;
        var children = ctx.Ecs.ChildrenOf(ctx.Entity);
        ctx.Ecs.Set(children[0], Transform.At(half + 5f, -half + y, 0f));
        ctx.Ecs.Set(children[1], Transform.At(-half + now * EasingFunctions.PlotSize, -half + y, 0f));
        Gizmos.Line2d((center.X - half, center.Y - half + y), (center.X + half, center.Y - half + y), Color.FromHsl(Hue, 0.8f, 0.55f));
    }
}
