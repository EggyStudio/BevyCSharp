using System.Numerics;
using System.Runtime.InteropServices;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The scene's distance field, where its cascades lie and what each frame builds and stamps, on the
/// CPU as 3DEngine's tests hold its plan, and the distances the GPU builds read back against the
/// meshes they were built from, a box's at every cell, a tunnel one cell wide, a moving box's stamp
/// and the budget a frame keeps to.
/// </summary>
[Collection("engine")]
public sealed class SceneFieldTests
{
    private static readonly SceneFieldPlan.Box CubeBounds = new(new Vector3(-1), new Vector3(1));

    private static (SceneFieldPlan.Instance, bool) At(Vector3 position, uint mesh = 1, uint generation = 0) =>
        (new SceneFieldPlan.Instance(mesh, generation, Matrix4x4.CreateTranslation(position), false, CubeBounds), false);

    /// <summary>The draw and the eye at the offsets the bridge writes them, which its own test holds to the same numbers.</summary>
    [Fact]
    public void ADrawLiesWhereTheBridgeWritesIt()
    {
        Assert.Equal(8, Marshal.OffsetOf<NativeFieldDraw>(nameof(NativeFieldDraw.Flags)).ToInt32());
        Assert.Equal(16, Marshal.OffsetOf<NativeFieldDraw>(nameof(NativeFieldDraw.Rows)).ToInt32());
        Assert.Equal(64, Marshal.OffsetOf<NativeFieldDraw>(nameof(NativeFieldDraw.Color)).ToInt32());
        Assert.Equal(80, Marshal.OffsetOf<NativeFieldDraw>(nameof(NativeFieldDraw.Emission)).ToInt32());
        Assert.Equal(96, Marshal.OffsetOf<NativeFieldDraw>(nameof(NativeFieldDraw.Low)).ToInt32());
        Assert.Equal(112, Marshal.OffsetOf<NativeFieldDraw>(nameof(NativeFieldDraw.High)).ToInt32());
        Assert.Equal(128, Marshal.SizeOf<NativeFieldDraw>());
        Assert.Equal(12, Marshal.OffsetOf<NativeFieldEye>(nameof(NativeFieldEye.Found)).ToInt32());
        Assert.Equal(16, Marshal.OffsetOf<NativeFieldEye>(nameof(NativeFieldEye.ForwardX)).ToInt32());
        Assert.Equal(32, Marshal.SizeOf<NativeFieldEye>());
    }

    [Fact]
    public void ACascadeLiesAroundTheEyeAndMovesOnlyPastItsGrid()
    {
        var plan = new SceneFieldPlan(2, 0.25f, 2);
        plan.Update(new Vector3(0.1f, 2, 6), []);

        // The corner is 32 cells of 0.25 short of the eye's 2-unit grid point, and the second
        // cascade's cells and grid are twice as wide.
        Assert.Equal(new Vector3(-8, -6, -2), plan.BuiltOrigin(0));
        Assert.Equal(new Vector3(-16, -16, -12), plan.BuiltOrigin(1));
        Assert.Equal([0, 1], plan.Builds.Select(build => build.Cascade));

        // Within the 2-unit cell of the grid nothing moves, and past it the first cascade alone.
        plan.Update(new Vector3(1.9f, 2, 6), []);
        Assert.Empty(plan.Builds);
        plan.Update(new Vector3(2.1f, 2, 6), []);
        Assert.Equal([0], plan.Builds.Select(build => build.Cascade));
        Assert.Equal(new Vector3(-6, -6, -2), plan.BuiltOrigin(0));
    }

    [Fact]
    public void AMeshDrawnTheSameForEightFramesIsBuiltAndStampedUntilThen()
    {
        var plan = new SceneFieldPlan(1, 0.25f, 1);
        var eye = new Vector3(0, 2, 6);
        for (var frame = 1; frame < SceneFieldPlan.SettleFrames; frame++)
        {
            plan.Update(eye, [At(Vector3.Zero)]);
            Assert.Equal(0, plan.StillCount);
            Assert.Single(plan.Shapes);
        }

        // Still, and the cascade it is in built again with it rather than stamped.
        plan.Update(eye, [At(Vector3.Zero)]);
        Assert.Equal(1, plan.StillCount);
        Assert.Single(Assert.Single(plan.Builds).Instances);
        Assert.Empty(plan.Shapes);

        // Drawn somewhere else, another that has not settled, and the cascade it left built
        // without it.
        plan.Update(eye, [At(Vector3.UnitX)]);
        Assert.Equal(0, plan.StillCount);
        Assert.Empty(Assert.Single(plan.Builds).Instances);
        Assert.Equal(-Vector3.UnitX, Assert.Single(plan.Shapes).ToOwn.Translation);
    }

