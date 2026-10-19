// Bevy's atmosphere example, examples/3d/atmosphere.rs at v0.20.0, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using System.Numerics;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// The sky a planet's air scatters, over a terrain and a sea drawn deferred, the sun turning
// overhead, lighting the sky, the fog under it and the sea's reflections, which the sky lights in
// turn. 1 and 2 work the sky out from lookup textures or by marching rays, 3 and 4 choose earth's
// air or Mars's, Enter stops and starts the sun, and the up and down arrows change the exposure.
// The camera flies as Bevy's free camera does where its crate is compiled in.
internal static class AtmosphereExample
{
    private static Entity _camera;
    private static Entity _sun;
    private static float _ev100;

    // Bevy's GameState.
    private static bool _paused;

    // Bevy's AtmospherePresets, the picture Mars's dust scatters by, which earth's air leaves alone,
    // and the sky as it is now, which each key changes a part of.
    private static AssetHandle _marsPhase;
    private static AtmosphereSettings _sky = new();

    public static void Configure(Config config) => config.CameraControllers = CameraControllers.Free;

    public static void Build(App app)
    {
        (_paused, _ev100, _sky) = (false, 13f, new AtmosphereSettings());

        app.Startup(SetupCameraFog, "atmosphere.SetupCameraFog");
        app.Startup(SetupTerrainScene, "atmosphere.SetupTerrainScene");
        app.Startup(PrintControls, "atmosphere.PrintControls");
        app.Update(DynamicScene, "atmosphere.DynamicScene");
        app.Update(AtmosphereControls, "atmosphere.AtmosphereControls");
    }

    private static void PrintControls(BehaviorContext ctx)
    {
        Console.WriteLine("Atmosphere Example Controls:");
        Console.WriteLine("    1          - Switch to lookup texture rendering method");
        Console.WriteLine("    2          - Switch to raymarched rendering method");
        Console.WriteLine("    3          - Switch to Earth atmosphere");
        Console.WriteLine("    4          - Switch to Mars atmosphere");
        Console.WriteLine("    Enter      - Pause/Resume sun motion");
        Console.WriteLine("    Up/Down    - Increase/Decrease exposure");
    }

    private static void AtmosphereControls(BehaviorContext ctx)
    {
        var input = ctx.Input;

        // Earth's air or Mars's, the planet set again under the ground and the sun's disk the size
        // it looks from there.
        if (input.KeyPressed(Key.Digit3))
        {
            _sky.Planet = SkyPlanet.Earth;
            Render.SetAtmosphere(_camera, _sky);
            Render.SetSunDisk(_sun, SunDisk.Earth);
            Console.WriteLine("Switched to Earth atmosphere");
        }

        if (input.KeyPressed(Key.Digit4))
        {
            _sky.Planet = SkyPlanet.Mars;
            Render.SetAtmosphere(_camera, _sky);
            Render.SetSunDisk(_sun, SunDisk.Mars);
            Console.WriteLine("Switched to Mars atmosphere");
        }

        if (input.KeyPressed(Key.Digit1))
        {
            _sky.Method = SkyMethod.LookupTexture;
            Render.SetAtmosphere(_camera, _sky);
            Console.WriteLine("Switched to lookup texture rendering method");
        }

        if (input.KeyPressed(Key.Digit2))
        {
            _sky.Method = SkyMethod.Raymarched;
            Render.SetAtmosphere(_camera, _sky);
            Console.WriteLine("Switched to raymarched rendering method");
        }

        if (input.KeyPressed(Key.Enter)) _paused = !_paused;

        var exposure = 0f;
        if (input.KeyDown(Key.ArrowUp)) exposure -= ctx.Time.Delta * 2f;
        if (input.KeyDown(Key.ArrowDown)) exposure += ctx.Time.Delta * 2f;
        if (exposure != 0f)
        {
            _ev100 += exposure;
            Render.SetExposure(_camera, _ev100);
        }
    }

