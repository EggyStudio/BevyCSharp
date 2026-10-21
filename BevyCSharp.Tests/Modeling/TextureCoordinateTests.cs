using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the faces' texture coordinates, the default and each projection on a known face of a
/// box, the coordinates moved, scaled and turned as a whole and face by face, a texel density on a
/// two meter face, the coordinates carried through the operations that change a face's corners, and
/// the recipe making them again.
/// </summary>
public sealed class TextureCoordinateTests
{
    private const float Near = 1e-4f;

    private static int FaceFacing(EditMesh mesh, Vec3 way) => Enumerable.Range(0, mesh.FaceCount).Single(face => (mesh.Normal(face) - way).Length < 1e-4f);

    /// <summary>The coordinates of a face's corner at a point.</summary>
    private static Vec2 At(EditMesh mesh, int face, Vec3 point)
    {
        var corners = mesh.Corners(face).ToArray();
        var corner = Array.FindIndex(corners, vertex => (mesh.Position(vertex) - point).Length < 1e-4f);
        Assert.True(corner >= 0, $"face {face} has no corner at {point}");
        return mesh.UvsOf(face)[corner];
    }

    private static void Same(Vec2 expected, Vec2 actual)
    {
        Assert.Equal(expected.X, actual.X, Near);
        Assert.Equal(expected.Y, actual.Y, Near);
    }

    /// <summary>A box of two meters from the origin, its face toward +Z, where Z is two, alone selected.</summary>
    private static (EditMesh Mesh, int Front) Box()
    {
        var mesh = new EditMesh().Box(new Vec3(2f));
        var front = FaceFacing(mesh, Vec3.UnitZ);
        mesh.SelectFaces([front]);
        return (mesh, front);
    }

    [Fact]
    public void EachProjectionGivesTheFrontOfABoxItsKnownCoordinates()
    {
        var (mesh, front) = Box();
        var (low, high) = (new Vec3(0f, 0f, 2f), new Vec3(2f, 2f, 2f));

        // The default, the face seen from in front, X to the right and Y up, its second coordinate
        // growing downward as a texture's does.
        Assert.False(mesh.HasOwnUvs(front));
        Same(new Vec2(0f, 0f), At(mesh, front, low));
        Same(new Vec2(2f, -2f), At(mesh, front, high));

        // From above, X and Z.
        mesh.UvPlanar(Vec3.UnitY);
        Assert.True(mesh.HasOwnUvs(front));
        Same(new Vec2(0f, 2f), At(mesh, front, low));
        Same(new Vec2(2f, 2f), At(mesh, front, high));

        // From a slant between +X and +Z, the right running along +X and -Z and down along -Y.
        mesh.UvPlanar(new Vec3(1f, 0f, 1f));
        Same(new Vec2(-2f / MathF.Sqrt(2f), 0f), At(mesh, front, low));
        Same(new Vec2(0f, -2f), At(mesh, front, high));

        // The box projection, the default written onto the face.
        mesh.UvBox();
        Same(new Vec2(2f, -2f), At(mesh, front, high));

        // Round the box's upright axis through its middle, the corners a root two away, a left one
        // three eighths of a turn round from +X and a right one an eighth, and down from the middle.
        mesh.UvCylinder(Vec3.UnitY, Vec3.One);
        var root2 = MathF.Sqrt(2f);
        Same(new Vec2(-3f * MathF.PI / 4f * root2, 1f), At(mesh, front, low));
        Same(new Vec2(-MathF.PI / 4f * root2, -1f), At(mesh, front, high));

        // Round the middle as a globe, every corner a root three away and a latitude of the angle
        // whose tangent is one over root two below or above the equator.
        mesh.UvSphere(Vec3.One);
        var root3 = MathF.Sqrt(3f);
        var latitude = MathF.Atan2(1f, root2);
        Same(new Vec2(-3f * MathF.PI / 4f * root3, latitude * root3), At(mesh, front, low));
        Same(new Vec2(-MathF.PI / 4f * root3, -latitude * root3), At(mesh, front, high));

        // Back to the default, which follows the face.
        mesh.UvReset();
        Assert.False(mesh.HasOwnUvs(front));
        Same(new Vec2(2f, -2f), At(mesh, front, high));

        Assert.Equal(
            new[] { "uv planar +y", "uv planar 1 0 1", "uv box", "uv cylinder y at 1 1 1", "uv sphere at 1 1 1", "uv reset" },
            mesh.Recipe.Skip(2));
    }

