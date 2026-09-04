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

    /// <summary>Keeps the private field from reading as unused.</summary>
    public readonly int Private => _private;
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
        Assert.Equal(
            ["Enabled", "Count", "Speed", "Offset", "When", "Unknown"],
            schema.Fields.Select(field => field.Name));
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
