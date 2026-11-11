namespace Bevy;

/// <summary>A pad connecting, a button moving or an axis moving, in the one order they came, as Bevy's <c>GamepadEvent</c> message carries them.</summary>
/// <param name="Connection">The pad connecting or disconnecting, where it did.</param>
/// <param name="Button">A button moving, where one did.</param>
/// <param name="Axis">A stick's axis moving, where one did.</param>
/// <remarks>
/// One of the three holds something. Read with <c>ctx.Read&lt;GamepadEvent&gt;()</c> where the
/// order between kinds matters, as a button pressed and then a stick moved, which the three
/// messages of their own do not keep.
/// </remarks>
public readonly record struct GamepadEvent(GamepadConnectionEvent? Connection, GamepadButtonChangedEvent? Button, GamepadAxisChangedEvent? Axis);
