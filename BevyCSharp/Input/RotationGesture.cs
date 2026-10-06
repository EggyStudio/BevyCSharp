namespace Bevy;

/// <summary>Two fingers turning on a touchpad, as Bevy's <c>RotationGesture</c> message carries it.</summary>
/// <param name="Delta">How far, in radians, positive counterclockwise.</param>
/// <remarks>Sent on macOS and iOS, and read with <c>ctx.Read&lt;RotationGesture&gt;()</c>.</remarks>
public readonly record struct RotationGesture(float Delta);
