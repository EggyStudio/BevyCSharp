using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The feature test's level, its plaza, a walled court round each zone where the zone stands, the
/// corridors that join them, the light hall's shell and the meadow's raised walkway, written by the
/// generator here and held to it.
/// </summary>
/// <remarks>
/// <para>
/// The file is the level the feature test loads, and an editor may change it. A change the
/// generator should make, as one the shapes' helpers bring, is written by running this class's tests
/// with <c>BCS_WRITE_LEVELS=1</c>, and the file is then held to the generator again.
/// </para>
/// <para>
/// The generator works in the feature test's meters, X east, Y up and Z south, and writes the file's
/// units, 32 a meter with Z up, the file's Y the feature test's minus Z. The floors' tops are the
/// ground's height, where the level's root stands, and the plaza's four units over it.
/// </para>
/// </remarks>
public sealed class FeatureTestLevelTests
{
    private const double Units = 32;

    // The plaza's walls stand nine meters from its middle, half a meter thick and five meters high
    // on a floor four units up, and each of its doorways is three meters wide and four high.
    private const double Apothem = 9;
    private const double Thickness = 0.5;
    private const double FloorTop = 4;
    private const double PlazaWall = 5;

    // The turns, counter-clockwise from above, of the plaza's walls with a doorway, from the wall
    // facing the file's plus Y, the feature test's north: the light hall north, the render gallery
    // west, the meadow south, the course east and the scenes north-east, as its zones lie.
    private static readonly double[] Doorways = [0, 90, 180, 270, 315];

    // How high the corridors', the courts' and the scenes yard's walls stand, so no horizon shows
    // over them from a floor, a walkway or a ramp's top.
    private const double CorridorWall = 7;
    private const double CourtWall = 10;
    private const double YardWall = 16;

    // Where each zone's player starts, as the feature test's Zones put it, eyes 1.7 m over the feet.
    private static readonly (string Zone, double X, double Y, double Z)[] Starts =
    [
        ("hub", 0, FloorTop / Units, 6), ("course", 46, 0, 0), ("gallery", -50, 0, 0),
        ("lights", 0, 0, -18), ("vegetation", 0, 0, 50), ("scenes", 45, 0, -45),
    ];

    private static string LevelPath => Path.Combine(CheatsheetTests.RepositoryRoot(), "games", "FeatureTest", "assets", "levels", "feature-test.map");

    /// <summary>The level as the generator builds it.</summary>
    internal static Level Generate()
    {
        var world = new LevelEntity();
        world["mapversion"] = "220";
        world["classname"] = "worldspawn";
        var brushes = world.Brushes;

        Plaza(brushes);

        // The courts, each round its zone where the zone's pieces stand.
        Court(brushes, 44, 116, -24, 40, CourtWall, west: [(24, 4, 5)]);
        Court(brushes, -80, -46, -20, 21, CourtWall, east: [(20, 4, 5)]);
        Court(brushes, -30, 30, 51, 122, CourtWall, north: [(30, 4, 5)]);
        Walkway(brushes);
        Yard(brushes);
        Hall(brushes);

        // The corridors from the plaza's doorways, each six meters wide, the west one an aisle and an
        // arcade beside it, the south one under arches, the north-east one along the diagonal.
        Floor(brushes, 9, 44, -3, 3);
        brushes.AddRange(Wall((9.25, -3), (44, -3), CorridorWall));
        brushes.AddRange(Wall((9.25, 3), (44, 3), CorridorWall));

        Floor(brushes, -46, -9, -3, 8);
        brushes.AddRange(Wall((-9.25, -3), (-46, -3), CorridorWall));
        brushes.AddRange(Wall((-9.25, 3), (-46, 3), CorridorWall, (6, 3, 4), (14, 3, 4), (22, 3, 4), (30, 3, 4)));
        brushes.AddRange(Wall((-11.5, 8), (-46, 8), CorridorWall));
        brushes.AddRange(Wall((-11.5, 3), (-11.5, 8), CorridorWall));

        Floor(brushes, -3, 3, 9, 51);
        brushes.AddRange(Wall((-3, 9.25), (-3, 51), CorridorWall));
        brushes.AddRange(Wall((3, 9.25), (3, 51), CorridorWall));
        foreach (var z in new[] { 16.0, 24, 32, 40, 48 })
            brushes.AddRange(LevelBrush.Arch(At(0, 4.2, z), 3 * Units, 0.6 * Units, 0.6 * Units, "proto/stairs"));

        Floor(brushes, -3, 3, -24, -9);
        brushes.AddRange(Wall((-3, -9.25), (-3, -23.85), CorridorWall));
        brushes.AddRange(Wall((3, -9.25), (3, -23.85), CorridorWall));

        // The north-east corridor runs from the plaza to the scenes yard's cut corner, where the
        // corner's line, x minus z at 86, meets the diagonal 86 over the root of two from the middle.
        var reach = 86 / Math.Sqrt(2);
        var diagonal = LevelBrush.Box(new LevelPoint(9 * Units, -3 * Units, -0.5 * Units), new LevelPoint(reach * Units, 3 * Units, 0), "proto/floor");
        brushes.Add(diagonal.Turned(45, new LevelPoint(0, 0, 0)));
        var (way, across) = ((1 / Math.Sqrt(2), -1 / Math.Sqrt(2)), (1 / Math.Sqrt(2), 1 / Math.Sqrt(2)));
        foreach (var side in new[] { -3.0, 3 })
            brushes.AddRange(Wall((9.25 * way.Item1 + side * across.Item1, 9.25 * way.Item2 + side * across.Item2), (reach * way.Item1 + side * across.Item1, reach * way.Item2 + side * across.Item2), CorridorWall));

        // Where the player starts, six meters south of the middle on the plaza's floor, facing north.
        var start = new LevelEntity();
        start["classname"] = "info_player_start";
        start["origin"] = $"0 -192 {LevelPoint.Number(FloorTop)}";
        start["angle"] = "90";

        var level = new Level();
        level.Entities.Add(world);
        level.Entities.Add(start);
        return level;
    }

