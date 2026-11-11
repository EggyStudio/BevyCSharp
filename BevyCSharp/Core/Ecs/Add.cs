namespace Bevy;

/// <summary>A component was added to an entity that did not have one.</summary>
/// <typeparam name="T">The component, a C# one or one of Bevy's through its wrapper.</typeparam>
/// <param name="Entity">The entity it was added to.</param>
/// <param name="Value">Its value once added, or for one of Bevy's a wrapper over it.</param>
/// <remarks>
/// <para>
/// Bevy's <c>On&lt;Add, T&gt;</c>, observed with <see cref="EcsWorld.Observe{TEvent}(Action{On{TEvent}})"/>.
/// One of Bevy's own components is observed through its wrapper, as Bevy's <c>On&lt;Add,
/// Pressed&gt;</c> is <c>Add&lt;PressedRef&gt;</c>:
/// </para>
/// <code>
/// ecs.Observe&lt;Add&lt;PressedRef&gt;&gt;(on => Restyle(on.Ecs, on.Entity));
/// </code>
/// <para>
/// Its <c>Value</c> is then a wrapper over the component, which reads it as it is when read rather
/// than as it was added, since Bevy's bytes are Rust's own.
/// </para>
/// </remarks>
public readonly record struct Add<T>(Entity Entity, T Value) : IEntityEvent, IReportedEvent where T : struct
{
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.Watch<T, Add<T>>(0, (entity, value) => new Add<T>(entity, value));
}
