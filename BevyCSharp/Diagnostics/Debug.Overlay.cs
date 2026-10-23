using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Bevy.Interop;
using Bevy.Reflected;

namespace Bevy;

// The overlay's two columns of lines, made of the engine's interface nodes the first time it shows
// and written again a few times a second while it does, what each line says, and the strip of the
// frames' times under the first lines, an image drawn again every frame while it shows.
public static unsafe partial class Debug
{
    /// <summary>The frames the strip holds, four seconds at sixty a second, as Minecraft's graph holds.</summary>
    internal const int StripFrames = 240;

    /// <summary>The strip's height in pixels, a bar's pixel for each millisecond and a bit.</summary>
    private const int StripHeight = 48;

    /// <summary>The time the strip's top stands for, in milliseconds, so a frame of 20 fps fills it.</summary>
    private const float StripTop = 50f;

    /// <summary>The passes the profiler lists, the slowest, with a line saying how many more there are.</summary>
    private const int ProfilerPasses = 24;

    private static App? _app;
    private static Entity _root = Entity.None;
    private static Entity _leftColumn = Entity.None;
    private static Entity _rightColumn = Entity.None;
    private static Entity _headColumn = Entity.None;
    private static Entity _besideColumn = Entity.None;
    private static Entity _restColumn = Entity.None;
    private static Entity _stripNode = Entity.None;
    private static AssetHandle _strip;
    private static readonly List<Line> HeadLines = [];
    private static readonly List<Line> BesideLines = [];
    private static readonly List<Line> LeftLines = [];
    private static readonly List<Line> RightLines = [];
    private static readonly float[] Times = new float[StripFrames];
    private static readonly byte[] StripPixels = new byte[StripFrames * StripHeight * 4];
    private static int _timesNext;
    private static int _timesHeld;
    private static double _written = double.NegativeInfinity;
    private static bool _rootShown;
    private static bool _chord;
    private static bool _folded;
    private static (bool Text, bool Graph)? _fpsOverlay;
    private static string[]? _system;

    /// <summary>What the overlay says, a list for each place a line goes, an empty line the gap between two parts.</summary>
    /// <param name="Head">The engine's version and the frame, over the strip.</param>
    /// <param name="Beside">The frames' times and the GPU's, beside the strip.</param>
    /// <param name="Left">The camera, the world and the program's sections, under the strip.</param>
    /// <param name="Right">The system, the GPU, the display and the renderer, with the profiler's passes under them.</param>
    internal sealed record Said(List<string> Head, List<string> Beside, List<string> Left, List<string> Right);

    /// <summary>A line of the overlay, its backing node, its text and what the text says, nothing until it is first written.</summary>
    private sealed class Line(Entity backing, Entity text)
    {
        public readonly Entity Backing = backing;
        public readonly Entity Text = text;
        public string? Says;
    }

    /// <summary>Takes the key, and makes, shows, hides and writes the overlay, once a frame from the engine's tick in a run that draws.</summary>
    internal static void TickOverlay(World world)
    {
        var ecs = world.Resource<EcsWorld>();
        var input = world.Resource<Input>();
        var time = world.Resource<Time>();

        // The overlay's nodes, its strip and its frames are an app's, and the next app makes its own.
        if (!ReferenceEquals(App.Running, _app))
        {
            _app = App.Running;
            (_root, _leftColumn, _rightColumn, _written, _rootShown) = (Entity.None, Entity.None, Entity.None, double.NegativeInfinity, false);
            (_headColumn, _besideColumn, _restColumn, _stripNode, _strip) = (Entity.None, Entity.None, Entity.None, Entity.None, default);
            (_timesNext, _timesHeld, _folded, _fpsOverlay) = (0, 0, false, null);
            HeadLines.Clear();
            BesideLines.Clear();
            LeftLines.Clear();
            RightLines.Clear();
        }

        // Every frame's time is kept whether the overlay shows or not, so the strip it opens on is
        // the last four seconds and not an empty one. The first frame, which has no time, is not.
        if (time.Delta > 0f)
        {
            Times[_timesNext] = time.Delta * 1000f;
            _timesNext = (_timesNext + 1) % StripFrames;
            _timesHeld = Math.Min(_timesHeld + 1, StripFrames);
        }

        if (OverlayKey is { } key)
        {
            if (input.KeyDown(key) && input.AnyKeyPressed() && !input.KeyPressed(key)) _chord = true;
            if (input.KeyReleased(key))
            {
                if (!_chord && !ImGuiRuntime.Typing) Turn(input.KeyDown(Key.ShiftLeft) || input.KeyDown(Key.ShiftRight));
                _chord = false;
            }
        }

        if (OverlayShown != _folded) Fold(world, OverlayShown);
        if (OverlayShown && _root.IsNone) Make(ecs);
        if (_root.IsNone) return;

        if (OverlayShown != _rootShown)
        {
            ecs.Set(_root, new Visibility(OverlayShown ? VisibilityMode.Inherited : VisibilityMode.Hidden));
            _rootShown = OverlayShown;
        }

        if (!OverlayShown) return;

        // The strip moves every frame, as a frame time graph does, and its pixels are a few tens
        // of kilobytes.
        DrawStrip();
        Render.WriteImagePixels(_strip, StripPixels);

        var now = time.ElapsedSeconds;
        if (now - _written < 0.25) return;

        _written = now;
        var said = Lines(world);
        Write(ecs, _headColumn, HeadLines, said.Head);
        Write(ecs, _besideColumn, BesideLines, said.Beside);
        Write(ecs, _restColumn, LeftLines, said.Left);
        Write(ecs, _rightColumn, RightLines, said.Right);
    }