    /// <summary>The plaza, an octagon of walls round a floor of a square's planes and the same square's turned an eighth, a doorway in five of its walls.</summary>
    private static void Plaza(List<LevelBrush> brushes)
    {
        var origin = new LevelPoint(0, 0, 0);
        var outer = (Apothem + Thickness) * Units;
        var square = LevelBrush.Box(new LevelPoint(-outer, -outer, -12), new LevelPoint(outer, outer, FloorTop), "proto/floor");
        var floor = new LevelBrush();
        floor.Faces.AddRange(square.Faces);
        floor.Faces.AddRange(square.Turned(45, origin).Faces.Where(face => Math.Abs(face.Normal.Z) < 0.5));
        brushes.Add(floor);

        var length = 2 * outer * Math.Tan(Math.PI / 8);
        for (var side = 0; side < 8; side++)
        {
            var turn = side * 45.0;
            var (low, high) = (new LevelPoint(-length / 2, Apothem * Units, FloorTop), new LevelPoint(length / 2, outer, FloorTop + PlazaWall * Units));
            IEnumerable<LevelBrush> wall = Doorways.Contains(turn)
                ? Level.Doorway(low, high, 3 * Units, 4 * Units, "proto/wall", arch: "proto/stairs")
                : [LevelBrush.Box(low, high, "proto/wall")];
            brushes.AddRange(turn == 0 ? wall : wall.Select(brush => brush.Turned(turn, origin)));
        }
    }

    /// <summary>
    /// A court, a floor and four walls round it from one corner to the other, a doorway in a wall
    /// where given, its distance along the wall from its west or north end, its width and its height.
    /// </summary>
    private static void Court(
        List<LevelBrush> brushes, double x0, double x1, double z0, double z1, double height,
        (double Along, double Width, double Height)[]? north = null, (double Along, double Width, double Height)[]? south = null,
        (double Along, double Width, double Height)[]? west = null, (double Along, double Width, double Height)[]? east = null)
    {
        Floor(brushes, x0, x1, z0, z1);
        var half = Thickness / 2;
        brushes.AddRange(Wall((x0 - half, z0), (x1 + half, z0), height, Shifted(north, half)));
        brushes.AddRange(Wall((x0 - half, z1), (x1 + half, z1), height, Shifted(south, half)));
        brushes.AddRange(Wall((x0, z0), (x0, z1), height, west ?? []));
        brushes.AddRange(Wall((x1, z0), (x1, z1), height, east ?? []));
    }

    private static (double, double, double)[] Shifted((double Along, double Width, double Height)[]? openings, double by) =>
        [.. (openings ?? []).Select(opening => (opening.Along + by, opening.Width, opening.Height))];

