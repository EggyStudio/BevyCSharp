using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>What a Feathers control is made with, the bridge's <c>BcsFeathersProps</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeFeathersProps
{
    /// <summary>A button's look, as <see cref="ButtonVariant"/> numbers it.</summary>
    public int Variant;

    /// <summary>Which corners are rounded, as <see cref="RoundedCorners"/> numbers them.</summary>
    public int Corners;

    /// <summary>A slider's least value.</summary>
    public float Min;

    /// <summary>A slider's greatest value.</summary>
    public float Max;
}
