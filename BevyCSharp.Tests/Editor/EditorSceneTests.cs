using Bevy;
using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the editor's document: a scene file that leaves the editor's own entities out and replaces
/// the scene when it is loaded, and one that does not read, which leaves the scene as it was.
/// </summary>
[Collection("engine")]
public sealed class EditorSceneTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-editor-");

    private string Level => _folder.File("level.scene.json");

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void LoadingReplacesTheSceneAndKeepsTheEditorsOwn()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var camera = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(camera, "View");
            ctx.Ecs.Add(camera, Transform.At(0f, 5f, 5f));
            ctx.Ecs.Add(camera, new EditorOnly());

            var box = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(box, "Box");
            ctx.Ecs.Add(box, Transform.Identity);
            ctx.Ecs.Add(box, new Sprung { Mass = 2f });

            Assert.Equal(1, EditorScene.Save(ctx.Ecs, Level));
            Assert.DoesNotContain("View", File.ReadAllText(Level));

            // Changes made after the save, which loading takes back.
            ctx.Ecs.GetRef<Sprung>(box).Mass = 9f;
            var stray = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(stray, "Stray");
            ctx.Ecs.Add(stray, Transform.Identity);

            var loaded = EditorScene.Load(ctx.Ecs, Level);

            var named = ctx.Ecs.All().Select(ctx.Ecs.NameOf).OfType<string>().ToList();
            Assert.Single(named, name => name == "Box");
            Assert.DoesNotContain("Stray", named);
            Assert.True(ctx.Ecs.IsAlive(camera));

            var restored = Assert.Single(loaded.Entities);
            Assert.Equal(2f, ctx.Ecs.GetOrDefault<Sprung>(restored).Mass);
        });

        harness.Run();
    }

    [Fact]
    public void AFileThatDoesNotReadLeavesTheSceneAsItWas()
    {
        File.WriteAllText(Level, """{ "format": "bevycsharp.scene.2", "entit""");

        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var box = ctx.Ecs.Spawn();
            ctx.Ecs.SetName(box, "Box");
            ctx.Ecs.Add(box, Transform.Identity);

            var loaded = EditorScene.Load(ctx.Ecs, Level);

            Assert.Contains("level.scene.json", loaded.Problem);
            Assert.Empty(loaded.Entities);
            Assert.True(ctx.Ecs.IsAlive(box), "the scene being edited was taken away for a file that does not read");
        });

        harness.Run();
    }
}
