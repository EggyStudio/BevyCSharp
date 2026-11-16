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
    private readonly TestFolder _folder = new("bcs-refs-");
    private readonly string _was = Streaming.AssetRoot;

    public DataReferenceCountTests()
    {
        Directory.CreateDirectory(_folder.Path);
        Streaming.AssetRoot = _folder.Path;
        DataAssets.ReloadAll();
        AssetIds.Reindex();
    }

    public void Dispose()
    {
        Streaming.AssetRoot = _was;
        DataAssets.ReloadAll();
        AssetIds.Reindex();
        _folder.Dispose();
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

    /// <summary>
    /// The entities sharing a data asset are counted by one walk of the world for calls in a row,
    /// as the fold under a reference asks every frame it is drawn, and by another once the count is
    /// forgotten, as "Make unique" forgets it.
    /// </summary>
    [Fact]
    public void TheEntitiesSharingAnAssetAreCountedOnceForCallsInARow()
    {
        using var harness = new EngineHarness(frames: 2);
        harness.OnContext(Stage.Startup, ctx =>
        {
            // The app sets the asset root as it starts, so it is pointed back here.
            Streaming.AssetRoot = _folder.Path;
            AssetIds.Reindex();
            EditorDataAssets.Forget();

            var sword = DataAssets.Create<WeaponStats>("sword.data.json");
            var weapon = ComponentSchemas.For("Bevy.Tests.Armed")!.Field("Weapon")!;
            for (var i = 0; i < 2; i++)
            {
                var armed = ctx.Ecs.Spawn();
                ctx.Ecs.Add(armed, default(Armed));
                Assert.True(weapon.Write(ctx.Ecs, armed, sword.Id));
            }

            var walks = EditorDataAssets.Walks;
            for (var call = 0; call < 3; call++) Assert.Equal(2, EditorDataAssets.Users(ctx.Ecs, sword.Id));
            Assert.Equal(walks + 1, EditorDataAssets.Walks);

            EditorDataAssets.Forget();
            Assert.Equal(2, EditorDataAssets.Users(ctx.Ecs, sword.Id));
            Assert.Equal(walks + 2, EditorDataAssets.Walks);
        });

        harness.Run();
    }
}
