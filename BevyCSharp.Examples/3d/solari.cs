// Bevy's solari example, examples/3d/solari.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using System.Text;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates realtime dynamic raytraced lighting using Bevy Solari, in the pica pica diorama with
// a robot patrolling it, its own light lit or dark, the sun on or off, and how long each of
// Solari's passes takes and how many cells its world cache holds in the corner. --many-lights
// draws a plain of eight thousand boxes under four hundred glowing spheres instead, and
// --pathtracer draws either with Bevy's reference path tracer in the lighting's place.
//
// Needs a bridge built with --solari and a GPU with ray queries. The boxes and the spheres' colors
// are drawn from .NET's generator seeded as Bevy's is, which deals other numbers than Bevy's.
internal static class Solari
{
    private static readonly Quat SunRotation = new(-0.13334629f, -0.86597735f, -0.3586996f, 0.3219264f);

    // The yellow of the robot's lamp, a million times over.
    private static readonly (float R, float G, float B, float A) RobotLight = Scaled(Color.FromSrgb(0.941f, 0.714f, 0.043f), 1_000_000f);

    // Where the robot walks, a corner at a time, and which way it faces there.
    internal static readonly (Vec3 At, Quat Facing)[] Path =
    [
        (new Vec3(-2f, 0.05f, -2.1f), Quat.FromRotationY(MathF.PI / 2f)),
        (new Vec3(2.2f, 0.05f, -2.1f), Quat.FromRotationY(0f)),
        (new Vec3(2.2f, 0.05f, 2.1f), Quat.FromRotationY(3f * MathF.PI / 2f)),
        (new Vec3(-2f, 0.05f, 2.1f), Quat.FromRotationY(MathF.PI)),
    ];

    private static readonly List<Entity> Roots = [];
    private static readonly HashSet<Entity> Traced = [];
    private static Entity _sun, _camera;
    private static AssetHandle _robotLight;

    // Bevy's command line, the reference path tracer in place of the realtime lighting and the
    // scene of many lights in place of the diorama.
    private static bool PathTracer => Environment.GetCommandLineArgs().Contains("--pathtracer");
    internal static bool ManyLights => Environment.GetCommandLineArgs().Contains("--many-lights");

    public static void Configure(Config config)
    {
        config.RayTracedLighting = true;
        config.PathTracer = PathTracer;
        config.GpuTimings = true;
    }

    public static void Build(App app)
    {
        Roots.Clear();
        Traced.Clear();
        (_sun, _robotLight) = (Entity.None, AssetHandle.None);

        if (ManyLights)
        {
            app.Startup(SetupManyLights, "solari.SetupManyLights");
        }
        else
        {
            app.Startup(SetupPicaPica, "solari.SetupPicaPica");
            app.SpawnGltf("pica_pica/mini_diorama_01.glb", (ctx, root) =>
            {
                ctx.Ecs.Set(root, new Transform(Vec3.Zero, Quat.Identity, new Vec3(10f)));
                Roots.Add(root);
            });
            app.SpawnGltf("pica_pica/robot_01.glb", (ctx, root) =>
            {
                ctx.Ecs.Set(root, new Transform(Path[0].At, Path[0].Facing, new Vec3(2f)));
                if (!PathTracer) ctx.Ecs.Add(root, new PatrolPath());
                Roots.Add(root);
            });
            app.Update(AddRaytracingMeshes, "solari.AddRaytracingMeshes");
        }

        // The path tracer changes nothing as it runs and says nothing, as in Bevy's.
        if (PathTracer) return;
        app.Update(ToggleRestir, "solari.ToggleRestir");
        if (ManyLights) return;
        app.Update(PauseScene, "solari.PauseScene");
        app.Update(ToggleLights, "solari.ToggleLights");
    }

    private static void SetupPicaPica(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _sun = SpawnSun(ecs);

        _camera = SpawnCamera(ecs, new Transform(new Vec3(0.219417f, 2.5764852f, 6.9718704f), new Quat(-0.1466768f, 0.013738206f, 0.002037309f, 0.989087f), Vec3.One));
        SpawnTexts(ecs);
    }

