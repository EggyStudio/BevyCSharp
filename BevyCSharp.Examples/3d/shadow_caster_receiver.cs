using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates how to prevent meshes from casting/receiving shadows in a 3d scene.
internal static class ShadowCasterReceiver
{

    private static readonly Dictionary<Entity, MeshFlags> Flags = [];
    private static Entity _point, _sun;
    private static bool _usingPoint;

    public static void Build(App app)
    {
        Console.WriteLine(
            "Controls:\n"
            + "    C      - toggle shadow casters (i.e. casters become not, and not casters become casters)\n"
            + "    R      - toggle shadow receivers (i.e. receivers become not, and not receivers become receivers)\n"
            + "    L      - switch between directional and point lights");

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Flags.Clear();

            var white = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Roughness = 1f });
            var sphere = Render.CreateMesh(MeshShape.Sphere, 0.25f);

            Spawn(ecs, sphere, Scene.Material((1f, 0f, 0f, 1f)), Transform.At(-1f, 2f, 0f), MeshFlags.None);
            Spawn(ecs, sphere, Scene.Material((0f, 0f, 1f, 1f)), Transform.At(1f, 2f, 0f), MeshFlags.NoShadowCasting);

            var plane = Render.CreateMesh(MeshShape.Plane, 20f, 20f);
            Spawn(ecs, plane, Scene.Material((0f, 1f, 0f, 1f)), Transform.At(0f, 1f, -10f), MeshFlags.NoShadowCasting | MeshFlags.NoShadowReceiving);
            Spawn(ecs, plane, white, Transform.Identity, MeshFlags.None);

            Console.WriteLine("Using DirectionalLight");

            _point = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 0f, Range = 500f, Shadows = true });
            ecs.Add(_point, Transform.At(5f, 5f, 0f));

            _sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 1000f, Shadows = true });
            ecs.Add(_sun, new Transform(Vec3.Zero, Quat.FromRotationY(MathF.PI / 2f) * Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
            Render.SetShadowCascades(_sun, maximum: 25f, firstBound: 7f);

            ecs.Camera(Transform.LookingAt(new Vec3(-5f, 5f, 5f), new Vec3(-1f, 1f, 0f), Vec3.UnitY));
        });

        app.Update(ctx =>
        {
            if (ctx.Input.KeyPressed(Key.L))
            {
                _usingPoint = !_usingPoint;
                Console.WriteLine(_usingPoint ? "Using PointLight" : "Using DirectionalLight");
                ctx.Ecs.Wrap<PointLightRef>(_point).Intensity = _usingPoint ? 1_000_000f : 0f;
                ctx.Ecs.Wrap<DirectionalLightRef>(_sun).Illuminance = _usingPoint ? 0f : 1000f;
            }

            if (ctx.Input.KeyPressed(Key.C))
            {
                Console.WriteLine("Toggling casters");
                Toggle(ctx.Ecs, MeshFlags.NoShadowCasting);
            }

            if (ctx.Input.KeyPressed(Key.R))
            {
                Console.WriteLine("Toggling receivers");
                Toggle(ctx.Ecs, MeshFlags.NoShadowReceiving);
            }
        }, "shadow_caster_receiver.Toggle");
    }

    private static void Spawn(EcsWorld ecs, AssetHandle mesh, AssetHandle material, Transform at, MeshFlags flags)
    {
        var entity = ecs.Mesh(mesh, material, at);
        Render.SetMeshFlags(ecs, entity, flags);
        Flags[entity] = flags;
    }

    // Every mesh's flag turned over, so what cast stops and what did not starts.
    private static void Toggle(EcsWorld ecs, MeshFlags flag)
    {
        foreach (var entity in Flags.Keys.ToList())
        {
            Flags[entity] ^= flag;
            Render.SetMeshFlags(ecs, entity, Flags[entity]);
        }
    }
}
