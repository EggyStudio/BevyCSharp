namespace Bevy;

/// <summary>
/// The pointer came over the entity, from off it or from something nearer it went off.
/// </summary>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <remarks>
/// Bevy's <c>Over</c>. Sent to the entity under the pointer and up its parents, so a parent hears
/// it each time the pointer comes over a child, which <see cref="Enter"/> does not repeat.
/// </remarks>
public readonly record struct Over(PointerHit Hit) : IPointerEvent;
