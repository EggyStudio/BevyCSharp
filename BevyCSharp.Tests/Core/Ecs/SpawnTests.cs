using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// What Bevy's examples spawn in one bundle spawned in one call here, a mesh with its material and
/// its place, a point light, a camera, and a glTF file's scene, and a scene walked from its root.
/// </summary>
[Collection("engine")]
public sealed class SpawnTests
{
    [SkippableFact]
    public void AMeshALightAndACameraAreEachSpawnedInPlace()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);
        AssetHandle mesh = AssetHandle.None, material = AssetHandle.None, drawnWith = AssetHandle.None, paintedWith = AssetHandle.None;
        Vec3 meshAt = default, lightAt = default, cameraAt = default;
        harness.OnContext(Stage.Startup, ctx =>
        {
            mesh = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
            material = Render.CreateMaterial(Color.FromSrgb8(200, 100, 50));
            var cube = ctx.Ecs.SpawnMesh(mesh, material, Transform.At(1f, 2f, 3f));
            (drawnWith, paintedWith, meshAt) = (Render.MeshOf(ctx.Ecs, cube), Render.MaterialOf(ctx.Ecs, cube), ctx.Ecs.GetRef<Transform>(cube).Translation);

            var light = ctx.Ecs.SpawnPointLight(new Vec3(4f, 5f, 6f), shadows: true);
            lightAt = ctx.Ecs.GetRef<Transform>(light).Translation;

            var camera = ctx.Ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 2f, 8f), Vec3.Zero, Vec3.UnitY));
            cameraAt = ctx.Ecs.GetRef<Transform>(camera).Translation;
        });
        harness.Run();

        Assert.Equal(mesh, drawnWith);
        Assert.Equal(material, paintedWith);
        Assert.Equal(new Vec3(1f, 2f, 3f), meshAt);
        Assert.Equal(new Vec3(4f, 5f, 6f), lightAt);
        Assert.Equal(new Vec3(0f, 2f, 8f), cameraAt);
    }

    [Fact]
    public void ADescendantIsFoundUnderItsRootNearerOnesFirst()
    {
        using var harness = new EngineHarness(frames: 1);
        var walked = new List<Entity>();
        Entity[] order = [];
        harness.OnContext(Stage.Startup, ctx =>
        {
            var root = ctx.Ecs.Spawn();
            var (a, b) = (ctx.Ecs.Spawn(), ctx.Ecs.Spawn());
            var (aa, ba) = (ctx.Ecs.Spawn(), ctx.Ecs.Spawn());
            var aaa = ctx.Ecs.Spawn();
            ctx.Ecs.SetParent(a, root);
            ctx.Ecs.SetParent(b, root);
            ctx.Ecs.SetParent(aa, a);
            ctx.Ecs.SetParent(ba, b);
            ctx.Ecs.SetParent(aaa, aa);
            order = [a, b, aa, ba, aaa];
            walked.AddRange(ctx.Ecs.Descendants(root));
        });
        harness.Run();

        Assert.Equal(order, walked);
    }

    [SkippableFact]
    public void AGltfSceneIsSpawnedOnceItHasLoadedAndHandedOverWhole()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 0, fps: 240);
        var calls = 0;
        var meshes = 0;
        harness.App.SpawnGltf("models/triangle.gltf", (ctx, root) =>
        {
            calls++;
            meshes = ctx.Ecs.Descendants(root).Count(entity => Render.MeshOf(ctx.Ecs, entity) != AssetHandle.None);
        });
        harness.OnContext(Stage.Last, ctx =>
        {
            if (calls > 0 || ctx.Time.FrameCount > 2400) ctx.Exit();
        });
        harness.Run();

        Assert.Equal(1, calls);
        Assert.True(meshes > 0, "the triangle's mesh is under the root it was handed");
    }
}