    [Fact]
    public void ACylindersSideAcrossTheSeamIsWrappedFromItsOwnMiddle()
    {
        // Eight sides about Y, which face along the axes, the one facing -X across the seam, where
        // the angle round comes back from a half turn to minus one, its corners an eighth of a turn
        // apart.
        var mesh = new EditMesh().Cylinder(1f, 2f, 8, Vec3.Zero, centered: true);
        var behind = Enumerable.Range(0, mesh.FaceCount).OrderBy(face => mesh.Center(face).X).First();
        mesh.SelectFaces([behind]).UvCylinder(Vec3.UnitY, Vec3.Zero);

        // Its two ends as far apart round the cylinder as a side is wide, and not a whole turn.
        var uvs = mesh.UvsOf(behind);
        var width = uvs.Max(uv => uv.X) - uvs.Min(uv => uv.X);
        Assert.Equal(MathF.PI / 4f, width, Near);
    }

    [Fact]
    public void CoordinatesAreMovedScaledAndTurnedAboutTheirMiddleAsAWholeOrFaceByFace()
    {
        var (mesh, front) = Box();
        var low = new Vec3(0f, 0f, 2f);

        // The front's default runs from (0, -2) to (2, 0), its middle (1, -1).
        Same(new Vec2(0.5f, 0.25f), At(mesh.UvMove(0.5f, 0.25f), front, low));
        Same(new Vec2(-1f, 1f), At(mesh.UvReset().UvScale(2f), front, low));
        Same(new Vec2(0f, -2f), At(mesh.UvReset().UvRotate(90f), front, low));
        Same(new Vec2(0f, 2f), At(mesh.UvReset().UvScale(1f, 3f), front, low));

        // With the top, whose default runs from (0, 0) to (2, 2), the middle of both is (1, 0), so
        // the front scaled with it moves further than the front scaled about its own middle.
        var top = FaceFacing(mesh, Vec3.UnitY);
        mesh.UvReset().SelectFaces([front, top]);
        Same(new Vec2(-1f, 0f), At(mesh.UvScale(2f), front, low));
        Same(new Vec2(-1f, 1f), At(mesh.UvReset().UvScale(2f, each: true), front, low));

        Assert.Equal(new[] { "uv scale 2", "uv reset", "uv scale 2 each" }, mesh.Recipe.TakeLast(3));
        Assert.Contains("uv scale 1 3", mesh.Recipe);
    }

    [Fact]
    public void ADensityOfHalfATexturesWidthAMeterShowsItOnceOnATwoMeterFace()
    {
        // 256 pixels a meter of a texture 512 wide, so two meters show it once.
        var (mesh, front) = Box();
        mesh.UvDensity(256f, 512);

        var uvs = mesh.UvsOf(front);
        Assert.Equal(1f, uvs.Max(uv => uv.X) - uvs.Min(uv => uv.X), Near);
        Assert.Equal(1f, uvs.Max(uv => uv.Y) - uvs.Min(uv => uv.Y), Near);

        // Scaled about the middle, (1, -1), by a half.
        Same(new Vec2(0.5f, -0.5f), At(mesh, front, new Vec3(0f, 0f, 2f)));
        Assert.Equal("uv density 256 512", mesh.Recipe[^1]);

        Assert.Throws<ArgumentOutOfRangeException>(() => mesh.UvDensity(0f, 512));
        Assert.Throws<ArgumentOutOfRangeException>(() => mesh.UvDensity(256f, 0));
    }

