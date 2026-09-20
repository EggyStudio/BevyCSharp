using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the loan of the world to a command.
/// </summary>
/// <remarks>
/// A command is a static method over words, with nowhere for a world to be passed in. Everything
/// ECS-touching in this engine is ambient on the world Bevy lends the running system, so a command
/// about entities is only meaningful inside a frame, and something has to say which frame. That is
/// the loan, and both halves of it are worth pinning down: that a lent command can see the world,
/// and that an unlent one says so instead of taking the program down.
/// </remarks>
[Collection("engine")]
public sealed class CliConsoleHostTests
{
    [Fact]
    public void ALentCommandSeesTheWorld()
    {
        string? answer = null;

        using var harness = new EngineHarness(frames: 2);

        harness.On(Stage.Update, world =>
        {
            var entity = world.Resource<EcsWorld>().Spawn();
            world.Resource<EcsWorld>().SetName(entity, "Lent");

            using (ConsoleHost.Lend(world)) answer = ConsoleCommands.Run("entity.list");
        });

        harness.Run();

        Assert.NotNull(answer);
        Assert.Contains("Lent", answer);
    }

    [Fact]
    public void TheLoanEndsWithTheScope()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.On(Stage.Update, world =>
        {
            using (ConsoleHost.Lend(world)) Assert.Same(world, ConsoleHost.World);

            Assert.Null(ConsoleHost.World);
        });

        harness.Run();
    }

    /// <summary>
    /// An unlent command answers with a sentence rather than throwing.
    /// </summary>
    /// <remarks>
    /// The console is a place where things are asked at the wrong moment, and a program that stops
    /// because somebody typed a command from outside a frame is not a tool. The dispatcher catches
    /// it; this is that contract, not an accident of it.
    /// </remarks>
    [Fact]
    public void AnUnlentCommandIsAnsweredRatherThanThrown()
    {
        Assert.Null(ConsoleHost.World);

        var answer = ConsoleCommands.Run("entity.list");

        Assert.NotNull(answer);
        Assert.Contains("failed", answer);
        Assert.Contains("inside a system", answer);
    }
}
