using System.Numerics;
using Bevy;
using Vegetation;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// Trees the Vegetation module grows built by <see cref="Trees.Build(TreeSkeleton, TreeDetail)"/>,
/// their stems closed tubes of bark, their triangles, their reduced level and a skeleton kept in a
/// file, the growth's own tests being the module's.
/// </summary>
public sealed class TreeTests(ITestOutputHelper output)
{
    private static readonly TreeSpecies Aspen = TreeSpecies.QuakingAspen;

    [Fact]
    public void ATreeBuildsIntoClosedTubesOfBark()
    {
        var skeleton = TreeSkeleton.Grow(Aspen, 3);
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
            var skeleton = TreeSkeleton.Grow(Aspen, seed);
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
            var skeleton = TreeSkeleton.Grow(species, 2);
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

        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Build(TreeSkeleton.Grow(Aspen, 2), (TreeDetail)7));
    }

    // A skeleton kept in a file builds as one grown does.
    [Fact]
    public void AKeptSkeletonBuildsAsOneGrownDoes()
    {
        var kept = new TreeSkeleton([new TreeNode(-1, Vector3.Zero, 0.2f), new TreeNode(0, Vector3.UnitY, 0.1f), new TreeNode(1, new Vector3(0.5f, 1.5f, 0f), 0.05f), new TreeNode(1, new Vector3(0f, 2f, 0f), 0.08f)]);
        var mesh = Trees.Build(kept);
        Assert.True(mesh.IsClosed);

        // The trunk carried on through the thicker child in twelve sides, two lengths and two ends,
        // and the thinner child a stem of its own in seven, one length and two ends.
        Assert.Equal(2 * 12 + 2 + 7 + 2, mesh.FaceCount);
    }
}
