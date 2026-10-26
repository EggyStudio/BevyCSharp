using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// A planted tree's furthest level, an octahedral impostor, the whole tree pictured once a variant
/// from eight by eight ways over the upper half of the sphere and drawn as one quad blending the
/// three frames pictured nearest the way it is seen from, past twenty of the tree's heights.
/// </summary>
[Collection("engine")]
public sealed class TreeImpostorsTests(ITestOutputHelper output)
{
    [Fact]
    public void AWayIsBlendedFromTheThreeFramesPicturedNearestIt()
    {
        // Every frame's way, read back, is that frame alone, level along the folded square's edge
        // and with a level right square to its up.
        for (var row = 0; row < Trees.ImpostorAcross; row++)
        for (var column = 0; column < Trees.ImpostorAcross; column++)
        {
            var way = Trees.ImpostorWay(column, row);
            Assert.True(way.Y >= 0f, $"frame {column}, {row} looks from below");
            if (column is 0 or Trees.ImpostorAcross - 1 || row is 0 or Trees.ImpostorAcross - 1) Assert.Equal(0f, way.Y, 5);
            var (right, up) = Trees.ImpostorBasis(way);
            Assert.Equal(0f, right.Y, 5);
            Assert.Equal(0f, Vec3.Dot(right, way), 5);
            Assert.Equal(0f, Vec3.Dot(up, way), 5);
            Assert.Equal(1f, up.Length, 4);

            var nearest = Trees.NearestFrames(way);
            var (frame, weight) = nearest.MaxBy(pair => pair.Weight);
            Assert.Equal((column, row), frame);
            Assert.Equal(1f, weight, 4);
        }

        // Any way over the half sphere takes three frames whose shares add to one, the largest
        // share's frame within a cell of the grid of it, whose widest, near the zenith where the
        // fold stretches the square most, span some thirty degrees, and a way from below is seen
        // from level.
        var random = new Random(70);
        var furthest = 0f;
        for (var trial = 0; trial < 2000; trial++)
        {
            var way = new Vec3((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble(), (float)random.NextDouble() * 2f - 1f).Normalized;
            var nearest = Trees.NearestFrames(way);
            Assert.Equal(1f, nearest.Sum(pair => pair.Weight), 4);
            Assert.All(nearest, pair => Assert.True(pair.Weight >= -1e-5f && pair.Frame.Column is >= 0 and < Trees.ImpostorAcross && pair.Frame.Row is >= 0 and < Trees.ImpostorAcross));
            var (column, row) = nearest.MaxBy(pair => pair.Weight).Frame;
            furthest = MathF.Max(furthest, MathF.Acos(Math.Clamp(Vec3.Dot(way, Trees.ImpostorWay(column, row)), -1f, 1f)) * 180f / MathF.PI);

            var below = new Vec3(way.X, -way.Y, way.Z);
            Assert.Equal(Trees.NearestFrames(new Vec3(way.X, 0f, way.Z)), Trees.NearestFrames(below));
        }

        output.WriteLine($"a way stands at most {furthest:F1} degrees from the frame it takes most from");
        Assert.InRange(furthest, 1f, 25f);
    }

    [Fact]
    public void AnImpostorsFramesHoldTheWholeTreeFromEveryWay()
    {
        foreach (var species in new[] { TreeSpecies.QuakingAspen, TreeSpecies.BlackTupelo, TreeSpecies.WeepingWillow, TreeSpecies.CaliforniaBlackOak })
        {
            var skeleton = Trees.Grow(species, 2);
            var plan = Trees.PlanImpostorLevel(skeleton, species, 2)!;
            var stems = Modeling.Build(Trees.Build(skeleton, TreeDetail.Full))[0];
            var leaves = Trees.BuildLeaves(skeleton, species, 2);

            // Every vertex inside the sphere a frame holds with its margin, so no way's picture of
            // the tree is cut by its frame's edge.
            var furthest = stems.Positions.Concat(leaves.Positions).Max(place => (place - plan.Middle).Length);
            Assert.InRange(furthest, 0.9f * plan.Extent, plan.Extent);

            // The quad's vertices span the sphere along every axis, so Bevy's bounds round it hold
            // the tree from every way, and carry a corner each.
            var quad = plan.Quad;
            foreach (var axis in new[] { Vec3.UnitX, Vec3.UnitY, Vec3.UnitZ })
            {
                Assert.Equal(-plan.Extent, quad.Positions.Min(place => Vec3.Dot(place - plan.Middle, axis)), 3);
                Assert.Equal(plan.Extent, quad.Positions.Max(place => Vec3.Dot(place - plan.Middle, axis)), 3);
            }

            Assert.Equal(4, quad.Uvs!.Chunk(2).Select(uv => (uv[0], uv[1])).Distinct().Count());
            output.WriteLine($"{species.LeafSet}'s tree, {skeleton.Bounds.High.Y:F1} m tall, in a sphere {furthest:F1} m across its half, a frame {2f * plan.Extent:F1} m a side");
        }

        // A fern's fronds are its whole leaves and it has no far levels.
        Assert.Null(Trees.PlanImpostorLevel(Trees.Grow(TreeSpecies.Fern, 2), TreeSpecies.Fern, 2));
    }

    [SkippableTheory]
    [InlineData("quaking-aspen")]
    [InlineData("black-tupelo")]
    [InlineData("california-black-oak")]
    public void AFarTreesImpostorCoversItsCrownAsItsCardsDo(string name)
    {
        Needs.Renderer();

        // A planted tree at twenty-eight of its heights, where its impostor is drawn, seen through a
        // narrow lens from level and from a third of the way up the sky, against the same tree's
        // cards and limbs drawn there in its place, in the sun under a pale sky. The impostor covers
        // the crown within a fifth and is as bright within a fifth.
        var species = Trees.Species(name)!;
        var height = Trees.Grow(species, 0x7E57_0003u).Bounds.High.Y;
        var middle = new Vec3(0f, height * 0.5f, 0f);
        Entity camera = default, planted = default;
        var run = new PictureRun
        {
            Width = 640,
            Height = 480,
            Frames = 3000,
            Scene = ecs =>
            {
                camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 7f, Far = 5000f });
                ecs.Add(camera, Transform.LookingAt(middle + new Vec3(0f, 0.05f, 1f).Normalized * (28f * height), middle, Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetClearColor((0.55f, 0.68f, 0.85f, 1f));
                Render.SetAmbientLight((0.55f, 0.68f, 0.85f), 1500f);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 30_000f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(new Vec3(6f, 8f, 4f), Vec3.Zero, Vec3.UnitY));
                planted = Trees.Plant(ecs, name, 3, Vec3.Zero);
            },
        };

        void Above(World world) =>
            world.Resource<EcsWorld>().GetRef<Transform>(camera) = Transform.LookingAt(middle + new Vec3(0f, 0.5f, 0.87f).Normalized * (28f * height), middle, Vec3.UnitY);

        run.Wait(10).Until("ready", _ => Render.PipelinesReady())
            .Until("dressed", world => world.Resource<EcsWorld>().ChildrenOf(planted).Length > 0)
            .Wait(60)
            .Capture("impostor")
            .Do("above", Above)
            .Wait(10)
            .Capture("impostor above")
            .Do("cards", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                Swapped(ecs, ecs.ChildrenOf(planted)[0]);
                ecs.GetRef<Transform>(camera) = Transform.LookingAt(middle + new Vec3(0f, 0.05f, 1f).Normalized * (28f * height), middle, Vec3.UnitY);
            })
            .Wait(30)
            .Capture("cards")
            .Do("above", Above)
            .Wait(10)
            .Capture("cards above")
            .Go();

