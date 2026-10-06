namespace Bevy;

/// <summary>An entity carrying a component was despawned.</summary>
/// <typeparam name="T">The component.</typeparam>
/// <param name="Entity">The entity, no longer alive by the time the observer runs.</param>
/// <param name="Value">The value the component had.</param>
/// <remarks>Bevy's <c>On&lt;Despawn, T&gt;</c>.</remarks>
public readonly record struct Despawn<T>(Entity Entity, T Value) : IEntityEvent, IReportedEvent where T : unmanaged
{
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.Watch<T, Despawn<T>>(4, (entity, value) => new Despawn<T>(entity, value));
}
