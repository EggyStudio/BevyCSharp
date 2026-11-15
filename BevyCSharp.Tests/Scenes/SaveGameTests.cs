using System.Text.Json;
using System.Text.Json.Nodes;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A component a save game keeps, as a player's progress is.</summary>
[Behavior]
[Persist]
public partial struct Wallet
{
    /// <summary>How many coins.</summary>
    public int Coins;
}

/// <summary>A component a save game keeps that names another entity, as a guard's mark is.</summary>
[Behavior]
[Persist]
public partial struct Lookout
{
    /// <summary>The entity watched, or none.</summary>
    public Entity Watching;
}

/// <summary>
/// Covers saving a game as what play changed over the scenes it started from, and loading it back
/// over the same scenes.
/// </summary>
/// <remarks>
/// The scene is written by one engine, played and saved in a second, and loaded in a third, so
/// nothing in memory can stand in for what the files hold.
/// </remarks>
[Collection("engine")]
public sealed class SaveGameTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-save-");

    public SaveGameTests()
    {
        Directory.CreateDirectory(_folder.Path);
        UserData.Root = Path.Combine(_folder.Path, "user");
    }

    public void Dispose()
    {
        UserData.Root = EngineHarness.UserDirectory;
        SaveGame.Persisted.Clear();
        _folder.Dispose();
    }

    [Fact]
    public void ASaveHoldsWhatPlayChangedAndLoadsBackOverTheScene()
    {
        var level = Path.Combine(_folder.Path, "level.scene.json");

        using (var making = new EngineHarness(frames: 2))
        {
            making.OnContext(Stage.Startup, ctx =>
            {
                var hero = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(hero, "Hero");
                ctx.Ecs.Add(hero, Transform.Identity);
                ctx.Ecs.Add(hero, new Wallet { Coins = 1 });
                ctx.Ecs.Add(hero, SaveId.New());

                var crate = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(crate, "Crate");
                ctx.Ecs.Add(crate, Transform.Identity);
                ctx.Ecs.Add(crate, SaveId.New());

                // The scene's own, which no save touches.
                var wall = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(wall, "Wall");
                ctx.Ecs.Add(wall, Transform.At(9f, 0f, 0f));

                SceneFile.Save(ctx.Ecs, level);
            });

            making.Run();
        }

        using (var playing = new EngineHarness(frames: 2))
        {
            playing.OnContext(Stage.Startup, ctx =>
            {
                var spawned = SaveGame.Start(ctx.Ecs, level);
                var hero = spawned.Single(entity => ctx.Ecs.NameOf(entity) == "Hero");

                ctx.Ecs.GetRef<Wallet>(hero).Coins = 42;
                ctx.Ecs.GetRef<Transform>(hero).Translation = new Vec3(3f, 0f, 0f);
                ctx.Ecs.Despawn(spawned.Single(entity => ctx.Ecs.NameOf(entity) == "Crate"));
                ctx.Ecs.GetRef<Transform>(spawned.Single(entity => ctx.Ecs.NameOf(entity) == "Wall")).Translation = Vec3.Zero;

                var arrow = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(arrow, "Arrow");
                ctx.Ecs.Add(arrow, Transform.At(0f, 2f, 0f));
                ctx.Ecs.Add(arrow, new EveryKind { Count = 3, Target = hero });
                ctx.Ecs.Add(arrow, SaveId.New());

                Assert.Equal(3, SaveGame.Save(ctx.Ecs));
            });

            playing.Run();
        }

        // Under the player's directory, holding the wallet and not the walls.
        var file = Path.Combine(_folder.Path, "user", "saves", "slot.save.json");
        var text = File.ReadAllText(file);
        Assert.Contains("Bevy.Tests.Wallet", text);
        Assert.DoesNotContain("Wall\"", text);
        using (var saved = JsonDocument.Parse(text))
            Assert.Single(saved.RootElement.GetProperty("scenes").EnumerateArray());

        using var loading = new EngineHarness(frames: 2);

        loading.OnContext(Stage.Startup, ctx =>
        {
            var loaded = SaveGame.Load(ctx.Ecs);
            Assert.Empty(loaded.Unknown);

            var hero = Assert.Single(loaded.Entities, entity => ctx.Ecs.NameOf(entity) == "Hero");
            Assert.Equal(42, ctx.Ecs.GetOrDefault<Wallet>(hero).Coins);

            // The transform is not marked, so the hero stands where the scene put it.
            Assert.Equal(Vec3.Zero, ctx.Ecs.GetRef<Transform>(hero).Translation);

            Assert.DoesNotContain(loaded.Entities, entity => ctx.Ecs.NameOf(entity) == "Crate");
            var wall = Assert.Single(loaded.Entities, entity => ctx.Ecs.NameOf(entity) == "Wall");
            Assert.Equal(new Vec3(9f, 0f, 0f), ctx.Ecs.GetRef<Transform>(wall).Translation);

            // Spawned in play, written whole, and still pointing at the hero.
            var arrow = Assert.Single(loaded.Entities, entity => ctx.Ecs.NameOf(entity) == "Arrow");
            Assert.Equal(new Vec3(0f, 2f, 0f), ctx.Ecs.GetRef<Transform>(arrow).Translation);
            Assert.Equal(hero, ctx.Ecs.GetOrDefault<EveryKind>(arrow).Target);
        });

        loading.Run();
    }

    /// <summary>
    /// A scene placed twice in a level keeps each placing's saved entities apart, so play that loots
    /// one of two chests a prefab places loads back with that chest looted and the other full. Each
    /// placing gives the prefab's ids its own, mixed with its id in the level, which stays the same
    /// between runs, where the prefab's ids alone would stand for both chests.
    /// </summary>
    [Fact]
    public void AScenePlacedTwiceKeepsEachPlacingsSavedEntitiesApart()
    {
        var chest = Path.Combine(_folder.Path, "chest.scene.json");
        var level = Path.Combine(_folder.Path, "level.scene.json");

        using (var prefab = new EngineHarness(frames: 2))
        {
            prefab.OnContext(Stage.Startup, ctx =>
            {
                var full = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(full, "Chest");
                ctx.Ecs.Add(full, Transform.Identity);
                ctx.Ecs.Add(full, new Wallet { Coins = 5 });
                ctx.Ecs.Add(full, SaveId.New());
                SceneFile.Save(ctx.Ecs, chest);
            });
            prefab.Run();
        }

        using (var making = new EngineHarness(frames: 2))
        {
            making.OnContext(Stage.Startup, ctx =>
            {
                ctx.Ecs.GetRef<Transform>(SceneInstances.Spawn(ctx.Ecs, chest)).Translation = new Vec3(-5f, 0f, 0f);
                ctx.Ecs.GetRef<Transform>(SceneInstances.Spawn(ctx.Ecs, chest)).Translation = new Vec3(5f, 0f, 0f);
                SceneFile.Save(ctx.Ecs, level);
            });
            making.Run();
        }

        // The chest on one side or the other, by where its placing stands.
        static Entity Chest(EcsWorld ecs, float side) => ecs.All().Single(entity => ecs.NameOf(entity) == "Chest"
            && Math.Sign(ecs.GetRef<Transform>(ecs.ParentOf(entity)).Translation.X) == Math.Sign(side));

        using (var playing = new EngineHarness(frames: 2))
        {
            playing.OnContext(Stage.Startup, ctx =>
            {
                SaveGame.Start(ctx.Ecs, level);
                Assert.NotEqual(ctx.Ecs.GetRef<SaveId>(Chest(ctx.Ecs, -1f)), ctx.Ecs.GetRef<SaveId>(Chest(ctx.Ecs, 1f)));
                ctx.Ecs.GetRef<Wallet>(Chest(ctx.Ecs, 1f)).Coins = 0;
                SaveGame.Save(ctx.Ecs);
            });
            playing.Run();
        }

        using var loading = new EngineHarness(frames: 2);
        loading.OnContext(Stage.Startup, ctx =>
        {
            var loaded = SaveGame.Load(ctx.Ecs);
            Assert.Empty(loaded.Unknown);
            Assert.Equal(5, ctx.Ecs.GetOrDefault<Wallet>(Chest(ctx.Ecs, -1f)).Coins);
            Assert.Equal(0, ctx.Ecs.GetOrDefault<Wallet>(Chest(ctx.Ecs, 1f)).Coins);
        });
        loading.Run();
    }

    /// <summary>
    /// A reference play cleared to none is saved as none and loads back as none over the scene's,
    /// so a lookout that stopped watching the hero is not watching again after a load.
    /// </summary>
    [Fact]
    public void AReferencePlayClearedLoadsBackAsNone()
    {
        var level = Path.Combine(_folder.Path, "level.scene.json");

        using (var making = new EngineHarness(frames: 2))
        {
            making.OnContext(Stage.Startup, ctx =>
            {
                var hero = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(hero, "Hero");
                ctx.Ecs.Add(hero, SaveId.New());

                var lookout = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(lookout, "Lookout");
                ctx.Ecs.Add(lookout, new Lookout { Watching = hero });
                ctx.Ecs.Add(lookout, SaveId.New());

                SceneFile.Save(ctx.Ecs, level);
            });
            making.Run();
        }

        using (var playing = new EngineHarness(frames: 2))
        {
            playing.OnContext(Stage.Startup, ctx =>
            {
                var lookout = SaveGame.Start(ctx.Ecs, level).Single(entity => ctx.Ecs.NameOf(entity) == "Lookout");
                Assert.False(ctx.Ecs.GetRef<Lookout>(lookout).Watching.IsNone);
                ctx.Ecs.GetRef<Lookout>(lookout).Watching = Entity.None;
                Assert.Equal(1, SaveGame.Save(ctx.Ecs));
            });
            playing.Run();
        }

        using var loading = new EngineHarness(frames: 2);
        loading.OnContext(Stage.Startup, ctx =>
        {
            var lookout = Assert.Single(SaveGame.Load(ctx.Ecs).Entities, entity => ctx.Ecs.NameOf(entity) == "Lookout");
            Assert.True(ctx.Ecs.GetOrDefault<Lookout>(lookout).Watching.IsNone);
        });
        loading.Run();
    }

    [Fact]
    public void ASaveHoldsOnlyTheFieldsPlayChangedSoAFixedLevelStillReachesIt()
    {
        var level = Path.Combine(_folder.Path, "fixed.scene.json");

        using (var making = new EngineHarness(frames: 2))
        {
            making.OnContext(Stage.Startup, ctx =>
            {
                var hero = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(hero, "Hero");
                ctx.Ecs.Add(hero, Transform.Identity);
                ctx.Ecs.Add(hero, new Vigor { Most = 5f, Rear = new Coil { Stiffness = 1f, Damping = 2f } });
                ctx.Ecs.Add(hero, SaveId.New());

                foreach (var (name, coins) in new[] { ("Chest", 7), ("Door", 3) })
                {
                    var holder = ctx.Ecs.Spawn();
                    ctx.Ecs.SetName(holder, name);
                    ctx.Ecs.Add(holder, new Wallet { Coins = coins });
                    ctx.Ecs.Add(holder, SaveId.New());
                }

                SceneFile.Save(ctx.Ecs, level);
            });

            making.Run();
        }

        SaveGame.Persisted.Add("Bevy.Transform");
        SaveGame.Persisted.Add("Bevy.Tests.Vigor");

        using (var playing = new EngineHarness(frames: 2))
        {
            playing.OnContext(Stage.Startup, ctx =>
            {
                var spawned = SaveGame.Start(ctx.Ecs, level);
                Entity Named(string name) => spawned.Single(entity => ctx.Ecs.NameOf(entity) == name);

                ctx.Ecs.GetRef<Transform>(Named("Hero")).Translation = new Vec3(3f, 0f, 0f);
                ctx.Ecs.GetRef<Vigor>(Named("Hero")).Rear.Damping = 4f;
                ctx.Ecs.Remove<Wallet>(Named("Chest"));

                // The hero and the emptied chest, and not the door, which play left alone.
                Assert.Equal(2, SaveGame.Save(ctx.Ecs));
            });

            playing.Run();
        }

        var file = Path.Combine(_folder.Path, "user", "saves", "slot.save.json");
        using (var saved = JsonDocument.Parse(File.ReadAllText(file)))
        {
            var components = saved.RootElement.GetProperty("entities").EnumerateArray()
                .Select(entry => entry.GetProperty("components"))
                .ToList();
            Assert.Equal(2, components.Count);

            // Each component with the one field that moved, down into the struct it lies in.
            var hero = Assert.Single(components, entry => entry.TryGetProperty("Bevy.Tests.Vigor", out _));
            Assert.Equal(["Translation"], hero.GetProperty("Bevy.Transform").EnumerateObject().Select(property => property.Name));
            var vigor = hero.GetProperty("Bevy.Tests.Vigor");
            Assert.Equal(["Rear"], vigor.EnumerateObject().Select(property => property.Name));
            Assert.Equal(["Damping"], vigor.GetProperty("Rear").EnumerateObject().Select(property => property.Name));

            var chest = Assert.Single(components, entry => entry.TryGetProperty("Bevy.Tests.Wallet", out _));
            Assert.Equal(JsonValueKind.Null, chest.GetProperty("Bevy.Tests.Wallet").ValueKind);
        }

        // The level fixed in an update, in two fields play never changed beside one it did.
        var scene = JsonNode.Parse(File.ReadAllText(level))!;
        var held = scene["entities"]!.AsArray().Single(entry => (string?)entry!["name"] == "Hero")!["components"]!["Bevy.Tests.Vigor"]!;
        held["Most"] = 8f;
        held["Rear"]!["Stiffness"] = 6f;
        held["Rear"]!["Damping"] = 9f;
        File.WriteAllText(level, scene.ToJsonString());

        using var loading = new EngineHarness(frames: 2);

        loading.OnContext(Stage.Startup, ctx =>
        {
            var loaded = SaveGame.Load(ctx.Ecs);
            Assert.Null(loaded.Problem);
            Entity Named(string name) => loaded.Entities.Single(entity => ctx.Ecs.NameOf(entity) == name);

            // The fix where play left the field, and the player's own where it did not.
            var vigor = ctx.Ecs.GetOrDefault<Vigor>(Named("Hero"));
            Assert.Equal(8f, vigor.Most);
            Assert.Equal(6f, vigor.Rear.Stiffness);
            Assert.Equal(4f, vigor.Rear.Damping);
            Assert.Equal(new Vec3(3f, 0f, 0f), ctx.Ecs.GetRef<Transform>(Named("Hero")).Translation);

            Assert.False(ctx.Ecs.Has<Wallet>(Named("Chest")));
            Assert.Equal(3, ctx.Ecs.GetOrDefault<Wallet>(Named("Door")).Coins);
        });

        loading.Run();
    }

    [Fact]
    public void ALoadDuringPlayReplacesTheGameAndLeavesWhatTheGameMadeItself()
    {
        var level = Path.Combine(_folder.Path, "yard.scene.json");
        File.WriteAllText(level, """
            { "format": "bevycsharp.scene.2",
              "entities": [
                { "id": 1, "name": "Hero",
                  "components": { "Bevy.Transform": { "Translation": [0, 0, 0] }, "Bevy.SaveId": { "Id": "00000000000000cd" } } },
                { "id": 2, "name": "Wall", "components": { "Bevy.Transform": { "Translation": [9, 0, 0] } } } ] }
            """);

        using var harness = new EngineHarness(frames: 2);
        var ran = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            if (ran) return;
            ran = true;

            var hero = SaveGame.Start(ctx.Ecs, level).Single(entity => ctx.Ecs.NameOf(entity) == "Hero");
            ctx.Ecs.Add(hero, new Wallet { Coins = 1 });
            SaveGame.Save(ctx.Ecs);

            // Played on past the save, with a camera the game made for itself and an arrow it
            // spawned with an id, which the save never saw.
            ctx.Ecs.GetRef<Wallet>(hero).Coins = 5;
            var camera = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(camera, "Camera");
            var arrow = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(arrow, "Arrow");
            ctx.Ecs.Add(arrow, SaveId.New());

            SaveGame.Load(ctx.Ecs);

            var named = ctx.Ecs.All().Select(ctx.Ecs.NameOf).ToList();
            Assert.Single(named, name => name == "Hero");
            Assert.Single(named, name => name == "Wall");
            Assert.DoesNotContain("Arrow", named);
            Assert.True(ctx.Ecs.IsAlive(camera));

            var loaded = ctx.Ecs.All().Single(entity => ctx.Ecs.NameOf(entity) == "Hero");
            Assert.Equal(1, ctx.Ecs.GetOrDefault<Wallet>(loaded).Coins);

            // And a second load ends what the first one loaded.
            SaveGame.Load(ctx.Ecs);
            Assert.Single(ctx.Ecs.All(), entity => ctx.Ecs.NameOf(entity) == "Wall");
        });

        harness.Run();
        Assert.True(ran);
    }

    /// <summary>
    /// A scene the game loaded itself and did not start the game from, as its interface's, stays
    /// through a load, which ends only what the game was started from, where every entity with a
    /// scene's id was taken for the game's and despawned.
    /// </summary>
    [Fact]
    public void ASceneTheGameLoadedItselfStaysThroughALoad()
    {
        var hud = Path.Combine(_folder.Path, "hud.scene.json");
        File.WriteAllText(hud, """
            { "format": "bevycsharp.scene.2",
              "entities": [ { "id": 1, "name": "Health bar", "components": { "Bevy.Transform": { "Translation": [0, 0, 0] } } } ] }
            """);
        var level = Path.Combine(_folder.Path, "keep.scene.json");
        File.WriteAllText(level, """
            { "format": "bevycsharp.scene.2",
              "entities": [ { "id": 1, "name": "Hero",
                "components": { "Bevy.Transform": { "Translation": [0, 0, 0] }, "Bevy.SaveId": { "Id": "00000000000000a1" } } } ] }
            """);

        using var harness = new EngineHarness(frames: 2);
        var ran = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            if (ran) return;
            ran = true;

            var bar = Assert.Single(SceneFile.Load(ctx.Ecs, hud).Entities);
            SaveGame.Start(ctx.Ecs, level);
            SaveGame.Save(ctx.Ecs);

            Assert.Null(SaveGame.Load(ctx.Ecs).Problem);
            Assert.True(ctx.Ecs.IsAlive(bar));
            Assert.Single(ctx.Ecs.All(), entity => ctx.Ecs.NameOf(entity) == "Hero");
        });

        harness.Run();
        Assert.True(ran);
    }

    /// <summary>
    /// A scene the save names that cannot be read is named in what the load returns, with what the
    /// other scene could not read, and the save's entries for its entities are kept, so a save made
    /// meanwhile writes them back and the player's progress there comes back once the scene is
    /// read again, where the load said nothing and the next save dropped that progress.
    /// </summary>
    [Fact]
    public void ASceneTheSaveNamesThatCannotBeReadIsSaidAndItsProgressKept()
    {
        var yard = Path.Combine(_folder.Path, "yard.scene.json");
        File.WriteAllText(yard, """
            { "format": "bevycsharp.scene.2",
              "entities": [ { "id": 1, "name": "Hero",
                "components": { "Bevy.Transform": { "Translation": [0, 0, 0] }, "Game.Gone": { "Value": 1 },
                  "Bevy.SaveId": { "Id": "00000000000000b1" } } } ] }
            """);
        var vault = Path.Combine(_folder.Path, "vault.scene.json");
        var whole = """
            { "format": "bevycsharp.scene.2",
              "entities": [ { "id": 1, "name": "Chest",
                "components": { "Bevy.Tests.Wallet": { "Coins": 5 }, "Bevy.SaveId": { "Id": "00000000000000b2" } } } ] }
            """;
        File.WriteAllText(vault, whole);

        using (var playing = new EngineHarness(frames: 2))
        {
            playing.OnContext(Stage.Startup, ctx =>
            {
                var chest = SaveGame.Start(ctx.Ecs, yard, vault).Single(entity => ctx.Ecs.NameOf(entity) == "Chest");
                ctx.Ecs.GetRef<Wallet>(chest).Coins = 0;
                SaveGame.Save(ctx.Ecs);
            });
            playing.Run();
        }

        // The vault cut short by an update, loaded and saved again while it is.
        File.WriteAllText(vault, whole[..40]);
        using (var broken = new EngineHarness(frames: 2))
        {
            broken.OnContext(Stage.Startup, ctx =>
            {
                var loaded = SaveGame.Load(ctx.Ecs);
                Assert.Contains("vault.scene.json", loaded.Problem);
                Assert.Contains("Game.Gone", loaded.Unknown);
                Assert.Single(loaded.Entities, entity => ctx.Ecs.NameOf(entity) == "Hero");
                SaveGame.Save(ctx.Ecs);
            });
            broken.Run();
        }

        File.WriteAllText(vault, whole);
        using var mended = new EngineHarness(frames: 2);
        mended.OnContext(Stage.Startup, ctx =>
        {
            var loaded = SaveGame.Load(ctx.Ecs);
            Assert.Null(loaded.Problem);
            var chest = Assert.Single(loaded.Entities, entity => ctx.Ecs.NameOf(entity) == "Chest");
            Assert.Equal(0, ctx.Ecs.GetOrDefault<Wallet>(chest).Coins);
        });
        mended.Run();
    }

    [Fact]
    public void ALoadIsSentTheFrameAfterOnce()
    {
        var level = Path.Combine(_folder.Path, "sent.scene.json");
        File.WriteAllText(level, """
            { "format": "bevycsharp.scene.2",
              "entities": [ { "id": 1, "name": "Hero",
                "components": { "Bevy.Transform": { "Translation": [0, 0, 0] }, "Bevy.SaveId": { "Id": "00000000000000ef" } } } ] }
            """);

        using var harness = new EngineHarness(frames: 8);
        var frame = 0;
        var loadedOn = 0;
        var heard = new List<(int Frame, SaveLoaded Message)>();

        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            foreach (var message in ctx.Read<SaveLoaded>()) heard.Add((frame, message));

            if (frame == 1)
            {
                SaveGame.Start(ctx.Ecs, level);
                SaveGame.Save(ctx.Ecs, "user://saves/sent.save.json");
            }
            else if (frame == 3)
            {
                SaveGame.Load(ctx.Ecs, "user://saves/sent.save.json");
                loadedOn = frame;
            }
        });

        harness.Run();

        var (when, loaded) = Assert.Single(heard);
        Assert.Equal(loadedOn + 1, when);
        Assert.Equal("user://saves/sent.save.json", loaded.Path);
        Assert.Contains(loaded.Entities, entity => entity != Entity.None);
    }

    [Fact]
    public void AComponentDeclaredElsewhereIsSavedOnceNamed()
    {
        var level = Path.Combine(_folder.Path, "small.scene.json");
        File.WriteAllText(level, """
            { "format": "bevycsharp.scene.2",
              "entities": [ { "id": 1, "name": "Hero",
                "components": { "Bevy.Transform": { "Translation": [0, 0, 0] }, "Bevy.SaveId": { "Id": "00000000000000ab" } } } ] }
            """);

        SaveGame.Persisted.Add("Bevy.Transform");

        using (var playing = new EngineHarness(frames: 2))
        {
            playing.OnContext(Stage.Startup, ctx =>
            {
                var hero = Assert.Single(SaveGame.Start(ctx.Ecs, level));
                Assert.Equal(0xabUL, ctx.Ecs.GetRef<SaveId>(hero).Value);

                ctx.Ecs.GetRef<Transform>(hero).Translation = new Vec3(5f, 0f, 0f);
                SaveGame.Save(ctx.Ecs, "user://saves/two.save.json");
            });

            playing.Run();
        }

        using var loading = new EngineHarness(frames: 2);

        loading.OnContext(Stage.Startup, ctx =>
        {
            var hero = Assert.Single(SaveGame.Load(ctx.Ecs, "user://saves/two.save.json").Entities);
            Assert.Equal(new Vec3(5f, 0f, 0f), ctx.Ecs.GetRef<Transform>(hero).Translation);
        });

        loading.Run();
    }

    [Fact]
    public void AValueTheSlotCarriesComesBackWithTheSave()
    {
        var level = Path.Combine(_folder.Path, "empty.scene.json");
        File.WriteAllText(level, """{ "format": "bevycsharp.scene.2", "entities": [] }""");

        var quests = new Persistent<Settings>("quests", SettingsJson.Default.Settings, () => new Settings());
        SaveGame.Carry(quests);

        try
        {
            using (var playing = new EngineHarness(frames: 2))
            {
                playing.OnContext(Stage.Startup, ctx =>
                {
                    SaveGame.Start(ctx.Ecs, level);
                    quests.Set(new Settings(Language: "chapter-two"));
                    SaveGame.Save(ctx.Ecs, "user://saves/three.save.json");

                    // Played on past the save.
                    quests.Set(new Settings(Language: "chapter-three"));
                });

                playing.Run();
            }

            using var loading = new EngineHarness(frames: 2);
            loading.OnContext(Stage.Startup, ctx =>
            {
                var loaded = SaveGame.Load(ctx.Ecs, "user://saves/three.save.json");
                Assert.Empty(loaded.Refused);
            });

            loading.Run();

            // Back with the save, and not written to its own file, which it has none of.
            Assert.Equal("chapter-two", quests.Value.Language);
            Assert.False(File.Exists(quests.FullPath));
        }
        finally
        {
            SaveGame.Drop(quests);
        }
    }
}
