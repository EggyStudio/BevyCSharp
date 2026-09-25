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
    [InlineData(nameof(NativeShaderMaterialConfig.Program), 0)]
    [InlineData(nameof(NativeShaderMaterialConfig.Parameters), 8)]
    [InlineData(nameof(NativeShaderMaterialConfig.ParameterCount), 16)]
    [InlineData(nameof(NativeShaderMaterialConfig.Data), 24)]
    [InlineData(nameof(NativeShaderMaterialConfig.DataLength), 32)]
    [InlineData(nameof(NativeShaderMaterialConfig.Textures), 36)]
    [InlineData(nameof(NativeShaderMaterialConfig.Cubes), 68)]
    [InlineData(nameof(NativeShaderMaterialConfig.Arrays), 76)]
    [InlineData(nameof(NativeShaderMaterialConfig.Volumes), 84)]
    [InlineData(nameof(NativeShaderMaterialConfig.Alpha), 92)]
    [InlineData(nameof(NativeShaderMaterialConfig.AlphaCutoff), 96)]
    [InlineData(nameof(NativeShaderMaterialConfig.Cull), 100)]
    [InlineData(nameof(NativeShaderMaterialConfig.DepthBias), 104)]
    [InlineData(nameof(NativeShaderMaterialConfig.Buffer), 108)]
    public void TheMaterialConfigIsWhereTheBridgeReadsIt(string field, int offset) =>
        Assert.Equal(offset, Marshal.OffsetOf<NativeShaderMaterialConfig>(field).ToInt32());

    [Theory]
    [InlineData(nameof(NativeShaderProgramConfig.Fragment), 32)]
    [InlineData(nameof(NativeShaderProgramConfig.PrepassFragment), 96)]
    [InlineData(nameof(NativeShaderProgramConfig.Defines), 128)]
    [InlineData(nameof(NativeShaderProgramConfig.DefineCount), 136)]
    [InlineData(nameof(NativeShaderProgramConfig.Compute), 144)]
    public void TheProgramConfigIsWhereTheBridgeReadsIt(string field, int offset) =>
        Assert.Equal(offset, Marshal.OffsetOf<NativeShaderProgramConfig>(field).ToInt32());

    [Theory]
    [InlineData(nameof(NativeShaderPassConfig.Parameters), 8)]
    [InlineData(nameof(NativeShaderPassConfig.Data), 24)]
    [InlineData(nameof(NativeShaderPassConfig.DataLength), 32)]
    [InlineData(nameof(NativeShaderPassConfig.Textures), 36)]
    [InlineData(nameof(NativeShaderPassConfig.AfterTonemapping), 52)]
    [InlineData(nameof(NativeShaderPassConfig.Buffer), 56)]
    public void ThePassConfigIsWhereTheBridgeReadsIt(string field, int offset) =>
        Assert.Equal(offset, Marshal.OffsetOf<NativeShaderPassConfig>(field).ToInt32());

    [Theory]
    [InlineData(nameof(NativeShaderDispatchConfig.Parameters), 8)]
    [InlineData(nameof(NativeShaderDispatchConfig.ParameterCount), 16)]
    [InlineData(nameof(NativeShaderDispatchConfig.Buffers), 20)]
    [InlineData(nameof(NativeShaderDispatchConfig.X), 36)]
    [InlineData(nameof(NativeShaderDispatchConfig.Z), 44)]
    [InlineData(nameof(NativeShaderDispatchConfig.Images), 48)]
    [InlineData(nameof(NativeShaderDispatchConfig.Textures), 56)]
    public void TheDispatchConfigIsWhereTheBridgeReadsIt(string field, int offset) =>
        Assert.Equal(offset, Marshal.OffsetOf<NativeShaderDispatchConfig>(field).ToInt32());

    [Fact]
    public void EachConfigIsTheSizeTheBridgeReads()
    {
        Assert.Equal(112, Marshal.SizeOf<NativeShaderMaterialConfig>());
        Assert.Equal(176, Marshal.SizeOf<NativeShaderProgramConfig>());
        Assert.Equal(64, Marshal.SizeOf<NativeShaderPassConfig>());
        Assert.Equal(64, Marshal.SizeOf<NativeShaderDispatchConfig>());
        Assert.Equal(32, Marshal.SizeOf<NativeShaderStage>());
        Assert.Equal(16, Marshal.OffsetOf<NativeShaderStage>(nameof(NativeShaderStage.Source)).ToInt32());
        Assert.Equal(24, Marshal.OffsetOf<NativeShaderStage>(nameof(NativeShaderStage.Language)).ToInt32());
        Assert.Equal(16, Marshal.SizeOf<NativeShaderDefine>());
    }
}
