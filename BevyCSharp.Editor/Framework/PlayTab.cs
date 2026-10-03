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

    private static int _target;
    private static bool _embed;

    /// <summary>Draws it.</summary>
    public static void Draw()
    {
        Actions();
        Export();

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
            (true, PlayJob.Exporting) => "Exporting",
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

    /// <summary>
    /// The second row: which platform to export for, whether to embed the assets, and Export.
    /// </summary>
    /// <remarks>
    /// Under the row that plays and builds rather than in it, since an export is asked for far less
    /// often and takes its own two choices, and the row above is already as wide as the tab.
    /// </remarks>
    private static void Export()
    {
        var targets = EditorPlay.Targets;
        _target = Math.Clamp(_target, 0, targets.Count - 1);

        ImGui.AlignTextToFramePadding();
        ImGui.TextDisabled("Export for");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(EditorWidgets.PillWidth("linux-arm64") + ImGui.GetFrameHeight());
        if (ImGui.BeginCombo("##target", targets[_target]))
        {
            for (var index = 0; index < targets.Count; index++)
            {
                if (ImGui.Selectable(targets[index], index == _target)) _target = index;
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.Checkbox("Embed assets", ref _embed);

        if (ImGui.IsItemHovered())
        {
            EditorWidgets.Tip(
                "Compiles the assets into a bridge of the game's own, so the folder a player gets holds "
                + "only the files the game reads itself. Costs a native build.");
        }

        ImGui.SameLine();
        if (EditorWidgets.Pill("Export", EditorPlay.Busy && EditorPlay.Job == PlayJob.Exporting)
            && EditorPlay.Export(targets[_target], _embed) is { } refused)
        {
            Console.WriteLine($"[play] {refused}");
        }

        if (EditorPlay.ExportFolder(targets[_target]) is { } folder)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(EditorText.Fit(folder, MathF.Max(1f, ImGui.GetContentRegionAvail().X)));
            if (ImGui.IsItemHovered()) EditorWidgets.Tip(folder);
        }

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
                "Play opens the game in a window of its own, Build says whether it compiles, and Export makes a folder to give a player.");
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
