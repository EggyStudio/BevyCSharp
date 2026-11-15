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
/// the native side allows. The component registry belongs to whichever app is current, so a test
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

    /// <summary>
    /// A deleted entity comes back on undo with what was under it, what it was drawn with, where
    /// it stood, and what pointed at it, and goes again on redo.
    /// </summary>
    [SkippableFact]
    public void ADeleteComesBackWithWhatIsUnderItAndWhatPointedAtIt()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 2);

        var deleted = false;
        var redone = false;
        string? lid = null;
        Vec3 at = default;
        MeshRecipe? recipe = null;
        MaterialSettings? paint = null;
        var pointed = false;
        var twice = false;
        var ran = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (ran) return;
            ran = true;

            var ecs = ctx.Ecs;
            EditorHistory.Clear();

            // Drawn with a primitive and a material made in memory, which only a scene's
            // resources can bring back.
            var box = ecs.Spawn();
            ecs.SetName(box, "Box");
            ecs.Add(box, Transform.At(1f, 2f, 3f));
            Render.SetMesh(ecs, box, Render.CreateMesh(MeshShape.Cuboid, 1f, 2f, 3f));
            Render.SetMaterial(ecs, box, Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 0f, 0f, 1f) }));

            var under = ecs.Spawn();
            ecs.SetName(under, "Lid");
            ecs.SetParent(under, box);

            var follower = ecs.Spawn();
            ecs.SetName(follower, "Follower");
            ecs.Add(follower, new EveryKind { Target = box, Count = 7 });

            EditorEntity.Delete(ecs, [box]);
            deleted = !ecs.IsAlive(box) && !ecs.IsAlive(under);

            EditorHistory.Undo(ecs);

            var back = ecs.All().Single(entity => ecs.NameOf(entity) == "Box");
            lid = ecs.ChildrenOf(back).Select(ecs.NameOf).SingleOrDefault();
            at = ecs.GetRef<Transform>(back).Translation;
            recipe = Render.RecipeOf(Render.MeshOf(ecs, back));
            Render.TryReadMaterial(Render.MaterialOf(ecs, back), out paint);
            pointed = ecs.GetRef<EveryKind>(follower).Target == back;

            EditorHistory.Redo(ecs);
            redone = !ecs.All().Any(entity => ecs.NameOf(entity) is "Box" or "Lid");

            // And back a second time, from what the redo kept.
            EditorHistory.Undo(ecs);
            var again = ecs.All().Single(entity => ecs.NameOf(entity) == "Box");
            twice = ecs.GetRef<EveryKind>(follower).Target == again && ecs.ChildrenOf(again).Length == 1;
        });

        harness.Run();

        Assert.True(deleted);
        Assert.Equal("Lid", lid);
        Assert.Equal(new Vec3(1f, 2f, 3f), at);
        Assert.Equal(new MeshRecipe(MeshShape.Cuboid, 1f, 2f, 3f), recipe);
        Assert.NotNull(paint);
        Assert.Equal((1f, 0f, 0f, 1f), paint.BaseColor);
        Assert.True(pointed, "the follower did not point at the box that came back");
        Assert.True(redone);
        Assert.True(twice);
    }

    /// <summary>
    /// The same field edited on one entity and then on another is two edits, each undone and redone
    /// on its own entity, since a run of edits is one only while it stays on one field of one entity.
    /// Under one key the second would join the first, which would then undo the first entity and
    /// redo the second, and leave the second's change out of the history.
    /// </summary>
    [Fact]
    public void OneFieldEditedOnTwoEntitiesInARowIsTwoEdits()
    {
        using var harness = new EngineHarness(frames: 2);

        var steps = new List<string>();
        var ran = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (ran) return;
            ran = true;

            var ecs = ctx.Ecs;
            EditorHistory.Clear();

            var (a, b) = (ecs.Spawn(), ecs.Spawn());
            ecs.Add(a, new EveryKind { Count = 0 });
            ecs.Add(b, new EveryKind { Count = 2 });

            var schema = ecs.ComponentsOf(a).Select(ComponentSchemas.For).First(found => found?.Name == "EveryKind")!;
            var count = schema.Field("Count")!;

            // Each through the path the details panel records a row by, under the row's own id.
            foreach (var (entity, value) in new[] { (a, 5), (b, 7) })
            {
                var before = count.Read(ecs, entity);
                count.Write(ecs, entity, value);
                ComponentFields.Recorded(ctx, entity, schema, count, "##EveryKind.Count", before);
            }

            string State() => $"{ecs.GetRef<EveryKind>(a).Count} {ecs.GetRef<EveryKind>(b).Count}";

            steps.Add(State());
            for (var i = 0; i < 2; i++)
            {
                EditorHistory.Undo(ecs);
                steps.Add(State());
            }

            for (var i = 0; i < 2; i++)
            {
                EditorHistory.Redo(ecs);
                steps.Add(State());
            }
        });

        harness.Run();

        Assert.Equal(["5 7", "5 2", "0 2", "5 2", "5 7"], steps);
    }

    /// <summary>
    /// An edit made with several entities selected, spread from the shown one onto the rest, is
    /// one step that undoes on every entity it reached, each back to what it held, where the step
    /// held the shown entity alone and an undo left the rest changed.
    /// </summary>
    [Fact]
    public void AnEditSpreadOverTheSelectionIsUndoneOnEveryEntityItReached()
    {
        using var harness = new EngineHarness(frames: 2);

        var steps = new List<string>();
        var ran = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (ran) return;
            ran = true;

            var ecs = ctx.Ecs;
            EditorHistory.Clear();

            var (shown, other, third) = (ecs.Spawn(), ecs.Spawn(), ecs.Spawn());
            ecs.Add(shown, new EveryKind { Count = 1 });
            ecs.Add(other, new EveryKind { Count = 2 });
            ecs.Add(third, new EveryKind { Count = 3 });

            var schema = ecs.ComponentsOf(shown).Select(ComponentSchemas.For).First(found => found?.Name == "EveryKind")!;
            var count = schema.Field("Count")!;

            // As the details panel's row does it, the widget writing the shown entity, then the edit
            // recorded and the value spread onto the rest of the selection.
            var before = count.Read(ecs, shown);
            count.Write(ecs, shown, 9);
            ComponentFields.Recorded(ctx, shown, schema, count, "##EveryKind.Count", before, [other, third]);
            ComponentFields.Spread(ctx, shown, [other, third], count, before);

            string State() => $"{ecs.GetRef<EveryKind>(shown).Count} {ecs.GetRef<EveryKind>(other).Count} {ecs.GetRef<EveryKind>(third).Count}";

            steps.Add(State());
            EditorHistory.Undo(ecs);
            steps.Add(State());
            EditorHistory.Redo(ecs);
            steps.Add(State());
        });

        harness.Run();

        Assert.Equal(["9 9 9", "1 2 3", "9 9 9"], steps);
    }

    /// <summary>
    /// A scene loaded starts with nothing to undo, where the edits made to the scene before it
    /// stayed and an undo wrote to an entity the load had taken away.
    /// </summary>
    [Fact]
    public void ALoadedSceneStartsWithNothingToUndo()
    {
        using var folder = new TestFolder("bcs-history-");
        var file = Path.Combine(folder.Path, "other.scene.json");
        File.WriteAllText(file, """
            { "format": "bevycsharp.scene.2",
              "entities": [ { "id": 1, "name": "Barrel", "components": { "Bevy.Tests.EveryKind": { "Count": 4 } } } ] }
            """);

        using var harness = new EngineHarness(frames: 2);
        var ran = false;
        var answered = "not asked";

        harness.OnContext(Stage.Update, ctx =>
        {
            if (ran) return;
            ran = true;

            var ecs = ctx.Ecs;
            EditorHistory.Clear();

            var box = ecs.Spawn();
            ecs.Add(box, new EveryKind { Count = 1 });
            EditorEntity.Rename(ecs, box, "Box");

            Assert.Null(EditorScene.Load(ecs, file).Problem);
            Assert.False(ecs.IsAlive(box));
            answered = EditorHistory.Undo(ecs) ?? "nothing";
        });

        harness.Run();

        Assert.Equal("nothing", answered);
    }

    /// <summary>
    /// A duplicate undone and redone makes new copies, and an edit made on a copy after the
    /// duplicate is redone on the copy made again, where it wrote to the copy the undo took away
    /// and was lost.
    /// </summary>
    [Fact]
    public void ARedoneDuplicateTakesTheEditsMadeOnTheCopyAfterIt()
    {
        using var harness = new EngineHarness(frames: 2);

        var steps = new List<string>();
        var ran = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (ran) return;
            ran = true;

            var ecs = ctx.Ecs;
            EditorHistory.Clear();

            var box = ecs.Spawn();
            ecs.SetName(box, "Box");
            ecs.Add(box, new EveryKind { Count = 1 });

            var copy = Assert.Single(EditorEntity.Duplicate(ecs, [box]));
            EditorEntity.Rename(ecs, copy, "Crate");

            string State() => string.Join(" ", ecs.All().Select(ecs.NameOf).OfType<string>().Where(name => name is "Box" or "Box 2" or "Crate").Order(StringComparer.Ordinal));

            steps.Add(State());
            for (var i = 0; i < 2; i++)
            {
                EditorHistory.Undo(ecs);
                steps.Add(State());
            }

            for (var i = 0; i < 2; i++)
            {
                EditorHistory.Redo(ecs);
                steps.Add(State());
            }
        });

        harness.Run();

        Assert.Equal(["Box Crate", "Box Box 2", "Box", "Box Box 2", "Box Crate"], steps);
    }

    /// <summary>
    /// The edits made before a delete still undo once the delete is undone, on the entity that came
    /// back rather than the one that went, and redo again in order.
    /// </summary>
    [Fact]
    public void EditsBeforeADeleteUndoOnWhatTheUndoBroughtBack()
    {
        using var harness = new EngineHarness(frames: 2);

        var steps = new List<string>();
        var ran = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (ran) return;
            ran = true;

            var ecs = ctx.Ecs;
            EditorHistory.Clear();

            var box = ecs.Spawn();
            ecs.SetName(box, "Box");
            ecs.Add(box, new EveryKind { Count = 1 });

            var schema = ecs.ComponentsOf(box).Select(ComponentSchemas.For).First(found => found?.Name == "EveryKind")!;
            var count = schema.Field("Count")!;

            // A field edited through the same path the details panel records it by, then a rename.
            var before = count.Read(ecs, box);
            count.Write(ecs, box, 5);
            ComponentFields.Recorded(ctx, box, schema, count, "EveryKind.Count", before);
            EditorEntity.Rename(ecs, box, "Crate");

            EditorEntity.Delete(ecs, [box]);

            string State()
            {
                var found = ecs.All().Where(entity => ecs.NameOf(entity) is "Box" or "Crate").ToArray();
                return found.Length == 0
                    ? "gone"
                    : $"{ecs.NameOf(found[0])} {ecs.GetRef<EveryKind>(found[0]).Count}";
            }

            steps.Add(State());
            for (var i = 0; i < 3; i++)
            {
                EditorHistory.Undo(ecs);
                steps.Add(State());
            }

            for (var i = 0; i < 3; i++)
            {
                EditorHistory.Redo(ecs);
                steps.Add(State());
            }
        });

        harness.Run();

        Assert.Equal(
            ["gone", "Crate 5", "Box 5", "Box 1", "Box 5", "Crate 5", "gone"],
            steps);
    }
}
