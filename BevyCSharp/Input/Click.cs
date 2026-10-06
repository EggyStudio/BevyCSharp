namespace Bevy;

/// <summary>A button went down and came up again over the entity.</summary>
/// <param name="Button">Which button.</param>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <param name="Duration">How long it was held.</param>
/// <param name="Count">How many clicks in a row this is, two for a double click.</param>
/// <remarks>
/// Bevy's <c>Click</c>. A press over one entity and a release over another clicks neither.
/// </remarks>
public readonly record struct Click(PointerButton Button, PointerHit Hit, TimeSpan Duration, int Count) : IPointerEvent;
