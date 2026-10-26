using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The feature test's hub as a level file, an octagonal plaza in the prototyping palette walled
/// round with a rounded doorway toward each zone, written by the generator here and held to it.
/// </summary>
/// <remarks>
/// The file is the hub the feature test loads, and an editor may change it. A change the generator
/// should make, as one the shapes' helpers bring, is written by running this class's tests with
/// <c>BCS_WRITE_LEVELS=1</c>, and the file is then held to the generator again.
/// </remarks>
public sealed class HubLevelTests
{
    // The plaza's walls stand nine meters from its middle, half a meter thick and five meters high
    // on a floor four units up, and each doorway is three meters wide and four high, its top a half
    // circle springing at two and a half meters.
    private const double Apothem = 288;
    private const double Thickness = 16;
    private const double FloorTop = 4;
    private const double WallHeight = 160;
    private const double OpeningWidth = 96;
    private const double OpeningHeight = 128;

    // The turns, counter-clockwise from above, of the walls with a doorway, from the wall facing the
    // file's plus Y, which is the feature test's north: the lights north, the render gallery west,
    // the vegetation south, the course east and the scenes north-east, as its Zones lie.
    private static readonly double[] Doorways = [0, 90, 180, 270, 315];

    private static string HubPath => Path.Combine(CheatsheetTests.RepositoryRoot(), "games", "FeatureTest", "assets", "levels", "hub.map");

    /// <summary>The hub as the generator builds it.</summary>
    internal static Level Hub()
    {
        var origin = new LevelPoint(0, 0, 0);
        var world = new LevelEntity();
        world["mapversion"] = "220";
        world["classname"] = "worldspawn";

        // The floor, an octagon as the walls' outsides are, the planes of a square and of the same
        // square turned an eighth, its foot under the ground it stands on.
        var square = LevelBrush.Box(new LevelPoint(-Apothem - Thickness, -Apothem - Thickness, -12), new LevelPoint(Apothem + Thickness, Apothem + Thickness, FloorTop), "proto/floor");
        var floor = new LevelBrush();
        floor.Faces.AddRange(square.Faces);
        floor.Faces.AddRange(square.Turned(45, origin).Faces.Where(face => Math.Abs(face.Normal.Z) < 0.5));
        world.Brushes.Add(floor);

        // Eight walls, each long enough that its outside meets its neighbors', a doorway in the
        // five toward the zones, its arch orange.
        var length = 2 * (Apothem + Thickness) * Math.Tan(Math.PI / 8);
        for (var side = 0; side < 8; side++)
        {
            var turn = side * 45.0;
            var (low, high) = (new LevelPoint(-length / 2, Apothem, FloorTop), new LevelPoint(length / 2, Apothem + Thickness, FloorTop + WallHeight));
            IEnumerable<LevelBrush> wall = Doorways.Contains(turn)
                ? Level.Doorway(low, high, OpeningWidth, OpeningHeight, "proto/wall", arch: "proto/stairs")
                : [LevelBrush.Box(low, high, "proto/wall")];
            world.Brushes.AddRange(turn == 0 ? wall : wall.Select(brush => brush.Turned(turn, origin)));
        }

        // Where the player starts, six meters south of the middle on the floor, facing north.
        var start = new LevelEntity();
        start["classname"] = "info_player_start";
        start["origin"] = $"0 -192 {LevelPoint.Number(FloorTop)}";
        start["angle"] = "90";

        var level = new Level();
        level.Entities.Add(world);
        level.Entities.Add(start);
        return level;
    }

    [Fact]
    public void TheHubsFileIsTheOneItsGeneratorWrites()
    {
        var made = Hub();
        if (Environment.GetEnvironmentVariable("BCS_WRITE_LEVELS") == "1") made.Save(HubPath);

        // Held by value, a millionth of a unit apart at most, since a sine on one system may differ
        // from another's in its last digit, which the text written there would carry.
        Assert.True(File.Exists(HubPath), $"There is no hub at {HubPath}; run these tests with BCS_WRITE_LEVELS=1 to write it.");
        var read = Level.Parse(File.ReadAllText(HubPath), HubPath);
        Assert.Equal(made.Entities.Select(entity => entity.Keys), read.Entities.Select(entity => entity.Keys));
        var (madeFaces, readFaces) = (made.Entities.SelectMany(entity => entity.Brushes).ToList(), read.Entities.SelectMany(entity => entity.Brushes).ToList());
        Assert.Equal(madeFaces.Count, readFaces.Count);
        for (var brush = 0; brush < madeFaces.Count; brush++)
        {
            Assert.Equal(madeFaces[brush].Faces.Count, readFaces[brush].Faces.Count);
            foreach (var (mine, theirs) in madeFaces[brush].Faces.Zip(readFaces[brush].Faces))
            {
                Assert.Equal(mine.Material, theirs.Material);
                foreach (var (a, b) in new[] { (mine.First, theirs.First), (mine.Second, theirs.Second), (mine.Third, theirs.Third), (mine.UAxis, theirs.UAxis), (mine.VAxis, theirs.VAxis) })
                    Assert.True((a - b).Length < 1e-6, $"brush {brush} reads {b} where the generator writes {a}; run these tests with BCS_WRITE_LEVELS=1 to write it again.");
            }
        }
    }

    [Fact]
    public void TheHubHasADoorwayTowardEachZoneAndItsStartWhereThePlayerStarts()
    {
        var level = Level.Parse(File.ReadAllText(HubPath), HubPath);
        var brushes = level.World!.Brushes;
        Assert.All(brushes, brush => Assert.NotEmpty(brush.Polygons().OfType<LevelPoint[]>()));

        // Two meters up from the middle out past the walls, nothing stands in the way toward each
        // zone, and a wall stands in it the three ways between.
        bool Solid(LevelPoint point) => brushes.Any(brush => brush.Faces.All(face => LevelPoint.Dot(face.Normal, point) - face.Distance <= 1e-6));
        bool Open(double turn)
        {
            var way = (Math.Cos((turn + 90) * Math.PI / 180), Math.Sin((turn + 90) * Math.PI / 180));
            return Enumerable.Range(1, 48).All(step => !Solid(new LevelPoint(way.Item1 * step * 8, way.Item2 * step * 8, FloorTop + 64)));
        }

        Assert.All(Doorways, turn => Assert.True(Open(turn), $"the way at {turn} degrees is walled"));
        Assert.All(new[] { 45.0, 135.0, 225.0 }, turn => Assert.False(Open(turn), $"the way at {turn} degrees is open"));

        // The floor's top a hair over the ground, and the start on it six meters south of the middle,
        // facing north, which is the feature test's hub start and minus Z.
        Assert.True(Solid(new LevelPoint(0, 0, FloorTop - 1)) && !Solid(new LevelPoint(0, 0, FloorTop + 1)));
        var start = level.Entities.Single(entity => entity.ClassName == "info_player_start");
        Assert.Equal(new Vec3(0f, 0.125f, 6f), new LevelPoint(0, -192, FloorTop).Meters(level.UnitsPerMeter));
        Assert.Equal(("0 -192 4", "90"), (start["origin"], start["angle"]));
    }
}
