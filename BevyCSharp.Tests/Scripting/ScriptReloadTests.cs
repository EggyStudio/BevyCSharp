using Bevy;
using Bevy.Scripting;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a script compiled again while the app runs, whose state declared on its enum is still the
/// state the app started with and whose components stay on their entities with their values.
/// </summary>
/// <remarks>
/// A script compiled again is a new assembly, so its enum and its components are new types with
/// the old ones' names. Unless the old are taken as the new, every system scoped to a state stops
/// running at the first reload and every system querying a component finds nothing, so a game
/// played from the editor would stop at the first script saved.
/// </remarks>
[Collection("engine")]
public sealed class ScriptReloadTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "bcs-script-reload-" + Guid.NewGuid().ToString("n"));

    public ScriptReloadTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    /// <summary>A script whose system, while the state is B, names an entity after its version.</summary>
    private void Write(string version) => File.WriteAllText(Path.Combine(_folder, "Phases.cs"), $$"""
        using Bevy;

        namespace Reloaded;

        [InitialState(A)]
        public enum Phase { A, B }

        [Behavior]
        public partial struct Phases
        {
            [OnUpdate, InState(Phase.A)]
            public static void Go(BehaviorContext ctx) => ctx.SetState(Phase.B);

            [OnUpdate, InState(Phase.B)]
            public static void Mark(BehaviorContext ctx)
            {
                foreach (var entity in ctx.Ecs.All())
                    if (ctx.Ecs.NameOf(entity) == "{{version}}") return;

                ctx.Ecs.SetName(ctx.Ecs.Spawn(), "{{version}}");
            }
        }
        """);

    [Fact]
    public void AReloadedScriptReadsTheStateTheAppStartedWith()
    {
        using var harness = new EngineHarness(frames: 20);
        harness.App.EnableDynamicSystems();

        Write("first");
        var host = new ScriptHost(harness.App, _folder);
        Assert.True(host.Reload(), host.LastError);

        var reloaded = false;
        var named = new List<string>();

        harness.OnContext(Stage.Update, ctx =>
        {
            if (ctx.Time.FrameCount == 6 && !reloaded)
            {
                reloaded = true;
                Write("second");
                Assert.True(host.Reload(), host.LastError);
            }

            named = [.. ctx.Ecs.All().Select(ctx.Ecs.NameOf).OfType<string>()];
        });

        harness.Run();
        host.Retire();

        Assert.True(reloaded);
        Assert.Contains("first", named);
        Assert.Contains("second", named);
    }

    /// <summary>A component that counts, by one before the reload and by a thousand after it.</summary>
    private void WriteCounter(int step, string extra = "") => File.WriteAllText(Path.Combine(_folder, "Counter.cs"), $$"""
        using Bevy;

        namespace Reloaded;

        [Behavior]
        public partial struct Counter
        {
            public int Count;
            public Entity Target;
            {{extra}}

            [OnUpdate]
            public void Tick(BehaviorContext ctx) => Count += {{step}};
        }
        """);

    [Fact]
    public void AReloadedScriptsComponentsStayOnTheirEntitiesWithTheirValues()
    {
        using var harness = new EngineHarness(frames: 20);
        harness.App.EnableDynamicSystems();

        WriteCounter(1);
        var host = new ScriptHost(harness.App, _folder);
        Assert.True(host.Reload(), host.LastError);

        var counted = Entity.None;
        var target = Entity.None;
        var before = -1;
        var after = -1;
        object? pointed = null;

        harness.OnContext(Stage.Startup, ctx =>
        {
            target = ctx.Ecs.Spawn();
            counted = ctx.Ecs.Spawn();

            var schema = ComponentSchemas.For("Reloaded.Counter")!;
            Assert.True(schema.Add(ctx.Ecs, counted));
            Assert.True(schema.Write(ctx.Ecs, counted, "Target", target));
        });

        harness.OnContext(Stage.Update, ctx =>
        {
            var schema = ComponentSchemas.For("Reloaded.Counter")!;

            if (ctx.Time.FrameCount == 6 && before < 0)
            {
                before = (int)schema.Read(ctx.Ecs, counted, "Count")!;

                // A field added as well, which starts at its default.
                WriteCounter(1000, "public float Added;");
                Assert.True(host.Reload(), host.LastError);
                Assert.Equal(1, host.Carried);
                return;
            }

            if (before < 0) return;

            schema = ComponentSchemas.For("Reloaded.Counter")!;
            after = (int)schema.Read(ctx.Ecs, counted, "Count")!;
            pointed = schema.Read(ctx.Ecs, counted, "Target");
        });

        harness.Run();
        host.Retire();

        // Counted by ones before, carried over, and by thousands after.
        Assert.InRange(before, 1, 999);
        Assert.True(after > 1000, $"the count went from {before} to {after}");
        Assert.Equal(before, after % 1000);
        Assert.Equal(target, pointed);
    }

    [Fact]
    public void AComponentASceneHeldBeforeItsScriptLoadedIsPutOnItsEntity()
    {
        var scene = Path.Combine(_folder, "level.scene.json");
        File.WriteAllText(scene, """
            { "format": "bevycsharp.scene.2",
              "entities": [
                { "id": 1, "name": "Target" },
                { "id": 2, "name": "Holder", "components": { "Reloaded.Later": { "Count": 5, "Target": { "entity": 1 } } } } ] }
            """);

        File.WriteAllText(Path.Combine(_folder, "Later.cs"), """
            using Bevy;

            namespace Reloaded;

            [Behavior]
            public partial struct Later
            {
                public int Count;
                public Entity Target;
            }
            """);

        using var harness = new EngineHarness(frames: 4);
        harness.App.EnableDynamicSystems();

        var host = new ScriptHost(harness.App, _folder);
        var ran = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            if (ran) return;
            ran = true;

            // As the editor does it, the level first and the scripts after.
            var load = SceneFile.Load(ctx.Ecs, scene);
            Assert.Contains("Reloaded.Later", load.Unknown);

            Assert.True(host.Reload(), host.LastError);
            Assert.Equal(1, host.Carried);

            var holder = load.Entities.Single(entity => ctx.Ecs.NameOf(entity) == "Holder");
            var target = load.Entities.Single(entity => ctx.Ecs.NameOf(entity) == "Target");
            var schema = ComponentSchemas.For("Reloaded.Later")!;

            Assert.Equal(5, schema.Read(ctx.Ecs, holder, "Count"));
            Assert.Equal(target, schema.Read(ctx.Ecs, holder, "Target"));
            Assert.False(ctx.Ecs.Has<SceneKept>(holder));
        });

        harness.Run();
        host.Retire();
        Assert.True(ran);
    }
}
