using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>What a Feathers control is made with, the bridge's <c>BcsFeathersProps</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeFeathersProps
{
    /// <summary>A button's look, as <see cref="ButtonVariant"/> numbers it.</summary>
    public int Variant;

    /// <summary>Which corners are rounded, as <see cref="RoundedCorners"/> numbers them.</summary>
    public int Corners;

    /// <summary>A slider's least value.</summary>
    public float Min;

    /// <summary>A slider's greatest value.</summary>
    public float Max;

    /// <summary>A theme token the control is colored by, as UTF-8, or null for its own.</summary>
    public byte* Token;

    /// <summary>How many bytes <see cref="Token"/> holds.</summary>
    public int TokenLength;

    /// <summary>A color slider's value, or the percentage of a swatch or a color input shown opaque.</summary>
    public float Value;

    /// <summary>A text input's width in characters, a dialog's width, or a swatch's corner radius, zero for the control's own.</summary>
    public float Extent;

    /// <summary>A select's rows shown, a text input's most characters, a color slider's channel or a plane's axes.</summary>
    public int Count;

    /// <summary>The row chosen among a select's or a list view's, or -1 for none.</summary>
    public int Selected;

    /// <summary>A menu button's arrow, or an icon left untinted, where non-zero.</summary>
    public uint Flag;

    /// <summary>A floating dialog's left edge, in pixels.</summary>
    public float Left;

    /// <summary>A floating dialog's top edge, in pixels.</summary>
    public float Top;
}
