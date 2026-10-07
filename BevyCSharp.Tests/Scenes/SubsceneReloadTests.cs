using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a scene file placed in a level spawned again when it is written while the level runs,
/// under the entity that placed it, its old copy gone, the level's overrides applied again and
/// what the level added under its nodes kept.
/// </summary>
/// <remarks>
/// The file is written as an editor or a text editor writes it, while the engine runs with the
/// asset root watched, as 3DEngine's test of the same writes its model (its <c>5b2234d2</c>).
/// </remarks>
[Collection("engine")]
public sealed class SubsceneReloadTests : IDisposable
{
    private readonly string _was = Streaming.AssetRoot;
    private readonly TestFolder _folder = new("bcs-reload-");

    public void Dispose()
    {
        DataAssets.Watching = false;
        Streaming.AssetRoot = _was;
        _folder.Dispose();
    }

    /// <summary>
    /// A room written again with its table moved and a chair added is spawned again under the same
    /// root, the old table gone, the lamp's override on it again and the rug the level laid on the
    /// table back under the new one.
    /// </summary>
    [Fact]
    public void APlacedSceneFileWrittenWhileTheLevelRunsIsSpawnedAgainUnderItsRoot()
    {
        var room = _folder.File("room.scene.json");
        File.WriteAllText(room, Room(tableAt: 1f, chair: false));

        var (placed, oldTable, rug) = (Entity.None, Entity.None, Entity.None);
        var (tableAt, chair, lampMass, rugOnTable, oldGone, ready) = (0f, false, 0f, false, false, false);
        var entities = (Before: 0, After: 0);
        var written = false;

        using var harness = new EngineHarness(frames: 240, fps: 120);
        harness.OnContext(Stage.Startup, ctx =>
        {
            Streaming.AssetRoot = _folder.Path;
            DataAssets.Watching = true;

            placed = SceneInstances.Spawn(ctx.Ecs, "room.scene.json");
            oldTable = SceneInstances.Find(ctx.Ecs, placed, "Table");
            Assert.True(SceneInstances.Set(ctx.Ecs, SceneInstances.Find(ctx.Ecs, placed, "Table/Lamp"), "Bevy.Tests.Sprung", "Mass", 5f));

            rug = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(rug, "Rug");
            ctx.Ecs.Add(rug, Transform.At(0f, 0.1f, 0f));
            ctx.Ecs.SetParent(rug, oldTable);
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            if (!written && ctx.Time.FrameCount > 5)
            {
                entities.Before = ctx.Ecs.All().Count();
                File.WriteAllText(room, Room(tableAt: 3f, chair: true));
                written = true;
            }

            if (written && ctx.Read<WorldInstanceReady>().ToArray().Any(message => message.Entity == placed))
            {
                ready = true;
                var table = SceneInstances.Find(ctx.Ecs, placed, "Table");
                tableAt = ctx.Ecs.GetOrDefault<Transform>(table).Translation.X;
                chair = !SceneInstances.Find(ctx.Ecs, placed, "Chair").IsNone;
                lampMass = ctx.Ecs.GetOrDefault<Sprung>(SceneInstances.Find(ctx.Ecs, placed, "Table/Lamp")).Mass;
                rugOnTable = ctx.Ecs.ParentOf(rug) == table;
                oldGone = !ctx.Ecs.IsAlive(oldTable);
                entities.After = ctx.Ecs.All().Count();
                ctx.Exit();
            }
        });

        harness.Run();

        Assert.True(ready, "the room was not spawned again after it was written");
        Assert.Equal(3f, tableAt);
        Assert.True(chair);
        Assert.Equal(5f, lampMass);
        Assert.True(oldGone, "the old table stayed");
        Assert.True(rugOnTable, "the rug was not put back on the new table");

        // The new copy holds one more entity, the chair, and the old copy's entities are gone.
        Assert.Equal(entities.Before + 1, entities.After);
    }

    // A room of a table with a lamp on it, and a chair where asked for.
    private static string Room(float tableAt, bool chair)
    {
        var at = tableAt.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var seat = chair ? """, { "id": 3, "name": "Chair", "components": { "Bevy.Transform": { "Translation": [0, 0, 2] } } }""" : "";
        return $$"""
            { "format": "bevycsharp.scene.2",
              "entities": [
                { "id": 1, "name": "Table", "components": { "Bevy.Transform": { "Translation": [{{at}}, 0, 0] } } },
                { "id": 2, "parent": 1, "name": "Lamp", "components": { "Bevy.Transform": { "Translation": [0, 1, 0] }, "Bevy.Tests.Sprung": { "Mass": 1 } } }
                {{seat}}
              ] }
            """;
    }
}
