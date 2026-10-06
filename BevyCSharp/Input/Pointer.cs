namespace Bevy;

/// <summary>
/// Something a pointer did to an entity, a mouse, a touch or a pointer of the game's own, found by
/// Bevy's picking, as Bevy's <c>Pointer&lt;E&gt;</c>.
/// </summary>
/// <typeparam name="TEvent">What it did, one of <see cref="Over"/>, <see cref="Click"/>, <see cref="Drag"/> and the rest.</typeparam>
/// <param name="Entity">The entity it did it to, the nearest one under the pointer.</param>
/// <param name="PointerId">Which pointer.</param>
/// <param name="Position">
/// Where the pointer is, in logical pixels from the window's top left corner.
/// </param>
/// <param name="Event">What it did, and with what.</param>
/// <remarks>
/// <para>
/// Observed as any entity event is, at one entity or at every one:
/// </para>
/// <code>
/// ecs.Observe&lt;Pointer&lt;Click&gt;&gt;(button, on => Spawn(on.Ecs));
/// ecs.Observe&lt;Pointer&lt;Drag&gt;&gt;(cube, on => Turn(on.Ecs, on.Entity, on.Event.Event.Delta));
/// </code>
/// <para>
/// It goes up the entity's parents after the entity's own observers, as Bevy's does, so a panel
/// hears a click on the text inside it, until an observer calls <see cref="On{TEvent}.Propagate"/>
/// with false. Bevy finds what is under the pointer with a backend for each kind of thing drawn,
/// the interface's nodes, meshes where <see cref="Config.MeshPicking"/> asks for them, and sprites
/// that carry Bevy's <c>Pickable</c>, as Bevy's own examples give theirs. The first observer of a
/// kind asks Bevy to report it, so a game that observes nothing pays nothing.
/// </para>
/// <para>
/// Needs a bridge with the renderer, which <see cref="App.HasRenderer"/> reports. In one without,
/// observing works and nothing is ever reported, since there is nothing drawn to point at.
/// </para>
/// </remarks>
public readonly record struct Pointer<TEvent>(Entity Entity, PointerId PointerId, Vec2 Position, TEvent Event)
    : IPropagatingEvent, IReportedEvent
    where TEvent : struct, IPointerEvent
{
    /// <inheritdoc/>
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.WatchPointer(PointerEvents.KindOf<TEvent>());
}
