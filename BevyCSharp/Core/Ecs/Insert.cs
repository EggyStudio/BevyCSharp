namespace Bevy;

/// <summary>A component was put on an entity, whether or not it had one before.</summary>
/// <typeparam name="T">The component, a C# one or one of Bevy's through its wrapper.</typeparam>
/// <param name="Entity">The entity it was put on.</param>
/// <param name="Value">Its value once put there, or for one of Bevy's a wrapper over it.</param>
/// <remarks>Bevy's <c>On&lt;Insert, T&gt;</c>, which follows an <see cref="Add{T}"/> where there was one.</remarks>
public readonly record struct Insert<T>(Entity Entity, T Value) : IEntityEvent, IReportedEvent where T : struct
{
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.Watch<T, Insert<T>>(1, (entity, value) => new Insert<T>(entity, value));
}
