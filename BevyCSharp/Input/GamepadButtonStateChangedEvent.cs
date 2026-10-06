namespace Bevy;

/// <summary>A pad's button coming to count as pressed or released, as Bevy's <c>GamepadButtonStateChangedEvent</c> message carries it.</summary>
/// <param name="Gamepad">The pad's entity, Bevy's <c>entity</c>.</param>
/// <param name="Button">Which button.</param>
/// <param name="State">Whether it went down or came up.</param>
/// <remarks>
/// Sent once where a button passes the point that counts as pressed, where
/// <see cref="GamepadButtonChangedEvent"/> is sent for each value it moves through. Read with
/// <c>ctx.Read&lt;GamepadButtonStateChangedEvent&gt;()</c>.
/// </remarks>
public readonly record struct GamepadButtonStateChangedEvent(Entity Gamepad, GamepadButton Button, ButtonState State);
