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

    /// <summary>Not drawn at all: nothing knows what to do with it.</summary>
    public Mystery Unknown;

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
        Assert.Equal(
            ["Enabled", "Count", "Speed", "Offset", "When", "Unknown", "Private", "Doubled"],
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

        // Nothing knows how to edit it, so it is a row with a type and no editor rather than a
        // guess at what its bytes mean.
        Assert.Equal(FieldKind.Opaque, schema.Field("Unknown")!.Kind);
        Assert.Equal("Mystery", schema.Field("Unknown")!.Type);
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
    [Colour]
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
        Assert.True(tint.Hints.Colour);
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
    public void AConditionAndAStepAreCarried()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Hinted");
        var fallback = Assert.Single(schema!.Fields, field => field.Name == "Fallback");

        Assert.Equal("Enabled", fallback.Hints.ShowIf);
        Assert.True(fallback.Hints.ShowIfNot);
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
