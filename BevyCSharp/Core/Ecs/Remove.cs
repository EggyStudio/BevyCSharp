namespace Bevy;

/// <summary>A component left an entity, by removal or despawn.</summary>
/// <typeparam name="T">The component, a C# one or one of Bevy's through its wrapper.</typeparam>
/// <param name="Entity">The entity it left.</param>
/// <param name="Value">
/// The value it had, or for one of Bevy's a wrapper over the entity, which no longer carries it.
/// </param>
/// <remarks>Bevy's <c>On&lt;Remove, T&gt;</c>. The observer runs once the component has gone, and is handed the value it had.</remarks>
public readonly record struct Remove<T>(Entity Entity, T Value) : IEntityEvent, IReportedEvent where T : struct
{
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.Watch<T, Remove<T>>(3, (entity, value) => new Remove<T>(entity, value));
}
