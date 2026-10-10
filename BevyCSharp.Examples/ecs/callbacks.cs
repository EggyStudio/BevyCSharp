// Bevy's callbacks example, examples/ecs/callbacks.rs at v0.19.1, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Stores systems in components and runs them on demand. Bevy registers each system and keeps its
// id, and here the component keeps the system's place in a list, which is the same thing.
internal static class Callbacks
{
    // The systems the callbacks name, Bevy's registered systems.
    internal static readonly List<Action<EcsWorld>> Systems = [];

    public static void Build(App app) => app.Startup(ctx =>
    {
        Systems.Clear();
        var ecs = ctx.Ecs;
        Spawn(ecs, _ => Console.WriteLine("This is the trivial callback system"));
        Spawn(ecs, world => Console.WriteLine($"This is the ordinary callback system. There are currently {world.Count<Callback>()} callbacks in the world."));
        Spawn(ecs, world => Console.WriteLine($"This is the exclusive callback system. There are currently {world.All().Length} entities in the world."));
    }, "callbacks.Setup");

    private static void Spawn(EcsWorld ecs, Action<EcsWorld> system)
    {
        Systems.Add(system);
        ecs.Add(ecs.Spawn(), new Callback { System = Systems.Count - 1 });
    }
}

/// <summary>A system kept on an entity, by its place among the example's systems, as Bevy keeps its id.</summary>
[Behavior]
public partial struct Callback
{
    /// <summary>The system's place.</summary>
    public int System;

    /// <summary>The system run, each frame, as Bevy's <c>run_callbacks</c> runs each by a command.</summary>
    [OnUpdate]
    public void RunCallbacks(BehaviorContext ctx) => ctx.Cmd.Run(Callbacks.Systems[System]);
}
