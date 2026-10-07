using System.Buffers;
using System.Text;
using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A data asset holding what a component cannot: text, lists and a dictionary.</summary>
[DataAsset]
public sealed class WeaponStats
{
    /// <summary>A number with a range, which the editor draws as a slider.</summary>
    [Range(0, 200)]
    public float Damage = 10f;

    /// <summary>Text.</summary>
    public string Title = "Sword";

    /// <summary>A list of text.</summary>
    public List<string> Tags = [];

    /// <summary>Numbers by name.</summary>
    public Dictionary<string, int> Bonuses = [];

    /// <summary>Another asset of the same kind.</summary>
    public DataRef<WeaponStats> Upgrade;
}

/// <summary>A data asset that is a struct, written back through its box.</summary>
[DataAsset]
public struct Shade
{
    /// <summary>A color.</summary>
    public Color Tint;

    /// <summary>A number.</summary>
    public int Weight;
}

/// <summary>A component referring to a data asset.</summary>
[Behavior]
public partial struct Armed
{
    /// <summary>The asset, by its file's id.</summary>
    public DataRef<WeaponStats> Weapon;
}

/// <summary>
/// Covers data assets: their files, their ids, and a component referring to one.
/// </summary>
/// <remarks>
/// Each test points the asset root at a directory of its own and puts it back afterwards, since
/// the ids and the cache are static and a test writes files.
/// </remarks>
[Collection("engine")]
public sealed class DataAssetTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-data-" + Guid.NewGuid().ToString("n"));
    private readonly string _was = Streaming.AssetRoot;

    public DataAssetTests()
    {
        Directory.CreateDirectory(_root);
        Streaming.AssetRoot = _root;
        DataAssets.ReloadAll();
        AssetIds.Reindex();
    }

    public void Dispose()
    {
        DataAssets.Watching = false;
        DataAssets.PostChanges(new MessageBus());
        Streaming.AssetRoot = _was;
        DataAssets.ReloadAll();
        AssetIds.Reindex();
        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void ADataAssetIsMadeAtItsDefaultsWithAnIdBesideIt()
    {
        var sword = DataAssets.Create<WeaponStats>("weapons/sword.data.json");

        Assert.True(sword.IsSet);
        Assert.True(File.Exists(Path.Combine(_root, "weapons/sword.data.json.uid")));
        Assert.Equal(10f, sword.Value.Damage);
        Assert.Equal("Bevy.Tests.WeaponStats", DataAssets.TypeOf("weapons/sword.data.json"));
        Assert.Contains("Bevy.Tests.WeaponStats", DataAssets.Types);

        var schema = DataAssets.SchemaOf(sword.Id)!;
        Assert.Equal(FieldKind.String, schema.Field("Title")!.Kind);
        Assert.Equal(FieldKind.List, schema.Field("Tags")!.Kind);
        Assert.Equal(FieldKind.String, schema.Field("Tags")!.ElementKind);
        Assert.Equal(FieldKind.Map, schema.Field("Bonuses")!.Kind);
        Assert.Equal(FieldKind.Data, schema.Field("Upgrade")!.Kind);
        Assert.Equal("Bevy.Tests.WeaponStats", schema.Field("Upgrade")!.Hints.Asset);
        Assert.Equal(200d, schema.Field("Damage")!.Hints.Maximum);
    }

    [Fact]
    public void WhatIsWrittenThroughItsFieldsIsSavedAndReadBack()
    {
        var sword = DataAssets.Create<WeaponStats>("sword.data.json");
        var axe = DataAssets.Create<WeaponStats>("axe.data.json");

        // Through the same fields the editor draws, which write the shared value.
        var schema = DataAssets.SchemaOf(sword.Id)!;
        var nowhere = new EcsWorld();
        Assert.True(schema.Write(nowhere, Entity.None, "Damage", 42.5));
        Assert.True(schema.Write(nowhere, Entity.None, "Title", "Longsword"));
        Assert.True(schema.Write(nowhere, Entity.None, "Tags", new ListValue(["sharp", "long"])));
        Assert.True(schema.Write(nowhere, Entity.None, "Bonuses", new MapValue([new("fire", 3L)])));
        Assert.True(schema.Write(nowhere, Entity.None, "Upgrade", axe.Id));
        DataAssets.Save(sword.Id);

        // Read from the file, not the cache.
        DataAssets.ReloadAll();
        var read = sword.Value;
        Assert.Equal(42.5f, read.Damage);
        Assert.Equal("Longsword", read.Title);
        Assert.Equal(["sharp", "long"], read.Tags);
        Assert.Equal(3, read.Bonuses["fire"]);
        Assert.Equal(axe, read.Upgrade);

        // Shared, so every reader has the same value.
        Assert.Same(read, sword.Value);
    }

    [Fact]
    public void AStructAssetIsWrittenBackThroughItsBox()
    {
        var shade = DataAssets.Create<Shade>("shade.data.json");

        var schema = DataAssets.SchemaOf(shade.Id)!;
        Assert.True(schema.Write(new EcsWorld(), Entity.None, "Tint", Color.FromHex("#336699")));
        Assert.True(schema.Write(new EcsWorld(), Entity.None, "Weight", 7));
        DataAssets.Save(shade.Id);

        DataAssets.ReloadAll();
        Assert.Equal(7, shade.Value.Weight);
        Assert.Equal(Color.FromHex("#336699"), shade.Value.Tint);
    }

    [Fact]
    public void AReferenceSurvivesItsFileBeingRenamed()
    {
        var sword = DataAssets.Create<WeaponStats>("sword.data.json");
        DataAssets.Save(sword, new WeaponStats { Damage = 99f });

        // Through the editor, which moves the sidecar with the file.
        AssetIds.Move("sword.data.json", "kept/blade.data.json");
        DataAssets.ReloadAll();
        Assert.Equal(99f, sword.Value.Damage);
        Assert.Equal("kept/blade.data.json", AssetIds.PathOf(sword.Id));

        // And outside it, with the sidecar moved along, found again by reading the root.
        Directory.CreateDirectory(Path.Combine(_root, "elsewhere"));
        File.Move(Path.Combine(_root, "kept/blade.data.json"), Path.Combine(_root, "elsewhere/edge.data.json"));
        File.Move(Path.Combine(_root, "kept/blade.data.json.uid"), Path.Combine(_root, "elsewhere/edge.data.json.uid"));
        DataAssets.ReloadAll();
        Assert.Equal(99f, sword.Value.Damage);
    }

    [Fact]
    public void AnAssetOfAnotherTypeIsRefused()
    {
        var shade = DataAssets.Create<Shade>("shade.data.json");
        var wrong = new DataRef<WeaponStats>(shade.Id);

        Assert.Throws<InvalidDataException>(() => wrong.Value);
        Assert.False(DataAssets.TryGet(wrong, out _));
        Assert.False(DataAssets.TryGet(new DataRef<WeaponStats>(12345), out _));
    }

    [Fact]
    public void AComponentRefersToAnAssetByIdAndAPathInAScene()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            // The app sets the asset root as it starts, so it is pointed back here.
            Streaming.AssetRoot = _root;
            AssetIds.Reindex();

            var sword = DataAssets.Create<WeaponStats>("sword.data.json");
            var schema = ComponentSchemas.For("Bevy.Tests.Armed")!;
            var weapon = schema.Field("Weapon")!;
            Assert.Equal(FieldKind.Data, weapon.Kind);
            Assert.Equal("Bevy.Tests.WeaponStats", weapon.Hints.Asset);

            var source = ctx.Ecs.Spawn();
            ctx.Ecs.Add(source, default(Armed));
            Assert.True(weapon.Write(ctx.Ecs, source, sword.Id));
            Assert.Equal(sword, ctx.Ecs.GetOrDefault<Armed>(source).Weapon);

            var buffer = new ArrayBufferWriter<byte>();
            using (var json = new Utf8JsonWriter(buffer))
                SceneValue.WriteComponent(json, schema, ctx.Ecs, source);
            var text = Encoding.UTF8.GetString(buffer.WrittenSpan);
            Assert.Contains($"\"uid\":\"{sword.Id:x16}\"", text);
            Assert.Contains("\"path\":\"sword.data.json\"", text);

            // A scene naming an id no sidecar holds is read by its path, as one whose sidecar was
            // lost would be.
            var stale = text.Replace($"{sword.Id:x16}", "00000000000000ff");
            var copy = ctx.Ecs.Spawn();
            ctx.Ecs.Add(copy, default(Armed));
            using var document = JsonDocument.Parse(stale);
            SceneValue.ReadComponent(document.RootElement, schema, ctx.Ecs, copy);
            Assert.Equal(sword, ctx.Ecs.GetOrDefault<Armed>(copy).Weapon);
        });

        harness.Run();
    }

    [Fact]
    public void AFileChangedOnDiskIsReadAgainAndOneThisSideWroteIsNot()
    {
        DataAssets.Watching = true;
        var bus = new MessageBus();

        var sword = DataAssets.Create<WeaponStats>("watched.data.json");
        DataAssets.PostChanges(bus);
        var before = DataAssets.Get(sword);

        // Written by this side, which the watcher passes over, so the loaded value stays the same one.
        DataAssets.Save(sword, before);
        for (var i = 0; i < 10; i++)
        {
            DataAssets.PostChanges(bus);
            Thread.Sleep(30);
        }

        Assert.Same(before, DataAssets.Get(sword));

        // Edited outside, as a text editor or a pull would.
        var file = Path.Combine(_root, "watched.data.json");
        File.WriteAllText(file, File.ReadAllText(file).Replace("\"Damage\": 10", "\"Damage\": 77"));

        var seen = 0f;
        for (var i = 0; i < 100 && seen != 77f; i++)
        {
            Thread.Sleep(30);
            DataAssets.PostChanges(bus);
            seen = DataAssets.Get(sword).Damage;
        }

        Assert.Equal(77f, seen);
    }

    [Fact]
    public void ACopyIsAFileOfItsOwnWithTheSameValues()
    {
        var sword = DataAssets.Create<WeaponStats>("blades/sword.data.json");
        var schema = DataAssets.SchemaOf(sword.Id)!;
        Assert.True(schema.Write(new EcsWorld(), Entity.None, "Damage", 33.0));

        var copy = DataAssets.Copy(sword.Id, "blades/sword 2.data.json");

        Assert.NotEqual(sword.Id, copy);
        Assert.Equal("blades/sword 2.data.json", AssetIds.PathOf(copy));
        Assert.Equal(33f, DataAssets.Get(new DataRef<WeaponStats>(copy)).Damage);

        // Changing the copy leaves the original alone.
        Assert.True(DataAssets.SchemaOf(copy)!.Write(new EcsWorld(), Entity.None, "Damage", 1.0));
        Assert.Equal(33f, DataAssets.Get(sword).Damage);

        Assert.Throws<IOException>(() => DataAssets.Copy(sword.Id, "blades/sword 2.data.json"));
    }
}
