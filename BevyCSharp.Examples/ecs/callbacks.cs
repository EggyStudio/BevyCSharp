// Bevy's callbacks example, examples/ecs/callbacks.rs at v0.19.1, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Stores systems in components and runs them on demand. Bevy registers each system and keeps its
// id, and here the component keeps the system's place in a list, which is the same thing.
internal static class Callbacks
{
    internal struct Callback
    {
        public int System;
    }

    private static readonly List<Action<BehaviorContext>> Systems = [];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            Systems.Clear();
            var ecs = ctx.Ecs;

            Spawn(ecs, _ => Console.WriteLine("This is the trivial callback system"));
            Spawn(ecs, inner => Console.WriteLine($"This is the ordinary callback system. There are currently {inner.Ecs.Count<Callback>()} callbacks in the world."));
            Spawn(ecs, inner => Console.WriteLine($"This is the exclusive callback system. There are currently {inner.Ecs.All().Length} entities in the world."));
        }, "callbacks.Setup");

        app.Update(ctx =>
        {
            foreach (var entity in ctx.Ecs.EntitiesWith<Callback>())
                Systems[ctx.Ecs.GetOrDefault<Callback>(entity).System](ctx);
        }, "callbacks.Run");
    }

    private static void Spawn(EcsWorld ecs, Action<BehaviorContext> system)
    {
        Systems.Add(system);
        ecs.Add(ecs.Spawn(), new Callback { System = Systems.Count - 1 });
    }
}
