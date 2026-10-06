namespace Bevy;

/// <summary>Which pointer did something, as Bevy's <c>PointerId</c>.</summary>
/// <param name="Kind">
/// The mouse, a finger on a touch screen, or a pointer of the game's own.
/// </param>
/// <param name="Number">
/// Which finger, for a touch, as Bevy numbers them, and the last eight bytes of its identifier for
/// a pointer of the game's own, which for one from <see cref="Picking.SpawnPointer"/> is the
/// number it was given. Zero for the mouse.
/// </param>
public readonly record struct PointerId(PointerKind Kind, ulong Number);
