// Bevy's spotlight example, examples/3d/spotlight.rs at v0.20.0, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Illustrates spot lights. Sixteen of them sway over forty cubes scattered at random, which are
// drawn from .NET's generator rather than Bevy's, so they lie where they lie rather than where
// Bevy's do.
internal static class Spotlight
{
    private static Entity _camera;

    public static void Build(App app)
    {
        app.Startup(Setup);
        app.Update(Update, "spotlight.Update");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render.SetAmbientLight((1f, 1f, 1f), 20f);

        var plane = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 100f, 100f), Render.CreateMaterial((1f, 1f, 1f, 1f)), Transform.Identity);
        ecs.Add(plane, new Moved());

        var random = new Random(19878367);
        var cube = Render.CreateMesh(MeshShape.Cuboid, 0.5f, 0.5f, 0.5f);
        var blue = Render.CreateMaterial(Color.FromSrgb8(124, 144, 255));
        for (var i = 0; i < 40; i++)
        {
            var at = Transform.At(Between(random, -5f, 5f), Between(random, 0f, 3f), Between(random, -5f, 5f));
            ecs.Add(ecs.SpawnMesh(cube, blue, at), new Moved());
        }

        var dot = Render.CreateMesh(MeshShape.Sphere, 0.05f);
        var pointer = Render.CreateMesh(MeshShape.Sphere, 0.1f);
        var red = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 0f, 0f, 1f), Emissive = (1f, 0f, 0f, 1f) });
        var maroon = Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb8(128, 0, 0), Emissive = (0.369f, 0f, 0f, 1f) });

        for (var x = 0; x < 4; x++)
        {
            for (var z = 0; z < 4; z++)
            {
                var light = Render.SpawnLight(new LightSettings
                {
                    Kind = LightKind.Spot,
                    Intensity = 40_000f,
                    Shadows = true,
                    InnerAngle = MathF.PI / 4f * 0.85f,
                    OuterAngle = MathF.PI / 4f,
                });
                ecs.Add(light, Transform.LookingAt(new Vec3(1f + x - 2f, 2f, z - 2f), new Vec3(1f + x - 2f, 0f, z - 2f), Vec3.UnitX));
                ecs.Add(light, new Sway());

                ecs.SetParent(ecs.SpawnMesh(dot, red, Transform.Identity), light);
                var tip = ecs.SpawnMesh(pointer, maroon, Transform.At(0f, 0f, -0.1f));
                Render.SetMeshFlags(ecs, tip, MeshFlags.NoShadowCasting);
                ecs.SetParent(tip, light);
            }
        }

        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-4f, 5f, 10f), Vec3.Zero, Vec3.UnitY));
        Render.SetPostProcessing(_camera, new PostSettings { Hdr = true });

        Ui.SpawnText(
            "Controls\n--------\nHorizontal Movement: WASD\nVertical Movement: Space and Shift\nRotate Camera: Left and Right Arrows",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    private static void Update(BehaviorContext ctx)
    {
        var elapsed = ctx.Time.Elapsed;

        // Every light sways the same way, and its cone opens and closes as it does. Bevy's XYZ
        // order is X, then Y, then Z, outermost first.
        var rotation = Quat.FromRotationX(-MathF.PI / 2f + MathF.Sin(elapsed * 0.67f * 3f) * 0.5f) * Quat.FromRotationY(MathF.Sin(elapsed * 3f) * 0.5f);
        var angle = (MathF.Sin(elapsed * 1.2f) + 1f) * (MathF.PI / 4f - 0.1f);
        var (inner, outer) = (angle * 0.8f, angle);

        foreach (var row in ctx.Ecs.Query<Sway>(markChanged: false))
        {
            var transform = ctx.Ecs.GetOrDefault<Transform>(row.Entity);
            transform.Rotation = rotation;
            ctx.Ecs.Set(row.Entity, transform);
            var spot = ctx.Ecs.Wrap<SpotLightRef>(row.Entity);
            (spot.InnerAngle, spot.OuterAngle) = (inner, outer);
        }

        var delta = ctx.Time.Delta;
        var turn = ctx.Input.KeyDown(Key.ArrowLeft) ? delta : ctx.Input.KeyDown(Key.ArrowRight) ? -delta : 0f;
        if (turn == 0f) return;

        // Turned about the origin, as Bevy's rotate_around does.
        var camera = ctx.Ecs.GetOrDefault<Transform>(_camera);
        var spin = Quat.FromRotationY(turn);
        camera.Translation = spin * camera.Translation;
        camera.Rotation = spin * camera.Rotation;
        ctx.Ecs.Set(_camera, camera);
    }

    private static float Between(Random random, float low, float high) => low + (float)random.NextDouble() * (high - low);
}

/// <summary>What WASD, Space and Shift move.</summary>
[Behavior]
public partial struct Moved
{
    [OnUpdate]
    public void Move(BehaviorContext ctx, ref Transform transform)
    {
        var input = ctx.Input;
        var z = input.KeyDown(Key.W) ? 1f : input.KeyDown(Key.S) ? -1f : 0f;
        var x = input.KeyDown(Key.A) ? 1f : input.KeyDown(Key.D) ? -1f : 0f;
        var y = input.KeyDown(Key.ShiftLeft) ? 1f : input.KeyDown(Key.Space) ? -1f : 0f;
        transform.Translation += new Vec3(x, y, z) * (2f * ctx.Time.Delta);
    }
}

/// <summary>A light that sways.</summary>
[Behavior]
public partial struct Sway;
