using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>Sent the frame a gamepad is connected.</summary>
/// <param name="Gamepad">The pad's entity, which <see cref="Bevy.Gamepad.Entity"/> also gives.</param>
/// <param name="Name">What the platform calls it.</param>
public readonly record struct GamepadConnected(Entity Gamepad, string Name);
