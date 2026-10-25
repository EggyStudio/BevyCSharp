using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// A grown tree's leaves by <see cref="Trees.BuildLeaves"/>, the same by seed, as many as the
/// paper's within their bound, standing on the twigs and facing up and out, a fern's as fronds along
/// its stems, and the bark's and the leaves' materials, a tree in leaf drawn.
/// </summary>
[Collection("engine")]
public sealed class TreeLeavesTests(ITestOutputHelper output)
{
    private static readonly TreeSpecies Aspen = TreeSpecies.QuakingAspen;

    [Fact]
    public void ASeedPlacesTheSameLeavesEachTime()
    {
        var skeleton = Trees.Grow(Aspen, 4);
        var first = Trees.BuildLeaves(skeleton, Aspen, 4);
        var again = Trees.BuildLeaves(skeleton, Aspen, 4);
        Assert.Equal(first.Positions, again.Positions);
        Assert.Equal(first.Uvs, again.Uvs);
        Assert.NotEqual(first.Positions, Trees.BuildLeaves(skeleton, Aspen, 5).Positions);
    }

    [Fact]
    public void ATreeCarriesThePapersLeavesUpToTheirBound()
    {
        // The aspen's paper carries 25 leaves on each of its 50 branches' 30 twigs, at six tenths.
        var aspen = Trees.BuildLeaves(Trees.Grow(Aspen, 3), Aspen, 3);
        Assert.InRange(aspen.Indices!.Length / 6, 0.8 * 25 * 50 * 30 * 0.6, 1.2 * 25 * 50 * 30 * 0.6);

        // The willow's paper carries many more than a tree's cards hold, which are drawn larger.
        var willow = Trees.BuildLeaves(Trees.Grow(TreeSpecies.WeepingWillow, 3), TreeSpecies.WeepingWillow, 3);
        Assert.InRange(willow.Indices!.Length / 6, 20_000, 31_000);

        var bare = Trees.BuildLeaves(Trees.Grow(Aspen with { Leaves = 0 }, 3), Aspen with { Leaves = 0 }, 3);
        Assert.Empty(bare.Positions);
    }

    [Fact]
    public void EveryPresetsLeavesKeepToTheirBound()
    {
        output.WriteLine("| Preset | Leaves on each twig, the paper's | Twigs | Cards | Triangles of the cards | Triangles of the stems |");
        output.WriteLine("|---|---|---|---|---|---|");
        foreach (var (name, species) in new[] { ("quaking aspen", TreeSpecies.QuakingAspen), ("black tupelo", TreeSpecies.BlackTupelo), ("weeping willow", TreeSpecies.WeepingWillow), ("California black oak", TreeSpecies.CaliforniaBlackOak), ("bush", TreeSpecies.Bush), ("fern", TreeSpecies.Fern) })
        {
            var skeleton = Trees.Grow(species, 2);
            var leaves = Trees.BuildLeaves(skeleton, species, 2);
            var nodes = skeleton.Nodes;
            var children = new int[nodes.Count];
            for (var i = 1; i < nodes.Count; i++) children[nodes[i].Parent]++;
            var twigs = Enumerable.Range(0, nodes.Count).Count(i => children[i] == 0);

            // A card is a leaf of four corners, and a frond strip two corners a point of its stem.
            var cards = species.Leaves > 0 ? leaves.Positions.Length / 4 : twigs;
            Assert.True(cards <= 30_000, $"{name} has {cards} cards");
            var stems = Modeling.Build(Trees.Build(skeleton))[0].Indices!.Length / 3;
            output.WriteLine($"| {name} | {species.Leaves} | {twigs} | {cards} | {leaves.Indices!.Length / 3} | {stems} |");
        }
    }

    [Fact]
    public void LeavesStandOnTheTwigsAndFaceUpAndOut()
    {
        var skeleton = Trees.Grow(Aspen, 2);
        var leaves = Trees.BuildLeaves(skeleton, Aspen, 2);
        var points = skeleton.Nodes.Select(node => node.Place).ToArray();

        // A card's stalk, the middle of its first two corners, stands within the spread and a leaf's
        // length, at the most enlarged, of a point of the tree.
        var reach = Aspen.LeafScale * 1.6f * 1.1f * 2.5f + 0.5f;
        var upward = 0;
        for (var card = 0; card < leaves.Positions.Length / 4; card++)
        {
            var stalk = (leaves.Positions[4 * card] + leaves.Positions[4 * card + 1]) * 0.5f;
            Assert.True(points.Any(point => (point - stalk).LengthSquared < reach * reach), $"card {card} stands at {stalk}, off the tree");

            // Every card faces the side of up and away from the trunk, and most face up, half
            // turned there by the aspen's bend of one half and half as they leave the twig.
            var normal = leaves.Normals![4 * card];
            var outward = new Vec3(stalk.X, 0f, stalk.Z);
            var light = Vec3.UnitY + (outward.LengthSquared > 1e-6f ? outward.Normalized * 0.5f : Vec3.Zero);
            Assert.True(Vec3.Dot(normal, light) >= -1e-4f, $"card {card} faces {normal}, away from the light");
            if (normal.Y > 0f) upward++;
        }

        Assert.True(upward > 0.55 * leaves.Positions.Length / 4, $"{upward} of {leaves.Positions.Length / 4} cards face up");
        Assert.All(leaves.Uvs!, uv => Assert.InRange(uv, 0f, 1f));
    }

