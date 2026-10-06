using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>A key as it reached the focused entity, as the bridge reports it.</summary>
/// <remarks>
/// The bridge's <c>BcsFocusedKey</c>, field for field, which <c>InputTests</c> holds to the
/// offsets the bridge asserts.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeFocusedKey
{
    /// <summary>How many bytes <see cref="Logical"/> and <see cref="Text"/> each hold.</summary>
    public const int TextCapacity = 28;

    /// <summary>The entity with the focus.</summary>
    public ulong Entity;

    /// <summary>The physical key's place in the key table, or -1 for one outside it.</summary>
    public int Key;

    /// <summary>One for a press and two for a release.</summary>
    public int State;

    /// <summary>One where the platform repeats a held key.</summary>
    public uint Repeat;

    /// <summary>The logical key's kind, as <see cref="LogicalKeyKind"/> numbers them, or three for one the platform could not identify.</summary>
    public byte LogicalKind;

    /// <summary>How many bytes of <see cref="Logical"/> are its name or character.</summary>
    public byte LogicalLength;

    /// <summary>How many bytes of <see cref="Text"/> it typed.</summary>
    public byte TextLength;

    /// <summary>One where it typed something.</summary>
    public byte HasText;

    /// <summary>The logical key's name or character, in UTF-8.</summary>
    public fixed byte Logical[TextCapacity];

    /// <summary>What it typed, in UTF-8.</summary>
    public fixed byte Text[TextCapacity];
}
