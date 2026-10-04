using System.Globalization;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates Bevy's built-in postprocessing features: chromatic aberration, a vignette and lens
// distortion, each changed with the arrow keys. The fog is Bevy's DistanceFog, put on through
// reflection.
internal static class PostProcessing
{
    private const float AdjustmentSpeed = 0.005f;
    private const float MaxChromaticAberration = 0.4f;

    private static readonly string[] Names =
    [
        "Chromatic aberration intensity", "Vignette intensity", "Vignette radius", "Vignette smoothness",
        "Vignette roundness", "Vignette edge_compensation", "Lens Distortion intensity",
        "Lens Distortion multiplier x", "Lens Distortion multiplier y",
    ];

    private static Entity _camera, _text;
    private static int _selected;
    private static float[] _values = [];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _selected = 0;

            // Each effect at Bevy's own default.
            _values = [0.02f, 1f, 0.75f, 5f, 1f, 1f, 0.5f, 1f, 1f];

            _camera = ecs.Camera(Transform.LookingAt(new Vec3(0.7f, 0.7f, 1f), new Vec3(0f, 0.3f, 0f), Vec3.UnitY));
            Render.SetPostProcessing(_camera, new PostSettings { Hdr = true });
            Render.SetEnvironmentMap(
                _camera,
                AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"),
                AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"),
                2000f);

            var fog = ecs.Insert<DistanceFogRef>(_camera);
            fog.Color = Color.FromSrgb(43 / 255f, 44 / 255f, 47 / 255f);
            fog.Falloff = new FogFalloff.Linear(1f, 8f);
            Apply();

            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 15_000f, Shadows = true });
            ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationY(MathF.PI * -0.15f) * Quat.FromRotationX(MathF.PI * -0.15f), Vec3.One));
            Render.SetShadowCascades(sun, maximum: 3f, firstBound: 0.9f);

            _text = Ui.SpawnText(Text(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        });

        app.SpawnGltf("models/TonemappingTest/TonemappingTest.gltf");
        app.SpawnGltf("models/FlightHelmet/FlightHelmet.gltf", (ctx, root) =>
            ctx.Ecs.Set(root, new Transform(new Vec3(0.5f, 0f, -0.5f), Quat.FromRotationY(-0.15f * MathF.PI), Vec3.One)));

        app.Update(ctx =>
        {
            var input = ctx.Input;
            var changed = false;
            if (input.KeyPressed(Key.ArrowUp) && _selected > 0) { _selected--; changed = true; }
            else if (input.KeyPressed(Key.ArrowDown) && _selected < Names.Length - 1) { _selected++; changed = true; }

            var delta = input.KeyDown(Key.ArrowLeft) ? -AdjustmentSpeed : input.KeyDown(Key.ArrowRight) ? AdjustmentSpeed : 0f;
            if (delta != 0f)
            {
                var value = _values[_selected] + delta;
                _values[_selected] = _selected switch
                {
                    0 => Math.Clamp(value, 0f, MaxChromaticAberration),
                    1 or 5 or 7 or 8 => Math.Clamp(value, 0f, 1f),
                    2 => Math.Clamp(value, 0f, 2f),
                    3 or 4 => MathF.Max(value, 0.01f),
                    _ => Math.Clamp(value, -1f, 1f),
                };
                Apply();
                changed = true;
            }

            if (changed) Ui.SetText(_text, Text());
        }, "post_processing.HandleKeyboardInput");
    }

    private static void Apply() => Render.SetEffects(_camera, new EffectSettings
    {
        Aberration = _values[0],
        Vignette = _values[1],
        VignetteRadius = _values[2],
        VignetteSmoothness = _values[3],
        VignetteRoundness = _values[4],
        VignetteEdgeCompensation = _values[5],
        Distortion = _values[6],
        DistortionAxes = (_values[7], _values[8]),
    });

    private static string Text() =>
        string.Concat(Names.Select((name, i) => (i == _selected ? "> " : "") + string.Create(CultureInfo.InvariantCulture, $"{name}: {_values[i]:0.00}\n")))
        + "\n(Press Up or Down to select)\n(Press Left or Right to change)";
}
