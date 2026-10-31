using System.Globalization;
using Bevy;
using Bevy.Physics;
using Levels;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A level file of brushes and entities in the Valve 220 map format TrenchBroom writes, which the
/// Levels module reads and tests, built into a scene by the engine's binding, its brushes meshed
/// with their collision and its entities spawned by class.
/// </summary>
[Collection("engine")]
public sealed class LevelTests
{
    // A room's floor and a wall, a ramp whose slope meets its floor along one edge, a player's
    // start, a door with two outputs of one name, and a tree, at the default 32 units a meter.
    private static readonly string Room = string.Join("\n",
        "// Game: Generic",
        "// Format: Valve",
        "// entity 0",
        "{",
        "\"mapversion\" \"220\"",
        "\"classname\" \"worldspawn\"",
        "// brush 0",
        "{",
        Box(-128, -128, -16, 128, 128, 0, "floor"),
        "}",
        "// brush 1",
        "{",
        Box(-128, 112, 0, 128, 128, 96, "wall"),
        "}",
        "// brush 2",
        "{",
        "( 0 -64 0 ) ( 0 -63 0 ) ( 0 -64 1 ) floor [ 0 1 0 0 ] [ 0 0 -1 0 ] 0 1 1",
        "( 0 -64 0 ) ( 0 -64 1 ) ( 1 -64 0 ) floor [ 1 0 0 0 ] [ 0 0 -1 0 ] 0 1 1",
        "( 0 -64 0 ) ( 1 -64 0 ) ( 0 -63 0 ) floor [ 1 0 0 0 ] [ 0 -1 0 0 ] 0 1 1",
        "( 128 -64 64 ) ( 0 -64 0 ) ( 0 64 0 ) floor [ 1 0 0 0 ] [ 0 -1 0 0 ] 0 1 1",
        "( 128 64 64 ) ( 129 64 64 ) ( 128 64 65 ) floor [ 1 0 0 0 ] [ 0 0 -1 0 ] 0 1 1",
        "( 128 64 64 ) ( 128 64 65 ) ( 128 65 64 ) floor [ 0 1 0 0 ] [ 0 0 -1 0 ] 0 1 1",
        "}",
        "}",
        "// entity 1",
        "{",
        "\"classname\" \"info_player_start\"",
        "\"origin\" \"-64 0 24\"",
        "\"angle\" \"90\"",
        "}",
        "// entity 2",
        "{",
        "\"classname\" \"func_door\"",
        "\"targetname\" \"door\"",
        "\"OnOpen\" \"lamp,TurnOn,,0,-1\"",
        "\"OnOpen\" \"chime,Play,,0.5,1\"",
        "// brush 0",
        "{",
        Box(-32, 104, 0, 32, 112, 96, "door"),
        "}",
        "}",
        "// entity 3",
        "{",
        "\"classname\" \"tree\"",
        "\"origin\" \"96 -96 0\"",
        "\"species\" \"black-tupelo\"",
        "\"seed\" \"3\"",
        "}",
        "");

