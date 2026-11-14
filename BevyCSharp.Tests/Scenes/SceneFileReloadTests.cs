using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the scene a game loaded at the top level spawned again in place when its file's bytes
/// change while the app watches its assets, what the program spawned itself kept, the app's own
/// save and the same bytes written again spawning nothing, the reload said in the log, posted and
/// listed by <c>live.status</c>, and a program that keeps its scene as it is told the file instead.
/// </summary>
/// <remarks>
/// The file is written as a text editor writes it, while the engine runs with the asset root
/// watched, so the scene is spawned again by the watcher, as it is for a person saving. Bevy's log
/// reaches the console's ring where the run's log is started, which the suite's apps leave off, so
/// the first case starts it in a folder of its own.
/// </remarks>
[Collection("engine")]
public sealed class SceneFileReloadTests : IDisposable
{
    private readonly string _was = Streaming.AssetRoot;
    private readonly string? _asked = Environment.GetEnvironmentVariable(Config.WatchAssetsVariable);
    private readonly TestFolder _folder = new("bcs-scene-reload-");
    private readonly TestFolder _logs = new("bcs-scene-reload-logs-");

    public void Dispose()
    {
        DataAssets.Watching = false;
        SceneFile.KeptWhenChanged = null;
        Streaming.AssetRoot = _was;
        Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, _asked);
        _folder.Dispose();
        _logs.Dispose();
    }

    /// <summary>
    /// A room the game loaded, saved by the app and written again with the same bytes, is spawned
    /// again by neither, and written with its table moved and a chair added is spawned again in
    /// place, the old table gone, the program's camera left alone and the rug it laid on the table
    /// back on the new one.
    /// </summary>
    [Fact]
    public void ASceneLoadedAtTheTopIsSpawnedAgainInPlaceWhenItsBytesChangeAndNotForTheAppsOwnSave()
    {
        var room = _folder.File("room.scene.json");
        File.WriteAllText(room, Room(tableAt: 1f, chair: false));
        Environment.SetEnvironmentVariable(Config.WatchAssetsVariable, "1");

        var (oldTable, camera, rug) = (Entity.None, Entity.None, Entity.None);
        var loaded = new List<Entity>();
        var reloads = new List<(int Frame, AssetReloaded Reload)>();
        var (frame, respawned) = (0, -1);
        var (tableAt, chair, cameraKept, rugOnTable, oldGone, oldAfterSave, oldAfterSame) = (0f, false, false, false, false, false, false);
        var status = "";
        string[] said;
        CrashLog.Start(_logs.Path, GraphicsBackend.Vulkan);
        try
        {
            using (var harness = new EngineHarness(frames: 600, fps: 120))
            {
                harness.OnContext(Stage.Startup, ctx =>
                {
                    Streaming.AssetRoot = _folder.Path;
                    DataAssets.Watching = true;

                    loaded.AddRange(SceneFile.Load(ctx.Ecs, "room.scene.json").Entities);
                    oldTable = loaded.Single(entity => ctx.Ecs.NameOf(entity) == "Table");

                    camera = ctx.Ecs.Spawn();
                    ctx.Ecs.SetName(camera, "Camera");

                    rug = ctx.Ecs.Spawn();
                    ctx.Ecs.SetName(rug, "Rug");
                    ctx.Ecs.SetParent(rug, oldTable);
                });

                harness.On(Stage.Update, world =>
                {
                    var ecs = world.Resource<EcsWorld>();
                    foreach (var reload in world.Resource<MessageBus>().Read<AssetReloaded>()) reloads.Add((frame, reload));

                    switch (frame)
                    {
                        // The app's own save of the scene's entities, which changes the bytes.
                        case 5:
                            SceneFile.Save(ecs, "room.scene.json", entity => loaded.Contains(entity));
                            break;

                        // Another hand writing the same bytes again.
                        case 65:
                            oldAfterSave = ecs.IsAlive(oldTable);
                            File.WriteAllBytes(room, File.ReadAllBytes(room));
                            break;

                        case 125:
                            oldAfterSame = ecs.IsAlive(oldTable);
                            File.WriteAllText(room, Room(tableAt: 3f, chair: true));
                            break;
                    }

                    if (respawned < 0 && reloads.Any(each => each.Reload.Kind == "SceneFile")) respawned = frame;
                    if (respawned >= 0 && frame == respawned + 5)
                    {
                        var table = ecs.All().Single(entity => ecs.NameOf(entity) == "Table");
                        tableAt = ecs.GetOrDefault<Transform>(table).Translation.X;
                        chair = ecs.All().Any(entity => ecs.NameOf(entity) == "Chair");
                        cameraKept = ecs.IsAlive(camera);
                        rugOnTable = ecs.ParentOf(rug) == table;
                        oldGone = !ecs.IsAlive(oldTable);
                        using (ConsoleHost.Lend(world)) status = ConsoleCommands.Run("live.status") ?? "";
                        App.RequestExit();
                    }

                    frame++;
                });
                harness.Run();
            }

            said = [.. ConsoleLog.All().Select(line => line.Text)];
        }
        finally
        {
            CrashLog.Stop();
        }

        Assert.True(oldAfterSave, "the app's own save spawned the scene again");
        Assert.True(oldAfterSame, "the same bytes written again spawned the scene again");
        Assert.True(respawned > 125, "the room was not spawned again after it was written");
        Assert.Equal([new AssetReloaded("room.scene.json", "SceneFile")], reloads.Select(each => each.Reload).Where(each => each.Kind == "SceneFile"));
        Assert.Equal(3f, tableAt);
        Assert.True(chair);
        Assert.True(oldGone, "the old table stayed");
        Assert.True(cameraKept, "the program's camera went with the scene");
        Assert.True(rugOnTable, "the rug was not put back on the new table");
        Assert.Contains("Spawned room.scene.json again in place after its file changed, 3 entities, with 1 of the program's own put back under them.", said);
        Assert.StartsWith($"watching the assets, as the environment's {Config.WatchAssetsVariable} asks, ", status);
        Assert.Contains("room.scene.json (SceneFile) at frame ", status);
    }

    /// <summary>
    /// A program that keeps its scene as it is, as the editor keeps its document, is told the file
    /// once for a change and its scene stays.
    /// </summary>
    [Fact]
    public void AProgramKeepingItsSceneIsToldTheFileOnceAndTheSceneStays()
    {
        var room = _folder.File("room.scene.json");
        File.WriteAllText(room, Room(tableAt: 1f, chair: false));

        var told = new List<string>();
        SceneFile.KeptWhenChanged = told.Add;
        var (table, frame, stayed, posted) = (Entity.None, 0, false, false);

        using var harness = new EngineHarness(frames: 200, fps: 120);
        harness.OnContext(Stage.Startup, ctx =>
        {
            Streaming.AssetRoot = _folder.Path;
            DataAssets.Watching = true;
            table = SceneFile.Load(ctx.Ecs, "room.scene.json").Entities.Single(entity => ctx.Ecs.NameOf(entity) == "Table");
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            posted |= ctx.Read<AssetReloaded>().ToArray().Any(each => each.Kind == "SceneFile");
            if (frame++ == 5) File.WriteAllText(room, Room(tableAt: 3f, chair: true));
            stayed = ctx.Ecs.IsAlive(table) && ctx.Ecs.GetOrDefault<Transform>(table).Translation.X == 1f;
        });
        harness.Run();

        Assert.Equal(["room.scene.json"], told);
        Assert.True(stayed, "the scene was spawned again");
        Assert.False(posted, "a scene kept as it is was said read again");
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
                { "id": 2, "parent": 1, "name": "Lamp", "components": { "Bevy.Transform": { "Translation": [0, 1, 0] } } }
                {{seat}}
              ] }
            """;
    }
}
