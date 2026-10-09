// Bevy's stacked_gradients example, examples/ui/styling/stacked_gradients.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows gradients stacked on one node, red and blue rising from two corners, a yellow beam from the
// middle, a sun near the top, and two dark wedges, each laid over the ones before it.
internal static class StackedGradients
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var grid = Ui.SpawnNode(new UiSettings { Display = UiDisplay.Grid, Width = Length.Percent(100f), Height = Length.Percent(100f) });
        var node = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Color = (0f, 0f, 0f, 1f) });
        ecs.SetParent(node, grid);

        // Bevy's gradients, a list in the order they are drawn.
        var (red, blue, yellow, white, black) = ((1f, 0f, 0f), (0f, 0f, 1f), (1f, 1f, 0f), (1f, 1f, 1f), (0f, 0f, 0f));
        ecs.Insert<BackgroundGradientRef>(node).Value =
        [
            Linear(MathF.Tau / 8f, Stop(red, 1f), Stop(red, 0f)),
            Linear(7f * MathF.Tau / 8f, Stop(blue, 1f), Stop(blue, 0f)),
            new Gradient.Conic(
                InterpolationColorSpace.Oklaba, 0f, Vec2.Zero, new Val.Px(0f), new Val.Px(0f),
                [Angular(yellow, 0f), Angular(yellow, 0f), Angular(yellow, 1f), Angular(yellow, 0f), Angular(yellow, 0f)]),
            new Gradient.Radial(
                InterpolationColorSpace.Oklaba, new Vec2(0f, -0.5f), new Val.Percent(5f), new Val.Px(0f), new RadialGradientShape.Circle(new Val.Vh(30f)),
                [Stop(white, 1f), Stop(yellow, 1f), Stop(yellow, 0.1f), Stop(yellow, 0f)]),
            Linear(MathF.Tau / 16f, Stop(black, 1f), Stop(black, 0f)),
            Linear(15f * MathF.Tau / 16f, Stop(black, 1f), Stop(black, 0f)),
        ];
    }, "stacked_gradients.Setup");

    private static Gradient Linear(float angle, params ColorStop[] stops) => new Gradient.Linear(InterpolationColorSpace.Oklaba, angle, stops);

    // A stop placed where the gradient puts it, given in sRGB as Bevy's palette colors are.
    private static ColorStop Stop((float R, float G, float B) color, float alpha) =>
        new(Color.FromSrgb(color.R, color.G, color.B, alpha), new Val.Auto(), 0.5f);

    private static AngularColorStop Angular((float R, float G, float B) color, float alpha) =>
        new(Color.FromSrgb(color.R, color.G, color.B, alpha), null, 0.5f);
}
