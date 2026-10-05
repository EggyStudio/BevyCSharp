// Bevy's 2d_gizmos example, examples/gizmos/2d_gizmos.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Gizmo;

// Shows the gizmos a 2D camera can draw, in two groups set apart. The left and right arrows thicken
// the straight shapes' lines, 1 hides them, U and I restyle them and J and K join them differently,
// and the up and down arrows, 2, I and K do the same for the round ones. Space pauses the clock they
// move by.
internal static class Gizmos2d
{
    // Bevy's css colors, given in sRGB.
    private static readonly (float R, float G, float B, float A) Red = Scene.Srgb(1f, 0f, 0f), Lime = Scene.Srgb(0f, 1f, 0f), Blue = Scene.Srgb(0f, 0f, 1f);
    private static readonly (float R, float G, float B, float A) Black = (0f, 0f, 0f, 1f), Fuchsia = Scene.Srgb(1f, 0f, 1f), Navy = Scene.Srgb8(0, 0, 128);
    private static readonly (float R, float G, float B, float A) YellowGreen = Scene.Srgb8(154, 205, 50), OrangeRed = Scene.Srgb8(255, 69, 0);
    private static readonly (float R, float G, float B, float A) Yellow = Scene.Srgb(1f, 1f, 0f), Green = Scene.Srgb8(0, 128, 0);

    // Bevy's own group draws the straight shapes and a group of the example's the round ones. The
    // bridge's two groups stand for them, which under a 2D camera differ in nothing else.
    private static GizmoLines _straight = new(GizmoGroup.Behind), _round = new(GizmoGroup.InFront);

