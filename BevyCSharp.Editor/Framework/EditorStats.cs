using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What the engine is doing, as a card over the scene's top right: how fast, where the time goes,
/// how much is in the world, how much memory it takes, what it is drawn with, and the keys.
/// </summary>
/// <remarks>
/// <para>
/// The card every engine's viewport has, Unity's statistics and Unreal's <c>stat</c> readouts among
/// them, because the question somebody tuning a scene asks most is whether it is fast enough and
/// what it is spending its frame on. A graph of the last few seconds of frames comes first, since a
/// hitch is a spike in a line and invisible in an average, and the numbers under it are the ones a
/// person reads off such a card: frames a second, the frame in milliseconds at its best, typical
/// and worst, and the render passes that took longest on the GPU.
/// </para>
/// <para>
/// Everything is read from what the engine already reports, and nothing is measured for the card's
/// own sake. The frame times are Bevy's own delta, the pass times are <see cref="Render.Timings"/>,
/// which the editor asks for when it is made, and the memory is .NET's own view of the managed heap
/// and of the process.
/// </para>
/// <para>
/// Shown only while it is asked for, because numbers nobody is reading are numbers lying over the
/// thing somebody is looking at.
/// </para>
/// </remarks>
public static class EditorStats
{
    /// <summary>Whether the card is up.</summary>
    public static bool Showing { get; set; }

    /// <summary>How many frames the graph and the figures under it cover.</summary>
    private const int Kept = 240;

    /// <summary>How wide the card is, which the graph takes all of.</summary>
    private const float Width = 300f;

    /// <summary>The last frames' lengths in milliseconds, oldest first once full.</summary>
    private static readonly float[] Frames = new float[Kept];

    private static int _next;
    private static int _count;

    /// <summary>What had been allocated on the managed heap as the last frame began.</summary>
    private static long _allocated = GC.GetTotalAllocatedBytes();

    /// <summary>What was allocated over the last frame, smoothed.</summary>
    private static double _perFrame;

    /// <summary>Notes the frame just gone, whether or not the card is up, so it opens on a full graph.</summary>
    internal static void Tick(BehaviorContext ctx)
    {
        Frames[_next] = (float)(ctx.Time.DeltaSeconds * 1000.0);
        _next = (_next + 1) % Kept;
        _count = Math.Min(_count + 1, Kept);

        var allocated = GC.GetTotalAllocatedBytes();
        _perFrame = (_perFrame * 0.9) + ((allocated - _allocated) * 0.1);
        _allocated = allocated;
    }

    /// <summary>Draws the card under the buttons in the scene's top right, while it is up.</summary>
    internal static void Draw(BehaviorContext ctx)
    {
        if (!Showing) return;

        var at = new Vector2(
            EditorShell.Free.Right - ToolbarView.Inset,
            ToolbarView.Origin.Y + ToolbarView.Inset + ((ToolbarView.RightRow + 1) * (EditorSurface.Tall + EditorSurface.Air)));

        ImGui.SetNextWindowPos(at, ImGuiCond.Always, new Vector2(1f, 0f));
        ImGui.SetNextWindowSizeConstraints(
            new Vector2(Width, 0f),
            new Vector2(Width, MathF.Max(120f, EditorShell.Free.Bottom - at.Y - ToolbarView.Inset)));

        ImGui.PushStyleColor(ImGuiCol.WindowBg, EditorTheme.LivePanel);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 10f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, EditorTheme.Current.ChildRounding);

        // Sized to what it holds, down to the bottom of the scene and no further, and scrolled past
        // that, since with a tab open the scene is short and a card cut off there hides the keys.
        var flags = ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav;

        if (ImGui.Begin("##stats", flags))
        {
            Performance(ctx);
            Passes();
            World(ctx);
            Memory();
            Renderer(ctx);
            Keys();
        }

