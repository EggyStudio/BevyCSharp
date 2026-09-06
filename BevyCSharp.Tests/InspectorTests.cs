using BevyCSharp.Editor.Drawers;
using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The part of the inspector that decides what a field looks like.
/// </summary>
/// <remarks>
/// No window and no widgets: which drawer takes a field, how many rows it asks for, and what a
/// field's attributes do to the answer are all decided from the schema alone. That is the half of
/// an inspector worth a test rather than a screenshot.
/// </remarks>
public sealed class InspectorDrawerTests
{
    [Fact]
    public void ANumberIsDrawnAsANumber()
    {
        var drawer = EditorDrawers.For(Field("Speed", FieldKind.Float));

        Assert.IsType<NumberDrawer>(drawer);
        Assert.Equal(1, drawer.Lines(Field("Speed", FieldKind.Float)));
    }

    [Fact]
    public void ARangeTurnsANumberIntoABar()
    {
        var field = Field("Speed", FieldKind.Float, new FieldHints(Minimum: 0d, Maximum: 10d));

        Assert.IsType<SliderDrawer>(EditorDrawers.For(field));
    }

    [Fact]
    public void AVectorAsksForThreeRows()
    {
        var field = Field("Offset", FieldKind.Vec3);
        var drawer = EditorDrawers.For(field);

        Assert.IsType<VectorDrawer>(drawer);
        Assert.Equal(3, drawer.Lines(field));
    }

    [Fact]
    public void AColourAsksForOneMore()
    {
        var field = Field("Tint", FieldKind.Vec3, new FieldHints(Colour: true));
        var drawer = EditorDrawers.For(field);

        // The colour itself, and the three numbers behind it on one line under it.
        Assert.IsType<ColourDrawer>(drawer);
        Assert.Equal(2, drawer.Lines(field));
    }

    [Fact]
    public void ThreeNumbersAcrossTheLineAreOneRow()
    {
        var stacked = Field("Offset", FieldKind.Vec3);
        var across = Field("Offset", FieldKind.Vec3, new FieldHints(Inline: true));

        Assert.Equal(3, EditorDrawers.For(stacked).Lines(stacked));
        Assert.Equal(1, EditorDrawers.For(across).Lines(across));
    }

    [Fact]
    public void ARotationIsDrawnAsThreeAngles()
    {
        var field = Field("Rotation", FieldKind.Quat);

        Assert.IsType<AngleDrawer>(EditorDrawers.For(field));
        Assert.Equal(3, EditorDrawers.For(field).Lines(field));
    }

    [Fact]
    public void FlagsAskForOneRowPerName()
    {
        var field = Field(
            "Parts",
            FieldKind.Flags,
            options: ["Head", "Body", "Tail"]);

        var drawer = EditorDrawers.For(field);

        Assert.IsType<FlagsDrawer>(drawer);

        // One for the field's own name, and one for each of the names under it.
        Assert.Equal(4, drawer.Lines(field));
    }

    [Fact]
    public void AnythingElseIsDrawnAsText()
    {
        Assert.IsType<TextDrawer>(EditorDrawers.For(Field("Mystery", FieldKind.Opaque)));
    }

    [Fact]
    public void TheLastDrawerAddedWins()
    {
        var mine = new TakesEverything();
        EditorDrawers.Add(mine);

        try
        {
            Assert.Same(mine, EditorDrawers.For(Field("Speed", FieldKind.Float)));
        }
        finally
        {
            EditorDrawers.Remove(mine);
        }
    }

    [Fact]
    public void ALabelReplacesTheFieldsOwnName()
    {
        var field = Field("Speed", FieldKind.Float, new FieldHints(Label: "How fast"));

        Assert.Equal("How fast", field.Title);
        Assert.Equal("Speed", Field("Speed", FieldKind.Float).Title);
    }

    [Fact]
    public void ReadOnlyIsAskedOfTheFieldRatherThanTheDrawer()
    {
        var field = Field("Ticks", FieldKind.Int, new FieldHints(ReadOnly: true), writable: true);

        Assert.False(field.IsWritable);
    }

    /// <summary>A drawer that takes anything, for the precedence test.</summary>
    private sealed class TakesEverything : IFieldDrawer
    {
        /// <inheritdoc/>
        public bool Handles(ComponentField field) => true;

        /// <inheritdoc/>
        public void Draw(InspectorRow row, int part, FieldTarget target)
        {
        }

        /// <inheritdoc/>
        public void Read(InspectorRow row, int part, FieldTarget target)
        {
        }
    }