    public static void Build(App app)
    {
        (_straight, _round) = (new GizmoLines(GizmoGroup.Behind), new GizmoLines(GizmoGroup.InFront));
        app.Startup(_ =>
        {
            Render2d.SpawnCamera2d();
            Ui.SpawnText(
                "Hold 'Left' or 'Right' to change the line width of straight gizmos\n"
                + "Hold 'Up' or 'Down' to change the line width of round gizmos\n"
                + "Press '1' / '2' to toggle the visibility of straight / round gizmos\n"
                + "Press 'U' / 'I' to cycle through line styles\n"
                + "Press 'J' / 'K' to cycle through line joins\n"
                + "Press 'Spacebar' to toggle pause",
                new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
            _straight.Apply();
            _round.Apply();
        }, "2d_gizmos.Setup");
        app.Update(DrawExampleCollection, "2d_gizmos.DrawExampleCollection");
        app.Update(UpdateConfig, "2d_gizmos.UpdateConfig");
    }

    private static void DrawExampleCollection(BehaviorContext ctx)
    {
        var t = ctx.Time.Elapsed;
        var sinTScaled = MathF.Sin(t) * 50f;
        const bool Straight = false, Round = true;

        Gizmos.Line2d((0f, -sinTScaled), (-80f, -80f), Red, Straight);

        // A ray is a line from a point along a vector, so it ends at the two added together.
        Gizmos.Line2d((0f, sinTScaled), (80f, sinTScaled + 80f), Lime, Straight);

        // Bevy's grid with its outer edges is the grid and the rectangle around it.
        var gray = (0.05f, 0.05f, 0.05f, 1f);
        Gizmos.Grid2d((0f, 0f), 16, 9, 80f, gray, inFront: Straight);
        Gizmos.Rect2d((0f, 0f), 16 * 80f, 9 * 80f, gray, inFront: Straight);

        // A strip whose color fades from each corner to the next.
        (float X, float Y)[] strip = [(0f, 300f), (-255f, -155f), (255f, -155f), (0f, 300f)];
        (float R, float G, float B, float A)[] colors = [Blue, Red, Lime, Blue];
        for (var i = 0; i < strip.Length - 1; i++) Gizmos.Line2d(strip[i], strip[i + 1], colors[i], colors[i + 1], Straight);

        Gizmos.Rect2d((0f, 0f), 650f, 650f, Black, inFront: Straight);
        Cross2d((-160f, 120f), 12f, Fuchsia, Straight);

        // A sine curve sampled at a resolution that rises and falls, each piece fading from teal
        // to hot pink by where along the curve it is, mixed in sRGB as Bevy mixes its colors.
        var resolution = (int)((MathF.Sin(t) + 1f) * 50f);
        if (resolution > 0)
        {
            (float X, float Y) previous = default;
            (float R, float G, float B, float A) previousColor = default;
            for (var n = 0; n <= resolution; n++)
            {
                var x = ((float)n / resolution - 0.5f) * 600f;
                var point = (x, MathF.Sin(x / 25f) * 100f);
                var mix = (x + 300f) / 600f;
                var color = Scene.Srgb(0f + (1f - 0f) * mix, 128 / 255f + (105 / 255f - 128 / 255f) * mix, 128 / 255f + (180 / 255f - 128 / 255f) * mix);
                if (n > 0) Gizmos.Line2d(previous, point, previousColor, color, Straight);
                (previous, previousColor) = (point, color);
            }
        }

        Gizmos.RoundedRect2d((0f, 0f), 630f, 630f, Black, cornerRadius: MathF.Cos(t / 3f) * 100f, inFront: Round);
        Gizmos.Circle2d((0f, 0f), 300f, Navy, Round, resolution: 64);
        Gizmos.Ellipse2d((0f, 0f), 100f, 200f, YellowGreen, angle: t % MathF.Tau, inFront: Round);

        // Bevy's arcs start from the Y axis and run anticlockwise, as the turn given here does.
        Gizmos.Arc2d((0f, 0f), 310f, MathF.PI / 2f, OrangeRed, from: sinTScaled / 10f, inFront: Round);
        Gizmos.Arc2d((0f, 0f), 80f, MathF.PI / 2f, OrangeRed, inFront: Round);
        ArcBetween((0f, 0f), (20f, 0f), (0f, 20f), OrangeRed, longWay: true, Round);
        ArcBetween((0f, 0f), (40f, 0f), (0f, 40f), OrangeRed, longWay: false, Round);

        Gizmos.Arrow2d((0f, 0f), FromAngle(sinTScaled / -10f + MathF.PI / 2f, 50f), Yellow, Straight);
        Gizmos.Arrow2d((0f, 0f), FromAngle(sinTScaled / -10f, 50f), Green, Straight, tipLength: 10f, doubleEnd: true);
    }

    private static (float X, float Y) FromAngle(float angle, float length) => (MathF.Cos(angle) * length, MathF.Sin(angle) * length);

    // Bevy's cross_2d, a line across each axis through the point.
    private static void Cross2d((float X, float Y) at, float halfSize, (float R, float G, float B, float A) color, bool inFront)
    {
        Gizmos.Line2d((at.X - halfSize, at.Y), (at.X + halfSize, at.Y), color, inFront);
        Gizmos.Line2d((at.X, at.Y - halfSize), (at.X, at.Y + halfSize), color, inFront);
    }

    // Bevy's short_arc_2d_between and long_arc_2d_between, the arc about a center from one point
    // round to the direction of another, the short way or the long one.
    private static void ArcBetween((float X, float Y) center, (float X, float Y) from, (float X, float Y) to, (float R, float G, float B, float A) color, bool longWay, bool inFront)
    {
        static float AngleTo((float X, float Y) a, (float X, float Y) b) => MathF.Atan2(a.X * b.Y - a.Y * b.X, a.X * b.X + a.Y * b.Y);

        var (fromAxis, toAxis) = ((from.X - center.X, from.Y - center.Y), (to.X - center.X, to.Y - center.Y));
        var start = AngleTo((0f, 1f), fromAxis);
        var sweep = AngleTo(fromAxis, toAxis);
        if (longWay) sweep -= MathF.Tau;
        var radius = MathF.Sqrt(fromAxis.Item1 * fromAxis.Item1 + fromAxis.Item2 * fromAxis.Item2);
        Gizmos.Arc2d(center, radius, sweep, color, from: start, inFront: inFront);
    }

    private static void UpdateConfig(BehaviorContext ctx)
    {
        var (input, delta) = (ctx.Input, (float)ctx.Time.RawDeltaSeconds);

        if (input.KeyDown(Key.ArrowRight)) _straight.Widen(5f * delta);
        if (input.KeyDown(Key.ArrowLeft)) _straight.Widen(-5f * delta);
        if (input.KeyPressed(Key.Digit1)) _straight.Toggle();
        if (input.KeyPressed(Key.U)) _straight.NextStyle(forward: true);
        if (input.KeyPressed(Key.I)) _straight.NextStyle(forward: false);
        if (input.KeyPressed(Key.J)) _straight.NextJoint(forward: true);
        if (input.KeyPressed(Key.K)) _straight.NextJoint(forward: false);

        if (input.KeyDown(Key.ArrowUp)) _round.Widen(5f * delta);
        if (input.KeyDown(Key.ArrowDown)) _round.Widen(-5f * delta);
        if (input.KeyPressed(Key.Digit2)) _round.Toggle();
        if (input.KeyPressed(Key.I)) _round.NextStyle(forward: true);
        if (input.KeyPressed(Key.K)) _round.NextJoint(forward: true);

        if (input.KeyPressed(Key.Space))
        {
            if (ctx.Time.Paused) ctx.Time.Resume();
            else ctx.Time.Pause();
        }
    }
}

/// <summary>
/// One gizmo group's line settings as Bevy's examples change them, kept here since the bridge sets
/// them and does not read them back.
/// </summary>
internal sealed class GizmoLines(GizmoGroup group)
{
    public float Width { get; private set; } = 2f;
    public bool Enabled { get; private set; } = true;
    public bool Perspective { get; private set; }
    private GizmoLine _style = GizmoLine.Solid;
    private GizmoJoint _joint = GizmoJoint.None;

