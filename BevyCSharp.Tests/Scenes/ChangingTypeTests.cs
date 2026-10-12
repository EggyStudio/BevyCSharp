using System.Text.Json;
using System.Text.Json.Nodes;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A spring with a field renamed, nested in a component so a rename can sit at either level.</summary>
public struct Coil
{
    /// <summary>How hard it pulls, once called Stiff.</summary>
    [FormerName("Stiff")]
    public float Stiffness;

    /// <summary>How quickly it settles.</summary>
    public float Damping;
}

/// <summary>A component renamed twice, the second time out of its namespace, with renamed fields.</summary>
[Behavior]
[FormerName("Hitpoints")]
[FormerName("Game.Old.Vitality")]
public partial struct Vigor
{
    /// <summary>The most it can hold, once called Max.</summary>
    [FormerName("Max")]
    public float Most;

    /// <summary>A struct field renamed, which takes every path under it along.</summary>
    [FormerName("Back")]
    public Coil Rear;
}

/// <summary>A data asset renamed, with a field renamed.</summary>
[DataAsset]
[FormerName("Loadout")]
public sealed class Kit
{
    /// <summary>How many.</summary>
    public int Count;

    /// <summary>What it is called, once called Label.</summary>
    [FormerName("Label")]
    public string Title = string.Empty;
}

/// <summary>A component at its third version, with what brings the first two up to it.</summary>
[Behavior]
[DataVersion(3)]
public partial struct Gauge
{
    /// <summary>The most it reads, kept in tenths since version 2.</summary>
    public float Most;

    /// <summary>What it reads, called Reading before version 3.</summary>
    public float Current;

    /// <summary>Brings a file written at an earlier version up to this one.</summary>
    public static JsonObject Migrate(int from, JsonObject value)
    {
        if (from < 2) value["Most"] = (float?)value["Most"] * 10;

        if (from < 3 && value["Reading"] is { } reading)
        {
            value["Current"] = reading.DeepClone();
            value.Remove("Reading");
        }

        return value;
    }
}

/// <summary>A data asset at its second version.</summary>
[DataAsset]
[DataVersion(2)]
public sealed class Recipe
{
    /// <summary>How many it makes, doubled in version 2.</summary>
    public int Yield;

    /// <summary>Brings a file written at the first version up to this one.</summary>
    public static JsonObject Migrate(int from, JsonObject value)
    {
        if (from < 2) value["Yield"] = (int?)value["Yield"] * 2;
        return value;
    }
}

