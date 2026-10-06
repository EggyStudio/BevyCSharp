using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the input surface that does not need a device to be meaningful.
/// </summary>
/// <remarks>
/// A headless run has no keyboard and no touchscreen, so what these check is that the mirrored
/// state is empty rather than uninitialized, and that reading it is safe every frame. Whether a
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
        // padding would not fail. It would quietly read input out of the wrong bytes, or write
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

    [SkippableFact]
    public void ATappedKeyIsPressedOnOneFrameAndReleasedOnOne()
    {
        Needs.Renderer();

        // Tapped on the third update, so each frame after it says whether the key went down and
        // came up, and a game toggling on the press toggles once rather than twice and back.
        using var harness = new EngineHarness(frames: 10);
        var frame = 0;
        var pressed = new List<int>();
        var released = new List<int>();

        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            if (ctx.Input.KeyPressed(Key.A)) pressed.Add(frame);
            if (ctx.Input.KeyReleased(Key.A)) released.Add(frame);
            if (frame == 3) SyntheticInput.Tap(Key.A, "a");
        });
        harness.Run();

        Assert.Single(pressed);
        Assert.Single(released);
    }

    /// <summary>Each field of a logical key sits where the bridge writes it.</summary>
    [Theory]
    [InlineData(nameof(NativeLogicalKey.Flags), 0)]
    [InlineData(nameof(NativeLogicalKey.Kind), 1)]
    [InlineData(nameof(NativeLogicalKey.Length), 2)]
    [InlineData(nameof(NativeLogicalKey.Text), 4)]
    public void EveryFieldOfALogicalKeySitsWhereTheBridgePutsIt(string field, int offset)
    {
        Assert.Equal(offset, Marshal.OffsetOf<NativeLogicalKey>(field).ToInt32());
        Assert.Equal(32, Marshal.SizeOf<NativeLogicalKey>());
    }

    /// <summary>Each field of a key handed to the focused entity sits where the bridge writes it.</summary>
    [Theory]
    [InlineData(nameof(NativeFocusedKey.Entity), 0)]
    [InlineData(nameof(NativeFocusedKey.Key), 8)]
    [InlineData(nameof(NativeFocusedKey.State), 12)]
    [InlineData(nameof(NativeFocusedKey.Repeat), 16)]
    [InlineData(nameof(NativeFocusedKey.LogicalKind), 20)]
    [InlineData(nameof(NativeFocusedKey.LogicalLength), 21)]
    [InlineData(nameof(NativeFocusedKey.TextLength), 22)]
    [InlineData(nameof(NativeFocusedKey.HasText), 23)]
    [InlineData(nameof(NativeFocusedKey.Logical), 24)]
    [InlineData(nameof(NativeFocusedKey.Text), 52)]
    public void EveryFieldOfAFocusedKeySitsWhereTheBridgePutsIt(string field, int offset)
    {
        Assert.Equal(offset, Marshal.OffsetOf<NativeFocusedKey>(field).ToInt32());
        Assert.Equal(80, Marshal.SizeOf<NativeFocusedKey>());
    }

    /// <summary>
    /// A key read by what it types or by its name is pressed and released on one frame each, as the
    /// physical key is, and a held key that types nothing reads as its name while it is held.
    /// </summary>
    [SkippableFact]
    public void AKeyIsReadByWhatItTypesAndByItsName()
    {
        Needs.Renderer();

        using var harness = new EngineHarness(frames: 20);
        var frame = 0;
        var seen = new List<string>();
        var stuck = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            var input = ctx.Input;
            if (input.KeyPressed(LogicalKey.Character("?"))) seen.Add($"? pressed {frame}");
            if (input.KeyReleased(LogicalKey.Character("?"))) seen.Add($"? released {frame}");
            if (input.KeyPressed(LogicalKey.Enter)) seen.Add($"enter pressed {input.KeyDown(LogicalKey.Control)}");
            if (frame == 18) stuck = input.KeyDown(LogicalKey.Character("?")) || input.KeyDown(LogicalKey.Enter);

            if (frame == 3) SyntheticInput.Tap(Key.Slash, "?");
            if (frame == 8) SyntheticInput.Press(Key.ControlLeft);
            if (frame == 10) SyntheticInput.Tap(Key.Enter);
            if (frame == 12) SyntheticInput.Lift(Key.ControlLeft);
        });
        harness.Run();

        // Tapped, it goes down and comes up on the one frame, two after the tap, as a physical key does.
        Assert.Equal(["? pressed 5", "? released 5", "enter pressed True"], seen);
        Assert.False(stuck, "a logical key stayed down after it was let go");
    }

    [Fact]
    public void TheWheelIsReadOnOneFrameAsARealOneIs()
    {
        // Rolled on the third update, in a run with no window, where the wheel is Bevy's message
        // all the same, so the frame's input carries it once, both ways, and then nothing.
        using var harness = new EngineHarness(frames: 10);
        var frame = 0;
        var wheels = new List<(int Frame, float X, float Y)>();

        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            if (ctx.Input.WheelX != 0f || ctx.Input.WheelY != 0f) wheels.Add((frame, ctx.Input.WheelX, ctx.Input.WheelY));
            if (frame == 3) SyntheticInput.Wheel(-2f, 0.5f);
        });
        harness.Run();

        var wheel = Assert.Single(wheels);
        Assert.InRange(wheel.Frame, 4, 5);
        Assert.Equal(0.5f, wheel.X);
        Assert.Equal(-2f, wheel.Y);
    }
}
