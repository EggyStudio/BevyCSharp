namespace Bevy;

/// <summary>A button came up with the pointer over the entity.</summary>
/// <param name="Button">Which button.</param>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <remarks>Bevy's <c>Release</c>, whether or not it went down over the same entity.</remarks>
public readonly record struct Release(PointerButton Button, PointerHit Hit) : IPointerEvent;