    private static void SetupCameraFog(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // Bevy's DefaultOpaqueRendererMethod::deferred, its black clear color and its ambient light
        // of none.
        Render.SetDeferredRendering(true);
        Render.SetClearColor((0f, 0f, 0f, 1f));
        Render.SetAmbientLight((1f, 1f, 1f), 0f);

        _marsPhase = AssetServer.Load(AssetKind.Image, "textures/mars_mie_phase.ktx2");
        _sky.DustPhase = _marsPhase;

        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2.8f, 0.045f, 0f), Vec3.Zero, Vec3.UnitY));

        // The atmosphere, earth's, with Bevy's own settings, which can be adjusted to change the
        // rendering quality.
        Render.SetAtmosphere(_camera, _sky);

        // The sunlight the atmosphere is meant for is very bright, so the exposure is raised to bring
        // the scene into a nicer range. ACES fitted was chosen because it looked good with the
        // scene, and bloom gives the sun a more natural look. Multisampling is off, which deferred
        // drawing does not support, for temporal antialiasing.
        Render.SetPostProcessing(_camera, new PostSettings
        {
            Hdr = true,
            Msaa = 1,
            AntiAlias = AntiAliasPass.Temporal,
            Tonemapper = Tonemapper.AcesFitted,
            Bloom = true,
        });
        Render.SetExposure(_camera, _ev100);

        // The reflections and the ambient light of this view come from the atmosphere.
        Render.SetSkyLighting(_camera);

        ecs.Insert<VolumetricFogRef>(_camera).AmbientIntensity = 0f;
        Render.SetScreenSpaceReflections(_camera, new ReflectionSettings { FadeInRoughness = (0f, 0f) });

        Render.SetFreeCamera(_camera, new FreeCameraSettings());
    }

    private static void SetupTerrainScene(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // The sun, at Bevy's raw sunlight, the illuminance of the sun before the air filters it,
        // which is the right light for the atmosphere to filter.
        _sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 130_000f, Shadows = true });
        ecs.Add(_sun, Transform.LookingAt(new Vec3(1f, 0.4f, 0f), Vec3.Zero, Vec3.UnitY));
        ecs.Insert<VolumetricLightRef>(_sun);
        Render.SetSunDisk(_sun, SunDisk.Earth);

        // The fog volume.
        var fog = ecs.Spawn();
        ecs.Add(fog, new Transform(new Vec3(0f, 0.5f, 0f), Quat.Identity, new Vec3(10f, 1f, 10f)));
        ecs.Insert<FogVolumeRef>(fog);

        var terrain = ecs.SpawnScene(AssetServer.LoadGltfScene("models/terrain/terrain.glb", 0));
        ecs.Set(terrain, new Transform(new Vec3(-1f, 0f, -0.5f), Quat.FromRotationY(MathF.PI / 2f), new Vec3(0.5f)));
        ecs.Add(terrain, new Terrain());

        SpawnWater(ecs);
    }

    // The sea, its ripples four octaves of a normal map drifting over each other, black and smooth as
    // glass, drawn into the deferred buffers by its own shader. Its octaves are Bevy's values, which
    // its example calls random ones for some variety.
    private static void SpawnWater(EcsWorld ecs)
    {
        var water = Shaders.CreateMaterial(Shaders.CreateProgram(new ShaderProgramSettings
            {
                PrepassVertex = new ShaderStage("shaders/water_material.slang", "prepass_vertex"),
                Deferred = "shaders/water_material.slang",
            }))
            .SetTexture("water_normals", AssetServer.LoadImage("textures/water_normals.png", TextureSettings.Data))
            .SetSampler("water_sampler", SamplerSettings.Linear)
            .Set("octave_vectors", new[] { new Vector4(0.080f, 0.059f, 0.073f, -0.062f), new Vector4(0.153f, 0.138f, -0.149f, -0.195f) })
            .Set("octave_scales", new Vector4(1f, 2.1f, 7.9f, 14.9f) * 500f)
            .Set("octave_strengths", new Vector4(0.16f, 0.18f, 0.093f, 0.044f) * 0.2f)
            .Set("roughness", 0f);

        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 2f, 2f), water, new Transform(Vec3.Zero, Quat.Identity, new Vec3(100f)));
    }

    // The sun turns about X, a tenth of a half turn a second, unless it is paused.
    private static void DynamicScene(BehaviorContext ctx)
    {
        if (_paused) return;

        var transform = ctx.Ecs.GetOrDefault<Transform>(_sun);
        transform.Rotation = Quat.FromRotationX(-ctx.Time.Delta * MathF.PI / 10f) * transform.Rotation;
        ctx.Ecs.Set(_sun, transform);
    }
}

/// <summary>Bevy's Terrain, marking the terrain's scene.</summary>
[Behavior]
public partial struct Terrain;
