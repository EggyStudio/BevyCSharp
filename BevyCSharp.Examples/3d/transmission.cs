// Bevy's transmission example, examples/3d/transmission.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using System.Globalization;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

using Quality = Bevy.Reflected.ScreenSpaceTransmissionRef.QualityVariant;

// Showcases light transmission in the physically based material: glass spheres that bend what is
// behind them, a candle whose wax lets its flame's light through, and a sheet of paper lit from
// behind, with keys for each material property and for the screen-space pass that draws them.
internal static class Transmission
{

    // A material the keys change, and which of its properties they change.
    private static Entity _camera;
    private static float _diffuse, _specular, _thickness, _ior, _roughness, _reflectance;
    private static bool _autoCamera, _hdr, _depthPrepass, _taa;
    private static ulong _steps;
    private static ScreenSpaceTransmissionRef.QualityVariant _quality;
    private static Random _random = new();

    public static void Build(App app)
    {
        (_diffuse, _specular, _thickness, _ior, _roughness, _reflectance) = (0.5f, 0.9f, 1.8f, 1.5f, 0.12f, 0.5f);
        (_autoCamera, _hdr, _depthPrepass, _taa) = (true, true, true, true);
        _random = new Random(19878367);

        app.Startup(Setup, "transmission.Setup");
        app.Update(Control, "transmission.ExampleControlSystem");
        app.Update(FlickerSystem, "transmission.FlickerSystem");
    }

    private static Entity Spawn(EcsWorld ecs, AssetHandle mesh, MaterialSettings settings, Transform at, bool color = true, bool specular = false, bool diffuse = false)
    {
        var entity = ecs.SpawnMesh(mesh, Render.CreateMaterial(settings), at);
        ecs.Add(entity, new TransmissionControls { DiffuseTransmission = diffuse, SpecularTransmission = specular, Color = color });
        return entity;
    }

    // Bevy's EulerRot::XYZ.
    private static Quat Euler(float x, float y, float z) => Quat.FromRotationX(x) * Quat.FromRotationY(y) * Quat.FromRotationZ(z);

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render.SetClearColor((0f, 0f, 0f, 1f));
        Render.SetAmbientLight((1f, 1f, 1f), 0f);
        Render.SetShadowMapSize(point: 2048);

        var sphere = Render.CreateMesh(MeshShape.Sphere, 0.9f);
        var cube = Render.CreateMesh(MeshShape.Cuboid, 0.7f, 0.7f, 0.7f);
        var plane = Render.CreateMesh(MeshShape.Plane, 2f, 2f);
        var cylinder = Render.CreateMesh(MeshShape.Cylinder, 0.5f, 2f);

        // Two opaque cubes behind the glass, to be seen bent through it.
        Spawn(ecs, cube, new MaterialSettings(), new Transform(new Vec3(0.25f, 0.5f, -2f), Euler(1.4f, 3.7f, 21.3f), Vec3.One));
        Spawn(ecs, cube, new MaterialSettings(), new Transform(new Vec3(-0.75f, 0.7f, -2f), Euler(0.4f, 2.3f, 4.7f), Vec3.One));

        // The candle's wax, which passes its flame's light diffusely.
        Spawn(ecs, cylinder, new MaterialSettings { BaseColor = Color.FromSrgb(0.9f, 0.2f, 0.3f), DiffuseTransmission = 0.7f, Roughness = 0.32f, Thickness = 0.2f },
            Transform.At(-1f, 0f, 0f), diffuse: true);

        // The flame, white hot with an orange edge, which casts no shadow of its own.
        var (white, orange) = (Color.FromSrgb(0.98f, 0.92f, 0.84f), Color.FromSrgb(1f, 0.27f, 0f));
        var emissive = (white.R * 20f + orange.R * 4f, white.G * 20f + orange.G * 4f, white.B * 20f + orange.B * 4f, 1f);
        var flame = ecs.SpawnMesh(sphere, Render.CreateMaterial(new MaterialSettings { Emissive = emissive, DiffuseTransmission = 1f }),
            new Transform(new Vec3(-1f, 1.15f, 0f), Quat.Identity, new Vec3(0.1f, 0.2f, 0.1f)));
        Render.SetMeshFlags(ecs, flame, MeshFlags.NoShadowCasting);
        ecs.Add(flame, new Flicker());

