using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Taking back what the editor changed.
/// </summary>
/// <remarks>
/// A pair of stacks over closures, so what is worth testing is which of them runs, in what order,
/// and what the stacks hold afterwards. The world is only passed through to the closures, but it
/// is a real one. The ECS is loaned for the length of a system callback, so the assertions run
/// inside one.
///
/// In the engine collection, because it runs a real app and two of those at once is not something
/// the native side allows: the component registry belongs to whichever app is current, so a test
/// building one while another is being torn down fails on a registration that has nowhere to go.
/// </remarks>
[Collection("engine")]
public sealed class EditorHistoryTests
{
    [Fact]
    public void WhatWasDoneIsUndoneAndCanBeDoneAgain()
    {
        using var harness = new EngineHarness(frames: 2);

        var value = 0;
        var undone = string.Empty;
        var redone = string.Empty;
        var betweenThem = 0;

        harness.OnContext(Stage.Update, ctx =>
        {
            EditorHistory.Clear();

            EditorHistory.Record("set", _ => value = 1, _ => value = 2);
            value = 2;

            undone = EditorHistory.Undo(ctx.Ecs) ?? string.Empty;
            betweenThem = value;

            redone = EditorHistory.Redo(ctx.Ecs) ?? string.Empty;
        });

        harness.Run();

        Assert.Equal("set", undone);
        Assert.Equal(1, betweenThem);
        Assert.Equal("set", redone);
        Assert.Equal(2, value);
    }

    [Fact]
    public void ChangingSomethingElseGivesUpWhatWasUndone()
    {
        using var harness = new EngineHarness(frames: 2);

        var couldRedo = false;
        var canRedo = true;

        harness.OnContext(Stage.Update, ctx =>
        {
            EditorHistory.Clear();

            EditorHistory.Record("first", _ => { }, _ => { });
            EditorHistory.Undo(ctx.Ecs);

            couldRedo = EditorHistory.CanRedo;

            // The branch that was undone stops being a thing to return to the moment the world is
            // changed by hand.
            EditorHistory.Record("second", _ => { }, _ => { });

            canRedo = EditorHistory.CanRedo;
        });

        harness.Run();

        Assert.True(couldRedo);
        Assert.False(canRedo);
    }

    [Fact]
    public void TypingIntoOneFieldIsOneChange()
    {
        using var harness = new EngineHarness(frames: 2);

        var value = 0;
        var after = -1;
        var more = true;

        harness.OnContext(Stage.Update, ctx =>
        {
            EditorHistory.Clear();

            // Three keystrokes into the same field, which is one edit to the person doing it.
            EditorHistory.Record("speed", _ => value = 0, _ => value = 1, "speed");
            EditorHistory.Record("speed", _ => value = 0, _ => value = 12, "speed");
            EditorHistory.Record("speed", _ => value = 0, _ => value = 123, "speed");

            value = 123;

            EditorHistory.Undo(ctx.Ecs);
            after = value;
            more = EditorHistory.CanUndo;

            EditorHistory.Redo(ctx.Ecs);
        });

        harness.Run();

        Assert.Equal(0, after);
        Assert.False(more);
        Assert.Equal(123, value);
    }

    [Fact]
    public void NothingToUndoIsAnswered()
    {
        using var harness = new EngineHarness(frames: 2);

        string? undo = "x";
        string? redo = "x";
        string? last = "x";

        harness.OnContext(Stage.Update, ctx =>
        {
            EditorHistory.Clear();

            undo = EditorHistory.Undo(ctx.Ecs);
            redo = EditorHistory.Redo(ctx.Ecs);
            last = EditorHistory.Last;
        });

        harness.Run();

        Assert.Null(undo);
        Assert.Null(redo);
        Assert.Null(last);
    }

    [Fact]
    public void TakingAComponentOffAndPuttingItBackKeepsWhatItHeld()
    {
        using var harness = new EngineHarness(frames: 2);

        var speed = 0f;
        var count = 0;
        var carried = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            EditorHistory.Clear();

            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new Described { Speed = 7.5f, Count = 3 });

            var schema = ComponentSchemas.For("Bevy.Tests.Described")!;

            EditorEntity.Drop(ctx.Ecs, entity, schema);

            // Gone, which is the change being undone.
            Assert.Null(schema.Read(ctx.Ecs, entity, "Speed"));

            EditorHistory.Undo(ctx.Ecs);

            carried = schema.Read(ctx.Ecs, entity, "Speed") is not null;
            speed = (float)(schema.Read(ctx.Ecs, entity, "Speed") ?? 0f);
            count = (int)(schema.Read(ctx.Ecs, entity, "Count") ?? 0);
        });

        harness.Run();

        // What a component holds is the component. One put back empty is the same loss with a row
        // drawn over it, so this asserts the values rather than the row.
        Assert.True(carried);
        Assert.Equal(7.5f, speed, 3);
        Assert.Equal(3, count);
    }

    [Fact]
    public void AddingAComponentIsTakenBackAsOneChange()
    {
        using var harness = new EngineHarness(frames: 2);

        var after = true;
        var again = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            EditorHistory.Clear();

            var entity = ctx.Ecs.Spawn();
            var schema = ComponentSchemas.For("Bevy.Tests.Described")!;

            EditorEntity.Carry(ctx.Ecs, entity, schema);

            EditorHistory.Undo(ctx.Ecs);
            after = schema.Read(ctx.Ecs, entity, "Count") is not null;

            EditorHistory.Redo(ctx.Ecs);
            again = schema.Read(ctx.Ecs, entity, "Count") is not null;
        });

        harness.Run();

        Assert.False(after);
        Assert.True(again);
    }
}
