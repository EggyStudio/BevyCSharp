using System.Numerics;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Playing and building the project, as a tab along the bottom.
/// </summary>
/// <remarks>
/// <para>
/// Here rather than as a button on the scene's toolbar, where most engines put it, so the viewport
/// holds the scene and what acts on the scene. Starting the game changes nothing in the view, since
/// the game opens a window of its own, so a button over the view would be the one control there
/// about something else.
/// </para>
/// <para>
/// The top row is the project and what can be done with it, and under it is what the game or the
/// build wrote, which the console also shows among everything else. Kept here on its own, because
/// the question after pressing Build is whether it built, and the answer is in the last lines.
/// </para>
/// </remarks>
public static class PlayTab
{
    private static int _seen;

    /// <summary>Draws it.</summary>
    public static void Draw()
    {
        Actions();

        EditorSurface.Pane("##played", Vector2.Zero, Output);
    }

    /// <summary>The state, the project, and play, build, stop and clear at the end of the row.</summary>
    private static void Actions()
    {
        var theme = EditorTheme.Current;
        var line = ImGui.GetTextLineHeight();

        ImGui.AlignTextToFramePadding();

        // A dot in the accent while something runs, as the shaders tab marks a compiler found.
        var at = ImGui.GetCursorScreenPos();
        var busy = EditorPlay.Busy;

        ImGui.GetWindowDrawList().AddCircleFilled(
            at + new Vector2(5f, ImGui.GetStyle().FramePadding.Y + (line * 0.5f)),
            4f,
            ImGui.GetColorU32(busy ? EditorTheme.LiveAccent : theme.Dim));

        ImGui.Dummy(new Vector2(12f, line));
        ImGui.SameLine();

        var state = (busy, EditorPlay.Job) switch
        {
            (true, PlayJob.Playing) => "Playing",
            (true, PlayJob.Building) => "Building",
            _ => "Stopped",
        };

        ImGui.TextDisabled(state);
        ImGui.SameLine();

        string[] labels = [EditorPlay.Running ? "Stop" : "Play", "Build", "Clear"];
        var buttons = labels.Sum(EditorWidgets.PillWidth) + (ImGui.GetStyle().ItemSpacing.X * labels.Length);

        // The project the buttons act on, as a field filling what the buttons leave, with the sample
        // named in its hint, so an empty field still says what will run.
        var project = EditorPlay.Project;
        var hint = EditorPlay.Resolved is { } found ? Path.GetFileName(found) : "Path to a .csproj";

        ImGui.SetNextItemWidth(MathF.Max(120f, ImGui.GetContentRegionAvail().X - buttons));

        if (ImGui.InputTextWithHint("##project", hint, ref project, 512)) EditorPlay.Project = project.Trim();

        ImGui.SameLine();
        if (EditorWidgets.Pill(labels[0], EditorPlay.Running)) EditorPlay.Toggle();

        ImGui.SameLine();
        if (EditorWidgets.Pill(labels[1], EditorPlay.Busy && EditorPlay.Job == PlayJob.Building)) EditorPlay.Build();

        ImGui.SameLine();
        if (EditorWidgets.Pill(labels[2], false)) EditorPlay.Clear();

        ImGui.Spacing();
    }

    /// <summary>What the game or the last build wrote, following the end unless scrolled up.</summary>
    private static void Output()
    {
        var lines = EditorPlay.Lines();

        if (lines.Length == 0)
        {
            EditorSurface.Empty(
                "Nothing has run yet",
                "Play opens the game in a window of its own, and Build says whether it compiles.");
            return;
        }

        var theme = EditorTheme.Current;

        ImGui.PushTextWrapPos(0f);

        foreach (var text in lines)
        {
            // Marked the way the console marks them, so a build's errors stand out of its noise.
            var color = text.Contains(": error ", StringComparison.Ordinal) ? theme.Bad
                : text.Contains(": warning ", StringComparison.Ordinal) ? theme.Warn
                : text.StartsWith("[play]", StringComparison.Ordinal) ? EditorTheme.LiveText
                : theme.Dim;

            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.TextUnformatted(text);
            ImGui.PopStyleColor();
        }

        if (_seen != EditorPlay.Written && ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 4f)
        {
            ImGui.SetScrollHereY(1f);
        }

        ImGui.PopTextWrapPos();

        _seen = EditorPlay.Written;
    }
}
