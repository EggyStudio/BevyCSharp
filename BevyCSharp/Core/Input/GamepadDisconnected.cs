using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>Sent the frame a gamepad is disconnected.</summary>
/// <param name="Gamepad">The entity the pad had.</param>
public readonly record struct GamepadDisconnected(Entity Gamepad);
