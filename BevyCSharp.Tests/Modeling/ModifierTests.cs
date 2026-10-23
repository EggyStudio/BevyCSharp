using System.Diagnostics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the modifier stack, each modifier's result on exact counts and volumes, the base left as
/// it was and still editable under it, the stack reordered, taken away from and applied, its lines
/// in the recipe, and the build of a stack against a budget.
/// </summary>
/// <remarks>
/// A modifier changes what <see cref="EditMesh.Built"/> answers and the build draws, and leaves
/// the mesh's own counts as they were, so each test reads both.
/// </remarks>
public sealed class ModifierTests
{
    private const float Close = 1e-4f;

    private static EditMesh Cube() => new EditMesh().Box(new Vec3(2f));

    /// <summary>The half of a box of two from X of zero, open where it meets the plane.</summary>
    private static EditMesh Half() => new EditMesh().Box(new Vec3(1f, 2f, 2f)).SelectFacing(-Vec3.UnitX).Delete();

    [Fact]
    public void AMirrorOnAHalfBoxBuildsTheBoxWithItsMiddleLoopAndLeavesTheHalf()
    {
        var mesh = Half().AddModifier("mirror x");
        var built = mesh.Built();

        // The whole box, its two halves' faces kept apart, so the counts of a box with a loop
        // round its middle, and the half itself as it was.
        var whole = Cube().SelectEdges([(0, 1)]).LoopCut(0.5f);
        Assert.Equal((whole.VertexCount, whole.EdgeCount, whole.FaceCount), (built.VertexCount, built.EdgeCount, built.FaceCount));
        Assert.True(built.IsClosed);
        Assert.Equal(8f, built.Volume, Close);
        Assert.Equal((8, 5, 4), (mesh.VertexCount, mesh.FaceCount, mesh.OpenEdges));

        // The base edited under it, the build follows.
        mesh.SelectFacing(Vec3.UnitY).Move(new Vec3(0f, 1f, 0f));
        Assert.Equal(12f, mesh.Built().Volume, Close);

        // Two axes, a quarter mirrored into the whole.
        var quarter = new EditMesh().Box(new Vec3(1f, 2f, 1f)).SelectFacing(-Vec3.UnitX).SelectFacing(-Vec3.UnitZ, SelectionMode.Add).Delete();
        var four = quarter.AddModifier("mirror x z").Built();
        Assert.True(four.IsClosed);
        Assert.Equal(8f, four.Volume, Close);
    }

    [Fact]
    public void AnArrayOfThreeIsThreeBoxesTwoMetersApart()
    {
        var mesh = new EditMesh().Box(new Vec3(1f)).AddModifier("array 3 2 0 0");
        var built = mesh.Built();

        Assert.Equal((24, 18), (built.VertexCount, built.FaceCount));
        Assert.Equal(3f, built.Volume, Close);
        Assert.Equal((Vec3.Zero, new Vec3(5f, 1f, 1f)), built.Bounds);
        Assert.Equal((8, 6), (mesh.VertexCount, mesh.FaceCount));

        // Along a line through three points, the copies spread evenly over its eight meters.
        var along = new EditMesh().Box(new Vec3(1f)).AddModifier("array 3 along 0 0 0 4 0 0 4 0 4").Built();
        Assert.Equal((Vec3.Zero, new Vec3(5f, 1f, 5f)), along.Bounds);
        Assert.Equal(3f, along.Volume, Close);
    }

