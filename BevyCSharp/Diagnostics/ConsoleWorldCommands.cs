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
                var value = field.Read(world, entity);

                // A row shown only under a condition reads nothing while the condition fails, as
                // the fields of an enum variant other than the chosen one do. Listing every such
                // row as "none" would bury the few that hold something.
                if (value is null && field.Hints.Conditions.Count > 0) continue;

                rows.Add($"{schema.Name}.{field.Name} = {Shortened(Show(value))}");
            }
        }

        return string.Join("\n", rows);
    }

    /// <summary>Changes one field on one entity.</summary>
    /// <remarks>
    /// <para>
    /// The value is read into whatever the field already holds, so a number goes in as a number and
    /// a name of an enum goes in as that enum. Three numbers separated by commas make a vector, and a
    /// rotation is the three angles in degrees the inspector shows or a quaternion's four numbers.
    /// </para>
    /// <para>
    /// The component is the longest name on the entity that the argument starts with, and the rest
    /// is the field. Splitting at the last dot instead would break a field inside a nested struct,
    /// whose name has dots of its own (<c>PointLight.color.Srgba.red</c>), and a component named
    /// by its full path, whose name has none to split at. Bevy names fields in lower case and C# in
    /// Pascal case, so a field that does not match exactly is matched ignoring case.
    /// </para>
    /// </remarks>
    [Command("entity.set", "Changes a field: entity.set <name|#index> <Component.Field> <value>")]
    internal static string Set(string which, string field, string value)
    {
        var world = ConsoleHost.Ecs;

        if (Find(world, which) is not { } entity) return Missing(which);

        var schema = world.ComponentsOf(entity)
            .Select(ComponentSchemas.For)
            .OfType<ComponentSchema>()
            .Where(schema =>
                field.Length > schema.Name.Length + 1
                && field[schema.Name.Length] == '.'
                && field.StartsWith(schema.Name, StringComparison.OrdinalIgnoreCase))
            .MaxBy(schema => schema.Name.Length);

        if (schema is null)
        {
            if (!field.Contains('.'))
            {
                ConsoleHost.Fail("BAD_ARGUMENT", $"'{field}' is not a Component.Field.");
                return $"'{field}' is not a Component.Field";
            }

            ConsoleHost.Fail("NO_SUCH_FIELD", $"{which} carries no {field}.");
            return $"{which} carries no {field}";
        }

        var fieldName = field[(schema.Name.Length + 1)..];
        var found = schema.Field(fieldName)
            ?? schema.Fields.FirstOrDefault(candidate =>
                candidate.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

        if (found is not null)
        {
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

            // Kept as an override when the entity is a node of an instance, as the inspector keeps
            // it, so a scene saved afterward holds the change.
            SceneInstances.Mark(world, entity, schema.QualifiedName, found.Name, was);

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

    /// <summary>Holds a key down until <c>input.keyup</c> lets it go.</summary>
    /// <remarks>
    /// A tap is a press and a release in one frame, which a menu reads and a game walking while a
    /// key is down never sees, so a script playing a game holds the key, waits the frames it walks
    /// for, and lets it go.
    /// </remarks>
    [Command("input.keydown", "Holds a key down: input.keydown <W|Space|...>")]
    internal static string KeyDown(string name)
    {
        if (!Enum.TryParse<Key>(name, ignoreCase: true, out var key))
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
            if (!Enum.TryParse<Key>(name, ignoreCase: true, out var key))
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
        if (!Enum.TryParse<Key>(name, ignoreCase: true, out var key))
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
    /// it with <c>frames.wait 3</c> rather than reading it the moment this answers.
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
                "This app is running headless, so it draws nothing to capture. Start one with a "
                + "window (bcs open --editor), or one that draws offscreen (bcs open --sample "
                + "--offscreen), which works on a machine with no display.");

            return "headless, so nothing is being drawn to capture";
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

    /// <summary>Stops or starts the game's clock.</summary>
    /// <remarks>
    /// The window, the interface and this console go on while the game is paused, so a stopped
    /// world can be looked at, changed with <c>entity.set</c> and stepped a frame at a time.
    /// </remarks>
    [Command("app.pause", "Stops the game's clock, or starts it again: app.pause [on|off]")]
    internal static string Pause(string line)
    {
        var time = ConsoleHost.Time;
        var wanted = line.Trim() switch
        {
            "on" or "true" or "1" => true,
            "off" or "false" or "0" => false,
            _ => !time.Paused,
        };

        if (wanted) time.Pause();
        else time.Resume();

        return wanted ? "paused" : "running";
    }

    /// <summary>Runs a paused game's clock for a few frames and stops it again.</summary>
    [Command("app.step", "Runs the game's clock for some frames, then stops it: app.step [frames]")]
    internal static string Step(string line)
    {
        var frames = int.TryParse(line.Trim(), out var asked) ? Math.Clamp(asked, 1, 10_000) : 1;
        ConsoleHost.Time.Step(frames);
        return frames == 1 ? "stepping a frame" : $"stepping {frames} frames";
    }

    /// <summary>Sets how fast the game's clock runs.</summary>
    [Command("app.speed", "How fast the game's clock runs, one being real time: app.speed <times>")]
    internal static string Speed(string line)
    {
        var time = ConsoleHost.Time;
        if (line.Trim().Length == 0) return $"speed {time.Speed:0.###}";

        if (!float.TryParse(line.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var speed) || speed < 0f)
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{line.Trim()}' is not a speed of zero or more.");
            return $"'{line.Trim()}' is not a speed";
        }

        time.SetSpeed(speed);
        return $"speed {speed:0.###}";
    }

    /// <summary>Closes the app.</summary>
    [Command("app.quit", "Asks the app to close after this frame")]
    internal static string Quit()
    {
        App.RequestExit();
        return "closing";
    }

    /// <summary>Gives an entity a name.</summary>
    [Command("entity.rename", "Names an entity: entity.rename <name|#index> <new name>")]
    internal static string Rename(string line)
    {
        // One line, so a name with spaces in it is the rest of the line, and the entity is the
        // first word, or the words in quotes when its own name has a space.
        var words = FirstAndRest(line);
        if (words.Length < 2)
        {
            ConsoleHost.Fail("BAD_ARGUMENTS", "entity.rename takes an entity and a name, as in entity.rename #12 Wall");
            return "entity.rename <name|#index> <new name>";
        }

        var world = ConsoleHost.Ecs;
        if (Find(world, words[0]) is not { } entity) return Missing(words[0]);

        world.SetName(entity, words[1]);
        return $"#{entity.Index} is called {words[1]}";
    }

    /// <summary>Puts a component on an entity, at its default values.</summary>
    /// <remarks>
    /// By the name the inspector shows it under, a game's own or Bevy's. The fields are then set
    /// with <c>entity.set</c>, as a component added in the editor is filled in afterward.
    /// </remarks>
    [Command("entity.add", "Puts a component on an entity: entity.add <name|#index> <Component>")]
    internal static string AddComponent(string which, string component)
    {
        var world = ConsoleHost.Ecs;
        if (Find(world, which) is not { } entity) return Missing(which);

        if (ComponentSchemas.For(component) is not { } schema)
        {
            ConsoleHost.Fail("NO_SUCH_COMPONENT", $"No component is called {component}.");
            return $"no component called {component}";
        }

        return schema.Add(world, entity) ? $"#{entity.Index} carries {schema.Name}" : $"{which} already carries {schema.Name}";
    }

    /// <summary>Changes one setting of the material an entity is drawn with.</summary>
    /// <remarks>
    /// The material itself, which every entity drawn with it shares, as the editor's Material card
    /// changes it. A color is hex, <c>#rrggbb</c> or with alpha, and a number is a number.
    /// </remarks>
    [Command("material.set", "Changes an entity's material: material.set <name|#index> <color|emissive|metallic|roughness|unlit> <value>")]
    internal static string Material(string which, string setting, string value)
    {
        var world = ConsoleHost.Ecs;
        if (Find(world, which) is not { } entity) return Missing(which);

        var material = Render.MaterialOf(world, entity);
        if (!material.IsValid || !Render.TryReadMaterial(material, out var settings) || settings is null)
        {
            ConsoleHost.Fail("NO_MATERIAL", $"{which} is not drawn with a standard material.");
            return $"{which} has no standard material";
        }

        try
        {
            switch (setting.ToLowerInvariant())
            {
                case "color":
                    var color = Color.FromHex(value);
                    settings.BaseColor = (color.R, color.G, color.B, color.A);
                    break;
                case "emissive":
                    var glow = Color.FromHex(value);
                    settings.Emissive = (glow.R, glow.G, glow.B, 1f);
                    break;
                case "metallic":
                    settings.Metallic = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "roughness":
                    settings.Roughness = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "unlit":
                    settings.Unlit = value is "1" or "true" or "on";
                    break;
                default:
                    ConsoleHost.Fail("NO_SUCH_SETTING", $"A material has no setting called {setting} here.");
                    return $"no setting called {setting}";
            }
        }
        catch (FormatException error)
        {
            ConsoleHost.Fail("BAD_VALUE", error.Message);
            return error.Message;
        }

        Render.WriteMaterial(material, settings);
        return $"{which}'s material has {setting} {value}";
    }

    /// <summary>
    /// A line split into its first word and the rest, the first being the words in quotes when the
    /// line starts with one, so an entity whose name has a space can be named.
    /// </summary>
    internal static string[] FirstAndRest(string line)
    {
        var trimmed = line.Trim();

        string[] split;
        if (trimmed.StartsWith('"') && trimmed.IndexOf('"', 1) is > 0 and var close)
        {
            var rest = trimmed[(close + 1)..].Trim();
            split = rest.Length > 0 ? [trimmed[1..close], rest] : [trimmed[1..close]];
        }
        else
        {
            split = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        }

        // The rest without the quotes a caller put round it as a whole, as bcs does for a word
        // with a space in it.
        if (split.Length == 2 && split[1].Length > 1 && split[1][0] == '"' && split[1][^1] == '"')
            split[1] = split[1][1..^1];

        return split;
    }

    /// <summary>The entity a word names, by name or by <c>#index</c>.</summary>
    internal static Entity? Find(EcsWorld world, string which)
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
    internal static string Missing(string which)
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
    /// listing of those in full is a screenful per entity. Somebody is scanning for the type's own
    /// name.
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

    /// <summary>
    /// A value cut down to a line, for a value Bevy reflects as pages of JSON.
    /// </summary>
    /// <remarks>
    /// A light's shadow cascades are a matrix per cascade per camera, and listing them whole would
    /// push every other row off the screen. The whole value is still there to ask for through
    /// <c>eval</c> and <c>GetReflected</c>.
    /// </remarks>
    private static string Shortened(string shown)
    {
        const int Line = 160;
        return shown.Length <= Line ? shown : $"{shown[..Line]}... ({shown.Length} characters)";
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
    /// The current value says what the field is, because nothing else on this side knows. The
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
        Quat => Rotation(value),
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

    /// <summary>
    /// A rotation, as the three angles in degrees the inspector shows for it or as the four numbers
    /// of a quaternion, x, y, z and w.
    /// </summary>
    /// <remarks>
    /// The angles as the inspector turns them (<see cref="Quat.FromEuler"/>), so a rotation read off
    /// the panel can be typed back. A quaternion is made whole, so four numbers rounded to a few
    /// places still name a rotation.
    /// </remarks>
    private static object? Rotation(string value)
    {
        var parts = value.Trim('(', ')', ' ').Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length is not (3 or 4)) return null;

        var numbers = new float[parts.Length];
        for (var index = 0; index < parts.Length; index++)
        {
            if (!float.TryParse(parts[index], NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[index]))
                return null;
        }

        if (parts.Length == 3)
        {
            const float Radians = MathF.PI / 180f;
            return Quat.FromEuler(numbers[0] * Radians, numbers[1] * Radians, numbers[2] * Radians);
        }

        var length = MathF.Sqrt(numbers.Sum(n => n * n));
        if (length < 1e-6f) return null;

        return new Quat(numbers[0] / length, numbers[1] / length, numbers[2] / length, numbers[3] / length);
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