    [Fact]
    public void ALevelIsBuiltIntoTheSceneWithItsCollisionAndItsEntitiesByClass()
    {
        using var harness = new EngineHarness(frames: 30, fps: 240, fixedHz: 120, frameSeconds: 1.0 / 240);
        harness.App.AddPlugin(new PhysicsPlugin());

        var root = Entity.None;
        var frame = 0;
        float? floorHit = null, rampHit = null, doorHit = null;
        (string Name, string Class, string Target)[] children = [];
        Transform start = default, door = default;
        IReadOnlyList<KeyValuePair<string, string>> doorKeys = [];
        Tree tree = default;
        var worldParts = 0;

        harness.OnContext(Stage.Startup, ctx => root = LevelSpawner.Spawn(ctx.Ecs, Level.Parse(Room), "room"));
        harness.OnContext(Stage.Update, ctx =>
        {
            // A spawn that failed leaves no root, and the failure says why.
            if (++frame != 20 || root == Entity.None) return;
            var ecs = ctx.Ecs;
            var entities = ecs.ChildrenOf(root);
            children = [.. entities.Select(entity => (ecs.NameOf(entity) ?? "", ecs.GetOrDefault<LevelKeys>(entity).ClassName.Value, ecs.GetOrDefault<LevelKeys>(entity).TargetName.Value))];
            worldParts = ecs.ChildrenOf(entities[0]).Length;
            start = ecs.GetOrDefault<Transform>(entities[1]);
            door = ecs.GetOrDefault<Transform>(entities[2]);
            doorKeys = LevelSpawner.KeysOf(ecs.GetOrDefault<LevelKeys>(entities[2]));
            tree = ecs.GetOrDefault<Tree>(entities[3]);

            var physics = ctx.Res<PhysicsWorld>();
            floorHit = physics.Raycast(new Vec3(-3f, 10f, 0f), -Vec3.UnitY, 50f)?.Point.Y;
            rampHit = physics.Raycast(new Vec3(1f, 10f, 0f), -Vec3.UnitY, 50f)?.Point.Y;
            doorHit = physics.Raycast(new Vec3(0f, 1f, 0f), -Vec3.UnitZ, 50f)?.Point.Z;
        });

        harness.Run();

        Assert.Equal([("worldspawn", "worldspawn", ""), ("info_player_start", "info_player_start", ""), ("door", "func_door", "door"), ("tree", "tree", "")], children);
        Assert.Equal(App.HasRenderer ? 2 : 0, worldParts);

        // The start two meters along minus X and three quarters of a meter up, its forward, minus Z,
        // where the file's yaw of 90 faces, the file's plus Y, which is minus Z with Y up.
        Assert.Equal(new Vec3(-2f, 0.75f, 0f), start.Translation);
        var forward = start.Rotation * -Vec3.UnitZ;
        Assert.True((forward - -Vec3.UnitZ).Length < 1e-5f, $"the start faces {forward}");

        // The door at its brushes' middle, its keys in their order, the tree of its species and seed.
        Assert.Equal(new Vec3(0f, 1.5f, -3.375f), door.Translation);
        Assert.Equal(["classname", "targetname", "OnOpen", "OnOpen"], doorKeys.Select(pair => pair.Key));
        Assert.Equal(("black-tupelo", 3u), (tree.Species.Value, tree.Seed));

        // The floor's top at zero, the ramp's slope a meter along it a quarter of the way up its two
        // meters, and the door's face toward the room at its brush's near side.
        Assert.Equal(0f, floorHit!.Value, 3);
        Assert.Equal(0.5f, rampHit!.Value, 3);
        Assert.Equal(-3.25f, doorHit!.Value, 3);
    }

    [Fact]
    public void ALevelFileIsBuiltFromTheConsoleAndAFileThatIsNotOneIsRefused()
    {
        using var folder = new TestFolder("bcs-levels-");
        var room = folder.File("room.map");
        File.WriteAllText(room, Room);
        var broken = folder.File("broken.map");
        File.WriteAllText(broken, "{ \"classname\" \"worldspawn\"\n");

        using var harness = new EngineHarness(frames: 3);
        string? built = null, refused = null, missing = null;
        var under = 0;
        harness.On(Stage.Update, world =>
        {
            if (built is not null) return;
            using (ConsoleHost.Lend(world))
            {
                built = ConsoleCommands.Run($"level.load {room}");
                refused = ConsoleCommands.Run($"level.load {broken}");
                missing = ConsoleCommands.Run($"level.load {folder.File("none.map")}");
            }

            var ecs = world.Resource<EcsWorld>();
            under = ecs.All().Count(entity => ecs.NameOf(entity) == "room");
        });

        harness.Run();

        Assert.StartsWith($"built {room} under ", built);
        Assert.EndsWith(", 4 entities", built);
        Assert.Equal(1, under);
        Assert.StartsWith($"{broken}, line 2: ", refused);
        Assert.Equal($"there is no level at {folder.File("none.map")}", missing);
    }

    [Fact]
    public void TheShippedPrototypePicturesAreTheOnesTheEngineDraws()
    {
        // Each shipped picture read back and held pixel for pixel to the one the engine draws, so a
        // change to the grid or the palette that leaves the pictures TrenchBroom shows behind fails
        // here, until `./bcs command level.textures BevyCSharp/Levels/TrenchBroom` writes them again.
        // Pixels rather than the file's bytes, since an encoder's compression may differ between
        // systems and runtimes while the picture is the same. A picture that differs is written
        // under the results' prototype-pictures folder, which the workflow keeps beside its page.
        var root = CheatsheetTests.RepositoryRoot();
        var shipped = Path.Combine(root, "BevyCSharp", "Levels", "TrenchBroom", "textures", "proto");
        Assert.Equal(Modeling.PrototypeColors.Keys.Order(StringComparer.Ordinal), Directory.GetFiles(shipped, "*.png").Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal));

