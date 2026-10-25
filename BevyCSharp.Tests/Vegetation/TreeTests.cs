using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// Trees grown by <see cref="Trees.Grow"/> and built by <see cref="Trees.Build"/>, their sameness by
/// seed, their size within their species, their thickness by the pipe model and their meshes.
/// </summary>
public sealed class TreeTests(ITestOutputHelper output)
{
    /// <summary>A tree near the paper's quaking aspen, its leaves left out.</summary>
    internal static readonly TreeSpecies Aspen = new()
    {
        Shape = TreeShape.TendFlame,
        Scale = 13f,
        ScaleVariation = 3f,
        BaseSize = 0.4f,
        Ratio = 0.015f,
        AttractionUp = 0.5f,
        Levels =
        [
            new TreeLevel { CurveVariation = 20f },
            new TreeLevel { DownAngle = 60f, DownAngleVariation = -50f, Branches = 50, Length = 0.3f, Curve = -40f, CurveVariation = 50f },
            new TreeLevel { DownAngle = 45f, DownAngleVariation = 10f, Branches = 30, Length = 0.6f, Curve = -40f, CurveVariation = 75f },
        ],
    };

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

    [Fact]
    public void ATreeStandsWithinItsSpeciesHeightAndCrown()
    {
        for (var seed = 1u; seed <= 6; seed++)
        {
            var skeleton = Trees.Grow(Aspen, seed);
            var nodes = skeleton.Nodes;
            var (low, high) = skeleton.Bounds;

            // Thirteen meters three either way, the trunk's tip within a few internodes of it.
            Assert.InRange(high.Y, 9f, 16.5f);
            Assert.True(low.Y >= 0f, $"seed {seed} grew a stem into the ground, to {low.Y}");

            // The widest reach is the first level's length of the trunk, sixty degrees out, with the
            // twigs that perceive the envelope's edge from inside it.
            var reach = 0.3f * high.Y * MathF.Sin(MathF.PI / 3f);
            var spread = nodes.Max(node => MathF.Sqrt(node.Place.X * node.Place.X + node.Place.Z * node.Place.Z));
            Assert.InRange(spread, 0.5f * reach, reach + 0.2f * high.Y);

            // The trunk's foot is as thick for the trunk's height as the ratio makes it.
            Assert.InRange(nodes[0].Radius / high.Y, 0.015f * 0.9f, 0.015f * 1.15f);

            // Below the crown's foot, four tenths up less a little for the lowest branches, which
            // leave at up to 110 degrees and dip, the trunk stands alone.
            var bare = 0.4f * high.Y - 1f;
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
        var nodes = Trees.Grow(Aspen, 3).Nodes;
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
    public void ASpeciesOutsideItsRangeIsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { Scale = -1f }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { ScaleVariation = 13f }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { BaseSize = 1f }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { Ratio = float.NaN }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { Levels = [] }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { Levels = [.. Aspen.Levels, new(), new()] }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { Levels = [new TreeLevel { Taper = 2f }] }, 1));
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
