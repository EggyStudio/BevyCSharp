using Bevy.Interop;
using ImGuiNET;

namespace Bevy;

/// <summary>
/// Puts input into the interface as though a hand had done it.
/// </summary>
/// <remarks>
/// <para>
/// For tests and tools. What a click does is not one thing. A widget decides it was clicked, an
/// editor decides what that meant, and something in the world changes. Calling the method a click
/// would have called tests the method and not the path to it, and the path is where the interesting
/// failures are: a handle that cannot be grabbed, a menu that opens once, a value that goes in and
/// does not come back.
/// </para>
/// <para>
/// It writes into ImGui's own event queue, which is where a real pointer's report ends up, so
/// everything the interface does behaves exactly as it would. It cannot move the operating system's
/// cursor, and does not try to, because what it drives is the application rather than the desktop.
/// </para>
/// <para>
/// A pointer is written into Bevy as the window's own messages too, and in an offscreen run, which
/// has no window, it is put on the image the run draws into, through Bevy's own pointer input, so
/// picking finds what is drawn under it there as it would under a hand's pointer in a window.
/// </para>
/// <para>
/// While anything here has been called, the pointer the interface sees is the one that was asked
/// for rather than the one on the desk. <see cref="Release"/> leaves it where it was put; there is
/// no need to hand it back.
/// </para>
/// </remarks>
public static class SyntheticInput
{
    /// <summary>Where the pretend pointer is, or nothing while the real one is in charge.</summary>
    internal static (float X, float Y)? Pretend { get; private set; }

    /// <summary>Moves the pointer to a point in the window, in logical pixels.</summary>
    public static void MoveTo(float x, float y) => Send(x, y, PointerAction.Move);

    /// <summary>Presses a button where the pointer is put.</summary>
    public static void Press(float x, float y, MouseButton button = MouseButton.Left) =>
        Send(x, y, PointerAction.Press, button);

    /// <summary>Releases a button where the pointer is put.</summary>
    public static void Release(float x, float y, MouseButton button = MouseButton.Left) =>
        Send(x, y, PointerAction.Release, button);

    /// <summary>Connects a pretended gamepad and returns its entity.</summary>
    /// <remarks>
    /// <para>
    /// For a script or a test on a machine with no pad attached. The bridge writes the messages a
    /// real pad's connection writes, so Bevy keeps it as it keeps a real one, and it is in
    /// <see cref="Input.Gamepads"/> from the next frame, with a <see cref="GamepadConnected"/> sent
    /// then. Every profile reads one, the headless one included, which finds no real pads.
    /// </para>
    /// <para>
    /// Its buttons and axes are set with <see cref="SetGamepadButton"/> and
    /// <see cref="SetGamepadAxis"/>, and each change is read a frame later, as a real pad's is.
    /// </para>
    /// </remarks>
    /// <param name="name">What it is called, as a platform names a real pad.</param>
    /// <exception cref="BevyNativeException">Called from outside a system.</exception>
    public static unsafe Entity ConnectGamepad(string name = "Console pad")
    {
        ArgumentNullException.ThrowIfNull(name);

        var bytes = System.Text.Encoding.UTF8.GetBytes(name);
        ulong entity;
        fixed (byte* text = bytes) entity = Native.bcs_gamepad_connect(text, (uint)bytes.Length);

        if (entity == 0)
            throw new BevyNativeException(NativeStatus.NoWorld, "Connecting a pretended gamepad needs the world, so it is done from inside a system.");

        return new Entity(entity);
    }

    /// <summary>Disconnects a pretended gamepad, which leaves <see cref="Input.Gamepads"/> the next frame.</summary>
    /// <exception cref="BevyNativeException">The entity is gone, or this was called from outside a system.</exception>
    public static void DisconnectGamepad(Entity gamepad) =>
        Native.Check(Native.bcs_gamepad_disconnect(gamepad.Bits), $"disconnecting gamepad {gamepad}");

    /// <summary>Sets one of a pretended pad's buttons, from zero, up, to one, down.</summary>
    /// <remarks>Down past three quarters counts as held, as Bevy reads a real pad's buttons.</remarks>
    /// <exception cref="BevyNativeException">Called from outside a system.</exception>
    public static void SetGamepadButton(Entity gamepad, GamepadButton button, float value = 1f) =>
        Native.Check(Native.bcs_gamepad_button(gamepad.Bits, (int)button, value), $"setting {button} on gamepad {gamepad}");

    /// <summary>Sets one of a pretended pad's axes, a stick from minus one to one and a trigger from zero to one.</summary>
    /// <exception cref="BevyNativeException">Called from outside a system.</exception>
    public static void SetGamepadAxis(Entity gamepad, GamepadAxis axis, float value) =>
        Native.Check(Native.bcs_gamepad_axis(gamepad.Bits, (int)axis, value), $"setting {axis} on gamepad {gamepad}");

