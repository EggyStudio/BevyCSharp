namespace Bevy;

/// <summary>A drag of the entity began, a button held on it and the pointer moved.</summary>
/// <param name="Button">Which button.</param>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <remarks>Bevy's <c>DragStart</c>.</remarks>
public readonly record struct DragStart(PointerButton Button, PointerHit Hit) : IPointerEvent;
