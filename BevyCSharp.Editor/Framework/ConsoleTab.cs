using System.Numerics;
using Bevy;
using Diagnostics;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The console, as a tab along the bottom.
/// </summary>
/// <remarks>
/// What the program wrote and what somebody types back. The lines are whatever went to the output
/// stream, because <see cref="ConsoleLog"/> tees it into the process's console, the Diagnostics
/// module's <see cref="ConsoleModel"/>, so a <c>Console.WriteLine</c> anywhere appears here without
/// anybody arranging it. The line being entered, its history and its completion are the model's,
/// and this tab draws them in the editor's look, with a search and a switch per level of its own.
/// </remarks>
public static class ConsoleTab
{
    private static long _seen;

    /// <summary>
    /// How many times the box was given text it did not type, which its id moves with.
    /// </summary>
    private static int _recalls;

    /// <summary>What is looked for in the lines, or nothing for every line.</summary>
    private static string _search = string.Empty;

    private static bool _showInfo = true;
    private static bool _showWarnings = true;
    private static bool _showErrors = true;

    /// <summary>
    /// How tall each line is once wrapped, by the line and how many times it was said, since the
    /// count is part of what is drawn. Kept for one width, and measured again when it changes.
    /// </summary>
    private static readonly Dictionary<(long Index, int Count), float> Heights = [];

    /// <summary>The width the heights were measured at.</summary>
    private static float _measuredAt;

    /// <summary>How much of a line is drawn.</summary>
    private const int Longest = 300;

    /// <summary>
    /// How a line reads on screen, with its repeat count when it has one.
    /// </summary>
    /// <remarks>
    /// Cut if it is very long, since a path or a stack trace written in one line would otherwise be
    /// measured and wrapped at its whole length every time the width changes.
    /// </remarks>
    internal static string Written(ConsoleLine line)
    {
        var text = line.Count > 1 ? $"{line.Text}  ({line.Count})" : line.Text;

        return text.Length <= Longest ? text : string.Concat(text.AsSpan(0, Longest), "...");
    }

    /// <summary>
    /// The lines worth showing, oldest first, by the levels shown and what is looked for.
    /// </summary>
    internal static ConsoleLine[] Lines()
    {
        // Trimmed, because a filter that finds nothing on account of a space somebody typed after
        // the word is a filter that looks broken.
        var wanted = _search.Trim();

        return
        [
            .. ConsoleLog.All().Where(line => Shows(line.Level)
                && (wanted.Length == 0 || line.Text.Contains(wanted, StringComparison.OrdinalIgnoreCase))),
        ];
    }

    /// <summary>Whether a level is one of the ones being shown, the lines entered always.</summary>
    private static bool Shows(ConsoleLevel level) => level switch
    {
        ConsoleLevel.Warning => _showWarnings,
        ConsoleLevel.Error => _showErrors,
        ConsoleLevel.Input => true,
        _ => _showInfo,
    };

    /// <summary>
    /// Draws the lines in view, and leaves the room the others would take above and below them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A log keeps two thousand lines, each wrapped at the edge of the region, and drawing all of
    /// them every frame is laying out text nobody sees. ImGui's list clipper does this for rows of
    /// one height, and a wrapped line is as tall as its wrapping makes it, so each line's height is
    /// measured once for the region's width and kept, and the lines between the top and the bottom
    /// of the view are drawn, with blank room for the rest so the scroll bar measures the whole
    /// log.
    /// </para>
    /// <para>
    /// A line advances the cursor by its height and the item spacing below it, and so does the room
    /// left for a run of lines, so a line scrolled into view lands exactly where it would have been
    /// drawn among all the others.
    /// </para>
    /// </remarks>
    private static void Visible(ConsoleLine[] lines)
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

    /// <summary>
    /// Leaves room a run of lines would take, which advances by the spacing after it too.
    /// </summary>
    private static void Room(float height, float spacing)
    {
        if (height > 0f) ImGui.Dummy(new Vector2(0f, MathF.Max(0f, height - spacing)));
    }

    /// <summary>
    /// A line's height once wrapped at a width, measured the first time it is asked.
    /// </summary>
    private static float HeightOf(ConsoleLine line, float width)
    {
        if (!Heights.TryGetValue((line.Index, line.Count), out var height))
        {
            height = ImGui.CalcTextSize(Written(line), false, width).Y;
            Heights[(line.Index, line.Count)] = height;
        }

        return height;
    }

