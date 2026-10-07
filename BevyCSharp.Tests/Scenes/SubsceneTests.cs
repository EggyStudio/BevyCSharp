using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a scene file placed in another as an instance: read under its root with its overrides
/// applied in the same frame, nested instances reached by a longer path, and a scene that would
/// contain itself refused.
/// </summary>
/// <remarks>
/// A subscene is read on the managed side, so most of these run on a headless bridge. The one with
/// a glTF scene nested inside is skipped there, as the glTF tests are.
/// </remarks>
[Collection("engine")]
public sealed class SubsceneTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-subscene-");

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void ASubsceneIsReadUnderItsRootAndItsOverridesAppliedAtOnce()
    {
        var room = _folder.File("room.scene.json");
        var level = _folder.File("level.scene.json");

        using (var making = new EngineHarness(frames: 2))
        {
            making.OnContext(Stage.Startup, ctx =>
            {
                var table = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(table, "Table");
                ctx.Ecs.Add(table, Transform.At(1f, 0f, 0f));

                var lamp = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(lamp, "Lamp");
                ctx.Ecs.Add(lamp, Transform.At(0f, 1f, 0f));
                ctx.Ecs.Add(lamp, new Sprung { Mass = 1f });
                ctx.Ecs.SetParent(lamp, table);

                SceneFile.Save(ctx.Ecs, room);
            });

            making.Run();
        }

        using (var placing = new EngineHarness(frames: 2))
        {
            placing.OnContext(Stage.Startup, ctx =>
            {
                var placed = SceneInstances.Spawn(ctx.Ecs, room);
                ctx.Ecs.SetName(placed, "Room");

                // There at once, since the file is read here rather than by Bevy.
                var lamp = SceneInstances.Find(ctx.Ecs, placed, "Table/Lamp");
                Assert.False(lamp.IsNone);
                Assert.True(SceneInstances.Set(ctx.Ecs, lamp, "Bevy.Tests.Sprung", "Mass", 5f));

                SceneFile.Save(ctx.Ecs, level);
            });

            placing.Run();
        }

        // The room by reference with the one change, and none of its entities.
        using (var saved = JsonDocument.Parse(File.ReadAllText(level)))
        {
            var entry = Assert.Single(saved.RootElement.GetProperty("entities").EnumerateArray());
            Assert.Equal("Room", entry.GetProperty("name").GetString());
            Assert.Single(entry.GetProperty("overrides").EnumerateArray());
        }

        using var loading = new EngineHarness(frames: 4);

        var root = Entity.None;
        var mass = 0f;
        var announced = false;

        loading.OnContext(Stage.Startup, ctx =>
        {
            root = Assert.Single(SceneFile.Load(ctx.Ecs, level).Entities);
            mass = ctx.Ecs.GetOrDefault<Sprung>(SceneInstances.Find(ctx.Ecs, root, "Table/Lamp")).Mass;
        });

        loading.OnContext(Stage.Update, ctx =>
        {
            if (ctx.Read<WorldInstanceReady>().ToArray().Any(message => message.Entity == root)) announced = true;
        });

        loading.Run();

        Assert.Equal(5f, mass);
        Assert.True(announced, "the subscene was never announced as ready");
    }

    [Fact]
    public void ASceneThatWouldContainItselfIsRefused()
    {
        var loop = _folder.File("loop.scene.json");
        File.WriteAllText(loop, $$"""
            { "format": "bevycsharp.scene.2",
              "entities": [ { "id": 1, "name": "Again", "instance": { "path": {{JsonSerializer.Serialize(loop)}} } } ] }
            """);

        var room = _folder.File("room.scene.json");
        File.WriteAllText(room, """
            { "format": "bevycsharp.scene.2",
              "entities": [ { "id": 1, "name": "Chair", "components": { "Bevy.Transform": { "Translation": [0, 0, 0] } } } ] }
            """);

        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            // Answered by the outermost read, with nothing of it left, the instances it placed
            // before it found itself taken back with it.
            var loaded = SceneFile.Load(ctx.Ecs, loop);
            Assert.Empty(loaded.Entities);
            Assert.Contains("contains an instance of itself", loaded.Problem);
            Assert.DoesNotContain(ctx.Ecs.All(), entity => ctx.Ecs.NameOf(entity) == "Again");

            // Placed by the game, the instance is left holding nothing, as one whose file is
            // missing.
            var placed = SceneInstances.Spawn(ctx.Ecs, loop);
            Assert.Empty(ctx.Ecs.ChildrenOf(placed));

            // A room placed in the world cannot be saved as the room, which would place itself.
            SceneInstances.Spawn(ctx.Ecs, room);
            Assert.Throws<InvalidOperationException>(() => SceneFile.Save(ctx.Ecs, room));
        });

        harness.Run();
    }

    [SkippableFact]
    public void AnOverrideReachesIntoAModelNestedInASubscene()
    {
        var dock = _folder.File("dock.scene.json");
        File.WriteAllText(dock, """
            { "format": "bevycsharp.scene.2",
              "entities": [ { "id": 1, "name": "Ship", "instance": { "path": "models/rig.gltf" },
                              "components": { "Bevy.Transform": { "Translation": [0, 0, 0] } } } ] }
            """);

        var harbor = _folder.File("harbor.scene.json");
        File.WriteAllText(harbor, $$"""
            { "format": "bevycsharp.scene.2",
              "entities": [ { "id": 1, "name": "Dock", "instance": { "path": {{JsonSerializer.Serialize(dock)}} },
                              "overrides": [ { "at": "Ship/Main/Hull/Turret",
                                               "set": { "Bevy.Transform": { "Translation": [0, 4, 0] } } } ],
                              "components": { "Bevy.Transform": { "Translation": [0, 0, 0] } } } ] }
            """);

        using var harness = new EngineHarness(frames: 300, fps: 240);
        Needs.Renderer();

        var root = Entity.None;
        var turret = Vec3.Zero;
        IReadOnlyList<string> missed = ["not read"];

        harness.OnContext(Stage.Startup, ctx => root = Assert.Single(SceneFile.Load(ctx.Ecs, harbor).Entities));

        harness.OnContext(Stage.Update, ctx =>
        {
            // The ship inside the dock, which Bevy spawns once the model has loaded.
            var ship = SceneInstances.Find(ctx.Ecs, root, "Ship");
            if (!ctx.Read<WorldInstanceReady>().ToArray().Any(message => message.Entity == ship)) return;

            turret = ctx.Ecs.GetRef<Transform>(SceneInstances.Find(ctx.Ecs, root, "Ship/Main/Hull/Turret")).Translation;
            missed = SceneInstances.Missed(ctx.Ecs, root);
            ctx.Exit();
        });

        harness.Run();

        Assert.Equal(new Vec3(0f, 4f, 0f), turret);
        Assert.Empty(missed);
    }

    [Fact]
    public void AChildAddedUnderANodeIsWrittenWithTheSceneAndPutBackUnderIt()
    {
        var room = _folder.File("room.scene.json");
        File.WriteAllText(room, """
            { "format": "bevycsharp.scene.2",
              "entities": [ { "id": 1, "name": "Table", "components": { "Bevy.Transform": { "Translation": [1, 0, 0] } } } ] }
            """);
        var level = _folder.File("furnished.scene.json");

        using (var placing = new EngineHarness(frames: 2))
        {
            placing.OnContext(Stage.Startup, ctx =>
            {
                var placed = SceneInstances.Spawn(ctx.Ecs, room);
                ctx.Ecs.SetName(placed, "Room");

                var rug = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(rug, "Rug");
                ctx.Ecs.Add(rug, Transform.At(0f, 0.1f, 0f));
                ctx.Ecs.SetParent(rug, SceneInstances.Find(ctx.Ecs, placed, "Table"));

                // The scene's own, so not an override of the room and free to be renamed.
                Assert.False(SceneInstances.IsFromModel(ctx.Ecs, rug));
                Assert.True(SceneInstances.IsFromModel(ctx.Ecs, SceneInstances.Find(ctx.Ecs, placed, "Table")));

                SceneFile.Save(ctx.Ecs, level);
            });

            placing.Run();
        }

        // The rug is an entity of the level, with the room's override naming where it goes.
        using (var saved = JsonDocument.Parse(File.ReadAllText(level)))
        {
            var entities = saved.RootElement.GetProperty("entities").EnumerateArray().ToArray();
            Assert.Equal(2, entities.Length);
            var placed = entities.Single(entry => entry.GetProperty("name").GetString() == "Room");
            var change = Assert.Single(placed.GetProperty("overrides").EnumerateArray());
            Assert.Equal("Table", change.GetProperty("at").GetString());
        }

        using var loading = new EngineHarness(frames: 2);

        loading.OnContext(Stage.Startup, ctx =>
        {
            var loaded = SceneFile.Load(ctx.Ecs, level);
            var placed = Assert.Single(loaded.Entities, entity => ctx.Ecs.NameOf(entity) == "Room");

            var rug = SceneInstances.Find(ctx.Ecs, placed, "Table/Rug");
            Assert.False(rug.IsNone);
            Assert.Equal(new Vec3(0f, 0.1f, 0f), ctx.Ecs.GetRef<Transform>(rug).Translation);
        });

        loading.Run();
    }
}
