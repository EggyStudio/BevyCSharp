using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers asking the world what is in it, rather than asking about something already known.
/// </summary>
/// <remarks>
/// What a hierarchy and an inspector are built on. Everything else in the ECS surface answers a
/// question about a component the caller already has a handle on; these are the calls that let a
/// tool show a world it knows nothing about.
/// </remarks>
[Collection("engine")]
public sealed class IntrospectionTests
{
    [Fact]
    public void EveryEntityIsListed()
    {
        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var before = ctx.Ecs.All().Length;

            var first = ctx.Ecs.Spawn();
            var second = ctx.Ecs.Spawn();

            var after = ctx.Ecs.All();

            Assert.Equal(before + 2, after.Length);
            Assert.Contains(first, after);
            Assert.Contains(second, after);

            ctx.Ecs.Despawn(first);
            Assert.DoesNotContain(first, ctx.Ecs.All());
        });

        harness.Run();
    }

    [Fact]
    public void AnEntityReportsWhatItCarries()
    {
        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, Transform.Identity);

            var components = ctx.Ecs.ComponentsOf(entity);

            Assert.Contains(NativeComponents.Transform, components);

            // Names come back for every id, which is what an inspector labels its rows with.
            var names = components.Select(ctx.Ecs.ComponentName).ToArray();
            Assert.All(names, name => Assert.False(string.IsNullOrWhiteSpace(name)));
            Assert.Contains(names, name => name.Contains("Transform", StringComparison.Ordinal));
        });

        harness.Run();
    }

    [Fact]
    public void AComponentRegisteredFromManagedCodeAnswersWithItsOwnName()
    {
        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, new Marker { Value = 7 });

            var id = ComponentType<Marker>.Id;
            var name = ctx.Ecs.ComponentName(id);

            // Registered from a layout under the managed type's full name, so this is the one
            // place a C# type's identity survives the trip into the engine and back.
            Assert.Contains("Marker", name, StringComparison.Ordinal);
            Assert.Contains(id, ctx.Ecs.ComponentsOf(entity));
        });

        harness.Run();
    }

    [Fact]
    public void AnEntityCanBeNamedAndUnnamed()
    {
        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            Assert.Null(ctx.Ecs.NameOf(entity));

            ctx.Ecs.SetName(entity, "Lamp post");
            Assert.Equal("Lamp post", ctx.Ecs.NameOf(entity));

            // Renaming replaces rather than adding a second name.
            ctx.Ecs.SetName(entity, "Lamp");
            Assert.Equal("Lamp", ctx.Ecs.NameOf(entity));

            // Nothing puts it back to being unnamed, which is not the same as being called "".
            ctx.Ecs.SetName(entity, null);
            Assert.Null(ctx.Ecs.NameOf(entity));
        });

        harness.Run();
    }

    [Fact]
    public void MostEntitiesAreCalledNothing()
    {
        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Startup, ctx =>
        {
            // A name is something a scene or a caller gave an entity, so having none is the
            // ordinary case and has to read as an answer rather than as a failure.
            var plain = ctx.Ecs.Spawn();
            Assert.Null(ctx.Ecs.NameOf(plain));

            Assert.Null(ctx.Ecs.NameOf(Entity.None));
        });

        harness.Run();
    }

    [Fact]
    public void AskingAboutAnEntityThatIsGoneSaysSoRatherThanThrowing()
    {
        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Despawn(entity);

            Assert.Empty(ctx.Ecs.ComponentsOf(entity));
            Assert.Null(ctx.Ecs.NameOf(entity));
        });

        harness.Run();
    }

    /// <summary>A component with a layout, so it registers under its own name.</summary>
    private struct Marker
    {
        public int Value;
    }
}
