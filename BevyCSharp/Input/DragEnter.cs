namespace Bevy;

/// <summary>Something dragged came over the entity.</summary>
/// <param name="Button">Which button.</param>
/// <param name="Dragged">What is being dragged.</param>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <remarks>Bevy's <c>DragEnter</c>, for a place a thing can be dropped.</remarks>
public readonly record struct DragEnter(PointerButton Button, Entity Dragged, PointerHit Hit) : IPointerEvent;
