using System.Runtime.InteropServices;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The managed mirror of the settings a standard material is made with has to agree with the
/// bridge's field by field.
/// </summary>
/// <remarks>
/// The struct grows at its end as the material gains settings, and a field added on one side in
/// another place than on the other reads every field after it from its neighbor while the two can
/// still measure the same. The fields checked are those either side of each group, and the same
/// numbers are asserted on the Rust side, so neither half can move without the other.
/// </remarks>
public sealed class NativeMaterialLayoutTests
{
    [Theory]
    [InlineData(nameof(NativeMaterialConfig.EmissiveR), 24)]
    [InlineData(nameof(NativeMaterialConfig.UvScaleX), 76)]
    [InlineData(nameof(NativeMaterialConfig.AttenuationR), 128)]
    [InlineData(nameof(NativeMaterialConfig.LightmapExposure), 180)]
    [InlineData(nameof(NativeMaterialConfig.DepthMap), 184)]
    [InlineData(nameof(NativeMaterialConfig.ReliefSteps), 196)]
    [InlineData(nameof(NativeMaterialConfig.SpecularTintR), 204)]
    [InlineData(nameof(NativeMaterialConfig.OpaqueRenderMethod), 228)]
    public void EveryFieldSitsWhereTheBridgePutIt(string field, int offset) =>
        Assert.Equal(offset, Marshal.OffsetOf<NativeMaterialConfig>(field).ToInt32());

    [Fact]
    public void TheWholeConfigIsTheSizeTheBridgeReads() =>
        Assert.Equal(232, Marshal.SizeOf<NativeMaterialConfig>());
}
