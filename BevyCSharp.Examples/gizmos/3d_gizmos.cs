using Bevy;

namespace BevyCSharp.Examples.Gizmo;

// Shows the gizmos a 3D camera can draw, in two groups set apart as 2d_gizmos sets them, with a
// sphere kept in an asset and drawn by an entity rather than asked for each frame. T draws every
// gizmo over the scene, P measures line widths in perspective, B shows every bounding box, and
// the camera flies with WASD and the mouse.
internal static class Gizmos3d
{
    private static readonly (float R, float G, float B, float A) Crimson = Scene.Srgb8(220, 20, 60), Purple = Scene.Srgb8(128, 0, 128), Green = Scene.Srgb8(0, 128, 0);
    private static readonly (float R, float G, float B, float A) Black = (0f, 0f, 0f, 1f), Lime = Scene.Srgb(0f, 1f, 0f), Fuchsia = Scene.Srgb(1f, 0f, 1f);
    private static readonly (float R, float G, float B, float A) Red = Scene.Srgb(1f, 0f, 0f), Turquoise = Scene.Srgb8(64, 224, 208), Blue = Scene.Srgb(0f, 0f, 1f);
    private static readonly (float R, float G, float B, float A) Orange = Scene.Srgb8(255, 165, 0), Navy = Scene.Srgb8(0, 0, 128), Yellow = Scene.Srgb(1f, 1f, 0f), OrangeRed = Scene.Srgb8(255, 69, 0);

    private static GizmoLines _straight = new(GizmoGroup.Behind), _round = new(GizmoGroup.InFront);
    private static bool _onTop, _bounds;

