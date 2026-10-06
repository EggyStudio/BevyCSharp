using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers a state's transitions read as Bevy's messages, a value set again, and what is despawned as a value is entered.</summary>
[Collection("engine")]
public sealed class StateTransitionTests
{
    /// <summary>
    /// Each transition is a message of its enum, the state's first value with nothing before it,
    /// and a value set again an identity transition, which entering the value runs on again, as
    /// Bevy's <c>NextState::set</c> has it, and a transition system from the value to itself on it
    /// alone.
    /// </summary>
    [Fact]
    public void EachTransitionIsAMessageAndAValueSetAgainIsOneToItself()
    {
        var frame = 0;
        var seen = new List<(Screen? Exited, Screen? Entered)>();
        var (entered, reentered) = (0, 0);

        using var harness = new EngineHarness(frames: 12);
        harness.App.AddState(Screen.Menu);
        harness.App.AddStateSystem(Screen.Playing, entering: true, new SystemDescriptor(_ => entered++, "Test.Entered"));
        harness.App.AddTransitionSystem(Screen.Playing, Screen.Playing, new SystemDescriptor(_ => reentered++, "Test.Reentered"));
        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            foreach (var transition in ctx.Read<StateTransitionEvent<Screen>>()) seen.Add((transition.Exited, transition.Entered));
            if (frame is 2 or 5) ctx.SetState(Screen.Playing);
        });
        harness.Run();

        Assert.Equal([(null, Screen.Menu), (Screen.Menu, Screen.Playing), (Screen.Playing, Screen.Playing)], seen);
        Assert.Equal((2, 1), (entered, reentered));
    }

    /// <summary>An entity scoped to entering a value goes as the state enters it, and one scoped to entering another stays.</summary>
    [Fact]
    public void AnEntityScopedToEnteringAValueGoesAsItIsEntered()
    {
        var frame = 0;
        var (playing, menu) = (Entity.None, Entity.None);
        bool? playingAlive = null, menuAlive = null;

        using var harness = new EngineHarness(frames: 8);
        harness.App.AddState(Screen.Menu);
        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            if (frame == 2)
            {
                (playing, menu) = (ctx.Ecs.Spawn(), ctx.Ecs.Spawn());
                ctx.Ecs.DespawnOnEnter(playing, Screen.Playing);
                ctx.Ecs.DespawnOnEnter(menu, Screen.Menu);
                ctx.SetState(Screen.Playing);
            }

            if (frame == 6) (playingAlive, menuAlive) = (ctx.Ecs.IsAlive(playing), ctx.Ecs.IsAlive(menu));
        });
        harness.Run();

        Assert.False(playingAlive);
        Assert.True(menuAlive);
    }

    /// <summary>An entity waiting on a rule over the transition goes at the first transition the rule answers true for, and not before.</summary>
    [Fact]
    public void AnEntityGoesAtTheFirstTransitionItsRuleAnswersTrueFor()
    {
        var frame = 0;
        var watched = Entity.None;
        bool? afterPlaying = null, afterPaused = null;

        using var harness = new EngineHarness(frames: 12);
        harness.App.AddState(Screen.Menu);
        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            if (frame == 2)
            {
                watched = ctx.Ecs.Spawn();
                ctx.Ecs.DespawnWhen<Screen>(watched, transition => transition.Entered == Screen.Paused);
                ctx.SetState(Screen.Playing);
            }

            if (frame == 5)
            {
                afterPlaying = ctx.Ecs.IsAlive(watched);
                ctx.SetState(Screen.Paused);
            }

            if (frame == 9) afterPaused = ctx.Ecs.IsAlive(watched);
        });
        harness.Run();

        Assert.True(afterPlaying);
        Assert.False(afterPaused);
    }
}
