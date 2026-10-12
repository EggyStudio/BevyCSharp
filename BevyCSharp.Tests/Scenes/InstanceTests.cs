using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a glTF scene placed as an instance: its nodes addressed by path, the changes made over it
/// recorded as overrides, and those written to a scene file and applied again after a load.
/// </summary>
/// <remarks>
/// A glTF scene is a world asset, which a headless bridge spawns nothing from, so these return
/// early there as the other glTF tests do. Each waits on <see cref="WorldInstanceReady"/> rather
/// than a frame count, since the file loads on its own schedule.
/// </remarks>
[Collection("engine")]
public sealed class InstanceTests : IDisposable
{
    private const string Rig = "models/rig.gltf";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-instance-" + Guid.NewGuid().ToString("n"));

    public InstanceTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [SkippableFact]
    public void AnInstanceIsReportedReadyAndItsNodesAreFoundByPath()
    {
        using var harness = new EngineHarness(frames: 300, fps: 240);
        Needs.Renderer();

        var root = Entity.None;
        var ready = false;
        string? path = null;
        var turret = Vec3.Zero;

        harness.OnContext(Stage.Startup, ctx => root = SceneInstances.Spawn(ctx.Ecs, Rig));

        harness.OnContext(Stage.Update, ctx =>
        {
            if (!ctx.Read<WorldInstanceReady>().ToArray().Any(message => message.Entity == root)) return;

            // Bevy spawns a glTF scene as an entity named after the scene, with the file's nodes
            // under it, so a path starts at the scene's name.
            ready = true;
            var found = SceneInstances.Find(ctx.Ecs, root, "Main/Hull/Turret");
            if (!found.IsNone)
            {
                path = SceneInstances.PathOf(ctx.Ecs, root, found);
                turret = ctx.Ecs.GetRef<Transform>(found).Translation;
            }

            ctx.Exit();
        });

        harness.Run();

        Assert.True(ready, "the instance was never reported ready");
        Assert.Equal("Main/Hull/Turret", path);
        Assert.Equal(new Vec3(0f, 1f, 0f), turret);
    }

    [SkippableFact]
    public void OverridesAreSavedAsChangesAndAppliedAgainAfterALoad()
    {
        var file = Path.Combine(_root, "fleet.scene.json");

        using (var saving = new EngineHarness(frames: 300, fps: 240))
        {
            Needs.Renderer();

            var root = Entity.None;

            saving.OnContext(Stage.Startup, ctx =>
            {
                root = SceneInstances.Spawn(ctx.Ecs, Rig);
                ctx.Ecs.SetName(root, "Ship");
                ctx.Ecs.GetRef<Transform>(root).Translation = new Vec3(5f, 0f, 0f);
            });

            saving.OnContext(Stage.Update, ctx =>
            {
                if (!ctx.Read<WorldInstanceReady>().ToArray().Any(message => message.Entity == root)) return;

                var hull = SceneInstances.Find(ctx.Ecs, root, "Main/Hull");
                var turret = SceneInstances.Find(ctx.Ecs, root, "Main/Hull/Turret");
                var antenna = SceneInstances.Find(ctx.Ecs, root, "Main/Hull/Antenna");

                Assert.True(SceneInstances.Set(ctx.Ecs, turret, "Bevy.Transform", "Translation", new Vec3(0f, 2f, 0f)));
                Assert.True(SceneInstances.Add(ctx.Ecs, hull, "Bevy.Tests.Sprung"));
                Assert.True(SceneInstances.Set(ctx.Ecs, hull, "Bevy.Tests.Sprung", "Mass", 4f));
                Assert.True(SceneInstances.Delete(ctx.Ecs, antenna));

                // Three overrides, the set on the hull folded into the add before it.
                Assert.Equal(3, SceneInstances.Overrides(ctx.Ecs, root).Count);

                SceneFile.Save(ctx.Ecs, file);
                ctx.Exit();
            });

            saving.Run();
        }

        // The instance alone, as its reference and its changes, with none of the model's nodes.
        using (var saved = JsonDocument.Parse(File.ReadAllText(file)))
        {
            var ship = Assert.Single(saved.RootElement.GetProperty("entities").EnumerateArray());
            Assert.Equal("Ship", ship.GetProperty("name").GetString());
            Assert.Equal("models/rig.gltf#Scene0", ship.GetProperty("instance").GetProperty("path").GetString());
            Assert.Equal(3, ship.GetProperty("overrides").GetArrayLength());
        }

        using var loading = new EngineHarness(frames: 300, fps: 240);

        var loaded = Entity.None;
        var applied = false;

        loading.OnContext(Stage.Startup, ctx => loaded = Assert.Single(SceneFile.Load(ctx.Ecs, file).Entities));

        loading.OnContext(Stage.Update, ctx =>
        {
            if (!ctx.Read<WorldInstanceReady>().ToArray().Any(message => message.Entity == loaded)) return;

            Assert.Equal(new Vec3(5f, 0f, 0f), ctx.Ecs.GetRef<Transform>(loaded).Translation);

            var turret = SceneInstances.Find(ctx.Ecs, loaded, "Main/Hull/Turret");
            Assert.Equal(new Vec3(0f, 2f, 0f), ctx.Ecs.GetRef<Transform>(turret).Translation);

            var hull = SceneInstances.Find(ctx.Ecs, loaded, "Main/Hull");
            Assert.Equal(4f, ctx.Ecs.GetOrDefault<Sprung>(hull).Mass);

            Assert.True(SceneInstances.Find(ctx.Ecs, loaded, "Main/Hull/Antenna").IsNone);
            Assert.Empty(SceneInstances.Missed(ctx.Ecs, loaded));

            applied = true;
            ctx.Exit();
        });

        loading.Run();
        Assert.True(applied, "the loaded instance was never reported ready");
    }

