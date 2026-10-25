using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Trees planted by name and seed, the registry of species, a forest spread over a disc, and the
/// system that dresses each tree in its variant's stems, leaves and colliders once it has grown.
/// </summary>
[Collection("engine")]
public sealed class TreePlantingTests
{
    [Fact]
    public void TheRegistryHoldsThePresetsAndASpeciesAGameRegisters()
    {
        Assert.Same(TreeSpecies.QuakingAspen, Trees.Species("quaking-aspen"));
        Assert.Contains("california-black-oak", Trees.SpeciesNames);
        Assert.Null(Trees.Species("no-such-tree"));

        var tall = TreeSpecies.QuakingAspen with { Scale = 20f };
        Trees.Register("tall-aspen", tall);
        Assert.Same(tall, Trees.Species("tall-aspen"));
        Assert.Throws<ArgumentException>(() => Trees.Register("Tall Aspen", tall));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Register("broken-aspen", tall with { Scale = -1f }));
    }

    [Fact]
    public void AForestIsSpreadOverItsDiscTheSameForTheSameSeed()
    {
        var middle = new Vec3(10f, 2f, -5f);
        Vec3[] places = [], again = [];
        var named = new List<string?>();
        using var harness = new EngineHarness(frames: 2);
        harness.OnContext(Stage.Startup, ctx =>
        {
            var ecs = ctx.Ecs;
            var forest = Trees.PlantForest(ecs, "quaking-aspen", 4, middle, 12f, 15);
            places = [.. forest.Select(tree => ecs.GetOrDefault<Transform>(tree).Translation)];
            named.AddRange(forest.Select(tree => ecs.GetOrDefault<Tree>(tree).Species.Value));
            again = [.. Trees.PlantForest(ecs, "quaking-aspen", 4, middle, 12f, 15).Select(tree => ecs.GetOrDefault<Transform>(tree).Translation)];
            Assert.Throws<ArgumentException>(() => Trees.Plant(ecs, "no-such-tree", 1, Vec3.Zero));
        });
        harness.Run();

        // An even spread puts each tree in a fifteenth of the disc, and none stands nearer another
        // than seven tenths of that spacing.
        var apart = 0.7f * MathF.Sqrt(MathF.PI * 12f * 12f / 15f);
        Assert.InRange(places.Length, 10, 15);
        Assert.All(places, place => Assert.True((place - middle).Length <= 12.001f && place.Y == middle.Y, $"a tree stands at {place}"));
        for (var i = 0; i < places.Length; i++)
            for (var j = i + 1; j < places.Length; j++)
                Assert.True((places[i] - places[j]).Length >= apart - 1e-3f, $"two trees stand {(places[i] - places[j]).Length} apart");

        Assert.All(named, name => Assert.Equal("quaking-aspen", name));
        Assert.Equal(places, again);
    }

    [SkippableFact]
    public void APlantedTreeIsDressedInItsVariantsStemsLeavesAndColliders()
    {
        Needs.Renderer();
        Entity first = default, second = default, renamed = default, bare = default;
        Entity firstBody = default, secondBody = default, renamedBody = default;
        AssetHandle firstMesh = default, secondMesh = default;
        var colliders = new List<Collider>();
        var leaves = 0;
        var size = 0f;
        var takesPart = false;

        var run = new PictureRun
        {
            Frames = 1200,
            Scene = ecs =>
            {
                // Seeds three and eleven share the fourth variant, each turned and sized its own way.
                first = Trees.Plant(ecs, "quaking-aspen", 3, new Vec3(-5f, 0f, -20f));
                second = Trees.Plant(ecs, "quaking-aspen", 11, new Vec3(5f, 0f, -20f));
                renamed = Trees.Plant(ecs, "quaking-aspen", 2, new Vec3(0f, 0f, -30f));
                bare = ecs.Spawn();
                ecs.Add(bare, Transform.Identity);
                ecs.Add(bare, new Tree { Species = "no-such-tree", Seed = 1 });
            },
        };

        static Entity Body(EcsWorld ecs, Entity tree) => ecs.ChildrenOf(tree).FirstOrDefault();

        run.Until("dressed", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                return new[] { first, second, renamed }.All(tree => ecs.ChildrenOf(tree).Length > 0);
            })
            .Do("read", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                firstBody = Body(ecs, first);
                secondBody = Body(ecs, second);
                firstMesh = Render.MeshOf(ecs, firstBody);
                secondMesh = Render.MeshOf(ecs, secondBody);
                size = ecs.GetOrDefault<Transform>(firstBody).Scale.X;
                takesPart = ecs.Has<Visibility>(first);
                foreach (var child in ecs.ChildrenOf(firstBody))
                {
                    if (ecs.Has<Collider>(child)) colliders.Add(ecs.GetOrDefault<Collider>(child));
                    else leaves++;
                }

                // A tree given another seed is dressed again.
                renamedBody = Body(ecs, renamed);
                ecs.GetRef<Tree>(renamed).Seed = 5;
            })
            .Until("dressed again", world => Body(world.Resource<EcsWorld>(), renamed) is var body && body != Entity.None && body != renamedBody)
            .Do("bare", world => Assert.Empty(world.Resource<EcsWorld>().ChildrenOf(bare)))
            .Go();

        Assert.Equal(firstMesh, secondMesh);
        Assert.NotEqual(firstBody, secondBody);
        Assert.InRange(size, 0.9f, 1.1f);
        // The tree's own entity takes part in Bevy's visibility, as the parent of its meshes, where
        // without it a tree's leaves stayed undrawn until something moved them.
        Assert.True(takesPart, "the tree's entity has no Visibility");
        Assert.Equal(1, leaves);
        // The aspen's limbs are thinner than a capsule's twelve centimeters, so its trunk stands alone,
        // a capsule meters tall.
        Assert.InRange(colliders.Count, 1, 24);
        Assert.All(colliders, collider => Assert.True(collider.Shape == ColliderShape.Capsule && collider.Size.X >= 0.12f, $"a collider is {collider.Shape} {collider.Size}"));
        Assert.True(colliders.Max(collider => collider.Size.Y) > 3f, "no capsule stands along the trunk");
    }
}
