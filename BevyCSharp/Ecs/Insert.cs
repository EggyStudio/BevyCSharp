namespace Bevy;

/// <summary>A component was put on an entity, whether or not it had one before.</summary>
/// <typeparam name="T">The component.</typeparam>
/// <param name="Entity">The entity it was put on.</param>
/// <param name="Value">Its value once put there.</param>
/// <remarks>Bevy's <c>On&lt;Insert, T&gt;</c>, which follows an <see cref="Add{T}"/> where there was one.</remarks>
public readonly record struct Insert<T>(Entity Entity, T Value) : IEntityEvent, IReportedEvent where T : unmanaged
{
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.Watch<T, Insert<T>>(1, (entity, value) => new Insert<T>(entity, value));
}
