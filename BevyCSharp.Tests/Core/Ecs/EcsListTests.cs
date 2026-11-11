using System.Buffers;
using System.Text;
using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A component holding lists in the managed store.</summary>
[Behavior]
public partial struct Bag
{
    /// <summary>Text, which only a stored list can hold.</summary>
    public EcsList<string> Names;

    /// <summary>Numbers, as many as there are.</summary>
    public EcsList<float> Weights;
}

/// <summary>A component holding dictionaries in the managed store.</summary>
[Behavior]
public partial struct Ledger
{
    /// <summary>Numbers by name, which a scene writes as an object.</summary>
    public EcsMap<string, int> Counts;

    /// <summary>Points by position, which a scene writes as pairs, a vector being no property name.</summary>
    public EcsMap<Vec3, float> Heights;
}

/// <summary>
/// Covers the lists and dictionaries a component holds a handle to, and the hook that frees them
/// with it.
/// </summary>
[Collection("engine")]
public sealed class EcsListTests
{
    [Fact]
    public void AListIsFreedWhenItsEntityIsDespawned()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var before = EcsStore.Live;

            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, default(Bag));

            // Made on the first add, through a reference to the component's own field, so the
            // component keeps the handle that add wrote.
            ref var bag = ref ctx.Ecs.GetRef<Bag>(entity);
            bag.Names.Add("rope");
            bag.Names.Add("lamp");
            bag.Weights.Add(2.5f);

            Assert.Equal(before + 2, EcsStore.Live);
            Assert.Equal(["rope", "lamp"], ctx.Ecs.GetOrDefault<Bag>(entity).Names.Items);

            var held = ctx.Ecs.GetOrDefault<Bag>(entity).Names;
            ctx.Ecs.Despawn(entity);

            // The store is back where it started, and a handle kept past the despawn fails rather
            // than reading a slot another list may have by now.
            Assert.Equal(before, EcsStore.Live);
            Assert.Throws<ObjectDisposedException>(() => held.Count);
        });

        harness.Run();
    }

    [Fact]
    public void AListIsFreedWhenItsComponentIsRemoved()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var before = EcsStore.Live;

            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, default(Bag));
            ctx.Ecs.GetRef<Bag>(entity).Weights.Add(1f);
            Assert.Equal(before + 1, EcsStore.Live);

            ctx.Ecs.Remove<Bag>(entity);
            Assert.Equal(before, EcsStore.Live);
            Assert.True(ctx.Ecs.IsAlive(entity));
        });

        harness.Run();
    }

    [Fact]
    public void AStoredListIsAListFieldWhoseItemsMayBeText()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var schema = ComponentSchemas.For("Bevy.Tests.Bag")!;
            var names = schema.Field("Names")!;
            Assert.Equal(FieldKind.List, names.Kind);
            Assert.Equal(FieldKind.String, names.ElementKind);

            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, default(Bag));
            Assert.Equal(ListValue.Empty, names.Read(ctx.Ecs, entity));

            // Written through a copy of the component, which makes the list and writes the new
            // handle back with it.
            Assert.True(names.Write(ctx.Ecs, entity, new ListValue(["a", "b"])));
            Assert.Equal(new ListValue(["a", "b"]), names.Read(ctx.Ecs, entity));

            // An item that is not text refuses the write, and the list keeps what it had.
            Assert.False(names.Write(ctx.Ecs, entity, new ListValue(["c", 4])));
            Assert.Equal(new ListValue(["a", "b"]), names.Read(ctx.Ecs, entity));

            var buffer = new ArrayBufferWriter<byte>();
            using (var json = new Utf8JsonWriter(buffer))
                SceneValue.WriteComponent(json, schema, ctx.Ecs, entity);

            var text = Encoding.UTF8.GetString(buffer.WrittenSpan);
            Assert.Contains("\"Names\":[\"a\",\"b\"]", text);

            var copy = ctx.Ecs.Spawn();
            ctx.Ecs.Add(copy, default(Bag));
            using var document = JsonDocument.Parse(text);
            SceneValue.ReadComponent(document.RootElement, schema, ctx.Ecs, copy);

            // A list of its own, not the original's handle, so freeing one leaves the other.
            Assert.Equal(["a", "b"], ctx.Ecs.GetOrDefault<Bag>(copy).Names.Items);
            Assert.NotEqual(ctx.Ecs.GetOrDefault<Bag>(entity).Names, ctx.Ecs.GetOrDefault<Bag>(copy).Names);
        });

        harness.Run();
    }

    [Fact]
    public void AMapIsFreedWithItsEntityAndKeepsItsKeysApart()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var before = EcsStore.Live;
            var schema = ComponentSchemas.For("Bevy.Tests.Ledger")!;

            var counts = schema.Field("Counts")!;
            Assert.Equal(FieldKind.Map, counts.Kind);
            Assert.Equal(FieldKind.String, counts.KeyKind);
            Assert.Equal(FieldKind.Int, counts.ElementKind);

            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, default(Ledger));
            ctx.Ecs.GetRef<Ledger>(entity).Counts.Set("apples", 3);
            Assert.Equal(before + 1, EcsStore.Live);

            var read = Assert.IsType<MapValue>(counts.Read(ctx.Ecs, entity));
            Assert.True(counts.Write(ctx.Ecs, entity, read.Adding("pears", 5L)));
            Assert.Equal(5, ctx.Ecs.GetOrDefault<Ledger>(entity).Counts["pears"]);

            // Two entries under one key would lose one of them, so the write is refused whole.
            Assert.False(counts.Write(ctx.Ecs, entity, read.Adding("apples", 9)));
            Assert.Equal(3, ctx.Ecs.GetOrDefault<Ledger>(entity).Counts["apples"]);

            ctx.Ecs.Despawn(entity);
            Assert.Equal(before, EcsStore.Live);
        });

        harness.Run();
    }

    [Fact]
    public void AMapIsWrittenToASceneAsAnObjectOrAsPairs()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var schema = ComponentSchemas.For("Bevy.Tests.Ledger")!;
            var source = ctx.Ecs.Spawn();
            ctx.Ecs.Add(source, default(Ledger));

            ref var ledger = ref ctx.Ecs.GetRef<Ledger>(source);
            ledger.Counts.Set("apples", 3);
            ledger.Heights.Set(new Vec3(1f, 0f, 2f), 4.5f);

            var buffer = new ArrayBufferWriter<byte>();
            using (var json = new Utf8JsonWriter(buffer))
                SceneValue.WriteComponent(json, schema, ctx.Ecs, source);

            var text = Encoding.UTF8.GetString(buffer.WrittenSpan);
            Assert.Contains("\"Counts\":{\"apples\":3}", text);
            Assert.Contains("\"Heights\":[[[1,0,2],4.5]]", text);

            var copy = ctx.Ecs.Spawn();
            ctx.Ecs.Add(copy, default(Ledger));
            using var document = JsonDocument.Parse(text);
            SceneValue.ReadComponent(document.RootElement, schema, ctx.Ecs, copy);

            var back = ctx.Ecs.GetOrDefault<Ledger>(copy);
            Assert.Equal(3, back.Counts["apples"]);
            Assert.Equal(4.5f, back.Heights[new Vec3(1f, 0f, 2f)]);
        });

        harness.Run();
    }
}
