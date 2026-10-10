// Bevy's solari example, examples/3d/solari.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using System.Text;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates realtime dynamic raytraced lighting using Bevy Solari, in the pica pica diorama with
// a robot patrolling it, its own light lit or dark, the sun on or off, and how long each of
// Solari's passes takes in the corner.
//
// Needs a bridge built with --solari and a GPU with ray queries. Bevy's example also has a path
// tracer and a scene of many lights behind its command line, which are left out here, as is the
// count of the world cache's cells its panel ends with, a number the bridge's timings do not
// carry.
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
    private static Entity _sun;
    private static AssetHandle _robotLight;

    public static void Configure(Config config)
    {
        config.RayTracedLighting = true;
        config.GpuTimings = true;
    }

    public static void Build(App app)
    {
        Roots.Clear();
        Traced.Clear();
        (_sun, _robotLight) = (Entity.None, AssetHandle.None);

        app.Startup(Setup, "solari.Setup");
        app.SpawnGltf("pica_pica/mini_diorama_01.glb", (ctx, root) =>
        {
            ctx.Ecs.Set(root, new Transform(Vec3.Zero, Quat.Identity, new Vec3(10f)));
            Roots.Add(root);
        });
        app.SpawnGltf("pica_pica/robot_01.glb", (ctx, root) =>
        {
            ctx.Ecs.Set(root, new Transform(Path[0].At, Path[0].Facing, new Vec3(2f)));
            ctx.Ecs.Add(root, new PatrolPath());
            Roots.Add(root);
        });
        app.Update(AddRaytracingMeshes, "solari.AddRaytracingMeshes");
        app.Update(PauseScene, "solari.PauseScene");
        app.Update(ToggleLights, "solari.ToggleLights");

    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _sun = SpawnSun(ecs);

        var camera = ecs.SpawnCamera3d(
            new Transform(new Vec3(0.219417f, 2.5764852f, 6.9718704f), new Quat(-0.1466768f, 0.013738206f, 0.002037309f, 0.989087f), Vec3.One),
            new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f) });
        ecs.Add(camera, new FreeCamera { Speed = 3f });
        if (Render.RayTracingActive) Render.SetRayTracedLighting(camera, true);

        ecs.Add(Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) }), new ControlText());

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
        ecs.Add(performance, new PerformanceText());
    }

    // Solari replaces shadow maps, so the sun casts none of its own.
    private static Entity SpawnSun(EcsWorld ecs)
    {
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 20_000f, Shadows = false });
        ecs.Add(sun, new Transform(Vec3.Zero, SunRotation, Vec3.One));
        return sun;
    }

    // Each mesh of the two models is given to the rays once it has loaded, as Bevy's observer does
    // when a scene is ready, and three of their materials are changed by name: the diorama's
    // lamps off, the robot's lamp lit, and its dark glass made opaque, since the rays do not pass
    // through what transmits light.
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

    // The controls as they stand, the pause, the sun and the robot's lamp.
    internal static string Controls(BehaviorContext ctx)
    {
        var controls = new StringBuilder(ctx.Time.Paused ? "(Space): Resume" : "(Space): Pause");
        controls.Append(_sun != Entity.None ? "\n(1): Disable directional light" : "\n(1): Enable directional light");
        controls.Append(Render.TryReadMaterial(_robotLight, out var settings) && RobotLightOn(settings!)
            ? "\n(2): Disable robot emissive light"
            : "\n(2): Enable robot emissive light");
        controls.Append("\nDenoising: App not compiled with DLSS support");
        return controls.ToString();
    }

    // Each of Solari's passes as the GPU timed it, smoothed over the last frames.
    internal static string Performance()
    {
        var timings = Render.Timings();
        var performance = new StringBuilder();
        var total = 0.0;
        foreach (var (label, pass) in new[]
        {
            ("Light tiles", "solari_lighting/presample_light_tiles"),
            ("World cache", "solari_lighting/world_cache"),
            ("Direct lighting", "solari_lighting/direct_lighting"),
            ("Diffuse indirect", "solari_lighting/diffuse_indirect_lighting"),
            ("Specular indirect", "solari_lighting/specular_indirect_lighting"),
        })
        {
            if (timings.FirstOrDefault(timing => timing.Name == pass).GpuMilliseconds is not { } milliseconds) continue;
            performance.Append(FormattableString.Invariant($"{label,-17}  {milliseconds:0.00} ms\n"));
            total += milliseconds;
        }

        performance.Append(FormattableString.Invariant($"{"Total",-17}  {total:0.00} ms\n"));
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