    [Fact]
    public void AStillMeshReplacedInPlaceStaysInTheFieldUntilTheOneReplacingItSettles()
    {
        // A cube settled and built, then its mesh written again in place, a generation on, as an
        // edited section of a game's world is.
        var plan = new SceneFieldPlan(1, 0.25f, 1);
        var eye = new Vector3(0.1f, 0.1f, 0.1f);
        for (var frame = 0; frame <= SceneFieldPlan.SettleFrames; frame++) plan.Update(eye, [At(Vector3.Zero)]);
        Assert.Equal(1, plan.StillCount);

        for (var frame = 1; frame < SceneFieldPlan.SettleFrames; frame++)
        {
            plan.Update(eye, [At(Vector3.Zero, generation: 1)]);
            Assert.Equal(1, plan.StillCount);
            Assert.Empty(plan.Builds);
            Assert.Empty(plan.Shapes);
        }

        // One build takes the cube out and puts its replacement in.
        plan.Update(eye, [At(Vector3.Zero, generation: 1)]);
        Assert.Equal(1, plan.StillCount);
        Assert.Equal(1u, Assert.Single(Assert.Single(plan.Builds).Instances).Generation);
        Assert.Empty(plan.Shapes);
    }

    [Fact]
    public void TheBudgetBuildsTheFinestDirtyCascadesFirst()
    {
        var plan = new SceneFieldPlan(3, 0.25f, 1);
        plan.Update(Vector3.Zero, []);
        Assert.Equal([0], plan.Builds.Select(build => build.Cascade));
        Assert.Null(plan.BuiltOrigin(1));
        plan.Update(Vector3.Zero, []);
        Assert.Equal([1], plan.Builds.Select(build => build.Cascade));
        plan.Update(Vector3.Zero, []);
        Assert.Equal([2], plan.Builds.Select(build => build.Cascade));
        plan.Update(Vector3.Zero, []);
        Assert.Empty(plan.Builds);
    }

    /// <summary>
    /// A mesh drawn twice in one place keeps every frame from being read by place, and once told,
    /// the frames drawn without the second copy are.
    /// </summary>
    [Fact]
    public void AMeshDrawnTwiceInOnePlaceIsToldAndTheFramesWithoutItsCopyAreReadByPlace()
    {
        var plan = new SceneFieldPlan(1, 0.25f, 1);
        (SceneFieldPlan.Instance, bool)[] twice = [At(Vector3.Zero), At(Vector3.Zero), At(Vector3.UnitX, mesh: 2)];
        plan.Update(Vector3.Zero, twice);
        plan.Update(Vector3.Zero, twice);
        Assert.True(plan.DrewTwice);
        Assert.False(plan.ReadByPlace);

        (SceneFieldPlan.Instance, bool)[] once = [At(Vector3.Zero), At(Vector3.UnitX, mesh: 2)];
        plan.Update(Vector3.Zero, once);
        Assert.False(plan.ReadByPlace);
        plan.Update(Vector3.Zero, once);
        Assert.True(plan.ReadByPlace);
        Assert.True(plan.DrewTwice, "told once, the field keeps gathering each mesh once");
    }

    [Fact]
    public void AMovingMeshIsStampedIntoTheBricksAroundItAndTheyArePutBackWhenItLeaves()
    {
        var plan = new SceneFieldPlan(1, 0.25f, 1);
        plan.Update(Vector3.Zero, []);
        plan.Update(Vector3.Zero, [At(new Vector3(0.1f, 0, 0))]);

        // The cube's 2 units and the band's 1 on each side, over bricks of one unit from the corner
        // at -8, are bricks 6 to 10 along each axis.
        Assert.Equal(5 * 5 * 5, plan.Bricks.Count);
        Assert.Contains((0, 6, 6, 6), plan.Bricks);
        Assert.Contains((0, 10, 10, 10), plan.Bricks);
        var stamped = plan.Bricks.ToHashSet();

        plan.Update(Vector3.Zero, []);
        Assert.Empty(plan.Shapes);
        Assert.True(stamped.SetEquals(plan.Bricks), "the bricks it was stamped into go back to the still field");
        plan.Update(Vector3.Zero, []);
        Assert.Empty(plan.Bricks);
    }

