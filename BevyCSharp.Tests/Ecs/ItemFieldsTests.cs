using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>One line of a loot table, a class with a list of its own.</summary>
public sealed class LootEntry
{
    /// <summary>What drops.</summary>
    public string Item = "Coin";

    /// <summary>How likely, against the others.</summary>
    [Range(0, 100)]
    public float Weight = 1f;

    /// <summary>Words a search finds it by.</summary>
    public List<string> Tags = [];
}

/// <summary>A data asset holding a list of classes.</summary>
[DataAsset]
public sealed class LootTable
{
    /// <summary>The lines.</summary>
    public List<LootEntry> Entries = [];
}

/// <summary>A point on a path, a struct with fields of its own.</summary>
public struct Waypoint
{
    /// <summary>Where.</summary>
    public Vec3 At;

    /// <summary>How long to stay.</summary>
    public float Wait;
}

/// <summary>A component holding a list of structs inline.</summary>
[Behavior]
public partial struct Patrol
{
    /// <summary>The points, in order.</summary>
    public InlineList8<Waypoint> Points;
}

/// <summary>
/// Covers lists whose items have fields of their own: described a field at a time, edited through
/// a copy, and written and read as objects.
/// </summary>
[Collection("engine")]
public sealed class ItemFieldsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-items-" + Guid.NewGuid().ToString("n"));
    private readonly string _was = Streaming.AssetRoot;

    public ItemFieldsTests()
    {
        Directory.CreateDirectory(_root);
        Streaming.AssetRoot = _root;
        DataAssets.ReloadAll();
        AssetIds.Reindex();
    }

    public void Dispose()
    {
        Streaming.AssetRoot = _was;
        DataAssets.ReloadAll();
        AssetIds.Reindex();
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void AListOfClassesIsEditedThroughCopiesAndSavedAsObjects()
    {
        var table = DataAssets.Create<LootTable>("loot.data.json");
        var schema = DataAssets.SchemaOf(table.Id)!;
        var entries = schema.Field("Entries")!;

        Assert.Equal(FieldKind.Struct, entries.ElementKind);
        var fields = Assert.IsType<ItemFields>(entries.Items);
        Assert.Equal("LootEntry", fields.Type);

        // A new item at its defaults, edited through its fields as the editor does.
        var bound = fields.Bind(fields.Create());
        var nowhere = new EcsWorld();
        Assert.True(bound.Schema.Write(nowhere, Entity.None, "Item", "Gem"));
        Assert.True(bound.Schema.Write(nowhere, Entity.None, "Weight", 5f));
        Assert.True(bound.Schema.Write(nowhere, Entity.None, "Tags", new ListValue(["shiny"])));
        var gem = bound.Value();

        Assert.True(entries.Write(nowhere, Entity.None, ListValue.Empty.Adding(gem)));

        // Editing a copy leaves the item the list holds as it was.
        var again = fields.Bind(gem);
        again.Schema.Write(nowhere, Entity.None, "Weight", 9f);
        Assert.Equal(5f, ((LootEntry)gem).Weight);

        DataAssets.Save(table.Id);

        var text = File.ReadAllText(Path.Combine(_root, "loot.data.json"));
        using (var saved = JsonDocument.Parse(text))
        {
            var written = Assert.Single(saved.RootElement.GetProperty("fields").GetProperty("Entries").EnumerateArray());
            Assert.Equal("Gem", written.GetProperty("Item").GetString());
            Assert.Equal("shiny", Assert.Single(written.GetProperty("Tags").EnumerateArray()).GetString());
        }

        DataAssets.ReloadAll();
        var read = Assert.Single(DataAssets.Get(table).Entries);
        Assert.Equal("Gem", read.Item);
        Assert.Equal(5f, read.Weight);
        Assert.Equal(["shiny"], read.Tags);
    }

    [Fact]
    public void AComponentsListOfStructsGoesThroughASceneFile()
    {
        var file = Path.Combine(_root, "patrol.scene.json");

        using (var saving = new EngineHarness(frames: 2))
        {
            saving.OnContext(Stage.Startup, ctx =>
            {
                var guard = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(guard, "Guard");
                var patrol = default(Patrol);
                patrol.Points.TryAdd(new Waypoint { At = new Vec3(1f, 0f, 0f), Wait = 2f });
                patrol.Points.TryAdd(new Waypoint { At = new Vec3(0f, 0f, 3f), Wait = 0.5f });
                ctx.Ecs.Add(guard, patrol);

                var schema = ComponentSchemas.For("Bevy.Tests.Patrol")!;
                Assert.Equal(FieldKind.Struct, schema.Field("Points")!.ElementKind);

                SceneFile.Save(ctx.Ecs, file);
            });

            saving.Run();
        }

        using var loading = new EngineHarness(frames: 2);
        loading.OnContext(Stage.Startup, ctx =>
        {
            var guard = Assert.Single(SceneFile.Load(ctx.Ecs, file).Entities);
            var points = ctx.Ecs.GetOrDefault<Patrol>(guard).Points;

            Assert.Equal(2, points.Count);
            Assert.Equal(new Vec3(0f, 0f, 3f), points.ItemAt(1).At);
            Assert.Equal(0.5f, points.ItemAt(1).Wait);
        });

        loading.Run();
    }
}
