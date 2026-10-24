using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

// The builds' budget of triangles a frame, 3DEngine's tests of its item 74, hundreds of meshes
// arriving in one frame built over as many frames as it takes, nearest the eye first, each frame
// under the budget on the plan's counts and the splat's time on the GPU under the frame that built
// them all at once, the field built in parts holding the distances of the field built whole, and a
// build in progress taking in a mesh that came and starting again for one that left.
public sealed partial class SceneFieldTests
{
    // The sections a voxel game streams in at once, as the reading that stalled a frame in 3DEngine
    // had them, 500 of 1,024 triangles each, flat boxes four meters a side on a grid of 25 by 20
    // about the eye.
    private const int Sections = 500, SectionTriangles = 1024;

    private static readonly SceneFieldPlan.Box SectionBounds = new(Vector3.Zero, new Vector3(4, 1, 4));

    private static Vector3 SectionAt(int i) => new((i % 25 - 12) * 4, 0, (i / 25 - 10) * 4);

    private static List<(SceneFieldPlan.Instance, bool)> AllSections() =>
        [.. Enumerable.Range(0, Sections).Select(i => (new SceneFieldPlan.Instance(1, 0, Matrix4x4.CreateTranslation(SectionAt(i)), false, SectionBounds), false))];

    // Runs a plan until it has settled the sections and built every cascade with them, and answers
    // each frame's triangles splatted from the frame they settled in, and the parts of each
    // cascade's build in order.
    private static (List<int> Frames, Dictionary<int, List<SceneFieldPlan.Build>> Parts) BuildSections(int triangleBudget)
    {
        var plan = new SceneFieldPlan(4, 0.25f, 4, triangleBudget: triangleBudget, triangles: _ => SectionTriangles);
        var eye = new Vector3(0, 2, 0);
        var drawn = AllSections();
        var (frames, parts) = (new List<int>(), new Dictionary<int, List<SceneFieldPlan.Build>>());
        for (var frame = 1; frame < 200; frame++)
        {
            plan.Update(eye, drawn);
            if (frame < SceneFieldPlan.SettleFrames) continue;
            frames.Add(plan.Splatted);
            foreach (var build in plan.Builds) (parts.TryGetValue(build.Cascade, out var list) ? list : parts[build.Cascade] = []).Add(build);
            if (plan.Builds.Count == 0 && plan.Waiting == 0) break;
        }
        return (frames, parts);
    }

    /// <summary>
    /// Five hundred meshes settling in one frame are splatted over as many frames as the triangle
    /// budget takes, no frame over it, each cascade's build started once and read whole on its last
    /// part, from the meshes one build would have built it from, each part's as near the eye as the
    /// next part's or nearer.
    /// </summary>
    [Fact]
    public void FiveHundredMeshesArrivingInOneFrameAreBuiltOverSeveralFramesUnderTheTriangleBudgetTheNearestFirst()
    {
        // All at once, as the cascades' budget alone has it, the frame they settle in splats every
        // cascade's reach, 500 meshes into the coarsest alone.
        var (whole, wholeParts) = BuildSections(int.MaxValue);
        Assert.True(whole[0] >= Sections * SectionTriangles, $"the frame the sections settled in splatted {whole[0]} triangles into four cascades");
        Assert.All(wholeParts.Values, list => Assert.True(list is [{ First: true, Last: true }], "each cascade in one build of one part"));

        const int budget = 65_536;
        var (frames, parts) = BuildSections(budget);
        Assert.All(frames, splatted => Assert.InRange(splatted, 0, budget));
        Assert.True(frames.Count(splatted => splatted > 0) >= whole[0] / budget, $"the builds took {frames.Count(splatted => splatted > 0)} frames");

        foreach (var (cascade, list) in parts)
        {
            Assert.True(list[0].First, $"cascade {cascade}'s build starts on its first part");
            Assert.DoesNotContain(list.Skip(1), part => part.First);
            Assert.True(list[^1].Last, $"cascade {cascade}'s build ends on its last part");
            Assert.DoesNotContain(list.SkipLast(1), part => part.Last || part.Changed is not null);
            Assert.Equal(
                wholeParts[cascade][0].Instances.Select(instance => instance.World.Translation).Order(Positions),
                list.SelectMany(part => part.Instances).Select(instance => instance.World.Translation).Order(Positions));
        }

        // Each part's meshes are as near the eye as the next part's or nearer.
        var eye = new Vector3(0, 2, 0);
        float Distance(SceneFieldPlan.Instance instance) => Vector3.DistanceSquared(instance.World.Translation + new Vector3(2, 0.5f, 2), eye);
        foreach (var list in parts.Values.Where(list => list.Count > 1))
            for (var part = 0; part + 1 < list.Count; part++)
                Assert.True(list[part].Instances.Max(Distance) <= list[part + 1].Instances.Min(Distance), "nearest the eye first");
    }

