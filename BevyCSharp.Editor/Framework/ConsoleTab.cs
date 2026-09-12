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

    /// <summary>Draws it.</summary>
    public static void Draw()
    {
        var lines = View.Lines();
        var room = ImGui.GetContentRegionAvail();

        // What the box to type in takes, asked for rather than guessed: a number written here is a
        // number that stops matching the moment the padding changes, and the last line of the log
        // is then cut in half by the edge of the region.
        var typing = ImGui.GetFrameHeightWithSpacing();

        if (ImGui.BeginChild("##log", new Vector2(0f, room.Y - typing)))
        {
            // Wrapped at the edge of the region rather than run off it. A path or a stack trace is
            // longer than any panel, and the half of it past the edge is the half worth reading.
            ImGui.PushTextWrapPos(0f);

            foreach (var line in lines)
            {
                var theme = EditorTheme.Current;

                // Out of the theme, so a look dialled in reaches the log as well. Written here in
                // four colours the palette already has rather than four of this file's own.
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

            // Follows what is written, unless somebody has scrolled up to read something.
            if (_seen != ConsoleLog.Written && ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 4f)
            {
                ImGui.SetScrollHereY(1f);
            }

            ImGui.PopTextWrapPos();

            _seen = ConsoleLog.Written;
        }

        ImGui.EndChild();

        ImGui.SetNextItemWidth(-1f);

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
    }
}