    [Fact]
    public void ABoxSubdividedToLevelOneHas26VerticesAndToLevelTwo98()
    {
        var one = Cube().AddModifier("subdivision 1").Built();
        Assert.Equal((26, 48, 24), (one.VertexCount, one.EdgeCount, one.FaceCount));
        Assert.True(one.IsClosed);

        var two = Cube().AddModifier("subdivision 2").Built();
        Assert.Equal((98, 192, 96), (two.VertexCount, two.EdgeCount, two.FaceCount));
        Assert.True(two.IsClosed);

        // Each level rounds the cube further in, its corners drawn toward the middle.
        Assert.InRange(two.Volume, 2f, one.Volume - 0.1f);
        Assert.InRange(one.Volume, 3f, 8f - 1f);

        // An open edge stays where it ends, a plane subdivided keeping its outline's middles.
        var plane = new EditMesh().Plane(2f, 2f).AddModifier("subdivision 1").Built();
        Assert.Equal((9, 4), (plane.VertexCount, plane.FaceCount));
        Assert.Equal(4, plane.OpenEdges / 2);
    }

    [Fact]
    public void ASolidifiedPlaneIsAClosedSlabAndASolidifiedBoxTwoShells()
    {
        var up = new EditMesh().Plane(2f, 2f).AddModifier("solidify 0.5").Built();
        Assert.Equal((8, 12, 6), (up.VertexCount, up.EdgeCount, up.FaceCount));
        Assert.True(up.IsClosed);
        Assert.Equal(2f, up.Volume, Close);
        Assert.Equal((Vec3.Zero, new Vec3(2f, 0.5f, 2f)), up.Bounds);

        var down = new EditMesh().Plane(2f, 2f).AddModifier("solidify -0.5").Built();
        Assert.True(down.IsClosed);
        Assert.Equal(2f, down.Volume, Close);
        Assert.Equal(-0.5f, down.Bounds.Low.Y, Close);

        // A box's walls a quarter thick inward, as thick at the corners as in the middles.
        var shell = Cube().AddModifier("solidify -0.25").Built();
        Assert.Equal((16, 12), (shell.VertexCount, shell.FaceCount));
        Assert.True(shell.IsClosed);
        Assert.Equal(8f - 1.5f * 1.5f * 1.5f, shell.Volume, Close);
    }

    [Fact]
    public void ABevelModifierBevelsEveryEdgeOrThoseMeetingPastAnAngle()
    {
        var every = Cube().AddModifier("bevel 0.25").Built();
        var by = Cube().SelectAll().Bevel(0.25f);
        Assert.Equal((by.VertexCount, by.FaceCount), (every.VertexCount, every.FaceCount));
        Assert.Equal(by.Volume, every.Volume, Close);

        // A cylinder's sides meet at 45 degrees and its ends at 90, so past 60 only its rims.
        var cylinder = new EditMesh().Cylinder(1f, 2f, 8).AddModifier("bevel 0.05 1 angle 60").Built();
        Assert.True(cylinder.IsClosed);
        Assert.Equal(10 + 16, cylinder.FaceCount);
        Assert.Equal(2 * 8 * 2, cylinder.VertexCount);
    }

    [Fact]
    public void ABooleanModifierTakesASolidAwayAndTriangulateCutsEveryFace()
    {
        var mesh = Cube().AddModifier("boolean subtract box 1 1 1 at 1.5 1.5 1.5");
        var built = mesh.Built();
        Assert.True(built.IsClosed);
        Assert.Equal(8f - 0.125f, built.Volume, Close);
        Assert.Equal(6, mesh.FaceCount);

        var triangles = Cube().AddModifier("triangulate").Built();
        Assert.Equal(12, triangles.FaceCount);
        Assert.All(Enumerable.Range(0, triangles.FaceCount), face => Assert.Equal(3, triangles.Corners(face).Length));

        // A boolean with a model that is not there is refused as it is added, the stack as it was.
        var missing = Cube();
        Assert.Throws<FileNotFoundException>(() => missing.AddModifier("boolean union models/not-there.model"));
        Assert.Empty(missing.Modifiers);
    }

