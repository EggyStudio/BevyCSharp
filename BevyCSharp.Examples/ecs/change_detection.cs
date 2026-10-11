// Bevy's change_detection example, examples/ecs/change_detection.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs.Changes;

// Shows how to tell when a component or a resource changed, was added, and by which call. Three
// systems now and then set a component and a resource to the elapsed seconds, only where the value
// differs, and a fourth says each change it sees.
//
// Bevy's draws its chances from an unseeded generator, as does this, which the capture seeds as it
// seeds the other examples that roll dice.
internal static class ChangeDetection
{
    // Bevy's MyResource, its equality its value's, as Bevy derives PartialEq for set_if_neq.
    internal sealed record MyResource(float Value)
    {
        public override string ToString() => FormattableString.Invariant($"MyResource({Value:0.0})");
    }

    private static Random _random = new();

    // Bevy's changed_by, which it keeps under its track_location feature.
    public static void Configure(Config config) => config.TrackChangeLocations = true;

    public static void Build(App app)
    {
        _random = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ? new Random(19878367) : new Random();

        app.Startup(ctx =>
        {
            ctx.Ecs.Add(ctx.Ecs.Spawn(), new MyComponent());
            ctx.World.InsertResource(new MyResource(0f));
        }, "change_detection.Setup");

        app.Update(ChangeComponent, "change_detection.ChangeComponent");
        app.Update(ChangeComponent, "change_detection.ChangeComponent2");
        app.Update(ChangeResource, "change_detection.ChangeResource");
        app.Update(Detect, "change_detection.ChangeDetection");
    }

    // A tenth of the time, the component set to the elapsed seconds, only where they differ.
    private static void ChangeComponent(BehaviorContext ctx)
    {
        foreach (var entity in ctx.Ecs.EntitiesWith<MyComponent>())
        {
            if (_random.NextDouble() >= 0.1) continue;
            var changed = new MyComponent { Value = MathF.Round(ctx.Time.Elapsed) };
            Log.Info($"New value: {changed} {entity.Index}v{entity.Generation}");
            ctx.Ecs.SetIfNeq(entity, changed);
        }
    }

    // The same of the resource.
    private static void ChangeResource(BehaviorContext ctx)
    {
        if (_random.NextDouble() >= 0.1) return;
        var changed = new MyResource(MathF.Round(ctx.Time.Elapsed));
        Log.Info($"New value: {changed}");
        ctx.World.SetResourceIfNeq(changed);
    }

    // Each component changed since this system last ran, and the resource where it changed, with
    // whether it was added and the call that changed it.
    private static void Detect(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var entity in ecs.EntitiesWith<MyComponent>())
        {
            if (!ecs.Changed<MyComponent>(entity)) continue;
            Log.Warn(Detected(ecs.GetOrDefault<MyComponent>(entity), ecs.Added<MyComponent>(entity), true, ecs.ChangedBy<MyComponent>(entity)));
        }

        var world = ctx.World;
        if (world.IsChanged<MyResource>())
            Log.Warn(Detected(world.Resource<MyResource>(), world.IsAdded<MyResource>(), true, world.ChangedBy<MyResource>()));
    }

    private static string Detected(object value, bool added, bool changed, string? by) =>
        $"Change detected!\n\t-> value: {value}\n\t-> added: {Lower(added)}\n\t-> changed: {Lower(changed)}\n\t-> changed by: {by ?? "unknown"}";

    private static string Lower(bool value) => value ? "true" : "false";
}

/// <summary>Bevy's MyComponent, a number a system sets now and then.</summary>
[Behavior]
public partial struct MyComponent : IEquatable<MyComponent>
{
    /// <summary>The number.</summary>
    public float Value;

    /// <summary>Equal where the numbers are, as Bevy derives PartialEq for set_if_neq.</summary>
    public readonly bool Equals(MyComponent other) => Value.Equals(other.Value);

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) => obj is MyComponent other && Equals(other);

    /// <inheritdoc/>
    public override readonly int GetHashCode() => Value.GetHashCode();

    /// <summary>The number as Bevy's Debug writes it.</summary>
    public override readonly string ToString() => FormattableString.Invariant($"MyComponent({Value:0.0})");
}
