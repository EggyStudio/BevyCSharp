using System.Buffers;
using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the meshes and materials a scene load makes going with the last entity drawn with them,
/// so a level loaded again does not hold every earlier load's.
/// </summary>
/// <remarks>
/// <c>build/soak.sh</c> found Courtyard holding a level's look more for each save it loaded, every
/// load making the meshes and materials again under handles nothing released. The scene here is
/// written from an entity made in the test, a cube in one material, and read twice, the first
/// load's entity despawned before the second, as a save loaded over a level does.
/// </remarks>
[Collection("engine")]
public sealed class SceneAssetsReleaseTests
{
    [SkippableFact]
    public void ALoadsMeshAndMaterialGoOnceItsEntitiesHaveAndTheNextLoadsStay()
    {
        Needs.Renderer();

        var (heldAfterFirst, heldAtEnd) = (0, 0);
        var (first, second) = (AssetHandle.None, AssetHandle.None);
        var (firstAlive, secondAlive, firstRecipe, secondRecipe) = (true, false, true, false);

        Run(frame: (world, frame, scene) =>
        {
            switch (frame)
            {
                case 0:
                    var loaded = SceneFile.Read(world, scene.RootElement).Entities;
                    first = Render.MeshOf(world, loaded[0]);
                    heldAfterFirst = AssetServer.LiveHandleCount;
                    break;
                case 1:
                    foreach (var entity in world.All().Where(e => world.Has<SceneId>(e)).ToList()) world.Despawn(entity);
                    second = Render.MeshOf(world, SceneFile.Read(world, scene.RootElement).Entities[0]);
                    break;
                case 6:
                    heldAtEnd = AssetServer.LiveHandleCount;
                    (firstAlive, secondAlive) = (AssetServer.IsAlive(first), AssetServer.IsAlive(second));
                    (firstRecipe, secondRecipe) = (Render.RecipeOf(first) is not null, Render.RecipeOf(second) is not null);
                    break;
            }
        });

        Assert.Equal(heldAfterFirst, heldAtEnd);
        Assert.False(firstAlive, "the first load's mesh is still held");
        Assert.True(secondAlive, "the second load's mesh went while its entity drew with it");
        Assert.False(firstRecipe, "the first load's recipe is still kept");
        Assert.True(secondRecipe);
    }

    [SkippableFact]
    public void ALoadKeepsWhatItMadeWhenScenesAreToldTo()
    {
        Needs.Renderer();

        var first = AssetHandle.None;
        var alive = false;

        SceneFile.KeepsAssets = true;
        try
        {
            Run(frame: (world, frame, scene) =>
            {
                if (frame == 0)
                {
                    var entity = SceneFile.Read(world, scene.RootElement).Entities[0];
                    first = Render.MeshOf(world, entity);
                    world.Despawn(entity);
                }

                if (frame == 6) alive = AssetServer.IsAlive(first);
            });
        }
        finally
        {
            SceneFile.KeepsAssets = false;
        }

        Assert.True(alive, "a kept mesh went with its entity");
    }

    /// <summary>
    /// Runs an app for eight frames, writing a scene of one cube in one material at the start and
    /// calling <paramref name="frame"/> on each frame after with the scene and the frame's number.
    /// </summary>
    private static void Run(Action<EcsWorld, int, JsonDocument> frame)
    {
        using var harness = new EngineHarness(frames: 9);
        JsonDocument? scene = null;
        var count = -1;

        harness.On(Stage.Update, world =>
        {
            var ecs = world.Resource<EcsWorld>();
            if (scene is null)
            {
                scene = Written(ecs);
                return;
            }

            frame(ecs, ++count, scene);
        });

        harness.Run();
        scene?.Dispose();
    }

    /// <summary>
    /// A scene of a cube in one material, written from an entity made for it, which is then gone,
    /// its handles released.
    /// </summary>
    private static JsonDocument Written(EcsWorld world)
    {
        var mesh = Render.CreateMesh(MeshShape.Cuboid);
        var material = Render.CreateMaterial((0.2f, 0.6f, 0.9f, 1f));
        var cube = world.Spawn();
        world.Add(cube, Transform.Identity);
        Render.SetMesh(world, cube, mesh);
        Render.SetMaterial(world, cube, material);

        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer)) SceneFile.Write(world, json, entity => entity == cube);

        world.Despawn(cube);
        AssetServer.Release(mesh);
        AssetServer.Release(material);
        return JsonDocument.Parse(buffer.WrittenMemory);
    }
}