    [SkippableFact]
    public void AnOverrideWhoseNodeIsGoneIsReportedAndKept()
    {
        var file = Path.Combine(_root, "renamed.scene.json");
        File.WriteAllText(file, """
            { "format": "bevycsharp.scene.2",
              "entities": [
                { "id": 1, "name": "Ship", "instance": { "path": "models/rig.gltf" },
                  "overrides": [
                    { "at": "Main/Hull/Cannon", "set": { "Bevy.Transform": { "Translation": [0, 9, 0] } } },
                    { "at": "Main/Hull/Turret", "set": { "Bevy.Transform": { "Translation": [0, 3, 0] } } } ],
                  "components": { "Bevy.Transform": { "Translation": [0, 0, 0] } } } ] }
            """);

        using var harness = new EngineHarness(frames: 300, fps: 240);
        Needs.Renderer();

        var root = Entity.None;
        IReadOnlyList<string> missed = [];
        var turret = Vec3.Zero;

        harness.OnContext(Stage.Startup, ctx => root = Assert.Single(SceneFile.Load(ctx.Ecs, file).Entities));

        harness.OnContext(Stage.Update, ctx =>
        {
            if (!ctx.Read<WorldInstanceReady>().ToArray().Any(message => message.Entity == root)) return;

            missed = SceneInstances.Missed(ctx.Ecs, root);
            turret = ctx.Ecs.GetRef<Transform>(SceneInstances.Find(ctx.Ecs, root, "Main/Hull/Turret")).Translation;

            SceneFile.Save(ctx.Ecs, file);
            ctx.Exit();
        });

        harness.Run();

        // The one that found its node applied, and the one that did not kept for a later repair.
        Assert.Equal(["Main/Hull/Cannon"], missed);
        Assert.Equal(new Vec3(0f, 3f, 0f), turret);
        Assert.Contains("Main/Hull/Cannon", File.ReadAllText(file));
    }

    [SkippableFact]
    public void AFieldPutBackToTheModelsValueLeavesNoOverride()
    {
        using var harness = new EngineHarness(frames: 300, fps: 240);
        Needs.Renderer();

        var root = Entity.None;
        var checkedIt = false;

        harness.OnContext(Stage.Startup, ctx => root = SceneInstances.Spawn(ctx.Ecs, Rig));

        harness.OnContext(Stage.Update, ctx =>
        {
            if (!ctx.Read<WorldInstanceReady>().ToArray().Any(message => message.Entity == root)) return;

            var turret = SceneInstances.Find(ctx.Ecs, root, "Main/Hull/Turret");

            // Written some other way and then marked, as the inspector does.
            var before = ctx.Ecs.GetRef<Transform>(turret).Translation;
            ctx.Ecs.GetRef<Transform>(turret).Translation = new Vec3(0f, 7f, 0f);
            Assert.True(SceneInstances.Mark(ctx.Ecs, turret, "Bevy.Transform", "Translation", before));
            Assert.True(SceneInstances.IsOverridden(ctx.Ecs, turret, "Bevy.Transform", "Translation"));
            Assert.False(SceneInstances.IsOverridden(ctx.Ecs, turret, "Bevy.Transform", "Scale"));

            // Reverted, the model's value is back and the override is gone.
            Assert.True(SceneInstances.Revert(ctx.Ecs, turret, "Bevy.Transform", "Translation"));
            Assert.Equal(new Vec3(0f, 1f, 0f), ctx.Ecs.GetRef<Transform>(turret).Translation);
            Assert.Empty(SceneInstances.Overrides(ctx.Ecs, root));

            // Set and then set back by hand, as an undo would, which leaves nothing either.
            Assert.True(SceneInstances.Set(ctx.Ecs, turret, "Bevy.Transform", "Translation", new Vec3(0f, 5f, 0f)));
            Assert.Single(SceneInstances.Overrides(ctx.Ecs, root));
            Assert.True(SceneInstances.Set(ctx.Ecs, turret, "Bevy.Transform", "Translation", new Vec3(0f, 1f, 0f)));
            Assert.Empty(SceneInstances.Overrides(ctx.Ecs, root));

            // A component the model never had, added and taken off again, leaves nothing either.
            var hull = SceneInstances.Find(ctx.Ecs, root, "Main/Hull");
            Assert.True(SceneInstances.Add(ctx.Ecs, hull, "Bevy.Tests.Sprung"));
            Assert.True(SceneInstances.Remove(ctx.Ecs, hull, "Bevy.Tests.Sprung"));
            Assert.Empty(SceneInstances.Overrides(ctx.Ecs, root));

            // The root's own components are the instance's, not an override.
            Assert.False(SceneInstances.Set(ctx.Ecs, root, "Bevy.Transform", "Translation", new Vec3(1f, 0f, 0f)));

            checkedIt = true;
            ctx.Exit();
        });

        harness.Run();
        Assert.True(checkedIt, "the instance was never reported ready");
    }

