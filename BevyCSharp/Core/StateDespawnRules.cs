namespace Bevy;

/// <summary>
/// The entities waiting for a transition of <typeparamref name="TState"/> their rule answers true
/// for, which <see cref="EcsWorld.DespawnWhen{TState}"/> adds and the posting of transitions applies.
/// </summary>
/// <remarks>
/// Kept here rather than as a component, since a rule is a delegate and a component holds plain
/// data alone. Each app starts with none, as its state slots do.
/// </remarks>
internal static class StateDespawnRules<TState> where TState : struct, Enum
{
    private static readonly Lock Gate = new();
    private static readonly List<(Entity Entity, Func<StateTransitionEvent<TState>, bool> Rule)> Rules = [];
    private static int _generation = -1;

    internal static void Add(Entity entity, Func<StateTransitionEvent<TState>, bool> rule)
    {
        lock (Gate)
        {
            Reset();
            Rules.Add((entity, rule));
        }
    }

    /// <summary>Despawns each entity whose rule answers true for the transition, its rule going with it.</summary>
    internal static void Apply(EcsWorld ecs, StateTransitionEvent<TState> transition)
    {
        List<Entity> gone;
        lock (Gate)
        {
            Reset();
            if (Rules.Count == 0) return;
            gone = [.. Rules.Where(entry => entry.Rule(transition)).Select(entry => entry.Entity)];
            Rules.RemoveAll(entry => gone.Contains(entry.Entity) || !ecs.IsAlive(entry.Entity));
        }

        foreach (var entity in gone)
        {
            if (ecs.IsAlive(entity)) ecs.Despawn(entity);
        }
    }

    private static void Reset()
    {
        if (_generation == ComponentRegistry.Generation) return;
        Rules.Clear();
        _generation = ComponentRegistry.Generation;
    }
}
