namespace Bevy;

/// <summary>The mouse moving, as Bevy's <c>MouseMotion</c> message carries it.</summary>
/// <param name="Delta">How far, in the mouse's own units, which follow the hand rather than the cursor and go on past the window's edge.</param>
/// <remarks>
/// What a camera that turns with the mouse reads, since the cursor stops at the edge of the screen
/// where the hand does not. Read with <c>ctx.Read&lt;MouseMotion&gt;()</c>.
/// </remarks>
public readonly record struct MouseMotion(Vec2 Delta);
