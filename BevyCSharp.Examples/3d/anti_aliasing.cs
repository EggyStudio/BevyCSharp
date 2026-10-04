using System.Text;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

using Sensitivity = Bevy.Reflected.FxaaRef.EdgeThresholdVariant;
using SensitivityMin = Bevy.Reflected.FxaaRef.EdgeThresholdMinVariant;
using SmaaPreset = Bevy.Reflected.SmaaRef.PresetVariant;

// Compares the antialiasing Bevy has: none, MSAA at two, four or eight samples, FXAA at five
// sensitivities, SMAA at four qualities and TAA, with contrast adaptive sharpening and an
// orthographic projection to try each against. Bevy's DLSS is behind a feature its examples are
// not built with by default, and is left out here as there.
internal static class AntiAliasing
{
    private const string Projection = "bevy_camera::projection::Projection";

    private enum Method { None, Msaa, Fxaa, Smaa, Taa }

    private static Entity _camera, _text;
    private static Method _method;
    private static int _samples;
    private static Sensitivity _sensitivity;
    private static SmaaPreset _preset;
    private static bool _orthographic;

    // Contrast adaptive sharpening as the keys set it, since the post-processing call takes the
    // component off when it is given no sharpening, and it is put back after each.
    private static (bool Enabled, float Strength, bool Denoise) _sharpening;