    /// <summary>
    /// The meadow court's raised walkway along its west wall, four meters up on pillars with a rail
    /// on its open side, and a ramp down from each end, so the meadow is seen from above inside walls
    /// higher than an eye on it.
    /// </summary>
    private static void Walkway(List<LevelBrush> brushes)
    {
        const double West = -29.75, East = -26.75, North = 62, South = 110, Top = 4;
        brushes.Add(Block(West, East, Top - 0.4, Top, North, South, "proto/wall"));
        brushes.Add(Block(East - 0.2, East, Top, Top + 1, North, South, "proto/wall"));
        for (var z = North; z <= South; z += 8) brushes.Add(Block(East - 0.5, East, 0, Top - 0.4, z - 0.25, z + 0.25, "proto/wall"));
        brushes.Add(Ramp((-28.25, 54), (-28.25, North), 3, Top));
        brushes.Add(Ramp((-28.25, 118), (-28.25, South), 3, Top));
    }

    /// <summary>The scenes yard round Sponza's site, its corner toward the plaza cut away by a wall with the north-east corridor's doorway.</summary>
    private static void Yard(List<LevelBrush> brushes)
    {
        const double X0 = 38, X1 = 104, Z0 = -104, Z1 = -36;
        Floor(brushes, X0, X1, Z0, Z1);
        var cut = 12 * Math.Sqrt(2);
        brushes.AddRange(Wall((X0, Z0 - 0.25), (X0, -48), YardWall));
        brushes.AddRange(Wall((X0, -48), (50, Z1), YardWall, (cut / 2, 4, 5)));
        brushes.AddRange(Wall((50, Z1), (X1 + 0.25, Z1), YardWall));
        brushes.AddRange(Wall((X1, Z1), (X1, Z0), YardWall));
        brushes.AddRange(Wall((X1 + 0.25, Z0), (X0 - 0.25, Z0), YardWall));
    }

    /// <summary>
    /// The light hall at the end of the corridor north of the plaza, its floor, its walls five and a
    /// half meters high, a rounded doorway toward the plaza, the partitions between its bays, the
    /// walls along its aisle with an arch into each bay, their piers meeting the partitions, and its
    /// roof, so its bays' lights light a shut room. The feature test's Lights stands its bays' lights
    /// at the same front and back.
    /// </summary>
    private static void Hall(List<LevelBrush> brushes)
    {
        const double Half = 12, Front = -24, Back = -56, Height = 5.5, Wall = 0.3;
        Floor(brushes, -Half, Half, Back, Front);
        brushes.AddRange(Walls((-Half - Wall / 2, Front), (Half + Wall / 2, Front), Height, Wall, (Half + Wall / 2, 4, 4)));
        brushes.AddRange(Walls((-Half - Wall / 2, Back), (Half + Wall / 2, Back), Height, Wall));
        brushes.AddRange(Walls((-Half, Front), (-Half, Back), Height, Wall));
        brushes.AddRange(Walls((Half, Front), (Half, Back), Height, Wall));
        foreach (var z in new[] { Front - 8, Front - 16, Front - 24 })
        {
            brushes.AddRange(Walls((-Half, z), (-4, z), Height, Wall));
            brushes.AddRange(Walls((4, z), (Half, z), Height, Wall));
        }

        // An arch six meters wide into each bay, its top four and a half meters up, so a bay's name
        // over it stands on the wall above, and a pier of two meters between two arches.
        (double, double, double)[] arches = [(4, 6, 4.5), (12, 6, 4.5), (20, 6, 4.5), (28, 6, 4.5)];
        brushes.AddRange(Walls((-4, Front), (-4, Back), Height, Wall, arches));
        brushes.AddRange(Walls((4, Front), (4, Back), Height, Wall, arches));

        brushes.Add(Block(-Half - Wall / 2, Half + Wall / 2, Height, Height + 0.3, Back - Wall / 2, Front + Wall / 2, "proto/ceiling"));
    }

    /// <summary>A floor half a meter deep, its top the ground's.</summary>
    private static void Floor(List<LevelBrush> brushes, double x0, double x1, double z0, double z1) =>
        brushes.Add(Block(x0, x1, -0.5, 0, z0, z1, "proto/floor"));

    /// <summary>A block between two corners in meters.</summary>
    private static LevelBrush Block(double x0, double x1, double y0, double y1, double z0, double z1, string material) =>
        LevelBrush.Box(At(x0, y0, z1), At(x1, y1, z0), material);

    /// <summary>A point in meters, X east, Y up over the ground and Z south, in the file's units.</summary>
    private static LevelPoint At(double x, double y, double z) => new(x * Units, -z * Units, y * Units);

