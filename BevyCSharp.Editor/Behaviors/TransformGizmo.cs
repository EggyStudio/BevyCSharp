using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Behaviors;

/// <summary>
/// Dragging the selection about with the mouse.
/// </summary>
/// <remarks>
/// <para>
/// The handles are drawn by <see cref="ViewportGizmos"/> and grabbed here. A handle is picked in
/// screen space, because that is where the pointer is: the axis is projected onto the viewport and
/// whichever line the cursor is nearest to, within a few pixels, is the one grabbed.
/// </para>
/// <para>
/// What happens then is answered in the world rather than in pixels. A move is where the cursor's
/// ray comes closest to the axis; a turn is where the ray meets the plane the axis is normal to; a
/// stretch is how far along the axis that closest point has travelled. Dragging in pixels and
/// scaling by some factor is what makes a gizmo feel like it is guessing.
/// </para>
/// </remarks>
[Behavior]
public partial struct TransformGizmo
{
    /// <summary>Which axis is being dragged, or -1.</summary>
    internal static int Axis { get; private set; } = -1;

    /// <summary>The frame a drag last ended on.</summary>
    private static ulong _released;

    /// <summary>
    /// Whether a handle is being dragged, or was let go on this frame.
    /// </summary>
    /// <remarks>
    /// The frame matters. A drag ends on a release, and the same release is what everything else
    /// reads as a click; whoever asks may run before or after the drag has finished, so the answer
    /// has to outlast the moment the axis is given up.
    /// </remarks>
    internal static bool DraggingOn(ulong frame) => Axis >= 0 || _released == frame;

    /// <summary>How near the cursor has to be to a handle to grab it, in logical pixels.</summary>
    private const float Grab = 12f;

    /// <summary>The entity being dragged.</summary>
    private static Entity _subject = Entity.None;

    /// <summary>What it was before the drag, so the drag is one change rather than many.</summary>
    private static Transform _before;

    /// <summary>Where along the axis, or at what angle, the drag started.</summary>
    private static float _start;

    /// <summary>Reads the pointer and moves, turns or stretches the selection.</summary>
    [OnUpdate]
    public static void Drag(BehaviorContext ctx)
    {
        if (!App.HasEditor || !App.HasRenderer) return;

        var camera = EditorSelection.Camera;
        if (camera.IsNone) return;

        var input = ctx.Input;
        var (x, y) = input.MousePosition;

        if (input.MouseReleased(MouseButton.Left)) Finish(ctx);

        if (input.MousePressed(MouseButton.Left) && Axis < 0) Begin(ctx, camera, x, y);

        if (Axis < 0 || !input.MouseDown(MouseButton.Left)) return;

        Apply(ctx, camera, x, y);
    }

    /// <summary>Grabs a handle, if the press landed on one.</summary>
    private static void Begin(BehaviorContext ctx, Entity camera, float x, float y)
    {
        if (EditorTools.Current == EditorTool.Select) return;
        if (!EditorSelection.Any) return;
        if (EditorShell.PointerOverPanel(x, y)) return;

        var entity = EditorSelection.Current;
        if (!ctx.Ecs.TryGet<Transform>(entity, out var transform)) return;
        if (!Render.TryGetBounds(entity, out var min, out var max)) return;

        var centre = (min + max) * 0.5f;
        var reach = ViewportGizmos.Reach(camera, centre);

        var nearest = -1;
        var closest = Grab;

        for (var i = 0; i < 3; i++)
        {
            var distance = ToHandle(camera, centre, reach, i, x, y);
            if (distance is not { } near || near >= closest) continue;

            closest = near;
            nearest = i;
        }

        if (nearest < 0) return;

        Axis = nearest;
        _subject = entity;
        _before = transform;

        // The point the handles are drawn about, kept for the whole drag. It is the middle of what
        // is on screen rather than the entity's own origin, and the two are not the same thing for
        // a mesh whose origin sits in a corner: measuring a turn about one while the ring is drawn
        // about the other is a gizmo that answers to a place nobody can see.
        _centre = centre;
        _start = Measure(ctx, camera, x, y, centre) ?? 0f;
    }

    /// <summary>Where the handles are drawn about, for as long as one is held.</summary>
    private static Vec3 _centre;

    /// <summary>
    /// How near the pointer is to one handle, in pixels, or nothing when it cannot be seen.
    /// </summary>
    /// <remarks>
    /// A handle is grabbed by what it looks like. Move and scale draw a line out along the axis
    /// and are measured against that line; a turn draws a ring in the plane the axis is normal to,
    /// and measuring that against the line as well is why a ring could only be grabbed near its
    /// centre — where nothing is drawn — and never on the part a hand reaches for.
    /// </remarks>
    private static float? ToHandle(
        Entity camera, Vec3 centre, float reach, int index, float x, float y)
    {
        var axis = ViewportGizmos.Axes[index];

        if (EditorTools.Current != EditorTool.Rotate)
        {
            if (!Render.TryProject(camera, centre, out var fromX, out var fromY)) return null;
            if (!Render.TryProject(camera, centre + (axis * reach), out var toX, out var toY))
            {
                return null;
            }

            return ToSegment(x, y, fromX, fromY, toX, toY);
        }

        // The ring, as the line the eye follows: a few dozen points around it, each pair a
        // segment. The projection is not a circle on screen unless the camera is looking straight
        // down the axis, so walking it is both simpler and truer than solving for an ellipse.
        var (first, second) = ViewportGizmos.Perpendiculars(axis);
        const int Steps = 32;

        float? closest = null;
        var haveLast = Render.TryProject(camera, centre + (first * reach), out var lastX, out var lastY);

        for (var step = 1; step <= Steps; step++)
        {
            var angle = step / (float)Steps * MathF.Tau;
            var point = centre
                + (first * (MathF.Cos(angle) * reach))
                + (second * (MathF.Sin(angle) * reach));

            var have = Render.TryProject(camera, point, out var pointX, out var pointY);

            if (haveLast && have)
            {
                var distance = ToSegment(x, y, lastX, lastY, pointX, pointY);
                if (closest is not { } best || distance < best) closest = distance;
            }

            (haveLast, lastX, lastY) = (have, pointX, pointY);
        }

        return closest;
    }

