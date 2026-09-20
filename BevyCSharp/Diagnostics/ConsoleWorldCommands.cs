using System.Globalization;
using ImGuiNET;

namespace Bevy;

/// <summary>
/// What the command line asks a running app.
/// </summary>
/// <remarks>
/// <para>
/// These are the ones about the app rather than about the console: what is in the world, what it
/// looks like, and what happens when something is clicked. They exist because the alternative is a
/// process per question. Starting an app to ask what an entity's position is costs a second of
/// startup, a fresh world, and a guess at which frame to ask on; asking one that is already running
/// costs a frame.
/// </para>
/// <para>
/// Every one of them is an ordinary <c>[Command]</c>, so each is also something a person can type
/// into the editor's console, and the console and the command line stay the same list seen twice.
/// </para>
/// </remarks>
internal static class ConsoleWorldCommands
{
    /// <summary>Says where the app is.</summary>
    [Command("app.status", "Says what the app is doing: frame, rate, renderer, entities")]
    internal static string Status()
    {
        var time = ConsoleHost.Time;
        var world = ConsoleHost.Ecs;

        return $"frame {time.FrameCount} at {time.SmoothedFps:0.#} fps, "
               + $"{time.ElapsedSeconds:0.##}s elapsed, {world.All().Length} entities, "
               + $"renderer {(App.HasRenderer ? "yes" : "no")}, "
               + $"interface {(App.HasEditor ? "yes" : "no")}, "
               + $"adapter {App.DescribeAdapter() ?? "none"}";
    }

    /// <summary>Says the last few lines the app wrote.</summary>
    [Command("log.tail", "The last lines written: log.tail <count>")]
    internal static string Tail(int count)
    {
        var lines = ConsoleLog.All();
        if (lines.Length == 0) return "the log is empty";

        var wanted = Math.Clamp(count, 1, lines.Length);
        var chosen = lines[^wanted..];

        return string.Join(
            "\n",
            chosen.Select(line => line.Count > 1
                ? $"[{line.Frame}] {line.Text} (x{line.Count})"
                : $"[{line.Frame}] {line.Text}"));
    }

    /// <summary>Lists what is in the world.</summary>
    /// <remarks>
    /// Named entities first and the rest after, because a world holds far more than anybody put in
    /// it by hand and the ones with names are the ones somebody meant.
    /// </remarks>
    [Command("entity.list", "Lists the entities, named ones first")]
    internal static string List()
    {
        var world = ConsoleHost.Ecs;
        var named = new List<string>();
        var rest = 0;

        foreach (var entity in world.All())
        {
            if (world.NameOf(entity) is not { Length: > 0 } name)
            {
                rest++;
                continue;
            }

            named.Add($"#{entity.Index} {name} [{Components(world, entity)}]");
        }

        named.Sort(StringComparer.Ordinal);

        return named.Count == 0
            ? $"nothing named, and {rest} unnamed entities"
            : string.Join("\n", named) + $"\nand {rest} unnamed";
    }

    /// <summary>Says everything one entity carries.</summary>
    [Command("entity.get", "What an entity holds: entity.get <name|#index>")]
    internal static string Get(string which)
    {
        var world = ConsoleHost.Ecs;

        if (Find(world, which) is not { } entity) return Missing(which);

        var rows = new List<string> { $"#{entity.Index} {world.NameOf(entity) ?? "(no name)"}" };

        foreach (var id in world.ComponentsOf(entity))
        {
            if (ComponentSchemas.For(id) is not { } schema)
            {
                rows.Add($"{world.ComponentName(id)} (no schema)");
                continue;
            }

            if (schema.Fields.Count == 0)
            {
                rows.Add(schema.Name);
                continue;
            }

            foreach (var field in schema.Fields)
            {
                rows.Add($"{schema.Name}.{field.Name} = {Show(field.Read(world, entity))}");
            }
        }

        return string.Join("\n", rows);
    }

    /// <summary>Changes one field on one entity.</summary>
    /// <remarks>
    /// The value is read into whatever the field already holds, so a number goes in as a number and
    /// a name of an enum goes in as that enum. Three numbers separated by commas make a vector.
    /// </remarks>
    [Command("entity.set", "Changes a field: entity.set <name|#index> <Component.Field> <value>")]
    internal static string Set(string which, string field, string value)
    {
        var world = ConsoleHost.Ecs;

        if (Find(world, which) is not { } entity) return Missing(which);

        var dot = field.LastIndexOf('.');
        if (dot <= 0 || dot == field.Length - 1)
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{field}' is not a Component.Field.");
            return $"'{field}' is not a Component.Field";
        }

        var componentName = field[..dot];
        var fieldName = field[(dot + 1)..];

