using System.Globalization;
using ImGuiNET;

namespace Bevy;

internal static partial class ConsoleWorldCommands
{
    /// <summary>Moves the pointer.</summary>
    [Command("input.move", "Moves the pointer: input.move <x> <y>")]
    internal static string Move(float x, float y)
    {
        SyntheticInput.MoveTo(x, y);
        return $"pointer at {x:0},{y:0}";
    }

    /// <summary>
    /// Presses and releases at a point.
    /// </summary>
    /// <remarks>
    /// Both halves in one frame, which makes a click for an interface that reads the button's
    /// state. Something that matches a release to the press that landed on it, such as picking a
    /// mesh, needs the two on separate frames, so it gets <c>input.press</c> and
    /// <c>input.release</c> instead.
    /// </remarks>
    [Command("input.click", "Clicks a point: input.click <x> <y>")]
    internal static string Click(float x, float y)
    {
        SyntheticInput.Press(x, y);
        SyntheticInput.Release(x, y);
        return $"clicked {x:0},{y:0}";
    }

    /// <summary>Holds the button down at a point.</summary>
    [Command("input.press", "Holds the button down: input.press <x> <y>")]
    internal static string PressAt(float x, float y)
    {
        SyntheticInput.Press(x, y);
        return $"pressed {x:0},{y:0}";
    }

    /// <summary>Lets the button go at a point.</summary>
    [Command("input.release", "Lets the button go: input.release <x> <y>")]
    internal static string ReleaseAt(float x, float y)
    {
        SyntheticInput.Release(x, y);
        return $"released {x:0},{y:0}";
    }

    /// <summary>
    /// Drags the pointer with a button held, an equal step a frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A swipe, a transform handle dragged or a panel moved, which <c>input.press</c>,
    /// <c>input.move</c> and <c>input.release</c> in separate calls cannot time, since each call
    /// is a round trip of a few frames and a drag is read a frame at a time. The button goes down
    /// at the start now, the pointer moves a step each frame for the frames given, and the button
    /// comes up at the end the frame after the last step, so the press and the release are on
    /// frames of their own, as picking asks. The answer comes the frame after the release.
    /// </para>
    /// <para>
    /// The left button unless another is named. Held at most a few frames short of
    /// <see cref="ConsoleHost.LaterFrames"/>, which is as long as a caller waits for an answer.
    /// </para>
    /// </remarks>
    [Command("input.drag", "Drags with a button held, a step a frame: input.drag <x> <y> <dx> <dy> <frames> [Left|Right|Middle]")]
    internal static string Drag(string line)
    {
        if (!TryReadDrag(line, out var drag, out var problem))
        {
            ConsoleHost.Fail("BAD_ARGUMENTS", problem);
            return "input.drag <x> <y> <dx> <dy> <frames> [Left|Right|Middle]";
        }

        SyntheticInput.Press(drag.X, drag.Y, drag.Button);

        var step = 0;
        var released = false;
        ConsoleHost.Later(() =>
        {
            if (released) return $"dragged {drag.Button} from {drag.X:0},{drag.Y:0} by {drag.Dx:0},{drag.Dy:0} over {drag.Frames} frames";

            if (step < drag.Frames)
            {
                var (x, y) = drag.At(++step);
                SyntheticInput.MoveTo(x, y);
                return null;
            }

            var (endX, endY) = drag.At(drag.Frames);
            SyntheticInput.Release(endX, endY, drag.Button);
            released = true;
            return null;
        });

        return $"dragging {drag.Button} from {drag.X:0},{drag.Y:0} by {drag.Dx:0},{drag.Dy:0} over {drag.Frames} frames";
    }

    /// <summary>A drag <c>input.drag</c> was asked for, from a point, by a distance, over a number of frames.</summary>
    internal readonly record struct PointerDrag(float X, float Y, float Dx, float Dy, int Frames, MouseButton Button)
    {
        /// <summary>Where the pointer is after the given step, the last being the end.</summary>
        public (float X, float Y) At(int step) => (X + (Dx * step / Frames), Y + (Dy * step / Frames));
    }

    /// <summary>Reads <c>input.drag</c>'s words, or says what is wrong with them.</summary>
    internal static bool TryReadDrag(string line, out PointerDrag drag, out string problem)
    {
        drag = default;
        problem = "input.drag takes where it starts, how far it goes and over how many frames, as in input.drag 400 300 120 0 10, and a button after them where it is not the left one.";

        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length is < 5 or > 6) return false;