    /// <summary>
    /// Rolls the wheel, in the lines a wheel with detents reports.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Positive is away from the hand, which is up in a list, and sideways is to the right. The
    /// pointer is not moved first, because what the wheel affects is decided by where it already
    /// is, so a test moves it and then rolls.
    /// </para>
    /// <para>
    /// It is written into Bevy as a real wheel's report begins, as a message Bevy counts into the
    /// frame's scroll at the start of the next frame, so <see cref="Input.WheelY"/> reads it then
    /// and the interface, which is fed from that, scrolls then too. Picking sends it to whatever the
    /// pointer is over as <see cref="Pointer{TEvent}"/> of <see cref="Scroll"/>, in a window and on
    /// the image an offscreen run draws into alike. Writing it into the interface's queue as well
    /// would count it twice, once now and once when the frame's input carries it.
    /// </para>
    /// </remarks>
    /// <param name="lines">How far away from the hand.</param>
    /// <param name="sideways">How far to the right.</param>
    /// <exception cref="BevyNativeException">Called from outside a system.</exception>
    public static void Wheel(float lines, float sideways = 0f) =>
        Native.Check(Native.bcs_input_wheel(sideways, lines, (int)ScrollUnit.Line), $"rolling the wheel {lines}");

    /// <summary>
    /// Presses a key where a real one is reported, at the window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The keyboard's half of <see cref="Send"/>. Everything between the window and a text field
    /// runs: the engine's own key state, the messages it writes, whatever turns those into the
    /// characters an interface inserts. Putting characters straight into the interface's queue
    /// tests the field and not the path to it, and the path is where a keyboard goes wrong.
    /// </para>
    /// <para>
    /// It arrives as a real key does, as a message Bevy reads into the key's state at the start of
    /// the next frame, and <see cref="Input"/> takes its picture of that state at the top of the
    /// frame after. So a key pressed from an update is <see cref="Input.KeyPressed(Key)"/> on exactly
    /// one frame, two after the one it was pressed on, and a <see cref="Tap"/> is pressed and
    /// released on that same frame.
    /// </para>
    /// </remarks>
    /// <param name="key">Which key.</param>
    /// <param name="typed">What it typed, or nothing for a key that types nothing.</param>
    public static void Press(Key key, string typed = "")
    {
        ArgumentNullException.ThrowIfNull(typed);

        Stroke(key, PointerAction.Press, typed);
    }

    /// <summary>Lets a key go, where a real one is reported.</summary>
    /// <param name="key">Which key.</param>
    public static void Lift(Key key) => Stroke(key, PointerAction.Release, string.Empty);

    /// <summary>Presses a key and lets it go again.</summary>
    /// <param name="key">Which key.</param>
    /// <param name="typed">What it typed, or nothing for a key that types nothing.</param>
    public static void Tap(Key key, string typed = "")
    {
        Press(key, typed);
        Lift(key);
    }

    /// <summary>One end of a keystroke, written to the window.</summary>
    private static unsafe void Stroke(Key key, PointerAction action, string typed)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(typed);

