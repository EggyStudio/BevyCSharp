namespace Bevy;

/// <summary>The pointer went off the entity and all its children.</summary>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <param name="WasInBounds">
/// Whether the pointer was over the entity's own bounds before it went.
/// </param>
/// <remarks>Bevy's <c>Leave</c>, the other half of <see cref="Enter"/>.</remarks>
public readonly record struct Leave(PointerHit Hit, bool WasInBounds) : IPointerEvent;
