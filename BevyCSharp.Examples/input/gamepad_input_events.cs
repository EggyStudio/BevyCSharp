// Bevy's gamepad_input_events example, examples/input/gamepad_input_events.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Inputs;

// Iterates and prints gamepad input and connection events, each kind of its own in one system,
// and in the other Bevy's ordered gamepad event, which keeps a pad's connections, buttons and axes
// in the one order they came.
internal static class GamepadInputEvents
{
    public static void Build(App app)
    {
        app.Update(GamepadEvents, "gamepad_input_events.GamepadEvents");
        app.Update(GamepadOrderedEvents, "gamepad_input_events.GamepadOrderedEvents");
    }

    private static void GamepadEvents(BehaviorContext ctx)
    {
        foreach (var connectionEvent in ctx.Read<GamepadConnectionEvent>()) Console.WriteLine(connectionEvent);
        foreach (var axisChangedEvent in ctx.Read<GamepadAxisChangedEvent>())
            Console.WriteLine(FormattableString.Invariant($"{axisChangedEvent.Axis} of {axisChangedEvent.Gamepad} is changed to {axisChangedEvent.Value}"));
        foreach (var buttonChangedEvent in ctx.Read<GamepadButtonChangedEvent>())
            Console.WriteLine(FormattableString.Invariant($"{buttonChangedEvent.Button} of {buttonChangedEvent.Gamepad} is changed to {buttonChangedEvent.Value}"));
        foreach (var buttonInputEvent in ctx.Read<GamepadButtonStateChangedEvent>()) Console.WriteLine(buttonInputEvent);
    }

    private static void GamepadOrderedEvents(BehaviorContext ctx)
    {
        foreach (var gamepadEvent in ctx.Read<GamepadEvent>())
        {
            if (gamepadEvent.Connection is { } connectionEvent) Console.WriteLine(connectionEvent);
            else if (gamepadEvent.Button is { } buttonEvent) Console.WriteLine(buttonEvent);
            else if (gamepadEvent.Axis is { } axisEvent) Console.WriteLine(axisEvent);
        }
    }
}