        var numbers = new float[4];
        for (var i = 0; i < 4; i++)
        {
            if (!float.TryParse(words[i], NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i])) return false;
        }

        if (!int.TryParse(words[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var frames) || frames < 1) return false;

        var button = MouseButton.Left;
        if (words.Length == 6 && (!TryName(words[5], out button) || button is not (MouseButton.Left or MouseButton.Right or MouseButton.Middle)))
        {
            problem = $"'{words[5]}' is not a button a drag holds. They are Left, Right and Middle.";
            return false;
        }

        drag = new PointerDrag(numbers[0], numbers[1], numbers[2], numbers[3], Math.Min(frames, (int)ConsoleHost.LaterFrames - 4), button);
        return true;
    }

    /// <summary>Lists the connected gamepads, by the number the pad commands name them by.</summary>
    [Command("input.pads", "Lists the connected gamepads")]
    internal static string Pads()
    {
        var pads = PadsNow();
        return pads.Count == 0
            ? "no gamepad is connected; input.button and input.axis connect a console pad"
            : string.Join("\n", pads.Select((pad, index) => $"{index}: {pad.Name} ({pad.Entity})"));
    }

    /// <summary>
    /// Holds a gamepad's button for a number of frames, on a console pad when none is connected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pads are numbered as <c>input.pads</c> lists them. Naming the number after the last, which
    /// is 0 on a machine with none, connects a pretended pad there first, so a script presses a
    /// button where no pad is attached. A pad connected that way is a pad from the next frame,
    /// which is when the button goes down, and the answer comes once it is let go, as
    /// <c>input.hold</c> answers for keys.
    /// </para>
    /// <para>
    /// The button is a <see cref="GamepadButton"/> by name, South for A or Cross, and the
    /// triggers are LeftTrigger2 and RightTrigger2.
    /// </para>
    /// </remarks>
    [Command("input.button", "Holds a gamepad button for frames, on a console pad when none is connected: input.button <pad> <South|Start|...> <frames>")]
    internal static string PadButton(string line)
    {
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 3
            || !int.TryParse(words[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
            || !TryName<GamepadButton>(words[1], out var button)
            || !int.TryParse(words[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var frames))
        {
            ConsoleHost.Fail("BAD_ARGUMENTS", "input.button takes a pad's number, a button and a number of frames, as in input.button 0 South 10.");
            return "input.button <pad> <South|Start|...> <frames>";
        }

        if (!TryPad(index, out var pad, out var problem))
        {
            ConsoleHost.Fail("BAD_ARGUMENT", problem);
            return problem;
        }

        var held = (ulong)Math.Clamp(frames, 1, (int)ConsoleHost.LaterFrames - 4);
        ulong? until = null;

        ConsoleHost.Later(() =>
        {
            // Down once the pad is one, which for a console pad connected by this command is the
            // next frame.
            if (until is null)
            {
                if (!PadsNow().Any(known => known.Entity == pad)) return null;

                SyntheticInput.SetGamepadButton(pad, button, 1f);
                until = ConsoleHost.Time.FrameCount + held;
                return null;
            }

            if (ConsoleHost.Time.FrameCount < until) return null;

            SyntheticInput.SetGamepadButton(pad, button, 0f);
            return $"held {button} on pad {index} for {held} frames";
        });

        return $"holding {button} on pad {index} for {held} frames";
    }

    /// <summary>
    /// Sets a gamepad's axis until it is set again, on a console pad when none is connected.
    /// </summary>
    /// <remarks>
    /// Numbered and connected as <c>input.button</c> does it. A stick's axis is from minus one to
    /// one, LeftX, LeftY, RightX and RightY with right and up positive, and a trigger's is from
    /// zero to one, LeftTrigger and RightTrigger. The answer comes once the pad holds the value.
    /// </remarks>
    [Command("input.axis", "Sets a gamepad axis until it is set again, on a console pad when none is connected: input.axis <pad> <LeftX|LeftY|RightX|RightY|LeftTrigger|RightTrigger> <value>")]
    internal static string PadAxis(string line)
    {
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 3
            || !int.TryParse(words[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
            || !TryName<GamepadAxis>(words[1], out var axis)
            || !float.TryParse(words[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            ConsoleHost.Fail("BAD_ARGUMENTS", "input.axis takes a pad's number, an axis and a value, as in input.axis 0 LeftX 0.6.");
            return "input.axis <pad> <LeftX|LeftY|RightX|RightY|LeftTrigger|RightTrigger> <value>";
        }

        if (!TryPad(index, out var pad, out var problem))
        {
            ConsoleHost.Fail("BAD_ARGUMENT", problem);
            return problem;
        }

        var set = false;
        ConsoleHost.Later(() =>
        {
            if (!PadsNow().Any(known => known.Entity == pad)) return null;
            if (set) return FormattableString.Invariant($"{axis} on pad {index} at {value}");

            SyntheticInput.SetGamepadAxis(pad, axis, value);
            set = true;
            return null;
        });

        return FormattableString.Invariant($"setting {axis} on pad {index} to {value}");
    }

    /// <summary>The pads as this frame reads them.</summary>
    private static IReadOnlyList<Gamepad> PadsNow() => ConsoleHost.World?.Resource<Input>().Gamepads ?? [];

    /// <summary>The pad a number names, connecting a console pad where it is the number after the last.</summary>
    private static bool TryPad(int index, out Entity pad, out string problem)
    {
        var pads = PadsNow();
        pad = Entity.None;
        problem = string.Empty;

        if (index >= 0 && index < pads.Count)
        {
            pad = pads[index].Entity;
            return true;
        }

        if (index == pads.Count)
        {
            pad = SyntheticInput.ConnectGamepad();
            return true;
        }

        problem = $"There is no pad {index}. {pads.Count} {(pads.Count == 1 ? "is" : "are")} connected, and {pads.Count} would connect a console pad.";
        return false;
    }

    /// <summary>Turns the wheel.</summary>
    [Command("input.wheel", "Turns the wheel, negative is down: input.wheel <lines>")]
    internal static string Wheel(float lines)
    {
        SyntheticInput.Wheel(lines);
        return $"wheel {lines:0.##}";
    }

    /// <summary>Types text.</summary>
    /// <remarks>
    /// Into the interface's own queue, which is where a field being typed into reads. An app with
    /// no interface has no such queue, and saying it typed there would be saying something that
    /// did not happen, so it refuses and points at <c>input.key</c>, which starts at the window and
    /// types each letter it taps.
    /// </remarks>
    [Command("input.type", "Types text where the focus is: input.type <text>")]
    internal static string Type(string text)
    {
        if (text.Length == 0) return "nothing to type";

        if (!ImGuiRuntime.IsRunning)
        {
            ConsoleHost.Fail(
                "NO_INTERFACE",
                "This app has no interface to type into. input.key taps a key at the window and types its letter.");
            return "there is no interface to type into";
        }

        SyntheticInput.Type(text);
        return $"typed {text}";
    }

    /// <summary>Pretends files were dropped on the window.</summary>
    /// <remarks>
    /// Each path is a <see cref="FileDropped"/> on the message bus, as a file dragged onto the window
    /// is, which a game reads from the next frame, so the way it takes files dropped on it is tried
    /// by a script with nothing dragged. Paths are split by semicolons, since a path holds spaces,
    /// and are not looked for on disk, a drop saying what was dropped and not that it is there.
    /// </remarks>
    [Command("input.drop", "Pretends files were dropped on the window: input.drop <path>[;<path>...]")]
    internal static string Drop(string paths)
    {
        var dropped = paths.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (dropped.Length == 0)
        {
            ConsoleHost.Fail("BAD_ARGUMENT", "input.drop takes the paths of the files dropped, split by semicolons.");
            return "input.drop <path>[;<path>...]";
        }

        if (ConsoleHost.World?.Resource<MessageBus>() is not { } bus)
        {
            ConsoleHost.Fail("NO_WORLD", "There is no world running to drop files on.");
            return "no world to drop files on";
        }

        foreach (var path in dropped) bus.Send(new FileDropped(path));
        return dropped.Length == 1 ? $"dropped {dropped[0]}" : $"dropped {dropped.Length} files";
    }

    /// <summary>Taps one named key.</summary>
    [Command("input.key", "Taps a key by name: input.key <Escape|Enter|A|Digit1|...>")]
    internal static string Tap(string name)
    {
        if (!TryName<Key>(name, out var key))
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{name}' is not a key name.");
            return $"'{name}' is not a key name";
        }

        SyntheticInput.Tap(key, Typed(key));
        return $"tapped {key}";
    }

    /// <summary>Holds a key down until <c>input.keyup</c> lets it go.</summary>
    /// <remarks>
    /// A tap is a press and a release in one frame, which a menu reads and a game walking while a
    /// key is down never sees, so a script playing a game holds the key, waits the frames it walks
    /// for, and lets it go.
    /// </remarks>
    [Command("input.keydown", "Holds a key down: input.keydown <W|Space|...>")]
    internal static string KeyDown(string name)
    {
        if (!TryName<Key>(name, out var key))
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{name}' is not a key name.");
            return $"'{name}' is not a key name";
        }

        SyntheticInput.Press(key, Typed(key));
        return $"holding {key}";
    }

    /// <summary>Holds keys down for a number of frames, and answers once they are let go.</summary>
    /// <remarks>
    /// <para>
    /// One command rather than a hold, a wait and a release, since each call to a running app costs
    /// a few frames, and a script steering a game by three of them moves it however far it goes in
    /// those as well as in the frames it asked for. Here the keys go down now and come up after
    /// exactly the frames given, inside the app, and the answer comes once they have.
    /// </para>
    /// <para>
    /// Several keys are named with commas between them, as <c>input.hold W,D 12</c> walks diagonally
    /// for twelve frames. Held at most <see cref="ConsoleHost.LaterFrames"/> frames, which is as
    /// long as a caller waits for an answer.
    /// </para>
    /// </remarks>
    [Command("input.hold", "Holds keys for a number of frames: input.hold <W|W,D|...> <frames>")]
    internal static string Hold(string line)
    {
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 2 || !int.TryParse(words[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var frames))
        {
            ConsoleHost.Fail("BAD_ARGUMENTS", "input.hold takes keys and a number of frames, as in input.hold W,D 12.");
            return "input.hold <W|W,D|...> <frames>";
        }

        var keys = new List<Key>();
        foreach (var name in words[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryName<Key>(name, out var key))
            {
                ConsoleHost.Fail("BAD_ARGUMENT", $"'{name}' is not a key name.");
                return $"'{name}' is not a key name";
            }

            keys.Add(key);
        }

        var held = (ulong)Math.Clamp(frames, 1, (int)ConsoleHost.LaterFrames - 1);
        var until = ConsoleHost.Time.FrameCount + held;

        foreach (var key in keys) SyntheticInput.Press(key, Typed(key));

        ConsoleHost.Later(() =>
        {
            if (ConsoleHost.Time.FrameCount < until) return null;

            foreach (var key in keys) SyntheticInput.Lift(key);
            return $"held {string.Join(',', keys)} for {held} frames";
        });

        return $"holding {string.Join(',', keys)} for {held} frames";
    }

    /// <summary>Lets go of a key <c>input.keydown</c> held.</summary>
    [Command("input.keyup", "Lets a held key go: input.keyup <W|Space|...>")]
    internal static string KeyUp(string name)
    {
        if (!TryName<Key>(name, out var key))
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{name}' is not a key name.");
            return $"'{name}' is not a key name";
        }

        SyntheticInput.Lift(key);
        return $"let go of {key}";
    }

    /// <summary>
    /// Taps one key in the interface's own queue.
    /// </summary>
    /// <remarks>
    /// The other end of <c>input.key</c>, and the one a field being typed into is listening on. A
    /// key starts at the window and reaches the interface through it, so either route types a
    /// letter. Enter, Escape and the arrows inside a text field are read by the interface directly,
    /// so a window key alone leaves a line typed and never submitted.
    /// </remarks>
    [Command("input.uikey", "Taps a key in the interface: input.uikey <Enter|Escape|UpArrow|Tab>")]
    internal static string UiKey(string name)
    {
        if (!TryName<ImGuiKey>(name, out var key))
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{name}' is not an interface key name.");
            return $"'{name}' is not an interface key name";
        }

        SyntheticInput.Key(key);
        return $"tapped {key} in the interface";
    }

    /// <summary>What a key types, for the ones that type something.</summary>
    private static string Typed(Key key) => key switch
    {
        >= Key.A and <= Key.Z => ((char)('a' + (key - Key.A))).ToString(),
        >= Key.Digit0 and <= Key.Digit9 => ((char)('0' + (key - Key.Digit0))).ToString(),
        Key.Space => " ",
        _ => string.Empty,
    };
}
