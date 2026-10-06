namespace Bevy;

/// <summary>A component left an entity, by removal or despawn.</summary>
/// <typeparam name="T">The component.</typeparam>
/// <param name="Entity">The entity it left.</param>
/// <param name="Value">The value it had.</param>
/// <remarks>Bevy's <c>On&lt;Remove, T&gt;</c>. The observer runs once the component has gone, and is handed the value it had.</remarks>
public readonly record struct Remove<T>(Entity Entity, T Value) : IEntityEvent, IReportedEvent where T : unmanaged
{
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.Watch<T, Remove<T>>(3, (entity, value) => new Remove<T>(entity, value));
}