        // Glass spheres, a large clear one and three small colored ones.
        foreach (var (color, at, scale) in new[]
        {
            ((1f, 1f, 1f, 1f), new Vec3(1f, 0f, 0f), 1f),
            (Color.FromSrgb(1f, 0f, 0f), new Vec3(1f, -0.5f, 2f), 0.5f),
            (Color.FromSrgb(0f, 1f, 0f), new Vec3(0f, -0.5f, 2f), 0.5f),
            (Color.FromSrgb(0f, 0f, 1f), new Vec3(-1f, -0.5f, 2f), 0.5f),
        })
        {
            Spawn(ecs, sphere, new MaterialSettings
            {
                BaseColor = color,
                Transmission = 0.9f,
                DiffuseTransmission = 1f,
                Thickness = 1.8f,
                RefractiveIndex = 1.5f,
                Roughness = 0.12f,
            }, new Transform(at, Quat.Identity, new Vec3(scale)), specular: true);
        }

        // A checkered floor.
        var black = new MaterialSettings { BaseColor = (0f, 0f, 0f, 1f), Reflectance = 0.3f, Roughness = 0.8f };
        var whiteFloor = new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Reflectance = 0.3f, Roughness = 0.8f };
        var (blackMaterial, whiteMaterial) = (Render.CreateMaterial(black), Render.CreateMaterial(whiteFloor));
        for (var x = -3; x < 4; x++)
        {
            for (var z = -3; z < 4; z++)
            {
                var tile = ecs.SpawnMesh(plane, (x + z) % 2 == 0 ? blackMaterial : whiteMaterial, Transform.At(x * 2f, -1f, z * 2f));
                ecs.Add(tile, new TransmissionControls { Color = true });
            }
        }

        // A sheet of paper standing behind the candle, which its shadow shows through.
        var paper = Spawn(ecs, plane,
            new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), DiffuseTransmission = 0.6f, Roughness = 0.8f, Reflectance = 1f, DoubleSided = true },
            new Transform(new Vec3(0f, 0.5f, -3f), Euler(MathF.PI / 2f, 0f, 0f), new Vec3(2f, 1f, 1f)), color: false, diffuse: true);
        ecs.Insert<TransmittedShadowReceiverRef>(paper);

        var mix = (white.R + (orange.R - white.R) * 0.2f, white.G + (orange.G - white.G) * 0.2f, white.B + (orange.B - white.B) * 0.2f);
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Color = mix, Intensity = 4000f, Radius = 0.2f, Range = 5f, Shadows = true });
        ecs.Add(light, Transform.At(-1f, 1.7f, 0f));
        ecs.Add(light, new Flicker());

        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(1f, 1.8f, 7f), Vec3.Zero, Vec3.UnitY));
        ApplyPost();
        // Bevy's Exposure { ev100: 6.0 }, as a lens: f/1 open for a 64th of a second at ISO 100.
        Render.SetLensExposure(_camera, aperture: 1f, shutter: 1f / 64f, sensitivity: 100f);
        ecs.Insert<ColorGradingRef>(_camera).GlobalPostSaturation = 1.2f;
        Render.SetEnvironmentMap(
            _camera,
            AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"),
            AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"),
            25f);

        var transmission = ecs.Get<ScreenSpaceTransmissionRef>(_camera) ?? ecs.Insert<ScreenSpaceTransmissionRef>(_camera);
        (_steps, _quality) = (transmission.Steps, transmission.Quality);

        ecs.Add(Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) }), new TransmissionDisplay());
    }

    private static void ApplyPost() => Render.SetPostProcessing(_camera, new PostSettings
    {
        Hdr = _hdr,
        Bloom = _hdr,
        Msaa = 1,
        AntiAlias = _taa ? AntiAliasPass.Temporal : AntiAliasPass.None,
        Tonemapper = Tonemapper.TonyMcMapface,
    });

    private static void Control(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var ecs = ctx.Ecs;
        var delta = ctx.Time.Delta;

        static void Step(Input input, Key down, Key up, ref float value, float delta, float lowest, float highest)
        {
            if (input.KeyDown(up)) value = MathF.Min(value + delta, highest);
            else if (input.KeyDown(down)) value = MathF.Max(value - delta, lowest);
        }

        Step(input, Key.Digit1, Key.Digit2, ref _diffuse, delta, 0f, 1f);
        Step(input, Key.Q, Key.W, ref _specular, delta, 0f, 1f);
        Step(input, Key.A, Key.S, ref _thickness, delta, 0f, 5f);
        Step(input, Key.Z, Key.X, ref _ior, delta, 1f, 3f);
        Step(input, Key.U, Key.I, ref _reflectance, delta, 0f, 1f);
        Step(input, Key.E, Key.R, ref _roughness, delta, 0f, 1f);

        // Bevy writes every controlled mesh's material each frame, which here is each whose values
        // moved, reached through the mesh as Bevy reaches it.
        var randomize = input.KeyPressed(Key.C);
        foreach (var entity in ecs.EntitiesWith<TransmissionControls>())
        {
            var controls = ecs.GetOrDefault<TransmissionControls>(entity);
            var (color, specular, diffuse) = (controls.Color, controls.SpecularTransmission, controls.DiffuseTransmission);
            var material = Render.MaterialOf(ecs, entity);
            if (!Render.TryReadMaterial(material, out var settings) || settings is null) continue;

            var before = (settings.Transmission, settings.Thickness, settings.RefractiveIndex, settings.Roughness, settings.Reflectance, settings.DiffuseTransmission, settings.BaseColor);
            if (specular)
            {
                (settings.Transmission, settings.Thickness, settings.RefractiveIndex) = (_specular, _thickness, _ior);
                (settings.Roughness, settings.Reflectance) = (_roughness, _reflectance);
            }
            if (diffuse) settings.DiffuseTransmission = _diffuse;
            if (color && randomize)
            {
                var random = Color.FromSrgb(_random.NextSingle(), _random.NextSingle(), _random.NextSingle());
                settings.BaseColor = (random.R, random.G, random.B, settings.BaseColor.A);
            }

            if (before != (settings.Transmission, settings.Thickness, settings.RefractiveIndex, settings.Roughness, settings.Reflectance, settings.DiffuseTransmission, settings.BaseColor))
                Render.WriteMaterial(material, settings);
        }

        if (input.KeyPressed(Key.H)) { _hdr = !_hdr; ApplyPost(); }
        if (input.KeyPressed(Key.D))
        {
            _depthPrepass = !_depthPrepass;
            if (_depthPrepass) ecs.Insert<DepthPrepassRef>(_camera);
            else ecs.Wrap<DepthPrepassRef>(_camera).Remove();
        }
        if (input.KeyPressed(Key.T)) { _taa = !_taa; ApplyPost(); }

        if (input.KeyPressed(Key.O) && _steps > 0) SetSteps(ecs, _steps - 1);
        if (input.KeyPressed(Key.P) && _steps < 4) SetSteps(ecs, _steps + 1);
        foreach (var (key, quality) in new[] { (Key.J, Quality.Low), (Key.K, Quality.Medium), (Key.L, Quality.High), (Key.Semicolon, Quality.Ultra) })
        {
            if (!input.KeyPressed(key)) continue;
            _quality = quality;
            ecs.Wrap<ScreenSpaceTransmissionRef>(_camera).Quality = quality;
        }

        // The camera turns slowly by itself until an arrow key takes it.
        var rotation = 0f;
        if (input.KeyDown(Key.ArrowRight)) (rotation, _autoCamera) = (delta, false);
        else if (input.KeyDown(Key.ArrowLeft)) (rotation, _autoCamera) = (-delta, false);
        else if (_autoCamera) rotation = delta * 0.25f;

        var camera = ecs.GetOrDefault<Transform>(_camera);
        var length = camera.Translation.Length;
        var distance = input.KeyDown(Key.ArrowDown) && length < 25f ? delta : input.KeyDown(Key.ArrowUp) && length > 2f ? -delta : 0f;
        var turn = Quat.FromRotationY(rotation);
        ecs.Set(_camera, new Transform(turn * (camera.Translation * MathF.Exp(distance)), turn * camera.Rotation, camera.Scale));

        var text = string.Create(CultureInfo.InvariantCulture, $"""
             J / K / L / ;  Screen Space Specular Transmissive Quality: {_quality}
                     O / P  Screen Space Specular Transmissive Steps: {_steps}
                     1 / 2  Diffuse Transmission: {_diffuse:0.00}
                     Q / W  Specular Transmission: {_specular:0.00}
                     A / S  Thickness: {_thickness:0.00}
                     Z / X  IOR: {_ior:0.00}
                     E / R  Perceptual Roughness: {_roughness:0.00}
                     U / I  Reflectance: {_reflectance:0.00}
                Arrow Keys  Control Camera
                         C  Randomize Colors
                         H  HDR + Bloom: {(_hdr ? "ON " : "OFF")}
                         D  Depth Prepass: {(_depthPrepass ? "ON " : "OFF")}
                         T  TAA: {(_taa ? (_depthPrepass ? "ON " : "N/A (Needs Depth Prepass)") : "OFF")}

            """);
        foreach (var display in ecs.EntitiesWith<TransmissionDisplay>()) Ui.SetText(display, text);
    }

    private static void SetSteps(EcsWorld ecs, ulong steps)
    {
        _steps = steps;
        ecs.Wrap<ScreenSpaceTransmissionRef>(_camera).Steps = steps;
    }

    // The flame and its light waver together, the flame the one of the two with a mesh, as Bevy's
    // query tells them apart.
    private static void FlickerSystem(BehaviorContext ctx)
    {
        var s = ctx.Time.Elapsed;
        var a = MathF.Cos(s * 6f) * 0.0125f + MathF.Cos(s * 4f) * 0.025f;
        var b = MathF.Cos(s * 5f) * 0.0125f + MathF.Cos(s * 3f) * 0.025f;
        var c = MathF.Cos(s * 7f) * 0.0125f + MathF.Cos(s * 2f) * 0.025f;

        var ecs = ctx.Ecs;
        foreach (var entity in ecs.EntitiesWith<Flicker>())
        {
            if (ecs.Get<Mesh3dRef>(entity) is not null)
            {
                var look = Transform.LookingAt(new Vec3(-1f, 1.23f, 0f), new Vec3(-1f - c, 1.7f - b, -a), Vec3.UnitX);
                ecs.Set(entity, new Transform(new Vec3(-1f - c, 1.23f, -a), Quat.FromRotationZ(MathF.PI / 2f) * look.Rotation, new Vec3(0.1f, 0.2f, 0.1f)));
            }
            else
            {
                ecs.Wrap<PointLightRef>(entity).Intensity = 4000f + 3000f * (a + b + c);
                ecs.Set(entity, Transform.At(-1f - c, 1.7f, -a));
            }
        }
    }
}

/// <summary>A mesh whose material the keys change, and which of their changes it takes.</summary>
[Behavior]
public partial struct TransmissionControls
{
    /// <summary>Whether 1 and 2 change its diffuse transmission.</summary>
    public bool DiffuseTransmission;

    /// <summary>Whether its specular transmission, thickness, refraction, roughness and reflectance follow the keys.</summary>
    public bool SpecularTransmission;

    /// <summary>Whether C gives it a random color.</summary>
    public bool Color;
}

/// <summary>The flame and its light, which waver together.</summary>
[Behavior]
public partial struct Flicker;

/// <summary>
/// The text saying the settings, Bevy's <c>ExampleDisplay</c> under another name since
/// auto_exposure's shares the namespace.
/// </summary>
[Behavior]
public partial struct TransmissionDisplay;
