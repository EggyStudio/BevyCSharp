using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates casting rays at meshes and chaining them, bouncing a laser off the inside walls of a
// box. Bevy casts with its MeshRayCast system parameter, and here with Picking.TryCast, which casts
// the same way and hands back the surface's normal to reflect about.
internal static class MeshRayCast
{
    private const int MaxBounces = 64;
    private const float LaserSpeed = 0.03f;

    private static Entity _camera;

    // Bevy's RayMap holds a ray only while the pointer is over the window, which is known here by
    // the window saying the cursor came in or left, or by the mouse moving.
    private static bool _cursorInside;

    public static void Build(App app)
    {
        _cursorInside = false;

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render.SetClearColor((0f, 0f, 0f, 1f));

            // A box of planes facing inward, so the laser is trapped inside.
            var plane = Render.CreateMesh(MeshShape.Plane, 1f, 1f);
            var gray = Scene.Srgb(0.5f, 0.5f, 0.5f, 0.01f);
            var material = Render.CreateMaterial(new MaterialSettings { BaseColor = gray, AlphaMode = AlphaMode.Blend });
            void Wall(Vec3 at, Vec3 axis, float angle) =>
                ecs.Mesh(plane, material, new Transform(at, Quat.FromAxisAngle(axis, angle), Vec3.One));

            Wall(new Vec3(0f, 0.5f, 0f), Vec3.UnitX, MathF.PI);
            Wall(new Vec3(0f, -0.5f, 0f), Vec3.UnitX, 0f);
            Wall(new Vec3(0.5f, 0f, 0f), Vec3.UnitZ, MathF.PI / 2f);
            Wall(new Vec3(-0.5f, 0f, 0f), Vec3.UnitZ, -MathF.PI / 2f);
            Wall(new Vec3(0f, 0f, 0.5f), Vec3.UnitX, -MathF.PI / 2f);
            Wall(new Vec3(0f, 0f, -0.5f), Vec3.UnitX, MathF.PI / 2f);

            // Bevy's EulerRot::XYZ, turned about X and then about the turned Y.
            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional });
            ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationX(-0.1f) * Quat.FromRotationY(0.2f), Vec3.One));

            _camera = ecs.Camera(Transform.LookingAt(new Vec3(1.5f, 1.5f, 1.5f), Vec3.Zero, Vec3.UnitY));
            Render.SetPostProcessing(_camera, new PostSettings { Bloom = true });
        }, "mesh_ray_cast.Setup");

        app.Update(ctx =>
        {
            // A ray that moves by itself, bounced off the walls.
            var t = MathF.Cos(MathF.Max(ctx.Time.Elapsed - 4f, 0f) * LaserSpeed) * MathF.PI;
            var origin = new Vec3(MathF.Sin(t), MathF.Cos(3f * t) * 0.5f, MathF.Cos(t)) * 0.5f;
            Gizmos.Sphere(origin, 0.1f, (1f, 1f, 1f, 1f), inFront: false);
            BounceRay(origin, -origin, red: true);

            // And one from the cursor.
            foreach (var _ in ctx.Read<CursorEntered>()) _cursorInside = true;
            foreach (var _ in ctx.Read<CursorLeft>()) _cursorInside = false;
            if (ctx.Input.MouseDelta != (0f, 0f)) _cursorInside = true;

            var (x, y) = ctx.Input.MousePosition;
            if (_cursorInside && Render.TryRay(_camera, x, y, out var from, out var towards))
                BounceRay(from, towards, red: false);
        }, "mesh_ray_cast.BouncingRaycast");
    }

    // Bounces a ray off the surfaces it meets, up to MaxBounces times, and draws its path brighter
    // where it began.
    private static void BounceRay(Vec3 origin, Vec3 direction, bool red)
    {
        var points = new List<(Vec3 At, (float R, float G, float B, float A) Color)> { (origin, Scene.Srgb(30f, 0f, 0f)) };

        for (var i = 0; i < MaxBounces; i++)
        {
            if (!Picking.TryCast(origin, direction, out _, out var point, out var normal)) break;

            // Bevy mixes black toward the color past it, which in sRGB is the color scaled.
            var brightness = 1f + 10f * (1f - (float)i / MaxBounces);
            points.Add((point, Shade(red, brightness)));
            Gizmos.Sphere(point, 0.005f, Shade(red, brightness * 2f), inFront: false);

            // Reflect the ray off the surface.
            direction = (direction - 2f * Vec3.Dot(direction, normal) * normal).Normalized;
            origin = point + direction * 1e-6f;
        }

        if (points.Count < 2) return;
        var segments = new GizmoSegment[points.Count - 1];
        for (var i = 0; i < segments.Length; i++)
            segments[i] = GizmoSegment.Fading(points[i].At, points[i + 1].At, points[i].Color, points[i + 1].Color);
        Gizmos.Lines(segments);
    }

    // CSS red or green, from black, scaled by brightness.
    private static (float R, float G, float B, float A) Shade(bool red, float brightness) =>
        red ? Scene.Srgb(brightness, 0f, 0f) : Scene.Srgb(0f, 0.5f * brightness, 0f);
}
