using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's <c>DenoiserSettings</c>, what its denoiser over ray-traced lighting filters by, the
/// history's clamp and the widths of the three weights.
/// </summary>
/// <remarks>
/// Four floats in the bridge's order, which lie at the same offsets on every platform, and a test
/// on the bridge's side holds them there.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeDenoiserSettings
{
    /// <summary>How many frames the history holds at most.</summary>
    public float HistoryLimit;

    /// <summary>The depth weight's width, the paper's sigma z.</summary>
    public float SigmaDepth;

    /// <summary>The normal weight's power, the paper's sigma n.</summary>
    public float SigmaNormal;

    /// <summary>The luminance weight's width, the paper's sigma l.</summary>
    public float SigmaLuminance;
}