    public static void Build(App app)
    {
        (_straight, _round, _onTop, _bounds) = (new GizmoLines(GizmoGroup.Behind), new GizmoLines(GizmoGroup.InFront), false, false);
        app.Startup(Setup, "3d_gizmos.Setup");
        app.Update(DrawExampleCollection, "3d_gizmos.DrawExampleCollection");
        app.Update(UpdateConfig, "3d_gizmos.UpdateConfig");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // A sphere drawn with ten thousand segments to each circle, which would cost a great deal
        // asked for every frame and costs nothing kept.
        var sphere = Gizmos.Record(() => Gizmos.Sphere(Vec3.Zero, 0.5f, Crimson, resolution: 30_000 / 3));
        var holder = ecs.Spawn();
        ecs.Add(holder, Transform.At(4f, 1f, 0f));
        Gizmos.Attach(ecs, holder, sphere);
        ecs.Wrap<Bevy.Reflected.GizmoRef>(holder).LineConfigWidth = 5f;

        var camera = ecs.Camera(Transform.LookingAt(new Vec3(0f, 1.5f, 6f), Vec3.Zero, Vec3.UnitY));
        ecs.Add(camera, new FreeCamera());

        ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Scene.Material(Scene.Srgb(0.3f, 0.5f, 0.3f)), Transform.Identity);
        ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Scene.Material(Scene.Srgb(0.8f, 0.7f, 0.6f)), Transform.At(0f, 0.5f, 0f));
        ecs.PointLight(new Vec3(4f, 8f, 4f), shadows: true);

        Ui.SpawnText(
            "Press 'T' to toggle drawing gizmos on top of everything else in the scene\n"
            + "Press 'P' to toggle perspective for line gizmos\n"
            + "Hold 'Left' or 'Right' to change the line width of straight gizmos\n"
            + "Hold 'Up' or 'Down' to change the line width of round gizmos\n"
            + "Press '1' or '2' to toggle the visibility of straight gizmos or round gizmos\n"
            + "Press 'B' to show all AABB boxes\n"
            + "Press 'U' or 'I' to cycle through line styles for straight or round gizmos\n"
            + "Press 'J' or 'K' to cycle through line joins for straight or round gizmos\n"
            + "Press 'Spacebar' to toggle pause",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });

        // The bridge draws its in-front group over the scene, and the example's own group is
        // depth tested like Bevy's until T says otherwise.
        Gizmos.SetDepthBias(0f, GizmoGroup.All);
        _straight.Apply();
        _round.Apply();
    }

    private static void DrawExampleCollection(BehaviorContext ctx)
    {
        var t = ctx.Time.Elapsed;
        const bool Straight = false, Round = true;

        Gizmos.Grid(Vec3.Zero, Quat.FromRotationX(MathF.PI / 2f), 20, 20, 2f, (0.65f, 0.65f, 0.65f, 1f), Straight);
        Gizmos.Grid(new Vec3(10f), Quat.FromRotationX(MathF.PI / 3f * 2f), 20, 20, 2f, Purple, Straight);
        Gizmos.Sphere(new Vec3(10f), 1f, Purple, Straight);

        // Bevy's plane, its normal as an arrow of one unit and a grid of five cells by ten lying
        // across it, which turns as it circles.
        var planeAt = new Vec3(4f + MathF.Sin(t), 4f + MathF.Cos(t), 4f);
        var planeTurn = Quat.FromRotationX(MathF.PI / 2f + t);
        Gizmos.Arrow(planeAt, planeAt + planeTurn * Vec3.UnitY, Green, Straight);
        Gizmos.Grid(planeAt, planeTurn * RotationArc(Vec3.UnitZ, Vec3.UnitY), 5, 10, 0.2f, Green, Straight, spacingDown: 0.1f);

        Gizmos.Box(new Vec3(0f, 0.5f, 0f), Quat.Identity, new Vec3(1.25f), Black, Straight);
        Gizmos.Rect(new Vec3(MathF.Cos(t) * 2.5f, 1f, 0f), Quat.FromRotationY(MathF.PI / 2f), 2f, 2f, Lime, Straight);
        Cross(new Vec3(-1f, 1f, 1f), 0.5f, Fuchsia, Straight);

        // A helix sampled at a resolution that rises and falls, fading from teal to hot pink along
        // its length.
        var resolution = (int)((MathF.Sin(t) + 1f) * 100f);
        if (resolution > 0)
        {
            var (previous, previousColor) = (Vec3.Zero, (0f, 0f, 0f, 0f));
            for (var n = 0; n <= resolution; n++)
            {
                var s = (float)n / resolution * 5f;
                var point = new Vec3(MathF.Sin(s * 10f), MathF.Cos(s * 10f), s - 6f);
                var mix = s / 5f;
                var color = Scene.Srgb(mix, (128f + (105f - 128f) * mix) / 255f, (128f + (180f - 128f) * mix) / 255f);
                if (n > 0) Gizmos.Fade(previous, point, previousColor, color, Straight);
                (previous, previousColor) = (point, color);
            }
        }

        Gizmos.Sphere(new Vec3(1f, 0.5f, 0f), 0.5f, Red, Round);
        Gizmos.RoundedCuboid(new Vec3(-2f, 0.75f, -0.75f), Quat.Identity, new Vec3(0.9f), Turquoise, edgeRadius: 0.1f, inFront: Round, arcResolution: 4);

        // Rays, each a line from a point along a vector.
        foreach (var y in new[] { 0f, 0.5f, 1f })
        {
            var start = new Vec3(1f, y, 0f);
            Gizmos.Line(start, start + new Vec3(-3f, MathF.Sin(t * 3f), 0f), Blue, Straight);
        }

        Gizmos.Arc(Vec3.One, RotationArc(Vec3.UnitY, Vec3.One.Normalized), 0.2f, MathF.PI, Orange, Round, resolution: 10);
        Gizmos.Circle(Vec3.Zero, RotationArc(Vec3.UnitZ, Vec3.UnitY), 3f, Black, Round);
        Gizmos.Circle(Vec3.Zero, RotationArc(Vec3.UnitZ, Vec3.UnitY), 3.1f, Navy, Round, resolution: 64);
        Gizmos.Sphere(Vec3.Zero, 3.2f, Black, Round, resolution: 64);

        Gizmos.Arrow(Vec3.Zero, new Vec3(1.5f), Yellow, Straight);
        Gizmos.Arrow(new Vec3(2f, 0f, 2f), new Vec3(2f, 2f, 2f), OrangeRed, Straight, tipLength: 0.5f, doubleEnd: true);
    }

    // The turn that carries one direction onto another, as Bevy's Quat::from_rotation_arc.
    private static Quat RotationArc(Vec3 from, Vec3 to)
    {
        var axis = Vec3.Cross(from, to);
        var angle = MathF.Acos(Math.Clamp(Vec3.Dot(from, to), -1f, 1f));
        return axis.Length < 1e-6f ? Quat.Identity : Quat.FromAxisAngle(axis.Normalized, angle);
    }

    // Bevy's cross, a line along each axis through the point.
    private static void Cross(Vec3 at, float halfSize, (float R, float G, float B, float A) color, bool inFront)
    {
        foreach (var axis in new[] { Vec3.UnitX, Vec3.UnitY, Vec3.UnitZ })
            Gizmos.Line(at - axis * halfSize, at + axis * halfSize, color, inFront);
    }

    private static void UpdateConfig(BehaviorContext ctx)
    {
        var (input, delta) = (ctx.Input, (float)ctx.Time.RawDeltaSeconds);

        if (input.KeyPressed(Key.T))
        {
            _onTop = !_onTop;
            Gizmos.SetDepthBias(_onTop ? -1f : 0f, GizmoGroup.All);
        }

        if (input.KeyPressed(Key.P))
        {
            _straight.TogglePerspective();
            _round.TogglePerspective();
        }

        if (input.KeyDown(Key.ArrowRight)) _straight.Widen(5f * delta);
        if (input.KeyDown(Key.ArrowLeft)) _straight.Widen(-5f * delta);
        if (input.KeyPressed(Key.Digit1)) _straight.Toggle();
        if (input.KeyPressed(Key.U)) _straight.NextStyle(forward: true);
        if (input.KeyPressed(Key.J)) _straight.NextJoint(forward: true);

        if (input.KeyDown(Key.ArrowUp)) _round.Widen(5f * delta);
        if (input.KeyDown(Key.ArrowDown)) _round.Widen(-5f * delta);
        if (input.KeyPressed(Key.Digit2)) _round.Toggle();
        if (input.KeyPressed(Key.I)) _round.NextStyle(forward: true);
        if (input.KeyPressed(Key.K)) _round.NextJoint(forward: true);

        if (input.KeyPressed(Key.B))
        {
            _bounds = !_bounds;
            Gizmos.ShowBounds(_bounds);
        }

        if (input.KeyPressed(Key.Space))
        {
            if (ctx.Time.Paused) ctx.Time.Resume();
            else ctx.Time.Pause();
        }
    }
}
