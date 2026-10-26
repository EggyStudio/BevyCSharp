using System.Numerics;
using System.Runtime.InteropServices;
using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// A planted tree swaying in the wind, each vertex carrying the pivot of the limb it swings with and
/// the pivot its leaf flutters about, moved by the trees' materials in their vertex stages.
/// </summary>
[Collection("engine")]
public sealed class TreeWindTests(ITestOutputHelper output)
{
    [Fact]
    public void EachVertexOfATreeCarriesThePivotOfTheLimbItSwingsWith()
    {
        foreach (var species in new[] { TreeSpecies.QuakingAspen, TreeSpecies.BlackTupelo, TreeSpecies.CaliforniaBlackOak })
        {
            var skeleton = Trees.Grow(species, 2);
            var places = skeleton.Nodes.Select(node => node.Place).ToHashSet();
            var (stems, leaves, far) = Trees.Swayed(skeleton, species, 2);

            // Every stem vertex swings about a point of the skeleton where its limb leaves its
            // parent, or stays where it is on the trunk, the foot among those.
            var (stemLimbs, stemOwn) = Streams(stems);
            var still = 0;
            for (var i = 0; i < stems.Positions.Length; i++)
            {
                var (pivot, phase) = (new Vec3(stemLimbs[i].X, stemLimbs[i].Y, stemLimbs[i].Z), stemLimbs[i].W);
                Assert.Equal(stems.Positions[i], new Vec3(stemOwn[i].X, stemOwn[i].Y, stemOwn[i].Z));
                if (pivot == stems.Positions[i] && phase == 0f) still++;
                else Assert.True(places.Contains(pivot) && phase is >= 0f and < 1f, $"a stem vertex swings about {pivot}, no point of the skeleton");
            }

            var foot = Enumerable.Range(0, stems.Positions.Length).MinBy(i => stems.Positions[i].Y);
            Assert.Equal(stems.Positions[foot], new Vec3(stemLimbs[foot].X, stemLimbs[foot].Y, stemLimbs[foot].Z));

            // A leaf's four corners swing with one limb and flutter about its stalk, the middle of
            // its first two corners, each leaf in a phase of its own.
            var (leafLimbs, leafOwn) = Streams(leaves);
            var cards = leaves.Positions.Length / 4;
            for (var card = 0; card < cards; card++)
            {
                var stalk = (leaves.Positions[card * 4] + leaves.Positions[card * 4 + 1]) * 0.5f;
                for (var corner = 1; corner < 4; corner++) Assert.Equal(leafLimbs[card * 4], leafLimbs[card * 4 + corner]);
                Assert.True((new Vec3(leafOwn[card * 4].X, leafOwn[card * 4].Y, leafOwn[card * 4].Z) - stalk).Length < 1e-4f, "a leaf flutters about another place than its stalk");
            }

            Assert.True(Enumerable.Range(0, cards).Select(card => leafOwn[card * 4].W).Distinct().Count() > cards * 9 / 10, "the leaves flutter in step");

            // The far level's cards swing about the pivots of the limbs whose leaves they picture.
            var swung = Enumerable.Range(0, cards).Select(card => leafLimbs[card * 4]).ToHashSet();
            var (quadLimbs, _) = Streams(far!.Quads);
            for (var i = 0; i < far.Quads.Positions.Length; i++)
            {
                var place = far.Quads.Positions[i];
                var limb = quadLimbs[i];
                if (new Vec3(limb.X, limb.Y, limb.Z) == place && limb.W == 0f) continue;
                Assert.Contains(limb, swung);
            }

            output.WriteLine($"{species.LeafSet}'s tree, {stems.Positions.Length - still} of {stems.Positions.Length} stem vertices swinging with a limb, {swung.Count} limbs among its {cards} leaves");
        }
    }

