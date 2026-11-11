namespace Bevy;

/// <summary>A pad's button moving, a trigger's travel among them, as Bevy's <c>GamepadButtonChangedEvent</c> message carries it.</summary>
/// <param name="Gamepad">The pad's entity, Bevy's <c>entity</c>.</param>
/// <param name="Button">Which button.</param>
/// <param name="State">Whether it counts as pressed at this value.</param>
/// <param name="Value">How far down, from zero to one.</param>
/// <remarks>Read with <c>ctx.Read&lt;GamepadButtonChangedEvent&gt;()</c>.</remarks>
public readonly record struct GamepadButtonChangedEvent(Entity Gamepad, GamepadButton Button, ButtonState State, float Value);