    /// <summary>Applies the drag to the selection.</summary>
    private static void Apply(BehaviorContext ctx, Entity camera, float x, float y)
    {
        if (!ctx.Ecs.TryGet<Transform>(_subject, out var current)) return;

        if (Measure(ctx, camera, x, y, _centre) is not { } now) return;

        var axis = ViewportGizmos.Axes[Axis];

        switch (EditorTools.Current)
        {
            case EditorTool.Move:
                var moved = EditorTools.Snapped(now - _start, EditorTools.MoveStep);
                current.Translation = _before.Translation + (axis * moved);
                break;

            case EditorTool.Rotate:
                var degrees = EditorTools.Snapped(
                    (now - _start) * 180f / MathF.PI, EditorTools.RotateStep);

                current.Rotation = Quat.FromAxisAngle(axis, degrees * MathF.PI / 180f)
                    * _before.Rotation;
                break;

            case EditorTool.Scale:
                // How far the grabbed point has travelled, as a fraction of where it started,
                // which is what makes dragging outwards grow the thing rather than move it.
                var reference = MathF.Abs(_start) < 0.001f ? 1f : _start;
                var factor = EditorTools.Snapped(now / reference, EditorTools.ScaleStep);

                current.Scale = new Vec3(
                    Axis == 0 ? _before.Scale.X * factor : _before.Scale.X,
                    Axis == 1 ? _before.Scale.Y * factor : _before.Scale.Y,
                    Axis == 2 ? _before.Scale.Z * factor : _before.Scale.Z);
                break;
        }

        ctx.Ecs.Set(_subject, current);
    }

    /// <summary>Finishes a drag and records it as one change.</summary>
    private static void Finish(BehaviorContext ctx)
    {
        if (Axis < 0) return;

        _released = ctx.Time.FrameCount;

        var entity = _subject;
        var before = _before;

        Axis = -1;
        _subject = Entity.None;

        if (!ctx.Ecs.TryGet<Transform>(entity, out var after)) return;
        if (after.Translation == before.Translation
            && after.Rotation == before.Rotation
            && after.Scale == before.Scale)
        {
            return;
        }

        EditorHistory.Record(
            EditorTools.Current.ToString().ToLowerInvariant(),
            world => world.Set(entity, before),
            world => world.Set(entity, after));
    }

    /// <summary>
    /// Where the pointer is, in whatever the current tool measures in.
    /// </summary>
    /// <remarks>
    /// Distance along the axis for a move and a stretch, and an angle about it for a turn. Both
    /// are answered against the cursor's ray in the world rather than against pixels, which is
    /// what keeps a drag exact however the camera is angled.
    /// </remarks>
    private static float? Measure(
        BehaviorContext ctx, Entity camera, float x, float y, Vec3 centre)
    {
        if (!Render.TryRay(camera, x, y, out var origin, out var direction)) return null;

        var axis = ViewportGizmos.Axes[Axis];

        if (EditorTools.Current == EditorTool.Rotate)
        {
            // Where the ray meets the plane the ring lies in, as an angle about the axis.
            var denominator = Vec3.Dot(direction, axis);
            if (MathF.Abs(denominator) < 1e-4f) return null;

            var travel = Vec3.Dot(centre - origin, axis) / denominator;
            if (travel <= 0f) return null;

            var point = origin + (direction * travel) - centre;
            var (first, second) = ViewportGizmos.Perpendiculars(axis);

            return MathF.Atan2(Vec3.Dot(point, second), Vec3.Dot(point, first));
        }

        // The point on the axis nearest the ray, as a distance from the centre. Two lines that
        // do not meet still have a pair of closest points, and that is the honest answer to
        // where a drag along an axis has reached.
        var u = axis;
        var v = direction;

        var b = Vec3.Dot(u, v);
        var determinant = 1f - (b * b);
        if (MathF.Abs(determinant) < 1e-5f) return null;

        var w = centre - origin;
        var d = Vec3.Dot(u, w);
        var e = Vec3.Dot(v, w);

        return ((b * e) - d) / determinant;
    }

    /// <summary>How far a point is from a line segment, in the plane both are drawn on.</summary>
    private static float ToSegment(
        float x, float y, float ax, float ay, float bx, float by)
    {
        var dx = bx - ax;
        var dy = by - ay;
        var length = (dx * dx) + (dy * dy);

        if (length < 1e-4f) return MathF.Sqrt(((x - ax) * (x - ax)) + ((y - ay) * (y - ay)));

        var along = Math.Clamp((((x - ax) * dx) + ((y - ay) * dy)) / length, 0f, 1f);
        var px = ax + (dx * along);
        var py = ay + (dy * along);

        return MathF.Sqrt(((x - px) * (x - px)) + ((y - py) * (y - py)));
    }
}
