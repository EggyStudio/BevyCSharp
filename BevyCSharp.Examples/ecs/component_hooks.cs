// Bevy's component_hooks example, examples/ecs/component_hooks.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// This example illustrates the different ways you can employ component lifecycle hooks. Each key
// pressed spawns an entity holding it, the hooks keep an index from each key to its entity as the
// component goes on and comes off, and a key let go takes its component off, whose remove hook
// despawns the entity. Bevy's hooks also say where a change came from, with its track_location
// feature, which is off by default here as there.
internal static class ComponentHooksExample
{
    public static void Build(App app)
    {
        app.World.InsertResource(new MyComponentIndex());

        app.OnAdd((HookContext ctx, in MyComponent component) =>
            {
                Console.WriteLine($"ComponentId({ctx.ComponentId}) added to {ctx.Entity} with value {component.Key}");
                ctx.Res<MyComponentIndex>()[component.Key] = ctx.Entity;
                ctx.Messages.Send(new MyMessage());
            })
            .OnInsert((HookContext ctx, in MyComponent _) => Console.WriteLine($"Current Index: {ctx.Res<MyComponentIndex>()}"))
            .OnDiscard((HookContext ctx, in MyComponent component) => ctx.Res<MyComponentIndex>().Remove(component.Key))
            .OnRemove((HookContext ctx, in MyComponent component) =>
            {
                Console.WriteLine($"ComponentId({ctx.ComponentId}) removed from {ctx.Entity} with value {component.Key}");
                ctx.Cmd.Despawn(ctx.Entity);
            });

        app.Update(TriggerHooks, "component_hooks.TriggerHooks");
    }

    // Bevy's trigger_hooks, the component taken off for each key let go and put on a new entity for
    // each key pressed.
    private static void TriggerHooks(BehaviorContext ctx)
    {
        foreach (var (key, entity) in ctx.Res<MyComponentIndex>())
            if (!ctx.Input.KeyDown(key)) ctx.Cmd.Remove<MyComponent>(entity);

        foreach (var key in Enum.GetValues<Key>())
            if (ctx.Input.KeyPressed(key)) ctx.Cmd.Spawn((entity, ecs) => ecs.Add(entity, new MyComponent { Key = key }));
    }

    // Bevy's MyComponentIndex, from each key to the entity holding it, written as Bevy's Debug writes it.
    private sealed class MyComponentIndex : Dictionary<Key, Entity>
    {
        public override string ToString() => $"MyComponentIndex({{{string.Join(", ", this.Select(pair => $"{pair.Key}: {pair.Value}"))}}})";
    }

    // Bevy's MyMessage, sent as a key's component is added.
    private readonly record struct MyMessage;

    // Bevy's MyComponent, a key held by an entity while the key is. Nested, since the
    // removal_detection example names a behavior of its own the same in this namespace.
    internal struct MyComponent
    {
        public Key Key;
    }
}
