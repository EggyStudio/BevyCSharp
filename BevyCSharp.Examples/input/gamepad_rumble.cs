using Bevy;

namespace BevyCSharp.Examples.Inputs;

// Shows how to rumble a gamepad, each face button a different rumble and Start stopping it.
internal static class GamepadRumble
{
    public static void Build(App app) => app.Update(ctx =>
    {
        foreach (var pad in ctx.Input.Gamepads)
        {
            if (pad.Pressed(GamepadButton.North))
            {
                Console.WriteLine("North face button: strong (low-frequency) with low intensity for rumble for 5 seconds. Press multiple times to increase intensity.");
                pad.Rumble(strong: 0.1f, weak: 0f, seconds: 5f);
            }

            if (pad.Pressed(GamepadButton.East))
            {
                Console.WriteLine("East face button: maximum rumble on both motors for 5 seconds");
                pad.Rumble(strong: 1f, weak: 1f, seconds: 5f);
            }

            if (pad.Pressed(GamepadButton.South))
            {
                Console.WriteLine("South face button: low-intensity rumble on the weak motor for 0.5 seconds");
                pad.Rumble(strong: 0f, weak: 0.25f, seconds: 0.5f);
            }

            if (pad.Pressed(GamepadButton.West))
            {
                Console.WriteLine("West face button: custom rumble intensity for 5 second");
                pad.Rumble(strong: 0.5f, weak: 0.25f, seconds: 5f);
            }

            if (pad.Pressed(GamepadButton.Start))
            {
                Console.WriteLine("Start button: Interrupt the current rumble");
                pad.StopRumble();
            }
        }
    }, "gamepad_rumble.Gamepad");

    public static void Drive(App app)
    {
        var pad = Entity.None;
        var step = 2;
        var script = new List<(int, Action)> { (step, () => pad = SyntheticInput.ConnectGamepad()) };
        foreach (var button in new[] { GamepadButton.North, GamepadButton.East, GamepadButton.South, GamepadButton.West, GamepadButton.Start })
        {
            script.Add((step += 2, () => SyntheticInput.SetGamepadButton(pad, button)));
            script.Add((step += 2, () => SyntheticInput.SetGamepadButton(pad, button, 0f)));
        }

        app.Script([.. script]);
    }
}
