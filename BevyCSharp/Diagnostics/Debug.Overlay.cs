using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Bevy.Interop;
using Bevy.Reflected;

namespace Bevy;

// The overlay's two columns of lines, made of the engine's interface nodes the first time it shows
// and written again a few times a second while it does, and what each line says.
public static unsafe partial class Debug
{
    private static App? _app;
    private static Entity _root = Entity.None;
    private static Entity _leftColumn = Entity.None;
    private static Entity _rightColumn = Entity.None;
    private static readonly List<Line> LeftLines = [];
    private static readonly List<Line> RightLines = [];
    private static double _written = double.NegativeInfinity;
    private static bool _rootShown;
    private static bool _chord;
    private static string[]? _system;

    /// <summary>A line of the overlay, its backing node, its text and what the text says.</summary>
    private sealed class Line(Entity backing, Entity text)
    {
        public readonly Entity Backing = backing;
        public readonly Entity Text = text;
        public string Says = "";
    }

    /// <summary>Takes the key, and makes, shows, hides and writes the overlay, once a frame from the engine's tick in a run that draws.</summary>
    internal static void TickOverlay(World world)
    {
        var ecs = world.Resource<EcsWorld>();
        var input = world.Resource<Input>();

        // The overlay's nodes are an app's, and the next app makes its own.
        if (!ReferenceEquals(App.Running, _app))
        {
            _app = App.Running;
            (_root, _leftColumn, _rightColumn, _written, _rootShown) = (Entity.None, Entity.None, Entity.None, double.NegativeInfinity, false);
            LeftLines.Clear();
            RightLines.Clear();
        }

        if (OverlayKey is { } key)
        {
            if (input.KeyDown(key) && input.AnyKeyPressed() && !input.KeyPressed(key)) _chord = true;
            if (input.KeyReleased(key))
            {
                if (!_chord && !ImGuiRuntime.Typing) OverlayShown = !OverlayShown;
                _chord = false;
            }
        }

        if (OverlayShown && _root.IsNone) Make(ecs);
        if (_root.IsNone) return;

        if (OverlayShown != _rootShown)
        {
            ecs.Set(_root, new Visibility(OverlayShown ? VisibilityMode.Inherited : VisibilityMode.Hidden));
            _rootShown = OverlayShown;
        }

        var now = world.Resource<Time>().ElapsedSeconds;
        if (!OverlayShown || now - _written < 0.25) return;

        _written = now;
        var (left, right) = Lines(world);
        Write(ecs, _leftColumn, LeftLines, left);
        Write(ecs, _rightColumn, RightLines, right);
    }

