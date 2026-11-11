namespace Bevy;

/// <summary>The pointer moved while over the entity.</summary>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <param name="Delta">How far it moved, in logical pixels.</param>
/// <remarks>Bevy's <c>Move</c>.</remarks>
public readonly record struct Move(PointerHit Hit, Vec2 Delta) : IPointerEvent;
