using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers <c>state.get</c> and <c>state.set</c>, which a script playing a game moves it between
/// screens with.
/// </summary>
/// <remarks>
/// A state is named as a person types it, so the names are given here in the wrong case on
/// purpose, and a name that is no state or no value answers with the ones there are, since the
/// person typing it needs those next.
/// </remarks>
[Collection("engine")]
public sealed class StateCommandTests
{
    [Fact]
    public void StateSetMovesAStateNamedByItsEnumAndStateGetReadsIt()
    {
        using var harness = new EngineHarness(frames: 4);
        harness.App.AddState(Screen.Menu).AddState(Connection.Offline);

        var answers = new List<string>();
        var frame = 0;
        harness.On(Stage.Update, world =>
        {
            using var lent = ConsoleHost.Lend(world);
            switch (frame++)
            {
                case 0:
                    answers.Add(ConsoleCommands.Run("state.get") ?? "");
                    answers.Add(ConsoleCommands.Run("state.set screen PLAYING") ?? "");
                    answers.Add(ConsoleCommands.Run("state.set Screen Lost") ?? "");
                    answers.Add(ConsoleCommands.Run("state.set Weather Rain") ?? "");
                    answers.Add(ConsoleCommands.Run("state.get Screen") ?? "");
                    break;
                case 1:
                    answers.Add(ConsoleCommands.Run("state.get Screen") ?? "");
                    break;
            }
        });

        harness.Run();

        var states = answers[0].Split('\n');
        Assert.Contains("Screen Menu", states);
        Assert.Contains("Connection Offline", states);
        Assert.Equal("Screen goes to Playing at its next transition", answers[1]);
        Assert.Equal("Screen has no value Lost, only Menu, Playing, Paused", answers[2]);
        Assert.StartsWith("no state is called Weather, only ", answers[3]);
        Assert.Equal("Menu", answers[4]);
        Assert.Equal("Playing", answers[5]);
    }
}
