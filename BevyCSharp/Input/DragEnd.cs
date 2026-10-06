namespace Bevy;

/// <summary>A drag of the entity ended, its button let go.</summary>
/// <param name="Button">Which button.</param>
/// <param name="Distance">
/// How far the pointer went from where the drag began, in logical pixels.
/// </param>
/// <remarks>Bevy's <c>DragEnd</c>.</remarks>
public readonly record struct DragEnd(PointerButton Button, Vec2 Distance) : IPointerEvent;