    /// <summary>
    /// What the overlay says, its left column and its right, a line each, an empty line the gap
    /// between two parts. Only valid inside a system.
    /// </summary>
    internal static (List<string> Left, List<string> Right) Lines(World world)
    {
        var ecs = world.Resource<EcsWorld>();
        var time = world.Resource<Time>();
        var held = Held();

        NativeSceneCounts scene = default;
        var counted = App.HasRenderer && Native.bcs_render_scene_counts(&scene) == NativeStatus.Ok;

        var left = new List<string>
        {
            Say($"BevyCSharp {Version()} ({(App.HasEditor ? "editor" : App.HasRenderer ? "render" : "headless")} bridge, ABI {Native.ExpectedAbiVersion})"),
            Say($"{time.SmoothedFps:0} fps, {time.Delta * 1000f:0.0} ms a frame, frame {time.FrameCount}"),
            "",
        };

        if (counted && scene.Camera != 0)
        {
            // Minecraft's north is minus Z, as this engine's forward is, and the heading turns from it
            // toward plus X, east.
            // The pitch rounded to a tenth and added to zero, so a level view reads 0.0 and not -0.0.
            var heading = (MathF.Atan2(scene.ForwardX, -scene.ForwardZ) * 180f / MathF.PI + 360f) % 360f;
            var pitch = MathF.Round(MathF.Asin(Math.Clamp(scene.ForwardY, -1f, 1f)) * 180f / MathF.PI, 1) + 0f;
            left.Add(Say($"XYZ {scene.X:0.000} / {scene.Y:0.000} / {scene.Z:0.000}"));
            left.Add(Say($"Facing {Compass(heading)} (towards {Towards(scene.ForwardX, scene.ForwardZ)}), heading {heading:0.0}, pitch {pitch:0.0}"));
            // In words, since the engine's font has no degree sign.
            left.Add(scene.Fov > 0f ? Say($"Field of view {scene.Fov * 180f / MathF.PI:0} degrees") : "Field of view none, the projection is orthographic");
        }
        else
        {
            left.Add("No camera");
        }

        left.Add("");
        left.Add(Say($"Entities {Count(held, "entities")}"));
        left.Add(Say($"Meshes {Count(held, "asset.Mesh")}, materials {Count(held, "asset.StandardMaterial")}, images {Count(held, "asset.Image")}, sounds {Count(held, "asset.AudioSource")}"));

        var system = System();
        var right = new List<string>
        {
            Say($".NET {Environment.Version}, {RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}"),
            $"{system[0]} ({system[1]})",
            Say($"CPU {system[2]}, {system[3]} cores, {Environment.ProcessorCount} threads"),
            Say($"Memory {Environment.WorkingSet / 1048576.0:0} MB of {system[4]}, managed {GC.GetTotalMemory(false) / 1048576.0:0} MB"),
            "",
        };

        if (App.DescribeAdapter() is { } adapter)
        {
            var parts = adapter.Split(" | ");
            right.Add(parts.Length > 2 ? $"GPU {parts[1]} ({Words(parts[2])}), {parts[0]}" : $"GPU {adapter}");
            if (parts.Length > 3) right.Add($"Driver {parts[3]}{(parts.Length > 4 && parts[4].Length > 0 ? " " + parts[4] : "")}");
        }

        right.Add(held.TryGetValue("gpu.allocated", out var allocated) && held.TryGetValue("gpu.reserved", out var reserved)
            ? Say($"GPU memory {allocated / 1048576.0:0} MB of {reserved / 1048576.0:0} MB reserved")
            : "GPU memory not reported by its backend");
        right.Add(Display(world));
        right.Add("");

        if (counted)
        {
            right.Add(Say($"Meshes in view {scene.Meshes:N0}, triangles {scene.Triangles:N0}"));
            right.Add(Say($"Lights {scene.Directional} directional, {scene.Point} point, {scene.Spot} spot, {scene.Shadowed} with shadow maps"));
        }

        right.Add(Render.RayTracingActive ? "Lighting ray traced, its global illumination Solari's" : "Lighting rasterized, no global illumination");
        if (Render.MeshletsActive) right.Add("Meshlets on");

        return (left, right);
    }

