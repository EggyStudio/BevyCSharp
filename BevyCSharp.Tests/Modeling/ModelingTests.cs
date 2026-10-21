using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers an edit mesh built into what the engine draws and collides with, a mesh of each slot
/// with each face's own corners, normals and texture coordinates in meters, smoothing within an
/// angle, concave faces cut inside their outlines, the meshes made once and written in place after
/// a change, and a collider a ball lands on.
/// </summary>
[Collection("engine")]
public sealed class ModelingTests
{
    private const float Close = 1e-4f;

    [Fact]
    public void ABoxBuildsIntoOneMeshOfTwentyFourCornersEachFaceWithItsOwnNormal()
    {
        var built = Modeling.Build(new EditMesh().Box(new Vec3(2f, 3f, 4f)));

        var data = Assert.Single(built);
        Assert.Equal((24, 36), (data.Positions.Length, data.Indices!.Length));

        // Each corner of the top faces up and is laid out by X and Z, in meters, so the top's
        // texture spans two by four.
        var top = Enumerable.Range(0, 24).Where(corner => data.Normals![corner] == Vec3.UnitY).ToArray();
        Assert.Equal(4, top.Length);
        Assert.All(top, corner => Assert.Equal(3f, data.Positions[corner].Y));
        Assert.Equal(2f, top.Max(corner => data.Uvs![corner * 2]) - top.Min(corner => data.Uvs![corner * 2]), Close);
        Assert.Equal(4f, top.Max(corner => data.Uvs![corner * 2 + 1]) - top.Min(corner => data.Uvs![corner * 2 + 1]), Close);

        // A wall's second coordinate runs down the world, so a texture stands upright on it.
        var front = Enumerable.Range(0, 24).Where(corner => data.Normals![corner] == Vec3.UnitZ).ToArray();
        var highest = front.MaxBy(corner => data.Positions[corner].Y);
        var lowest = front.MinBy(corner => data.Positions[corner].Y);
        Assert.Equal(3f, data.Uvs![lowest * 2 + 1] - data.Uvs[highest * 2 + 1], Close);

        // Every triangle winds the way its corners face.
        AllTrianglesFaceTheirNormals(data);
    }

    [Fact]
    public void FacesInSlotsBuildIntoAMeshEachInTheOrderOfTheSlots()
    {
        var mesh = new EditMesh().Box(new Vec3(4f, 3f, 4f)).SelectFacing(Vec3.UnitY).Material("ceiling").SelectAll().Material("wall");
        var built = Modeling.Build(mesh);

        Assert.Equal(new[] { EditMesh.DefaultSlot, "ceiling", "wall" }, mesh.Slots);
        Assert.Equal(3, built.Length);
        Assert.Empty(built[0].Positions);
        Assert.Empty(built[1].Positions);
        Assert.Equal((24, 36), (built[2].Positions.Length, built[2].Indices!.Length));

        mesh.SelectFacing(-Vec3.UnitY).Material("floor");
        built = Modeling.Build(mesh);
        Assert.Equal((20, 4), (built[2].Positions.Length, built[3].Positions.Length));
    }

    [Fact]
    public void ACylinderSmoothedPastTheAngleBetweenItsSidesIsRoundAndItsEndsStayFlat()
    {
        var mesh = new EditMesh().Cylinder(1f, 2f, 8, Vec3.Zero, centered: true).SelectAll().Shade(true, 50f);
        var data = Modeling.Build(mesh)[0];

        for (var corner = 0; corner < data.Positions.Length; corner++)
        {
            var normal = data.Normals![corner];
            var point = data.Positions[corner];
            if (MathF.Abs(normal.Y) > 0.5f)
            {
                // An end, ninety degrees from the sides, keeps its own normal.
                Assert.Equal(1f, MathF.Abs(normal.Y), Close);
                continue;
            }

            // A side's corner faces straight out from the axis, as a round side's would.
            var outward = new Vec3(point.X, 0f, point.Z).Normalized;
            Assert.Equal(0f, normal.Y, Close);
            Assert.True(Vec3.Dot(normal, outward) > 0.9999f, $"{normal} at {point}");
        }

        // Smoothed at thirty degrees, less than the forty-five between sides, it stays faceted.
        var faceted = Modeling.Build(new EditMesh().Cylinder(1f, 2f, 8).SelectAll().Shade(true))[0];
        Assert.Equal(10, faceted.Normals!.Distinct().Count());
    }

    [Fact]
    public void AFlightsConcaveSidesAreCutInsideTheirOutlines()
    {
        var mesh = new EditMesh().Stairs(6, 0.2f, 0.3f, 1.5f);
        var data = Modeling.Build(mesh)[0];

        // Each side of 14 corners is 12 triangles, and the 14 faces of four corners 2 each.
        Assert.Equal((2 * 12 + 14 * 2) * 3, data.Indices!.Length);
        AllTrianglesFaceTheirNormals(data);
        Assert.Equal(mesh.Volume, Volume(data.Positions, data.Indices), Close);
    }

