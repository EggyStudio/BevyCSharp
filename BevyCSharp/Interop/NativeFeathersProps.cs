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
}
