using Bevy;
using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>A component with two fields, so an edit can change one and be asked about the other.</summary>
[Behavior]
public partial struct Tuning
{
    /// <summary>The one the tests edit.</summary>
    public float Speed;

    /// <summary>The one they leave alone.</summary>
    public float Mass;
}

/// <summary>
/// Covers editing several things at once.
/// </summary>
/// <remarks>
/// The panel is about one entity and a selection is often several, so what a person changes in it
/// is meant for all of them. The drawing is ImGui and needs a window; what is checked here is the
/// step after the widget, which decides what the change reaches.
/// </remarks>
[Collection("engine")]
public sealed class EditorMultiEditTests
{
    [Fact]
    public void AChangeReachesEverythingElseSelected()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, ctx =>
        {
            var schema = ComponentSchemas.All.Single(each => each.Name == nameof(Tuning));
            var speed = schema.Fields.Single(each => each.Name == nameof(Tuning.Speed));

            var shown = ctx.Ecs.Spawn();
            var other = ctx.Ecs.Spawn();

            ctx.Ecs.Add(shown, new Tuning { Speed = 1f, Mass = 5f });
            ctx.Ecs.Add(other, new Tuning { Speed = 2f, Mass = 7f });

            // What the widget did, which the panel does not describe to anything downstream.
            var before = speed.Read(ctx.Ecs, shown);
            speed.Write(ctx.Ecs, shown, 9f);

            var written = ComponentFields.Spread(ctx, shown, [other], speed, before);

            Assert.Equal(1, written);
            Assert.Equal(9f, ctx.Ecs.GetOrDefault<Tuning>(other).Speed);

            // And only the field that was edited, so the rest of the component is left as it was.
            Assert.Equal(7f, ctx.Ecs.GetOrDefault<Tuning>(other).Mass);
        });

        harness.Run();
    }

    /// <summary>A widget that changed nothing writes nothing, so the history stays quiet.</summary>
    [Fact]
    public void AnEditThatChangedNothingWritesNothing()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, ctx =>
        {
            var schema = ComponentSchemas.All.Single(each => each.Name == nameof(Tuning));
            var speed = schema.Fields.Single(each => each.Name == nameof(Tuning.Speed));

            var shown = ctx.Ecs.Spawn();
            var other = ctx.Ecs.Spawn();

            ctx.Ecs.Add(shown, new Tuning { Speed = 1f });
            ctx.Ecs.Add(other, new Tuning { Speed = 2f });

            var before = speed.Read(ctx.Ecs, shown);

            Assert.Equal(0, ComponentFields.Spread(ctx, shown, [other], speed, before));

            // The other one keeps what it had rather than being leveled to whatever the panel
            // happened to be showing.
            Assert.Equal(2f, ctx.Ecs.GetOrDefault<Tuning>(other).Speed);
        });

        harness.Run();
    }

    /// <summary>Nothing else selected is nothing to write to.</summary>
    [Fact]
    public void OneThingSelectedWritesNowhereElse()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Update, ctx =>
        {
            var schema = ComponentSchemas.All.Single(each => each.Name == nameof(Tuning));
            var speed = schema.Fields.Single(each => each.Name == nameof(Tuning.Speed));

            var shown = ctx.Ecs.Spawn();
            ctx.Ecs.Add(shown, new Tuning { Speed = 1f });

            var before = speed.Read(ctx.Ecs, shown);
            speed.Write(ctx.Ecs, shown, 4f);

            Assert.Equal(0, ComponentFields.Spread(ctx, shown, [], speed, before));
        });

        harness.Run();
    }
}
