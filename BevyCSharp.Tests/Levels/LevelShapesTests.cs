using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The brushes a level is blocked out with, a box, a ramp, an arch and a doorway with a rounded
/// opening, each closing a convex solid with its faces mapped as a new face is, and a brush turned
/// about the vertical with its mapping taken again from the world.
/// </summary>
public sealed class LevelShapesTests
{
    [Fact]
    public void ABoxIsSixFacesOfFourCornersMappedAsANewFaceIs()
    {
        var box = LevelBrush.Box(new LevelPoint(-64, -32, 0), new LevelPoint(64, 32, 96), "proto/wall");
        var polygons = box.Polygons();

        Assert.Equal(6, polygons.Length);
        Assert.All(polygons, polygon => Assert.Equal(4, polygon!.Length));
        var corners = polygons.SelectMany(polygon => polygon!).Distinct().ToList();
        Assert.Equal((-64.0, 64.0, -32.0, 32.0, 0.0, 96.0), (corners.Min(p => p.X), corners.Max(p => p.X), corners.Min(p => p.Y), corners.Max(p => p.Y), corners.Min(p => p.Z), corners.Max(p => p.Z)));
        Assert.Equal(128.0 * 64 * 96, Volume(box), 6);

        // A floor's material along X and minus Y, a wall's along X or Y and down, a sixteenth's scale.
        var top = box.Faces.Single(face => face.Normal.Z > 0.99);
        Assert.Equal((new LevelPoint(1, 0, 0), new LevelPoint(0, -1, 0), LevelBrush.DefaultScale), (top.UAxis, top.VAxis, top.ScaleU));
        var east = box.Faces.Single(face => face.Normal.X > 0.99);
        Assert.Equal((new LevelPoint(0, 1, 0), new LevelPoint(0, 0, -1)), (east.UAxis, east.VAxis));

        // Written and read back the same.
        Assert.Equal(box.Faces, Written(box).Faces);
    }

    [Fact]
    public void ARampsSlopeRisesAlongXFromItsFootToItsTop()
    {
        var ramp = LevelBrush.Wedge(new LevelPoint(0, -64, 0), new LevelPoint(128, 64, 64), "proto/stairs");
        var polygons = ramp.Polygons();

        Assert.Equal([3, 3, 4, 4, 4], polygons.Select(polygon => polygon!.Length).Order());
        var slope = ramp.Faces.Single(face => face.Normal.Z > 0.1 && face.Normal.X < -0.1);
        Assert.Equal(-64 / Math.Sqrt(64 * 64 + 128 * 128), slope.Normal.X, 9);
        Assert.Equal(128.0 * 128 * 64 / 2, Volume(ramp), 6);
    }

    [Fact]
    public void AnArchIsItsRingCutIntoStraightSegments()
    {
        var center = new LevelPoint(0, 0, 64);
        var arch = LevelBrush.Arch(center, 48, 16, 32, "proto/stairs", segments: 8);

        Assert.Equal(8, arch.Count);
        foreach (var segment in arch)
        {
            var corners = segment.Polygons().OfType<LevelPoint[]>().SelectMany(polygon => polygon).Distinct().ToList();
            Assert.Equal(8, corners.Count);
            Assert.All(corners, corner =>
            {
                var reach = Math.Sqrt((corner.X - center.X) * (corner.X - center.X) + (corner.Z - center.Z) * (corner.Z - center.Z));
                Assert.True(Math.Abs(reach - 48) < 1e-9 || Math.Abs(reach - 64) < 1e-9, $"a corner {reach} from the ring's middle");
            });
        }
    }

