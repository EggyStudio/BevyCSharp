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
        var parts = new List<string>();
        FloatRange fullEnd = default, reducedStart = default, reducedEnd = default, cardsEnd = default, impostorStart = default;
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
                    else parts.Add(ecs.NameOf(child) ?? "");
                }

                // The full level's stems on the body, and the reduced level's leaves taking over
                // where the full level's are dithered out.
                fullEnd = ecs.Get<Bevy.Reflected.VisibilityRangeRef>(firstBody)!.Value.EndMargin;
                var reduced = ecs.ChildrenOf(firstBody).First(child => ecs.NameOf(child) == "quaking-aspen 3, leaves, reduced");
                reducedStart = ecs.Get<Bevy.Reflected.VisibilityRangeRef>(reduced)!.Value.StartMargin;
                reducedEnd = ecs.Get<Bevy.Reflected.VisibilityRangeRef>(reduced)!.Value.EndMargin;
                var cards = ecs.ChildrenOf(firstBody).First(child => ecs.NameOf(child) == "quaking-aspen 3, cards");
                cardsEnd = ecs.Get<Bevy.Reflected.VisibilityRangeRef>(cards)!.Value.EndMargin;
                var impostor = ecs.ChildrenOf(firstBody).First(child => ecs.NameOf(child) == "quaking-aspen 3, impostor");
                impostorStart = ecs.Get<Bevy.Reflected.VisibilityRangeRef>(impostor)!.Value.StartMargin;

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
        Assert.Equal(["quaking-aspen 3, cards", "quaking-aspen 3, impostor", "quaking-aspen 3, leaves", "quaking-aspen 3, leaves, reduced", "quaking-aspen 3, limbs", "quaking-aspen 3, stems, reduced"], parts.Order(StringComparer.Ordinal));

        // The aspen, about 13 meters tall, changes level two and a half to three of its heights away.
        Assert.Equal(fullEnd, reducedStart);
        Assert.InRange(fullEnd.Start, 2.5f * 9f, 2.5f * 18f);
        Assert.Equal(0.5f / 2.5f * fullEnd.Start, fullEnd.End - fullEnd.Start, 3);
        // The reduced level gives way to the far one of cards six heights away, and the cards to the
        // impostor twenty heights away.
        Assert.Equal(6f / 2.5f * fullEnd.Start, reducedEnd.Start, 2);
        Assert.Equal(cardsEnd, impostorStart);
        Assert.Equal(20f / 2.5f * fullEnd.Start, impostorStart.Start, 2);
        // The aspen's limbs are thinner than a capsule's twelve centimeters, so its trunk stands alone,
        // a capsule meters tall.
        Assert.InRange(colliders.Count, 1, 24);
        Assert.All(colliders, collider => Assert.True(collider.Shape == ColliderShape.Capsule && collider.Size.X >= 0.12f, $"a collider is {collider.Shape} {collider.Size}"));
        Assert.True(colliders.Max(collider => collider.Size.Y) > 3f, "no capsule stands along the trunk");
    }

    [SkippableFact]
    public void TheScenesCameraIsWhereATreesShadowIsMeasuredFrom()
    {
        Needs.Renderer();

        // A camera spawned first and drawing into an image of its own, as a light probe's face
        // cameras do, and the scene's camera after it. Bevy measures a sun's shadows by the first
        // camera of all in an offscreen run unless one carries ShadowLodOrigin, so a tree near the
        // scene's camera had its full level dithered out of its shadow by the far camera's distance
        // and its reduced level left out as too near, and cast none. The scene's camera is given
        // the origin, and the other is not.
        Entity aside = default, camera = default;
        var origins = (Aside: true, Camera: false);
        var run = new PictureRun
        {
            Frames = 600,
            Scene = ecs =>
            {
                aside = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 45f });
                ecs.Add(aside, Transform.LookingAt(new Vec3(0f, 10f, 500f), new Vec3(0f, 0f, 520f), Vec3.UnitY));
                Render.SetCameraTarget(aside, Render.CreateTarget(64, 64));
                camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 45f });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 10f, 22f), Vec3.Zero, Vec3.UnitY));
            },
        };

        run.Wait(10).Do("read", world =>
        {
            var ecs = world.Resource<EcsWorld>();
            origins = (ecs.Get<Bevy.Reflected.ShadowLodOriginRef>(aside) is not null, ecs.Get<Bevy.Reflected.ShadowLodOriginRef>(camera) is not null);
        }).Go();

        Assert.False(origins.Aside, "the camera drawing into an image of its own is the shadows' origin");
        Assert.True(origins.Camera, "the scene's camera is not the shadows' origin");
    }
}
