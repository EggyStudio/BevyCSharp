// Bevy's motion_blur example, examples/3d/motion_blur.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates per-pixel motion blur, with cars racing round a track lined with trees and barriers
// and a camera chasing one or tracking it from the side.
internal static class MotionBlur
{
    private const int Cars = 20;
    private const int Trees = 30;
    private const int Cones = 100;

    private sealed record Car(Entity Body, Entity[] Wheels, float Offset);

    private static readonly List<Car> Racers = [];
    private static Entity _camera, _text;
    private static float _shutterAngle;
    private static uint _samples;
    private static bool _chase;

    public static void Build(App app)
    {
        app.Startup(Setup);
        app.Update(Update, "motion_blur.Update");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Racers.Clear();
        (_shutterAngle, _samples, _chase) = (1f, 2, true);

        _camera = ecs.SpawnCamera3d(Transform.Identity);
        ApplyBlur();

        Render.SetAmbientLight((1f, 1f, 1f), 300f);

        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 3000f, Shadows = true });
        ecs.Add(sun, Transform.LookingAt(Vec3.Zero, new Vec3(-1f, -0.7f, -1f), Vec3.UnitX));

        // The sky, a sphere around everything seen from inside.
        ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Sphere, 0.5f),
            Render.CreateMaterial(new MaterialSettings { Unlit = true, BaseColor = (0.1f, 0.6f, 1f, 1f) }),
            new Transform(Vec3.Zero, Quat.Identity, new Vec3(-4000f)));

        // The ground, a gray check repeated four thousand times across it.
        var ground = new MeshData
        {
            Positions = [new(-0.5f, 0f, -0.5f), new(0.5f, 0f, -0.5f), new(0.5f, 0f, 0.5f), new(-0.5f, 0f, 0.5f)],
            Normals = [Vec3.UnitY, Vec3.UnitY, Vec3.UnitY, Vec3.UnitY],
            Uvs = [4000f, 0f, 0f, 0f, 0f, 4000f, 4000f, 4000f],
            Indices = [0, 3, 2, 0, 2, 1],
        };
        var checker = UvDebugTexture();
        ecs.SpawnMesh(
            Render.CreateMesh(ground),
            Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Roughness = 1f, BaseColorTexture = checker }),
            new Transform(new Vec3(0f, -0.65f, 0f), Quat.Identity, new Vec3(80f)));

        SpawnCars(ecs);
        SpawnTrees(ecs);
        SpawnBarriers(ecs);

        _text = Ui.SpawnText(Text(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    private static void SpawnCars(EcsWorld ecs)
    {
        var box = Render.CreateMesh(MeshShape.Cuboid, 0.3f, 0.15f, 0.55f);
        var cylinder = Render.CreateMesh(MeshShape.Cylinder, 0.5f, 1f);
        var wheelMaterial = Render.CreateMaterial(new MaterialSettings { BaseColorTexture = AssetServer.Load(AssetKind.Image, "branding/icon.png") });
        var colors = new (float, float, float)[] { (1f, 0f, 0f), (1f, 1f, 0f), (0f, 0f, 0f), (0f, 0f, 1f), (0f, 1f, 0f), (1f, 0f, 1f), (0.5f, 0.5f, 0f), (1f, 0.5f, 0f) }
            .Select(c => Render.CreateMaterial(new MaterialSettings { BaseColor = (c.Item1, c.Item2, c.Item3, 1f) }))
            .ToArray();

        for (var i = 0; i < Cars; i++)
        {
            var color = colors[i % colors.Length];
            var car = ecs.SpawnMesh(box, color, new Transform(Vec3.Zero, Quat.Identity, new Vec3(0.5f)));
            ecs.SetParent(ecs.SpawnMesh(box, color, new Transform(new Vec3(0f, 0.08f, 0.03f), Quat.Identity, new Vec3(1f, 1f, 0.5f))), car);

            var wheels = new List<Entity>();
            foreach (var (x, z) in new[] { (1f, 1f), (1f, -1f), (-1f, 1f), (-1f, -1f) })
            {
                var wheel = ecs.SpawnMesh(cylinder, wheelMaterial, new Transform(new Vec3(0.14f * x, -0.045f, 0.15f * z), Quat.FromRotationZ(MathF.PI / 2f), new Vec3(0.15f, 0.04f, 0.15f)));
                ecs.SetParent(wheel, car);
                wheels.Add(wheel);
            }

            Racers.Add(new Car(car, [.. wheels], i * 2f));
        }
    }

    private static void SpawnTrees(EcsWorld ecs)
    {
        var capsule = Render.CreateMesh(MeshShape.Capsule, 0.5f, 1f);
        var sphere = Render.CreateMesh(MeshShape.Sphere, 0.5f);
        var leaves = Render.CreateMaterial(new MaterialSettings { BaseColor = (0f, 1f, 0f, 1f) });
        var trunk = Render.CreateMaterial(new MaterialSettings { BaseColor = (0.4f, 0.2f, 0.2f, 1f) });

        foreach (var offset in new[] { 0.07f, -0.07f })
        {
            for (var i = 0; i < Trees; i++)
            {
                var (x, z) = TrackPosition(offset, i / (float)Trees * MathF.PI * 2f);
                ecs.SpawnMesh(sphere, leaves, new Transform(new Vec3(x, -0.3f, z), Quat.Identity, new Vec3(0.3f)));
                ecs.SpawnMesh(capsule, trunk, new Transform(new Vec3(x, -0.5f, z), Quat.Identity, new Vec3(0.05f, 0.3f, 0.05f)));
            }
        }
    }

    private static void SpawnBarriers(EcsWorld ecs)
    {
        var capsule = Render.CreateMesh(MeshShape.Capsule, 0.5f, 1f);
        var orange = Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb8(255, 87, 51), Reflectance = 1f });

        foreach (var offset in new[] { 0.04f, -0.04f })
        {
            for (var i = 0; i < Cones; i++)
            {
                var (x, z) = TrackPosition(offset, i / (float)Cones * MathF.PI * 2f);
                ecs.SpawnMesh(capsule, orange, new Transform(new Vec3(x, -0.65f, z), Quat.Identity, new Vec3(0.07f)));
            }
        }
    }

    private static void Update(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var changed = true;
        if (input.KeyPressed(Key.Digit1)) _shutterAngle -= 0.25f;
        else if (input.KeyPressed(Key.Digit2)) _shutterAngle += 0.25f;
        else if (input.KeyPressed(Key.Digit3)) _samples = _samples > 0 ? _samples - 1 : 0;
        else if (input.KeyPressed(Key.Digit4)) _samples++;
        else if (input.KeyPressed(Key.Space)) _chase = !_chase;
        else changed = false;

        _shutterAngle = Math.Clamp(_shutterAngle, 0f, 1f);
        _samples = Math.Min(_samples, 64u);
        if (changed)
        {
            ApplyBlur();
            Ui.SetText(_text, Text());
        }

        MoveCars(ctx);
        MoveCamera(ctx.Ecs);
    }

    // Round the track, faster on its straights, each wheel turned by how far its car went.
    private static void MoveCars(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var car in Racers)
        {
            var t = ctx.Time.Elapsed * 0.25f + 0.5f * car.Offset;
            var dx = MathF.Cos(t);
            var dz = -MathF.Sin(3f * t);
            t += MathF.Sqrt(dx * dx + dz * dz) * 0.15f;

            var transform = ecs.GetOrDefault<Transform>(car.Body);
            var previous = transform.Translation;
            var (x, z) = TrackPosition(0f, t);
            var now = new Vec3(x, -0.59f, z);
            var delta = now - previous;
            if (delta.Length > 1e-6f) transform = Transform.LookingAt(now, now + delta, Vec3.UnitY) with { Scale = transform.Scale };
            transform.Translation = now;
            ecs.Set(car.Body, transform);

            foreach (var wheel in car.Wheels)
            {
                var spin = ecs.GetOrDefault<Transform>(wheel);
                var circumference = 2f * MathF.PI * spin.Scale.X;
                spin.Rotation *= Quat.FromRotationY(delta.Length / circumference * MathF.PI * 2f);
                ecs.Set(wheel, spin);
            }
        }
    }

    private static void MoveCamera(EcsWorld ecs)
    {
        var tracked = ecs.GetOrDefault<Transform>(Racers[0].Body);
        var forward = tracked.Rotation * -Vec3.UnitZ;

        if (_chase)
        {
            var at = tracked.Translation + new Vec3(0f, 0.15f, 0f) - forward * 0.6f;
            ecs.Set(_camera, Transform.LookingAt(at, at + forward, Vec3.UnitY));
            Render.SetPerspective(_camera, 1f * 180f / MathF.PI, 0.1f, 1000f);
        }
        else
        {
            ecs.Set(_camera, Transform.LookingAt(new Vec3(15f, -0.5f, 0f), tracked.Translation, Vec3.UnitY));
            Render.SetPerspective(_camera, 0.05f * 180f / MathF.PI, 0.1f, 1000f);
        }
    }

    private static void ApplyBlur() => Render.SetEffects(_camera, new EffectSettings { ShutterAngle = _shutterAngle, MotionBlurSamples = _samples });

    // A point on the figure the cars race round, offset to one side of it.
    private static (float X, float Z) TrackPosition(float offset, float t)
    {
        const float XTweak = 2f, YTweak = 3f, Scale = 8f;
        var x0 = MathF.Sin(XTweak * t);
        var y0 = MathF.Cos(YTweak * t);
        var dx = XTweak * MathF.Cos(XTweak * t);
        var dy = YTweak * -MathF.Sin(YTweak * t);
        var length = MathF.Sqrt(dx * dx + dy * dy);
        return ((x0 + offset * dy / length) * Scale, (y0 - offset * dx / length) * Scale);
    }

    // A seven by seven gray check, each row the palette turned three pixels further, repeated.
    private static AssetHandle UvDebugTexture()
    {
        const int Size = 7;
        byte[] palette = [164, 164, 164, 255, 168, 168, 168, 255, 153, 153, 153, 255, 139, 139, 139, 255, 153, 153, 153, 255, 177, 177, 177, 255, 159, 159, 159, 255];
        var pixels = new byte[Size * Size * 4];
        for (var y = 0; y < Size; y++)
        {
            palette.CopyTo(pixels, Size * y * 4);
            palette = [.. palette[^12..], .. palette[..^12]];
        }

        var image = Render.CreateImage(pixels, Size, Size);
        Render.SetSampler(image, new TextureSettings { Wrap = TextureWrap.Repeat, MinFilter = TextureFilter.Linear, MipmapFilter = TextureFilter.Linear });
        return image;
    }

    private static string Text() =>
        FormattableString.Invariant($"Shutter angle: {_shutterAngle:0.00}\nSamples: {_samples}\n")
        + "1/2: -/+ shutter angle (blur amount)\n3/4: -/+ sample count (blur quality)\nSpacebar: cycle camera\n";
}
