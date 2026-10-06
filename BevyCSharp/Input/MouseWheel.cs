namespace Bevy;

/// <summary>The wheel turning or a touchpad scrolling, as Bevy's <c>MouseWheel</c> message carries it.</summary>
/// <param name="Unit">Whether it moved by lines, as a wheel's notches do, or by pixels, as a touchpad does.</param>
/// <param name="X">How far across.</param>
/// <param name="Y">How far up, positive away from the hand.</param>
/// <param name="Window">The window it happened over.</param>
/// <param name="Phase">Where a touchpad's scroll is in its life, which a wheel reports as moved.</param>
/// <remarks>Read with <c>ctx.Read&lt;MouseWheel&gt;()</c>.</remarks>
public readonly record struct MouseWheel(ScrollUnit Unit, float X, float Y, Entity Window, TouchPhase Phase);
