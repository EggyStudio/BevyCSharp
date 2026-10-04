using System.Text.Json;
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
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-save-" + Guid.NewGuid().ToString("n"));

    public SaveGameTests()
    {
        Directory.CreateDirectory(_root);
        UserData.Root = Path.Combine(_root, "user");
    }

    public void Dispose()
    {
        UserData.Root = EngineHarness.UserDirectory;
        SaveGame.Persisted.Clear();
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void ASaveHoldsWhatPlayChangedAndLoadsBackOverTheScene()
    {
        var level = Path.Combine(_root, "level.scene.json");

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
        var file = Path.Combine(_root, "user", "saves", "slot.save.json");
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

    [Fact]
    public void ALoadDuringPlayReplacesTheGameAndLeavesWhatTheGameMadeItself()
    {
        var level = Path.Combine(_root, "yard.scene.json");
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

    [Fact]
    public void ALoadIsSentTheFrameAfterOnce()
    {
        var level = Path.Combine(_root, "sent.scene.json");
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
        var level = Path.Combine(_root, "small.scene.json");
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
        var level = Path.Combine(_root, "empty.scene.json");
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
