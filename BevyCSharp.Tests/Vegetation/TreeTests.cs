using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// Trees grown by <see cref="Trees.Grow(TreeSpecies, uint)"/> and built by <see cref="Trees.Build(TreeSkeleton, TreeDetail)"/>,
/// their sameness by seed, the presets' sizes against the paper's, their thickness by the pipe
/// model, the boxes they grow round and their meshes.
/// </summary>
public sealed class TreeTests(ITestOutputHelper output)
{
    private static readonly TreeSpecies Aspen = TreeSpecies.QuakingAspen;

    [Fact]
    public void ASeedGrowsTheSameSkeletonBitForBit()
    {
        var first = Trees.Grow(Aspen, 7).Nodes;
        var again = Trees.Grow(Aspen, 7).Nodes;
        Assert.Equal(first.Count, again.Count);
        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].Parent, again[i].Parent);
            foreach (var (one, other) in new[] { (first[i].Place.X, again[i].Place.X), (first[i].Place.Y, again[i].Place.Y), (first[i].Place.Z, again[i].Place.Z), (first[i].Radius, again[i].Radius) })
            {
                Assert.Equal(BitConverter.SingleToInt32Bits(one), BitConverter.SingleToInt32Bits(other));
            }
        }

        Assert.Equal(Trees.Grow(Aspen, 7), Trees.Grow(Aspen with { Levels = [.. Aspen.Levels] }, 7));
        Assert.NotEqual(Trees.Grow(Aspen, 7), Trees.Grow(Aspen, 8));
    }

    // The presets, each with the heights the paper's scale and trunk length allow it, and its width
    // over its height as the paper's plates draw it, read off them at about 0.45 for the aspen,
    // 0.55 for the tupelo, 0.9 for the willow and 1.05 for the oak, held to three tenths either way.
    // A tree may stand a little over its trunk, as a crown's tips rise past it, and the bush's and the
    // fern's are the library's own.
    [Theory]
    [InlineData("quaking aspen", 10f, 16f, 0.45f)]
    [InlineData("black tupelo", 18f, 28f, 0.55f)]
    [InlineData("weeping willow", 8f, 16f, 0.9f)]
    [InlineData("California black oak", 0f, 20f, 1.05f)]
    [InlineData("bush", 1.1f, 1.9f, 1f)]
    [InlineData("fern", 0.4f, 1.1f, 2.5f)]
    public void APresetStandsAsTallAsThePapersAndAsWideAsItsPlate(string name, float lowest, float highest, float plate)
    {
        var species = name switch
        {
            "quaking aspen" => TreeSpecies.QuakingAspen,
            "black tupelo" => TreeSpecies.BlackTupelo,
            "weeping willow" => TreeSpecies.WeepingWillow,
            "California black oak" => TreeSpecies.CaliforniaBlackOak,
            "bush" => TreeSpecies.Bush,
            _ => TreeSpecies.Fern,
        };

        var ratios = new List<float>();
        for (var seed = 1u; seed <= 3; seed++)
        {
            var (low, high) = Trees.Grow(species, seed).Bounds;
            var width = (high.X - low.X + high.Z - low.Z) / 2f;
            Assert.InRange(high.Y, lowest * 0.9f, highest * 1.15f);
            Assert.True(low.Y >= 0f, $"seed {seed} grew a stem into the ground, to {low.Y}");
            ratios.Add(width / high.Y);
            output.WriteLine($"| {name} | {seed} | {lowest:0.#} to {highest:0.#} m | {high.Y:0.0} m | {width:0.0} m | {plate:0.00} | {width / high.Y:0.00} |");
        }

        Assert.InRange(ratios.Average(), plate * 0.7f, plate * 1.3f);
    }

    [Fact]
    public void TheAspensTrunkIsBareBelowItsCrownAndAsThickAsItsRatio()
    {
        for (var seed = 1u; seed <= 3; seed++)
        {
            var skeleton = Trees.Grow(Aspen, seed);
            var nodes = skeleton.Nodes;
            var (_, high) = skeleton.Bounds;

            // The trunk's foot is as thick for the trunk's height as the ratio and the flare make it.
            Assert.InRange(nodes[0].Radius / high.Y, 0.015f * 1.594f * 0.85f, 0.015f * 1.594f * 1.15f);

            // Below the crown's foot, four tenths of the scale up, the trunk stands alone, the bound
            // lowered for the crown's tips standing over the trunk and for the lowest branches,
            // which leave at up to 110 degrees and dip.
            var bare = 0.35f * high.Y - 1f;
            var children = new int[nodes.Count];
            for (var i = 1; i < nodes.Count; i++) children[nodes[i].Parent]++;
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Place.Y < bare) Assert.True(children[i] <= 1, $"seed {seed} branches at {nodes[i].Place}, below the crown's foot at {bare}");
            }
        }
    }

    [Fact]
    public void AStemIsAsThickAsThePipeModelMakesIt()
    {
        var nodes = Trees.Grow(Aspen with { Flare = 0f }, 3).Nodes;
        var unit = nodes.Min(node => node.Radius);
        var carried = new float[nodes.Count];
        for (var i = 1; i < nodes.Count; i++) carried[nodes[i].Parent] += nodes[i].Radius * nodes[i].Radius;

        for (var i = 0; i < nodes.Count; i++)
        {
            // Every level tapers fully, so a point's cross section is its own unit and those of the
            // points it carries.
            var expected = unit * unit + carried[i];
            Assert.True(MathF.Abs(nodes[i].Radius * nodes[i].Radius - expected) <= expected * 1e-3f, $"point {i} is {nodes[i].Radius} thick and its stems call for {MathF.Sqrt(expected)}");
            if (i > 0) Assert.True(nodes[i].Radius < nodes[nodes[i].Parent].Radius, $"point {i} is thicker than the point it grew from");
        }
    }

    [Fact]
    public void ATrunkAloneGrowsStraightToItsHeight()
    {
        var nodes = Trees.Grow(new TreeSpecies { Scale = 8f, Levels = [new TreeLevel()] }, 1).Nodes;
        Assert.InRange(nodes.Count, 40, 41);
        Assert.All(nodes, node => Assert.True(node.Place.X == 0f && node.Place.Z == 0f, $"a trunk with no curve leans, to {node.Place}"));
        Assert.InRange(nodes[^1].Place.Y, 7.6f, 8.01f);
        Assert.Equal(8f * 0.015f, nodes[0].Radius, 5);

        // A flare of one doubles the trunk at the ground, less a hundredth, as the paper's does.
        var flared = Trees.Grow(new TreeSpecies { Scale = 8f, Flare = 1f, Levels = [new TreeLevel()] }, 1).Nodes;
        Assert.Equal(8f * 0.015f * 1.99f, flared[0].Radius, 4);
        Assert.Equal(nodes[^1].Radius, flared[^1].Radius);
    }

    [Fact]
    public void ATrunkSplitsAtTheCrownsFootIntoItsBaseSplits()
    {
        var species = new TreeSpecies { Scale = 8f, BaseSize = 0.25f, BaseSplits = 2, Levels = [new TreeLevel { SplitAngle = 20f }] };
        var nodes = Trees.Grow(species, 4).Nodes;
        var children = new int[nodes.Count];
        for (var i = 1; i < nodes.Count; i++) children[nodes[i].Parent]++;

        // One point carries three, two meters up, and the three trunks run on to the top apart.
        var fork = Assert.Single(Enumerable.Range(0, nodes.Count), i => children[i] > 1);
        Assert.Equal(3, children[fork]);
        Assert.InRange(nodes[fork].Place.Y, 1.8f, 2.4f);
        var tips = Enumerable.Range(0, nodes.Count).Where(i => children[i] == 0).Select(i => nodes[i].Place).ToArray();
        Assert.Equal(3, tips.Length);
        Assert.All(tips, tip => Assert.InRange(tip.Y, 6f, 8.01f));
        Assert.True(tips.Min(tip => tips.Where(other => other != tip).Min(other => (other - tip).Length)) > 0.3f, "the trunks run on together");
    }

    [Fact]
    public void AWillowKeepsInsideItsPruningEnvelope()
    {
        // The envelope is four tenths of the scale at its widest, and the scale twenty meters at most.
        var widest = 0.4f * 20f;
        foreach (var node in Trees.Grow(TreeSpecies.WeepingWillow, 2).Nodes)
        {
            Assert.True(MathF.Sqrt(node.Place.X * node.Place.X + node.Place.Z * node.Place.Z) <= widest, $"a point stands at {node.Place}, outside the envelope");
        }
    }

    [Fact]
    public void ATreeGrowsRoundABoxAndAwayFromIt()
    {
        // A wall a meter from the trunk along X, past the aspen's crown in height and breadth.
        (Vec3 Low, Vec3 High) wall = (new Vec3(1f, 0f, -20f), new Vec3(3f, 40f, 20f));
        var open = Trees.Grow(Aspen, 5).Nodes;
        var walled = Trees.Grow(Aspen, 5, [wall]).Nodes;

        Assert.All(walled, node => Assert.False(node.Place.X >= wall.Low.X && node.Place.X <= wall.High.X, $"a point stands in the wall at {node.Place}"));
        // The wall takes the sky from the crown's side next to it, so the crown stands away from it by
        // more than it loses to it, half a meter and more where the open tree stands about the trunk.
        Assert.True(walled.Average(node => node.Place.X) < open.Average(node => node.Place.X) - 0.5f, "the crown does not lean away from the wall");
        Assert.Equal(walled, Trees.Grow(Aspen, 5, [wall]).Nodes);
    }

    [Fact]
    public void ATreeBuildsIntoClosedTubesOfBark()
    {
        var skeleton = Trees.Grow(Aspen, 3);
        var mesh = Trees.Build(skeleton);
        Assert.Equal([Trees.BarkSlot], mesh.Slots);
        Assert.True(mesh.IsClosed, $"{mesh.OpenEdges} edges of the tree are open");

        var (low, high) = mesh.Bounds;
        var (skeletonLow, skeletonHigh) = skeleton.Bounds;
        var trunk = skeleton.Nodes[0].Radius;
        Assert.True(low.Y >= skeletonLow.Y - trunk && high.Y <= skeletonHigh.Y + trunk, $"the mesh runs from {low} to {high} and the skeleton from {skeletonLow} to {skeletonHigh}");

        // The mesh's recipe makes it again, a line a stem.
        var again = EditMesh.FromRecipe(mesh.RecipeText);
        Assert.Equal(mesh.VertexCount, again.VertexCount);
        Assert.Equal(mesh.Position(mesh.VertexCount - 1), again.Position(again.VertexCount - 1));
    }

    [Fact]
    public void AnAspenKeepsWithinItsTriangles()
    {
        output.WriteLine("| Seed | Points | Tips | Height | Spread | Trunk's radius | Triangles |");
        output.WriteLine("|---|---|---|---|---|---|---|");
        for (var seed = 1u; seed <= 6; seed++)
        {
            var skeleton = Trees.Grow(Aspen, seed);
            var nodes = skeleton.Nodes;
            var tips = nodes.Count - nodes.Skip(1).Select(node => node.Parent).Distinct().Count();
            var spread = nodes.Max(node => MathF.Sqrt(node.Place.X * node.Place.X + node.Place.Z * node.Place.Z));

            // Twelve sides on the trunk and as few as three on a twig, the paper's tree without its
            // leaves in some tens of thousands of triangles.
            var triangles = Modeling.Build(Trees.Build(skeleton))[0].Indices!.Length / 3;
            Assert.InRange(triangles, 5_000, 40_000);
            output.WriteLine($"| {seed} | {nodes.Count} | {tips} | {skeleton.Bounds.High.Y:0.0} m | {spread:0.0} m | {nodes[0].Radius * 100f:0.0} cm | {triangles} |");
        }
    }

    [Fact]
    public void AReducedTreeKeepsItsTrunkAndLimbsInFewerTriangles()
    {
        foreach (var species in new[] { Aspen, TreeSpecies.BlackTupelo, TreeSpecies.CaliforniaBlackOak })
        {
            var skeleton = Trees.Grow(species, 2);
            var full = Trees.Build(skeleton);
            var reduced = Trees.Build(skeleton, TreeDetail.Reduced);
            var fullTriangles = Modeling.Build(full)[0].Indices!.Length / 3;
            var reducedTriangles = Modeling.Build(reduced)[0].Indices!.Length / 3;

            // Half the sides and the thinnest stems left out, the tree as tall within a fiftieth,
            // its top a twig over the trunk's tip or a ring of fewer sides turned another way, and
            // closed.
            Assert.InRange(reducedTriangles, fullTriangles / 10, fullTriangles / 2);
            Assert.InRange(reduced.Bounds.High.Y, 0.98f * full.Bounds.High.Y, 1.02f * full.Bounds.High.Y);
            Assert.True(reduced.IsClosed, $"{reduced.OpenEdges} edges of the reduced tree are open");
            output.WriteLine($"{species.LeafSet}'s tree, {fullTriangles} triangles of stem in full and {reducedTriangles} reduced");
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Build(Trees.Grow(Aspen, 2), (TreeDetail)7));
    }

    [Fact]
    public void ASpeciesOutsideItsRangeIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { Scale = -1f }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { ScaleVariation = 14f }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { BaseSplits = -1 }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { Shape = (TreeShape)99 }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { PruneRatio = 2f }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { PruneWidthPeak = 0f }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen, 1, [(Vec3.One, Vec3.Zero)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { BaseSize = 1f }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { Ratio = float.NaN }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { Levels = [] }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { Levels = [.. Aspen.Levels, new(), new()] }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { Levels = [new TreeLevel { Taper = 3.5f }] }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { Levels = [new TreeLevel(), new TreeLevel { Branches = -1 }] }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { Levels = [new TreeLevel { Curve = float.PositiveInfinity }] }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { Levels = [new TreeLevel { Length = 0f }] }, 1));
    }

    [Fact]
    public void ASkeletonOutOfOrderIsRefused()
    {
        Assert.Throws<ArgumentException>(() => new TreeSkeleton([]));
        Assert.Throws<ArgumentException>(() => new TreeSkeleton([new TreeNode(0, Vec3.Zero, 0.1f)]));
        Assert.Throws<ArgumentException>(() => new TreeSkeleton([new TreeNode(-1, Vec3.Zero, 0.1f), new TreeNode(1, Vec3.UnitY, 0.1f)]));
        Assert.Throws<ArgumentException>(() => new TreeSkeleton([new TreeNode(-1, Vec3.Zero, 0f)]));

        // A skeleton kept in a file builds as one grown does.
        var kept = new TreeSkeleton([new TreeNode(-1, Vec3.Zero, 0.2f), new TreeNode(0, Vec3.UnitY, 0.1f), new TreeNode(1, new Vec3(0.5f, 1.5f, 0f), 0.05f), new TreeNode(1, new Vec3(0f, 2f, 0f), 0.08f)]);
        var mesh = Trees.Build(kept);
        Assert.True(mesh.IsClosed);

        // The trunk carried on through the thicker child in twelve sides, two lengths and two ends,
        // and the thinner child a stem of its own in seven, one length and two ends.
        Assert.Equal(2 * 12 + 2 + 7 + 2, mesh.FaceCount);
    }
}
