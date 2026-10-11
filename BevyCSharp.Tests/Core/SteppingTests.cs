using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A stage stepped one system at a time, as Bevy's system_stepping steps <c>Update</c>, driven from
/// a stage that is not stepped as Bevy's breakout drives it from its own schedule.
/// </summary>
[Collection("engine")]
public sealed class SteppingTests
{
    [Fact]
    public void AStageWaitsAndRunsASystemAStepWhileTheRestRun()
    {
        using var harness = new EngineHarness(frames: 8);
        var frame = 0;
        var ran = new List<(int Frame, string Name)>();
        SteppedSystem[]? systems = null;
        string? atStep = null, described = null;
        int? afterContinue = -1;

        harness.App
            .On(Stage.PreUpdate, _ => frame++, "Tests.Count")
            .Chain(Stage.Update,
                new SystemDescriptor(_ => ran.Add((frame, "One")), "Tests.One"),
                new SystemDescriptor(_ => ran.Add((frame, "Two")), "Tests.Two"));

        harness.On(Stage.Last, _ =>
        {
            switch (frame)
            {
                case 1:
                    Stepping.Add(Stage.Update);
                    Stepping.Enable();
                    break;
                case 3:
                    // Known once Update has run with stepping on, and everything but the two left
                    // to run, as Bevy's helper leaves Bevy's own.
                    systems = Stepping.Systems;
                    foreach (var other in systems!.Where(system => !system.Name.StartsWith("Tests.")))
                        Stepping.AlwaysRun(other.Stage, other.Name);
                    Stepping.StepFrame();
                    break;
                case 4:
                    atStep = Stepping.Systems![Stepping.Cursor!.Value].Name;
                    described = Stepping.Describe();
                    Stepping.ContinueFrame();
                    break;
                case 5:
                    afterContinue = Stepping.Cursor;
                    Stepping.Disable();
                    break;
            }
        }, "Tests.Drive");

        harness.Run();

        // Both ran before stepping was on, neither while it waited, then one a step, the rest on
        // continuing, and both once it was off.
        Assert.Equal(
            [(1, "One"), (1, "Two"), (4, "One"), (5, "Two"), (6, "One"), (6, "Two"), (7, "One"), (7, "Two"), (8, "One"), (8, "Two")],
            ran);

        var names = systems!.Select(system => system.Name).ToList();
        Assert.True(names.IndexOf("Tests.One") < names.IndexOf("Tests.Two"), string.Join(", ", names));
        Assert.All(systems!, system => Assert.Equal(Stage.Update, system.Stage));
        Assert.Equal("Tests.Two", atStep);
        Assert.Contains("Update", described);
        Assert.Null(afterContinue);
    }

    [Fact]
    public void AStageThatRunsOnceIsRefused()
    {
        using var harness = new EngineHarness(frames: 1);
        Exception? refused = null;
        harness.On(Stage.Update, _ => refused = Record.Exception(() => Stepping.Add(Stage.Startup)));

        harness.Run();

        Assert.IsType<ArgumentOutOfRangeException>(refused);
    }
}
