using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A component describing every field kind a tool has an editor for.</summary>
/// <remarks>
/// Fields only, and no methods at all, which is the case that proves a plain data component is
/// described as readily as one that runs systems.
/// </remarks>
[Behavior]
public partial struct Described
{
    /// <summary>Drawn as a checkbox.</summary>
    public bool Enabled;

    /// <summary>Drawn as a whole number.</summary>
    public int Count;

    /// <summary>Drawn as a number.</summary>
    public float Speed;

    /// <summary>Drawn as three numbers.</summary>
    public Vec3 Offset;

    /// <summary>Drawn as a list of names.</summary>
    public Season When;

    /// <summary>Taken apart, since what it holds is drawable even though it is not.</summary>
    public Mystery Unknown;

    /// <summary>Not drawn at all: there is nothing inside it to draw either.</summary>
    public Sealed Shut;

    /// <summary>Left out, because a tool has no business with a behavior's own state.</summary>
#pragma warning disable CS0649 // Never assigned: only its absence from the schema is under test.
    private int _private;
#pragma warning restore CS0649

    /// <summary>Left out, because it belongs to the type rather than to an entity.</summary>
    public static int Shared;

    /// <summary>Keeps the private field from reading as unused, and is shown as a read only row.</summary>
    public readonly int Private => _private;

    /// <summary>Shown, and written through itself: what it is given is doubled on the way in.</summary>
    public float Doubled
    {
        readonly get => Speed;
        set => Speed = value * 2f;
    }
}

/// <summary>An enum, so a field of it has a fixed set of names.</summary>
public enum Season
{
    /// <summary>The first.</summary>
    Spring,

    /// <summary>The second.</summary>
    Summer,

    /// <summary>The third.</summary>
    Autumn,

    /// <summary>The fourth.</summary>
    Winter,
}

/// <summary>A blittable struct that is not one of the kinds a tool can edit.</summary>
public struct Mystery
{
    /// <summary>Anything at all.</summary>
    public long Bits;
}

/// <summary>A value that keeps what it holds to itself.</summary>
public struct Sealed
{
#pragma warning disable CS0169 // Never used: its absence from the schema is the point.
    private readonly long _bits;
#pragma warning restore CS0169
}

/// <summary>
/// Covers the field tables the generator emits, which is what turns a component id into rows.
/// </summary>
[Collection("engine")]
public sealed class ComponentSchemaTests
{
    [Fact]
    public void AComponentDeclaresItsFieldsInOrder()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Described");

        Assert.NotNull(schema);
        Assert.Equal("Described", schema.Name);