    /// <summary>
    /// The key let go alone, which shows or hides the overlay, or with Shift the profiler, showing
    /// the overlay with it, as Minecraft's Shift+F3 opens its debug screen with the profiler's chart.
    /// </summary>
    /// <remarks>
    /// The key alone hides the profiler with the overlay, so the overlay shown again by it alone
    /// comes back without it, as Minecraft's does.
    /// </remarks>
    private static void Turn(bool shift)
    {
        if (shift)
        {
            ProfilerShown = !ProfilerShown;
            if (ProfilerShown) OverlayShown = true;
            return;
        }

        OverlayShown = !OverlayShown;
        if (!OverlayShown) ProfilerShown = false;
    }

    /// <summary>
    /// Hides Bevy's FPS overlay while the engine's shows, since both stand at the top left and the
    /// engine's says the frame's rate and draws its times too, and gives it back as it was after.
    /// </summary>
    /// <remarks>
    /// Its text and its graph are hidden apart, so each comes back as the program had it, and an
    /// app without it, which asked for no <see cref="DevTools.FpsOverlay"/>, has nothing done.
    /// </remarks>
    private static void Fold(World world, bool shown)
    {
        _folded = shown;
        if (!world.Resource<Config>().DevTools.HasFlag(DevTools.FpsOverlay)) return;
        if (world.Resource<EcsWorld>().Resource<FpsOverlayConfigRef>() is not { } overlay) return;

        if (shown)
        {
            _fpsOverlay = (overlay.Enabled, overlay.FrameTimeGraphConfigEnabled);
            overlay.Enabled = false;
            overlay.FrameTimeGraphConfigEnabled = false;
        }
        else if (_fpsOverlay is { } was)
        {
            overlay.Enabled = was.Text;
            overlay.FrameTimeGraphConfigEnabled = was.Graph;
            _fpsOverlay = null;
        }
    }

    /// <summary>The frames' times the strip holds, in milliseconds, the oldest first.</summary>
    internal static float[] FrameTimes()
    {
        var times = new float[_timesHeld];
        var first = (_timesNext - _timesHeld + StripFrames) % StripFrames;
        for (var i = 0; i < _timesHeld; i++) times[i] = Times[(first + i) % StripFrames];
        return times;
    }

    /// <summary>
    /// The last frame's time, the frames' average and their one percent low, the rate the slowest
    /// hundredth of them came at, which a game that stutters shows where its average does not.
    /// </summary>
    /// <remarks>
    /// The slowest hundredth is at least one frame, so 240 frames give the slowest three, and their
    /// times are averaged before the rate is taken, as the common tools take it.
    /// </remarks>
    internal static (float Now, float Average, float LowFps) Stats(ReadOnlySpan<float> times)
    {
        if (times.IsEmpty) return (0f, 0f, 0f);

        var sorted = times.ToArray();
        Array.Sort(sorted);
        var slowest = (sorted.Length + 99) / 100;
        var low = 0f;
        for (var i = sorted.Length - slowest; i < sorted.Length; i++) low += sorted[i];
        low /= slowest;

        var sum = 0f;
        foreach (var t in times) sum += t;

        return (times[^1], sum / times.Length, low > 0f ? 1000f / low : 0f);
    }