    [SkippableFact]
    public void AWindBendsATreeDownwindAndTwoTreesAlikeSwayOutOfStep()
    {
        Needs.Renderer();

        // Two aspens of one seed, so of one variant, turn and size, ten meters apart, seen from 25
        // meters, first in still air and then in a wind of 20 meters a second blowing to the right,
        // each crown's middle read from the pixels unlike the sky on its half of the picture.
        var height = Trees.Grow(TreeSpecies.QuakingAspen, 0x7E57_0003u).Bounds.High.Y;
        IReadOnlyList<Entity> trees = [];
        var run = new PictureRun
        {
            Width = 640,
            Height = 480,
            Frames = 3000,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 30f, Far = 1000f });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, height * 0.6f, 25f), new Vec3(0f, height * 0.6f, 0f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetClearColor((0.55f, 0.68f, 0.85f, 1f));
                Render.SetAmbientLight((0.55f, 0.68f, 0.85f), 1500f);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 20_000f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(new Vec3(2f, 8f, 6f), Vec3.Zero, Vec3.UnitY));
                trees = [Trees.Plant(ecs, "quaking-aspen", 3, new Vec3(-5f, 0f, 0f)), Trees.Plant(ecs, "quaking-aspen", 3, new Vec3(5f, 0f, 0f))];
            },
        };

        string[] blown = ["blown 0", "blown 1", "blown 2", "blown 3", "blown 4", "blown 5"];
        Trees.Wind = Vec3.Zero;
        try
        {
            run.Wait(10).Until("ready", _ => Render.PipelinesReady())
                .Until("dressed", world => trees.All(tree => world.Resource<EcsWorld>().ChildrenOf(tree).Length > 0))
                .Wait(60)
                .Capture("still")
                .Wait(17)
                .Capture("still later")
                .Do("blow", _ => Trees.Wind = new Vec3(20f, 0f, 0f))
                .Wait(30);
            foreach (var name in blown) run.Capture(name).Wait(11);
            run.Go();
        }
        finally
        {
            Trees.Wind = null;
        }

        var still = Middles(run.Picture("still"));
        Assert.Equal(still, Middles(run.Picture("still later")));

        var shifts = blown.Select(name => Middles(run.Picture(name))).Select(middle => (Left: middle.Left - still.Left, Right: middle.Right - still.Right)).ToArray();
        foreach (var (left, right) in shifts) output.WriteLine($"blown, the left crown's middle {left:+0.00;-0.00} pixels from still and the right's {right:+0.00;-0.00}");

        // Both lean downwind, and at some moment one leans more than the other.
        Assert.True(shifts.Average(shift => shift.Left) > 1f && shifts.Average(shift => shift.Right) > 1f, "the crowns do not lean downwind");
        Assert.True(shifts.Max(shift => MathF.Abs(shift.Left - shift.Right)) > 0.25f, "the two trees sway in step");
    }

    [SkippableFact]
    public void TheWindsCostIsReadInAForest()
    {
        Needs.Shaders();

        // Twenty-four tupelos planted in four rows going away, near enough to be drawn whole, at
        // 1920 by 1080, in still air and then in a wind of 15 meters a second.
        IReadOnlyList<PassTiming> still = [], blown = [];
        IReadOnlyList<Entity> trees = [];
        var run = new PictureRun
        {
            Width = 1920,
            Height = 1080,
            Frames = 2400,
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
                trees = [.. Enumerable.Range(0, 24).Select(i => Trees.Plant(ecs, "black-tupelo", (uint)i, new Vec3((i % 6 - 2.5f) * 7f + i / 6 % 2 * 3f, 0f, -(i / 6) * 9f)))];
            },
        };

        Trees.Wind = Vec3.Zero;
        try
        {
            run.Wait(10).Until("ready", _ => Render.PipelinesReady())
                .Until("dressed", world => trees.All(tree => world.Resource<EcsWorld>().ChildrenOf(tree).Length > 0))
                .Wait(240)
                .Do("still", _ => still = Render.Timings())
                .Do("blow", _ => Trees.Wind = new Vec3(15f, 0f, 0f))
                .Wait(240)
                .Do("blown", _ => blown = Render.Timings())
                .Go();
        }
        finally
        {
            Trees.Wind = null;
        }

        double? Pass(IReadOnlyList<PassTiming> timings, string name) => timings.FirstOrDefault(timing => timing.Name == name).GpuMilliseconds;
        string[] passes = ["per_view_shadow_pass", "main_opaque_pass_3d"];
        Skip.If(passes.Any(name => Pass(still, name) is null || Pass(blown, name) is null), "needs a GPU whose passes the app can time");
        foreach (var name in passes) output.WriteLine($"24 tupelos at 1920 by 1080, {name} {Pass(still, name):0.00} ms in still air and {Pass(blown, name):0.00} ms in the wind");
    }

    /// <summary>A mesh's two sway streams, a value a vertex each.</summary>
    private static (Vector4[] Limbs, Vector4[] Own) Streams(MeshData mesh)
    {
        var streams = mesh.Streams ?? throw new Xunit.Sdk.XunitException("the mesh carries no sway");
        var limbs = streams.Single(stream => stream.Name == "bcs_tree_limb");
        var own = streams.Single(stream => stream.Name == "bcs_tree_own");
        Assert.Equal((8u, 9u), (limbs.Location, own.Location));
        Assert.All(new[] { limbs, own }, stream => Assert.Equal(MeshStreamFormat.Float4, stream.Format));
        return (MemoryMarshal.Cast<byte, Vector4>(limbs.Data).ToArray(), MemoryMarshal.Cast<byte, Vector4>(own.Data).ToArray());
    }

    /// <summary>The middle across of the pixels unlike the sky in each half of a picture.</summary>
    private static (float Left, float Right) Middles(CapturedImage picture)
    {
        var sky = picture.At(0, 0);
        var (sums, counts) = (new double[2], new int[2]);
        for (var y = 0u; y < picture.Height; y++)
        for (var x = 0u; x < picture.Width; x++)
        {
            var (r, g, b, _) = picture.At(x, y);
            if (Math.Abs(r - sky.R) + Math.Abs(g - sky.G) + Math.Abs(b - sky.B) <= 30) continue;
            var half = x < picture.Width / 2 ? 0 : 1;
            sums[half] += x;
            counts[half]++;
        }

        return ((float)(sums[0] / Math.Max(counts[0], 1)), (float)(sums[1] / Math.Max(counts[1], 1)));
    }
}
