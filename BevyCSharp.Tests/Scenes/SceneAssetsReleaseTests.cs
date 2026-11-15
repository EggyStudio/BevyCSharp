using System.Buffers;
using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A component holding a picture, as a sign's face is.</summary>
[Behavior]
public partial struct Signboard
{
    /// <summary>The picture.</summary>
    [Asset(AssetKind.Image)]
    public AssetHandle Face;
}

/// <summary>
/// Covers the meshes and materials a scene load makes going with the last entity drawn with them,
/// and the assets its components' fields name going once no field holds them, so a level loaded
/// again does not hold every earlier load's.
/// </summary>
/// <remarks>
/// A game loading saves over a level would hold a level's look more for each save it loads, every
/// load making the meshes and materials again under handles nothing released. The scene here is
/// written from an entity made in the test, a cube in one material, and read twice, the first
/// load's entity despawned before the second, as a save loaded over a level does.
/// </remarks>
[Collection("engine")]
public sealed class SceneAssetsReleaseTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-field-assets-");

    public void Dispose() => _folder.Dispose();

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

    [Fact]
    public void ALevelsFieldsShareOneHandleForAnAssetThatGoesOnceNoFieldHoldsIt()
    {
        var level = _folder.File("level.scene.json");
        var other = _folder.File("other.scene.json");
        var held = new List<int>();
        var (checkerAlive, signAlive) = (true, false);

        using var harness = new EngineHarness(frames: 2);
        harness.OnContext(Stage.Startup, ctx =>
        {
            var world = ctx.Ecs;
            Signs(world, level, "textures/checker.png", "textures/checker.png");
            Signs(world, other, "textures/cubemap.png");
            var before = AssetServer.LiveHandleCount;

            // The level twice, the first load's signs gone before the second, as a save loaded
            // over a level takes them.
            var first = SceneFile.Load(world, level).Entities;
            held.Add(AssetServer.LiveHandleCount - before);
            foreach (var entity in first) world.Despawn(entity);

            var second = SceneFile.Load(world, level).Entities;
            held.Add(AssetServer.LiveHandleCount - before);
            var checker = world.GetRef<Signboard>(second[0]).Face;
            foreach (var entity in second) world.Despawn(entity);

            // Then another scene once the level has gone, whose sign names another picture.
            var sign = world.GetRef<Signboard>(SceneFile.Load(world, other).Entities[0]).Face;
            held.Add(AssetServer.LiveHandleCount - before);
            (checkerAlive, signAlive) = (AssetServer.IsAlive(checker), AssetServer.IsAlive(sign));
        });

        harness.Run();

        Assert.Equal([1, 1, 1], held);
        Assert.False(checkerAlive, "the level's picture is still held once the level has gone");
        Assert.True(signAlive, "the other scene's picture went while its sign held it");
    }

    /// <summary>
    /// Writes a scene of a sign for each picture named, each holding the picture in a field, and
    /// takes the signs and their handles away again.
    /// </summary>
    private static void Signs(EcsWorld world, string path, params string[] pictures)
    {
        var signs = new List<Entity>();
        foreach (var picture in pictures)
        {
            var sign = world.Spawn();
            world.Add(sign, new Signboard { Face = AssetServer.Load(AssetKind.Image, picture) });
            signs.Add(sign);
        }

        SceneFile.Save(world, path, signs.Contains);

        foreach (var sign in signs)
        {
            AssetServer.Release(world.GetRef<Signboard>(sign).Face);
            world.Despawn(sign);
        }
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
