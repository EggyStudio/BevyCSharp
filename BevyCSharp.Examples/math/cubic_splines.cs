// Bevy's cubic_splines example, examples/math/cubic_splines.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Maths;

// This example exhibits different available modes of constructing cubic Bézier curves. A curve is
// drawn through control points a click and drag adds, the drag giving each its tangent, made as a
// Hermite, a Catmull-Rom or a B-spline curve, looping or not.
internal static class CubicSplines
{
    private const string Instructions = """
        Click and drag to add control points and their tangents
        R: Remove the last control point
        S: Cycle the spline construction being used
        C: Toggle cyclic curve construction
        """;

    // Bevy's resources, the modes, the control points and the curve they make, and the mouse.
    private static SplineMode _splineMode;
    private static bool _cyclic;
    private static readonly List<(Vec2 Point, Vec2 Tangent)> ControlPoints = [];
    private static CubicCurve<Vec2>? _curve;
    private static Vec2? _mousePosition;
    private static Vec2? _editStart;
    private static Entity _camera, _splineText, _cyclingText;

    public static void Build(App app)
    {
        app.Startup(Setup, "cubic_splines.Setup");

        // Bevy chains its systems, so each sees what the one before it changed this frame.
        app.Update(ctx =>
        {
            var changed = HandleKeypress(ctx);
            HandleMouseMove(ctx);
            changed |= HandleMousePress(ctx);
            DrawEditMove();
            if (changed) UpdateCurve();
            DrawCurve();
            DrawControlPoints();
        }, "cubic_splines.Update");
    }

