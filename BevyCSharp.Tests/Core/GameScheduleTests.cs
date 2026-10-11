using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Schedules of a game's own, run when a system asks, and a system inside a state's transition run
/// with each move, which Bevy's custom_transitions puts together as its <c>OnReenter</c>.
/// </summary>
[Collection("engine")]
public sealed class GameScheduleTests
{
    [Fact]
    public void AScheduleOfTheGamesOwnRunsWhenAskedAndOneUnknownDoesNot()
    {
        var ran = 0;
        var answers = new List<(bool Own, bool Unknown)>();

        using var harness = new EngineHarness(frames: 3);
        harness.App.AddSystem(new ScheduleLabel("Tests.Own"), new SystemDescriptor(_ => ran++, "Tests.InOwn"));
        harness.OnContext(Stage.Update, ctx =>
            answers.Add((ctx.TryRunSchedule(new ScheduleLabel("Tests.Own")), ctx.TryRunSchedule(new ScheduleLabel("Tests.Unknown")))));
        harness.Run();

        Assert.Equal(3, ran);
        Assert.All(answers, answer => Assert.Equal((true, false), answer));
    }

    /// <summary>
    /// A schedule placed after a frame's stage runs in every frame right after it, one placed after
    /// Bevy's pre-startup runs once before the startup stage, a single-threaded one as well, and a
    /// stage that is no schedule of Bevy's, or a second placing, is refused.
    /// </summary>
    [Fact]
    public void APlacedScheduleRunsInItsPlaceInTheMainOrder()
    {
        var order = new List<string>();

        using var harness = new EngineHarness(frames: 2);
        var app = harness.App;
        app.AddSchedule(new ScheduleLabel("Tests.AfterUpdate"), Stage.Update, singleThreaded: true);
        app.AddSchedule(new ScheduleLabel("Tests.CustomStartup"), Stage.PreStartup);
        app.AddSystem(new ScheduleLabel("Tests.AfterUpdate"), new SystemDescriptor(_ => order.Add("AfterUpdate"), "Tests.InAfterUpdate"));
        app.AddSystem(new ScheduleLabel("Tests.CustomStartup"), new SystemDescriptor(_ => order.Add("CustomStartup"), "Tests.InCustomStartup"));
        app.AddSystem(Stage.PreStartup, new SystemDescriptor(_ => order.Add("PreStartup"), "Tests.PreStartup"));
        app.AddSystem(Stage.Startup, new SystemDescriptor(_ => order.Add("Startup"), "Tests.Startup"));
        app.AddSystem(Stage.Update, new SystemDescriptor(_ => order.Add("Update"), "Tests.Update"));
        app.AddSystem(Stage.Last, new SystemDescriptor(_ => order.Add("Last"), "Tests.Last"));

        Assert.Throws<ArgumentOutOfRangeException>(() => app.AddSchedule(new ScheduleLabel("Tests.Fixed"), Stage.FixedUpdate));
        Assert.Throws<InvalidOperationException>(() => app.AddSchedule(new ScheduleLabel("Tests.AfterUpdate"), Stage.Last));
        harness.Run();

        Assert.Equal(["PreStartup", "CustomStartup", "Startup", "Update", "AfterUpdate", "Last", "Update", "AfterUpdate", "Last"], order);
    }

    /// <summary>
    /// A system among a state's entries sees the first value entered from none, a move, and a value
    /// set again, and runs a schedule named for the value it entered, inside the transition, so
    /// what that schedule does is there before the frame's Update.
    /// </summary>
    [Fact]
    public void AStepSystemRunsWithEachMoveAndRunsTheValuesSchedule()
    {
        var frame = 0;
        var moves = new List<(Screen? Exited, Screen? Entered)>();
        var reentered = new List<int>();

        using var harness = new EngineHarness(frames: 8);
        harness.App.AddState(Screen.Menu);
        harness.App.OnStateTransition<Screen>(TransitionStep.Enter, (ctx, move) =>
        {
            moves.Add((move.Exited, move.Entered));
            ctx.TryRunSchedule(new ScheduleLabel($"Tests.OnReenter({move.Entered})"));
        }, "Tests.RunReenter");
        harness.App.AddSystem(new ScheduleLabel("Tests.OnReenter(Playing)"), new SystemDescriptor(_ => reentered.Add(frame), "Tests.Reentered"));
        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            if (frame is 2 or 4) ctx.SetState(Screen.Playing);
        });
        harness.Run();

        Assert.Equal([(null, Screen.Menu), (Screen.Menu, Screen.Playing), (Screen.Playing, Screen.Playing)], moves);

        // Set in Update and entered in the next frame's transition, before its Update counts it.
        Assert.Equal([2, 4], reentered);
    }

    [Fact]
    public void ASystemInAScheduleOfItsOwnCannotBeOrdered()
    {
        using var harness = new EngineHarness(frames: 1);
        harness.App.AddSystem(new ScheduleLabel("Tests.Own"), new SystemDescriptor(_ => { }, "Tests.InOwn").After("Tests.Other"));

        var refused = Assert.Throws<InvalidOperationException>(harness.Run);
        Assert.Contains("schedule of the game's own", refused.Message);
    }
}