    // Bevy's setup_many_lights, a plain of boxes of every color and roughness under a grid of
    // spheres glowing in every color, from a camera high above, its bloom a little lower than
    // Bevy's own.
    private static void SetupManyLights(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var random = new Random(42);
        float Between(float low, float high) => low + random.NextSingle() * (high - low);

        // A plane 400 across whose texture repeats three times along each side, its texture
        // coordinates scaled as Bevy's are, since the rays read a mesh's coordinates and not a
        // material's scale of them.
        var plane = Render.CreateMesh(new MeshData
        {
            Positions = [new Vec3(-200f, 0f, -200f), new Vec3(200f, 0f, -200f), new Vec3(-200f, 0f, 200f), new Vec3(200f, 0f, 200f)],
            Normals = [Vec3.UnitY, Vec3.UnitY, Vec3.UnitY, Vec3.UnitY],
            Uvs = [0f, 0f, 3f, 0f, 0f, 3f, 3f, 3f],
            Indices = [3, 1, 2, 0, 2, 1],
        });
        var checker = AssetServer.LoadImage("textures/uv_checker_bw.png", new TextureSettings { Wrap = TextureWrap.Repeat });
        SpawnTraced(ecs, plane, Render.CreateMaterial(new MaterialSettings { BaseColorTexture = checker, Roughness = 0f }), Transform.Identity);

        var cube = Render.CreateMesh(MeshShape.Cuboid);
        for (var i = 0; i < 8000; i++)
        {
            var material = Render.CreateMaterial(new MaterialSettings
            {
                BaseColor = Color.FromSrgb(random.NextSingle(), random.NextSingle(), random.NextSingle()),
                Roughness = random.NextSingle(),
            });
            var scale = new Vec3(Between(0.2f, 2f), Between(0.2f, 2f), Between(0.2f, 2f));
            var at = new Vec3(Between(-180f, 180f), 0.2f, Between(-180f, 180f));
            SpawnTraced(ecs, cube, material, new Transform(at, Quat.Identity, scale));
        }

        var sphere = Render.CreateMesh(MeshShape.Sphere);
        for (var x = -10; x <= 10; x++)
        {
            for (var y = -10; y <= 10; y++)
            {
                var glow = (random.NextSingle() * 60000f, random.NextSingle() * 60000f, random.NextSingle() * 60000f, 1f);
                var material = Render.CreateMaterial(new MaterialSettings { Emissive = glow });
                SpawnTraced(ecs, sphere, material, new Transform(new Vec3(x * 20f, 7f, y * 20f), Quat.Identity, Vec3.One));
            }
        }

        _camera = SpawnCamera(ecs, new Transform(new Vec3(6.11329f, 166.74896f, 451.8226f), new Quat(-0.183938f, 0.009093744f, 0.0017017953f, 0.9828943f), Vec3.One));
        ecs.Insert<BloomRef>(_camera).Intensity = 0.1f;
        SpawnTexts(ecs);
    }

