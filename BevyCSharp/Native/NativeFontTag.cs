using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One OpenType tag and its value, the bridge's <c>BcsFontTag</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeFontTag
{
    /// <summary>The tag's four ASCII characters.</summary>
    public fixed byte Tag[4];

    /// <summary>Its value.</summary>
    public float Value;
}
