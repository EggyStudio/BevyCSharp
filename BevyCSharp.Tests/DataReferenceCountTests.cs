using Bevy;
using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>A rack in an armory, an item with a reference of its own.</summary>
public sealed class Rack
{
    /// <summary>What hangs on it.</summary>
    public DataRef<WeaponStats> Weapon;
}

/// <summary>A data asset sharing weapons through its items rather than a field of its own.</summary>
[DataAsset]
public sealed class Armory
{
    /// <summary>The racks.</summary>
    public List<Rack> Racks = [];
}

/// <summary>
/// Covers finding what refers to a data asset when the reference is inside a list's items, as the
/// editor counts what shares an asset.
/// </summary>
[Collection("engine")]
public sealed class DataReferenceCountTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-refs-" + Guid.NewGuid().ToString("n"));
    private readonly string _was = Streaming.AssetRoot;

    public DataReferenceCountTests()
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
    public void AReferenceInsideAListsItemCountsAsReferringToTheAsset()
    {
        var sword = DataAssets.Create<WeaponStats>("sword.data.json");
        var axe = DataAssets.Create<WeaponStats>("axe.data.json");
        var armory = DataAssets.Create<Armory>("armory.data.json");

        var schema = DataAssets.SchemaOf(armory.Id)!;
        var racks = schema.Field("Racks")!;
        var nowhere = new EcsWorld();

        // One rack holding the sword, written through the item's fields as the editor writes it.
        var bound = racks.Items!.Bind(racks.Items.Create());
        Assert.True(bound.Schema.Write(nowhere, Entity.None, "Weapon", sword.Id));
        Assert.True(racks.Write(nowhere, Entity.None, ListValue.Empty.Adding(bound.Value())));

        var value = racks.Read(nowhere, Entity.None);
        Assert.True(EditorDataAssets.Refers(nowhere, racks, value, sword.Id));
        Assert.False(EditorDataAssets.Refers(nowhere, racks, value, axe.Id));

        // And the armory is counted among the sword's users, as the fold under a reference says.
        Assert.Equal(1, EditorDataAssets.Holders(nowhere, sword.Id));
        Assert.Equal(0, EditorDataAssets.Holders(nowhere, axe.Id));
    }
}
