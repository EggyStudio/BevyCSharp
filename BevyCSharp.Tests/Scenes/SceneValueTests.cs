using System.Buffers;
using System.Text;
using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A choice of one, for the enum writer.</summary>
public enum Mood
{
    /// <summary>The first.</summary>
    Calm,

    /// <summary>The second.</summary>
    Restless,
}

/// <summary>A set of bits, for the flags writer.</summary>
[Flags]
public enum Marks
{
    /// <summary>None of them.</summary>
    None = 0,

    /// <summary>One.</summary>
    Hot = 1,

    /// <summary>Another.</summary>
    Wet = 2,

    /// <summary>A third.</summary>
    Loud = 4,
}

/// <summary>A component with a field of every kind a scene writes.</summary>
[Behavior]
public partial struct EveryKind
{
    /// <summary>A flag.</summary>
    public bool On;

    /// <summary>A whole number.</summary>
    public int Count;

    /// <summary>A number.</summary>
    public float Speed;

    /// <summary>A wider number.</summary>
    public double Mass;

    /// <summary>Two numbers.</summary>
    public Vec2 Offset;

    /// <summary>Three numbers.</summary>
    public Vec3 At;

    /// <summary>Four numbers.</summary>
    public Vec4 Weights;

    /// <summary>A rotation.</summary>
    public Quat Turn;

    /// <summary>A color.</summary>
    public Color Tint;

    /// <summary>One of a set.</summary>
    public Mood Mood;

    /// <summary>Several of a set.</summary>
    public Marks Marks;

    /// <summary>Another entity.</summary>
    public Entity Target;
}

/// <summary>A component holding a reference of each kind a scene names, an entity, an asset and a data asset.</summary>
[Behavior]
public partial struct EveryReference
{
    /// <summary>Another entity.</summary>
    public Entity Target;

    /// <summary>A picture.</summary>
    [Asset("image")]
    public AssetHandle Picture;

    /// <summary>A data asset.</summary>
    public DataRef<WeaponStats> Weapon;
}

/// <summary>A component holding whole numbers at the ends of a ulong's range and a long's, as a seed or a hash is.</summary>
[Behavior]
public partial struct WideNumbers
{
    /// <summary>A seed past what a long holds.</summary>
    public ulong Seed;

    /// <summary>The least a long holds.</summary>
    public long Least;

    /// <summary>Counts by a hash, which a scene writes as an object.</summary>
    public EcsMap<ulong, int> ByHash;
}

/// <summary>
/// Covers writing a component's values to JSON and reading them back.
/// </summary>
/// <remarks>
/// Every kind goes out and comes back through a fresh entity, compared field by field, because a
/// writer and a reader that agree with each other are the whole contract a scene file needs.
/// </remarks>
[Collection("engine")]
public sealed class SceneValueTests
{
    [Fact]
    public void TheNewKindsAreGivenToTheTypesThatHoldThem()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.EveryKind")!;