    /// <summary>A field with nothing behind it, which is all a drawer needs to be chosen.</summary>
    private static ComponentField Field(
        string name,
        FieldKind kind,
        FieldHints? hints = null,
        IReadOnlyList<string>? options = null,
        bool writable = true) =>
        new(
            name,
            kind,
            kind.ToString(),
            static (_, _) => null,
            writable ? static (_, _, _) => true : null,
            options,
            hints);
}

/// <summary>
/// How an inspector's lines are worked out for a real entity.
/// </summary>
/// <remarks>
/// The pipeline reads a live world: which components an entity carries, what each field's
/// attributes asked for, and whether a condition holds this frame. So it is tested against a
/// running engine rather than against a hand-built schema.
/// </remarks>
[Collection("engine")]
public sealed class InspectorPlanTests
{
    [Fact]
    public void AComponentBecomesAHeadingAndItsFields()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, default(Hinted));

            var lines = Build(ctx.Ecs, entity);

            Assert.Contains(
                lines,
                line => line.Kind == InspectorLineKind.Heading && line.Schema?.Name == "Hinted");

            // The label the attribute asked for, rather than the field's own name.
            Assert.Contains(
                lines,
                line => line.Field?.Title == "How fast");
        });

        harness.Run();
    }

    [Fact]
    public void AHiddenFieldIsLeftOut()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, default(Hinted));

            var lines = Build(ctx.Ecs, entity);

            Assert.DoesNotContain(lines, line => line.Field?.Name == "Working");
        });

        harness.Run();
    }

    [Fact]
    public void AHeadingAndAGapComeBeforeTheirField()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, default(Hinted));

            var lines = Build(ctx.Ecs, entity);
            var note = lines.FindIndex(line => line.Text == "Looks");
            var tint = lines.FindIndex(line => line.Field?.Name == "Tint");

            Assert.True(note >= 0, "the heading is drawn");
            Assert.True(note < tint, "the heading comes first");
            Assert.Equal(InspectorLineKind.Gap, lines[note - 1].Kind);
        });

        harness.Run();
    }

    [Fact]
    public void AConditionDecidesWhetherAFieldIsThere()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();

            // The condition is reversed, so the row is there while the flag is off.
            ctx.Ecs.Add(entity, new Hinted { Enabled = false });
            Assert.Contains(Build(ctx.Ecs, entity), line => line.Field?.Name == "Fallback");

            ctx.Ecs.Set(entity, new Hinted { Enabled = true });
            Assert.DoesNotContain(Build(ctx.Ecs, entity), line => line.Field?.Name == "Fallback");
        });

        harness.Run();
    }

    [Fact]
    public void AHiddenMethodIsNotOffered()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, default(Hinted));

            var lines = Build(ctx.Ecs, entity);

            Assert.Contains(lines, line => line.Method?.Title == "Do the thing");
            Assert.DoesNotContain(lines, line => line.Method?.Name == "Internal");
        });

        harness.Run();
    }

    [Fact]
    public void APassCanTakeOverAField()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, default(Hinted));

            var taken = 0;

            bool Mine(InspectorPlan plan, ComponentField field)
            {
                if (field.Name != "Speed") return false;

                taken++;
                plan.Note("taken");
                return true;
            }

            EditorInspector.OnField(Mine, priority: 10);

            try
            {
                var lines = Build(ctx.Ecs, entity);

                Assert.Equal(1, taken);
                Assert.Contains(lines, line => line.Text == "taken");
                Assert.DoesNotContain(lines, line => line.Field?.Name == "Speed");
            }
            finally
            {
                EditorInspector.RemoveField(Mine);
            }
        });

        harness.Run();
    }

    [Fact]
    public void AFoldHoldsWhatFollowsItAndOpensAndShuts()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, default(Arranged));

            var lines = Build(ctx.Ecs, entity);

            // The fold's own heading, once, before the fields inside it.
            var fold = lines.FindIndex(line =>
                line.Kind == InspectorLineKind.Group && line.Text == "Advanced");
            var radius = lines.FindIndex(line => line.Field?.Name == "Radius");

            Assert.True(fold >= 0);
            Assert.True(radius > fold);

            // A fold inside a fold is a heading one level deeper, not a second top-level one.
            var inner = Assert.Single(
                lines, line => line.Kind == InspectorLineKind.Group && line.Text == "Debug");

            Assert.Equal(1, inner.Depth);
            Assert.Equal(0, lines[fold].Depth);

            // Shut, the fold is still drawn, since that is what somebody opens it with, and
            // everything inside it is gone: the fields, and the fold inside it too.
            var shut = Build(ctx.Ecs, entity, folds: new HashSet<string> { lines[fold].Key });

            Assert.Contains(
                shut, line => line.Kind == InspectorLineKind.Group && line.Text == "Advanced");

            Assert.DoesNotContain(shut, line => line.Field?.Name == "Radius");
            Assert.DoesNotContain(shut, line => line.Text == "Debug");

            // What is outside the fold is untouched by any of it.
            Assert.Contains(shut, line => line.Field?.Name == "Mode");
        });

        harness.Run();
    }

    [Fact]
    public void AConditionAgainstAValueDecidesTheRow()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new Arranged { Mode = ArrangedMode.Steady });

            Assert.Contains(Build(ctx.Ecs, entity), line => line.Field?.Name == "Held");

            ctx.Ecs.Set(entity, new Arranged { Mode = ArrangedMode.Wild });

            Assert.DoesNotContain(Build(ctx.Ecs, entity), line => line.Field?.Name == "Held");
        });

        harness.Run();
    }

    [Fact]
    public void EveryConditionOnAFieldHasToHold()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();

            // Hidden while the mode is Wild, and shown only while the switch is on. Neither alone
            // is enough.
            ctx.Ecs.Add(entity, new Arranged { Mode = ArrangedMode.Off, Switched = false });
            Assert.DoesNotContain(Build(ctx.Ecs, entity), line => line.Field?.Name == "Careful");

            ctx.Ecs.Set(entity, new Arranged { Mode = ArrangedMode.Wild, Switched = true });
            Assert.DoesNotContain(Build(ctx.Ecs, entity), line => line.Field?.Name == "Careful");

            ctx.Ecs.Set(entity, new Arranged { Mode = ArrangedMode.Off, Switched = true });
            Assert.Contains(Build(ctx.Ecs, entity), line => line.Field?.Name == "Careful");
        });

        harness.Run();
    }

    [Fact]
    public void ButtonsThatShareALineBecomeOneRow()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, default(Arranged));

            var lines = Build(ctx.Ecs, entity);
            var row = Assert.Single(lines, line => line.Kind == InspectorLineKind.Buttons);

            Assert.Equal(
                ["Save", "Load", "Reset"],
                row.Buttons!.Select(method => method.Title));

            // And none of the three is also a row of its own.
            Assert.DoesNotContain(
                lines,
                line => line.Kind == InspectorLineKind.Method && line.Method?.Name == "Load");
        });

        harness.Run();
    }

    [Fact]
    public void ALineIsTheSameLineFrameAfterFrame()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, default(Arranged));

            var first = Build(ctx.Ecs, entity);
            var again = Build(ctx.Ecs, entity);

            // What a panel uses to notice that a row now stands for something else. A row of
            // buttons carries a list, and a list compared by reference is a new one every frame,
            // which would make every button row look like it had just turned.
            Assert.Equal(first.Count, again.Count);

            for (var i = 0; i < first.Count; i++) Assert.Equal(first[i], again[i]);
        });

        harness.Run();
    }

    [Fact]
    public void AChangedFieldCallsWhatItSaidToCall()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, default(Arranged));

            var schema = ComponentSchemas.For("Bevy.Tests.Arranged")!;
            var radius = schema.Field("Radius")!;

            EditorFields.Change(ctx.Ecs, entity, radius, 2f);

            Assert.True(ctx.Ecs.TryGet<Arranged>(entity, out var read));
            Assert.Equal(2f, read.Radius);
            Assert.Equal(1, read.Rebuilt);

            // Taking it back counts as changing it. Something derived from a value is as wrong
            // after an undo as it was before the write.
            EditorHistory.Undo(ctx.Ecs);

            Assert.True(ctx.Ecs.TryGet<Arranged>(entity, out read));
            Assert.Equal(0f, read.Radius);
            Assert.Equal(2, read.Rebuilt);
        });

        harness.Run();
    }

    [Fact]
    public void AListIsAFoldPerElementWithButtonsToChangeIt()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var items = new List<string> { "one", "two" };
            var plan = new InspectorPlan(ctx.Ecs, Entity.None);

            InspectorList.Add(
                plan,
                "test/slots",
                "Slots",
                items.Count,
                (into, index) => into.Note(items[index]),
                add: () => items.Add("new"),
                remove: items.RemoveAt);

            // The list itself, then a fold per element with what the element drew inside it, and a
            // way to take each one away.
            var heading = Assert.Single(plan.Lines, line => line.Key == "test/slots");
            Assert.Equal("Slots (2)", heading.Text);

            Assert.Equal("Slot 1", Assert.Single(plan.Lines, l => l.Key == "test/slots/0").Text);
            Assert.Equal("Slot 2", Assert.Single(plan.Lines, l => l.Key == "test/slots/1").Text);
            Assert.Contains(plan.Lines, line => line.Text == "one");

            var buttons = plan.Lines
                .Select(line => line.Line)
                .OfType<InspectorAction>()
                .ToList();

            Assert.Equal(3, buttons.Count);
            Assert.Equal("Add Slots", buttons[^1].Button);

            // Pressing one reaches the list it was built from, which is the whole point: nothing
            // here knows what the elements are or where they live.
            buttons[^1].Pressed();
            Assert.Equal(3, items.Count);

            buttons[0].Pressed();
            Assert.Equal(["two", "new"], items);
        });

        harness.Run();
    }

    [Fact]
    public void AShutListDrawsItsHeadingAndNothingElse()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var drawn = 0;
            var plan = new InspectorPlan(ctx.Ecs, Entity.None)
            {
                Folds = new HashSet<string> { "test/slots" },
            };

            InspectorList.Add(plan, "test/slots", "Slots", 3, (_, _) => drawn++, add: () => { });

            // Nothing inside is drawn, and nothing inside is even asked to draw: a list of a
            // thousand things costs one row while it is shut.
            var line = Assert.Single(plan.Lines);

            Assert.Equal("Slots (3)", line.Text);
            Assert.Equal(0, drawn);
        });

        harness.Run();
    }

    /// <summary>Every line for an entity, with nothing folded.</summary>
    private static List<InspectorLine> Build(
        EcsWorld world, Entity entity, IReadOnlySet<string>? folds = null)
    {
        var tags = new List<(ComponentSchema? Schema, int Component)>();

        return
        [
            .. EditorInspector.Build(world, entity, new HashSet<int>(), tags, null, folds),
        ];
    }
}

