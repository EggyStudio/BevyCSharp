using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// What the editor is pointed at, and what survives the world changing under it.
/// </summary>
/// <remarks>
/// The selection remembers what it held by name so that reloading a script, which despawns
/// everything it made and makes it again with new ids, does not lose it. That memory has to tell
/// a selection somebody gave up from one the world took away, which is the distinction these
/// check.
///
/// In the engine collection, because it runs a real app and two of those at once is not something
/// the native side allows: the component registry belongs to whichever app is current, so a test
/// building one while another is being torn down fails on a registration that has nowhere to go.
/// </remarks>
[Collection("engine")]
public sealed class EditorSelectionTests
{
    [Fact]
    public void ChoosingNothingStays()
    {
        using var harness = new EngineHarness(frames: 3);

        var after = 1;

        harness.OnContext(Stage.Update, ctx =>
        {
            var thing = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(thing, "Chosen");

            EditorSelection.Select(thing);

            // Two passes, because what would undo this runs on the pass after the one that let go.
            EditorSelection.Prune(ctx.Ecs);
            EditorSelection.Clear();
            EditorSelection.Prune(ctx.Ecs);
            EditorSelection.Prune(ctx.Ecs);

            after = EditorSelection.Count;
        });

        harness.Run();

        Assert.Equal(0, after);
    }

    [Fact]
    public void WhatAReloadTookAwayComesBackByName()
    {
        using var harness = new EngineHarness(frames: 6);

        var step = 0;
        var made = Entity.None;
        var again = Entity.None;
        var found = false;

        // A frame apart, because a despawn asked for inside a system happens at the end of it, so
        // the entity is still alive for the rest of the callback that let it go.
        harness.OnContext(Stage.Update, ctx =>
        {
            switch (step++)
            {
                case 0:
                    made = ctx.Ecs.Spawn();
                    ctx.Ecs.SetName(made, "Reloaded");
                    EditorSelection.Select(made);
                    break;

                case 1:
                    // The name is kept while everything chosen is alive, which is the only moment
                    // it can be asked for.
                    EditorSelection.Prune(ctx.Ecs);
                    ctx.Ecs.Despawn(made);
                    break;

                case 2:
                    EditorSelection.Prune(ctx.Ecs);

                    // What the script made again: a new id with the same name.
                    again = ctx.Ecs.Spawn();
                    ctx.Ecs.SetName(again, "Reloaded");
                    break;

                case 3:
                    EditorSelection.Prune(ctx.Ecs);
                    found = EditorSelection.Count == 1 && EditorSelection.Current == again;
                    EditorSelection.Clear();
                    break;
            }
        });

        harness.Run();

        Assert.True(found);
    }

    [Fact]
    public void TakingTheLastOneOutLeavesNothing()
    {
        using var harness = new EngineHarness(frames: 3);

        var after = 1;

        harness.OnContext(Stage.Update, ctx =>
        {
            var thing = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(thing, "Toggled");

            EditorSelection.Select(thing);
            EditorSelection.Prune(ctx.Ecs);

            EditorSelection.Toggle(thing);
            EditorSelection.Prune(ctx.Ecs);
            EditorSelection.Prune(ctx.Ecs);

            after = EditorSelection.Count;
        });

        harness.Run();

        Assert.Equal(0, after);
    }
}
