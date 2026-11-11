using System.Runtime.InteropServices;
using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers Bevy's input messages read in C#, one for each change in the order it came.</summary>
/// <remarks>
/// A pretended device's report is processed by Bevy at the start of a later frame, as a real one's
/// is, so each step is taken a few frames after the last and the messages are gathered every frame.
/// </remarks>
[Collection("engine")]
public sealed class InputMessageTests
{
    /// <summary>Each field of a drained message sits where the bridge writes it.</summary>
    [Theory]
    [InlineData(nameof(NativeInputMessage.Kind), 0)]
    [InlineData(nameof(NativeInputMessage.Code), 4)]
    [InlineData(nameof(NativeInputMessage.State), 8)]
    [InlineData(nameof(NativeInputMessage.Flags), 12)]
    [InlineData(nameof(NativeInputMessage.Entity), 16)]
    [InlineData(nameof(NativeInputMessage.Id), 24)]
    [InlineData(nameof(NativeInputMessage.X), 32)]
    [InlineData(nameof(NativeInputMessage.U), 48)]
    [InlineData(nameof(NativeInputMessage.LogicalKind), 52)]
    [InlineData(nameof(NativeInputMessage.NameLength), 55)]
    [InlineData(nameof(NativeInputMessage.Logical), 56)]
    [InlineData(nameof(NativeInputMessage.Text), 84)]
    [InlineData(nameof(NativeInputMessage.Name), 112)]
    public void EveryFieldOfAMessageSitsWhereTheBridgeWritesIt(string field, int offset)
    {
        Assert.Equal(offset, Marshal.OffsetOf<NativeInputMessage>(field).ToInt32());
        Assert.Equal(160, Marshal.SizeOf<NativeInputMessage>());
    }

    /// <summary>
    /// A pad connecting, its button pressed and let go and its stick tilted are each a message of
    /// their own kind, and Bevy's ordered gamepad event holds them in the order they came.
    /// </summary>
    [Fact]
    public void APadsChangesAreEachAMessageAndTheOrderedOnesKeepTheirOrder()
    {
        using var harness = new EngineHarness(frames: 20);
        var pad = Entity.None;
        var frame = 0;
        var connections = new List<GamepadConnectionEvent>();
        var states = new List<GamepadButtonStateChangedEvent>();
        var values = new List<GamepadButtonChangedEvent>();
        var axes = new List<GamepadAxisChangedEvent>();
        var ordered = new List<string>();

        harness.OnContext(Stage.Startup, _ => pad = SyntheticInput.ConnectGamepad("Test pad"));
        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            connections.AddRange(ctx.Read<GamepadConnectionEvent>().ToArray());
            states.AddRange(ctx.Read<GamepadButtonStateChangedEvent>().ToArray());
            values.AddRange(ctx.Read<GamepadButtonChangedEvent>().ToArray());
            axes.AddRange(ctx.Read<GamepadAxisChangedEvent>().ToArray());
            foreach (var e in ctx.Read<GamepadEvent>())
            {
                ordered.Add(e.Connection is { } c ? $"connection {c.Connected}"
                    : e.Button is { } b ? $"button {b.Button} {b.State}"
                    : $"axis {e.Axis!.Value.Axis}");
            }

            if (frame == 4) SyntheticInput.SetGamepadButton(pad, GamepadButton.South);
            if (frame == 8) SyntheticInput.SetGamepadAxis(pad, GamepadAxis.LeftX, 0.6f);
            if (frame == 12) SyntheticInput.SetGamepadButton(pad, GamepadButton.South, 0f);
        });
        harness.Run();

        var connected = Assert.Single(connections);
        Assert.Equal((pad, true, "Test pad"), (connected.Gamepad, connected.Connected, connected.Name));

        Assert.Equal([(GamepadButton.South, ButtonState.Pressed), (GamepadButton.South, ButtonState.Released)],
            states.Select(s => (s.Button, s.State)));
        Assert.All(states, s => Assert.Equal(pad, s.Gamepad));
        Assert.Equal([1f, 0f], values.Where(v => v.Button == GamepadButton.South).Select(v => v.Value));

        // As Bevy filters it, the stick's travel scaled out from the default dead zone of 0.05.
        var tilt = Assert.Single(axes);
        Assert.Equal(GamepadAxis.LeftX, tilt.Axis);
        Assert.Equal((0.6f - 0.05f) / 0.95f, tilt.Value, 3);

        Assert.Equal(["connection True", "button South Pressed", "axis LeftX", "button South Released"], ordered);
    }

    /// <summary>
    /// A key typed and lifted, a button clicked and the wheel rolled in a run with no window are each
    /// read as their message, the key with what it typed.
    /// </summary>
    [SkippableFact]
    public void AKeyAButtonAndTheWheelAreEachAMessage()
    {
        Needs.Renderer();

        var frame = 0;
        var keys = new List<KeyboardInput>();
        var buttons = new List<MouseButtonInput>();
        var wheels = new List<MouseWheel>();

        using var app = new App(Config.OffscreenFor(64, 64, frames: 16));
        app.AddPlugin(new EnginePlugin());
        app.Update(ctx =>
        {
            frame++;
            keys.AddRange(ctx.Read<KeyboardInput>().ToArray());
            buttons.AddRange(ctx.Read<MouseButtonInput>().ToArray());
            wheels.AddRange(ctx.Read<MouseWheel>().ToArray());

            switch (frame)
            {
                case 2: SyntheticInput.Press(Key.A, "a"); break;
                case 4: SyntheticInput.Lift(Key.A); break;
                case 6: SyntheticInput.Press(10f, 10f); break;
                case 8: SyntheticInput.Release(10f, 10f); break;
                case 10: SyntheticInput.Wheel(2f); break;
            }
        }, "Test.Gather");
        Assert.Equal(0, app.Run());

        Assert.Equal([(Key.A, ButtonState.Pressed, "a"), (Key.A, ButtonState.Released, null)],
            keys.Select(k => (k.KeyCode, k.State, k.Text)));
        Assert.Equal(LogicalKey.Character("a"), keys[0].LogicalKey);
        Assert.Equal([(MouseButton.Left, ButtonState.Pressed), (MouseButton.Left, ButtonState.Released)],
            buttons.Select(b => (b.Button, b.State)));
        var wheel = Assert.Single(wheels);
        Assert.Equal((ScrollUnit.Line, 2f), (wheel.Unit, wheel.Y));
    }
}
