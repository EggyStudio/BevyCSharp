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

        if (ImGui.BeginChild("##log", new Vector2(0f, room.Y - 30f)))
        {
            foreach (var line in lines)
            {
                var color = line.Level switch
                {
                    LogLevel.Warning => new Vector4(0.95f, 0.72f, 0.31f, 1f),
                    LogLevel.Error => new Vector4(0.95f, 0.43f, 0.40f, 1f),
                    LogLevel.Echo => new Vector4(0.90f, 0.92f, 0.95f, 1f),
                    _ => new Vector4(0.66f, 0.70f, 0.76f, 1f),
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
