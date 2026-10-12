using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One connected gamepad as the bridge copies it out, mirroring <c>BcsGamepad</c>.</summary>
/// <remarks>
/// The button bits are numbered as <see cref="GamepadButton"/> declares them, which is the order the
/// bridge's <c>BUTTONS</c> table pins, so Bevy's own enum growing moves neither.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeGamepad
{
    /// <summary>Bytes of a name one snapshot carries.</summary>
    public const int NameCapacity = 48;

    /// <summary>The pad's entity.</summary>
    public ulong Entity;

    /// <summary>Bit per button held.</summary>
    public uint Down;

    /// <summary>Bit per button that went down this frame.</summary>
    public uint Pressed;

    /// <summary>Bit per button that came up this frame.</summary>
    public uint Released;

    /// <summary>The sticks' X and Y, left then right, then the left and right triggers.</summary>
    public fixed float Axes[6];

    /// <summary>The USB vendor id, or zero.</summary>
    public ushort Vendor;

    /// <summary>The USB product id, or zero.</summary>
    public ushort Product;

    /// <summary>Bytes of <see cref="Name"/> in use.</summary>
    public uint NameLength;

    /// <summary>The pad's name, UTF-8.</summary>
    public fixed byte Name[NameCapacity];
}
