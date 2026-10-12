using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a gamepad read through <see cref="Input.Gamepads"/>, a pretended one, which every
/// profile reads, so these run on the headless bridge as on the others.
/// </summary>
/// <remarks>
/// What a pretended pad is set to is processed by Bevy at the start of a later frame, as a real
/// pad's report is, so each step is taken a few frames after the last and each check a few frames
/// after its step, rather than a frame apart.
/// </remarks>
[Collection("engine")]
public sealed class GamepadTests
{
    [Fact]
    public void APadConnectsPressesTiltsAndDisconnects()
    {
        using var harness = new EngineHarness(frames: 24);

        var pad = Entity.None;
        var frame = 0;
        var connected = new List<GamepadConnected>();
        var disconnected = new List<GamepadDisconnected>();
        string? name = null;
        bool down = false, pressedOnce = false, released = false, triggerHeld = true;
        int presses = 0, padsAfter = -1;
        float stick = 0f, trigger = 0f;

        harness.OnContext(Stage.Startup, _ => pad = SyntheticInput.ConnectGamepad("Test pad"));

        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            foreach (var message in ctx.Read<GamepadConnected>()) connected.Add(message);
            foreach (var message in ctx.Read<GamepadDisconnected>()) disconnected.Add(message);

            var pads = ctx.Input.Gamepads;
            var now = pads.FirstOrDefault(known => known.Entity == pad);
            if (now is not null && now.Pressed(GamepadButton.South)) presses++;

            switch (frame)
            {
                case 4:
                    name = now?.Name;
                    SyntheticInput.SetGamepadButton(pad, GamepadButton.South);
                    SyntheticInput.SetGamepadAxis(pad, GamepadAxis.LeftX, 0.6f);
                    SyntheticInput.SetGamepadAxis(pad, GamepadAxis.LeftTrigger, 0.5f);
                    break;

                case 8:
                    down = now?.Down(GamepadButton.South) ?? false;
                    stick = now?.LeftStick.X ?? 0f;
                    trigger = now?.Axis(GamepadAxis.LeftTrigger) ?? 0f;

                    // Half way down is an axis and not yet a press, which is three quarters.
                    triggerHeld = now?.Down(GamepadButton.LeftTrigger2) ?? true;
                    SyntheticInput.SetGamepadButton(pad, GamepadButton.South, 0f);
                    break;

                case > 8 and < 14:
                    released |= now?.Released(GamepadButton.South) ?? false;
                    break;

                case 14:
                    SyntheticInput.DisconnectGamepad(pad);
                    break;

                case 20:
                    padsAfter = pads.Count;
                    break;
            }

            pressedOnce = presses == 1;
        });

        harness.Run();

        Assert.Equal("Test pad", name);
        Assert.Equal([new GamepadConnected(pad, "Test pad")], connected);
        Assert.True(down, "South was not held");
        Assert.True(pressedOnce, $"South was pressed {presses} times");
        Assert.True(released, "South was not released");
        Assert.Equal(0.6f, stick, 3);
        Assert.Equal(0.5f, trigger, 3);
        Assert.False(triggerHeld);
        Assert.Equal([new GamepadDisconnected(pad)], disconnected);
        Assert.Equal(0, padsAfter);
    }

    [Fact]
    public void ARumbleIsTakenByAPadThatCannotRumble()
    {
        using var harness = new EngineHarness(frames: 6);
        var pad = Entity.None;
        Exception? rumble = null;
        var tried = false;
        var frame = 0;

        harness.OnContext(Stage.Startup, _ => pad = SyntheticInput.ConnectGamepad());
        harness.OnContext(Stage.Update, ctx =>
        {
            if (++frame != 4 || ctx.Input.Gamepads is not [var connected, ..]) return;

            tried = true;
            rumble = Record.Exception(() =>
            {
                connected.Rumble(1f, 0.5f, 0.2f);
                connected.StopRumble();
            });
        });

        harness.Run();

        Assert.NotEqual(Entity.None, pad);
        Assert.True(tried, "the pad never connected");
        Assert.Null(rumble);
    }
}
