using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>A gamepad's buttons, as Bevy names them.</summary>
/// <remarks>
/// In the bridge's order, which is Bevy's list of standard buttons, so a button's bit means the same
/// on both sides. The face buttons are named by where they sit, since a pad's labels differ
/// by maker, so <see cref="South"/> is A on an Xbox pad and Cross on a PlayStation one.
/// </remarks>
public enum GamepadButton
{
    /// <summary>The bottom face button, A or Cross.</summary>
    South,

    /// <summary>The right face button, B or Circle.</summary>
    East,

    /// <summary>The top face button, Y or Triangle.</summary>
    North,

    /// <summary>The left face button, X or Square.</summary>
    West,

    /// <summary>The C button, on the pads that have one.</summary>
    C,

    /// <summary>The Z button, on the pads that have one.</summary>
    Z,

    /// <summary>The left shoulder button, LB or L1.</summary>
    LeftTrigger,

    /// <summary>The left trigger, LT or L2, down past three quarters of its travel.</summary>
    LeftTrigger2,

    /// <summary>The right shoulder button, RB or R1.</summary>
    RightTrigger,

    /// <summary>The right trigger, RT or R2, down past three quarters of its travel.</summary>
    RightTrigger2,

    /// <summary>The select or back button.</summary>
    Select,

    /// <summary>The start or menu button.</summary>
    Start,

    /// <summary>The mode or home button.</summary>
    Mode,

    /// <summary>The left stick pressed in.</summary>
    LeftThumb,

    /// <summary>The right stick pressed in.</summary>
    RightThumb,

    /// <summary>Up on the directional pad.</summary>
    DPadUp,

    /// <summary>Down on the directional pad.</summary>
    DPadDown,

    /// <summary>Left on the directional pad.</summary>
    DPadLeft,

    /// <summary>Right on the directional pad.</summary>
    DPadRight,
}

/// <summary>A gamepad's analog inputs.</summary>
public enum GamepadAxis
{
    /// <summary>The left stick across, minus one to one, right positive.</summary>
    LeftX,

    /// <summary>The left stick up and down, minus one to one, up positive.</summary>
    LeftY,

    /// <summary>The right stick across, minus one to one, right positive.</summary>
    RightX,

    /// <summary>The right stick up and down, minus one to one, up positive.</summary>
    RightY,

    /// <summary>How far the left trigger is down, zero to one.</summary>
    LeftTrigger,

    /// <summary>How far the right trigger is down, zero to one.</summary>
    RightTrigger,
}

/// <summary>Sent the frame a gamepad is connected.</summary>
/// <param name="Gamepad">The pad's entity, which <see cref="Bevy.Gamepad.Entity"/> also gives.</param>
/// <param name="Name">What the platform calls it.</param>
public readonly record struct GamepadConnected(Entity Gamepad, string Name);

/// <summary>Sent the frame a gamepad is disconnected.</summary>
/// <param name="Gamepad">The entity the pad had.</param>
public readonly record struct GamepadDisconnected(Entity Gamepad);

/// <summary>One connected gamepad as it stood at the start of this frame.</summary>
/// <remarks>
/// <para>
/// Read through <see cref="Input.Gamepads"/>, a pad a player plugged in, in the order Bevy
/// found them. A pad is an entity of Bevy's, which stays the same while it is connected, so a game
/// that gives each player a pad keeps the pad's <see cref="Entity"/>.
/// </para>
/// <para>
/// The sticks come through Bevy's dead zones, so a stick at rest reads zero rather than drifting.
/// A trigger is both an axis, how far it is down, and a button, down past three quarters.
/// </para>
/// <para>
/// Real pads are found by gilrs, which the render and editor profiles carry. Every profile reads a
/// pad pretended by <see cref="SyntheticInput.ConnectGamepad"/>, which is how a script or a test
/// presses a button on a machine with no pad attached.
/// </para>
/// </remarks>
public sealed class Gamepad
{
    private readonly uint _down;
    private readonly uint _pressed;
    private readonly uint _released;
    private readonly float[] _axes;

    internal unsafe Gamepad(in NativeGamepad native)
    {
        Entity = new Entity(native.Entity);
        _down = native.Down;
        _pressed = native.Pressed;
        _released = native.Released;
        _axes = new float[6];
        for (var i = 0; i < 6; i++) _axes[i] = native.Axes[i];
        Vendor = native.Vendor;
        Product = native.Product;

        var length = (int)Math.Min(native.NameLength, (uint)NativeGamepad.NameCapacity);
        fixed (byte* name = native.Name) Name = Encoding.UTF8.GetString(name, length);
    }

    /// <summary>The pad's entity, the same while it stays connected.</summary>
    public Entity Entity { get; }

    /// <summary>What the platform calls it, such as "Xbox Wireless Controller".</summary>
    public string Name { get; }

    /// <summary>The USB vendor id, or zero where the platform did not say.</summary>
    public ushort Vendor { get; }

    /// <summary>The USB product id, or zero where the platform did not say.</summary>
    public ushort Product { get; }

    /// <summary>Whether a button is held.</summary>
    public bool Down(GamepadButton button) => (_down & Bit(button)) != 0;

    /// <summary>Whether a button went down this frame.</summary>
    public bool Pressed(GamepadButton button) => (_pressed & Bit(button)) != 0;

    /// <summary>Whether a button came up this frame.</summary>
    public bool Released(GamepadButton button) => (_released & Bit(button)) != 0;

    /// <summary>Whether any button is held.</summary>
    public bool AnyDown => _down != 0;

    /// <summary>Where an axis stands, a stick from minus one to one and a trigger from zero to one.</summary>
    public float Axis(GamepadAxis axis) => (uint)axis < (uint)_axes.Length ? _axes[(int)axis] : 0f;

    /// <summary>The left stick, right and up positive.</summary>
    public Vec2 LeftStick => new(_axes[0], _axes[1]);

    /// <summary>The right stick, right and up positive.</summary>
    public Vec2 RightStick => new(_axes[2], _axes[3]);

    /// <summary>
    /// Rumbles the pad, its strong motor and its weak one each from zero to one, for some seconds.
    /// </summary>
    /// <remarks>
    /// Rumbles add up, so two at half strength rumble at full until the shorter ends, and
    /// <see cref="StopRumble"/> clears them before one meant to replace them. A pad with no motors,
    /// a pretended pad, and every pad on a profile without gilrs take the request and do nothing.
    /// </remarks>
    /// <exception cref="BevyNativeException">The pad has been disconnected.</exception>
    public void Rumble(float strong, float weak, float seconds) =>
        Native.Check(Native.bcs_gamepad_rumble(Entity.Bits, strong, weak, Math.Max(seconds, 1e-3f)), $"rumbling {Name}");

    /// <summary>Stops every rumble the pad is playing.</summary>
    /// <exception cref="BevyNativeException">The pad has been disconnected.</exception>
    public void StopRumble() => Native.Check(Native.bcs_gamepad_rumble(Entity.Bits, 0f, 0f, 0f), $"stopping {Name}'s rumble");

    /// <inheritdoc/>
    public override string ToString() => $"{Name} ({Entity})";

    private static uint Bit(GamepadButton button) => (uint)button < 32 ? 1u << (int)button : 0u;
}