    // An entity drawing a mesh the rays meet, drawn by the rays alone where the path tracer draws.
    private static void SpawnTraced(EcsWorld ecs, AssetHandle mesh, AssetHandle material, Transform transform)
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, transform);
        Render.SetMesh(ecs, entity, mesh);
        Render.SetMaterial(ecs, entity, material);
        if (!Render.RayTracingActive) return;
        Render.SetRayTraced(entity, mesh);
        if (PathTracer) ecs.Wrap<Mesh3dRef>(entity).Remove();
    }

    // The camera, lit by Solari or drawn by the path tracer where the GPU traces rays.
    private static Entity SpawnCamera(EcsWorld ecs, Transform transform)
    {
        var camera = ecs.SpawnCamera3d(transform, new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f) });
        ecs.Add(camera, new FreeCamera { Speed = 3f });
        if (Render.RayTracingActive)
        {
            if (PathTracer) Render.SetPathTraced(camera, true);
            else Render.SetRayTracedLighting(camera, true);
        }

        return camera;
    }

    // The controls in the bottom corner and the passes' times in the top one, which the path
    // tracer leaves empty, as Bevy's does.
    private static void SpawnTexts(EcsWorld ecs)
    {
        var controls = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
        if (!PathTracer) ecs.Add(controls, new ControlText());

        var panel = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Right = Length.Px(0f),
            Padding = Sides.All(Length.Px(4f)),
            Corners = new Corners(Length.Zero, Length.Zero, Length.Zero, Length.Px(4f)),
            Color = Color.FromSrgb(0.1f, 0.1f, 0.1f, 0.8f),
        });
        var performance = Ui.SpawnText(string.Empty, new UiSettings(), new UiTextSettings { FontSize = 8f });
        ecs.SetParent(performance, panel);
        if (!PathTracer) ecs.Add(performance, new PerformanceText());
    }

    // Solari replaces shadow maps, so the sun casts none of its own.
    private static Entity SpawnSun(EcsWorld ecs)
    {
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 20_000f, Shadows = false });
        ecs.Add(sun, new Transform(Vec3.Zero, SunRotation, Vec3.One));
        return sun;
    }

    // Each mesh of the two models is given to the rays once it has loaded, as Bevy's observer does
    // when a scene is ready, and drawn by the rays alone where the path tracer draws, and three of
    // their materials are changed by name: the diorama's lamps off, the robot's lamp lit, and its
    // dark glass made opaque, since the rays do not pass through what transmits light.
    private static void AddRaytracingMeshes(BehaviorContext ctx)
    {
        if (!Render.RayTracingActive) return;
        var ecs = ctx.Ecs;

        foreach (var root in Roots)
        {
            foreach (var entity in ecs.Descendants(root))
            {
                if (Traced.Contains(entity)) continue;
                var mesh = Render.MeshOf(ecs, entity);
                if (mesh == AssetHandle.None || AssetServer.StateOf(mesh) != AssetLoadState.Loaded) continue;

                Render.SetRayTraced(entity, mesh);
                if (PathTracer) ecs.Wrap<Mesh3dRef>(entity).Remove();
                Traced.Add(entity);

                var material = Render.MaterialOf(ecs, entity);
                if (ecs.Get<GltfMaterialNameRef>(entity)?.Value is not { } name || !Render.TryReadMaterial(material, out var settings)) continue;

                switch (name)
                {
                    case "material":
                        settings!.Emissive = (0f, 0f, 0f, 1f);
                        break;
                    case "Lights":
                        (settings!.Emissive, settings.AlphaMode, settings.Transmission) = (RobotLight, AlphaMode.Opaque, 0f);
                        _robotLight = material;
                        break;
                    case "Glass_Dark_01":
                        (settings!.AlphaMode, settings.Transmission) = (AlphaMode.Opaque, 0f);
                        break;
                    default:
                        continue;
                }
                Render.WriteMaterial(material, settings);
            }
        }
    }

    private static void PauseScene(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space)) return;
        if (ctx.Time.Paused) ctx.Time.Resume();
        else ctx.Time.Pause();
    }

    private static void ToggleLights(BehaviorContext ctx)
    {
        var (ecs, input) = (ctx.Ecs, ctx.Input);
        if (input.KeyPressed(Key.Digit1))
        {
            if (_sun != Entity.None)
            {
                ecs.Despawn(_sun);
                _sun = Entity.None;
            }
            else
            {
                _sun = SpawnSun(ecs);
            }
        }

        if (input.KeyPressed(Key.Digit2) && Render.TryReadMaterial(_robotLight, out var settings))
        {
            settings!.Emissive = RobotLightOn(settings) ? (0f, 0f, 0f, 1f) : RobotLight;
            Render.WriteMaterial(_robotLight, settings);
        }
    }

    // The controls as they stand, the pause, the sun and the robot's lamp in the diorama, and the
    // reuse of samples in either scene.
    internal static string Controls(BehaviorContext ctx)
    {
        var controls = new StringBuilder();
        if (!ManyLights)
        {
            controls.Append(ctx.Time.Paused ? "(Space): Resume" : "(Space): Pause");
            controls.Append(_sun != Entity.None ? "\n(1): Disable directional light" : "\n(1): Enable directional light");
            controls.Append(Render.TryReadMaterial(_robotLight, out var settings) && RobotLightOn(settings!)
                ? "\n(2): Disable robot emissive light"
                : "\n(2): Enable robot emissive light");
        }

        controls.Append("\nDenoising: App not compiled with DLSS support");
        controls.Append(ctx.Ecs.Get<SolariLightingRef>(_camera)?.Restir == true ? "\n(R): Disable ReSTIR" : "\n(R): Enable ReSTIR");
        return controls.ToString();
    }

    // Bevy's toggle_restir, R turning the reuse of samples between pixels and frames on and off.
    private static void ToggleRestir(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.R) && ctx.Ecs.Get<SolariLightingRef>(_camera) is { } lighting) lighting.Restir = !lighting.Restir;
    }

    // Each of Solari's passes as the GPU timed it, and how many of the world cache's cells are in
    // use, each smoothed over the last frames as Bevy reads them from its store of diagnostics.
    internal static string Performance()
    {
        var performance = new StringBuilder();
        var total = 0.0;
        foreach (var (label, path) in new[]
        {
            ("Light tiles", "render/solari_lighting/presample_light_tiles/elapsed_gpu"),
            ("World cache", "render/solari_lighting/world_cache/elapsed_gpu"),
            ("Lighting", "render/solari_lighting/lighting/elapsed_gpu"),
        })
        {
            if (!Diagnostics.TryRead(path, out var reading) || reading.Smoothed is not { } milliseconds) continue;
            performance.Append(FormattableString.Invariant($"{label,-17}  {milliseconds:0.00} ms\n"));
            total += milliseconds;
        }

        performance.Append(FormattableString.Invariant($"{"Total",-17}  {total:0.00} ms\n"));

        // Out of the cache's two to the twentieth.
        if (Diagnostics.TryRead("render/solari_lighting/world_cache_active_cells_count", out var cells) && cells.Smoothed is { } count)
            performance.Append(FormattableString.Invariant($"\nWorld cache cells {(uint)count} ({count * 100.0 / (1 << 20):0}%)"));

        return performance.ToString();
    }

    private static bool RobotLightOn(MaterialSettings settings) => settings.Emissive is not (0f, 0f, 0f, _);

    private static (float R, float G, float B, float A) Scaled(Color color, float by) => (color.R * by, color.G * by, color.B * by, 1f);
}

