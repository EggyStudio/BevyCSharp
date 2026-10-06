using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One of Bevy's input messages as the bridge drains it. Mirrors <c>BcsInputMessage</c>.</summary>
/// <remarks>
/// What each field holds depends on <see cref="Kind"/>, which <c>App.PostInputMessages</c> reads
/// each kind's fields from. One shape for every kind, so a frame's messages cross in one call in
/// the order the bridge read them.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeInputMessage
{
    /// <summary>How many bytes <see cref="Logical"/> and <see cref="Text"/> each hold.</summary>
    public const int TextCapacity = 28;

    /// <summary>How many bytes <see cref="Name"/> holds.</summary>
    public const int NameCapacity = 48;

    /// <summary>Which message, as the bridge's <c>Kind</c> numbers them.</summary>
    public int Kind;

    /// <summary>The key, the mouse button, the scroll unit, the touch's phase, or the pad's button or axis, by kind.</summary>
    public int Code;

    /// <summary>One for a press and two for a release, or a wheel's phase.</summary>
    public int State;

    /// <summary>Which of the optional fields hold something.</summary>
    public uint Flags;

    /// <summary>The window, or the pad.</summary>
    public ulong Entity;

    /// <summary>A touch's finger, or a pad's vendor and product.</summary>
    public ulong Id;

    /// <summary>A position's or a delta's across, a wheel's, or a value.</summary>
    public float X;

    /// <summary>A position's or a delta's down, or a wheel's.</summary>
    public float Y;

    /// <summary>A cursor's delta across, or a touch's force.</summary>
    public float Z;

    /// <summary>A cursor's delta down, or the most force a touch can have.</summary>
    public float W;

    /// <summary>A stylus's altitude.</summary>
    public float U;

    /// <summary>A key's logical kind, as <see cref="NativeFocusedKey.LogicalKind"/> holds it.</summary>
    public byte LogicalKind;

    /// <summary>How many bytes of <see cref="Logical"/> are the logical key.</summary>
    public byte LogicalLength;

    /// <summary>How many bytes of <see cref="Text"/> the key typed.</summary>
    public byte TextLength;

    /// <summary>How many bytes of <see cref="Name"/> are the pad's name.</summary>
    public byte NameLength;

    /// <summary>The logical key's name or character, in UTF-8.</summary>
    public fixed byte Logical[TextCapacity];

    /// <summary>What the key typed, in UTF-8.</summary>
    public fixed byte Text[TextCapacity];

    /// <summary>The pad's name as it connected, in UTF-8.</summary>
    public fixed byte Name[NameCapacity];
}