    /// <summary>
    /// What the overlay says, a list for each place a line goes. Only valid inside a system.
    /// </summary>
    internal static Said Lines(World world)
    {
        var ecs = world.Resource<EcsWorld>();
        var time = world.Resource<Time>();
        var held = Held();
        var timings = Render.Timings();

        NativeSceneCounts scene = default;
        var counted = App.HasRenderer && Native.bcs_render_scene_counts(&scene) == NativeStatus.Ok;

        var head = new List<string>
        {
            Say($"BevyCSharp {Version()} ({(App.HasEditor ? "editor" : App.HasRenderer ? "render" : "headless")} bridge, ABI {Native.ExpectedAbiVersion})"),
            Say($"{time.SmoothedFps:0} fps, {time.Delta * 1000f:0.0} ms a frame, frame {time.FrameCount}"),
        };

        var (now, average, lowFps) = Stats(FrameTimes());
        var beside = new List<string>
        {
            Say($"Now {now:0.0} ms, average {average:0.0} ms, 1% low {lowFps:0} fps"),
            GpuTime(timings),
        };

        var left = new List<string>();

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
        AddSections(left);

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
        right.Add(SceneField.Said());
        if (Render.MeshletsActive) right.Add("Meshlets on");
        if (ProfilerShown) AddPasses(right, timings);

        return new Said(head, beside, left, right);
    }

    /// <summary>
    /// The frame's time on the GPU, its passes' times added, which Bevy measures where the app
    /// asked for timings and the adapter has timestamp queries.
    /// </summary>
    /// <remarks>
    /// Only the outer passes are added, those whose names have no slash, since a span inside a
    /// pass is named after the pass and a slash and its time is part of the pass's.
    /// </remarks>
    private static string GpuTime(IReadOnlyList<PassTiming> timings)
    {
        if (timings.Count == 0) return "GPU time not measured, Config.GpuTimings measures it";

        var outer = timings.Where(pass => pass.GpuMilliseconds is not null && !pass.Name.Contains('/')).ToList();
        return outer.Count == 0
            ? "GPU time not measured, the adapter has no timestamp queries"
            : Say($"GPU {outer.Sum(pass => pass.GpuMilliseconds!.Value):0.0} ms a frame, {outer.Count} passes added");
    }

    /// <summary>The profiler's lines, the passes the slowest first by the GPU's time, then the CPU's.</summary>
    private static void AddPasses(List<string> right, IReadOnlyList<PassTiming> timings)
    {
        right.Add("");
        if (timings.Count == 0)
        {
            right.Add("Pass timings not measured, Config.GpuTimings measures them");
            return;
        }

        right.Add("Passes, the slowest first");
        var slowest = timings
            .OrderByDescending(pass => pass.GpuMilliseconds ?? -1.0)
            .ThenByDescending(pass => pass.CpuMilliseconds ?? -1.0)
            .ToList();

        foreach (var pass in slowest.Take(ProfilerPasses))
        {
            var gpu = pass.GpuMilliseconds is { } g ? Say($"GPU {g:0.00} ms") : "GPU none";
            var cpu = pass.CpuMilliseconds is { } c ? Say($"CPU {c:0.00} ms") : "CPU none";
            right.Add($"{pass.Name} {gpu}, {cpu}");
        }

        if (slowest.Count > ProfilerPasses) right.Add(Say($"and {slowest.Count - ProfilerPasses} more"));
    }

    /// <summary>
    /// The program's sections, each after a gap, its name and then its lines, or why it said
    /// nothing where its lines threw.
    /// </summary>
    /// <remarks>
    /// The list is copied before any section is asked, so a section that adds or removes one, its
    /// own among them, changes the next writing and not this one.
    /// </remarks>
    private static void AddSections(List<string> left)
    {
        (string Name, Func<IEnumerable<string>> Lines)[] sections;
        lock (Sections) sections = [.. Sections];

        foreach (var (name, lines) in sections)
        {
            left.Add("");
            left.Add(name);

            try
            {
                left.AddRange(lines());
            }
            catch (Exception error)
            {
                // Said where its lines would be rather than logged, since a section asked four
                // times a second would fill the log, and the overlay is where its author looks.
                left.Add($"{name}: {error.GetType().Name}, {error.Message}");
            }
        }
    }

