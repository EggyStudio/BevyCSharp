using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One of Bevy's logical keys as the bridge copies it out.</summary>
/// <remarks>
/// The bridge's <c>BcsLogicalKey</c>, field for field, which <c>InputTests</c> holds to the offsets
/// the bridge asserts.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeLogicalKey
{
    /// <summary>How many bytes <see cref="Text"/> holds.</summary>
    public const int TextCapacity = 28;

    /// <summary>One while held, two on the frame it went down, four on the frame it came up, added.</summary>
    public byte Flags;

    /// <summary>What kind of key, as <see cref="LogicalKeyKind"/> numbers them.</summary>
    public byte Kind;

    /// <summary>How many bytes of <see cref="Text"/> are its name or its character.</summary>
    public ushort Length;

    /// <summary>Its name or its character, in UTF-8.</summary>
    public fixed byte Text[TextCapacity];
}