    [SkippableFact]
    public void AChildAddedUnderAModelsNodeGoesBackUnderItOnceTheModelSpawns()
    {
        var file = Path.Combine(_root, "flagged.scene.json");

        using (var saving = new EngineHarness(frames: 300, fps: 240))
        {
            Needs.Renderer();

            var root = Entity.None;
            saving.OnContext(Stage.Startup, ctx => root = SceneInstances.Spawn(ctx.Ecs, Rig));

            saving.OnContext(Stage.Update, ctx =>
            {
                if (!ctx.Read<WorldInstanceReady>().ToArray().Any(message => message.Entity == root)) return;

                var flag = ctx.Ecs.Spawn();
                ctx.Ecs.SetName(flag, "Flag");
                ctx.Ecs.Add(flag, Transform.At(0f, 0.5f, 0f));
                ctx.Ecs.SetParent(flag, SceneInstances.Find(ctx.Ecs, root, "Main/Hull/Turret"));

                SceneFile.Save(ctx.Ecs, file);
                ctx.Exit();
            });

            saving.Run();
        }

        using var loading = new EngineHarness(frames: 300, fps: 240);

        var placed = Entity.None;
        var found = false;

        loading.OnContext(Stage.Startup, ctx =>
            placed = Assert.Single(SceneFile.Load(ctx.Ecs, file).Entities, entity => SceneInstances.IsInstance(ctx.Ecs, entity)));

        loading.OnContext(Stage.Update, ctx =>
        {
            if (!ctx.Read<WorldInstanceReady>().ToArray().Any(message => message.Entity == placed)) return;

            found = !SceneInstances.Find(ctx.Ecs, placed, "Main/Hull/Turret/Flag").IsNone;
            ctx.Exit();
        });

        loading.Run();
        Assert.True(found, "the flag was not put back under the turret");
    }

    [SkippableFact]
    public void ARenamedNodeKeepsItsPathAndItsNewNameAfterALoad()
    {
        var file = Path.Combine(_root, "renamed-node.scene.json");

        using (var saving = new EngineHarness(frames: 300, fps: 240))
        {
            Needs.Renderer();

            var root = Entity.None;
            saving.OnContext(Stage.Startup, ctx => root = SceneInstances.Spawn(ctx.Ecs, Rig));

            saving.OnContext(Stage.Update, ctx =>
            {
                if (!ctx.Read<WorldInstanceReady>().ToArray().Any(message => message.Entity == root)) return;

                var turret = SceneInstances.Find(ctx.Ecs, root, "Main/Hull/Turret");
                Assert.True(SceneInstances.Rename(ctx.Ecs, turret, "Gun"));

                // Still found by the model's name, and an override after the rename is made there.
                Assert.Equal("Main/Hull/Turret", SceneInstances.PathOf(ctx.Ecs, root, turret));
                Assert.True(SceneInstances.Set(ctx.Ecs, turret, "Bevy.Transform", "Translation", new Vec3(0f, 6f, 0f)));

                SceneFile.Save(ctx.Ecs, file);
                ctx.Exit();
            });

            saving.Run();
        }

        using var loading = new EngineHarness(frames: 300, fps: 240);

        var placed = Entity.None;
        string? name = null;
        var moved = Vec3.Zero;

        loading.OnContext(Stage.Startup, ctx => placed = Assert.Single(SceneFile.Load(ctx.Ecs, file).Entities));

        loading.OnContext(Stage.Update, ctx =>
        {
            if (!ctx.Read<WorldInstanceReady>().ToArray().Any(message => message.Entity == placed)) return;

            var turret = SceneInstances.Find(ctx.Ecs, placed, "Main/Hull/Turret");
            name = ctx.Ecs.NameOf(turret);
            moved = ctx.Ecs.GetRef<Transform>(turret).Translation;
            ctx.Exit();
        });

        loading.Run();

        Assert.Equal("Gun", name);
        Assert.Equal(new Vec3(0f, 6f, 0f), moved);
    }
}
