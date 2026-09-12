using System.Runtime.InteropServices;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The managed mirror of the native input snapshot has to agree with it field by field.
/// </summary>
/// <remarks>
/// A mirror that is the right size but has one field in the wrong place reads whatever its
/// neighbour wrote, and nothing about the size says so. That is not hypothetical: a padding field
/// that the native struct does not have put <c>TextLength</c> where <c>touch_count</c> is, and the
/// two together still came to the same number of bytes, so every typed character was dropped
/// between the window and the interface for as long as the bridge existed.
/// <para>
/// The same numbers are asserted on the Rust side, so neither half can move without the other.
/// </para>
/// </remarks>
public sealed class NativeInputLayoutTests
{
    [Theory]
    [InlineData(nameof(NativeInput.MouseX), 0)]
    [InlineData(nameof(NativeInput.WheelY), 20)]
    [InlineData(nameof(NativeInput.MouseDown), 72)]
    [InlineData(nameof(NativeInput.MousePressed), 76)]
    [InlineData(nameof(NativeInput.MouseReleased), 80)]
    [InlineData(nameof(NativeInput.TextLength), 84)]
    [InlineData(nameof(NativeInput.TouchCount), 88)]
    [InlineData(nameof(NativeInput.Text), 92)]
    [InlineData(nameof(NativeInput.Touches), 128)]
    public void EveryFieldSitsWhereTheBridgePutIt(string field, int offset) =>
        Assert.Equal(offset, Marshal.OffsetOf<NativeInput>(field).ToInt32());

    [Fact]
    public void TheWholeSnapshotIsTheSizeTheBridgeWrites() =>
        Assert.Equal(320, Marshal.SizeOf<NativeInput>());
}
