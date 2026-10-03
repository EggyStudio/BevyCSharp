using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The console, as a tab along the bottom.
/// </summary>
/// <remarks>
/// What the program wrote and what somebody types back. The lines are whatever went to the output
/// stream, because <see cref="ConsoleLog"/> tees it, so a <c>Console.WriteLine</c> anywhere appears
/// here without anybody arranging it. What a console does apart from being drawn is
/// <see cref="ConsoleView"/>'s.
/// </remarks>
public static class ConsoleTab
{
    private static readonly ConsoleView View = new();

    private static string _typed = string.Empty;
    private static int _seen;

    /// <summary>How many times an earlier command has been put back in the box.</summary>
    private static int _recalls;

    /// <summary>
    /// How tall each line is once wrapped, by the line and how many times it was said, since the
    /// count is part of what is drawn. Kept for one width, and measured again when it changes.
    /// </summary>
    private static readonly Dictionary<(int Index, int Count), float> Heights = [];

    /// <summary>The width the heights were measured at.</summary>
    private static float _measuredAt;

    /// <summary>
    /// Draws the lines in view, and leaves the room the others would take above and below them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A log keeps two thousand lines, each wrapped at the edge of the region, and drawing all of
    /// them every frame is laying out text nobody sees. ImGui's list clipper does this for rows of
    /// one height, and a wrapped line is as tall as its wrapping makes it, so each line's height is
    /// measured once for the region's width and kept, and the lines between the top and the bottom
    /// of the view are drawn, with blank room for the rest so the scroll bar measures the whole log.
    /// </para>
    /// <para>
    /// A line advances the cursor by its height and the item spacing below it, and so does the room
    /// left for a run of lines, so a line scrolled into view lands exactly where it would have been
    /// drawn among all the others.
    /// </para>
    /// </remarks>
    private static void Visible(LogLine[] lines)
    {
        var width = ImGui.GetContentRegionAvail().X;
        if (width != _measuredAt || Heights.Count > ConsoleLog.Depth * 2)
        {
            Heights.Clear();
            _measuredAt = width;
        }

        var spacing = ImGui.GetStyle().ItemSpacing.Y;
        var top = ImGui.GetScrollY();
        var bottom = top + ImGui.GetWindowHeight();
        var y = 0f;
        var skipped = 0f;
        var index = 0;

        // Above the view.
        for (; index < lines.Length; index++)
        {
            var step = HeightOf(lines[index], width) + spacing;
            if (y + step > top) break;

            y += step;
        }

        Room(y, spacing);

        // In view.
        for (; index < lines.Length && y < bottom; index++)
        {
            Line(lines[index]);
            y += HeightOf(lines[index], width) + spacing;
        }

        // Below it, measured all the same, since the room has to be the whole of their height.
        for (; index < lines.Length; index++) skipped += HeightOf(lines[index], width) + spacing;

        Room(skipped, spacing);
    }

    /// <summary>Leaves room a run of lines would take, which advances by the spacing after it too.</summary>
    private static void Room(float height, float spacing)
    {
        if (height > 0f) ImGui.Dummy(new Vector2(0f, MathF.Max(0f, height - spacing)));
    }

    /// <summary>A line's height once wrapped at a width, measured the first time it is asked.</summary>
    private static float HeightOf(LogLine line, float width)
    {
        if (!Heights.TryGetValue((line.Index, line.Count), out var height))
        {
            height = ImGui.CalcTextSize(ConsoleView.Written(line), false, width).Y;
            Heights[(line.Index, line.Count)] = height;
        }

        return height;
    }

    /// <summary>Draws one line in its level's color.</summary>
    private static void Line(LogLine line)
    {
        var theme = EditorTheme.Current;

        // Out of the theme, so a look dialed in reaches the log as well. Written here in four
        // colors the palette already has rather than four of this file's own.
        var color = line.Level switch
        {
            LogLevel.Warning => theme.Warn,
            LogLevel.Error => theme.Bad,
            LogLevel.Echo => EditorTheme.LiveText,
            _ => theme.Dim,
        };

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(ConsoleView.Written(line));
        ImGui.PopStyleColor();
    }

