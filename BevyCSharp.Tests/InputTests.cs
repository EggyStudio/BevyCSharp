using System.Runtime.CompilerServices;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the input surface that does not need a device to be meaningful.
/// </summary>
/// <remarks>
/// A headless run has no keyboard and no touchscreen, so what these check is that the mirrored
/// state is empty rather than uninitialised, and that reading it is safe every frame. Whether a
/// real keypress arrives as the right character is confirmed by typing into the sample, which
/// echoes what it was given.
/// </remarks>
[Collection("engine")]
public sealed class InputTests
{
    [Fact]
    public void TypedTextIsEmptyRatherThanNullWhenNothingWasTyped()
    {
        using var harness = new EngineHarness(frames: 3);
        var seen = new List<string?>();

        harness.OnContext(Stage.Update, ctx => seen.Add(ctx.Input.Text));
        harness.Run();

        Assert.NotEmpty(seen);
        Assert.All(seen, text => Assert.Equal(string.Empty, text));
    }

    [Fact]
    public void NoTouchesAreReportedWithoutATouchscreen()
    {
        using var harness = new EngineHarness(frames: 3);
        var counts = new List<int>();

        harness.OnContext(Stage.Update, ctx => counts.Add(ctx.Input.Touches.Length));
        harness.Run();

        Assert.NotEmpty(counts);
        Assert.All(counts, count => Assert.Equal(0, count));
    }

    [Fact]
    public void TheTouchSlotsAreBoundsChecked()
    {
        // The snapshot carries a fixed number of slots and reports how many are in use. Reading
        // past that would return whatever the last frame left there, so the accessor refuses.
        var touches = default(Interop.NativeInput).Touches;

        Assert.Throws<ArgumentOutOfRangeException>(() => touches[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => touches[Interop.NativeInput.TouchCapacity]);

        // Every slot inside the array is readable, and zeroed until the bridge fills it.
        for (var i = 0; i < Interop.NativeInput.TouchCapacity; i++) Assert.Equal(0u, touches[i].Id);
    }

    [Fact]
    public void TheFrameSnapshotIsTheSameSizeOnBothSidesOfTheBridge()
    {
        // Rust writes this struct straight into C#'s memory, so a disagreement about its size or
        // padding would not fail: it would quietly read input out of the wrong bytes, or write
        // past the end. The numbers are checked against the engine's in the native crate, so a
        // change to either half breaks one of the two.
        Assert.Equal(40, Unsafe.SizeOf<Interop.NativeTime>());
        Assert.Equal(24, Unsafe.SizeOf<Interop.NativeTouch>());
        Assert.Equal(320, Unsafe.SizeOf<Interop.NativeInput>());
        Assert.Equal(360, Unsafe.SizeOf<Interop.NativeFrameState>());
    }

    [Fact]
    public void TheKeyboardStillReportsNothingPressedWhenIdle()
    {
        // The bitsets and the new text buffer come from the same snapshot, so this pins down that
        // adding the latter did not disturb the former.
        using var harness = new EngineHarness(frames: 3);
        var anyDown = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            anyDown |= ctx.Input.AnyKeyDown();
            anyDown |= ctx.Input.MouseDown(MouseButton.Left);
        });

        harness.Run();

        Assert.False(anyDown);
    }
}