/// <summary>
/// Selecting more than one thing, and what an inspector does with it.
/// </summary>
/// <remarks>
/// The selection is a list with a current one at the end of it. Everything that acts on a single
/// thing acts on that one; everything that can act on many reads the list. A list of one is the
/// ordinary case, and these check that it still behaves as one.
/// </remarks>
[Collection("engine")]
public sealed class SelectionTests
{
    [Fact]
    public void SelectingReplacesAndTogglingAdds()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var first = ctx.Ecs.Spawn();
            var second = ctx.Ecs.Spawn();

            EditorSelection.Select(first);
            Assert.Equal(first, EditorSelection.Current);
            Assert.Equal(1, EditorSelection.Count);

            EditorSelection.Toggle(second);
            Assert.Equal(second, EditorSelection.Current);
            Assert.Equal(2, EditorSelection.Count);
            Assert.True(EditorSelection.Holds(first));

            // Taking the current one out leaves the one picked before it in charge, so the handles
            // stay on something.
            EditorSelection.Toggle(second);
            Assert.Equal(first, EditorSelection.Current);
            Assert.Equal(1, EditorSelection.Count);

            EditorSelection.Select(second);
            Assert.Equal(1, EditorSelection.Count);
            Assert.False(EditorSelection.Holds(first));

