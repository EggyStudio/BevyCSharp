namespace Bevy;

/// <summary>Two fingers tapping a touchpad twice, as Bevy's <c>DoubleTapGesture</c> message carries it.</summary>
/// <remarks>Sent on macOS, and read with <c>ctx.Read&lt;DoubleTapGesture&gt;()</c>.</remarks>
public readonly record struct DoubleTapGesture;
