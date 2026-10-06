namespace Bevy;

/// <summary>Something dragged was let go over the entity.</summary>
/// <param name="Button">Which button.</param>
/// <param name="Dropped">What was dropped.</param>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <remarks>
/// Bevy's <c>DragDrop</c>, sent to the entity dropped on, while the one dropped hears <see
/// cref="DragEnd"/>.
/// </remarks>
public readonly record struct DragDrop(PointerButton Button, Entity Dropped, PointerHit Hit) : IPointerEvent;
