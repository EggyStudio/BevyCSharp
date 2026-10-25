using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// A grown tree's leaves by <see cref="Trees.BuildLeaves(TreeSkeleton, TreeSpecies, uint, TreeDetail)"/>, the same by seed, as many as the
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
    public void AReducedCrownIsEveryOtherLeafAtItsPlaceTwiceTheArea()
    {
        var skeleton = Trees.Grow(Aspen, 4);
        var full = Trees.BuildLeaves(skeleton, Aspen, 4);
        var reduced = Trees.BuildLeaves(skeleton, Aspen, 4, TreeDetail.Reduced);
        var cards = full.Positions.Length / 4;
        Assert.Equal(cards / 2, reduced.Positions.Length / 4);
        Assert.Equal(cards / 2 * 6, reduced.Indices!.Length);

        static Vec3 Stalk(Vec3[] positions, int card) => (positions[card * 4] + positions[card * 4 + 1]) * 0.5f;
        static float Area(Vec3[] positions, int card) =>
            Vec3.Cross(positions[card * 4 + 1] - positions[card * 4], positions[card * 4 + 3] - positions[card * 4]).Length;

        for (var card = 0; card < cards / 2; card++)
        {
            Assert.True((Stalk(reduced.Positions, card) - Stalk(full.Positions, 2 * card)).Length < 1e-4f, $"card {card} stands off its leaf's stalk");
            Assert.InRange(Area(reduced.Positions, card) / Area(full.Positions, 2 * card), 1.999f, 2.001f);
        }

        // A fern's fronds are its whole leaves and are kept.
        var fern = Trees.Grow(TreeSpecies.Fern, 4);
        Assert.Equal(Trees.BuildLeaves(fern, TreeSpecies.Fern, 4).Positions, Trees.BuildLeaves(fern, TreeSpecies.Fern, 4, TreeDetail.Reduced).Positions);
        Assert.Throws<ArgumentOutOfRangeException>(() => Trees.BuildLeaves(skeleton, Aspen, 4, (TreeDetail)7));
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
    [SkippableFact]
    public void ACrownKeepsItsLeavesWithDistance()
    {
        Needs.Renderer();

        // Each preset alone against the sky, from 37 degrees above and from the side, at a distance
        // that holds its crown in the picture and at twice and four times it. A crown's leaves cover
        // the same share of it however far it stands, so its leaves' pixels times the distance
        // squared hold, where a leaf a card samples from a level of its atlas a texel or two across
        // would be lost to the transparent texels round it and the crown thin away.
        TreeSpecies[] presets = [TreeSpecies.QuakingAspen, TreeSpecies.BlackTupelo, TreeSpecies.WeepingWillow, TreeSpecies.CaliforniaBlackOak, TreeSpecies.Bush, TreeSpecies.Fern];
        var crowns = presets.Select(species =>
        {
            var skeleton = Trees.Grow(species, 2);
            var leaves = Trees.BuildLeaves(skeleton, species, 2);
            var low = leaves.Positions.Aggregate((a, b) => new Vec3(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Min(a.Z, b.Z)));
            var high = leaves.Positions.Aggregate((a, b) => new Vec3(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y), MathF.Max(a.Z, b.Z)));
            var size = MathF.Max(high.Y - low.Y, MathF.Max(high.X - low.X, high.Z - low.Z));
            return (Species: species, Skeleton: skeleton, Leaves: leaves, Middle: (low + high) * 0.5f, Near: 1.6f * size);
        }).ToArray();
        (string Name, Vec3 Way)[] ways = [("above", new Vec3(-0.6f, 0.6f, -0.53f).Normalized), ("the side", new Vec3(-1f, 0.05f, -0.4f).Normalized)];
        float[] times = [1f, 2f, 4f];

        Entity camera = default;
        var shown = new List<Entity[]>();
        var albedos = new List<AssetHandle>();
        var run = new PictureRun
        {
            Width = 640,
            Height = 360,
            Frames = 4000,
            Scene = ecs =>
            {
                camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 45f });
                ecs.Add(camera, Transform.Identity);
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetClearColor((0.6f, 0.75f, 0.95f, 1f));
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f });
                ecs.Add(sun, Transform.LookingAt(new Vec3(3f, 6f, 5f), Vec3.Zero, Vec3.UnitY));
                foreach (var crown in crowns)
                {
                    var stems = ecs.Spawn();
                    Render.SetMesh(ecs, stems, Modeling.Meshes(Trees.Build(crown.Skeleton))[0]);
                    Render.SetMaterial(ecs, stems, Trees.BarkMaterial(crown.Species));
                    var cards = ecs.Spawn();
                    Render.SetMesh(ecs, cards, Render.CreateMesh(crown.Leaves));
                    Render.SetMaterial(ecs, cards, Trees.LeafMaterial(crown.Species));
                    foreach (var part in new[] { stems, cards })
                    {
                        ecs.Add(part, Transform.Identity);
                        ecs.Add(part, Visibility.Hidden);
                    }

                    shown.Add([stems, cards]);
                    albedos.Add(TreeTextures.Find(crown.Species.LeafSet!)!.Load().Albedo);
                }
            },
        };

        run.Wait(10).Until("ready", _ => Render.PipelinesReady())
            .Until("loaded", _ => albedos.All(image => AssetServer.StateOf(image) is AssetLoadState.Loaded or AssetLoadState.Failed));
        for (var k = 0; k < crowns.Length; k++)
        {
            var (index, crown) = (k, crowns[k]);
            run.Do($"show {k}", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                for (var other = 0; other < shown.Count; other++)
                    foreach (var part in shown[other])
                        ecs.GetRef<Visibility>(part) = other == index ? Visibility.Inherited : Visibility.Hidden;
            });
            foreach (var (name, way) in ways)
                foreach (var time in times)
                {
                    var eye = crown.Middle + way * (crown.Near * time);
                    run.Do($"{k} {name} {time}", world => world.Resource<EcsWorld>().GetRef<Transform>(camera) = Transform.LookingAt(eye, crown.Middle, Vec3.UnitY))
                        .Wait(20)
                        .Capture($"{k} {name} {time}");
                }
        }

        run.Go();

        var thinned = new List<string>();
        for (var k = 0; k < crowns.Length; k++)
            foreach (var (name, _) in ways)
            {
                var kept = times.Select(time =>
                {
                    var picture = run.Picture($"{k} {name} {time}");
                    var leafy = PictureRun.Count(picture, (r, g, b) => g > r && g > b + 8);
                    return leafy * time * time;
                }).ToArray();
                output.WriteLine($"{crowns[k].Species.LeafSet} from {name}, {crowns[k].Near:F1} m and twice and four times it: {string.Join(", ", kept.Select(value => $"{value / kept[0]:F2}"))} of the nearest's leaves for the crown's size");
                if (kept.Skip(1).Any(value => value < 0.75f * kept[0])) thinned.Add($"{crowns[k].Species.LeafSet} from {name}");
            }

        Assert.True(thinned.Count == 0, $"the crowns thin with distance, {string.Join(", ", thinned)}");
    }
    [SkippableFact]
    public void AReducedTreeCoversItsCrownAsTheFullOneDoesWhereTheyChange()
    {
        Needs.Renderer();

        // Each tree drawn whole and drawn reduced, alone against the sky from the side and from 37
        // degrees above at two and a half of its heights, where a planted tree is dithered from one
        // to the other. The reduced tree covers as many pixels within a tenth and as many of leaf
        // within a sixth, its larger cards a little more, where the twigs it leaves out showed
        // between the leaves as lines a pixel wide, so the change does not show.
        TreeSpecies[] presets = [TreeSpecies.QuakingAspen, TreeSpecies.BlackTupelo, TreeSpecies.WeepingWillow, TreeSpecies.CaliforniaBlackOak, TreeSpecies.Bush];
        TreeDetail[] levels = [TreeDetail.Full, TreeDetail.Reduced];
        var trees = presets.Select(species =>
        {
            var skeleton = Trees.Grow(species, 2);
            var height = skeleton.Bounds.High.Y;
            return (Species: species, Skeleton: skeleton, Middle: new Vec3(0f, height * 0.55f, 0f), Distance: 2.5f * height);
        }).ToArray();
        (string Name, Vec3 Way)[] ways = [("the side", new Vec3(-1f, 0.05f, -0.4f).Normalized), ("above", new Vec3(-0.6f, 0.6f, -0.53f).Normalized)];

        Entity camera = default;
        var shown = new List<Entity[]>();
        var albedos = new List<AssetHandle>();
        var run = new PictureRun
        {
            Width = 480,
            Height = 360,
            Frames = 3000,
            Scene = ecs =>
            {
                camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 45f });
                ecs.Add(camera, Transform.Identity);
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetClearColor((0.6f, 0.75f, 0.95f, 1f));
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f });
                ecs.Add(sun, Transform.LookingAt(new Vec3(3f, 6f, 5f), Vec3.Zero, Vec3.UnitY));
                foreach (var tree in trees)
                    foreach (var level in levels)
                    {
                        var stems = ecs.Spawn();
                        Render.SetMesh(ecs, stems, Modeling.Meshes(Trees.Build(tree.Skeleton, level))[0]);
                        Render.SetMaterial(ecs, stems, Trees.BarkMaterial(tree.Species));
                        var cards = ecs.Spawn();
                        Render.SetMesh(ecs, cards, Render.CreateMesh(Trees.BuildLeaves(tree.Skeleton, tree.Species, 2, level)));
                        Render.SetMaterial(ecs, cards, Trees.LeafMaterial(tree.Species));
                        foreach (var part in new[] { stems, cards })
                        {
                            ecs.Add(part, Transform.Identity);
                            ecs.Add(part, Visibility.Hidden);
                        }

                        shown.Add([stems, cards]);
                        albedos.Add(TreeTextures.Find(tree.Species.LeafSet!)!.Load().Albedo);
                    }
            },
        };

        run.Wait(10).Until("ready", _ => Render.PipelinesReady())
            .Until("loaded", _ => albedos.All(image => AssetServer.StateOf(image) is AssetLoadState.Loaded or AssetLoadState.Failed));
        for (var k = 0; k < trees.Length * levels.Length; k++)
        {
            var (index, tree) = (k, trees[k / levels.Length]);
            run.Do($"show {k}", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                for (var other = 0; other < shown.Count; other++)
                    foreach (var part in shown[other])
                        ecs.GetRef<Visibility>(part) = other == index ? Visibility.Inherited : Visibility.Hidden;
            });
            foreach (var (name, way) in ways)
            {
                var eye = tree.Middle + way * tree.Distance;
                run.Do($"{k} {name}", world => world.Resource<EcsWorld>().GetRef<Transform>(camera) = Transform.LookingAt(eye, tree.Middle, Vec3.UnitY))
                    .Wait(20)
                    .Capture($"{k} {name}");
            }
        }

        run.Go();

        var apart = new List<string>();
        for (var t = 0; t < trees.Length; t++)
            foreach (var (name, _) in ways)
            {
                // A leaf's pixel is greener than it is red and blue, and the rest that is not the
                // sky is the stems' or a leaf's in shade.
                (int Leaves, int Stems) Counted(int level)
                {
                    var picture = run.Picture($"{t * levels.Length + level} {name}");
                    var sky = picture.At(0, 0);
                    var leaves = PictureRun.Count(picture, (r, g, b) => g > r && g > b + 8);
                    var any = PictureRun.Count(picture, (r, g, b) => Math.Abs(r - sky.R) + Math.Abs(g - sky.G) + Math.Abs(b - sky.B) > 30);
                    return (leaves, any - leaves);
                }

                var (full, reduced) = (Counted(0), Counted(1));
                output.WriteLine($"{trees[t].Species.LeafSet}'s tree from {name} at {trees[t].Distance:F1} m, leaves {full.Leaves} whole and {reduced.Leaves} reduced, stems {full.Stems} and {reduced.Stems}");
                var (fullTree, reducedTree) = (full.Leaves + full.Stems, reduced.Leaves + reduced.Stems);
                if (Math.Abs(reducedTree - fullTree) > fullTree / 10 || Math.Abs(reduced.Leaves - full.Leaves) > full.Leaves / 6)
                    apart.Add($"{trees[t].Species.LeafSet} from {name}");
            }

        Assert.True(apart.Count == 0, $"the reduced trees differ from the whole ones, {string.Join(", ", apart)}");
    }
}