        ImGui.End();

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();
    }

    /// <summary>The graph of the last frames, and the figures that sum it up.</summary>
    private static void Performance(BehaviorContext ctx)
    {
        var frames = Recent();

        var average = frames.Length == 0 ? 0f : frames.Average();
        var best = frames.Length == 0 ? 0f : frames.Min();
        var worst = frames.Length == 0 ? 0f : frames.Max();

        // Big, because it is the one number everybody opening this is looking for.
        ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));
        ImGui.TextUnformatted(Say($"{ctx.Time.SmoothedFps:0} fps"));
        ImGui.SameLine();
        ImGui.TextDisabled(Say($"{average:0.00} ms"));
        ImGui.PopFont();

        Graph(frames, worst);

        Row("best", Say($"{best:0.00} ms"));
        Row("worst", Say($"{worst:0.00} ms"), worst > average * 2f && worst > 20f);
        Row("frame", Say($"{ctx.Time.FrameCount:N0}"));
        Row("running", Duration(ctx.Time.ElapsedSeconds));
    }

    /// <summary>
    /// The frames as a line, with a rule at the length a frame at sixty a second has.
    /// </summary>
    private static void Graph(float[] frames, float worst)
    {
        const float Tall = 56f;

        var at = ImGui.GetCursorScreenPos();
        var size = new Vector2(ImGui.GetContentRegionAvail().X, Tall);
        var draw = ImGui.GetWindowDrawList();

        ImGui.Dummy(size);

        EditorDraw.Rounded(at, at + size, 8f, ImGui.GetColorU32(EditorTheme.LiveGroup), draw);

        if (frames.Length < 2) return;

        // Scaled so a smooth sixty sits a third of the way up, and a spike still fits.
        var top = MathF.Max(1000f / 20f, worst * 1.1f);
        var inside = new Vector2(size.X - 8f, size.Y - 8f);
        var origin = at + new Vector2(4f, 4f);

        float Y(float ms) => origin.Y + inside.Y - (Math.Min(ms, top) / top * inside.Y);

        var sixty = Y(1000f / 60f);

        draw.AddLine(
            new Vector2(origin.X, sixty),
            new Vector2(origin.X + inside.X, sixty),
            ImGui.GetColorU32(EditorTheme.Alpha(EditorTheme.LiveText, 0.18f)),
            1f);

        var step = inside.X / (Kept - 1);
        var start = Kept - frames.Length;
        var line = ImGui.GetColorU32(EditorTheme.LiveAccent);
        var spike = ImGui.GetColorU32(EditorTheme.Current.Warn);

        for (var index = 1; index < frames.Length; index++)
        {
            var from = new Vector2(origin.X + ((start + index - 1) * step), Y(frames[index - 1]));
            var to = new Vector2(origin.X + ((start + index) * step), Y(frames[index]));

            // A frame longer than a thirtieth of a second is a hitch somebody saw.
            draw.AddLine(from, to, frames[index] > 1000f / 30f ? spike : line, 1.5f);
        }
    }

    /// <summary>The render passes that took longest, on the GPU where the adapter measures it.</summary>
    private static void Passes()
    {
        var timings = Render.Timings();

        Heading("GPU");

        if (timings.Count == 0)
        {
            ImGui.TextDisabled("No timings, since the app did not ask for them");
            return;
        }

        var gpu = timings.Sum(timing => timing.GpuMilliseconds ?? 0);
        var cpu = timings.Sum(timing => timing.CpuMilliseconds ?? 0);

        Row("render", gpu > 0 ? Say($"{gpu:0.00} ms") : "not measured");
        Row("recording", Say($"{cpu:0.00} ms"));
        Row("passes", Say($"{timings.Count}"));

        // The few that matter, since a frame's time is nearly always in a handful of its passes.
        foreach (var timing in timings
                     .OrderByDescending(timing => timing.GpuMilliseconds ?? timing.CpuMilliseconds ?? 0)
                     .Take(4))
        {
            var took = timing.GpuMilliseconds ?? timing.CpuMilliseconds ?? 0;
            Row(timing.Name, Say($"{took:0.000} ms"), dim: true);
        }
    }

    /// <summary>How much is in the world.</summary>
    private static void World(BehaviorContext ctx)
    {
        var all = ctx.Ecs.All();
        var named = 0;
        var parented = 0;

        foreach (var entity in all)
        {
            if (ctx.Ecs.NameOf(entity) is { Length: > 0 }) named++;
            if (!ctx.Ecs.ParentOf(entity).IsNone) parented++;
        }

        Heading("World");
        Row("entities", Say($"{all.Length:N0}"));
        Row("named", Say($"{named:N0}"));
        Row("in a hierarchy", Say($"{parented:N0}"));
        Row("selected", Say($"{EditorSelection.Count:N0}"));
        Row("undo steps", EditorHistory.Last is { } last ? $"last: {last}" : "none");
    }

    /// <summary>What the process and its managed heap take.</summary>
    private static void Memory()
    {
        var info = GC.GetGCMemoryInfo();

        Heading("Memory");
        Row("process", Bytes(Environment.WorkingSet));
        Row("managed heap", Bytes(GC.GetTotalMemory(false)));
        Row("allocated a frame", Bytes((long)_perFrame), _perFrame > 64 * 1024);
        Row("collections", Say($"{GC.CollectionCount(0)} / {GC.CollectionCount(1)} / {GC.CollectionCount(2)}"));
        Row("last pause", Say($"{info.PauseDurations[0].TotalMilliseconds:0.00} ms"));
    }

    /// <summary>What it is drawn with.</summary>
    private static void Renderer(BehaviorContext ctx)
    {
        var size = ImGuiRuntime.Size * ImGuiRuntime.Scale;
        var config = ctx.Res<Config>();

        Heading("Renderer");

        // The adapter is reported as one line of fields apart by bars, the API first and the
        // device's name second, and the name is too long to sit beside anything, so it has a line
        // of its own.
        var adapter = (App.DescribeAdapter() ?? string.Empty).Split(" | ");

        Row("backend", adapter.Length > 0 && adapter[0].Length > 0 ? adapter[0] : "not reported yet");

        if (adapter.Length > 1)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, EditorTheme.Current.Dim);
            ImGui.TextWrapped(adapter[1]);
            ImGui.PopStyleColor();
        }

        Row("window", Say($"{size.X:0} x {size.Y:0} at {ImGuiRuntime.Scale:0.##}x"));
        Row("vsync", config.Vsync ? "on" : "off");
        Row("bridge", App.HasEditor ? "editor" : App.HasRenderer ? "render" : "headless");
        Row(".NET", Environment.Version.ToString());
        Row("threads", Say($"{Process.GetCurrentProcess().Threads.Count}"));
    }

    /// <summary>What the keys do here, which is what this button used to show on its own.</summary>
    private static void Keys()
    {
        Heading("Keys");

        foreach (var (key, does) in EditorHints.Current())
        {
            Row(key, does, dim: true);
        }
    }

    /// <summary>A heading over a group of rows, with the air above it that parts it from the last.</summary>
    private static void Heading(string text)
    {
        ImGui.Dummy(new Vector2(0f, 4f));
        EditorSurface.Heading(text);
    }

    /// <summary>
    /// A name on the left and its value on the right, the value in figures so a column of them
    /// holds still as they change.
    /// </summary>
    /// <param name="name">What the value is.</param>
    /// <param name="value">The value.</param>
    /// <param name="warn">Whether it is worth a second look, which colors it.</param>
    /// <param name="dim">Whether the row is detail under the one above it.</param>
    private static void Row(string name, string value, bool warn = false, bool dim = false)
    {
        if (dim) ImGui.TextDisabled(name);
        else ImGui.TextUnformatted(name);

        ImGui.SameLine();

        // Against the card's right edge, and never over the name when the two do not both fit.
        var width = ImGui.CalcTextSize(value).X;
        var from = ImGui.GetCursorPosX();
        var right = from + ImGui.GetContentRegionAvail().X;

        ImGui.SetCursorPosX(MathF.Max(from + 8f, right - width));

        var color = warn ? EditorTheme.Current.Warn : dim ? EditorTheme.Current.Dim : EditorTheme.Current.Text;

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(value);
        ImGui.PopStyleColor();
    }

    /// <summary>The frames kept, oldest first.</summary>
    private static float[] Recent()
    {
        var recent = new float[_count];

        for (var index = 0; index < _count; index++)
        {
            recent[index] = Frames[(_next - _count + index + Kept) % Kept];
        }

        return recent;
    }

    /// <summary>A number of bytes the way somebody says it.</summary>
    private static string Bytes(long bytes) => bytes switch
    {
        >= 1L << 30 => Say($"{bytes / (double)(1L << 30):0.00} GB"),
        >= 1L << 20 => Say($"{bytes / (double)(1L << 20):0.0} MB"),
        >= 1L << 10 => Say($"{bytes / (double)(1L << 10):0.0} KB"),
        _ => Say($"{bytes} B"),
    };

    /// <summary>A length of time as hours, minutes and seconds.</summary>
    private static string Duration(double seconds)
    {
        var span = TimeSpan.FromSeconds(seconds);

        return span.TotalHours >= 1
            ? Say($"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}")
            : Say($"{span.Minutes}:{span.Seconds:00}");
    }

    /// <summary>A number as it reads the same wherever the editor is run.</summary>
    private static string Say(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