        Assert.Equal(FieldKind.Vec2, schema.Field("Offset")!.Kind);
        Assert.Equal(FieldKind.Vec4, schema.Field("Weights")!.Kind);
        Assert.Equal(FieldKind.Color, schema.Field("Tint")!.Kind);
        Assert.Equal(FieldKind.Flags, schema.Field("Marks")!.Kind);
    }

    [Fact]
    public void EveryKindIsWrittenAndReadBack()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var target = ctx.Ecs.Spawn();
            var original = new EveryKind
            {
                On = true,
                Count = -42,
                Speed = 1.5f,
                Mass = 2.25,
                Offset = new Vec2(1f, -2f),
                At = new Vec3(3f, 4f, 5f),
                Weights = new Vec4(0.1f, 0.2f, 0.3f, 0.4f),
                Turn = Quat.FromEuler(0.1f, 0.2f, 0.3f),
                Tint = Color.FromHex("#ff8800"),
                Mood = Mood.Restless,
                Marks = Marks.Hot | Marks.Loud,
                Target = target,
            };

            var source = ctx.Ecs.Spawn();
            ctx.Ecs.Add(source, original);

            var schema = ComponentSchemas.For("Bevy.Tests.EveryKind")!;
            var references = new SceneReferences();
            references.Name(target, 7);

            var text = Written(json => SceneValue.WriteComponent(json, schema, ctx.Ecs, source, references));

            // Read by eye as well as by code, names rather than numbers, arrays for vectors.
            Assert.Contains("\"Mood\":\"Restless\"", text);
            Assert.Contains("\"Marks\":[\"Hot\",\"Loud\"]", text);
            Assert.Contains("\"Target\":{\"entity\":7}", text);
            Assert.Contains("\"Offset\":[1,-2]", text);

            // Into a fresh entity, with the referenced one spawned again under the same id.
            var copy = ctx.Ecs.Spawn();
            ctx.Ecs.Add(copy, default(EveryKind));
            var respawned = ctx.Ecs.Spawn();
            var reading = new SceneReferences();
            reading.Spawned(7, respawned);

            using var document = JsonDocument.Parse(text);
            var read = SceneValue.ReadComponent(document.RootElement, schema, ctx.Ecs, copy, reading);
            Assert.Equal(schema.Fields.Count(field => !field.Derived), read);

            var back = ctx.Ecs.GetOrDefault<EveryKind>(copy);
            Assert.Equal(original with { Target = respawned }, back);
        });

        harness.Run();
    }

    [Fact]
    public void ANestedStructIsWrittenAsAnObject()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var source = ctx.Ecs.Spawn();
            ctx.Ecs.Add(source, new Sprung
            {
                Mass = 2f,
                Front = new Spring { Stiffness = 5f, Damping = 0.5f, Held = new Anchor { Fixed = true, At = new Vec3(1f, 2f, 3f) } },
            });

            var schema = ComponentSchemas.For("Bevy.Tests.Sprung")!;
            var text = Written(json => SceneValue.WriteComponent(json, schema, ctx.Ecs, source));

            // The shape of the type, not the inspector's flattened rows.
            using var document = JsonDocument.Parse(text);
            var front = document.RootElement.GetProperty("Front");
            Assert.Equal(5f, front.GetProperty("Stiffness").GetSingle());
            Assert.True(front.GetProperty("Held").GetProperty("Fixed").GetBoolean());

            var copy = ctx.Ecs.Spawn();
            ctx.Ecs.Add(copy, default(Sprung));
            SceneValue.ReadComponent(document.RootElement, schema, ctx.Ecs, copy);

            var back = ctx.Ecs.GetOrDefault<Sprung>(copy);
            Assert.Equal(new Vec3(1f, 2f, 3f), back.Front.Held.At);
            Assert.Equal(0.5f, back.Front.Damping);
            Assert.Equal(2f, back.Mass);
        });

        harness.Run();
    }

    [Fact]
    public void AReferenceTheSceneCannotFollowIsWrittenAsNothing()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            // An entity the writer did not number would point at nothing after a reload, so it is
            // written as null rather than as a handle valid for one run.
            var source = ctx.Ecs.Spawn();
            ctx.Ecs.Add(source, new EveryKind { Target = ctx.Ecs.Spawn() });

            var schema = ComponentSchemas.For("Bevy.Tests.EveryKind")!;
            var text = Written(json => SceneValue.WriteComponent(json, schema, ctx.Ecs, source));

            Assert.Contains("\"Target\":null", text);
        });

        harness.Run();
    }

    /// <summary>
    /// A reference cleared to none, of an entity, an asset or a data asset, is written as none and
    /// read back as none over a component holding one, where a null is passed over and would leave
    /// what the component held.
    /// </summary>
    [Fact]
    public void AReferenceClearedToNoneIsWrittenAsNoneAndReadBackOverOneHeld()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var cleared = ctx.Ecs.Spawn();
            ctx.Ecs.Add(cleared, new EveryReference());

            var schema = ComponentSchemas.For("Bevy.Tests.EveryReference")!;
            var text = Written(json => SceneValue.WriteComponent(json, schema, ctx.Ecs, cleared));
            foreach (var field in new[] { "Target", "Picture", "Weapon" })
                Assert.Contains($"\"{field}\":{{\"none\":true}}", text);

            var held = ctx.Ecs.Spawn();
            ctx.Ecs.Add(held, new EveryReference { Target = cleared, Weapon = new DataRef<WeaponStats>(0x5eed) });
            using var document = JsonDocument.Parse(text);
            SceneValue.ReadComponent(document.RootElement, schema, ctx.Ecs, held);

            var back = ctx.Ecs.GetOrDefault<EveryReference>(held);
            Assert.True(back.Target.IsNone);
            Assert.Equal(0ul, back.Weapon.Id);
        });

        harness.Run();
    }

    /// <summary>
    /// A ulong past what a long holds, in a field and as a map's key, is written as itself and read
    /// back, where it was turned into a long on the way out, which threw and ended the save.
    /// </summary>
    [Fact]
    public void AUlongPastWhatALongHoldsIsWrittenAndReadBack()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var source = ctx.Ecs.Spawn();
            ctx.Ecs.Add(source, new WideNumbers { Seed = ulong.MaxValue, Least = long.MinValue });
            ctx.Ecs.GetRef<WideNumbers>(source).ByHash.Set(0xF000_0000_0000_0001ul, 3);

            var schema = ComponentSchemas.For("Bevy.Tests.WideNumbers")!;
            var text = Written(json => SceneValue.WriteComponent(json, schema, ctx.Ecs, source));
            Assert.Contains("\"Seed\":18446744073709551615", text);
            Assert.Contains("\"Least\":-9223372036854775808", text);
            Assert.Contains("\"ByHash\":{\"17293822569102704641\":3}", text);

            var copy = ctx.Ecs.Spawn();
            ctx.Ecs.Add(copy, default(WideNumbers));
            using var document = JsonDocument.Parse(text);
            SceneValue.ReadComponent(document.RootElement, schema, ctx.Ecs, copy);

            var back = ctx.Ecs.GetOrDefault<WideNumbers>(copy);
            Assert.Equal(ulong.MaxValue, back.Seed);
            Assert.Equal(long.MinValue, back.Least);
            Assert.Equal(3, back.ByHash[0xF000_0000_0000_0001ul]);
        });

        harness.Run();
    }

    [Fact]
    public void AColorRoundTripsThroughSrgb()
    {
        var picked = Color.FromSrgb(1f, 0.5f, 0.25f, 0.75f);
        var shown = picked.ToSrgb();

        Assert.Equal(1f, shown.X, 4);
        Assert.Equal(0.5f, shown.Y, 4);
        Assert.Equal(0.25f, shown.Z, 4);
        Assert.Equal(0.75f, shown.W);

        // sRGB one half is about a fifth of the light, which says the transfer function ran.
        Assert.Equal(0.214f, picked.G, 3);
        Assert.Equal(Color.FromSrgb(1f, 136f / 255f, 0f), Color.FromHex("ff8800"));
        Assert.Throws<FormatException>(() => Color.FromHex("#12"));
    }

    /// <summary>What a writer produced, as text.</summary>
    private static string Written(Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer)) write(json);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