        foreach (var id in world.ComponentsOf(entity))
        {
            if (ComponentSchemas.For(id) is not { } schema) continue;
            if (!schema.Name.Equals(componentName, StringComparison.OrdinalIgnoreCase)) continue;
            if (schema.Field(fieldName) is not { } found) break;

            var was = found.Read(world, entity);

            if (Parse(value, was) is not { } parsed)
            {
                ConsoleHost.Fail(
                    "BAD_ARGUMENT", $"'{value}' cannot be read as {was?.GetType().Name ?? "that"}.");
                return $"'{value}' is not a {was?.GetType().Name ?? "value"} this field can hold";
            }

            if (!found.Write(world, entity, parsed))
            {
                ConsoleHost.Fail("READ_ONLY", $"{field} cannot be written.");
                return $"{field} did not take the value";
            }

            return $"{field} = {Show(found.Read(world, entity))}";
        }

        ConsoleHost.Fail("NO_SUCH_FIELD", $"{which} carries no {field}.");
        return $"{which} carries no {field}";
    }

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
    /// Both halves in one frame, which is what a click is for an interface that reads the button's
    /// state. Something that matches a release to the press that landed on it — picking a mesh, for
    /// one — needs the two on separate frames, so it gets <c>input.press</c> and
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

    /// <summary>Turns the wheel.</summary>
    [Command("input.wheel", "Turns the wheel, negative is down: input.wheel <lines>")]
    internal static string Wheel(float lines)
    {
        SyntheticInput.Wheel(lines);
        return $"wheel {lines:0.##}";
    }

    /// <summary>Types text.</summary>
    [Command("input.type", "Types text where the focus is: input.type <text>")]
    internal static string Type(string text)
    {
        if (text.Length == 0) return "nothing to type";

        SyntheticInput.Type(text);
        return $"typed {text}";
    }

    /// <summary>Taps one named key.</summary>
    [Command("input.key", "Taps a key by name: input.key <Escape|Enter|A|Digit1|...>")]
    internal static string Tap(string name)
    {
        if (!Enum.TryParse<Key>(name, ignoreCase: true, out var key))
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{name}' is not a key name.");
            return $"'{name}' is not a key name";
        }

        SyntheticInput.Tap(key, Typed(key));
        return $"tapped {key}";
    }

    /// <summary>
    /// Taps one key in the interface's own queue.
    /// </summary>
    /// <remarks>
    /// The other end of <c>input.key</c>, and the one a field being typed into is listening on. A
    /// key starts at the window and reaches the interface through it, so either route types a
    /// letter - but Enter, Escape and the arrows inside a text field are read by the interface
    /// directly, and a window key alone leaves a line typed and never submitted.
    /// </remarks>
    [Command("input.uikey", "Taps a key in the interface: input.uikey <Enter|Escape|UpArrow|Tab>")]
    internal static string UiKey(string name)
    {
        if (!Enum.TryParse<ImGuiKey>(name, ignoreCase: true, out var key))
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{name}' is not an interface key name.");
            return $"'{name}' is not an interface key name";
        }

        SyntheticInput.Key(key);
        return $"tapped {key} in the interface";
    }

    /// <summary>Writes the window to a PNG.</summary>
    /// <remarks>
    /// The file appears a frame or two later, because the picture comes back off the GPU. Wait for
    /// it — <c>frames.wait 3</c> — rather than reading it the moment this answers.
    /// </remarks>
    [Command("shot", "Captures the window to a PNG: shot <path>")]
    internal static string Shot(string path)
    {
        if (path.Length == 0)
        {
            ConsoleHost.Fail("BAD_ARGUMENT", "shot needs a path to write to.");
            return "shot <path>";
        }

        if (!App.HasRenderer)
        {
            ConsoleHost.Fail(
                "NO_RENDERER",
                "This native bridge was built without Bevy's renderer, so there is nothing to "
                + "capture. Rebuild it with build/build-native.sh --render.");

            return "no renderer, so nothing was captured";
        }

        // Asked of the run rather than of the build. A bridge with the renderer compiled in still
        // captures nothing when this particular run opened no window, and waiting for a file that
        // was never going to appear is the slowest possible way to learn that.
        if (ConsoleHost.World?.Resource<Config>() is { Headless: true })
        {
            ConsoleHost.Fail(
                "NO_WINDOW",
                "This app is running headless, so there is no window to capture. Start one with a "
                + "window - 'bcs open --editor' - and capture that.");

            return "headless, so there is no window to capture";
        }

        Render.Screenshot(path);
        return $"capturing to {path}";
    }

    /// <summary>Answers once the frame counter has moved on.</summary>
    /// <remarks>
    /// What makes "do this, let it settle, then look" one call. An interface reacts over several
    /// frames, a capture is read back over several more, and a caller that asks immediately sees
    /// the frame before the one it caused.
    /// </remarks>
    [Command("frames.wait", "Answers after N more frames: frames.wait <count>")]
    internal static string Wait(int count)
    {
        var now = ConsoleHost.Time.FrameCount;
        var wanted = (ulong)Math.Clamp(count, 1, 10_000);

        ConsoleHost.Hold(now + wanted);
        return $"waited {wanted} frames, from {now}";
    }

    /// <summary>Closes the app.</summary>
    [Command("app.quit", "Asks the app to close after this frame")]
    internal static string Quit()
    {
        App.RequestExit();
        return "closing";
    }

    /// <summary>The entity a word names, by name or by <c>#index</c>.</summary>
    private static Entity? Find(EcsWorld world, string which)
    {
        if (which.Length == 0) return null;

        if (which[0] == '#' && uint.TryParse(which[1..], out var index))
        {
            foreach (var entity in world.All())
            {
                if (entity.Index == index) return entity;
            }

            return null;
        }

        foreach (var entity in world.All())
        {
            if (world.NameOf(entity) == which) return entity;
        }

        return null;
    }

    /// <summary>What to say, and to report, when nothing answers to a name.</summary>
    private static string Missing(string which)
    {
        ConsoleHost.Fail(
            "NO_SUCH_ENTITY", $"No entity is called '{which}'. Run entity.list to see what is.");

        return $"no entity called {which}";
    }

    /// <summary>The names of what an entity carries, as one line.</summary>
    private static string Components(EcsWorld world, Entity entity)
    {
        var names = new List<string>();

        foreach (var id in world.ComponentsOf(entity))
        {
            names.Add(ComponentSchemas.For(id)?.Name ?? Short(world.ComponentName(id)));
        }

        names.Sort(StringComparer.Ordinal);
        return string.Join(", ", names);
    }

    /// <summary>
    /// The last part of a qualified name, which is the part worth reading.
    /// </summary>
    /// <remarks>
    /// Bevy reports its own components by a Rust path with a generic argument on the end, and a
    /// listing of those in full is a screenful per entity. The type's own name is what somebody is
    /// scanning for.
    /// </remarks>
    private static string Short(string name)
    {
        var head = name;

        var generic = head.IndexOf('<');
        if (generic > 0) head = head[..generic];

        var rust = head.LastIndexOf("::", StringComparison.Ordinal);
        if (rust >= 0) head = head[(rust + 2)..];

        var dot = head.LastIndexOf('.');
        if (dot >= 0 && dot < head.Length - 1) head = head[(dot + 1)..];

        return head;
    }

    /// <summary>A field's value as one line, in a form the setter reads back.</summary>
    private static string Show(object? value) => value switch
    {
        null => "none",
        Vec3 vector =>
            $"{vector.X.ToString("0.###", CultureInfo.InvariantCulture)},"
            + $"{vector.Y.ToString("0.###", CultureInfo.InvariantCulture)},"
            + $"{vector.Z.ToString("0.###", CultureInfo.InvariantCulture)}",
        float single => single.ToString("0.###", CultureInfo.InvariantCulture),
        double number => number.ToString("0.###", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "none",
    };

    /// <summary>
    /// A word read into whatever the field already holds, or nothing when it will not go.
    /// </summary>
    /// <remarks>
    /// The current value is what says what the field is. Nothing else on this side knows: the
    /// schema describes a field's kind for an inspector to draw, and a kind is not a type.
    /// </remarks>
    private static object? Parse(string value, object? current) => current switch
    {
        bool => value.ToLowerInvariant() switch
        {
            "1" or "on" or "true" or "yes" => true,
            "0" or "off" or "false" or "no" => false,
            _ => null,
        },
        Vec3 => Vector(value),
        float => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)
            ? f
            : null,
        double => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
            ? d
            : null,
        Enum existing => Enum.TryParse(existing.GetType(), value, ignoreCase: true, out var parsed)
            ? parsed
            : null,
        string => value,
        IConvertible convertible => Convert(value, convertible),
        _ => null,
    };

    /// <summary>Three numbers separated by commas.</summary>
    private static object? Vector(string value)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 3) return null;

        var numbers = new float[3];

        for (var index = 0; index < 3; index++)
        {
            if (!float.TryParse(
                    parts[index], NumberStyles.Float, CultureInfo.InvariantCulture,
                    out numbers[index]))
            {
                return null;
            }
        }

        return new Vec3(numbers[0], numbers[1], numbers[2]);
    }

    /// <summary>Anything else that knows how to become itself from a string.</summary>
    private static object? Convert(string value, IConvertible current)
    {
        try
        {
            return System.Convert.ChangeType(
                value, current.GetTypeCode(), CultureInfo.InvariantCulture);
        }
        catch (Exception error) when (error is FormatException or OverflowException
                                          or InvalidCastException)
        {
            return null;
        }
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
