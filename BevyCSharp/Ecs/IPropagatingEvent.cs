namespace Bevy;

/// <summary>
/// An entity event that goes on up to the entity's parent, and its parent's, after the entity's own
/// observers, until one of them stops it or it reaches the root.
/// </summary>
/// <remarks>
/// Bevy's <c>#[entity_event(propagate, auto_propagate)]</c>, for an event such as an attack on a
/// piece of armor, which the armor may block before the body it hangs from feels it.
/// <see cref="On{TEvent}.Propagate"/> stops it.
/// </remarks>
public interface IPropagatingEvent : IEntityEvent;
