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
    public void ALeafDeepInACrownTakesLessOfTheSkyThanOneOnItsRimAndCarriesTheWayOut()
    {
        foreach (var species in new[] { Aspen, TreeSpecies.BlackTupelo, TreeSpecies.WeepingWillow, TreeSpecies.CaliforniaBlackOak, TreeSpecies.Bush })
        {
            var leaves = Trees.BuildLeaves(Trees.Grow(species, 2), species, 2);
            var cards = leaves.Positions.Length / 4;
            var middles = Enumerable.Range(0, cards).Select(card => (leaves.Positions[card * 4] + leaves.Positions[card * 4 + 1] + leaves.Positions[card * 4 + 2] + leaves.Positions[card * 4 + 3]) * 0.25f).ToArray();
            var low = middles.Aggregate((a, b) => new Vec3(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Min(a.Z, b.Z)));
            var high = middles.Aggregate((a, b) => new Vec3(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y), MathF.Max(a.Z, b.Z)));
            var (middle, half) = ((low + high) * 0.5f, (high - low) * 0.5f);

            // How far out a leaf stands, one at the crown's bounding ellipsoid, and its shade, the
            // gray its vertices carry.
            float Out(Vec3 place) => MathF.Sqrt(Square((place.X - middle.X) / half.X) + Square((place.Y - middle.Y) / half.Y) + Square((place.Z - middle.Z) / half.Z));
            var shades = Enumerable.Range(0, cards).Select(card => leaves.Colors![card * 16]).ToArray();
            var deep = Enumerable.Range(0, cards).Where(card => Out(middles[card]) < 0.35f).ToArray();
            var rim = Enumerable.Range(0, cards).Where(card => Out(middles[card]) > 0.85f).ToArray();
            var upperRim = rim.Where(card => middles[card].Y > middle.Y).ToArray();

            // Deep in the crown a leaf takes less of the sky than one on its upper rim, the deepest
            // at half and none darker.
            Assert.All(shades, shade => Assert.InRange(shade, 0.5f, 1f));
            var (deepShade, rimShade) = (deep.Average(card => shades[card]), upperRim.Average(card => shades[card]));
            Assert.True(rimShade - deepShade >= 0.12f, $"{species.LeafSet}'s deep leaves are {deepShade:F2} and its upper rim's {rimShade:F2}");
            // The way out of the crown each card carries, unfolded as the leaf material unfolds it,
            // points out of the crown on its rim.
            var outward = rim.Count(card => Vec3.Dot(Unfolded(leaves.SecondUvs![card * 8], leaves.SecondUvs[card * 8 + 1]), middles[card] - middle) > 0f);
            Assert.True(outward >= rim.Length * 85 / 100, $"{outward} of {species.LeafSet}'s {rim.Length} rim leaves carry a way out of the crown");
            output.WriteLine($"{species.LeafSet}, shades {shades.Min():F2} to {shades.Max():F2}, {deep.Length} deep leaves at {deepShade:F2} and {upperRim.Length} on the upper rim at {rimShade:F2}, {outward} of the rim's {rim.Length} with a way out of the crown");
        }

        static float Square(float value) => value * value;
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
    [SkippableFact]
    public void ACrownShadedByTheSkyItSeesDarkensItsMiddleMoreThanItsEdge()
    {
        Needs.Renderer();

        // Each crown in the sun under a pale sky, drawn with each leaf's gray, the share of the sky
        // it sees, and drawn without it, the same cards from the same place, so a pixel of one is
        // the same leaf as the pixel of the other. The gray darkens the leaves seen through the
        // crown's middle, which stand deep in it, more than those at its edge, which stand on its
        // rim.
        TreeSpecies[] presets = [TreeSpecies.QuakingAspen, TreeSpecies.BlackTupelo];
        var crowns = presets.Select(species =>
        {
            var skeleton = Trees.Grow(species, 2);
            var shaded = Trees.BuildLeaves(skeleton, species, 2);
            var plain = new MeshData { Positions = shaded.Positions, Normals = shaded.Normals, Uvs = shaded.Uvs, Indices = shaded.Indices };
            var (low, high) = skeleton.Bounds;
            var size = MathF.Max(high.Y, MathF.Max(high.X - low.X, high.Z - low.Z));
            return (Species: species, Skeleton: skeleton, Plain: plain, Shaded: shaded, Middle: new Vec3(0f, high.Y * 0.6f, 0f), Distance: 1.3f * size);
        }).ToArray();

        Entity camera = default;
        var shown = new List<Entity[]>();
        var albedos = new List<AssetHandle>();
        var run = new PictureRun
        {
            Width = 480,
            Height = 480,
            Frames = 3000,
            Scene = ecs =>
            {
                camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 45f });
                ecs.Add(camera, Transform.Identity);
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetClearColor((0.55f, 0.68f, 0.85f, 1f));
                Render.SetAmbientLight((0.55f, 0.68f, 0.85f), 1500f);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 30_000f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(new Vec3(6f, 8f, 4f), Vec3.Zero, Vec3.UnitY));
                foreach (var crown in crowns)
                    foreach (var leaves in new[] { crown.Plain, crown.Shaded })
                    {
                        var stems = ecs.Spawn();
                        Render.SetMesh(ecs, stems, Modeling.Meshes(Trees.Build(crown.Skeleton))[0]);
                        Render.SetMaterial(ecs, stems, Trees.BarkMaterial(crown.Species));
                        var cards = ecs.Spawn();
                        var mesh = Render.CreateMesh(leaves);
                        Render.GenerateTangents(mesh);
                        Render.SetMesh(ecs, cards, mesh);
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
        for (var k = 0; k < crowns.Length * 2; k++)
        {
            var (index, crown) = (k, crowns[k / 2]);
            var eye = crown.Middle + new Vec3(0f, 0.15f, 1f).Normalized * crown.Distance;
            run.Do($"show {k}", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                for (var other = 0; other < shown.Count; other++)
                    foreach (var part in shown[other])
                        ecs.GetRef<Visibility>(part) = other == index ? Visibility.Inherited : Visibility.Hidden;
                ecs.GetRef<Transform>(camera) = Transform.LookingAt(eye, crown.Middle, Vec3.UnitY);
            }).Wait(30).Capture($"{k}");
        }

        run.Go();

        for (var c = 0; c < crowns.Length; c++)
        {
            var (plain, shaded) = (run.Picture($"{2 * c}"), run.Picture($"{2 * c + 1}"));
            var sky = plain.At(0, 0);
            static float Brightness((byte R, byte G, byte B, byte A) texel) => 0.2126f * texel.R + 0.7152f * texel.G + 0.0722f * texel.B;
            bool Leaf((byte R, byte G, byte B, byte A) texel) => Math.Abs(texel.R - sky.R) + Math.Abs(texel.G - sky.G) + Math.Abs(texel.B - sky.B) > 40 && texel.G > texel.R;

            // The leaves both pictures show, by how far from the middle of the crown's outline
            // each stands, one at its edge.
            var leaves = new List<(float X, float Y, float Plain, float Shaded)>();
            for (var y = 0u; y < plain.Height; y++)
            for (var x = 0u; x < plain.Width; x++)
                if (Leaf(plain.At(x, y)) && Leaf(shaded.At(x, y)))
                    leaves.Add((x, y, Brightness(plain.At(x, y)), Brightness(shaded.At(x, y))));

            var (left, right, top, bottom) = (leaves.Min(leaf => leaf.X), leaves.Max(leaf => leaf.X), leaves.Min(leaf => leaf.Y), leaves.Max(leaf => leaf.Y));
            float Out((float X, float Y, float Plain, float Shaded) leaf) =>
                MathF.Sqrt(MathF.Pow((leaf.X - (left + right) / 2f) / ((right - left) / 2f), 2f) + MathF.Pow((leaf.Y - (top + bottom) / 2f) / ((bottom - top) / 2f), 2f));
            float Kept(IEnumerable<(float X, float Y, float Plain, float Shaded)> some) => some.Sum(leaf => leaf.Shaded) / some.Sum(leaf => leaf.Plain);
            var (middle, edge) = (Kept(leaves.Where(leaf => Out(leaf) < 0.45f)), Kept(leaves.Where(leaf => Out(leaf) > 0.8f)));
            output.WriteLine($"{crowns[c].Species.LeafSet}, the middle keeps {middle:F2} of its brightness with the sky's shade and the edge {edge:F2}");
            Assert.True(middle <= edge - 0.01f, $"{crowns[c].Species.LeafSet}'s middle keeps {middle:F2} and its edge {edge:F2}");
        }
    }
    /// <summary>A way unfolded from the square it was folded onto octahedrally, as the leaf material's shader unfolds it.</summary>
    private static Vec3 Unfolded(float u, float v)
    {
        var (x, z) = (u * 2f - 1f, v * 2f - 1f);
        var y = 1f - MathF.Abs(x) - MathF.Abs(z);
        var t = MathF.Max(-y, 0f);
        x += x >= 0f ? -t : t;
        z += z >= 0f ? -t : t;
        return new Vec3(x, y, z).Normalized;
    }

    [SkippableFact]
    public void ACrownsDiffuseLightBentOutOfItShowsTheSunsSideAndMirrorsNoSky()
    {
        Needs.Renderer();

        // Each crown drawn with the leaf material from cards that carry the way out of the crown,
        // and from the same cards without it, which the material lights by their own normals
        // throughout, as the standard material does. In a grazing sun from the side, the side
        // toward the sun stands out further from the far side with the way out. From below under a
        // high sun, the leaves' color stays the same, the specular kept on the leaves' own normals
        // so the crown mirrors no sky.
        TreeSpecies[] presets = [TreeSpecies.QuakingAspen, TreeSpecies.CaliforniaBlackOak];
        var crowns = presets.Select(species =>
        {
            var skeleton = Trees.Grow(species, 2);
            var bent = Trees.BuildLeaves(skeleton, species, 2);
            var own = new MeshData { Positions = bent.Positions, Normals = bent.Normals, Uvs = bent.Uvs, Colors = bent.Colors, Indices = bent.Indices };
            var (low, high) = skeleton.Bounds;
            var size = MathF.Max(high.Y, MathF.Max(high.X - low.X, high.Z - low.Z));
            return (Species: species, Skeleton: skeleton, Own: own, Bent: bent, Middle: new Vec3(0f, high.Y * 0.6f, 0f), Distance: 1.3f * size, Height: high.Y);
        }).ToArray();

        // The leaves' mean brightness over the third of the crown toward the sun and the third away,
        // and their mean color.
        static (float Far, float Near, (float R, float G, float B) Color) Read(CapturedImage picture)
        {
            var sky = picture.At(0, 0);
            var leaves = new List<(uint X, byte R, byte G, byte B)>();
            for (var y = 0u; y < picture.Height; y++)
            for (var x = 0u; x < picture.Width; x++)
            {
                var (r, g, b, _) = picture.At(x, y);
                if (Math.Abs(r - sky.R) + Math.Abs(g - sky.G) + Math.Abs(b - sky.B) > 40 && g > r) leaves.Add((x, r, g, b));
            }

            static float Brightness((uint X, byte R, byte G, byte B) leaf) => 0.2126f * leaf.R + 0.7152f * leaf.G + 0.0722f * leaf.B;
            var (left, right) = (leaves.Min(leaf => leaf.X), leaves.Max(leaf => leaf.X));
            var third = (right - left) / 3f;
            return (leaves.Where(leaf => leaf.X < left + third).Average(Brightness), leaves.Where(leaf => leaf.X > right - third).Average(Brightness),
                ((float)leaves.Average(leaf => leaf.R), (float)leaves.Average(leaf => leaf.G), (float)leaves.Average(leaf => leaf.B)));
        }

        // In a grazing sun from the right, each crown from the side, and under a high sun the aspen
        // from below its crown.
        var side = Pictures(crowns.Select(crown => (crown.Species, crown.Skeleton, crown.Own, crown.Bent, Eye: crown.Middle + new Vec3(0f, 0.15f, 1f).Normalized * crown.Distance, At: crown.Middle)).ToArray(), new Vec3(10f, 2.2f, 0f), 900f);
        var aspen = crowns[0];
        var below = Pictures([(aspen.Species, aspen.Skeleton, aspen.Own, aspen.Bent, Eye: new Vec3(0f, 1.6f, 9f), At: new Vec3(0f, aspen.Height * 0.7f, 0f))], new Vec3(-3f, 10f, -4f), 1500f);

        for (var c = 0; c < crowns.Length; c++)
        {
            var (own, bent) = (Read(side[c].Own), Read(side[c].Bent));
            output.WriteLine($"{crowns[c].Species.LeafSet} from the side, by its own normals {own.Far:F1} far and {own.Near:F1} toward the sun, bent out of the crown {bent.Far:F1} and {bent.Near:F1}");
            Assert.True(bent.Near - bent.Far >= (c == 0 ? 1.3f : 1f) * (own.Near - own.Far), $"{crowns[c].Species.LeafSet}'s crown bent stands {bent.Near - bent.Far:F1} brighter toward the sun, by its own normals {own.Near - own.Far:F1}");
        }

        var (ownBelow, bentBelow) = (Read(below[0].Own).Color, Read(below[0].Bent).Color);
        output.WriteLine($"the aspen from below, by its own normals {ownBelow.R:F0} {ownBelow.G:F0} {ownBelow.B:F0}, bent out of the crown {bentBelow.R:F0} {bentBelow.G:F0} {bentBelow.B:F0}");
        Assert.True(Math.Abs(bentBelow.B - ownBelow.B) <= 8f && bentBelow.G - bentBelow.B >= ownBelow.G - ownBelow.B - 8f, "the aspen from below takes a cast of the sky");
    }

    /// <summary>Each crown drawn with the leaf material by its own normals and bent, from an eye, in a sun from a way under a sky of a brightness.</summary>
    private static (CapturedImage Own, CapturedImage Bent)[] Pictures((TreeSpecies Species, TreeSkeleton Skeleton, MeshData Own, MeshData Bent, Vec3 Eye, Vec3 At)[] crowns, Vec3 sunFrom, float sky)
    {
        Entity camera = default;
        var shown = new List<Entity[]>();
        var albedos = new List<AssetHandle>();
        var run = new PictureRun
        {
            Width = 480,
            Height = 480,
            Frames = 3000,
            Scene = ecs =>
            {
                camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 50f });
                ecs.Add(camera, Transform.Identity);
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetClearColor((0.55f, 0.68f, 0.85f, 1f));
                Render.SetAmbientLight((0.55f, 0.68f, 0.85f), sky);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 40_000f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(sunFrom, Vec3.Zero, Vec3.UnitY));
                foreach (var crown in crowns)
                    foreach (var leaves in new[] { crown.Own, crown.Bent })
                    {
                        var stems = ecs.Spawn();
                        Render.SetMesh(ecs, stems, Modeling.Meshes(Trees.Build(crown.Skeleton))[0]);
                        Render.SetMaterial(ecs, stems, Trees.BarkMaterial(crown.Species));
                        var cards = ecs.Spawn();
                        var mesh = Render.CreateMesh(leaves);
                        Render.GenerateTangents(mesh);
                        Render.SetMesh(ecs, cards, mesh);
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
        for (var k = 0; k < crowns.Length * 2; k++)
        {
            var (index, crown) = (k, crowns[k / 2]);
            run.Do($"show {k}", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                for (var other = 0; other < shown.Count; other++)
                    foreach (var part in shown[other])
                        ecs.GetRef<Visibility>(part) = other == index ? Visibility.Inherited : Visibility.Hidden;
                ecs.GetRef<Transform>(camera) = Transform.LookingAt(crown.Eye, crown.At, Vec3.UnitY);
            }).Wait(30).Capture($"{k}");
        }

        run.Go();
        return [.. Enumerable.Range(0, crowns.Length).Select(c => (run.Picture($"{2 * c}"), run.Picture($"{2 * c + 1}")))];
    }
    [SkippableFact]
    public void TheLeavesLitAsACrownCostIsReadInAForest()
    {
        Needs.Shaders();
        var standard = LeavesOpaquePass(crown: false);
        var crown = LeavesOpaquePass(crown: true);
        Skip.If(standard is null || crown is null, "needs a GPU whose passes the app can time");
        output.WriteLine($"24 black tupelos at 1920 by 1080, the opaque pass {standard:0.00} ms with the leaves lit by the standard material and {crown:0.00} ms lit as a crown");
    }

    /// <summary>
    /// The opaque pass's time, smoothed over the frames before, drawing a forest of tupelos at 1080p
    /// with their leaves lit by the standard material, once a pixel, or by the leaf material as a
    /// crown, twice.
    /// </summary>
    private static double? LeavesOpaquePass(bool crown)
    {
        var species = TreeSpecies.BlackTupelo;
        var skeletons = new[] { Trees.Grow(species, 1), Trees.Grow(species, 2), Trees.Grow(species, 3) };
        IReadOnlyList<PassTiming> timings = [];
        var run = new PictureRun
        {
            Width = 1920,
            Height = 1080,
            Frames = 900,
            Configure = config => config.GpuTimings = true,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 60f, Far = 5_000f });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1.7f, 12f), new Vec3(0f, 4f, -20f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAtmosphere(camera, new AtmosphereSettings());
                Render.SetSkyLighting(camera);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 12_500f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(new Vec3(-3f, 5f, 2f), Vec3.Zero, Vec3.UnitY));

                // The standard material made as the leaf material is, from the same leaf set.
                var leaves = Trees.LeafMaterial(species);
                if (!crown)
                {
                    var (albedo, normal, surface) = TreeTextures.Find(species.LeafSet!)!.Load();
                    leaves = Render.CreateMaterial(new MaterialSettings
                    {
                        BaseColorTexture = albedo,
                        NormalMap = normal,
                        OcclusionTexture = surface,
                        MetallicRoughnessTexture = surface,
                        DiffuseTransmissionTexture = surface,
                        DiffuseTransmission = 1f,
                        Roughness = 1f,
                        Metallic = 1f,
                        AlphaMode = AlphaMode.Mask,
                        DoubleSided = true,
                    });
                }

                var bark = Trees.BarkMaterial(species);
                var stems = skeletons.Select(skeleton => Modeling.Meshes(Trees.Build(skeleton))[0]).ToArray();
                var crowns = skeletons.Select((skeleton, i) => Render.CreateMesh(Trees.BuildLeaves(skeleton, species, (uint)(i + 1)))).ToArray();

                // Six trees a row in four rows going away, each a third of the three trees grown.
                for (var i = 0; i < 24; i++)
                {
                    var at = Transform.At((i % 6 - 2.5f) * 7f + i / 6 % 2 * 3f, 0f, -(i / 6) * 9f);
                    foreach (var (mesh, material) in new[] { (stems[i % 3], bark), (crowns[i % 3], leaves) })
                    {
                        var entity = ecs.Spawn();
                        Render.SetMesh(ecs, entity, mesh);
                        Render.SetMaterial(ecs, entity, material);
                        ecs.Add(entity, at);
                    }
                }
            },
        };
        run.Wait(10).Until("ready", _ => Render.PipelinesReady()).Wait(240).Do("timings", _ => timings = Render.Timings()).Go();
        return timings.FirstOrDefault(timing => timing.Name == "main_opaque_pass_3d").GpuMilliseconds;
    }
}
