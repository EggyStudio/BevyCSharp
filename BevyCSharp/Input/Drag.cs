namespace Bevy;

/// <summary>The pointer moved while dragging the entity.</summary>
/// <param name="Button">Which button.</param>
/// <param name="Distance">How far it is from where the drag began, in logical pixels.</param>
/// <param name="Delta">How far it moved since the last one, in logical pixels.</param>
/// <remarks>
/// Bevy's <c>Drag</c>. Sent to the entity dragged wherever the pointer is, so it follows the
/// pointer off the entity.
/// </remarks>
public readonly record struct Drag(PointerButton Button, Vec2 Distance, Vec2 Delta) : IPointerEvent;