    [Fact]
    public void AFernsFrondsRunTheLengthOfItsStems()
    {
        var fern = TreeSpecies.Fern;
        var skeleton = Trees.Grow(fern, 1);
        var nodes = skeleton.Nodes;
        var children = new int[nodes.Count];
        for (var i = 1; i < nodes.Count; i++) children[nodes[i].Parent]++;
        var tips = Enumerable.Range(0, nodes.Count).Count(i => children[i] == 0);

        // A strip a frond, two corners at each point of its stem from its foot, two triangles between
        // points, so a strip has a point more than its lengths and the strips are as many as the tips.
        var fronds = Trees.BuildLeaves(skeleton, fern, 1);
        Assert.Equal(14, tips);
        var corners = fronds.Positions.Length / 2;
        var lengths = fronds.Indices!.Length / 6;
        Assert.Equal(tips, corners - lengths);
        Assert.True(lengths > 10 * tips, $"the fronds run {lengths} lengths of their stems");
    }

    [Fact]
    public void LeafControlsOutOfRangeAreRefused()
    {
        var skeleton = Trees.Grow(Aspen, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.BuildLeaves(skeleton, Aspen with { LeafScale = 0f }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.BuildLeaves(skeleton, Aspen with { LeafBend = 2f }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.BuildLeaves(skeleton, Aspen with { LeafShape = (TreeLeafShape)99 }, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.Grow(Aspen with { LeafLevel = new TreeLevel { Rotate = float.NaN } }, 1));
    }

    [SkippableFact]
    public void ATreeInLeafCoversItsCrownWithItsSetsOrTheGeneratedLooks()
    {
        Needs.Renderer();
        var skeleton = Trees.Grow(Aspen, 2);
        var leaves = Trees.BuildLeaves(skeleton, Aspen, 2);
        var generated = Aspen with { BarkSet = null, LeafSet = "no-such-leaves", LeafShape = TreeLeafShape.ThreeLobeMaple };
        var generatedLeaves = Trees.BuildLeaves(skeleton, generated, 2);
        var made = new List<(AssetHandle Bark, AssetHandle Leaves, AssetHandle Again, AssetHandle Fall)>();

        Entity crown = default;
        var run = new PictureRun
        {
            Width = 160,
            Height = 180,
            Frames = 600,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 45f });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 8f, 30f), new Vec3(0f, 8f, 0f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetClearColor((0.6f, 0.75f, 0.95f, 1f));
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f });
                ecs.Add(sun, Transform.LookingAt(new Vec3(3f, 6f, 5f), Vec3.Zero, Vec3.UnitY));

                foreach (var (species, mesh, x) in new[] { (Aspen, leaves, -6f), (generated, generatedLeaves, 6f) })
                {
                    var tree = ecs.Spawn();
                    Render.SetMesh(ecs, tree, Modeling.Meshes(Trees.Build(skeleton))[0]);
                    Render.SetMaterial(ecs, tree, Trees.BarkMaterial(species));
                    ecs.Add(tree, Transform.At(x, 0f, 0f));
                    crown = ecs.Spawn();
                    Render.SetMesh(ecs, crown, Render.CreateMesh(mesh));
                    Render.SetMaterial(ecs, crown, Trees.LeafMaterial(species));
                    ecs.Add(crown, Transform.At(x, 0f, 0f));
                    made.Add((Trees.BarkMaterial(species), Trees.LeafMaterial(species), Trees.LeafMaterial(species, 0f), Trees.LeafMaterial(species, 1f)));
                }
            },
        };
        run.Wait(10).Until("ready", _ => Render.PipelinesReady()).Wait(30).Capture("trees").Go();

        // Each look is made once and a fall share is another material.
        Assert.All(made, look => Assert.Equal(look.Leaves, look.Again));
        Assert.All(made, look => Assert.NotEqual(look.Leaves, look.Fall));
        Assert.NotEqual(made[0].Bark, made[1].Bark);

        // Both crowns, the sets' and the generated, hide much of the sky behind them.
        var picture = run.Picture("trees");
        foreach (var (from, to) in new[] { (20u, 70u), (90u, 140u) })
        {
            var covered = 0;
            var total = 0;
            for (var y = 30u; y < 110u; y++)
            for (var x = from; x < to; x++, total++)
            {
                var texel = picture.At(x, y);
                if (texel.B < texel.G + 20) covered++;
            }

            Assert.True(covered > total / 5, $"{covered} of {total} pixels of the crown between {from} and {to} hide the sky");
        }
    }
}