    [Fact]
    public void AFacesOwnCoordinatesStayOnItThroughAnExtrudeAFlipATriangulationAndAnInset()
    {
        // Laid from above, the front's coordinates are its corners' X and Z, which an extrude
        // carries out to Z three with the face, where the default would follow it.
        var (mesh, front) = Box();
        mesh.UvPlanar(Vec3.UnitY).Extrude(1f);
        Same(new Vec2(0f, 2f), At(mesh, front, new Vec3(0f, 0f, 3f)));

        // Turned the other way, each corner keeps its own.
        mesh.Flip();
        Same(new Vec2(2f, 2f), At(mesh, front, new Vec3(2f, 2f, 3f)));
        mesh.Flip();

        // Cut into two triangles, each keeps those of its corners.
        mesh.Triangulate();
        var halves = Enumerable.Range(0, mesh.FaceCount).Where(mesh.IsFaceSelected).ToArray();
        Assert.Equal(2, halves.Length);
        Assert.All(halves, half => Assert.True(mesh.HasOwnUvs(half)));
        foreach (var half in halves)
        {
            foreach (var corner in mesh.Corners(half))
            {
                var point = mesh.Position(corner);
                Same(new Vec2(point.X, 2f), At(mesh, half, point));
            }
        }

        // An inset's smaller face takes the coordinates the face had where its corners moved to,
        // a quarter of a meter in on the front's default, given to it first.
        var (inset, face) = Box();
        inset.UvBox().UvMove(0.5f, 0f).Inset(0.25f);
        Same(new Vec2(0.75f, -0.25f), At(inset, face, new Vec3(0.25f, 0.25f, 2f)));
        Same(new Vec2(2.25f, -1.75f), At(inset, face, new Vec3(1.75f, 1.75f, 2f)));
    }

    [Fact]
    public void AWindowCutThroughAWallLeavesTheWallsPiecesTheCoordinatesItHadThere()
    {
        // A wall facing -Z, its coordinates the default moved half a picture along, then a window
        // cut through it.
        var wall = new EditMesh().Box(new Vec3(4f, 3f, 0.2f));
        wall.SelectFacing(-Vec3.UnitZ).UvBox().UvMove(0.5f, 0f);
        wall.Window(1f, 1.2f, 2f, 0.9f);

        // Every piece of the wall's face keeps them, the default being -X and -Y seen from -Z.
        var pieces = Enumerable.Range(0, wall.FaceCount)
            .Where(face => (wall.Normal(face) + Vec3.UnitZ).Length < 1e-4f && Math.Abs(wall.Center(face).Z) < 1e-4f)
            .ToArray();
        Assert.NotEmpty(pieces);
        foreach (var piece in pieces)
        {
            Assert.True(wall.HasOwnUvs(piece), $"piece {piece} lost its coordinates");
            foreach (var corner in wall.Corners(piece))
            {
                var point = wall.Position(corner);
                Same(new Vec2(-point.X + 0.5f, -point.Y), At(wall, piece, point));
            }
        }

        // The opening's sides, which the cutter's faces made, take the default.
        Assert.Contains(Enumerable.Range(0, wall.FaceCount), face => !wall.HasOwnUvs(face));
    }

    [Fact]
    public void TheRecipeMakesTheCoordinatesAgainAndAnUndoTakesThemBack()
    {
        var (mesh, _) = Box();
        mesh.UvCylinder(Vec3.UnitY).UvScale(2f, 1f, each: true).UvRotate(30f).SelectAll().UvDensity(512f, 1024).Extrude(0.5f);

        var made = EditMesh.FromRecipe(mesh.RecipeText);
        Assert.Equal(mesh.FaceCount, made.FaceCount);
        for (var face = 0; face < mesh.FaceCount; face++)
        {
            Assert.Equal(mesh.HasOwnUvs(face), made.HasOwnUvs(face));
            var (ours, theirs) = (mesh.UvsOf(face), made.UvsOf(face));
            for (var i = 0; i < ours.Length; i++) Same(ours[i], theirs[i]);
        }

        // Each line read back as it was written.
        foreach (var line in new[] { "uv planar -z", "uv planar 0.3 0.4 0.5", "uv cylinder 1 1 0 at 0 1 0", "uv scale 2 1 each", "uv rotate 45 each", "uv density 256 512", "uv move -0.5 0.25" })
            Assert.Equal(line, Box().Mesh.Run(line).Recipe[^1]);

        var (moved, front) = Box();
        moved.UvMove(0.5f, 0f);
        Assert.True(moved.HasOwnUvs(front));
        moved.Undo();
        Assert.False(moved.HasOwnUvs(front));

        Assert.Throws<FormatException>(() => Box().Mesh.Run("uv wrap"));
        Assert.Throws<ArgumentException>(() => Box().Mesh.Run("uv planar 0 0 0"));
    }
}
