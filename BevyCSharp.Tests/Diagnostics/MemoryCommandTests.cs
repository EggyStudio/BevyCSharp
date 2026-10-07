using System.Globalization;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers <c>memory</c>, which a long play reads at intervals to find what keeps climbing.
/// </summary>
/// <remarks>
/// What it answers is read by a script, so the test reads it the way <c>build/soak-check.py</c>
/// does, as name and number pairs, and holds the figures that move with what a game does to moving
/// with it. Bytes are not held to an exact count, since Bevy allocates for itself on every frame.
/// </remarks>
[Collection("engine")]
public sealed class MemoryCommandTests
{
    [Fact]
    public void MemoryReadsWhatTheProgramHoldsAsNameAndNumberPairs()
    {
        Dictionary<string, long>? before = null;
        Dictionary<string, long>? after = null;

        using var harness = new EngineHarness(frames: 2);

        harness.On(Stage.Update, world =>
        {
            var ecs = world.Resource<EcsWorld>();
            using (ConsoleHost.Lend(world)) before = Pairs(ConsoleCommands.Run("memory.collect") ?? "");

            for (var i = 0; i < 10; i++) ecs.Spawn();
            if (App.HasRenderer) Render.CreateMesh(MeshShape.Cuboid);

            using (ConsoleHost.Lend(world)) after = Pairs(ConsoleCommands.Run("memory") ?? "");
        });

        harness.Run();

        Assert.NotNull(before);
        Assert.NotNull(after);
        foreach (var name in new[] { "managed", "heap", "process", "nativeBytes", "nativeBlocks" })
            Assert.True(before[name] > 0, $"{name} is {before[name]}");

        Assert.Equal(before["entities"] + 10, after["entities"]);
        Assert.True(after["entityIds"] >= after["entities"]);
        Assert.Equal(before["handles"] + (App.HasRenderer ? 1 : 0), after["handles"]);
        if (App.HasRenderer)
            Assert.Equal(before.GetValueOrDefault("asset.Mesh") + 1, after["asset.Mesh"]);
    }

    [Fact]
    public void MemoryAnsweredOutsideAFrameLeavesTheWorldOut()
    {
        var pairs = Pairs(ConsoleMemoryCommands.Memory());

        Assert.True(pairs["nativeBytes"] > 0);
        Assert.False(pairs.ContainsKey("entities"), "there is no world to count entities in");
        Assert.False(pairs.ContainsKey("handles"));
    }

    private static Dictionary<string, long> Pairs(string answer)
    {
        var words = answer.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(words.Length % 2 == 0, $"an odd number of words in '{answer}'");

        return Enumerable.Range(0, words.Length / 2)
            .ToDictionary(i => words[2 * i], i => long.Parse(words[2 * i + 1], CultureInfo.InvariantCulture));
    }
}
