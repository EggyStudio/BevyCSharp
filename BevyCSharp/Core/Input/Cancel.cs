namespace Bevy;

/// <summary>
/// The pointer stopped being followed while over the entity, as a touch the system took away.
/// </summary>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <remarks>Bevy's <c>Cancel</c>.</remarks>
public readonly record struct Cancel(PointerHit Hit) : IPointerEvent;
