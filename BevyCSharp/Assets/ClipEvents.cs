namespace Bevy;

/// <summary>The events placed on clips, by the numbers the bridge reports them under.</summary>
/// <remarks>
/// A clip holds the number alone, since the event is a C# value Bevy cannot hold, and the number
/// finds what to trigger. The table outlives an app, as the clips placed in one do not, which
/// costs a delegate for each event ever placed, a handful for a game.
/// </remarks>
internal static class ClipEvents
{
    private static readonly Lock Gate = new();
    private static readonly List<Action<ObserverRegistry, Entity>> Placed = [];

    /// <summary>Keeps an event, answering the number the bridge reports it under.</summary>
    internal static uint Keep<TEvent>(TEvent value) where TEvent : IAnimationEvent
    {
        lock (Gate)
        {
            Placed.Add((registry, at) => registry.TriggerAt(value, at));
            return (uint)(Placed.Count - 1);
        }
    }

    /// <summary>Triggers the event the bridge reported, at the entity it happened at.</summary>
    internal static void Fire(ObserverRegistry registry, uint number, Entity at)
    {
        Action<ObserverRegistry, Entity>? fire;
        lock (Gate) fire = number < Placed.Count ? Placed[(int)number] : null;
        fire?.Invoke(registry, at);
    }
}