    [Fact]
    public void TheColliderIsTheMeshsOwnVerticesWithItsFacesCutIntoTriangles()
    {
        var mesh = new EditMesh().Stairs(4, 0.25f, 0.5f, 2f);
        var shape = Modeling.Collider(mesh);

        Assert.Equal(20, shape.Positions!.Length);
        Assert.Equal((2 * 8 + 10 * 2) * 3, shape.Indices!.Length);
        Assert.Equal(2.5f, Volume(shape.Positions, shape.Indices), Close);
        Assert.Throws<ArgumentException>(() => Modeling.Collider(new EditMesh()));
    }

    [Fact]
    public void ABallDroppedOnABuiltFloorLandsOnIt()
    {
        var resting = float.NaN;

        using var harness = new EngineHarness(frames: 1);
        harness.OnContext(Stage.Startup, ctx =>
        {
            using var physics = new PhysicsWorld(new PhysicsSettings());
            var floor = ctx.Ecs.Spawn();
            ctx.Ecs.Add(floor, Transform.Identity);
            physics.Add(floor, Modeling.Collider(new EditMesh().Box(new Vec3(10f, 1f, 10f), new Vec3(0f, 0.5f, 0f), centered: true)), BodyKind.Static, Transform.Identity);

            var ball = ctx.Ecs.Spawn();
            var start = Transform.At(0f, 5f, 0f);
            ctx.Ecs.Add(ball, start);
            physics.Add(ball, PhysicsShape.Sphere(0.5f), BodyKind.Dynamic, start);

            for (var step = 0; step < 192; step++) physics.Step(ctx.Ecs, 1f / 64f);
            resting = ctx.Ecs.GetOrDefault<Transform>(ball).Translation.Y;
        });
        harness.Run();

        // On the floor's top at a meter, the ball's middle half a meter above it.
        Assert.Equal(1.5f, resting, 0.02f);
    }

    [SkippableFact]
    public void AModelsMeshesAreMadeOnceAndWrittenInPlaceAfterAChange()
    {
        Needs.Renderer();

        AssetHandle[] first = [], unchanged = [], changed = [], slotted = [];
        var (before, after) = (0f, 0f);
        var (tangents, tangentsAfter) = (false, false);

        using var harness = new EngineHarness(frames: 1);
        harness.OnContext(Stage.Startup, _ =>
        {
            var mesh = new EditMesh().Box(new Vec3(2f, 3f, 4f));
            first = Modeling.Meshes(mesh);
            unchanged = Modeling.Meshes(mesh.SelectFacing(Vec3.UnitY));
            before = Highest(first[0]);
            tangents = HasTangents(first[0]);

            changed = Modeling.Meshes(mesh.Move(new Vec3(0f, 1f, 0f)));
            after = Highest(changed[0]);
            tangentsAfter = HasTangents(changed[0]);

            slotted = Modeling.Meshes(mesh.Material("ceiling"));
        });
        harness.Run();

        Assert.Single(first);
        Assert.NotEqual(AssetHandle.None, first[0]);
        Assert.Equal(first, unchanged);
        Assert.Equal(first, changed);
        Assert.Equal((3f, 4f), (before, after));

        // Tangents worked out from the faces' texture coordinates, and again after the write.
        Assert.Equal((true, true), (tangents, tangentsAfter));

        // The top in a slot of its own takes a mesh of its own, and the first keeps its handle.
        Assert.Equal(2, slotted.Length);
        Assert.Equal(first[0], slotted[0]);
        Assert.NotEqual(AssetHandle.None, slotted[1]);

        static bool HasTangents(AssetHandle handle) =>
            Render.TryGetMeshInfo(handle, out var info) && info.Attributes.HasFlag(MeshAttributes.Tangents);

        static float Highest(AssetHandle handle)
        {
            Assert.True(Render.TryReadMesh(handle, out var read));
            return read!.Positions.Max(point => point.Y);
        }
    }

    /// <summary>Asserts each triangle's winding gives a normal along its corners' normals.</summary>
    private static void AllTrianglesFaceTheirNormals(MeshData data)
    {
        var indices = data.Indices!;
        for (var t = 0; t < indices.Length; t += 3)
        {
            var (a, b, c) = (data.Positions[indices[t]], data.Positions[indices[t + 1]], data.Positions[indices[t + 2]]);
            var winding = Vec3.Cross(b - a, c - a);
            Assert.True(Vec3.Dot(winding, data.Normals![indices[t]]) > 0f, $"the triangle at {t / 3} winds against its normal");
        }
    }

    /// <summary>The volume triangles close, by the solid each makes with the origin.</summary>
    private static float Volume(Vec3[] positions, uint[] indices)
    {
        double sum = 0;
        for (var t = 0; t < indices.Length; t += 3)
        {
            var (a, b, c) = (positions[indices[t]], positions[indices[t + 1]], positions[indices[t + 2]]);
            sum += Vec3.Dot(a, Vec3.Cross(b, c));
        }

        return (float)(sum / 6.0);
    }
}