/// <summary>
/// The robot's walk, which corner of the example's path it is making for. Bevy's holds the path
/// too, which is no value a component here can keep, so the path is the example's.
/// </summary>
[Behavior]
public partial struct PatrolPath
{
    /// <summary>The corner it is making for.</summary>
    public int I;

    /// <summary>
    /// Walked a meter a second to the corner, turning to face along the next side when it gets
    /// there, by the virtual clock, so it stops while the scene is paused.
    /// </summary>
    [OnUpdate]
    public void Patrol(BehaviorContext ctx, ref Transform transform)
    {
        var (target, facing) = Solari.Path[I];
        var distance = (target - transform.Translation).Length;
        if (distance < 0.01f)
        {
            (transform.Translation, transform.Rotation) = (target, facing);
            I = (I + 1) % Solari.Path.Length;
            (target, facing) = Solari.Path[I];
            distance = (target - transform.Translation).Length;
        }

        var step = ctx.Time.Delta;
        if (step > distance) (transform.Translation, transform.Rotation) = (target, facing);
        else transform.Translation += (target - transform.Translation) * (step / distance);
    }
}

/// <summary>The text that says the controls.</summary>
[Behavior]
public partial struct ControlText
{
    /// <summary>The controls written as they stand, after Update, as Bevy writes them in PostUpdate.</summary>
    [OnPostUpdate]
    public void UpdateControlText(BehaviorContext ctx) => Ui.SetText(ctx.Entity, Solari.Controls(ctx));
}

/// <summary>The text that says how long each pass took.</summary>
[Behavior]
public partial struct PerformanceText
{
    /// <summary>The passes' times written, after Update, as Bevy writes them in PostUpdate.</summary>
    [OnPostUpdate]
    public void UpdatePerformanceText(BehaviorContext ctx) => Ui.SetText(ctx.Entity, Solari.Performance());
}
