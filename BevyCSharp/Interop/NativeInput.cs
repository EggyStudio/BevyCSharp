using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// Input state mirrored from Bevy's <c>ButtonInput</c> and accumulated-mouse resources.
/// </summary>
/// <remarks>
/// Keyboard state arrives as a bitset indexed by <see cref="Key"/>. The bit order is pinned by
/// the key table in the native crate; <see cref="Key"/> declares the identical order.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeInput
{
    /// <summary>Number of 64-bit words in each keyboard bitset.</summary>
    public const int KeyWords = 2;

    /// <summary>Cursor X in physical window pixels.</summary>
    public float MouseX;

    /// <summary>Cursor Y in physical window pixels.</summary>
    public float MouseY;

    /// <summary>Cursor X movement since the previous frame.</summary>
    public float MouseDeltaX;

    /// <summary>Cursor Y movement since the previous frame.</summary>
    public float MouseDeltaY;

    /// <summary>Horizontal scroll since the previous frame.</summary>
    public float WheelX;

    /// <summary>Vertical scroll since the previous frame.</summary>
    public float WheelY;

    /// <summary>Bit per key currently held.</summary>
    public fixed ulong KeysDown[KeyWords];

    /// <summary>Bit per key that went down this frame.</summary>
    public fixed ulong KeysPressed[KeyWords];

    /// <summary>Bit per key that went up this frame.</summary>
    public fixed ulong KeysReleased[KeyWords];

    /// <summary>Bytes of typed text one frame can carry.</summary>
    public const int TextCapacity = 32;

    /// <summary>Simultaneous touches one frame can carry.</summary>
    public const int TouchCapacity = 8;

    /// <summary>Bit per mouse button currently held.</summary>
    public uint MouseDown;

    /// <summary>Bit per mouse button that went down this frame.</summary>
    public uint MousePressed;

    /// <summary>Bit per mouse button that went up this frame.</summary>
    public uint MouseReleased;

    /// <summary>Bytes of <see cref="Text"/> that are in use.</summary>
    public uint TextLength;

    /// <summary>Entries of <see cref="Touches"/> that are in use.</summary>
    public uint TouchCount;

    /// <summary>What the keyboard produced this frame, as UTF-8.</summary>
    public fixed byte Text[TextCapacity];

    /// <summary>Touches in progress.</summary>
    public NativeTouchArray Touches;
}
