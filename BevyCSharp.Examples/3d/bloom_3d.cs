using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Illustrates bloom post-processing using HDR and emissive materials.
//
// Bevy picks each sphere's look by hashing its place with Rust's hasher, so the pattern here is a
// different scatter of the same four looks. The bridge's bloom has intensity, threshold and mode,
// and not Bevy's low-frequency boost, its curvature, its high-pass frequency or its scale, so the
// keys for those do nothing.
internal static class Bloom3d
{
    private static Entity _camera;
    private static Entity _text;
    private static readonly PostSettings Bloom = new() { Bloom = true, BloomIntensity = 0.15f };

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _camera = ecs.Camera(
                Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY),
                new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f) });
            Render.SetPostProcessing(_camera, Bloom);

            // Something bright in a dark place, to see the bloom.
            var blue = Render.CreateMaterial(new MaterialSettings { Emissive = (0f, 0f, 150f, 1f) });
            var white = Render.CreateMaterial(new MaterialSettings { Emissive = (1000f, 1000f, 1000f, 1f) });
            var red = Render.CreateMaterial(new MaterialSettings { Emissive = (50f, 0f, 0f, 1f) });
            var black = Render.CreateMaterial(new MaterialSettings { BaseColor = (0f, 0f, 0f, 1f) });

            var mesh = Render.CreateMesh(MeshShape.Sphere, 0.4f);
            for (var x = -5; x < 5; x++)
            {
                for (var z = -5; z < 5; z++)
                {
                    // A fixed hash of the place, the same on every run, as Bevy's is.
                    var hash = (uint)((x * 73856093) ^ (z * 19349663)) + 3u;
                    var (material, scale) = (hash % 6) switch
                    {
                        0 => (blue, 0.5f),
                        1 => (white, 0.1f),
                        2 => (red, 1f),
                        _ => (black, 1.5f),
                    };
                    var sphere = ecs.Mesh(mesh, material, new Transform(new Vec3(x * 2f, 0f, z * 2f), Quat.Identity, new Vec3(scale)));
                    ecs.Add(sphere, new Bouncing());
                }
            }

            _text = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
        });

        app.Update(ctx =>
        {
            var input = ctx.Input;
            var step = ctx.Time.Delta / 10f;

            if (input.KeyPressed(Key.Space)) Bloom.Bloom = !Bloom.Bloom;
            if (input.KeyDown(Key.A)) Bloom.BloomIntensity -= step;
            if (input.KeyDown(Key.Q)) Bloom.BloomIntensity += step;
            Bloom.BloomIntensity = Math.Clamp(Bloom.BloomIntensity, 0f, 1f);
            if (input.KeyDown(Key.G)) Bloom.BloomMode = BloomMode.Additive;
            if (input.KeyDown(Key.T)) Bloom.BloomMode = BloomMode.EnergyConserving;
            if (input.KeyDown(Key.H)) Bloom.BloomThreshold -= step;
            if (input.KeyDown(Key.Y)) Bloom.BloomThreshold += step;
            Bloom.BloomThreshold = Math.Max(Bloom.BloomThreshold, 0f);
            if (input.KeyDown(Key.J)) Bloom.BloomThresholdSoftness -= step;
            if (input.KeyDown(Key.U)) Bloom.BloomThresholdSoftness += step;
            Bloom.BloomThresholdSoftness = Math.Clamp(Bloom.BloomThresholdSoftness, 0f, 1f);

            if (input.AnyKeyDown() || input.KeyPressed(Key.Space)) Render.SetPostProcessing(_camera, Bloom);

            Ui.SetText(_text, Bloom.Bloom
                ? "Bloom (Toggle: Space)\n"
                  + $"(Q/A) Intensity: {Bloom.BloomIntensity:0.00}\n"
                  + $"(T/G) Mode: {(Bloom.BloomMode == BloomMode.Additive ? "Additive" : "Energy-conserving")}\n"
                  + $"(Y/H) Threshold: {Bloom.BloomThreshold:0.00}\n"
                  + $"(U/J) Threshold softness: {Bloom.BloomThresholdSoftness:0.00}\n"
                : "Bloom: Off (Toggle: Space)");
        }, "bloom_3d.UpdateBloomSettings");
    }
}

/// <summary>Rises and falls with time, by where it is.</summary>
[Behavior]
public partial struct Bouncing
{
    [OnUpdate]
    public void Bounce(BehaviorContext ctx, ref Transform transform) =>
        transform.Translation = transform.Translation with { Y = MathF.Sin(transform.Translation.X + transform.Translation.Z + ctx.Time.Elapsed) };
}
