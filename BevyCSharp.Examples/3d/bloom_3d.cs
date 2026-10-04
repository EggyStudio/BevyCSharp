using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Illustrates bloom post-processing using HDR and emissive materials.
//
// Bevy picks each sphere's look by hashing its place with Rust's hasher, so the pattern here is a
// different scatter of the same four looks. Intensity, threshold and mode are the camera's
// post-processing settings, and the low-frequency boost, its curvature, the high-pass frequency and
// the scale are Bevy's Bloom component's fields, set through reflection.
internal static class Bloom3d
{
    private static Entity _camera;
    private static Entity _text;
    private static readonly PostSettings Bloom = new() { Bloom = true, BloomIntensity = 0.15f };

    // The fields the post-processing settings do not hold, at Bevy's natural bloom, kept here and
    // written to the camera's Bloom after the settings are, since writing those makes it again.
    private static float _boost = 0.7f, _curvature = 0.95f, _highPass = 1f, _scale = 1f;

    private static void WriteRest(EcsWorld ecs)
    {
        if (!Bloom.Bloom) return;
        var bloom = ecs.Wrap<BloomRef>(_camera);
        (bloom.LowFrequencyBoost, bloom.LowFrequencyBoostCurvature, bloom.HighPassFrequency) = (_boost, _curvature, _highPass);
        bloom.Scale = bloom.Scale with { X = _scale };
    }

    // A value moved down or up by a step and kept between zero and the highest it may be.
    private static float Nudge(float value, bool down, bool up, float step, float highest = 1f) =>
        Math.Clamp(value + (up ? step : 0f) - (down ? step : 0f), 0f, highest);

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _camera = ecs.Camera(
                Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY),
                new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f) });
            Render.SetPostProcessing(_camera, Bloom);
            (_boost, _curvature, _highPass, _scale) = (0.7f, 0.95f, 1f, 1f);
            WriteRest(ctx.Ecs);

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

            _boost = Nudge(_boost, input.KeyDown(Key.S), input.KeyDown(Key.W), step);
            _curvature = Nudge(_curvature, input.KeyDown(Key.D), input.KeyDown(Key.E), step);
            _highPass = Nudge(_highPass, input.KeyDown(Key.F), input.KeyDown(Key.R), step);
            _scale = Nudge(_scale, input.KeyDown(Key.K), input.KeyDown(Key.I), step, 16f);

            if (input.AnyKeyDown() || input.KeyPressed(Key.Space))
            {
                Render.SetPostProcessing(_camera, Bloom);
                WriteRest(ctx.Ecs);
            }

            Ui.SetText(_text, Bloom.Bloom
                ? "Bloom (Toggle: Space)\n"
                  + FormattableString.Invariant($"(Q/A) Intensity: {Bloom.BloomIntensity:0.00}\n")
                  + FormattableString.Invariant($"(W/S) Low-frequency boost: {_boost:0.00}\n")
                  + FormattableString.Invariant($"(E/D) Low-frequency boost curvature: {_curvature:0.00}\n")
                  + FormattableString.Invariant($"(R/F) High-pass frequency: {_highPass:0.00}\n")
                  + $"(T/G) Mode: {(Bloom.BloomMode == BloomMode.Additive ? "Additive" : "Energy-conserving")}\n"
                  + FormattableString.Invariant($"(Y/H) Threshold: {Bloom.BloomThreshold:0.00}\n")
                  + FormattableString.Invariant($"(U/J) Threshold softness: {Bloom.BloomThresholdSoftness:0.00}\n")
                  + FormattableString.Invariant($"(I/K) Horizontal Scale: {_scale:0.00}\n")
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