    private static IEnumerable<LevelBrush> Wall((double X, double Z) from, (double X, double Z) to, double height, params (double Along, double Width, double Height)[] openings) =>
        Walls(from, to, height, Thickness, openings);

    /// <summary>
    /// A wall from one point of the ground to another, centered on the line between them, its
    /// rounded openings at their distances along it from its start, each in a stretch of wall a
    /// meter wider each side, made along the file's X at the origin and turned and moved into place.
    /// </summary>
    private static IEnumerable<LevelBrush> Walls((double X, double Z) from, (double X, double Z) to, double height, double thickness, params (double Along, double Width, double Height)[] openings)
    {
        var (dx, dz) = (to.X - from.X, to.Z - from.Z);
        var length = Math.Sqrt(dx * dx + dz * dz);
        var (low, high) = (-thickness / 2 * Units, thickness / 2 * Units);
        var pieces = new List<LevelBrush>();
        var at = 0.0;
        foreach (var (along, width, tall) in openings.OrderBy(opening => opening.Along))
        {
            var half = width / 2 + 1;
            if (along - half > at) pieces.Add(LevelBrush.Box(new LevelPoint(at * Units, low, 0), new LevelPoint((along - half) * Units, high, height * Units), "proto/wall"));
            pieces.AddRange(Level.Doorway(new LevelPoint((along - half) * Units, low, 0), new LevelPoint((along + half) * Units, high, height * Units), width * Units, tall * Units, "proto/wall", arch: "proto/stairs"));
            at = along + half;
        }

        if (length > at) pieces.Add(LevelBrush.Box(new LevelPoint(at * Units, low, 0), new LevelPoint(length * Units, high, height * Units), "proto/wall"));

        // The file's Y is the feature test's minus Z, so a wall running south turns clockwise.
        var turn = Math.Round(Math.Atan2(-dz, dx) * 180 / Math.PI, 9);
        var origin = new LevelPoint(0, 0, 0);
        return pieces.Select(piece => (turn == 0 ? piece : piece.Turned(turn, origin)).Moved(At(from.X, 0, from.Z)));
    }

    /// <summary>A ramp from one point of the ground to another, as wide as given, rising from nothing at its start to its height at its end.</summary>
    private static LevelBrush Ramp((double X, double Z) from, (double X, double Z) to, double width, double rise)
    {
        var (dx, dz) = (to.X - from.X, to.Z - from.Z);
        var length = Math.Sqrt(dx * dx + dz * dz);
        var ramp = LevelBrush.Wedge(new LevelPoint(0, -width / 2 * Units, 0), new LevelPoint(length * Units, width / 2 * Units, rise * Units), "proto/stairs");
        var turn = Math.Round(Math.Atan2(-dz, dx) * 180 / Math.PI, 9);
        return (turn == 0 ? ramp : ramp.Turned(turn, new LevelPoint(0, 0, 0))).Moved(At(from.X, 0, from.Z));
    }

    [Fact]
    public void TheLevelsFileIsTheOneItsGeneratorWrites()
    {
        var made = Generate();
        if (Environment.GetEnvironmentVariable("BCS_WRITE_LEVELS") == "1") made.Save(LevelPath);

        // Held by value, a millionth of a unit apart at most, since a sine on one system may differ
        // from another's in its last digit, which the text written there would carry.
        Assert.True(File.Exists(LevelPath), $"There is no level at {LevelPath}; run these tests with BCS_WRITE_LEVELS=1 to write it.");
        var read = Level.Parse(File.ReadAllText(LevelPath), LevelPath);
        Assert.Equal(made.Entities.Select(entity => entity.Keys), read.Entities.Select(entity => entity.Keys));
        var (madeBrushes, readBrushes) = (made.Entities.SelectMany(entity => entity.Brushes).ToList(), read.Entities.SelectMany(entity => entity.Brushes).ToList());
        Assert.Equal(madeBrushes.Count, readBrushes.Count);
        for (var brush = 0; brush < madeBrushes.Count; brush++)
        {
            Assert.Equal(madeBrushes[brush].Faces.Count, readBrushes[brush].Faces.Count);
            foreach (var (mine, theirs) in madeBrushes[brush].Faces.Zip(readBrushes[brush].Faces))
            {
                Assert.Equal(mine.Material, theirs.Material);
                foreach (var (a, b) in new[] { (mine.First, theirs.First), (mine.Second, theirs.Second), (mine.Third, theirs.Third), (mine.UAxis, theirs.UAxis), (mine.VAxis, theirs.VAxis) })
                    Assert.True((a - b).Length < 1e-6, $"brush {brush} reads {b} where the generator writes {a}; run these tests with BCS_WRITE_LEVELS=1 to write it again.");
            }
        }
    }