    public void Apply()
    {
        Gizmos.Configure(width: Width, enabled: Enabled, which: group);
        Gizmos.SetLineStyle(_style, gapScale: 3f, lineScale: 5f, joint: _joint, jointResolution: 4, perspective: Perspective, which: group);
    }

    // Wider or narrower, kept between none and fifty pixels.
    public void Widen(float by)
    {
        Width = Math.Clamp(Width + by, 0f, 50f);

        // Zero tells the bridge to leave the width alone, so a line taken to nothing is drawn at
        // the narrowest width that still says something.
        Gizmos.Configure(width: MathF.Max(Width, 0.01f), enabled: Enabled, which: group);
    }

    public void Toggle()
    {
        Enabled = !Enabled;
        Apply();
    }

    // Bevy's perspective lines are five times as wide, since their width is measured at the near
    // plane.
    public void TogglePerspective()
    {
        Perspective = !Perspective;
        Width *= Perspective ? 5f : 1f / 5f;
        Apply();
    }

    // Solid, dotted, dashed and round again, or the other way.
    public void NextStyle(bool forward)
    {
        _style = (_style, forward) switch
        {
            (GizmoLine.Solid, true) or (GizmoLine.Dashed, false) => GizmoLine.Dotted,
            (GizmoLine.Dotted, true) or (GizmoLine.Solid, false) => GizmoLine.Dashed,
            _ => GizmoLine.Solid,
        };
        Apply();
    }

    // Bevel, miter, round, none and round again, or the other way.
    public void NextJoint(bool forward)
    {
        GizmoJoint[] order = [GizmoJoint.Bevel, GizmoJoint.Miter, GizmoJoint.Round, GizmoJoint.None];
        var at = Array.IndexOf(order, _joint);
        _joint = order[(at + (forward ? 1 : order.Length - 1)) % order.Length];
        Apply();
    }
}
