using Bevy;
using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>A component holding text, a number, and a value worked out as the game runs.</summary>
[Behavior]
public partial struct Signpost
{
    /// <summary>Text, which a component holds through a handle into the managed store.</summary>
    public EcsText Caption;

    /// <summary>A number every file keeps.</summary>
    public float Distance;

    /// <summary>A value no file keeps, left as the type starts it on a load.</summary>
    [NotSaved] public float Since;
}

/// <summary>A resource of the game's own, which a scene writes with its entities.</summary>
[SceneResource]
public sealed class Tally
{
    /// <summary>A number.</summary>
    public int Score;

    /// <summary>Text, which a resource holds as a plain string, living on the managed side.</summary>
    public string Leader = string.Empty;
}

/// <summary>
/// Covers a world a scene saves whole, as Bevy's world_serialization saves one, with a component's
/// text, a field left out and a resource of the game's own.
/// </summary>
[Collection("engine")]
public sealed class SceneWholeTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-whole-");

    public void Dispose() => _folder.Dispose();

    /// <summary>
    /// A component's text, its number and a resource are saved and come back in another world, the
    /// field left out comes back as the type starts it, and the scene does not hold it.
    /// </summary>
    [Fact]
    public void TextAFieldLeftOutAndAResourceComeBackInAnotherWorld()
    {
        var file = _folder.File("whole.scene.json");

        using (var saving = new EngineHarness(frames: 2))
        {
            saving.OnContext(Stage.Startup, ctx =>
            {
                var post = ctx.Ecs.Spawn();
                ctx.Ecs.Add(post, new Signpost { Caption = "north", Distance = 2f, Since = 5f });
                ctx.World.InsertResource(new Tally { Score = 7, Leader = "Ada" });
                SceneFile.Save(ctx.Ecs, file);
            });

            saving.Run();
        }

        var text = File.ReadAllText(file);
        Assert.Contains("\"Caption\": \"north\"", text);
        Assert.DoesNotContain("Since", text);
        Assert.Contains("\"worldResources\"", text);

        using var loading = new EngineHarness(frames: 2);
        Signpost read = default;
        string caption = string.Empty;
        Tally? tally = null;

        loading.OnContext(Stage.Startup, ctx =>
        {
            var loaded = SceneFile.Load(ctx.Ecs, file);
            read = ctx.Ecs.GetOrDefault<Signpost>(Assert.Single(loaded.Entities));
            caption = read.Caption;
            if (ctx.World.TryGetResource<Tally>(out var held)) tally = held;
        });

        loading.Run();

        Assert.Equal(("north", 2f, 0f), (caption, read.Distance, read.Since));
        Assert.NotNull(tally);
        Assert.Equal((7, "Ada"), (tally!.Score, tally.Leader));
    }

    /// <summary>
    /// A scene written before a field was left out, holding it, and before resources were written,
    /// loads as it did, the field at the type's start and no resource put in.
    /// </summary>
    [Fact]
    public void ASceneFromBeforeReadsAsBefore()
    {
        var file = _folder.File("before.scene.json");
        File.WriteAllText(file, """
            {
              "format": "bevycsharp.scene.2",
              "entities": [
                { "id": 1, "components": { "Bevy.Tests.Signpost": { "Caption": "east", "Distance": 4, "Since": 9 } } }
              ]
            }
            """);

        using var loading = new EngineHarness(frames: 2);
        Signpost read = default;
        string caption = string.Empty;
        var hasTally = true;

        loading.OnContext(Stage.Startup, ctx =>
        {
            var loaded = SceneFile.Load(ctx.Ecs, file);
            read = ctx.Ecs.GetOrDefault<Signpost>(Assert.Single(loaded.Entities));
            caption = read.Caption;
            hasTally = ctx.World.TryGetResource<Tally>(out _);
        });

        loading.Run();

        Assert.Equal(("east", 4f, 0f), (caption, read.Distance, read.Since));
        Assert.False(hasTally);
    }

    /// <summary>
    /// A document written and read back to put entities back, as the editor's undo of a delete
    /// keeps one, holds no resources and leaves the world's as the game has since made them.
    /// </summary>
    [Fact]
    public void ADocumentOfEntitiesLeavesTheResourcesAlone()
    {
        using var harness = new EngineHarness(frames: 2);
        var written = string.Empty;
        var score = 0;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var post = ctx.Ecs.Spawn();
            ctx.Ecs.Add(post, new Signpost { Caption = "west", Distance = 1f });
            ctx.World.InsertResource(new Tally { Score = 7 });

            var buffer = new System.Buffers.ArrayBufferWriter<byte>();
            using (var json = new System.Text.Json.Utf8JsonWriter(buffer))
                SceneFile.Write(ctx.Ecs, json, entity => entity == post);
            written = System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);

            ctx.World.SetResource(new Tally { Score = 9 });
            using var document = System.Text.Json.JsonDocument.Parse(written);
            Assert.Single(SceneFile.Read(ctx.Ecs, document.RootElement).Entities);
            score = ctx.World.Resource<Tally>().Score;
        });

        harness.Run();

        Assert.DoesNotContain("worldResources", written);
        Assert.Equal(9, score);
    }

    /// <summary>
    /// Text is freed with its component, a clone has text of its own, and the inspector's write of
    /// it to the rest of a selection gives each its own, where a handle shared would let one
    /// entity's text change another's.
    /// </summary>
    [Fact]
    public void TextIsEachEntitysOwn()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, ctx =>
        {
            if (ctx.Time.FrameCount != 1) return;

            var schema = ComponentSchemas.All.Single(each => each.Name == nameof(Signpost));
            var field = schema.Fields.Single(each => each.Name == nameof(Signpost.Caption));
            Assert.Equal(FieldKind.String, field.Kind);

            var before = EcsStore.Live;
            var shown = ctx.Ecs.Spawn();
            var other = ctx.Ecs.Spawn();
            ctx.Ecs.Add(shown, new Signpost { Caption = "west" });
            ctx.Ecs.Add(other, new Signpost { Caption = "south" });

            // What the inspector's text box does, then its spreading to the rest of the selection.
            var was = field.Read(ctx.Ecs, shown);
            field.Write(ctx.Ecs, shown, "up");
            Assert.Equal(1, ComponentFields.Spread(ctx, shown, [other], field, was));
            ctx.Ecs.GetRef<Signpost>(shown).Caption.Value = "down";
            Assert.Equal(("down", "up"), ((string)ctx.Ecs.GetOrDefault<Signpost>(shown).Caption, (string)ctx.Ecs.GetOrDefault<Signpost>(other).Caption));

            var copy = ctx.Ecs.Clone(shown);
            ctx.Ecs.GetRef<Signpost>(shown).Caption.Value = "left";
            Assert.Equal("down", (string)ctx.Ecs.GetOrDefault<Signpost>(copy).Caption);

            ctx.Ecs.Despawn(shown);
            ctx.Ecs.Despawn(other);
            ctx.Ecs.Despawn(copy);
            Assert.Equal(before, EcsStore.Live);
        });

        harness.Run();
    }
}