    /// <summary>Makes the overlay's root and its two columns, over every other node and letting the pointer through.</summary>
    private static void Make(EcsWorld ecs)
    {
        _root = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Px(0f),
            Top = Length.Px(0f),
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Direction = UiDirection.Row,
            Justify = UiJustify.SpaceBetween,
            Align = UiAlign.Start,
            Padding = Sides.All(Length.Px(2f)),
        });
        _leftColumn = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Start });
        _rightColumn = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.End });
        ecs.SetParent(_leftColumn, _root);
        ecs.SetParent(_rightColumn, _root);

        ecs.Insert<GlobalZIndexRef>(_root).Value = 1_000_000;
        foreach (var node in (ReadOnlySpan<Entity>)[_root, _leftColumn, _rightColumn]) PassThrough(ecs, node);
        ecs.SetName(_root, "Debug overlay");
        _rootShown = true;
    }

    /// <summary>Gives a column a line for each it is to say, made or taken away as the count changes, and writes each line that changed.</summary>
    private static void Write(EcsWorld ecs, Entity column, List<Line> lines, List<string> says)
    {
        while (lines.Count < says.Count)
        {
            // Minecraft's look, light gray text on a backing of black at half.
            var backing = Ui.SpawnNode(new UiSettings { Color = (0f, 0f, 0f, 0.5f), Padding = new Sides(Length.Px(3f), Length.Px(1f), Length.Px(3f), Length.Px(0f)) });
            var text = Ui.SpawnText("", new UiSettings { Color = (0.88f, 0.88f, 0.88f, 1f) }, 15f);
            ecs.SetParent(text, backing);
            ecs.SetParent(backing, column);
            PassThrough(ecs, backing);
            PassThrough(ecs, text);
            lines.Add(new Line(backing, text));
        }

        while (lines.Count > says.Count)
        {
            ecs.Despawn(lines[^1].Text);
            ecs.Despawn(lines[^1].Backing);
            lines.RemoveAt(lines.Count - 1);
        }

        for (var i = 0; i < says.Count; i++)
        {
            var line = lines[i];
            if (line.Says == says[i]) continue;

            // An empty line is a gap the height of a line, its backing hidden, which keeps its space.
            if ((line.Says.Length == 0) != (says[i].Length == 0))
                ecs.Set(line.Backing, new Visibility(says[i].Length == 0 ? VisibilityMode.Hidden : VisibilityMode.Inherited));

            Ui.SetText(line.Text, says[i].Length == 0 ? " " : says[i]);
            line.Says = says[i];
        }
    }

    /// <summary>Lets the pointer through a node of the overlay to whatever is under it.</summary>
    private static void PassThrough(EcsWorld ecs, Entity node)
    {
        var pickable = ecs.Insert<PickableRef>(node);
        pickable.ShouldBlockLower = false;
        pickable.IsHoverable = false;
    }

    /// <summary>The bridge's account of what it holds, its names and numbers.</summary>
    private static Dictionary<string, long> Held()
    {
        var words = Native.ReadText(Native.bcs_memory_describe, "reading what the bridge holds").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var held = new Dictionary<string, long>(StringComparer.Ordinal);
        for (var i = 0; i + 1 < words.Length; i += 2)
        {
            if (long.TryParse(words[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)) held[words[i]] = number;
        }

        return held;
    }

    private static long Count(Dictionary<string, long> held, string name) => held.GetValueOrDefault(name);

    /// <summary>The OS, the kernel, the CPU, its cores and the memory installed, read once for the process.</summary>
    private static string[] System()
    {
        if (_system is { } known) return known;

        var lines = Native.ReadText(Native.bcs_system_describe, "reading the system").Split('\n');
        return _system = lines.Length >= 5 ? lines : [.. lines, .. Enumerable.Repeat("not available", 5 - lines.Length)];
    }

    /// <summary>The window's size and scale, or the image's an offscreen run draws into.</summary>
    private static string Display(World world)
    {
        var config = world.Resource<Config>();
        if (config.Offscreen) return Say($"Display offscreen, {config.Width} x {config.Height}");

        try
        {
            var (width, height) = Window.Size();
            return Say($"Display {width} x {height}, scale {Window.Scale():0.##}");
        }
        catch (BevyNativeException)
        {
            return "Display none";
        }
    }

    /// <summary>The engine's version, without the commit a build adds after a plus.</summary>
    private static string Version()
    {
        var version = typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        var plus = version.IndexOf('+');
        return plus >= 0 ? version[..plus] : version;
    }

    /// <summary>The eight points of the compass a heading in degrees is nearest, north being minus Z.</summary>
    internal static string Compass(float heading) =>
        new[] { "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest" }[(int)MathF.Round(heading / 45f) % 8];

    /// <summary>The axis and its sign a heading faces nearest along the ground, as Minecraft names it.</summary>
    private static string Towards(float x, float z) =>
        MathF.Abs(x) > MathF.Abs(z) ? (x > 0f ? "positive X" : "negative X") : (z > 0f ? "positive Z" : "negative Z");

    /// <summary>A device type as words, its name's parts split apart.</summary>
    private static string Words(string type) =>
        string.Concat(type.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

    private static string Say(FormattableString line) => FormattableString.Invariant(line);
}
