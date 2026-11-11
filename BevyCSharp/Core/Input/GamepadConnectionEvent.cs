namespace Bevy;

/// <summary>A pad connecting or disconnecting, as Bevy's <c>GamepadConnectionEvent</c> message carries it.</summary>
/// <param name="Gamepad">The pad's entity.</param>
/// <param name="Connected">Whether it connected rather than disconnected.</param>
/// <param name="Name">What the platform calls it, where it connected.</param>
/// <param name="VendorId">Its USB vendor, where the platform says.</param>
/// <param name="ProductId">Its USB product, where the platform says.</param>
/// <remarks>
/// Read with <c>ctx.Read&lt;GamepadConnectionEvent&gt;()</c>. <see cref="GamepadConnected"/> and
/// <see cref="GamepadDisconnected"/> say the same with less, sent as the frame's pads are read.
/// </remarks>
public readonly record struct GamepadConnectionEvent(Entity Gamepad, bool Connected, string? Name, ushort? VendorId, ushort? ProductId);
