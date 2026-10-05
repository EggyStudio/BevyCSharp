// Bevy's align example, examples/transforms/align.rs at v0.19.1, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Transforms;

// Demonstrates aligning a ship by two of its axes to two directions, its nose to the white arrow
// exactly and its right wing as near the gray one as it can be.
internal static class Align
{
    private const string Instructions =
        "The bright red axis is the primary alignment axis, and it will always be\n"
        + "made to coincide with the primary target direction (white) exactly.\n"
        + "The fainter red axis is the secondary alignment axis, and it is made to\n"
        + "line up with the secondary target direction (gray) as closely as possible.\n"
        + "Press 'R' to generate random target directions.\n"
        + "Press 'T' to align the ship to those directions.\n"
        + "Click and drag the mouse to rotate the camera.\n"
        + "Press 'H' to hide/show these instructions.";

    private static Random _random = new(19878367);
    private static Entity _camera, _ship = Entity.None, _text;
    private static Vec3 _first, _second;
    private static Quat _target;
    private static bool _inMotion, _shown;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_random, _inMotion, _shown, _ship) = (new Random(19878367), false, true, Entity.None);

            _camera = ecs.Camera(Transform.LookingAt(new Vec3(3f, 2.5f, 4f), Vec3.Zero, Vec3.UnitY));
            ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 100f, 100f), Scene.Material(Scene.Srgb(0.3f, 0.5f, 0.3f)), Transform.At(0f, -2f, 0f));
            ecs.PointLight(new Vec3(4f, 7f, -4f), shadows: true);

            (_first, _second) = (RandomDirection(), RandomDirection());
            _target = Aligned(_first, _second);
            _text = Ui.SpawnText(Instructions, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        }, "align.Setup");

        app.SpawnGltf("models/ship/craft_speederD.gltf", (_, ship) => _ship = ship);

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var input = ctx.Input;

            Gizmos.Arrow(Vec3.Zero, _first * 1.5f, (1f, 1f, 1f, 1f));
            Gizmos.Arrow(Vec3.Zero, _second * 1.5f, Scene.Srgb(0.5f, 0.5f, 0.5f));
            if (_ship == Entity.None) return;

            var ship = ecs.GetOrDefault<Transform>(_ship);
            Gizmos.Arrow(ship.Translation, ship.Translation + ship.Rotation * -Vec3.UnitZ * 1.5f, (1f, 0f, 0f, 1f));
            Gizmos.Arrow(ship.Translation, ship.Translation + ship.Rotation * Vec3.UnitX * 1.5f, Scene.Srgb(0.65f, 0f, 0f));

            if (input.KeyPressed(Key.R))
            {
                (_first, _second) = (RandomDirection(), RandomDirection());
                (_inMotion, _target) = (false, Aligned(_first, _second));
            }

            if (input.KeyPressed(Key.T)) _inMotion = !_inMotion;
            if (input.KeyPressed(Key.H)) Ui.SetText(_text, (_shown = !_shown) ? Instructions : string.Empty);

            // A drag turns the camera about the middle, a pixel being a seventy-fifth of a radian.
            if (input.MouseDown(MouseButton.Left) && input.MouseDeltaX != 0f)
            {
                var camera = ecs.GetOrDefault<Transform>(_camera);
                var turn = Quat.FromRotationY(-input.MouseDeltaX / 75f);
                ecs.Set(_camera, new Transform(turn * camera.Translation, turn * camera.Rotation, camera.Scale));
            }

            // Bevy's smooth_nudge, which closes the same share of the gap each second whatever the
            // frame rate.
            if (!_inMotion) return;
            ship.Rotation = Scene.Slerp(ship.Rotation, _target, 1f - MathF.Exp(-3f * ctx.Time.Delta));
            ecs.Set(_ship, ship);
            var dot = MathF.Abs(ship.Rotation.X * _target.X + ship.Rotation.Y * _target.Y + ship.Rotation.Z * _target.Z + ship.Rotation.W * _target.W);
            if (dot >= 1f - 1e-7f) _inMotion = false;
        }, "align.Update");
    }

    // A direction picked evenly over the sphere, from three normally spread numbers.
    private static Vec3 RandomDirection()
    {
        float Normal() => MathF.Sqrt(-2f * MathF.Log(1f - _random.NextSingle())) * MathF.Cos(MathF.Tau * _random.NextSingle());
        return new Vec3(Normal(), Normal(), Normal()).Normalized;
    }

    // Bevy's Transform::aligned_by, the rotation taking the ship's nose, -Z, to first exactly and
    // its right, X, as near second as can be, by building the turned axes and reading the rotation
    // off them.
    private static Quat Aligned(Vec3 first, Vec3 second)
    {
        var back = -first;
        var right = (second - first * Vec3.Dot(second, first)).Normalized;
        var up = Vec3.Cross(back, right);
        return FromAxes(right, up, back);
    }

    // The rotation whose X, Y and Z axes land on the three given, from the matrix they make.
    private static Quat FromAxes(Vec3 x, Vec3 y, Vec3 z)
    {
        var trace = x.X + y.Y + z.Z;
        if (trace > 0f)
        {
            var s = MathF.Sqrt(trace + 1f) * 2f;
            return new Quat((y.Z - z.Y) / s, (z.X - x.Z) / s, (x.Y - y.X) / s, 0.25f * s);
        }

        if (x.X > y.Y && x.X > z.Z)
        {
            var s = MathF.Sqrt(1f + x.X - y.Y - z.Z) * 2f;
            return new Quat(0.25f * s, (y.X + x.Y) / s, (z.X + x.Z) / s, (y.Z - z.Y) / s);
        }

        if (y.Y > z.Z)
        {
            var s = MathF.Sqrt(1f + y.Y - x.X - z.Z) * 2f;
            return new Quat((y.X + x.Y) / s, 0.25f * s, (z.Y + y.Z) / s, (z.X - x.Z) / s);
        }

        var t = MathF.Sqrt(1f + z.Z - x.X - y.Y) * 2f;
        return new Quat((z.X + x.Z) / t, (z.Y + y.Z) / t, 0.25f * t, (x.Y - y.X) / t);
    }
}
