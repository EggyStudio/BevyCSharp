// Bevy's relationships example, examples/ecs/relationships.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Entities that target each other, a relationship of the example's own, as Bevy's ChildOf relates a
// child to its parent. Each targets one other at a time and is targeted by any number, which Bevy
// keeps a list of. The relations are made, changed and taken away, and followed to find a cycle.
//
// Bevy's runs each step once on a world of its own, and this runs them in the one frame of a
// headless run, in the same order.
internal static class Relationships
{
    // Bevy's Targeting and the TargetedBy it keeps, named here by a type of the example's own.
    internal readonly struct Targeting;

    private static readonly List<Entity> Spawned = [];

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Spawned.Clear();

        // Bevy's spawning_entities_with_relationships.
        var alice = Spawn(ecs, "Alice");
        var bob = Spawn(ecs, "Bob");
        ecs.Relate<Targeting>(bob, alice);
        var charlie = Spawn(ecs, "Charlie");
        ecs.Relate<Targeting>(charlie, bob);
        ecs.Relate<Targeting>(Spawn(ecs, "James"), charlie);
        ecs.Relate<Targeting>(Spawn(ecs, "Devon"), charlie);
        ecs.Relate<Targeting>(alice, charlie);

        DebugRelationships(ecs);

        // Bevy's mutate_relationships.
        Console.WriteLine("Making Devon target Alice.\n");
        ecs.Relate<Targeting>(Find(ecs, "Devon"), alice);
        DebugRelationships(ecs);

        var cycle = CheckForCycles(ecs);
        Console.WriteLine($"{cycle} \n");

        // Bevy's untarget.
        Console.WriteLine("Removing Charlie's targeting relationship.\n");
        ecs.Unrelate<Targeting>(charlie);
        DebugRelationships(ecs);
        Console.WriteLine($"{CheckForCycles(ecs)} \n");
    }, "relationships.Run");

    private static Entity Spawn(EcsWorld ecs, string name)
    {
        var entity = ecs.Spawn();
        ecs.SetName(entity, name);
        Spawned.Add(entity);
        return entity;
    }

    private static Entity Find(EcsWorld ecs, string name) => Spawned.First(entity => ecs.NameOf(entity) == name);

    // Who each entity targets and who targets it.
    private static void DebugRelationships(EcsWorld ecs)
    {
        var relationships = new System.Text.StringBuilder();
        foreach (var entity in Spawned)
        {
            if (ecs.RelationOf<Targeting>(entity) is not { } target) continue;
            var targetedBy = ecs.RelatedTo<Targeting>(entity);
            var by = targetedBy.Length > 0 ? string.Join(", ", targetedBy.Select(ecs.NameOf)) : "nobody";
            relationships.Append($"{ecs.NameOf(entity)} is targeting {ecs.NameOf(target)}, and is targeted by {by}\n");
        }

        Console.WriteLine(relationships);
    }

    // Each entity's targets followed in turn, the first one met again a cycle, which Bevy's
    // answers as an error holding where it began and what it met.
    private static string CheckForCycles(EcsWorld ecs)
    {
        foreach (var initial in Spawned.Where(entity => ecs.RelationOf<Targeting>(entity) is not null))
        {
            var visited = new List<Entity>();
            var targeting = ecs.NameOf(initial);
            Console.WriteLine($"Checking for cycles starting at {targeting}");

            foreach (var target in ecs.RelationAncestors<Targeting>(initial))
            {
                var targetName = ecs.NameOf(target);
                Console.WriteLine($"{targeting} is targeting {targetName}");
                targeting = targetName;

                if (visited.Contains(target))
                    return $"Err(TargetingCycle {{ initial_entity: {Id(initial)}, visited: {{{string.Join(", ", visited.Select(Id))}}} }})";
                visited.Add(target);
            }
        }

        return "Ok(())";
    }

    private static string Id(Entity entity) => $"{entity.Index}v{entity.Generation}";
}