    /// <summary>Draws one line in its level's color.</summary>
    private static void Line(ConsoleLine line)
    {
        var theme = EditorTheme.Current;

        // Out of the theme, so a look dialed in reaches the log as well. Written here in four
        // colors the palette already has rather than four of this file's own.
        var color = line.Level switch
        {
            ConsoleLevel.Warning => theme.Warn,
            ConsoleLevel.Error => theme.Bad,
            ConsoleLevel.Input => EditorTheme.LiveText,
            _ => theme.Dim,
        };

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(Written(line));
        ImGui.PopStyleColor();
    }

    /// <summary>Draws it.</summary>
    public static void Draw()
    {
        Filters();

        var lines = Lines();
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

        Entry();
    }

    /// <summary>
    /// The box the line is entered in, the model's line, which runs it on Enter with the world
    /// lent, walks the history or the candidates with the arrows and takes a candidate on Tab.
    /// </summary>
    /// <remarks>
    /// The box sits under an id that changes whenever the model's line is put there rather than
    /// typed, since ImGui reads the string it was given when a box becomes active and not while it
    /// is, so a box that keeps its id keeps showing what was in it when it was last opened. The
    /// arrows and Tab are read here rather than through ImGui's own callbacks, which need an
    /// unmanaged function to rewrite the buffer being edited.
    /// </remarks>
    private static void Entry()
    {
        var model = ConsoleLog.Model;
        var typed = model.Typed;

        ImGui.SetNextItemWidth(-1f);
        ImGui.PushID(_recalls);

        var hint = model.Hint is { Length: > 0 } said ? said : "Type a command";
        if (ImGui.InputTextWithHint("##entry", hint, ref typed, 512, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            if (typed != model.Typed) model.Edit(typed);

            var world = EditorShell.Context?.World;
            if (world is not null)
            {
                using (ConsoleHost.Lend(world)) model.Enter();
            }
            else
            {
                model.Enter();
            }

            _recalls++;

            // Back where it was, so a run of commands is a run of commands rather than a click
            // between each one.
            ImGui.SetKeyboardFocusHere(-1);
        }
        else if (typed != model.Typed)
        {
            model.Edit(typed);
        }

        Steer(model);

        ImGui.PopID();
    }

    /// <summary>
    /// Walks the history or the candidates with the arrows, and takes a candidate on Tab.
    /// </summary>
    private static void Steer(ConsoleModel model)
    {
        if (!ImGui.IsItemActive()) return;

        var was = model.Typed;

        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow)) model.Navigate(-1);
        else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow)) model.Navigate(1);
        else if (ImGui.IsKeyPressed(ImGuiKey.Tab)) model.Accept();
        else return;

        if (model.Typed == was) return;

        _recalls++;
        ImGui.SetKeyboardFocusHere(-1);
    }

    /// <summary>
    /// What is shown, as a box to search in and a button per level.
    /// </summary>
    /// <remarks>
    /// A console keeps every line a program wrote, which is more lines than anybody reads. What
    /// makes it a tool rather than a wall is being able to say "only the errors" or "only the ones
    /// with that word in them".
    /// </remarks>
    private static void Filters()
    {
        var levels = new (string Name, Func<bool> Read, Action<bool> Write)[]
        {
            ("info", () => _showInfo, on => _showInfo = on),
            ("warnings", () => _showWarnings, on => _showWarnings = on),
            ("errors", () => _showErrors, on => _showErrors = on),
        };

        var buttons = 0f;

        foreach (var (name, _, _) in levels)
        {
            buttons += EditorWidgets.PillWidth(name) + ImGui.GetStyle().ItemSpacing.X;
        }

        ImGui.SetNextItemWidth(MathF.Max(80f, ImGui.GetContentRegionAvail().X - buttons));
        ImGui.InputTextWithHint("##find", "Search the log", ref _search, 128);

        foreach (var (name, read, write) in levels)
        {
            ImGui.SameLine();

            var on = read();

            // The accent for what is in force, as everywhere else, and the plate for what is not.
            if (EditorWidgets.Pill(name, on)) write(!on);
        }

        ImGui.Spacing();
    }
}
