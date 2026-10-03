using System.Buffers;
using System.Text;
using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A component holding lists inside its own bytes.</summary>
[Behavior]
public partial struct Route
{
    /// <summary>Numbers, of which there can be eight.</summary>
    [Range(0, 10)]
    public InlineList8<float> Speeds;

    /// <summary>Names from a set, of which there can be four.</summary>
    public InlineList4<Mood> Moods;
}

/// <summary>
/// Covers the inline lists, and lists as a kind of field the schemas, the inspector and a scene
/// all understand.
/// </summary>
[Collection("engine")]
public sealed class InlineListTests
{
    [Fact]
    public void AListGrowsToItsCapacityAndNoFurther()
    {
        var list = new InlineList4<int>();
        Assert.Equal(0, list.Count);
        Assert.Equal(4, list.Capacity);

        for (var i = 0; i < 4; i++) list.Add(i * 10);

        Assert.False(list.TryAdd(99));
        Assert.Throws<InvalidOperationException>(() => list.Add(99));
        Assert.Equal([0, 10, 20, 30], list.Items.ToArray());
    }

    [Fact]
    public void TakingAnItemOutMovesTheRestDownAndLeavesNothingBehind()
    {
        var list = new InlineList4<int>();
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.RemoveAt(0);

        Assert.Equal([2, 3], list.Items.ToArray());

        // Two lists of the same items are the same bytes, which change detection compares, so the
        // slot the last item left is cleared rather than holding its old value.
        var same = new InlineList4<int>();
        same.Add(2);
        same.Add(3);
        Assert.Equal(same, list);

        list[0] = 7;
        Assert.Equal(7, list.Items[0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => list[2]);
    }

    [Fact]
    public void AListFieldIsDescribedAsAListOfItsItemKind()
    {
        var schema = ComponentSchemas.For("Bevy.Tests.Route")!;

        var speeds = schema.Field("Speeds")!;
        Assert.Equal(FieldKind.List, speeds.Kind);
        Assert.Equal(FieldKind.Float, speeds.ElementKind);
        Assert.Equal(10d, speeds.Hints.Maximum);

        var moods = schema.Field("Moods")!;
        Assert.Equal(FieldKind.Enum, moods.ElementKind);
        Assert.Equal(["Calm", "Restless"], moods.Options);
    }

    [Fact]
    public void AListIsReadAndWrittenWholeThroughItsSchema()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var route = new Route();
            route.Speeds.Add(1f);
            route.Speeds.Add(2f);

            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, route);

            var speeds = ComponentSchemas.For("Bevy.Tests.Route")!.Field("Speeds")!;
            var read = Assert.IsType<ListValue>(speeds.Read(ctx.Ecs, entity));
            Assert.Equal(new ListValue([1f, 2f]), read);

            // Two reads of the same list are equal, so a tool comparing them sees no change.
            Assert.Equal(read, speeds.Read(ctx.Ecs, entity));

            // A double from a slider is taken as the float the list holds.
            Assert.True(speeds.Write(ctx.Ecs, entity, read.Adding(3.5d)));
            Assert.Equal([1f, 2f, 3.5f], ctx.Ecs.GetOrDefault<Route>(entity).Speeds.Items.ToArray());

            Assert.True(speeds.Write(ctx.Ecs, entity, read.Moving(1, 0).Without(1)));
            Assert.Equal([2f], ctx.Ecs.GetOrDefault<Route>(entity).Speeds.Items.ToArray());

            // Past the capacity is refused, and the component is left as it was.
            var tooMany = new ListValue(Enumerable.Range(0, 9).Select(i => (object?)(float)i));
            Assert.False(speeds.Write(ctx.Ecs, entity, tooMany));
            Assert.Equal([2f], ctx.Ecs.GetOrDefault<Route>(entity).Speeds.Items.ToArray());
        });

        harness.Run();
    }

    [Fact]
    public void AListIsWrittenToASceneAsAnArray()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var route = new Route();
            route.Speeds.Add(0.5f);
            route.Moods.Add(Mood.Restless);
            route.Moods.Add(Mood.Calm);

            var source = ctx.Ecs.Spawn();
            ctx.Ecs.Add(source, route);

            var schema = ComponentSchemas.For("Bevy.Tests.Route")!;
            var buffer = new ArrayBufferWriter<byte>();
            using (var json = new Utf8JsonWriter(buffer))
                SceneValue.WriteComponent(json, schema, ctx.Ecs, source);

            var text = Encoding.UTF8.GetString(buffer.WrittenSpan);
            Assert.Contains("\"Moods\":[\"Restless\",\"Calm\"]", text);
            Assert.Contains("\"Speeds\":[0.5]", text);

            var copy = ctx.Ecs.Spawn();
            ctx.Ecs.Add(copy, default(Route));
            using var document = JsonDocument.Parse(text);
            SceneValue.ReadComponent(document.RootElement, schema, ctx.Ecs, copy);

            Assert.Equal(route.Moods, ctx.Ecs.GetOrDefault<Route>(copy).Moods);
            Assert.Equal(route.Speeds, ctx.Ecs.GetOrDefault<Route>(copy).Speeds);
        });

        harness.Run();
    }
}