        fixed (byte* text = bytes)
        {
            Native.Check(
                Native.bcs_input_key(
                    (int)key, (int)action, bytes.Length > 0 ? text : null, (uint)bytes.Length),
                $"sending a key {action} for {key}");
        }
    }

    /// <summary>
    /// Presses and releases a key in the interface's own queue.
    /// </summary>
    /// <remarks>
    /// A key that types something types it as well, because a keyboard does that and a field is
    /// waiting for it. This reaches the interface only; <see cref="Press(Key, string)"/> starts
    /// where a real key starts.
    /// </remarks>
    public static void Key(ImGuiKey key, string? typed = null)
    {
        if (!ImGuiRuntime.IsRunning) return;

        var io = ImGui.GetIO();

        io.AddKeyEvent(key, true);

        if (typed is { Length: > 0 })
        {
            foreach (var character in typed) io.AddInputCharacter(character);
        }

        io.AddKeyEvent(key, false);
    }

    /// <summary>
    /// Composes text as the platform's input method would, with the caret over
    /// <paramref name="caretStart"/> to <paramref name="caretEnd"/>, or hidden at -1.
    /// </summary>
    /// <remarks>
    /// For driving a field that takes composed input, which no key can produce. It arrives as an
    /// <see cref="ImeComposing"/> on the next frame, with or without a window, since what reads it
    /// does not ask which window it came from.
    /// </remarks>
    public static void Compose(string text, int caretStart = -1, int caretEnd = -1) =>
        Ime(0, text, caretStart, caretEnd);

    /// <summary>Commits text as the platform's input method would, arriving as an <see cref="ImeCommit"/>.</summary>
    public static void Commit(string text) => Ime(1, text, -1, -1);

    private static unsafe void Ime(int kind, string text, int caretStart, int caretEnd)
    {
        ArgumentNullException.ThrowIfNull(text);

        // The caret in bytes of UTF-8, as the platform reports it, from indices into the string.
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        int Bytes(int index) => index < 0 ? -1 : System.Text.Encoding.UTF8.GetByteCount(text.AsSpan(0, Math.Min(index, text.Length)));

        fixed (byte* at = bytes)
        {
            Native.Check(
                Native.bcs_input_ime(kind, at, (uint)bytes.Length, Bytes(caretStart), Bytes(caretEnd)),
                "composing as an input method");
        }
    }

    /// <summary>Types a run of characters.</summary>
    public static void Type(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!ImGuiRuntime.IsRunning) return;

        var io = ImGui.GetIO();
        foreach (var character in text) io.AddInputCharacter(character);
    }

    /// <summary>
    /// Moves, presses or releases the pointer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Into both halves of what a pointer does, which is the interface's own event queue and the
    /// window's messages. The second raycasts the scene and steers the camera, so a click told to
    /// only one of them tests half the path a hand takes.
    /// </para>
    /// <para>
    /// A run with no window, such as an editor opened with <c>--offscreen</c>, has the pointer put
    /// on the image it draws into, so picking finds what is drawn under it there as it would in a
    /// window. One that draws nowhere takes the interface's half alone, so its panels, buttons and
    /// fields can be clicked while nothing in the scene is, and a left press and release in one
    /// place there is also kept as a click (<see cref="TryTakeClickWithoutWindow"/>), for a tool to
    /// answer by casting a ray. A run with nowhere to draw and no interface has nowhere to send a
    /// pointer and refuses.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">This run has neither a window nor an interface.</exception>
    public static void Send(
        float x, float y, PointerAction action, MouseButton button = MouseButton.Left)
    {
        var status = Native.bcs_input_pointer(x, y, (int)action, (int)button);
        var windowless = status == NativeStatus.InvalidState;

        if (windowless && !ImGuiRuntime.IsRunning)
            throw new BevyNativeException(
                NativeStatus.InvalidState,
                $"Sending a pointer {action} at {x},{y} failed, because this run has no window and "
                + "no interface to send it to. A headless run draws neither. Run with a window, or "
                + "drive what the click would have done directly.");

        if (!windowless) Native.Check(status, $"sending a pointer {action} at {x},{y}");
        else if (button == MouseButton.Left) Unwindowed(x, y, action);

        if (!ImGuiRuntime.IsRunning) return;

        Pretend = (x, y);

        var io = ImGui.GetIO();
        io.AddMousePosEvent(x, y);

        var which = button switch
        {
            MouseButton.Right => 1,
            MouseButton.Middle => 2,
            _ => 0,
        };

        switch (action)
        {
            case PointerAction.Press:
                io.AddMouseButtonEvent(which, true);
                break;

            case PointerAction.Release:
                io.AddMouseButtonEvent(which, false);
                break;
        }
    }

    /// <summary>Gives the pointer back to the hand on the desk.</summary>
    public static void Forget() => Pretend = null;

    /// <summary>How far a release may land from its press and still be a click, in pixels.</summary>
    private const float ClickSlop = 4f;

    private static readonly Lock Gate = new();
    private static readonly Queue<(float X, float Y)> ClicksWithoutWindow = new();
    private static (float X, float Y)? _pressedWithoutWindow;

    /// <summary>
    /// Takes the oldest left click given to a run with no window that nothing has answered yet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a run with neither a window nor an image it draws into, where Bevy's picking has nothing
    /// to find a pointer on, so a click reaches the interface and nothing in the scene. This hands
    /// such a click to whoever answers clicks on the scene, which casts a ray from its camera
    /// through the point (<see cref="Render.TryRay"/>, <see cref="Picking.TryCast(Vec3, Vec3, out Entity, out Vec3, out Vec3)"/>). An offscreen
    /// run has its pointer put on the image it draws into, where picking finds what is under it,
    /// and gives nothing here.
    /// </para>
    /// <para>
    /// A click is a press and a release no more than a few pixels apart, so a drag is not one.
    /// </para>
    /// </remarks>
    /// <param name="x">Where, in logical pixels from the top left.</param>
    /// <param name="y">Where, in logical pixels from the top left.</param>
    /// <returns>Whether there was a click to take.</returns>
    public static bool TryTakeClickWithoutWindow(out float x, out float y)
    {
        lock (Gate)
        {
            if (ClicksWithoutWindow.TryDequeue(out var click))
            {
                (x, y) = click;
                return true;
            }
        }

        x = y = 0f;
        return false;
    }

    /// <summary>Keeps a left press, and turns a release near it into a click.</summary>
    private static void Unwindowed(float x, float y, PointerAction action)
    {
        lock (Gate)
        {
            switch (action)
            {
                case PointerAction.Press:
                    _pressedWithoutWindow = (x, y);
                    break;

                case PointerAction.Release when _pressedWithoutWindow is { } pressed:
                    if (MathF.Abs(pressed.X - x) <= ClickSlop && MathF.Abs(pressed.Y - y) <= ClickSlop)
                        ClicksWithoutWindow.Enqueue((x, y));

                    _pressedWithoutWindow = null;
                    break;
            }
        }
    }
}