    /// <summary>
    /// A box settled into one cascade holds at every cell its distance to the box's surface, below
    /// zero inside, exact within the band and the band's beyond, as half a float holds it.
    /// </summary>
    [SkippableFact]
    public void TheFieldBuiltOnTheGpuHoldsTheDistancesToABox()
    {
        Needs.Shaders();
        var center = new Vector3(0.3f, 0.6f, -0.2f);
        var (distances, origin, said) = Built(1, ecs =>
        {
            PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
            PictureRun.Cube(ecs, Render.CreateMaterial(0.5f, 0.5f, 0.5f), 2f, new Vec3(center.X, center.Y, center.Z));
        }, 1);

        const int size = SceneFieldPlan.Resolution;
        const float cell = 0.25f, band = SceneFieldPlan.Band * cell;
        int near = 0, far = 0;
        float worst = 0;
        for (var z = 0; z < size; z++)
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var p = origin + (new Vector3(x, y, z) + new Vector3(0.5f)) * cell - center;
                    var q = Vector3.Abs(p) - Vector3.One;
                    var expected = Vector3.Max(q, Vector3.Zero).Length() + MathF.Min(MathF.Max(q.X, MathF.Max(q.Y, q.Z)), 0);
                    var found = distances[(z * size + y) * size + x];
                    if (MathF.Abs(expected) < band - 0.01f)
                    {
                        near++;
                        worst = MathF.Max(worst, MathF.Abs(found - expected));
                    }
                    else if (expected > band + 0.01f)
                    {
                        far++;
                        Assert.Equal(band, found, 0.01f);
                    }
                }

