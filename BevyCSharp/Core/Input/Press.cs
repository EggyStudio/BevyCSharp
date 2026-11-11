namespace Bevy;

/// <summary>A button went down with the pointer over the entity.</summary>
/// <param name="Button">Which button.</param>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <param name="Count">
/// How many presses in a row this is, two for the second of a double click.
/// </param>
/// <remarks>Bevy's <c>Press</c>.</remarks>
public readonly record struct Press(PointerButton Button, PointerHit Hit, int Count) : IPointerEvent;
