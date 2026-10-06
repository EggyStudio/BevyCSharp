namespace Bevy;

/// <summary>A component's value was given up, by being replaced, removed or despawned.</summary>
/// <typeparam name="T">The component.</typeparam>
/// <param name="Entity">The entity that held it.</param>
/// <param name="Value">The value given up.</param>
/// <remarks>Bevy's <c>On&lt;Discard, T&gt;</c>, for keeping an index of values in step as they change.</remarks>
public readonly record struct Discard<T>(Entity Entity, T Value) : IEntityEvent, IReportedEvent where T : unmanaged
{
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.Watch<T, Discard<T>>(2, (entity, value) => new Discard<T>(entity, value));
}
