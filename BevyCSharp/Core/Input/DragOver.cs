namespace Bevy;

/// <summary>Something dragged moved over the entity.</summary>
/// <param name="Button">Which button.</param>
/// <param name="Dragged">What is being dragged.</param>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <remarks>Bevy's <c>DragOver</c>.</remarks>
public readonly record struct DragOver(PointerButton Button, Entity Dragged, PointerHit Hit) : IPointerEvent;
