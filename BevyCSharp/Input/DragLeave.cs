namespace Bevy;

/// <summary>Something dragged went off the entity.</summary>
/// <param name="Button">Which button.</param>
/// <param name="Dragged">What is being dragged.</param>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <remarks>Bevy's <c>DragLeave</c>.</remarks>
public readonly record struct DragLeave(PointerButton Button, Entity Dragged, PointerHit Hit) : IPointerEvent;