    [Fact]
    public void TheStackIsReorderedRemovedFromAndAppliedIntoTheBase()
    {
        var mesh = Cube().AddModifier("subdivision 1").AddModifier("triangulate");
        Assert.Equal(48, mesh.Built().FaceCount);

        // Triangulated first, each triangle is cut into three four sided faces.
        mesh.MoveModifier(1, 0);
        Assert.Equal(new[] { "triangulate", "subdivision 1" }, mesh.Modifiers);
        Assert.Equal(36, mesh.Built().FaceCount);

        mesh.RemoveModifier(0);
        Assert.Equal(24, mesh.Built().FaceCount);

        // Applied, the subdivision is the base's own and the stack is empty.
        mesh.ApplyModifier(0);
        Assert.Empty(mesh.Modifiers);
        Assert.Equal((26, 24), (mesh.VertexCount, mesh.FaceCount));
        Assert.Equal(24, mesh.SelectedFaceCount);

        Assert.Equal(
            new[] { "box 2 2 2", "modifier add subdivision 1", "modifier add triangulate", "modifier move 1 0", "modifier remove 0", "modifier apply 0" },
            mesh.Recipe);

        var again = EditMesh.FromRecipe(mesh.RecipeText);
        Assert.Equal((mesh.VertexCount, mesh.FaceCount), (again.VertexCount, again.FaceCount));
        Assert.Equal(mesh.Volume, again.Volume, Close);

        // Taken back, the modifier is in the stack again and the base is the box.
        Assert.True(mesh.Undo());
        Assert.Equal(new[] { "subdivision 1" }, mesh.Modifiers);
        Assert.Equal(8, mesh.VertexCount);
    }

    [Fact]
    public void AModifiersLineIsWrittenAsItsWordsAndALineThatIsNoneIsRefused()
    {
        var mesh = Cube()
            .AddModifier("mirror  x  y at 1 0 0")
            .AddModifier("array 2 0 3 0")
            .AddModifier("bevel 0.1 2 angle 30")
            .AddModifier("solidify -0.1")
            .AddModifier("boolean intersect box 4 4 4 at -1 -1 -1");

        Assert.Equal(new[] { "mirror x y at 1 0 0", "array 2 0 3 0", "bevel 0.1 2 angle 30", "solidify -0.1", "boolean intersect box 4 4 4 at -1 -1 -1" }, mesh.Modifiers);

        Assert.Throws<FormatException>(() => Cube().AddModifier("twist 90"));
        Assert.Throws<FormatException>(() => Cube().AddModifier("mirror"));
        Assert.Throws<FormatException>(() => Cube().AddModifier("subdivision 1 2"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cube().AddModifier($"subdivision {EditMesh.MostLevels + 1}"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cube().AddModifier("solidify 0"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cube().MoveModifier(0, 1));
    }

    [Fact]
    public void TheBuildDrawsAndCollidesAsTheStackMakesIt()
    {
        var mesh = Cube().AddModifier("subdivision 1");

        var built = Modeling.Build(mesh);
        Assert.Equal(24 * 4, built.Sum(slot => slot.Positions.Length));
        Assert.Equal(24 * 2 * 3, built.Sum(slot => slot.Indices?.Length ?? 0));
    }

    [Fact]
    public void AStackOfFourBuildsWithinItsBudget()
    {
        // A half mirrored, arrayed four times, beveled in two segments and subdivided twice, 5,760
        // faces. It built in 13 ms once warm and 70 ms the first time on four cores, and 200 ms is
        // the budget a level's model is built within when the window changes it.
        EditMesh Stack() => new EditMesh().Box(new Vec3(1f, 2f, 2f)).AddModifier("mirror x").AddModifier("array 4 3 0 0").AddModifier("bevel 0.05 2").AddModifier("subdivision 2");

        Modeling.Build(Stack());
        var mesh = Stack();
        mesh.SelectFacing(Vec3.UnitY).Move(new Vec3(0f, 0.1f, 0f));

        var clock = Stopwatch.StartNew();
        var built = Modeling.Build(mesh);
        clock.Stop();

        Assert.Equal(5760 * 2, built.Sum(slot => slot.Indices?.Length ?? 0) / 3);
        Assert.InRange(clock.Elapsed.TotalMilliseconds, 0, 200);
    }
}
