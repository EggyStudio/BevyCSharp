namespace Bevy;

/// <summary>A component was added to an entity that did not have one.</summary>
/// <typeparam name="T">The component.</typeparam>
/// <param name="Entity">The entity it was added to.</param>
/// <param name="Value">Its value once added.</param>
/// <remarks>Bevy's <c>On&lt;Add, T&gt;</c>, observed with <see cref="EcsWorld.Observe{TEvent}(Action{On{TEvent}})"/>.</remarks>
public readonly record struct Add<T>(Entity Entity, T Value) : IEntityEvent, IReportedEvent where T : unmanaged
{
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.Watch<T, Add<T>>(0, (entity, value) => new Add<T>(entity, value));
}