    private static void Setup(BehaviorContext ctx)
    {
        (_splineMode, _cyclic, _mousePosition, _editStart) = (SplineMode.Hermite, false, null, null);
        ControlPoints.Clear();
        ControlPoints.AddRange([
            (new Vec2(-500f, -200f), new Vec2(0f, 200f)),
            (new Vec2(-250f, 250f), new Vec2(200f, 0f)),
            (new Vec2(250f, 250f), new Vec2(0f, -200f)),
            (new Vec2(500f, -200f), new Vec2(-200f, 0f)),
        ]);
        UpdateCurve();

        _camera = Render2d.SpawnCamera2d();

        // The instructions and the modes on the left, one under another.
        var column = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Top = Length.Px(12f),
            Left = Length.Px(12f),
            Direction = UiDirection.Column,
            RowGap = Length.Px(20f),
        });
        _splineText = Ui.SpawnText(string.Empty, new UiSettings());
        _cyclingText = Ui.SpawnText(string.Empty, new UiSettings());
        ctx.Ecs.SetParent(Ui.SpawnText(Instructions, new UiSettings()), column);
        ctx.Ecs.SetParent(_splineText, column);
        ctx.Ecs.SetParent(_cyclingText, column);
        UpdateTexts();
    }

    // Bevy's form_curve, the curve the control points make in the modes chosen, or none where they
    // are too few.
    private static void UpdateCurve()
    {
        var points = ControlPoints.Select(c => c.Point);
        _curve = _splineMode switch
        {
            SplineMode.Hermite when _cyclic => new CubicHermite<Vec2>(points, ControlPoints.Select(c => c.Tangent)).ToCurveCyclic(),
            SplineMode.Hermite => new CubicHermite<Vec2>(points, ControlPoints.Select(c => c.Tangent)).ToCurve(),
            SplineMode.Cardinal when _cyclic => CubicCardinalSpline<Vec2>.CatmullRom(points).ToCurveCyclic(),
            SplineMode.Cardinal => CubicCardinalSpline<Vec2>.CatmullRom(points).ToCurve(),
            _ when _cyclic => new CubicBSpline<Vec2>(points).ToCurveCyclic(),
            _ => new CubicBSpline<Vec2>(points).ToCurve(),
        };
        UpdateTexts();
    }

    // Bevy's update_spline_mode_text and update_cycling_mode_text.
    private static void UpdateTexts()
    {
        if (_splineText.IsNone) return;
        Ui.SetText(_splineText, $"Spline: {(_splineMode == SplineMode.B ? "B" : _splineMode.ToString())}");
        Ui.SetText(_cyclingText, _cyclic ? "Cyclic" : "Not Cyclic");
    }

    // Bevy's draw_curve, the curve as a hundred lines a segment, so it stays smooth as it grows.
    private static void DrawCurve()
    {
        if (_curve is null) return;
        Vec3[] line = [.. _curve.IterPositions(100 * _curve.Segments.Count).Select(p => new Vec3(p.X, p.Y, 0f))];
        Gizmos.Polyline(line, Color.FromSrgb(1f, 1f, 1f), inFront: true);
    }

    // Bevy's draw_control_points, each a circle, its tangent an arrow where the spline reads it.
    private static void DrawControlPoints()
    {
        foreach (var (point, tangent) in ControlPoints)
        {
            Gizmos.Circle2d((point.X, point.Y), 10f, Color.FromSrgb(0f, 1f, 0f));
            if (_splineMode == SplineMode.Hermite)
            {
                var end = point + tangent;
                Gizmos.Arrow2d((point.X, point.Y), (end.X, end.Y), Color.FromSrgb(1f, 0f, 0f));
            }
        }
    }

    // Bevy's handle_keypress, answering whether the curve has to be made again.
    private static bool HandleKeypress(BehaviorContext ctx)
    {
        var changed = false;
        if (ctx.Input.KeyPressed(Key.S))
        {
            _splineMode = _splineMode switch
            {
                SplineMode.Hermite => SplineMode.Cardinal,
                SplineMode.Cardinal => SplineMode.B,
                _ => SplineMode.Hermite,
            };
            changed = true;
        }

        if (ctx.Input.KeyPressed(Key.C))
        {
            _cyclic = !_cyclic;
            changed = true;
        }

        if (ctx.Input.KeyPressed(Key.R) && ControlPoints.Count > 0)
        {
            ControlPoints.RemoveAt(ControlPoints.Count - 1);
            changed = true;
        }

        return changed;
    }

    // Bevy's handle_mouse_move, the cursor's last place this frame.
    private static void HandleMouseMove(BehaviorContext ctx)
    {
        var moves = ctx.Read<CursorMoved>();
        if (!moves.IsEmpty) _mousePosition = moves[^1].Position;
    }

    // Bevy's handle_mouse_press. A press starts the drag, and the release adds the point where it
    // started with the drag as its tangent, both turned from the window into the world.
    private static bool HandleMousePress(BehaviorContext ctx)
    {
        if (_mousePosition is not { } mouse) return false;

        var added = false;
        foreach (var press in ctx.Read<MouseButtonInput>())
        {
            if (press.Button != MouseButton.Left) continue;
            if (press.State == ButtonState.Pressed)
            {
                _editStart ??= mouse;
                continue;
            }

            if (_editStart is not { } start || World(start) is not { } point || World(mouse) is not { } end) continue;
            ControlPoints.Add((point, end - point));
            _editStart = null;
            added = true;
        }

        return added;
    }

    // Bevy's draw_edit_move, the point being added and the tangent its drag gives it.
    private static void DrawEditMove()
    {
        if (_editStart is not { } from || _mousePosition is not { } mouse || World(from) is not { } start || World(mouse) is not { } end) return;
        Gizmos.Circle2d((start.X, start.Y), 10f, Color.FromSrgb(0f, 1f, 0.7f));
        Gizmos.Circle2d((start.X, start.Y), 7f, Color.FromSrgb(0f, 1f, 0.7f));
        Gizmos.Arrow2d((start.X, start.Y), (end.X, end.Y), Color.FromSrgb(1f, 0f, 0.7f));
    }

    // Bevy's viewport_to_world_2d, the world's point under a point of the window, where the camera
    // draws one.
    private static Vec2? World(Vec2 window) =>
        Render.TryRay(_camera, window.X, window.Y, out var origin, out _) ? new Vec2(origin.X, origin.Y) : null;

    // Bevy's SplineMode.
    private enum SplineMode
    {
        Hermite,
        Cardinal,
        B,
    }
}
