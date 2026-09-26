using System.Runtime.InteropServices;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Pins the managed mirrors of the shader configs to the offsets the bridge's own tests pin the
/// Rust side to.
/// </summary>
/// <remarks>
/// A struct that is the right size can still put a field somewhere else, and across the boundary
/// that reads as a different number rather than as an error. The same numbers are asserted in
/// <c>native/bevy_csharp/src/render/shaders.rs</c>, so the two sides can only drift apart by both
/// tests being changed.
/// </remarks>
public sealed class ShaderLayoutTests
{
    [Theory]
    [InlineData(nameof(NativeShaderProgramConfig.Fragment), 24)]
    [InlineData(nameof(NativeShaderProgramConfig.Compute), 96)]
    [InlineData(nameof(NativeShaderProgramConfig.Pass), 120)]
    [InlineData(nameof(NativeShaderProgramConfig.Defines), 144)]
    [InlineData(nameof(NativeShaderProgramConfig.DefineCount), 152)]
    public void TheProgramConfigIsWhereTheBridgeReadsIt(string field, int offset) =>
        Assert.Equal(offset, Marshal.OffsetOf<NativeShaderProgramConfig>(field).ToInt32());

    [Theory]
    [InlineData(nameof(NativeSamplerConfig.Linear), 12)]
    [InlineData(nameof(NativeSamplerConfig.Anisotropy), 24)]
    public void TheSamplerConfigIsWhereTheBridgeReadsIt(string field, int offset) =>
        Assert.Equal(offset, Marshal.OffsetOf<NativeSamplerConfig>(field).ToInt32());

    [Theory]
    [InlineData(nameof(NativeViewImage.Format), 8)]
    [InlineData(nameof(NativeViewImage.Mips), 20)]
    [InlineData(nameof(NativeViewImage.Copy), 24)]
    [InlineData(nameof(NativeViewImage.Clear), 28)]
    public void TheViewImageIsWhereTheBridgeReadsIt(string field, int offset) =>
        Assert.Equal(offset, Marshal.OffsetOf<NativeViewImage>(field).ToInt32());

    [Theory]
    [InlineData(nameof(NativeViewDispatch.Groups), 12)]
    [InlineData(nameof(NativeViewDispatch.Scale), 24)]
    [InlineData(nameof(NativeViewDispatch.Offset), 32)]
    public void TheViewDispatchIsWhereTheBridgeReadsIt(string field, int offset) =>
        Assert.Equal(offset, Marshal.OffsetOf<NativeViewDispatch>(field).ToInt32());

    [Fact]
    public void EachConfigIsTheSizeTheBridgeReads()
    {
        Assert.Equal(208, Marshal.SizeOf<NativeShaderProgramConfig>());
        Assert.Equal(160, Marshal.OffsetOf<NativeShaderProgramConfig>(nameof(NativeShaderProgramConfig.DrawVertex)).ToInt32());
        Assert.Equal(184, Marshal.OffsetOf<NativeShaderProgramConfig>(nameof(NativeShaderProgramConfig.DrawFragment)).ToInt32());
        Assert.Equal(48, Marshal.SizeOf<NativeViewDraw>());
        Assert.Equal(32, Marshal.OffsetOf<NativeViewDraw>(nameof(NativeViewDraw.DepthWrite)).ToInt32());
        Assert.Equal(40, Marshal.OffsetOf<NativeViewDraw>(nameof(NativeViewDraw.Target)).ToInt32());
        Assert.Equal(24, Marshal.SizeOf<NativeShaderStage>());
        Assert.Equal(16, Marshal.OffsetOf<NativeShaderStage>(nameof(NativeShaderStage.Source)).ToInt32());
        Assert.Equal(16, Marshal.SizeOf<NativeShaderDefine>());
        Assert.Equal(28, Marshal.SizeOf<NativeSamplerConfig>());
        Assert.Equal(32, Marshal.SizeOf<NativeViewImage>());
        Assert.Equal(36, Marshal.SizeOf<NativeViewDispatch>());
    }

    /// <summary>
    /// The listing the bridge hands the inspector reads back as the parameters it describes, which
    /// is what keeps a change of format on one side from emptying the inspector without a word.
    /// </summary>
    [Fact]
    public void AParameterListingReadsBackAsItsParameters()
    {
        var parameters = ShaderParameter.Parse(
            "number\ttint\tfloat\t4\t1\nnumber\tweights\tfloat\t1\t1000\n"
            + "texture\tlayers\t64\nsampler\tlinear\t1\nstruct\tsun\t1\nnumber\tmode\tint\t1\t1");

        Assert.Equal(
            [
                new ShaderParameter(ShaderParameterKind.Number, "tint", ShaderScalar.Float, 4, 1),
                new ShaderParameter(ShaderParameterKind.Number, "weights", ShaderScalar.Float, 1, 1000),
                new ShaderParameter(ShaderParameterKind.Texture, "layers", ShaderScalar.Float, 0, 64),
                new ShaderParameter(ShaderParameterKind.Sampler, "linear", ShaderScalar.Float, 0, 1),
                new ShaderParameter(ShaderParameterKind.Struct, "sun", ShaderScalar.Float, 0, 1),
                new ShaderParameter(ShaderParameterKind.Number, "mode", ShaderScalar.Int, 1, 1),
            ],
            parameters);
    }
}