    /// <summary>
    /// Draws the frames' times into the strip's pixels, a column a frame with the newest at the
    /// right, on black at half with faint rows at 60 and 30 fps, as the RivaTuner overlay draws its
    /// graph.
    /// </summary>
    /// <remarks>
    /// A bar is green to 60 fps, yellow to 30 and red past it, and a frame slower than the strip's
    /// top fills it. The strip holds no frame yet at its left until four seconds have run.
    /// </remarks>
    private static void DrawStrip()
    {
        var sixty = StripHeight - (int)MathF.Round(1000f / 60f / StripTop * StripHeight);
        var thirty = StripHeight - (int)MathF.Round(1000f / 30f / StripTop * StripHeight);
        var first = (_timesNext - _timesHeld + StripFrames) % StripFrames;
        var empty = StripFrames - _timesHeld;

        for (var x = 0; x < StripFrames; x++)
        {
            var ms = x < empty ? 0f : Times[(first + x - empty) % StripFrames];
            var bar = ms <= 0f ? 0 : Math.Clamp((int)MathF.Ceiling(ms / StripTop * StripHeight), 1, StripHeight);
            var (r, g, b) = ms <= 1000f / 60f + 0.5f ? ((byte)80, (byte)220, (byte)100) : ms <= 1000f / 30f + 0.5f ? ((byte)240, (byte)200, (byte)60) : ((byte)240, (byte)80, (byte)70);

            for (var y = 0; y < StripHeight; y++)
            {
                var at = (y * StripFrames + x) * 4;
                if (y >= StripHeight - bar)
                    (StripPixels[at], StripPixels[at + 1], StripPixels[at + 2], StripPixels[at + 3]) = (r, g, b, 255);
                else if (y == sixty || y == thirty)
                    (StripPixels[at], StripPixels[at + 1], StripPixels[at + 2], StripPixels[at + 3]) = (160, 160, 160, 160);
                else
                    (StripPixels[at], StripPixels[at + 1], StripPixels[at + 2], StripPixels[at + 3]) = (0, 0, 0, 128);
            }
        }
    }

    /// <summary>
    /// Makes the overlay's root and its two columns, over every other node and letting the pointer
    /// through, the left one the lines over the strip, the strip with the lines beside it, and the
    /// rest.
    /// </summary>
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

        _headColumn = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Start });
        var stripRow = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, Align = UiAlign.Start });
        _restColumn = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Start });
        ecs.SetParent(_headColumn, _leftColumn);
        ecs.SetParent(stripRow, _leftColumn);
        ecs.SetParent(_restColumn, _leftColumn);

        DrawStrip();
        _strip = Render.CreateImage(StripPixels, StripFrames, StripHeight);
        // Its least size its own, so a narrow window cuts the strip off rather than squeezes it.
        _stripNode = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(StripFrames),
            Height = Length.Px(StripHeight),
            MinWidth = Length.Px(StripFrames),
            MinHeight = Length.Px(StripHeight),
        });
        Ui.SetImage(_stripNode, _strip);
        _besideColumn = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Start });
        ecs.SetParent(_stripNode, stripRow);
        ecs.SetParent(_besideColumn, stripRow);

        ecs.Insert<GlobalZIndexRef>(_root).Value = 1_000_000;
        foreach (var node in (ReadOnlySpan<Entity>)[_root, _leftColumn, _rightColumn, _headColumn, stripRow, _restColumn, _stripNode, _besideColumn])
            PassThrough(ecs, node);
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

            // An empty line is a gap the height of a line, its backing hidden, which keeps its space,
            // and its text a space, since a text of nothing has no height. A line made for a gap is
            // written too, which nothing written yet tells apart from one that said nothing before.
            var gap = says[i].Length == 0;
            if (line.Says is null || (line.Says.Length == 0) != gap)
                ecs.Set(line.Backing, new Visibility(gap ? VisibilityMode.Hidden : VisibilityMode.Inherited));

            Ui.SetText(line.Text, gap ? " " : says[i]);
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
