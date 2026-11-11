namespace Bevy;

/// <summary>The pointer scrolled while over the entity.</summary>
/// <param name="Unit">What the amounts are counted in.</param>
/// <param name="X">How far across.</param>
/// <param name="Y">How far up and down, up being more.</param>
/// <param name="Hit">Where the pointer met the entity.</param>
/// <remarks>Bevy's <c>Scroll</c>, from a mouse wheel or a touchpad.</remarks>
public readonly record struct Scroll(ScrollUnit Unit, float X, float Y, PointerHit Hit) : IPointerEvent;
