namespace Bevy;

/// <summary>A finger touching, moving on or leaving the screen, as Bevy's <c>TouchInput</c> message carries it.</summary>
/// <param name="Phase">Started, moved, ended, or canceled where the platform took the touch away.</param>
/// <param name="Position">Where, in logical pixels from the window's top left corner.</param>
/// <param name="Window">The window.</param>
/// <param name="Force">How hard, where the platform says, or null.</param>
/// <param name="Id">Which finger, the same for as long as it stays down.</param>
/// <remarks>
/// Read with <c>ctx.Read&lt;TouchInput&gt;()</c>, each change in the order it came, where
/// <see cref="Input.Touches"/> holds each finger as the frame leaves it.
/// </remarks>
public readonly record struct TouchInput(TouchPhase Phase, Vec2 Position, Entity Window, TouchForce? Force, ulong Id);
