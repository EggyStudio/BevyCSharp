// Bevy's bloom_2d example, examples/2d/bloom_2d.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using System.Text;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.TwoD;

using Tonemap = Bevy.Reflected.TonemappingRef.ValueVariant;
using CompositeMode = Bevy.Reflected.BloomRef.CompositeModeVariant;

// Illustrates bloom post-processing in 2D, on a sprite and two meshes far brighter than white
// against black, with Bevy's Bloom component changed field by field through its wrapper.
internal static class Bloom2d
{
    // The tonemappers in the order O steps through them, as Bevy's next_tonemap does.
    private static readonly Tonemap[] Tonemappers =
    [
        Tonemap.None, Tonemap.AcesFitted, Tonemap.AgX, Tonemap.BlenderFilmic, Tonemap.Reinhard,
        Tonemap.ReinhardLuminance, Tonemap.SomewhatBoringDisplayTransform, Tonemap.TonyMcMapface,
        Tonemap.KhronosPbrNeutral,
    ];

    private static Entity _camera, _text;

    public static void Build(App app)
    {
        app.Startup(Setup, "bloom_2d.Setup");
        app.Update(UpdateBloomSettings, "bloom_2d.UpdateBloomSettings");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // A tonemapper that turns what is too bright toward white, bloom, and dithering against the
        // banding bloom's gradients leave.
        _camera = Render2d.SpawnCamera2d();
        ecs.Wrap<CameraRef>(_camera).ClearColor = new ClearColorConfig.Custom(new Color(0f, 0f, 0f, 1f));
        ecs.Wrap<TonemappingRef>(_camera).Value = Tonemap.TonyMcMapface;
        ecs.Insert<BloomRef>(_camera);
        ecs.Insert<DebandDitherRef>(_camera).Value = DebandDitherRef.ValueVariant.Enabled;

        // Things bright enough to bloom, in a dark place to see it.
        var bird = ecs.Spawn();
        ecs.Add(bird, Transform.Identity);
        Render2d.SetSprite(ecs, bird, AssetServer.Load(AssetKind.Image, "branding/bevy_bird_dark.png"),
            new SpriteSettings { Color = Color.FromSrgb(5f, 5f, 5f), Size = (160f, 160f) });

        foreach (var (mesh, color, x) in new[]
        {
            (Render.CreateMesh(MeshShape.Circle, 100f), Color.FromSrgb(7.5f, 0f, 7.5f), -200f),
            (Render.CreateMesh(MeshShape.RegularPolygon, 100f, 6f), Color.FromSrgb(6.25f, 9.4f, 9.1f), 200f),
        })
        {
            var entity = ecs.Spawn();
            ecs.Add(entity, Transform.At(x, 0f, 0f));
            Render2d.SetMesh(ecs, entity, mesh);
            Render2d.SetMaterial(ecs, entity, Render2d.CreateMaterial(new ColorMaterialSettings { Color = color }));
        }

        _text = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    private static void UpdateBloomSettings(BehaviorContext ctx)
    {
        var (ecs, input) = (ctx.Ecs, ctx.Input);
        var text = new StringBuilder();
        var tonemapping = ecs.Wrap<TonemappingRef>(_camera);

        if (ecs.Get<BloomRef>(_camera) is { } bloom)
        {
            text.Append("Bloom (Toggle: Space)\n")
                .Append(FormattableString.Invariant($"(Q/A) Intensity: {bloom.Intensity:0.00}\n"))
                .Append(FormattableString.Invariant($"(W/S) Low-frequency boost: {bloom.LowFrequencyBoost:0.00}\n"))
                .Append(FormattableString.Invariant($"(E/D) Low-frequency boost curvature: {bloom.LowFrequencyBoostCurvature:0.00}\n"))
                .Append(FormattableString.Invariant($"(R/F) High-pass frequency: {bloom.HighPassFrequency:0.00}\n"))
                .Append($"(T/G) Mode: {(bloom.CompositeMode == CompositeMode.Additive ? "Additive" : "Energy-conserving")}\n")
                .Append(FormattableString.Invariant($"(Y/H) Threshold: {bloom.PrefilterThreshold:0.00}\n"))
                .Append(FormattableString.Invariant($"(U/J) Threshold softness: {bloom.PrefilterThresholdSoftness:0.00}\n"))
                .Append(FormattableString.Invariant($"(I/K) Horizontal Scale: {bloom.Scale.X:0.00}\n"));

            if (input.KeyPressed(Key.Space))
            {
                bloom.Remove();
            }
            else
            {
                var step = ctx.Time.Delta / 10f;
                float Nudge(float value, Key down, Key up, float by, float highest) =>
                    Math.Clamp(value + (input.KeyDown(up) ? by : 0f) - (input.KeyDown(down) ? by : 0f), 0f, highest);

                // Each field is written only while its keys are held, since a write goes through
                // reflection and most frames change nothing.
                if (input.KeyDown(Key.A) || input.KeyDown(Key.Q)) bloom.Intensity = Nudge(bloom.Intensity, Key.A, Key.Q, step, 1f);
                if (input.KeyDown(Key.S) || input.KeyDown(Key.W)) bloom.LowFrequencyBoost = Nudge(bloom.LowFrequencyBoost, Key.S, Key.W, step, 1f);
                if (input.KeyDown(Key.D) || input.KeyDown(Key.E)) bloom.LowFrequencyBoostCurvature = Nudge(bloom.LowFrequencyBoostCurvature, Key.D, Key.E, step, 1f);
                if (input.KeyDown(Key.F) || input.KeyDown(Key.R)) bloom.HighPassFrequency = Nudge(bloom.HighPassFrequency, Key.F, Key.R, step, 1f);
                if (input.KeyDown(Key.G)) bloom.CompositeMode = CompositeMode.Additive;
                if (input.KeyDown(Key.T)) bloom.CompositeMode = CompositeMode.EnergyConserving;
                if (input.KeyDown(Key.H) || input.KeyDown(Key.Y)) bloom.PrefilterThreshold = Nudge(bloom.PrefilterThreshold, Key.H, Key.Y, ctx.Time.Delta, float.MaxValue);
                if (input.KeyDown(Key.J) || input.KeyDown(Key.U)) bloom.PrefilterThresholdSoftness = Nudge(bloom.PrefilterThresholdSoftness, Key.J, Key.U, step, 1f);
                if (input.KeyDown(Key.K) || input.KeyDown(Key.I)) bloom.Scale = bloom.Scale with { X = Nudge(bloom.Scale.X, Key.K, Key.I, ctx.Time.Delta * 2f, 16f) };
            }
        }
        else
        {
            text.Append("Bloom: Off (Toggle: Space)\n");
            if (input.KeyPressed(Key.Space)) ecs.Insert<BloomRef>(_camera);
        }

        text.Append($"(O) Tonemapping: {tonemapping.Value}\n");
        if (input.KeyPressed(Key.O))
            tonemapping.Value = Tonemappers[(Array.IndexOf(Tonemappers, tonemapping.Value) + 1) % Tonemappers.Length];

        Ui.SetText(_text, text.ToString());
    }
}