    /// <summary>Draws it.</summary>
    public static void Draw()
    {
        Filters();

        var lines = View.Lines();
        var room = ImGui.GetContentRegionAvail();

        // What the box to type in takes, asked for rather than guessed, because a number written
        // here is a number that stops matching the moment the padding changes, and the last line
        // of the log is then cut in half by the edge of the region.
        var typing = ImGui.GetFrameHeightWithSpacing();

        if (EditorSurface.Region("##log", new Vector2(0f, room.Y - typing)))
        {
            // Wrapped at the edge of the region rather than run off it. A path or a stack trace is
            // longer than any panel, and the half of it past the edge is the half worth reading.
            ImGui.PushTextWrapPos(0f);

            Visible(lines);

            // Follows what is written, unless somebody has scrolled up to read something.
            if (_seen != ConsoleLog.Written && ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 4f)
            {
                ImGui.SetScrollHereY(1f);
            }

            ImGui.PopTextWrapPos();

            _seen = ConsoleLog.Written;
        }

        EditorSurface.EndRegion();

        ImGui.SetNextItemWidth(-1f);

        // Under an id that changes whenever the text is put there rather than typed. ImGui reads
        // the string it was given when a box becomes active and not while it is, so a box that
        // keeps its name keeps showing what was in it when it was last opened.
        ImGui.PushID(_recalls);

        if (ImGui.InputTextWithHint(
                "##entry",
                View.Hint(_typed) is { Length: > 0 } hint ? hint : "Type a command",
                ref _typed,
                512,
                ImGuiInputTextFlags.EnterReturnsTrue))
        {
            View.Run(_typed);
            _typed = string.Empty;

            // Back where it was, so a run of commands is a run of commands rather than a click
            // between each one.
            ImGui.SetKeyboardFocusHere(-1);
        }

        Recall();

        ImGui.PopID();
    }

    /// <summary>
    /// What is shown, as a box to search in and a button per level.
    /// </summary>
    /// <remarks>
    /// A console keeps every line a program wrote, which is more lines than anybody reads. What
    /// makes it a tool rather than a wall is being able to say "only the errors" or "only the ones
    /// with that word in them", and both of those are <see cref="ConsoleView"/>'s already.
    /// </remarks>
    private static void Filters()
    {
        var levels = new (string Name, Func<bool> Read, Action<bool> Write)[]
        {
            ("info", () => View.ShowInfo, on => View.ShowInfo = on),
            ("warnings", () => View.ShowWarnings, on => View.ShowWarnings = on),
            ("errors", () => View.ShowErrors, on => View.ShowErrors = on),
        };

        var buttons = 0f;

        foreach (var (name, _, _) in levels)
        {
            buttons += EditorWidgets.PillWidth(name) + ImGui.GetStyle().ItemSpacing.X;
        }

        var search = View.Search;

        ImGui.SetNextItemWidth(MathF.Max(80f, ImGui.GetContentRegionAvail().X - buttons));

        if (ImGui.InputTextWithHint("##find", "Search the log", ref search, 128))
        {
            View.Search = search;
        }

        foreach (var (name, read, write) in levels)
        {
            ImGui.SameLine();

            var on = read();

            // The accent for what is in force, as everywhere else, and the plate for what is not.
            if (EditorWidgets.Pill(name, on)) write(!on);
        }

        ImGui.Spacing();
    }

    /// <summary>
    /// Puts an earlier command back in the box, the way a shell does.
    /// </summary>
    /// <remarks>
    /// Read here rather than through ImGui's own history callback, which needs an unmanaged
    /// function to rewrite the buffer it is editing. Asking for the focus again makes the box take
    /// up the new text, because ImGui reads the string it was given when a box becomes active and
    /// not while it is.
    /// </remarks>
    private static void Recall()
    {
        if (!ImGui.IsItemActive()) return;

        var was = _typed;

        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow)) _typed = View.Back(_typed);
        else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow)) _typed = View.Forward(_typed);
        else return;

        if (_typed == was) return;

        _recalls++;
        ImGui.SetKeyboardFocusHere(-1);
    }
}
