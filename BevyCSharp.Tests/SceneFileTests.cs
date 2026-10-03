using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers writing entities to a scene file and spawning them from it into another world.
/// </summary>
/// <remarks>
/// A scene saved from one engine's world and loaded into a second engine's, so nothing the first
/// world held can make the comparison pass by accident. Each test writes into a directory of its own.
/// </remarks>
[Collection("engine")]
public sealed class SceneFileTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-scene-" + Guid.NewGuid().ToString("n"));

    public SceneFileTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void ASceneSavedFromOneWorldIsTheSameSceneInAnother()
    {
        var file = Path.Combine(_root, "level.scene.json");

        using (var saving = new EngineHarness(frames: 2))
        {
            saving.OnContext(Stage.Startup, ctx =>
            {
                var rig = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(rig, "Rig");
                ctx.Ecs.Add(rig, Transform.At(1f, 2f, 3f));

                var arm = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(arm, "Arm");
                ctx.Ecs.Add(arm, Transform.At(0f, 1f, 0f));
                ctx.Ecs.Add(arm, new Sprung { Mass = 2f, Front = new Spring { Stiffness = 5f } });
                ctx.Ecs.Add(arm, new EveryKind { Count = 7, Mood = Mood.Restless, Target = rig });
                ctx.Ecs.SetParent(arm, rig);

                // One with no name, which a world file of edits by name could not keep.
                var bag = ctx.Ecs.Spawn();
                ctx.Ecs.Add(bag, default(Bag));
                ctx.Ecs.GetRef<Bag>(bag).Names.Add("rope");

                Assert.Equal(3, SceneFile.Save(ctx.Ecs, file));
            });

            saving.Run();
        }

        using var loading = new EngineHarness(frames: 2);

        loading.OnContext(Stage.Startup, ctx =>
        {
            var loaded = SceneFile.Load(ctx.Ecs, file);
            Assert.Equal(3, loaded.Entities.Count);
            Assert.Empty(loaded.Unknown);

            var rig = Assert.Single(loaded.Entities, entity => ctx.Ecs.NameOf(entity) == "Rig");
            var arm = Assert.Single(loaded.Entities, entity => ctx.Ecs.NameOf(entity) == "Arm");
            var bag = Assert.Single(loaded.Entities, entity => ctx.Ecs.NameOf(entity) is null);

            // The hierarchy, and the transform relative to it.
            Assert.Equal(rig, ctx.Ecs.ParentOf(arm));
            Assert.Equal(new Vec3(1f, 2f, 3f), ctx.Ecs.GetRef<Transform>(rig).Translation);
            Assert.Equal(new Vec3(0f, 1f, 0f), ctx.Ecs.GetRef<Transform>(arm).Translation);

            // A C# component field by field, a nested struct included.
            Assert.Equal(2f, ctx.Ecs.GetOrDefault<Sprung>(arm).Mass);
            Assert.Equal(5f, ctx.Ecs.GetOrDefault<Sprung>(arm).Front.Stiffness);

            // A reference to another entity in the scene, now the one it spawned as.
            var kinds = ctx.Ecs.GetOrDefault<EveryKind>(arm);
            Assert.Equal(7, kinds.Count);
            Assert.Equal(Mood.Restless, kinds.Mood);
            Assert.Equal(rig, kinds.Target);

            Assert.Equal(["rope"], ctx.Ecs.GetOrDefault<Bag>(bag).Names.Items);
        });

        loading.Run();
    }

    [Fact]
    public void AnEntityKeepsItsIdAcrossSaves()
    {
        var file = Path.Combine(_root, "ids.scene.json");

        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var first = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(first, "First");
            ctx.Ecs.Add(first, Transform.Identity);

            var second = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(second, "Second");
            ctx.Ecs.Add(second, Transform.Identity);

            SceneFile.Save(ctx.Ecs, file);
            var before = Ids(file);

            // A third added before them in the world's order takes the next free id, and the two
            // that were there keep theirs, so a diff shows one entity added and nothing renumbered.
            ctx.Ecs.Despawn(first);
            var replaced = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(replaced, "Third");
            ctx.Ecs.Add(replaced, Transform.Identity);
            var restored = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(restored, "First");
            ctx.Ecs.Add(restored, Transform.Identity);
            ctx.Ecs.Add(restored, new SceneId { Value = before["First"] });

            SceneFile.Save(ctx.Ecs, file);
            var after = Ids(file);

            Assert.Equal(before["First"], after["First"]);
            Assert.Equal(before["Second"], after["Second"]);
            Assert.Equal(Math.Max(before["First"], before["Second"]) + 1, after["Third"]);
        });

        harness.Run();
    }

    [Fact]
    public void AComponentThisBuildDoesNotKnowIsReportedAndNotSpawned()
    {
        var scene = """
            { "format": "bevycsharp.scene.2",
              "entities": [
                { "id": 1, "name": "Odd",
                  "components": { "Game.Gone": { "X": 1 }, "Bevy.Transform": { "Translation": [4, 5, 6] } } } ] }
            """;

        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            using var document = JsonDocument.Parse(scene);
            var loaded = SceneFile.Read(ctx.Ecs, document.RootElement);

            Assert.Equal(["Game.Gone"], loaded.Unknown);
            var odd = Assert.Single(loaded.Entities);
            Assert.Equal(new Vec3(4f, 5f, 6f), ctx.Ecs.GetRef<Transform>(odd).Translation);

            Assert.Throws<InvalidDataException>(() =>
            {
                using var wrong = JsonDocument.Parse("""{ "format": "bevycsharp.world.1", "entities": [] }""");
                SceneFile.Read(ctx.Ecs, wrong.RootElement);
            });
        });

        harness.Run();
    }

    [Fact]
    public void BevysOwnComponentsAreWrittenAsBevysJsonAndReadBack()
    {
        const string Light = "bevy_light::point_light::PointLight";
        var file = Path.Combine(_root, "lit.scene.json");
        var ran = false;

        using (var saving = new EngineHarness(frames: 2))
        {
            saving.OnContext(Stage.Startup, ctx =>
            {
                if (!App.HasRenderer) return;

                var lamp = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(lamp, "Lamp");
                ctx.Ecs.Add(lamp, Transform.At(0f, 3f, 0f));
                ctx.Ecs.InsertReflected(lamp, Light);
                ctx.Ecs.SetReflected(lamp, Light, ".intensity", "1234");
                ctx.Ecs.SetReflectedColor(lamp, Light, ".color", Color.FromHex("#ff8800"));

                SceneFile.Save(ctx.Ecs, file, entity => ctx.Ecs.NameOf(entity) == "Lamp");
            });

            saving.Run();
        }

        if (!App.HasRenderer) return;

        // What the engine works out for itself is left for it to work out again.
        var text = File.ReadAllText(file);
        Assert.Contains(Light, text);
        Assert.DoesNotContain("GlobalTransform", text);
        Assert.DoesNotContain("ViewVisibility", text);

        using var loading = new EngineHarness(frames: 2);

        loading.OnContext(Stage.Startup, ctx =>
        {
            var loaded = SceneFile.Load(ctx.Ecs, file);
            Assert.Empty(loaded.Refused);

            var lamp = Assert.Single(loaded.Entities);
            Assert.Equal("1234.0", ctx.Ecs.GetReflected(lamp, Light, ".intensity"));
            Assert.Equal(Color.FromHex("#ff8800").G, ctx.Ecs.GetReflectedColor(lamp, Light, ".color")!.Value.G, 4);
            Assert.Equal(new Vec3(0f, 3f, 0f), ctx.Ecs.GetRef<Transform>(lamp).Translation);
            ran = true;
        });

        loading.Run();
        Assert.True(ran);
    }

    [Fact]
    public void AMeshAndMaterialMadeInMemoryAreWrittenAsHowToMakeThemAndStayShared()
    {
        var file = Path.Combine(_root, "made.scene.json");
        var ran = false;

        using (var saving = new EngineHarness(frames: 2))
        {
            saving.OnContext(Stage.Startup, ctx =>
            {
                if (!App.HasRenderer) return;

                var box = Render.CreateMesh(MeshShape.Cuboid, 1f, 2f, 3f);
                var paint = Render.CreateMaterial(new MaterialSettings { BaseColor = (0.2f, 0.4f, 0.6f, 1f), Roughness = 0.7f });

                foreach (var name in new[] { "Left", "Right" })
                {
                    var crate = ctx.Ecs.Spawn();
                    ctx.Ecs.SetName(crate, name);
                    ctx.Ecs.Add(crate, Transform.Identity);
                    Render.SetMesh(ctx.Ecs, crate, box);
                    Render.SetMaterial(ctx.Ecs, crate, paint);
                }

                SceneFile.Save(ctx.Ecs, file);
            });

            saving.Run();
        }

        if (!App.HasRenderer) return;

        // One resource each, however many entities use it.
        var written = File.ReadAllText(file);
        using (var document = JsonDocument.Parse(written))
            Assert.True(2 == document.RootElement.GetProperty("resources").GetArrayLength(), written);

        using var loading = new EngineHarness(frames: 2);

        loading.OnContext(Stage.Startup, ctx =>
        {
            var loaded = SceneFile.Load(ctx.Ecs, file);
            var left = Assert.Single(loaded.Entities, entity => ctx.Ecs.NameOf(entity) == "Left");
            var right = Assert.Single(loaded.Entities, entity => ctx.Ecs.NameOf(entity) == "Right");

            // Still one mesh and one material, shared as they were.
            var mesh = Render.MeshOf(ctx.Ecs, left);
            Assert.True(mesh.IsValid);
            Assert.Equal(mesh, Render.MeshOf(ctx.Ecs, right));
            Assert.Equal(Render.MaterialOf(ctx.Ecs, left), Render.MaterialOf(ctx.Ecs, right));

            // Made again as what they were.
            Assert.Equal(new MeshRecipe(MeshShape.Cuboid, 1f, 2f, 3f), Render.RecipeOf(mesh));
            Assert.True(Render.TryReadMaterial(Render.MaterialOf(ctx.Ecs, left), out var paint));
            Assert.Equal((0.2f, 0.4f, 0.6f, 1f), paint!.BaseColor);
            Assert.Equal(0.7f, paint.Roughness);
            ran = true;
        });

        loading.Run();
        Assert.True(ran);
    }

    /// <summary>Each entity's id in a scene file, by its name.</summary>
    private static Dictionary<string, int> Ids(string file)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(file));
        return document.RootElement.GetProperty("entities").EnumerateArray()
            .Where(entry => entry.TryGetProperty("name", out _))
            .ToDictionary(entry => entry.GetProperty("name").GetString()!, entry => entry.GetProperty("id").GetInt32());
    }
}
