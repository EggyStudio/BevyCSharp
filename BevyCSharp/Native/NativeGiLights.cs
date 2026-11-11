using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's <c>BcsGiLights</c>, the lights global illumination's probes are lit by, laid out as
/// the start of <c>bcs_gi</c>'s <c>GiLights</c>, which the probes' buffer is written from as it is.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = Bytes)]
internal struct NativeGiLights
{
    /// <summary>The bytes of the sun, the sky, the counts and sixteen lights of sixty-four bytes.</summary>
    public const int Bytes = 64 + 16 * 64;

    /// <summary>How many point and spot lights, as a float, the counts' first.</summary>
    [FieldOffset(48)] public float Count;
}
