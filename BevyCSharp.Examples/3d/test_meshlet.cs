// Bevy's test_meshlet example, tests/3d/test_meshlet.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Meshlets shown beside the meshes they were cut from as the app starts: a sphere and a cube, each
// drawn as a mesh and as a meshlet mesh at four distances, in a material whose every mip level is a
// checkerboard of its own color, so the level each draws at shows. M swaps the meshlet meshes'
// material for one that colors each cluster.
//
// Needs a bridge built with --meshlet and a GPU with 64-bit texture atomics. Elsewhere it draws the
// meshes alone, since a meshlet mesh is drawn by nothing else.
internal static class TestMeshlet
{
    private static AssetHandle _mipmap;
    private static AssetHandle _debug;
    private static bool _showingDebug;

    // Room for the clusters Bevy's example asks for, which turns meshlets on.
    public static void Configure(Config config) => config.MeshletClusters = 1 << 14;

    public static void Build(App app)
    {
        app.Startup(Setup, "test_meshlet.Setup");
        app.Update(ToggleMaterials, "test_meshlet.ToggleMaterials");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _showingDebug = false;

        var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 1.5f, -3f), new Vec3(0f, 0f, 1.5f), Vec3.UnitY));
        Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
        ecs.Add(camera, new FreeCamera());
        Render.SetAmbientLight((1f, 1f, 1f), 500f);

        // Bevy's FULL_DAYLIGHT, from above and a little behind, looking at the middle with Z up.
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 20_000f });
        ecs.Add(sun, Transform.LookingAt(new Vec3(0f, 1f, -0.5f), Vec3.Zero, Vec3.UnitZ));

        var top = new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) };
        Ui.SpawnText("M: toggle debug material", top);
        Ui.SpawnText("meshlet", new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Percent(25f) });
        Ui.SpawnText("regular", new UiSettings { Absolute = true, Bottom = Length.Px(12f), Right = Length.Percent(25f) });

        var sphere = Render.CreateMesh(MeshShape.UvSphere, 0.5f, 48f, 27f);
        var cube = Render.CreateMesh(MeshShape.Cuboid, 0.8f, 0.8f, 0.8f);
        _mipmap = MipmapMaterial();
        _debug = Render.CreateClusterMaterial();

        // Cut into clusters here, as Bevy's MeshletMesh::from_mesh does, only where meshlets run.
        var meshlets = Render.MeshletsActive;
        var sphereMeshlet = meshlets ? Render.CreateMeshletMesh(sphere) : AssetHandle.None;
        var cubeMeshlet = meshlets ? Render.CreateMeshletMesh(cube) : AssetHandle.None;

        foreach (var distance in new[] { 0f, 8f, 32f, 128f })
        {
            ecs.SpawnMesh(sphere, _mipmap, Transform.At(-0.5f, 0f, distance));
            ecs.SpawnMesh(cube, _mipmap, Transform.At(-1.5f, 0f, distance));

            if (!meshlets) continue;
            Meshlet(ecs, sphereMeshlet, Transform.At(0.5f, 0f, distance));
            Meshlet(ecs, cubeMeshlet, Transform.At(1.5f, 0f, distance));
        }
    }

    private static void Meshlet(EcsWorld ecs, AssetHandle meshlet, Transform at)
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, at);
        ecs.Add(entity, new TestMeshletMesh());
        Render.SetMeshletMesh(ecs, entity, meshlet);
        Render.SetMaterial(ecs, entity, _mipmap);
    }

    // M puts the other material on every meshlet mesh.
    private static void ToggleMaterials(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.M)) return;

        _showingDebug = !_showingDebug;
        foreach (var entity in ctx.Ecs.EntitiesWith<TestMeshletMesh>())
            Render.SetMaterial(ctx.Ecs, entity, _showingDebug ? _debug : _mipmap);
    }

    // A material whose image has a checkerboard of its own color at each mip level, so which level a
    // surface is drawn at shows, as Bevy's mipmap_material makes it.
    private static AssetHandle MipmapMaterial()
    {
        (byte R, byte G, byte B)[] colors = [(255, 0, 0), (0, 255, 0), (0, 0, 255), (255, 255, 0), (0, 255, 255), (255, 0, 255)];
        const uint size = 256;

        var image = Shaders.CreateImage(size, size, ShaderImageFormat.Rgba8, mips: (uint)colors.Length);
        for (var level = 0; level < colors.Length; level++)
        {
            var side = size >> level;
            Shaders.WriteImage<byte>(image, Checkerboard((int)side, colors[level]), 0, 0, side, side, mip: (uint)level);
        }

        return Render.CreateMaterial(new MaterialSettings { BaseColorTexture = image, Roughness = 1f });
    }

    // A square of the color and black, in eight squares a side down to squares a texel across, as
    // Bevy's checkerboard draws it.
    private static byte[] Checkerboard(int size, (byte R, byte G, byte B) color)
    {
        var shift = Math.Max(0, (int)Math.Log2(size) - 3);
        var texels = new byte[size * size * 4];

        for (var i = 0; i < size * size; i++)
        {
            var (x, y) = ((i % size) >> shift, (i / size) >> shift);
            var lit = ((x + y) & 1) == 0;
            (texels[i * 4], texels[i * 4 + 1], texels[i * 4 + 2], texels[i * 4 + 3]) =
                lit ? (color.R, color.G, color.B, (byte)255) : ((byte)0, (byte)0, (byte)0, (byte)255);
        }

        return texels;
    }
}

/// <summary>A meshlet mesh of the example, whose material M swaps.</summary>
[Behavior]
public partial struct TestMeshletMesh
{
}