            EditorSelection.Clear();
        });

        harness.Run();
    }

    [Fact]
    public void AnEditReachesEverythingSelected()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var first = ctx.Ecs.Spawn();
            var second = ctx.Ecs.Spawn();

            ctx.Ecs.Add(first, new Hinted { Speed = 1f });
            ctx.Ecs.Add(second, new Hinted { Speed = 2f });

            var schema = ComponentSchemas.For("Bevy.Tests.Hinted");
            var speed = Assert.Single(schema!.Fields, field => field.Name == "Speed");
            var target = new FieldTarget(speed, ctx.Ecs, first, [first, second]);

            Assert.False(target.Agree());

            target.Write(9f);

            Assert.True(ctx.Ecs.TryGet<Hinted>(first, out var one));
            Assert.True(ctx.Ecs.TryGet<Hinted>(second, out var two));
            Assert.Equal(9f, one.Speed);
            Assert.Equal(9f, two.Speed);
            Assert.True(target.Agree());
        });

        harness.Run();
    }

    [Fact]
    public void OnlyWhatEverythingCarriesIsShown()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var both = ctx.Ecs.Spawn();
            var one = ctx.Ecs.Spawn();

            ctx.Ecs.Add(both, default(Hinted));
            ctx.Ecs.Add(both, new Described { Speed = 1f });
            ctx.Ecs.Add(one, default(Hinted));

            var tags = new List<(ComponentSchema? Schema, int Component)>();
            var lines = EditorInspector.Build(
                ctx.Ecs, both, new HashSet<int>(), tags, [both, one]);

            Assert.Contains(lines, line => line.Schema?.Name == "Hinted");
            Assert.DoesNotContain(lines, line => line.Schema?.Name == "Described");
        });

        harness.Run();
    }
}