        foreach (var view in new[] { "", " above" })
        {
            var (far, near) = (Read(run.Picture("impostor" + view)), Read(run.Picture("cards" + view)));
            output.WriteLine($"{name} at twenty-eight heights{view}, the impostor {far.Pixels} pixels at {far.Brightness:F0} and the cards {near.Pixels} at {near.Brightness:F0}");
            Assert.InRange(far.Pixels / (float)near.Pixels, 0.8f, 1.25f);
            Assert.InRange(far.Brightness / near.Brightness, 0.8f, 1.2f);
        }
    }

    [SkippableFact]
    public void TheImpostorsCostIsReadInAFarForest()
    {
        Needs.Shaders();

        // A thousand tupelos planted on a disc thirty-eight of their heights off and twelve across
        // its half, some fifteen meters apart, drawn as impostors, then the same trees as their
        // limbs and cards, at 1920 by 1080. The sun's shadow does not reach so far.
        var species = TreeSpecies.BlackTupelo;
        var height = Enumerable.Range(0, Trees.Variants).Max(variant => Trees.Grow(species, (uint)(0x7E57_0000 + variant)).Bounds.High.Y) * 1.1f;
        IReadOnlyList<PassTiming> impostors = [], cards = [];
        IReadOnlyList<Entity> trees = [];
        var run = new PictureRun
        {
            Width = 1920,
            Height = 1080,
            Frames = 2400,
            Configure = config => config.GpuTimings = true,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 45f, Far = 10_000f });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1.7f, 0f), new Vec3(0f, height * 0.5f, -38f * height), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAtmosphere(camera, new AtmosphereSettings());
                Render.SetSkyLighting(camera);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 12_500f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(new Vec3(-3f, 5f, 2f), Vec3.Zero, Vec3.UnitY));
                trees = Trees.PlantForest(ecs, "black-tupelo", 7, new Vec3(0f, 0f, -38f * height), 12f * height, 1000);
            },
        };

        run.Wait(10).Until("ready", _ => Render.PipelinesReady())
            .Until("dressed", world => trees.All(tree => world.Resource<EcsWorld>().ChildrenOf(tree).Length > 0))
            .Wait(240)
            .Do("impostors", _ => impostors = Render.Timings())
            .Do("cards", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                foreach (var tree in trees) Swapped(ecs, ecs.ChildrenOf(tree)[0]);
            })
            .Wait(10).Until("ready again", _ => Render.PipelinesReady()).Wait(240)
            .Do("timings", _ => cards = Render.Timings())
            .Go();

        double? Pass(IReadOnlyList<PassTiming> timings, string name) => timings.FirstOrDefault(timing => timing.Name == name).GpuMilliseconds;
        string[] passes = ["per_view_shadow_pass", "main_opaque_pass_3d"];
        Skip.If(passes.Any(name => Pass(impostors, name) is null || Pass(cards, name) is null), "needs a GPU whose passes the app can time");
        output.WriteLine($"{trees.Count} tupelos, the nearest some {(38f - 12f) * height:F0} meters off");
        foreach (var name in passes) output.WriteLine($"tupelos thirty-eight heights off at 1920 by 1080, {name} {Pass(cards, name):0.00} ms as limbs and cards and {Pass(impostors, name):0.00} ms as impostors");
    }

    /// <summary>A tree's limbs and cards drawn at every distance and its impostor at none.</summary>
    private static void Swapped(EcsWorld ecs, Entity body)
    {
        foreach (var part in ecs.ChildrenOf(body))
        {
            var name = ecs.NameOf(part) ?? "";
            var drawn = name.EndsWith(", cards", StringComparison.Ordinal) || name.EndsWith(", limbs", StringComparison.Ordinal);
            if (!drawn && !name.EndsWith(", impostor", StringComparison.Ordinal)) continue;
            var range = ecs.Insert<Bevy.Reflected.VisibilityRangeRef>(part);
            range.StartMargin = drawn ? new FloatRange(0f, 0f) : new FloatRange(1e7f, 1e7f);
            range.EndMargin = new FloatRange(1e7f, 1e7f);
        }
    }

    /// <summary>The pixels unlike the sky in its corner, and their mean brightness.</summary>
    private static (int Pixels, float Brightness) Read(CapturedImage picture)
    {
        var sky = picture.At(0, 0);
        var (pixels, sum) = (0, 0f);
        for (var y = 0u; y < picture.Height; y++)
        for (var x = 0u; x < picture.Width; x++)
        {
            var (r, g, b, _) = picture.At(x, y);
            if (Math.Abs(r - sky.R) + Math.Abs(g - sky.G) + Math.Abs(b - sky.B) <= 30) continue;
            pixels++;
            sum += 0.2126f * r + 0.7152f * g + 0.0722f * b;
        }

        return (pixels, pixels > 0 ? sum / pixels : 0f);
    }
}