        Assert.True(near > 1000, $"{near} cells near the box");
        Assert.True(far > 100_000, $"{far} cells past the band");
        Assert.True(worst < 0.01f, $"a cell within the band was {worst} from its distance");
        Assert.Contains("Scene field 1 of 1 cascades built, cells 0.25 m, 16 m across, meshes 1, still 1, boxes 0", said);
    }

    /// <summary>
    /// A tunnel between two walls one cell apart is clear along its middle, each cell there in front
    /// of both walls by half a cell, where the walls themselves are inside.
    /// </summary>
    [SkippableFact]
    public void ATunnelOneCellWideIsClearInItsMiddle()
    {
        Needs.Shaders();

        // The cells' middles lie on quarters and an eighth, so the gap from 0 to 0.25 along x holds
        // a row of middles at 0.125 and the walls on either side a unit thick.
        var (distances, origin, _) = Built(1, ecs =>
        {
            PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
            var stone = Render.CreateMaterial(0.5f, 0.5f, 0.5f);
            Slab(ecs, stone, new Vector3(-0.5f, 1f, 0f), new Vector3(1f, 2f, 4f));
            Slab(ecs, stone, new Vector3(0.75f, 1f, 0f), new Vector3(1f, 2f, 4f));
        }, 2);

        const int size = SceneFieldPlan.Resolution;
        const float cell = 0.25f;
        float At(Vector3 p)
        {
            var at = (p - origin) / cell - new Vector3(0.5f);
            var (x, y, z) = ((int)MathF.Round(at.X), (int)MathF.Round(at.Y), (int)MathF.Round(at.Z));
            return distances[(z * size + y) * size + x];
        }

        for (var z = -1.875f; z <= 1.875f; z += cell)
        {
            Assert.InRange(At(new Vector3(0.125f, 1.125f, z)), 0.1f, 0.15f);
            Assert.True(At(new Vector3(-0.375f, 1.125f, z)) < 0f, "the left wall is inside");
            Assert.True(At(new Vector3(0.625f, 1.125f, z)) < 0f, "the right wall is inside");
        }
    }

    /// <summary>
    /// A box moved each frame is stamped where it stands, a frame behind, and the cells it left go
    /// back to what the still meshes make, the band's distance in an empty cascade.
    /// </summary>
    [SkippableFact]
    public void AMovingBoxIsStampedWhereItStandsAndTheCellsItLeftAreEmptyAgain()
    {
        Needs.Shaders();
        var box = Entity.None;
        var x = -2f;
        float[]? distances = null;
        var origin = Vector3.Zero;
        var read = default(BufferRead);
        var moving = true;

        var run = new PictureRun
        {
            Width = 64,
            Height = 48,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
                box = PictureRun.Cube(ecs, Render.CreateMaterial(0.5f, 0.5f, 0.5f), 1f, new Vec3(x, 0.5f, 0f));
                Render.SetSceneField(1, 0.25f);
            },
            EachFrame = world =>
            {
                if (!moving || box.IsNone) return;
                x += 0.05f;
                world.Resource<EcsWorld>().Set(box, Transform.At(x, 0.5f, 0f));
            },
        };

        run.Until("built", _ => SceneField.Plan is { } plan && plan.BuiltOrigin(0) is not null)
            .Until("moved", _ => x >= 1.5f)
            .Do("stopping", _ => moving = false)
            .Wait(4)
            .Do("asking for the field", _ =>
            {
                origin = SceneField.Plan!.BuiltOrigin(0)!.Value;
                Assert.Single(SceneField.Plan.Shapes);
                read = Shaders.BeginImageRead(SceneField.Image);
            })
            .Until("read back", _ => Halves(read, out distances))
            .Go();

        const int size = SceneFieldPlan.Resolution;
        const float cell = 0.25f;
        float At(Vector3 p)
        {
            var at = (p - origin) / cell - new Vector3(0.5f);
            return distances![((int)MathF.Round(at.Z) * size + (int)MathF.Round(at.Y)) * size + (int)MathF.Round(at.X)];
        }

        // Inside where it stopped, half a unit from each face of a unit box, and empty where it
        // started.
        Assert.InRange(At(new Vector3(x, 0.5f, 0f)), -0.5f, -0.25f);
        Assert.Equal(SceneFieldPlan.Band * cell, At(new Vector3(-1.875f, 0.625f, 0.125f)), 0.01f);
    }

    /// <summary>
    /// Four cascades at a budget of one are built a frame each, the finest first, and at a budget of
    /// two in two frames, no frame building more than its budget.
    /// </summary>
    [SkippableTheory]
    [InlineData(1)]
    [InlineData(2)]
    public void TheBudgetKeepsAFramesBuildsToTheCascadesAsked(int budget)
    {
        Needs.Shaders();
        var built = new List<int>();

        var run = new PictureRun
        {
            Width = 64,
            Height = 48,
            Scene = ecs =>
            {
                PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
                Render.SetSceneField(4, 0.25f, budget);
            },
            EachFrame = _ => built.Add(SceneField.BuiltLastFrame),
        };

        run.Until("built", _ => SceneField.Plan is { } plan && plan.BuiltOrigin(3) is not null).Wait(3).Go();

        Assert.All(built, count => Assert.InRange(count, 0, budget));
        var building = built.SkipWhile(count => count == 0).TakeWhile(count => count > 0).ToList();
        Assert.Equal(Enumerable.Repeat(budget, 4 / budget), building);
    }

    /// <summary>A slab of the given middle and size.</summary>
    private static void Slab(EcsWorld ecs, AssetHandle material, Vector3 middle, Vector3 size)
    {
        var slab = ecs.Spawn();
        Render.SetMesh(ecs, slab, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
        Render.SetMaterial(ecs, slab, material);
        ecs.Add(slab, Transform.At(middle.X, middle.Y, middle.Z));
    }

    /// <summary>
    /// Runs a scene with a field of the cascades given until its meshes have settled and their
    /// cascade is built, and reads the first cascade back, with its corner and what <c>gi.state</c>
    /// said then.
    /// </summary>
    private static (float[] Distances, Vector3 Origin, string Said) Built(int cascades, Action<EcsWorld> scene, int meshes)
    {
        float[]? distances = null;
        var origin = Vector3.Zero;
        var said = "";
        var read = default(BufferRead);

        var run = new PictureRun
        {
            Width = 64,
            Height = 48,
            Scene = ecs =>
            {
                scene(ecs);
                Render.SetSceneField(cascades, 0.25f);
            },
        };

        run.Until("settled", _ => SceneField.Plan is { } plan && plan.StillCount == meshes && plan.Shapes.Count == 0)
            .Wait(4)
            .Do("asking for the field", _ =>
            {
                origin = SceneField.Plan!.BuiltOrigin(0)!.Value;
                said = ConsoleFieldCommands.State();
                read = Shaders.BeginImageRead(SceneField.Image);
            })
            .Until("read back", _ => Halves(read, out distances))
            .Go();

        return (distances!, origin, said);
    }

    /// <summary>An image of half floats read back, as floats.</summary>
    private static bool Halves(BufferRead read, out float[]? values)
    {
        values = null;
        if (!Shaders.TryReadBuffer(read, out var bytes) || bytes is null) return false;
        values = [.. MemoryMarshal.Cast<byte, Half>(bytes).ToArray().Select(half => (float)half)];
        return true;
    }
}
