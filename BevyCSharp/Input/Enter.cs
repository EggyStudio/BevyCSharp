namespace Bevy;

/// <summary>
/// The pointer came over the entity or one of its children, having been over neither.
/// </summary>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <param name="IsInBounds">
/// Whether the pointer is over the entity's own bounds, rather than a child's out past them.
/// </param>
/// <remarks>
/// Bevy's <c>Enter</c>. Unlike <see cref="Over"/>, moving from the entity onto a child of it is no
/// new enter, so a panel hears one when the pointer comes in and one <see cref="Leave"/> when it
/// goes, whatever is inside.
/// </remarks>
public readonly record struct Enter(PointerHit Hit, bool IsInBounds) : IPointerEvent;