    /// <summary>
    /// A build in progress takes in a mesh that settles within its reach without starting again, and
    /// starts again without a mesh it splatted that leaves.
    /// </summary>
    [Fact]
    public void ABuildInProgressTakesInAMeshThatSettlesAndStartsAgainForASplattedOneThatLeaves()
    {
        // A budget of one mesh's triangles, so a build of ten meshes takes ten frames, the meshes in
        // a row going away from the eye.
        var plan = new SceneFieldPlan(1, 1, 1, triangleBudget: 10, triangles: _ => 10);
        var eye = new Vector3(0, 2, 6);
        var row = Enumerable.Range(0, 10).Select(k => At(new Vector3(0, 0, 4 - 2 * k))).ToList();
        plan.Update(eye, []);
        Assert.Single(plan.Builds);
        for (var frame = 1; frame < SceneFieldPlan.SettleFrames; frame++) plan.Update(eye, row);

        // The frame they settle in starts the build with the nearest, and a newcomer drawn from the
        // frame after settles while the build runs, nearer the eye than the meshes left.
        plan.Update(eye, row);
        var started = Assert.Single(plan.Builds);
        Assert.True(started is { First: true, Last: false, Instances.Length: 1 }, "the build's first part splats one mesh");
        Assert.Equal(new Vector3(0, 0, 4), started.Instances[0].World.Translation);
        var newcomer = At(new Vector3(0, 0, 2.5f));
        var parts = new List<SceneFieldPlan.Build>();
        for (var frame = 0; frame < 30 && (parts.Count == 0 || !parts[^1].Last); frame++)
        {
            plan.Update(eye, [.. row, newcomer]);
            parts.AddRange(plan.Builds);
        }
        Assert.NotEmpty(parts);
        Assert.DoesNotContain(parts, part => part.First);
        Assert.True(parts[^1].Last);
        Assert.Contains(new Vector3(0, 0, 2.5f), parts.SelectMany(part => part.Instances).Select(i => i.World.Translation));
        plan.Update(eye, [.. row, newcomer]);
        Assert.Empty(plan.Builds);

        // The farthest leaves, and the cascade is built again; its build's first part splats the
        // nearest, which then leaves too, and the build starts again without it.
        var left = row.Take(9).Append(newcomer).ToList();
        plan.Update(eye, left);
        var again = Assert.Single(plan.Builds);
        Assert.True(again is { First: true, Last: false });
        Assert.Equal(new Vector3(0, 0, 4), again.Instances[0].World.Translation);
        left.RemoveAt(0);
        plan.Update(eye, left);
        Assert.True(Assert.Single(plan.Builds).First, "a mesh the build splatted left, so it starts again");
        var splatted = plan.Builds.SelectMany(b => b.Instances).Select(i => i.World.Translation).ToList();
        for (var frame = 0; frame < 30 && !plan.Builds[^1].Last; frame++)
        {
            plan.Update(eye, left);
            splatted.AddRange(plan.Builds.SelectMany(b => b.Instances).Select(i => i.World.Translation));
        }
        Assert.Equal(left.Select(entry => entry.Item1.World.Translation).Order(Positions), splatted.Order(Positions));
    }

    /// <summary>
    /// A finer cascade the eye moves past is built before a coarser cascade's build in progress,
    /// which starts again after it.
    /// </summary>
    [Fact]
    public void AFinerCascadeTheEyeMovesPastIsBuiltBeforeACoarserBuildInProgressWhichStartsAgainAfter()
    {
        // Two cascades and a budget of one mesh a frame, so the coarser's build of the sections it
        // reaches takes many frames.
        var plan = new SceneFieldPlan(2, 0.25f, 2, triangleBudget: SectionTriangles, triangles: _ => SectionTriangles);
        var eye = new Vector3(0, 2, 0);
        var drawn = AllSections();
        // The finer's sections take as many frames from the eighth, and the coarser's begin after.
        for (var frame = 0; frame < 80; frame++) plan.Update(eye, drawn);
        var running = Assert.Single(plan.Builds);
        Assert.True(running is { Cascade: 1, First: false }, "the coarser cascade's build is under way");

        // The eye passes the finer cascade's grid, which is built at once, and the coarser's build
        // starts again the frame after it ends.
        eye += new Vector3(2.5f, 0, 0);
        plan.Update(eye, drawn);
        Assert.NotEmpty(plan.Builds);
        Assert.True(plan.Builds[0] is { Cascade: 0, First: true }, "the finer cascade comes first");
        var after = new List<SceneFieldPlan.Build>(plan.Builds);
        for (var frame = 0; frame < 60 && !after.Any(b => b.Cascade == 1); frame++)
        {
            plan.Update(eye, drawn);
            after.AddRange(plan.Builds);
        }
        Assert.True(after.First(b => b.Cascade == 1).First, "the coarser cascade's build starts again");
    }