        // The property comes last because it is declared last. A property is described alongside
        // the fields and read through itself, so what a tool shows is what the type says rather
        // than what is stored behind it.
        // A struct with fields of its own is taken apart, and the path it came from is its name.
        // A struct with nothing reachable inside it stays one row, because there is nothing better
        // to say about it than what it is.
        Assert.Equal(
            [
                "Enabled", "Count", "Speed", "Offset", "When", "Unknown.Bits", "Shut", "Private",
                "Doubled",
            ],
            schema.Fields.Select(field => field.Name));
    }

    [Fact]
    public void APropertyWithNoSetterIsDescribedAsReadOnly()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Described");
        var read = Assert.Single(schema!.Fields, field => field.Name == "Private");

        Assert.Equal(FieldKind.Int, read.Kind);
        Assert.True(read.Hints.ReadOnly);
        Assert.False(read.IsWritable);
    }

    [Fact]
    public void EachFieldSaysHowItShouldBeDrawn()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Described")!;

        Assert.Equal(FieldKind.Bool, schema.Field("Enabled")!.Kind);
        Assert.Equal(FieldKind.Int, schema.Field("Count")!.Kind);
        Assert.Equal(FieldKind.Float, schema.Field("Speed")!.Kind);
        Assert.Equal(FieldKind.Vec3, schema.Field("Offset")!.Kind);
        Assert.Equal(FieldKind.Enum, schema.Field("When")!.Kind);

        // Nothing knows how to edit the struct itself, so what is drawn is what it holds. Its own
        // row would have been a type and no editor, which helps nobody.
        Assert.Equal(FieldKind.Int, schema.Field("Unknown.Bits")!.Kind);
        Assert.Equal("Unknown", schema.Field("Unknown.Bits")!.Hints.Foldout);

        // One whose fields cannot be reached is a row with a type and no editor rather than a
        // guess at what its bytes mean.
        Assert.Equal(FieldKind.Opaque, schema.Field("Shut")!.Kind);
        Assert.Equal("Sealed", schema.Field("Shut")!.Type);
    }

    [Fact]
    public void AnEnumFieldCarriesTheNamesItCanTake()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Described")!;

        Assert.Equal(
            ["Spring", "Summer", "Autumn", "Winter"],
            schema.Field("When")!.Options);
    }

    [Fact]
    public void AFieldIsReadByNameFromALiveEntity()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new Described
            {
                Enabled = true,
                Count = 7,
                Speed = 2.5f,
                Offset = new Vec3(1f, 2f, 3f),
                When = Season.Autumn,
            });

            var schema = ComponentSchemas.For("Bevy.Tests.Described")!;

            Assert.Equal(true, schema.Read(ctx.Ecs, entity, "Enabled"));
            Assert.Equal(7, schema.Read(ctx.Ecs, entity, "Count"));
            Assert.Equal(2.5f, schema.Read(ctx.Ecs, entity, "Speed"));
            Assert.Equal(new Vec3(1f, 2f, 3f), schema.Read(ctx.Ecs, entity, "Offset"));
            Assert.Equal(Season.Autumn, schema.Read(ctx.Ecs, entity, "When"));
        });

        harness.Run();
    }

    [Fact]
    public void AFieldIsWrittenByNameAndLandsInTheWorld()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, default(Described));

            var schema = ComponentSchemas.For("Bevy.Tests.Described")!;

            Assert.True(schema.Write(ctx.Ecs, entity, "Count", 12));
            Assert.True(schema.Write(ctx.Ecs, entity, "Enabled", true));

            // A tool holds whatever the control it drew produced, so a float field is handed a
            // double and an enum field an index, and both have to land.
            Assert.True(schema.Write(ctx.Ecs, entity, "Speed", 4.5d));
            Assert.True(schema.Write(ctx.Ecs, entity, "When", 2));

            var written = ctx.Ecs.GetOrDefault<Described>(entity);
            Assert.Equal(12, written.Count);
            Assert.True(written.Enabled);
            Assert.Equal(4.5f, written.Speed);
            Assert.Equal(Season.Autumn, written.When);
        });

        harness.Run();
    }

    [Fact]
    public void AValueThatDoesNotFitIsRefusedRatherThanThrown()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new Described { Count = 3 });

            var schema = ComponentSchemas.For("Bevy.Tests.Described")!;

            // Half-typed input, which is what a text field hands over between keystrokes.
            Assert.False(schema.Write(ctx.Ecs, entity, "Count", "-"));
            Assert.Equal(3, ctx.Ecs.GetOrDefault<Described>(entity).Count);

            // A field nobody declared.
            Assert.False(schema.Write(ctx.Ecs, entity, "Nonexistent", 1));
            Assert.Null(schema.Read(ctx.Ecs, entity, "Nonexistent"));
        });

        harness.Run();
    }

    [Fact]
    public void AnEntityWithoutTheComponentReadsAsNothing()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var bare = ctx.Ecs.Spawn();
            var schema = ComponentSchemas.For("Bevy.Tests.Described")!;

            Assert.Null(schema.Read(ctx.Ecs, bare, "Count"));
            Assert.False(schema.Write(ctx.Ecs, bare, "Count", 1));
        });

        harness.Run();
    }

    [Fact]
    public void AComponentIdFoundInTheWorldFindsItsSchema()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new Described { Count = 9 });
            ctx.Ecs.Add(entity, Transform.At(1f, 2f, 3f));

            // The whole round trip an inspector makes: ask what the entity carries, then ask
            // what each of those is, without naming a single type.
            var described = new List<string>();
            foreach (var id in ctx.Ecs.ComponentsOf(entity))
            {
                if (ComponentSchemas.For(id) is not { } schema) continue;
                described.Add(schema.Name);
            }

            Assert.Contains("Described", described);
            Assert.Contains("Transform", described);
        });

        harness.Run();
    }

    [Fact]
    public void BevysOwnTransformIsDescribedByHand()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, Transform.At(1f, 2f, 3f));

            var schema = ComponentSchemas.For("Bevy.Transform")!;

            Assert.Equal(new Vec3(1f, 2f, 3f), schema.Read(ctx.Ecs, entity, "Translation"));
            Assert.True(schema.Write(ctx.Ecs, entity, "Translation", new Vec3(4f, 5f, 6f)));
            Assert.Equal(new Vec3(4f, 5f, 6f), ctx.Ecs.GetOrDefault<Transform>(entity).Translation);
        });

        harness.Run();
    }
}

