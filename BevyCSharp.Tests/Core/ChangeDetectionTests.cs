using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A number on an entity, which the change detection tests set and watch.</summary>
[Behavior]
public partial struct ChangeProbe
{
    /// <summary>The number.</summary>
    public int Value;
}

/// <summary>A resource the change detection tests set and watch.</summary>
public sealed record ChangeCounter(int Value);

/// <summary>A resource a change detection test's system marks changed itself.</summary>
public sealed record ChangeOwn(int Value);

/// <summary>
/// When a resource or a component was added and changed, weighed against the running system's last
/// run, and the call that changed it, as Bevy's change_detection reads them.
/// </summary>
[Collection("engine")]
public sealed class ChangeDetectionTests
{
    /// <summary>
    /// A resource is added and changed on a system's first run, changed again on the run after a
    /// different value is set, and not on the run after the same value is set, and a system does not
    /// see on its next run the change it made itself.
    /// </summary>
    [Fact]
    public void AResourcesChangeIsSeenOnceBySystemsThatRunAfterIt()
    {
        using var harness = new EngineHarness(frames: 4);
        var frame = 0;
        var seen = new List<(bool Changed, bool Added)>();
        var own = new List<bool>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            ctx.World.InsertResource(new ChangeCounter(0));
            ctx.World.InsertResource(new ChangeOwn(0));
        });
        harness.OnContext(Stage.PreUpdate, ctx =>
        {
            frame++;
            if (frame is 2 or 3) ctx.World.SetResourceIfNeq(new ChangeCounter(1));
        });
        harness.OnContext(Stage.Update, ctx => seen.Add((ctx.World.IsChanged<ChangeCounter>(), ctx.World.IsAdded<ChangeCounter>())));
        harness.OnContext(Stage.PostUpdate, ctx =>
        {
            own.Add(ctx.World.IsChanged<ChangeOwn>());
            ctx.World.MarkChanged<ChangeOwn>();
        });
        harness.Run();

        Assert.Equal([(true, true), (true, false), (false, false), (false, false)], seen);

        // Inserted before its first run, and its own mark after that no change to it.
        Assert.Equal([true, false, false, false], own);
    }

    /// <summary>
    /// A component is added on the first run after it was put on, changed on the run after a
    /// different value is set, and untouched by setting the value it holds.
    /// </summary>
    [Fact]
    public void AComponentIsAddedOnceAndChangedOnlyByADifferentValue()
    {
        using var harness = new EngineHarness(frames: 3);
        var frame = 0;
        var probe = Entity.None;
        var seen = new List<(bool Changed, bool Added)>();
        var wrote = new List<bool>();

        harness.OnContext(Stage.Startup, ctx =>
        {
            probe = ctx.Ecs.Spawn();
            ctx.Ecs.Add(probe, new ChangeProbe());
        });
        harness.OnContext(Stage.PreUpdate, ctx =>
        {
            frame++;
            if (frame is 2 or 3) wrote.Add(ctx.Ecs.SetIfNeq(probe, new ChangeProbe { Value = 1 }));
        });
        harness.OnContext(Stage.Update, ctx => seen.Add((ctx.Ecs.Changed<ChangeProbe>(probe), ctx.Ecs.Added<ChangeProbe>(probe))));
        harness.Run();

        Assert.Equal([true, false], wrote);
        Assert.Equal([(true, true), (true, false), (false, false)], seen);
    }

    /// <summary>
    /// With the switch on, the call that last set a resource or a component is named by its file and
    /// line, and a later write with no call to name, as a reference into the component, names none.
    /// </summary>
    [Fact]
    public void TheCallThatLastSetAValueIsNamedWhereTheSwitchIsOn()
    {
        using var harness = new EngineHarness(frames: 2, configure: config => config.TrackChangeLocations = true);
        var frame = 0;
        var probe = Entity.None;
        string? resource = null, set = null, written = "unread";

        harness.OnContext(Stage.Startup, ctx =>
        {
            probe = ctx.Ecs.Spawn();
            ctx.World.InsertResource(new ChangeCounter(0));
        });
        harness.OnContext(Stage.Update, ctx =>
        {
            if (++frame == 1)
            {
                ctx.Ecs.Set(probe, new ChangeProbe { Value = 1 });
                set = ctx.Ecs.ChangedBy<ChangeProbe>(probe);
                resource = ctx.World.ChangedBy<ChangeCounter>();
                return;
            }

            ctx.Ecs.GetRef<ChangeProbe>(probe).Value = 2;
            written = ctx.Ecs.ChangedBy<ChangeProbe>(probe);
        });
        harness.Run();

        Assert.StartsWith("ChangeDetectionTests.cs:", set);
        Assert.StartsWith("ChangeDetectionTests.cs:", resource);
        Assert.Null(written);
    }

    /// <summary>With the switch off, as it is by default, no call is named.</summary>
    [Fact]
    public void NoCallIsNamedWhereTheSwitchIsOff()
    {
        using var harness = new EngineHarness(frames: 1);
        var probe = Entity.None;
        string? set = "unread";

        harness.OnContext(Stage.Update, ctx =>
        {
            probe = ctx.Ecs.Spawn();
            ctx.Ecs.Set(probe, new ChangeProbe { Value = 1 });
            set = ctx.Ecs.ChangedBy<ChangeProbe>(probe);
        });
        harness.Run();

        Assert.Null(set);
    }
}
