namespace Bevy;

/// <summary>Two fingers pinching on a touchpad, as Bevy's <c>PinchGesture</c> message carries it.</summary>
/// <param name="Delta">How much, positive spreading apart and negative drawing together.</param>
/// <remarks>Sent on macOS and iOS, and read with <c>ctx.Read&lt;PinchGesture&gt;()</c>.</remarks>
public readonly record struct PinchGesture(float Delta);