    [Fact]
    public void ADoorwayIsAWallWithARoundedOpeningThroughIt()
    {
        // A wall 256 across, 16 thick and 160 high with an opening 64 wide whose top stands 128 up,
        // its half circle springing at 96.
        var (low, high) = (new LevelPoint(-128, -8, 0), new LevelPoint(128, 8, 160));
        var doorway = Level.Doorway(low, high, 64, 128, "proto/wall", arch: "proto/stairs");

        // The two sides, then eight segments round the half circle and two more where the rays
        // through the wall's top corners cut them.
        Assert.Equal(2 + 10, doorway.Count);
        Assert.Equal(["proto/wall", "proto/wall"], doorway.Take(2).Select(brush => brush.Faces[0].Material));
        Assert.All(doorway.Skip(2), brush => Assert.Equal("proto/stairs", brush.Faces[0].Material));

        // The wall's volume less the opening's, a rectangle under a half circle the segments' inner
        // chords stand for, the chords cutting less than the circle and no less than eight even
        // chords do, which the corners' rays only cut finer.
        var wall = 256.0 * 16 * 160 - 64 * 96 * 16;
        Assert.InRange(doorway.Sum(Volume), wall - Math.PI * 32 * 32 / 2 * 16, wall - 0.5 * 32 * 32 * 8 * Math.Sin(Math.PI / 8) * 16);

        // Nothing stands in the opening's middle or under its top, and the wall stands beside it and
        // over the half circle.
        bool Solid(LevelPoint point) => doorway.Any(brush => brush.Faces.All(face => LevelPoint.Dot(face.Normal, point) - face.Distance <= 1e-6));
        Assert.False(Solid(new LevelPoint(0, 0, 8)));
        Assert.False(Solid(new LevelPoint(0, 0, 127)));
        Assert.False(Solid(new LevelPoint(30, 0, 97)));
        Assert.True(Solid(new LevelPoint(0, 0, 129)));
        Assert.True(Solid(new LevelPoint(-40, 0, 8)));
        Assert.True(Solid(new LevelPoint(31, 0, 125)));

        // An opening that leaves no wall above or beside it is refused.
        Assert.Throws<ArgumentException>(() => Level.Doorway(low, high, 64, 160, "proto/wall"));
        Assert.Throws<ArgumentException>(() => Level.Doorway(low, high, 256, 140, "proto/wall"));
    }

    [Fact]
    public void ABrushTurnedAboutTheVerticalTakesItsMappingAgainFromTheWorld()
    {
        var box = LevelBrush.Box(new LevelPoint(0, -8, 0), new LevelPoint(128, 8, 96), "proto/wall");
        var turned = box.Turned(90, new LevelPoint(0, 0, 0));
        var corners = turned.Polygons().OfType<LevelPoint[]>().SelectMany(polygon => polygon).ToList();

        // A quarter turn counter-clockwise from above takes X onto Y.
        Assert.Equal(-8, corners.Min(p => p.X), 9);
        Assert.Equal(8, corners.Max(p => p.X), 9);
        Assert.Equal(0, corners.Min(p => p.Y), 9);
        Assert.Equal(128, corners.Max(p => p.Y), 9);
        Assert.Equal(Volume(box), Volume(turned), 6);

        // The face that looked along X looks along Y, its material running along X and down as a
        // face looking along Y takes it.
        var north = turned.Faces.Single(face => face.Normal.Y > 0.99);
        Assert.Equal((new LevelPoint(1, 0, 0), new LevelPoint(0, 0, -1)), (north.UAxis, north.VAxis));
        Assert.Equal(turned.Faces.Select(face => face.Normal), Written(turned).Faces.Select(face => face.Normal));
    }

    /// <summary>A convex brush's volume, a pyramid from its first corner to each face.</summary>
    private static double Volume(LevelBrush brush)
    {
        var polygons = brush.Polygons().OfType<LevelPoint[]>().ToList();
        var apex = polygons[0][0];
        var volume = 0.0;
        foreach (var polygon in polygons)
        for (var i = 1; i + 1 < polygon.Length; i++)
        {
            var (a, b, c) = (polygon[0] - apex, polygon[i] - apex, polygon[i + 1] - apex);
            volume += LevelPoint.Dot(a, LevelPoint.Cross(b, c)) / 6;
        }

        return Math.Abs(volume);
    }

    /// <summary>A brush written in a level and read back.</summary>
    private static LevelBrush Written(LevelBrush brush)
    {
        var level = new Level();
        var world = new LevelEntity();
        world["classname"] = "worldspawn";
        world.Brushes.Add(brush);
        level.Entities.Add(world);
        return Level.Parse(level.Write()).World!.Brushes[0];
    }
}
