namespace Bevy;

/// <summary>A pad's stick moving, as Bevy's <c>GamepadAxisChangedEvent</c> message carries it.</summary>
/// <param name="Gamepad">The pad's entity, Bevy's <c>entity</c>.</param>
/// <param name="Axis">Which stick's axis, Bevy reporting a trigger as a button.</param>
/// <param name="Value">Where it is now, from minus one to one.</param>
/// <remarks>Read with <c>ctx.Read&lt;GamepadAxisChangedEvent&gt;()</c>. An axis beyond the sticks' four sends none.</remarks>
public readonly record struct GamepadAxisChangedEvent(Entity Gamepad, GamepadAxis Axis, float Value);