    /// <summary>
    /// On the GPU, 500 meshes of 1,024 triangles drawn from the first frame are built over more frames
    /// under the triangle budget than at once, no frame splatting more than the budget and none of the
    /// splat's dispatches near the slowest of the run that built them all at once, and the field they
    /// build in parts holds the distances of the field built whole.
    /// </summary>
    [SkippableFact]
    public void OnTheGpuFiveHundredMeshesArrivingAtOnceAreBuiltInPartsWithNoSplatNearTheOneOfAllAtOnce()
    {
        Needs.Shaders();
        var whole = BuildSectionsOnTheGpu(int.MaxValue);
        var budgeted = BuildSectionsOnTheGpu(SceneFieldPlan.DefaultTriangleBudget);

        Assert.True(whole.Most >= Sections * SectionTriangles, $"the run without a budget splatted {whole.Most} triangles in its busiest frame");
        Assert.InRange(budgeted.Most, 1, SceneFieldPlan.DefaultTriangleBudget);
        Assert.True(budgeted.Frames > whole.Frames, $"the budget spread the builds over {budgeted.Frames} frames, {whole.Frames} without it");

        // A part's codes are the least of its triangles' and those of the parts before it, as the
        // whole build's are the least of all of them, so every cell resolves to the same distance.
        Assert.Equal(whole.Distances.Length, budgeted.Distances.Length);
        var worst = whole.Distances.Zip(budgeted.Distances, (a, b) => MathF.Abs(a - b)).Max();
        Assert.True(worst < 1e-3f, $"a cell built in parts lies {worst} m from the same cell built whole");

        // A GPU that reads no timestamps reads the splat at nothing every frame, and there the counts
        // alone are held.
        if (whole.Slowest > 0)
            Assert.True(budgeted.Slowest < whole.Slowest / 2,
                $"the slowest splat under the budget took {budgeted.Slowest:0.000} ms, all at once {whole.Slowest:0.000} ms");
    }

    // Draws the 500 sections from the first frame with a field of four cascades under the triangle
    // budget given until every section is still and built and nothing waits or is stamped, and answers the most triangles a
    // frame splatted, how many frames splatted any, the slowest reading of the splat's GPU time, and
    // the field's every cascade read back.
    private static (int Most, int Frames, double Slowest, float[] Distances) BuildSectionsOnTheGpu(int triangleBudget)
    {
        var (most, frames, slowest) = (0, 0, 0.0);
        float[]? distances = null;
        var read = default(BufferRead);
        new PictureRun
        {
            Width = 64,
            Height = 48,
            Configure = config => config.GpuTimings = true,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs);
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 2f, 0f), new Vec3(0f, 0f, -10f), Vec3.UnitY));
                Render.SetSceneField(4, 0.25f, 4, triangleBudget: triangleBudget);
                var section = Render.CreateMesh(Grid(16, 32));
                var material = Render.CreateMaterial(0.5f, 0.5f, 0.5f);
                for (var i = 0; i < Sections; i++)
                {
                    var at = SectionAt(i);
                    var entity = ecs.Spawn();
                    Render.SetMesh(ecs, entity, section);
                    Render.SetMaterial(ecs, entity, material);
                    ecs.Add(entity, Transform.At(at.X, at.Y, at.Z));
                }
            },
            EachFrame = _ =>
            {
                if (SceneField.Plan is not { } plan) return;
                most = Math.Max(most, plan.Splatted);
                if (plan.Splatted > 0) frames++;
                if (Diagnostics.TryRead("render/shader field_splat/elapsed_gpu", out var reading) && reading.Value is { } ms) slowest = Math.Max(slowest, ms);
            },
        }
            .Until("built", _ => SceneField.Plan is { } plan && plan.StillCount == Sections && plan.Waiting == 0 && plan.Builds.Count == 0
                && plan.Shapes.Count == 0 && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Wait(4)
            .Do("asking for the field", _ => read = Shaders.BeginImageRead(SceneField.Image))
            .Until("read back", _ => Halves(read, out distances))
            .Go();

        return (most, frames, slowest, distances!);
    }

    // A flat grid four meters a side, its corner at the origin, of as many squares across and deep as
    // given, two triangles each.
    private static MeshData Grid(int across, int deep)
    {
        var positions = new List<Vec3>();
        for (var z = 0; z <= deep; z++)
            for (var x = 0; x <= across; x++)
                positions.Add(new Vec3(4f * x / across, 0f, 4f * z / deep));
        var indices = new List<uint>();
        for (var z = 0; z < deep; z++)
            for (var x = 0; x < across; x++)
            {
                var corner = (uint)(z * (across + 1) + x);
                var below = corner + (uint)(across + 1);
                indices.AddRange([corner, below, corner + 1, corner + 1, below, below + 1]);
            }
        return new MeshData { Positions = [.. positions], Indices = [.. indices] };
    }

    // Orders places by x, then y, then z, so two lists of the same places compare equal.
    private static readonly Comparer<Vector3> Positions = Comparer<Vector3>.Create((a, b) =>
        a.X != b.X ? a.X.CompareTo(b.X) : a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.Z.CompareTo(b.Z));
}
