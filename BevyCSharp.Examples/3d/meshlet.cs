using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Meshlet rendering for dense high-poly scenes (experimental). A row of bunnies in Bevy's standard
// material and a row drawn each cluster in a color of its own, from a mesh cut into clusters ahead
// of time. As Bevy says of its own, this is not the kind of scene that gains from meshlets.
//
// Needs a bridge built with --meshlet and a GPU with 64-bit texture atomics. Elsewhere it opens and
// draws the floor alone, since a meshlet mesh is drawn by nothing else.
internal static class Meshlet
{
    private static Entity _wiggler;

    // Room for the clusters Bevy's example asks for, which turns meshlets on.
    public static void Configure(Config config) => config.MeshletClusters = 1 << 14;

    public static void Build(App app)
    {
        app.Startup(Setup, "meshlet.Setup");

        // Bevy's BunnyWiggler, one bunny moved back and forth along Z.
        app.Update(ctx =>
        {
            if (_wiggler == Entity.None) return;
            var at = ctx.Ecs.GetOrDefault<Transform>(_wiggler);
            at.Translation += new Vec3(0f, 0f, MathF.Cos(ctx.Time.Elapsed * 10f) * 0.003f);
            ctx.Ecs.Set(_wiggler, at);
        }, "meshlet.BunnyWiggler");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _wiggler = Entity.None;

        if (ecs.Resource<DirectionalLightShadowMapRef>() is { } shadowMap) shadowMap.Size = 4096;

        var camera = ecs.Camera(Transform.LookingAt(new Vec3(1.8f, 0.4f, -0.1f), Vec3.Zero, Vec3.UnitY));
        Render.SetEnvironmentMap(
            camera,
            AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"),
            AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"),
            150f);
        ecs.Add(camera, new FreeCamera());

        // Bevy's FULL_DAYLIGHT, turned by its EulerRot::ZYX, with one cascade reaching fifteen
        // meters.
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 20_000f, Shadows = true });
        ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationY(MathF.PI * -0.15f) * Quat.FromRotationX(MathF.PI * -0.15f), Vec3.One));
        Render.SetShadowCascades(sun, cascades: 1, maximum: 15f);

        ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Roughness = 1f }), Transform.Identity);

        if (!Render.MeshletsActive) return;

        // A mesh cut into clusters ahead of time with Bevy's meshlet processor, as Bevy's example
        // loads it.
        var bunny = AssetServer.Load(AssetKind.MeshletMesh, "meshlet/bunny.meshlet_mesh");
        var clusters = Render.CreateClusterMaterial();
        string[] colors = ["#dc2626", "#ea580c", "#facc15", "#16a34a", "#0284c7"];

        for (var x = -2; x <= 2; x++)
        {
            var hex = Color.FromHex(colors[x + 2]);
            var front = Spawn(ecs, bunny, Render.CreateMaterial(new MaterialSettings { BaseColor = (hex.R, hex.G, hex.B, 1f), Roughness = (x + 2) / 4f }),
                new Transform(new Vec3(x / 2f, 0f, -0.3f), Quat.Identity, new Vec3(0.2f)));
            if (x == 1) _wiggler = front;

            Spawn(ecs, bunny, clusters, new Transform(new Vec3(x / 2f, 0f, 0.3f), Quat.FromRotationY(MathF.PI), new Vec3(0.2f)));
        }
    }

    private static Entity Spawn(EcsWorld ecs, AssetHandle meshlet, AssetHandle material, Transform at)
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, at);
        Render.SetMeshletMesh(ecs, entity, meshlet);
        Render.SetMaterial(ecs, entity, material);
        return entity;
    }
}