/// <summary>A component whose fields say how they want to be drawn.</summary>
/// <remarks>
/// Every attribute the generator reads, on one struct, so that the emitted hints are checked
/// against what was written rather than against what the emitter happens to do today.
/// </remarks>
[Behavior]
public partial struct Hinted
{
    /// <summary>A slider between two ends, with a name of its own.</summary>
    [Range(0d, 10d)]
    [Label("How fast")]
    [Tooltip("Metres a second.")]
    [Unit("m/s")]
    public float Speed;

    /// <summary>Under a heading, and after a gap.</summary>
    [Header("Looks")]
    [Space]
    [Color]
    public Vec3 Tint;

    /// <summary>Shown, and not editable.</summary>
    [ReadOnly]
    public int Counted;

    /// <summary>Not shown at all.</summary>
    [Hidden]
    public int Working;

    /// <summary>Shown only while <see cref="Enabled"/> is off.</summary>
    [ShowIf(nameof(Enabled), Not = true)]
    [Step(0.5d)]
    [Order(3)]
    public float Fallback;

    /// <summary>What the one above answers to.</summary>
    public bool Enabled;

    /// <summary>A button with words of its own.</summary>
    [Button("Do the thing")]
    [Tooltip("Runs it once.")]
    public void Run()
    {
    }

    /// <summary>A method nothing offers.</summary>
    [Hidden]
    public void Internal()
    {
    }
}

/// <summary>
/// A component using the attributes that arrange an inspector rather than describe a value.
/// </summary>
/// <remarks>
/// Separate from <see cref="Hinted"/> so that each fixture reads as one thing: what a value is, and
/// where it goes on the screen.
/// </remarks>
[Behavior]
public partial struct Arranged
{
    /// <summary>Which of the three ways this thing works.</summary>
    public ArrangedMode Mode;

    /// <summary>Shown only while the mode is the second one.</summary>
    [ShowIf(nameof(Mode), ArrangedMode.Steady)]
    public float Held;

    /// <summary>Shown unless the mode is the third, and only while it is switched on.</summary>
    [HideIf(nameof(Mode), ArrangedMode.Wild)]
    [ShowIf(nameof(Switched))]
    public float Careful;

    /// <summary>What the one above answers to.</summary>
    public bool Switched;

    /// <summary>Inside a fold, with a line and a warning above it.</summary>
    [Foldout("Advanced")]
    [Separator]
    [Info("Changing this rebuilds the thing.", Kind = NoteKind.Warning)]
    [OnValueChanged(nameof(Rebuild))]
    public float Radius;

    /// <summary>Inside a fold inside that one, which starts shut.</summary>
    [Foldout("Advanced/Debug", Open = false)]
    public bool Noisy;

    /// <summary>Drawn across the panel with no name beside it.</summary>
    [Wide]
    public int Note;

    /// <summary>A bar with the number beside it, read only.</summary>
    [Range(0d, 1d, Readout = SliderReadout.Number)]
    public float Weight;

    /// <summary>Three numbers on one line.</summary>
    [Inline]
    public Vec3 Corner;

    /// <summary>The first of three buttons on one line.</summary>
    [Button("Save", Line = ButtonLine.Start, Weight = 2d)]
    public void Save()
    {
    }

    /// <summary>The second.</summary>
    [Button("Load", Line = ButtonLine.Middle)]
    public void Load()
    {
    }

    /// <summary>The last, after which the line is closed.</summary>
    [Button("Reset", Line = ButtonLine.End)]
    public void Wipe()
    {
    }

    /// <summary>What a change to the radius calls.</summary>
    [Hidden]
    public void Rebuild() => Rebuilt++;

    /// <summary>How many times that has happened.</summary>
    [Hidden]
    public int Rebuilt;
}

/// <summary>A value of its own, held by a component that has one.</summary>
public struct Spring
{
    /// <summary>How hard it pulls.</summary>
    [Unit("N/m")]
    public float Stiffness;

    /// <summary>How quickly it settles.</summary>
    public float Damping;

    /// <summary>Where it is anchored, which is a struct inside a struct.</summary>
    public Anchor Held;
}

/// <summary>A value inside a value, so nesting past one level is covered.</summary>
public struct Anchor
{
    /// <summary>Whether it is anchored at all.</summary>
    public bool Fixed;

    /// <summary>Where.</summary>
    public Vec3 At;
}

/// <summary>A component whose field is a struct with fields of its own.</summary>
[Behavior]
public partial struct Sprung
{
    /// <summary>An ordinary field, so the two kinds sit side by side.</summary>
    public float Mass;

    /// <summary>A value of its own, which is taken apart into a fold.</summary>
    public Spring Front;
}

