// Bevy's reflection_probes example, examples/3d/reflection_probes.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates reflection probes. A mirror-like sphere sits among cubes, reflecting the room it is
// in through a probe captured there, or the sky through the camera's environment map, or the sky
// through a map Bevy filters from one image as the app runs. Space switches between the three,
// Enter turns the camera, and the arrows change how rough the sphere is.
internal static class ReflectionProbes
{
    private const float EnvMapIntensity = 5000f;

    private enum ReflectionMode { EnvironmentMap, ReflectionProbe, GeneratedEnvironmentMap }

    private static Entity _camera, _probe, _scene, _text;
    private static AssetHandle _sphereMaterial, _diffuse, _specular, _probeSpecular, _cubes;
    private static MaterialSettings _sphere = new();
    private static ReflectionMode _mode;
    private static bool _rotating;
    private static float _roughness;

    public static void Build(App app)
    {
        (_mode, _rotating, _roughness) = (ReflectionMode.ReflectionProbe, false, 0.2f);
        (_probe, _scene) = (Entity.None, Entity.None);

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _diffuse = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2");
            _specular = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2");
            _probeSpecular = AssetServer.Load(AssetKind.Image, "environment_maps/cubes_reflection_probe_specular_rgb9e5_zstd.ktx2");
            _cubes = AssetServer.LoadGltfScene("models/cubes/Cubes.glb", 0);

            // Bevy's Exposure { ev100: 11.0 }, as a lens: f/1 open for a 2048th of a second at ISO 100.
            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-3.883f, 0.325f, 2.781f), Vec3.Zero, Vec3.UnitY));
            Render.SetPostProcessing(_camera, new PostSettings { Hdr = true, Tonemapper = Tonemapper.AcesFitted });
            Render.SetLensExposure(_camera, aperture: 1f, shutter: 1f / 2048f, sensitivity: 100f);
            Render.SetEnvironmentMap(_camera, _diffuse, _specular, EnvMapIntensity);
            Render.SetSkybox(_camera, _specular, EnvMapIntensity);

            _sphere = new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Metallic = 1f, Roughness = _roughness };
            _sphereMaterial = Render.CreateMaterial(_sphere);
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Sphere, 1f), _sphereMaterial, Transform.Identity);

            SpawnReflectionProbe(ecs);
            _text = Ui.SpawnText(Describe(), new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
        }, "reflection_probes.Setup");

        app.Update(SpawnScene, "reflection_probes.SpawnScene");
        app.Update(Controls, "reflection_probes.Controls");
    }

    // The probe the room was captured into, as large as the room, which reflects it uncorrected.
    private static void SpawnReflectionProbe(EcsWorld ecs)
    {
        _probe = ecs.Spawn();
        ecs.Add(_probe, new Transform(Vec3.Zero, Quat.Identity, new Vec3(2f)));
        ecs.Insert<ParallaxCorrectionRef>(_probe).Value = new ParallaxCorrection.None();
        Render.SetReflectionProbe(_probe, _diffuse, _probeSpecular, EnvMapIntensity);
    }

    // The cubes, spawned once their file has loaded, and again each time the probe returns.
    private static void SpawnScene(BehaviorContext ctx)
    {
        if (_mode != ReflectionMode.ReflectionProbe || _scene != Entity.None) return;
        if (AssetServer.StateOf(_cubes) != AssetLoadState.Loaded) return;
        _scene = ctx.Ecs.SpawnScene(_cubes);
    }

    private static void Controls(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var input = ctx.Input;
        var changed = false;

        if (input.KeyPressed(Key.Space))
        {
            _mode = (ReflectionMode)(((int)_mode + 1) % 3);
            foreach (var entity in new[] { _probe, _scene })
                if (entity != Entity.None && ecs.IsAlive(entity)) ecs.Despawn(entity);
            (_probe, _scene) = (Entity.None, Entity.None);
            if (_mode == ReflectionMode.ReflectionProbe) SpawnReflectionProbe(ecs);

            // The camera is lit by the sky's map, or by one Bevy filters from the sky's image.
            ecs.Get<GeneratedEnvironmentMapLightRef>(_camera)?.Remove();
            if (_mode == ReflectionMode.GeneratedEnvironmentMap)
            {
                Render.SetEnvironmentMap(_camera, AssetHandle.None, AssetHandle.None);
                var generated = ecs.Insert<GeneratedEnvironmentMapLightRef>(_camera);
                (generated.EnvironmentMap, generated.Intensity) = (_specular, EnvMapIntensity);
            }
            else
            {
                Render.SetEnvironmentMap(_camera, _diffuse, _specular, EnvMapIntensity);
            }

            changed = true;
        }

        if (input.KeyPressed(Key.Enter)) (_rotating, changed) = (!_rotating, true);

        var delta = input.KeyDown(Key.ArrowUp) ? 0.01f : input.KeyDown(Key.ArrowDown) ? -0.01f : 0f;
        if (delta != 0f)
        {
            _roughness = Math.Clamp(_roughness + delta, 0f, 1f);
            _sphere.Roughness = _roughness;
            Render.WriteMaterial(_sphereMaterial, _sphere);
            changed = true;
        }

        if (_rotating)
        {
            // Round the middle in the ground's plane at a fifth of a half turn a second.
            var angle = ctx.Time.Delta * MathF.PI / 5f;
            var at = ecs.GetOrDefault<Transform>(_camera).Translation;
            var (cos, sin) = (MathF.Cos(angle), MathF.Sin(angle));
            var turned = new Vec3(at.X * cos - at.Z * sin, at.Y, at.X * sin + at.Z * cos);
            ecs.Set(_camera, Transform.LookingAt(turned, Vec3.Zero, Vec3.UnitY));
        }

        if (changed) Ui.SetText(_text, Describe());
    }

    private static string Describe() => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"""
        {_mode switch { ReflectionMode.EnvironmentMap => "Environment map", ReflectionMode.ReflectionProbe => "Reflection probe", _ => "Generated environment map" }}
        {(_rotating ? "Press Enter to stop rotation" : "Press Enter to start rotation")}
        Roughness: {_roughness:0.00}
        Press Space to switch reflection mode
        Up/Down arrows to change roughness
        """);
}