    [Fact]
    public void ThePlazaHasADoorwayTowardEachZoneAndItsStartWhereThePlayerStarts()
    {
        var level = Level.Parse(File.ReadAllText(LevelPath), LevelPath);
        var brushes = level.World!.Brushes;
        Assert.All(brushes, brush => Assert.NotEmpty(brush.Polygons().OfType<LevelPoint[]>()));

        // Two meters up from the middle out past the plaza's walls, nothing stands in the way toward
        // each zone, and a wall stands in it the three ways between.
        bool Open(double turn)
        {
            var angle = (turn + 90) * Math.PI / 180;
            return Hit(brushes, new LevelPoint(0, 0, FloorTop + 2 * Units), new LevelPoint(Math.Cos(angle), Math.Sin(angle), 0), 12 * Units) is null;
        }

        Assert.All(Doorways, turn => Assert.True(Open(turn), $"the way at {turn} degrees is walled"));
        Assert.All(new[] { 45.0, 135.0, 225.0 }, turn => Assert.False(Open(turn), $"the way at {turn} degrees is open"));

        var start = level.Entities.Single(entity => entity.ClassName == "info_player_start");
        Assert.Equal(new Vec3(0f, 0.125f, 6f), new LevelPoint(0, -192, FloorTop).Meters(level.UnitsPerMeter));
        Assert.Equal(("0 -192 4", "90"), (start["origin"], start["angle"]));
    }

    [Fact]
    public void NoHorizonShowsFromAnyStartTheWalkwayOrARampsTop()
    {
        var brushes = Level.Parse(File.ReadAllText(LevelPath), LevelPath).World!.Brushes;

        // Each zone's start stands on a floor with nothing where its body is.
        foreach (var (zone, x, y, z) in Starts)
        {
            var feet = At(x, y, z);
            Assert.True(Hit(brushes, feet + new LevelPoint(0, 0, Units), new LevelPoint(0, 0, -1), 1.2 * Units) is { } below && below >= Units - 1, $"the {zone} start stands on no floor");
            Assert.Null(Hit(brushes, feet + new LevelPoint(0, 0, 0.2 * Units), new LevelPoint(0, 0, 1), 1.6 * Units));
        }

        // From every start, the walkway's middle and each ramp's top, at an eye's height, a wall
        // stands across every level way within the level, so the sky shows over walls and never the
        // horizon.
        (string, double, double, double)[] eyes =
        [
            .. Starts.Select(start => (start.Zone, start.X, start.Y + 1.7, start.Z)),
            ("the walkway", -28.25, 5.7, 86), ("the north ramp's top", -28.25, 5.7, 62), ("the south ramp's top", -28.25, 5.7, 110),
        ];
        foreach (var (where, x, y, z) in eyes)
        for (var step = 0; step < 64; step++)
        {
            var angle = step * Math.Tau / 64;
            var way = new LevelPoint(Math.Cos(angle), Math.Sin(angle), 0);
            Assert.True(Hit(brushes, At(x, y, z), way, 400 * Units) is not null, $"the horizon shows from {where} at {step * 360 / 64.0} degrees");
        }
    }

    /// <summary>How far along a ray it first meets a brush, or nothing within the reach.</summary>
    private static double? Hit(IReadOnlyList<LevelBrush> brushes, LevelPoint from, LevelPoint way, double reach)
    {
        double? nearest = null;
        foreach (var brush in brushes)
        {
            var (enter, leave, misses) = (0.0, reach, false);
            foreach (var face in brush.Faces)
            {
                var (normal, distance) = (face.Normal, face.Distance);
                var (along, outside) = (LevelPoint.Dot(normal, way), LevelPoint.Dot(normal, from) - distance);
                if (Math.Abs(along) < 1e-12)
                {
                    if (outside > 1e-6) misses = true;
                    continue;
                }

                var t = -outside / along;
                if (along < 0) enter = Math.Max(enter, t);
                else leave = Math.Min(leave, t);
            }

            if (!misses && enter <= leave && (nearest is null || enter < nearest)) nearest = enter;
        }

        return nearest;
    }
}
