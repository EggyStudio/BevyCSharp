using Bevy;
using Bevy.Physics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers what a command reads from its words, a parameter with a default left off, an enum's
/// member by its name alone, a list's items split by semicolons, and files dropped by a command.
/// </summary>
/// <remarks>
/// The ones that change the world run a command inside a system, with the world lent to the
/// console as the command line lends it, and read what changed there or at the next frame.
/// </remarks>
[Collection("engine")]
public sealed class CommandArgumentTests
{
    /// <summary>A parameter with a default can be left off, the last first, and the schema says which can.</summary>
    [Fact]
    public void AParameterWithADefaultIsLeftOff()
    {
        Assert.Equal("cube 2 1.5 none", ConsoleCommands.Run("tests.defaults cube"));
        Assert.Equal("cube 3 1.5 none", ConsoleCommands.Run("tests.defaults cube 3"));
        Assert.Equal("cube 3 0.5 lid", ConsoleCommands.Run("tests.defaults cube 3 0.5 lid"));
        Assert.Equal("needs 1 argument", ConsoleCommands.Run("tests.defaults"));
        Assert.Equal("not a whole: many", ConsoleCommands.Run("tests.defaults cube many"));
        Assert.Equal("all of it", ConsoleCommands.Run("tests.lined"));
        Assert.Equal("a b", ConsoleCommands.Run("tests.lined a b"));

        var command = ConsoleCommands.Find("tests.defaults")!;
        Assert.Equal([false, true, true, true], command.Parameters.Select(parameter => parameter.Optional));
        Assert.Equal("<which> [count] [scale] [label]", command.Usage);
    }

    /// <summary>An enum's member is read by its name in any case, an alias too, and a number or a list of names is not.</summary>
    [Fact]
    public void AnEnumsMemberIsReadByItsNameAlone()
    {
        Assert.True(ConsoleWorldCommands.TryName<GamepadButton>("south", out var button));
        Assert.Equal(GamepadButton.South, button);
        Assert.True(ConsoleWorldCommands.TryName<Key>("LCtrl", out var key));
        Assert.Equal(Key.ControlLeft, key);

        Assert.False(ConsoleWorldCommands.TryName<GamepadButton>("100", out _));
        Assert.False(ConsoleWorldCommands.TryName<GamepadButton>("0", out _));
        Assert.False(ConsoleWorldCommands.TryName<GamepadButton>("South, North", out _));
        Assert.False(ConsoleWorldCommands.TryName<Key>("Nothing", out _));
    }

    /// <summary>
    /// entity.set writes a list from its items split by semicolons, an enum's items by name, and
    /// refuses a number for an enum, in a list or a field of its own.
    /// </summary>
    [Fact]
    public void EntitySetWritesAListFromItemsSplitBySemicolonsAndAnEnumByName()
    {
        var answers = new List<string?>();
        var (route, kind) = (default(Route), default(BodyKind));
        var ran = false;

        using var harness = new EngineHarness(frames: 2);
        harness.On(Stage.Update, world =>
        {
            if (ran) return;
            ran = true;

            var ecs = world.Resource<EcsWorld>();
            var path = ecs.Spawn();
            ecs.SetName(path, "Path");
            ecs.Add(path, new Route());
            ecs.Add(path, new RigidBody { Kind = BodyKind.Static });

            using (ConsoleHost.Lend(world))
            {
                answers.Add(ConsoleCommands.Run("entity.set Path Route.Speeds 1;2.5;4"));
                answers.Add(ConsoleCommands.Run("entity.set Path Route.Moods restless;Calm"));
                answers.Add(ConsoleCommands.Run("entity.set Path Route.Moods calm;7"));
                answers.Add(ConsoleCommands.Run("entity.set Path RigidBody.Kind 1"));
                answers.Add(ConsoleCommands.Run("entity.set Path RigidBody.Kind kinematic"));
            }

            route = ecs.GetOrDefault<Route>(path);
            kind = ecs.GetOrDefault<RigidBody>(path).Kind;
        });

        harness.Run();

        Assert.Equal([1f, 2.5f, 4f], route.Speeds.Items.ToArray());
        Assert.Equal([Mood.Restless, Mood.Calm], route.Moods.Items.ToArray());
        Assert.Contains("is not a", answers[2]);
        Assert.Contains("is not a", answers[3]);
        Assert.Equal(BodyKind.Kinematic, kind);
    }

    /// <summary>input.drop sends a file dropped for each path, spaces kept, which a game reads at the next frame.</summary>
    [Fact]
    public void InputDropSendsAFileDroppedForEachPath()
    {
        var dropped = new List<string>();
        string? answer = null;
        var frame = 0;

        using var harness = new EngineHarness(frames: 4);
        harness.On(Stage.Update, world =>
        {
            if (++frame != 1) return;
            using (ConsoleHost.Lend(world)) answer = ConsoleCommands.Run("input.drop /levels/the yard.scene.json; /notes.txt");
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            foreach (var file in ctx.Read<FileDropped>()) dropped.Add(file.Path);
        });

        harness.Run();

        Assert.Equal("dropped 2 files", answer);
        Assert.Equal(["/levels/the yard.scene.json", "/notes.txt"], dropped);
    }
}

/// <summary>Commands that exist only for the tests above to call.</summary>
internal static class ArgumentProbeCommands
{
    [Command("tests.defaults", "Takes one word and three that can be left off")]
    internal static string Defaults(string which, int count = 2, float scale = 1.5f, string? label = null) =>
        $"{which} {count} {scale.ToString(System.Globalization.CultureInfo.InvariantCulture)} {label ?? "none"}";

    [Command("tests.lined", "Takes the rest of the line, or a default for none")]
    internal static string Lined(string text = "all of it") => text;
}