    public static void Build(App app)
    {
        (_method, _samples, _sensitivity, _preset, _orthographic) = (Method.Msaa, 4, Sensitivity.High, SmaaPreset.High, false);
        _sharpening = (false, 0.6f, false);

        app.Startup(Setup, "anti_aliasing.Setup");
        app.SpawnGltf("models/FlightHelmet/FlightHelmet.gltf");
        app.Update(ModifyAa, "anti_aliasing.ModifyAa");
        app.Update(ModifySharpening, "anti_aliasing.ModifySharpening");
        app.Update(ModifyProjection, "anti_aliasing.ModifyProjection");
        app.Update(_ => Ui.SetText(_text, Describe()), "anti_aliasing.UpdateUi");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 20f, 20f), Scene.Material(Scene.Srgb(0.1f, 0.2f, 0.1f)), Transform.Identity);

        var cube = Render.CreateMesh(MeshShape.Cuboid, 0.25f, 0.25f, 0.25f);
        var checker = Render.CreateMaterial(new MaterialSettings { BaseColorTexture = UvDebugTexture() });
        for (var i = 0; i < 5; i++)
            ecs.Mesh(cube, checker, Transform.At(i * 0.25f - 1f, 0.125f, -i * 0.5f));

        // Bevy's FULL_DAYLIGHT, turned by its EulerRot::ZYX, with its cascades kept close.
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 20_000f, Shadows = true });
        ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationY(MathF.PI * -0.15f) * Quat.FromRotationX(MathF.PI * -0.15f), Vec3.One));
        Render.SetShadowCascades(sun, maximum: 3f, firstBound: 0.9f);

        _camera = ecs.Camera(Transform.LookingAt(new Vec3(0.7f, 0.7f, 1f), new Vec3(0f, 0.3f, 0f), Vec3.UnitY));
        Apply(ecs);
        Render.SetEnvironmentMap(
            _camera,
            AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"),
            AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"),
            150f);
        var fogColor = Scene.Srgb8(43, 44, 47);
        var fog = ecs.Insert<DistanceFogRef>(_camera);
        fog.Color = new Color(fogColor.R, fogColor.G, fogColor.B, fogColor.A);
        fog.Falloff = new FogFalloff.Linear(1f, 4f);

        _text = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    // Puts the chosen method on the camera, with its sensitivity or quality written after, since
    // the post-processing call makes the pass again at its default.
    private static void Apply(EcsWorld ecs)
    {
        Render.SetPostProcessing(_camera, new PostSettings
        {
            Hdr = true,
            Msaa = _method == Method.Msaa ? _samples : 1,
            AntiAlias = _method switch
            {
                Method.Fxaa => AntiAliasPass.Fxaa,
                Method.Smaa => AntiAliasPass.Smaa,
                Method.Taa => AntiAliasPass.Temporal,
                _ => AntiAliasPass.None,
            },
        });

        if (_method == Method.Fxaa)
        {
            var fxaa = ecs.Wrap<FxaaRef>(_camera);
            (fxaa.EdgeThreshold, fxaa.EdgeThresholdMin) = (_sensitivity, Enum.Parse<SensitivityMin>(_sensitivity.ToString()));
        }
        if (_method == Method.Smaa) ecs.Wrap<SmaaRef>(_camera).Preset = _preset;
        Sharpen(ecs);
    }

    private static void Sharpen(EcsWorld ecs)
    {
        var cas = ecs.Get<ContrastAdaptiveSharpeningRef>(_camera) ?? ecs.Insert<ContrastAdaptiveSharpeningRef>(_camera);
        (cas.Enabled, cas.SharpeningStrength, cas.Denoise) = _sharpening;
    }

    private static void ModifyAa(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var before = (_method, _samples, _sensitivity, _preset);

        if (input.KeyPressed(Key.Digit1)) _method = Method.None;
        if (input.KeyPressed(Key.Digit2) && _method != Method.Msaa) (_method, _samples) = (Method.Msaa, 4);
        if (input.KeyPressed(Key.Digit3)) _method = Method.Fxaa;
        if (input.KeyPressed(Key.Digit4)) _method = Method.Smaa;
        if (input.KeyPressed(Key.Digit5)) _method = Method.Taa;

        switch (_method)
        {
            case Method.Msaa:
                if (input.KeyPressed(Key.Q)) _samples = 2;
                if (input.KeyPressed(Key.W)) _samples = 4;
                if (input.KeyPressed(Key.E)) _samples = 8;
                break;
            case Method.Fxaa:
                if (input.KeyPressed(Key.Q)) _sensitivity = Sensitivity.Low;
                if (input.KeyPressed(Key.W)) _sensitivity = Sensitivity.Medium;
                if (input.KeyPressed(Key.E)) _sensitivity = Sensitivity.High;
                if (input.KeyPressed(Key.R)) _sensitivity = Sensitivity.Ultra;
                if (input.KeyPressed(Key.T)) _sensitivity = Sensitivity.Extreme;
                break;
            case Method.Smaa:
                if (input.KeyPressed(Key.Q)) _preset = SmaaPreset.Low;
                if (input.KeyPressed(Key.W)) _preset = SmaaPreset.Medium;
                if (input.KeyPressed(Key.E)) _preset = SmaaPreset.High;
                if (input.KeyPressed(Key.R)) _preset = SmaaPreset.Ultra;
                break;
        }

        if ((_method, _samples, _sensitivity, _preset) != before) Apply(ctx.Ecs);
    }

    private static void ModifySharpening(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var before = _sharpening;
        if (input.KeyPressed(Key.Digit0)) _sharpening.Enabled = !_sharpening.Enabled;
        if (_sharpening.Enabled)
        {
            if (input.KeyPressed(Key.Minus)) _sharpening.Strength = Math.Clamp(_sharpening.Strength - 0.1f, 0f, 1f);
            if (input.KeyPressed(Key.Equal)) _sharpening.Strength = Math.Clamp(_sharpening.Strength + 0.1f, 0f, 1f);
            if (input.KeyPressed(Key.D)) _sharpening.Denoise = !_sharpening.Denoise;
        }
        if (_sharpening != before) Sharpen(ctx.Ecs);
    }

    // An orthographic projection holds its scaling mode, an enum inside a variant, which a wrapper
    // does not type, so it is written as JSON, Bevy's default_3d at a scale of 0.002.
    private static void ModifyProjection(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.O)) return;
        _orthographic = !_orthographic;
        if (_orthographic)
        {
            ctx.Ecs.SetReflected(_camera, Projection, string.Empty,
                """{"Orthographic":{"near":0.0,"far":1000.0,"viewport_origin":[0.5,0.5],"scaling_mode":"WindowSize","scale":0.002,"area":{"min":[-1.0,-1.0],"max":[1.0,1.0]}}}""");
        }
        else
        {
            Render.SetPerspective(_camera, 45f, 0.1f, 1000f);
        }
    }

    private static string Describe()
    {
        var text = new StringBuilder("Antialias Method\n");
        void Item(string label, char shortcut, bool chosen)
        {
            var star = chosen ? "*" : "";
            text.Append($"({shortcut}) {star}{label}{star}\n");
        }

        Item("No AA", '1', _method == Method.None);
        Item("MSAA", '2', _method == Method.Msaa);
        Item("FXAA", '3', _method == Method.Fxaa);
        Item("SMAA", '4', _method == Method.Smaa);
        Item("TAA", '5', _method == Method.Taa);

        if (_method == Method.Msaa)
        {
            text.Append("\n----------\n\nSample Count\n");
            Item("2", 'Q', _samples == 2);
            Item("4", 'W', _samples == 4);
            Item("8", 'E', _samples == 8);
        }
        if (_method == Method.Fxaa)
        {
            text.Append("\n----------\n\nSensitivity\n");
            Item("Low", 'Q', _sensitivity == Sensitivity.Low);
            Item("Medium", 'W', _sensitivity == Sensitivity.Medium);
            Item("High", 'E', _sensitivity == Sensitivity.High);
            Item("Ultra", 'R', _sensitivity == Sensitivity.Ultra);
            Item("Extreme", 'T', _sensitivity == Sensitivity.Extreme);
        }
        if (_method == Method.Smaa)
        {
            text.Append("\n----------\n\nQuality\n");
            Item("Low", 'Q', _preset == SmaaPreset.Low);
            Item("Medium", 'W', _preset == SmaaPreset.Medium);
            Item("High", 'E', _preset == SmaaPreset.High);
            Item("Ultra", 'R', _preset == SmaaPreset.Ultra);
        }

        text.Append("\n----------\n\n");
        Item("Sharpening", '0', _sharpening.Enabled);
        if (_sharpening.Enabled)
        {
            text.Append(FormattableString.Invariant($"(-/+) Strength: {_sharpening.Strength:0.0}\n"));
            Item("Denoising", 'D', _sharpening.Denoise);
        }

        text.Append("\n----------\n\n");
        Item("Orthographic", 'O', _orthographic);
        return text.ToString();
    }

    // Bevy's uv_debug_texture, eight rows of a rainbow palette each shifted one along.
    private static AssetHandle UvDebugTexture()
    {
        const int Size = 8;
        byte[] palette = [255, 102, 159, 255, 255, 159, 102, 255, 236, 255, 102, 255, 121, 255, 102, 255, 102, 255, 198, 255, 102, 198, 255, 255, 121, 102, 255, 255, 236, 102, 255, 255];
        var pixels = new byte[Size * Size * 4];
        for (var y = 0; y < Size; y++)
        {
            var offset = y * 4;
            for (var i = 0; i < palette.Length; i++)
                pixels[y * Size * 4 + i] = palette[(i + palette.Length - offset) % palette.Length];
        }
        return Render.CreateImage(pixels, Size, Size);
    }
}