/// <summary>The three ways <see cref="Arranged"/> can work.</summary>
public enum ArrangedMode
{
    /// <summary>The first.</summary>
    Off,

    /// <summary>The second.</summary>
    Steady,

    /// <summary>The third.</summary>
    Wild,
}

/// <summary>Covers the hints a field's attributes leave on the schema.</summary>
[Collection("engine")]
public sealed class FieldHintTests
{
    [Fact]
    public void AttributesReachTheSchema()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Hinted");
        Assert.NotNull(schema);

        var speed = Assert.Single(schema.Fields, field => field.Name == "Speed");

        Assert.Equal("How fast", speed.Title);
        Assert.Equal("Metres a second.", speed.Hints.Tooltip);
        Assert.Equal("m/s", speed.Hints.Unit);
        Assert.True(speed.Hints.HasRange);
        Assert.Equal(0d, speed.Hints.Minimum);
        Assert.Equal(10d, speed.Hints.Maximum);
    }

    [Fact]
    public void AHeadingAndAGapAreCarried()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Hinted");
        var tint = Assert.Single(schema!.Fields, field => field.Name == "Tint");

        Assert.Equal("Looks", tint.Hints.Header);
        Assert.True(tint.Hints.Space);
        Assert.True(tint.Hints.Color);
    }

    [Fact]
    public void ReadOnlyMakesAFieldUnwritable()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Hinted");
        var counted = Assert.Single(schema!.Fields, field => field.Name == "Counted");

        Assert.True(counted.Hints.ReadOnly);
        Assert.False(counted.IsWritable);
    }

    [Fact]
    public void HiddenIsCarriedRatherThanDropped()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Hinted");
        var working = Assert.Single(schema!.Fields, field => field.Name == "Working");

        // Left in the table and marked, rather than left out of it: a tool decides what to show,
        // and something that saves a component still needs every field.
        Assert.True(working.Hints.Hidden);
    }

    [Fact]
    public void AStructInsideAComponentIsTakenApart()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Sprung");
        Assert.NotNull(schema);

        // The path is the name, so nothing collides, and what it is called on screen is the last
        // part of it. The rest of the path is said by the fold it sits in.
        var stiffness = Assert.Single(schema.Fields, field => field.Name == "Front.Stiffness");

        Assert.Equal("Stiffness", stiffness.Title);
        Assert.Equal("Front", stiffness.Hints.Foldout);
        Assert.Equal("N/m", stiffness.Hints.Unit);
        Assert.Equal(FieldKind.Float, stiffness.Kind);

        // Two levels down, in a fold inside a fold.
        var at = Assert.Single(schema.Fields, field => field.Name == "Front.Held.At");

        Assert.Equal("At", at.Title);
        Assert.Equal("Front/Held", at.Hints.Foldout);
        Assert.Equal(FieldKind.Vec3, at.Kind);

        // The struct itself is not also a row: it has been replaced by its parts, not annotated
        // with them.
        Assert.DoesNotContain(schema.Fields, field => field.Name == "Front");

        // A vector is left alone. It is three numbers a tool already draws as one thing, and
        // taking it apart would say the same thing worse.
        Assert.DoesNotContain(schema.Fields, field => field.Name.StartsWith("Front.Held.At."));
    }

    [Fact]
    public void APartOfAStructIsWrittenThroughTheWholeThing()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new Sprung { Mass = 2f, Front = new Spring { Stiffness = 5f } });

            var schema = ComponentSchemas.For("Bevy.Tests.Sprung")!;
            var damping = Assert.Single(schema.Fields, field => field.Name == "Front.Damping");

            Assert.True(damping.Write(ctx.Ecs, entity, 0.25f));

            // The rest of the component survives the write. A part written back through a copy of
            // the whole is the only way to write one, and getting it wrong wipes its neighbours.
            Assert.True(ctx.Ecs.TryGet<Sprung>(entity, out var read));
            Assert.Equal(0.25f, read.Front.Damping);
            Assert.Equal(5f, read.Front.Stiffness);
            Assert.Equal(2f, read.Mass);
        });

        harness.Run();
    }

    [Fact]
    public void AConditionComparesAgainstAValue()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Arranged");
        var held = Assert.Single(schema!.Fields, field => field.Name == "Held");
        var condition = Assert.Single(held.Hints.Conditions);

        // The name rather than the number behind it: what the field reads as at runtime is the
        // name, and a condition written against one has to be checked against one.
        Assert.Equal("Mode", condition.Field);
        Assert.Equal("Steady", condition.Value);
        Assert.False(condition.Not);
    }

    [Fact]
    public void SeveralConditionsAreAllKept()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Arranged");
        var careful = Assert.Single(schema!.Fields, field => field.Name == "Careful");

        Assert.Equal(2, careful.Hints.Conditions.Count);

        var hide = careful.Hints.Conditions[0];
        Assert.Equal("Mode", hide.Field);
        Assert.Equal("Wild", hide.Value);
        Assert.True(hide.Not);

        var show = careful.Hints.Conditions[1];
        Assert.Equal("Switched", show.Field);
        Assert.Null(show.Value);
        Assert.False(show.Not);
    }

    [Fact]
    public void FoldsSeparatorsAndNotesAreCarried()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Arranged");
        var radius = Assert.Single(schema!.Fields, field => field.Name == "Radius");

        Assert.Equal("Advanced", radius.Hints.Foldout);
        Assert.True(radius.Hints.Separator);
        Assert.Equal("Changing this rebuilds the thing.", radius.Hints.Note);
        Assert.Equal(NoteKind.Warning, radius.Hints.NoteKind);
        Assert.Equal("Rebuild", Assert.Single(radius.Hints.Changed));

        var noisy = Assert.Single(schema.Fields, field => field.Name == "Noisy");

        Assert.Equal("Advanced/Debug", noisy.Hints.Foldout);
        Assert.False(noisy.Hints.FoldoutOpen);
    }

    [Fact]
    public void WidthAndReadoutAreCarried()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Arranged");

        Assert.True(Assert.Single(schema!.Fields, field => field.Name == "Note").Hints.Wide);
        Assert.True(Assert.Single(schema.Fields, field => field.Name == "Corner").Hints.Inline);

        var weight = Assert.Single(schema.Fields, field => field.Name == "Weight");
        Assert.Equal(SliderReadout.Number, weight.Hints.Readout);
    }

    [Fact]
    public void ButtonsSayWhereOnTheLineTheySit()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Arranged");

        var save = Assert.Single(schema!.Methods, method => method.Name == "Save");
        Assert.Equal(ButtonLine.Start, save.Hints.Line);
        Assert.Equal(2d, save.Hints.Weight);
        Assert.Equal("Save", save.Title);

        Assert.Equal(
            ButtonLine.Middle,
            Assert.Single(schema.Methods, method => method.Name == "Load").Hints.Line);

        Assert.Equal(
            ButtonLine.End,
            Assert.Single(schema.Methods, method => method.Name == "Wipe").Hints.Line);
    }

    [Fact]
    public void AFieldKnowsWhichComponentItBelongsTo()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Arranged");
        var radius = Assert.Single(schema!.Fields, field => field.Name == "Radius");

        // What lets a write find the methods to call afterwards, and a condition find the field it
        // names, without anything having to carry the schema alongside the field.
        Assert.Same(schema, radius.Schema);
        Assert.NotNull(schema.Method("Rebuild"));
    }

    [Fact]
    public void AConditionAndAStepAreCarried()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Hinted");
        var fallback = Assert.Single(schema!.Fields, field => field.Name == "Fallback");

        var condition = Assert.Single(fallback.Hints.Conditions);

        Assert.Equal("Enabled", condition.Field);
        Assert.True(condition.Not);
        Assert.Null(condition.Value);
        Assert.Equal(0.5d, fallback.Hints.Step);
        Assert.Equal(3, fallback.Hints.Order);
    }

    [Fact]
    public void AMethodCarriesItsButtonAndItsTooltip()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Hinted");
        var run = Assert.Single(schema!.Methods, method => method.Name == "Run");

        Assert.Equal("Do the thing", run.Title);
        Assert.Equal("Runs it once.", run.Hints.Tooltip);

        var hidden = Assert.Single(schema.Methods, method => method.Name == "Internal");
        Assert.True(hidden.Hints.Hidden);
    }
}

/// <summary>Covers a property being read and written through itself.</summary>
[Collection("engine")]
public sealed class PropertyFieldTests
{
    [Fact]
    public void APropertyIsWrittenThroughItself()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new Described { Speed = 1f });

            var schema = ComponentSchemas.For("Bevy.Tests.Described");
            var doubled = Assert.Single(schema!.Fields, field => field.Name == "Doubled");

            Assert.True(doubled.IsWritable);
            Assert.True(doubled.Write(ctx.Ecs, entity, 4f));

            // The property doubled it on the way in, which is the whole point of describing the
            // property rather than the field behind it.
            Assert.True(ctx.Ecs.TryGet<Described>(entity, out var after));
            Assert.Equal(8f, after.Speed);
            Assert.Equal(8f, doubled.Read(ctx.Ecs, entity));
        });

        harness.Run();
    }
}