/// <summary>
/// Covers changing a type without breaking the files written before the change: a type and its
/// fields renamed, and components and values this build does not know carried through a save.
/// </summary>
/// <remarks>
/// The scenes are written by hand, as an older or a newer build would have left them, so each test
/// says exactly what the file held rather than what a save of this build would write.
/// </remarks>
[Collection("engine")]
public sealed class ChangingTypeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-change-" + Guid.NewGuid().ToString("n"));
    private readonly string _was = Streaming.AssetRoot;

    public ChangingTypeTests()
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
    public void FormerNamesAreOnTheSchemaAsFullPaths()
    {
        using var harness = new EngineHarness(frames: 1);

        harness.OnContext(Stage.Startup, ctx =>
        {
            // A name given alone is in the type's own namespace, and one with a dot is as it is.
            var schema = ComponentSchemas.For("Bevy.Tests.Vigor")!;
            Assert.Equal(["Bevy.Tests.Hitpoints", "Game.Old.Vitality"], schema.FormerNames);
            Assert.Same(schema, ComponentSchemas.For("Bevy.Tests.Hitpoints"));
            Assert.Same(schema, ComponentSchemas.For("Game.Old.Vitality"));

            Assert.Equal(["Max"], schema.Field("Most")!.Hints.FormerNames);

            // Under a renamed struct field, every path either level may have had.
            Assert.Equal(
                ["Rear.Stiff", "Back.Stiffness", "Back.Stiff"],
                schema.Field("Rear.Stiffness")!.Hints.FormerNames);
            Assert.Equal(["Back.Damping"], schema.Field("Rear.Damping")!.Hints.FormerNames);
        });

        harness.Run();
    }

    [Fact]
    public void AFileWrittenBeforeARenameLoadsAndSavesUnderTheNewNames()
    {
        var scene = """
            { "format": "bevycsharp.scene.2",
              "entities": [
                { "id": 1, "name": "Old",
                  "components": {
                    "Bevy.Tests.Hitpoints": { "Max": 9, "Back": { "Stiff": 4, "Damping": 2 } },
                    "Bevy.Transform": { "Translation": [0, 0, 0] } } } ] }
            """;
        var file = Path.Combine(_root, "renamed.scene.json");

        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            using var document = JsonDocument.Parse(scene);
            var loaded = SceneFile.Read(ctx.Ecs, document.RootElement);
            Assert.Empty(loaded.Unknown);

            var old = Assert.Single(loaded.Entities);
            var vigor = ctx.Ecs.GetOrDefault<Vigor>(old);
            Assert.Equal(9f, vigor.Most);
            Assert.Equal(4f, vigor.Rear.Stiffness);
            Assert.Equal(2f, vigor.Rear.Damping);

            SceneFile.Save(ctx.Ecs, file, entity => entity == old);
        });

        harness.Run();

        // Written under the new names only, with nothing of the old ones kept beside them.
        using var saved = JsonDocument.Parse(File.ReadAllText(file));
        var components = saved.RootElement.GetProperty("entities")[0].GetProperty("components");
        Assert.False(components.TryGetProperty("Bevy.Tests.Hitpoints", out _));

        var written = components.GetProperty("Bevy.Tests.Vigor");
        Assert.Equal(9f, written.GetProperty("Most").GetSingle());
        Assert.Equal(4f, written.GetProperty("Rear").GetProperty("Stiffness").GetSingle());
        Assert.False(written.TryGetProperty("Max", out _));
        Assert.False(written.TryGetProperty("Back", out _));
    }

    [Fact]
    public void WhatThisBuildDoesNotKnowIsWrittenBackWhereItWas()
    {
        var scene = """
            { "format": "bevycsharp.scene.2",
              "entities": [
                { "id": 1, "name": "Newer",
                  "components": {
                    "Game.Gone": { "X": 1, "Y": [1, 2] },
                    "Bevy.Tests.Vigor": { "Most": 5, "Glow": 3, "Rear": { "Stiffness": 1, "Spin": "fast" } },
                    "Bevy.Transform": { "Translation": [0, 0, 0] } } } ] }
            """;
        var file = Path.Combine(_root, "kept.scene.json");

        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var before = EcsStore.Live;

            using var document = JsonDocument.Parse(scene);
            var loaded = SceneFile.Read(ctx.Ecs, document.RootElement);
            Assert.Equal(["Game.Gone"], loaded.Unknown);

            var newer = Assert.Single(loaded.Entities);
            Assert.Equal(5f, ctx.Ecs.GetOrDefault<Vigor>(newer).Most);

            // A copy keeps its own, and giving both back leaves the store as it was.
            var copy = ctx.Ecs.Clone(newer);
            Assert.Equal(before + 2, EcsStore.Live);

            SceneFile.Save(ctx.Ecs, file, entity => entity == newer);

            ctx.Ecs.Despawn(copy);
            ctx.Ecs.Despawn(newer);
            Assert.Equal(before, EcsStore.Live);
        });

        harness.Run();

        using var saved = JsonDocument.Parse(File.ReadAllText(file));
        var components = saved.RootElement.GetProperty("entities")[0].GetProperty("components");

        var gone = components.GetProperty("Game.Gone");
        Assert.Equal(1, gone.GetProperty("X").GetInt32());
        Assert.Equal(2, gone.GetProperty("Y").GetArrayLength());

        // Unknown values back beside the fields, at the depth they were found.
        var vigor = components.GetProperty("Bevy.Tests.Vigor");
        Assert.Equal(5f, vigor.GetProperty("Most").GetSingle());
        Assert.Equal(3, vigor.GetProperty("Glow").GetInt32());
        Assert.Equal("fast", vigor.GetProperty("Rear").GetProperty("Spin").GetString());
        Assert.Equal(1f, vigor.GetProperty("Rear").GetProperty("Stiffness").GetSingle());
    }

    [Fact]
    public void ADataAssetWrittenBeforeARenameLoadsAndKeepsWhatItDoesNotRead()
    {
        File.WriteAllText(
            Path.Combine(_root, "kit.data.json"),
            """
            { "format": "bevycsharp.data.1", "type": "Bevy.Tests.Loadout",
              "fields": { "Count": 3, "Label": "Old", "Extra": [1] } }
            """);
        var id = AssetIds.IdOf("kit.data.json", create: true);

        // A picker filtering by type offers it under the type's current name.
        Assert.Equal("Bevy.Tests.Kit", DataAssets.TypeOf("kit.data.json"));
        Assert.DoesNotContain("Bevy.Tests.Loadout", DataAssets.Types);

        var kit = DataAssets.Get(new DataRef<Kit>(id));
        Assert.Equal(3, kit.Count);
        Assert.Equal("Old", kit.Title);

        DataAssets.Save(id);

        using var saved = JsonDocument.Parse(File.ReadAllText(Path.Combine(_root, "kit.data.json")));
        Assert.Equal("Bevy.Tests.Kit", saved.RootElement.GetProperty("type").GetString());
        var fields = saved.RootElement.GetProperty("fields");
        Assert.Equal("Old", fields.GetProperty("Title").GetString());
        Assert.False(fields.TryGetProperty("Label", out _));
        Assert.Equal(1, fields.GetProperty("Extra").GetArrayLength());
    }

    [Fact]
    public void AFileAtAnEarlierVersionIsBroughtUpToThisOne()
    {
        var scene = """
            { "format": "bevycsharp.scene.2",
              "entities": [
                { "id": 1, "name": "First", "components": { "Bevy.Tests.Gauge": { "Most": 5, "Reading": 2 } } },
                { "id": 2, "name": "Second", "components": { "Bevy.Tests.Gauge": { "$version": 2, "Most": 50, "Reading": 2 } } },
                { "id": 3, "name": "Third", "components": { "Bevy.Tests.Gauge": { "$version": 3, "Most": 50, "Current": 2 } } } ] }
            """;
        var file = Path.Combine(_root, "versions.scene.json");

        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            Assert.Equal(3, ComponentSchemas.For("Bevy.Tests.Gauge")!.Version);

            using var document = JsonDocument.Parse(scene);
            var loaded = SceneFile.Read(ctx.Ecs, document.RootElement);

            // Each from where it was, to the same place.
            foreach (var entity in loaded.Entities)
            {
                var gauge = ctx.Ecs.GetOrDefault<Gauge>(entity);
                Assert.Equal(50f, gauge.Most);
                Assert.Equal(2f, gauge.Current);
            }

            SceneFile.Save(ctx.Ecs, file, entity => loaded.Entities.Contains(entity));
        });

        harness.Run();

        // Written at the current version, with nothing of the old shape kept.
        using var saved = JsonDocument.Parse(File.ReadAllText(file));
        foreach (var entry in saved.RootElement.GetProperty("entities").EnumerateArray())
        {
            var gauge = entry.GetProperty("components").GetProperty("Bevy.Tests.Gauge");
            Assert.Equal(SceneValue.VersionName, gauge.EnumerateObject().First().Name);
            Assert.Equal(3, gauge.GetProperty(SceneValue.VersionName).GetInt32());
            Assert.False(gauge.TryGetProperty("Reading", out _));
        }
    }

    [Fact]
    public void ADataAssetAtAnEarlierVersionIsBroughtUpOnceAndSavedAtThisOne()
    {
        File.WriteAllText(
            Path.Combine(_root, "bread.data.json"),
            """{ "format": "bevycsharp.data.1", "type": "Bevy.Tests.Recipe", "fields": { "Yield": 4 } }""");
        var id = AssetIds.IdOf("bread.data.json", create: true);

        Assert.Equal(8, DataAssets.Get(new DataRef<Recipe>(id)).Yield);
        DataAssets.Save(id);

        // Read again from the file it was saved to, which records the version, so it is not doubled twice.
        DataAssets.ReloadAll();
        Assert.Equal(8, DataAssets.Get(new DataRef<Recipe>(id)).Yield);
        Assert.Contains("\"$version\": 2", File.ReadAllText(Path.Combine(_root, "bread.data.json")));
    }
}
