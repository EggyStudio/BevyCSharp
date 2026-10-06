namespace Bevy;

/// <summary>The pointer went off the entity, onto something else or off everything.</summary>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <remarks>Bevy's <c>Out</c>, the other half of <see cref="Over"/>.</remarks>
public readonly record struct Out(PointerHit Hit) : IPointerEvent;