        var differences = new List<string>();
        foreach (var (name, color) in Modeling.PrototypeColors)
        {
            var drawn = Modeling.PrototypePicture(color, LevelSpawner.PrototypePixels);
            var read = AnimationReader.Png(File.ReadAllBytes(Path.Combine(shipped, name + ".png"))).Frames[0];
            Assert.Equal((LevelSpawner.PrototypePixels, LevelSpawner.PrototypePixels), (read.Width, read.Height));
            var at = Enumerable.Range(0, drawn.Length).FirstOrDefault(i => drawn[i] != read.Pixels[i], -1);
            if (at < 0) continue;

            var (pixel, x, y) = (at / 4 * 4, at / 4 % LevelSpawner.PrototypePixels, at / 4 / LevelSpawner.PrototypePixels);
            var count = Enumerable.Range(0, drawn.Length / 4).Count(i => !drawn.AsSpan(i * 4, 4).SequenceEqual(read.Pixels.AsSpan(i * 4, 4)));
            differences.Add($"{name}.png first differs at ({x}, {y}), drawn {string.Join(" ", drawn[pixel..(pixel + 4)])} and shipped {string.Join(" ", read.Pixels[pixel..(pixel + 4)])}, {count} pixels in all");
            var kept = Path.Combine(root, "BevyCSharp.Tests", "TestResults", "prototype-pictures");
            Directory.CreateDirectory(kept);
            File.WriteAllBytes(Path.Combine(kept, name + ".png"), PngWriter.Still(LevelSpawner.PrototypePixels, LevelSpawner.PrototypePixels, drawn));
        }

        Assert.True(differences.Count == 0, string.Join("; ", differences));

        // Every value the drawing rounds to a byte stands a thousandth of a step or more from where
        // the rounding turns, so a system whose power function differs in its last digit draws the
        // same pixels. The grid's three tones, tinted by each prototype's color as the drawing tints
        // them, are the values it rounds.
        var grid = Images.GridPixels(LevelSpawner.PrototypePixels, Modeling.PrototypeFace, Modeling.PrototypeLine, Modeling.PrototypeFine);
        var tones = Enumerable.Range(0, grid.Length / 4).Select(i => grid[i * 4]).Distinct().ToArray();
        foreach (var (name, color) in Modeling.PrototypeColors)
        foreach (var tint in new[] { color.R, color.G, color.B })
        foreach (var tone in tones)
        {
            var lit = Color.FromSrgb8(tone, tone, tone).R * tint;
            var step = new Color(lit, lit, lit, 1f).ToSrgb().X * 255f;
            Assert.True(Math.Abs(step - Math.Floor(step) - 0.5) > 1e-3, $"{name}'s tint takes the tone {tone} to {step}, too near where its rounding turns");
        }
    }

    /// <summary>An axis-aligned box brush's six faces, as TrenchBroom writes one.</summary>
    private static string Box(int x1, int y1, int z1, int x2, int y2, int z2, string material)
    {
        static string P(int x, int y, int z) => $"( {x.ToString(CultureInfo.InvariantCulture)} {y.ToString(CultureInfo.InvariantCulture)} {z.ToString(CultureInfo.InvariantCulture)} )";
        return string.Join("\n",
            $"{P(x1, y1, z1)} {P(x1, y1 + 1, z1)} {P(x1, y1, z1 + 1)} {material} [ 0 1 0 0 ] [ 0 0 -1 0 ] 0 1 1",
            $"{P(x1, y1, z1)} {P(x1, y1, z1 + 1)} {P(x1 + 1, y1, z1)} {material} [ 1 0 0 0 ] [ 0 0 -1 0 ] 0 1 1",
            $"{P(x1, y1, z1)} {P(x1 + 1, y1, z1)} {P(x1, y1 + 1, z1)} {material} [ 1 0 0 0 ] [ 0 -1 0 0 ] 0 1 1",
            $"{P(x2, y2, z2)} {P(x2, y2 + 1, z2)} {P(x2 + 1, y2, z2)} {material} [ 1 0 0 0 ] [ 0 -1 0 0 ] 0 1 1",
            $"{P(x2, y2, z2)} {P(x2 + 1, y2, z2)} {P(x2, y2, z2 + 1)} {material} [ 1 0 0 0 ] [ 0 0 -1 0 ] 0 1 1",
            $"{P(x2, y2, z2)} {P(x2, y2, z2 + 1)} {P(x2, y2 + 1, z2)} {material} [ 0 1 0 0 ] [ 0 0 -1 0 ] 0 1 1");
    }
}
