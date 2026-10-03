using System.Numerics;
using Bevy;
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
/// The top row is the project and what can be done with it, and under it the game or the build's
/// output is shown, which the console also shows among everything else. Kept here on its own,
/// because the question after pressing Build is whether it built, and the answer is in the last
/// lines.
/// </para>
/// </remarks>
public static class PlayTab
{
    private static int _seen;

    private static int _target;
    private static ShippedAssets _shipped;

    /// <summary>Draws it.</summary>
    public static void Draw()
    {
        Actions();
        Export();

        if (!EditorPlay.Running)
        {
            EditorSurface.Pane("##played", Vector2.Zero, Output);
            return;
        }

        // While the game runs, its world beside what it writes: what it holds, the fields of the
        // one picked, and the output, each a pane, with the clock's buttons over them.
        EditorRemote.Watch();
        Clock();

        var gap = ImGui.GetStyle().ItemSpacing.X;
        var across = ImGui.GetContentRegionAvail().X - (gap * 2f);

        EditorSurface.Pane("##remote", new Vector2(across * 0.25f, 0f), RemoteList);
        ImGui.SameLine();
        EditorSurface.Pane("##fields", new Vector2(across * 0.35f, 0f), RemoteFields);
        ImGui.SameLine();
        EditorSurface.Pane("##played", Vector2.Zero, Output);
    }

    /// <summary>What the running game is doing, with its clock's pause and step.</summary>
    private static void Clock()
    {
        ImGui.AlignTextToFramePadding();

        if (EditorRemote.Session is not { } session)
        {
            ImGui.TextDisabled("Waiting for the game to answer");
            ImGui.Spacing();
            return;
        }

        if (EditorWidgets.Pill(EditorRemote.Paused ? "Resume" : "Pause", EditorRemote.Paused)) EditorRemote.TogglePause();

        ImGui.SameLine();
        if (EditorWidgets.Pill("Step", false)) EditorRemote.Step();

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextDisabled(EditorRemote.Problem is { } problem
            ? problem
            : $"{session.Name}, {EditorRemote.Entities.Count} named entities. A change here lasts until it stops.");

        ImGui.Spacing();
    }

    /// <summary>The running game's named entities, one to pick.</summary>
    private static void RemoteList()
    {
        var entities = EditorRemote.Entities;

        if (entities.Count == 0)
        {
            EditorSurface.Empty("Nothing named yet", "The game's named entities appear here as it answers.");
            return;
        }

        foreach (var entity in entities)
        {
            if (ImGui.Selectable($"{entity.Name}##{entity.Id}", entity.Id == EditorRemote.Picked)) EditorRemote.Pick(entity.Id);
            if (ImGui.IsItemHovered()) EditorWidgets.Tip($"{entity.Id}, carrying {entity.Components}");
        }
    }

    /// <summary>The picked entity's fields, each a box that sets it in the game when Enter is pressed.</summary>
    private static void RemoteFields()
    {
        if (EditorRemote.Picked is null)
        {
            EditorSurface.Empty("Nothing picked", "Pick one of the game's entities to see what it holds.");
            return;
        }

        foreach (var field in EditorRemote.Fields)
        {
            ImGui.PushID(field.Field);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(field.Field);
            ImGui.SameLine(MathF.Max(ImGui.GetContentRegionAvail().X * 0.45f, ImGui.CalcTextSize(field.Field).X + 12f));
            ImGui.SetNextItemWidth(-1f);

            // What is being typed is kept while the box has the keyboard, since the game's answer
            // arrives a few times a second and would otherwise write over it.
            var text = Typed.TryGetValue(field.Field, out var typing) ? typing : field.Value;
            if (ImGui.InputText("##value", ref text, 256, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                EditorRemote.Set(field.Field, text);
                Typed.Remove(field.Field);
            }
            else if (ImGui.IsItemActive())
            {
                Typed[field.Field] = text;
            }
            else
            {
                Typed.Remove(field.Field);
            }

            ImGui.PopID();
        }
    }

    /// <summary>What is being typed into a field of the game's, by the field.</summary>
    private static readonly Dictionary<string, string> Typed = new(StringComparer.Ordinal);

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

        string[] labels = [EditorPlay.Running ? "Stop" : "Play", "Play scene", "Build", "Clear"];
        var buttons = labels.Sum(EditorWidgets.PillWidth) + (ImGui.GetStyle().ItemSpacing.X * labels.Length);

        // The project the buttons act on, as a field filling what the buttons leave, with the sample
        // named in its hint, so an empty field still says what will run.
        var project = EditorPlay.Project;
        var hint = EditorPlay.Resolved is { } found ? Path.GetFileName(found) : "Path to a .csproj";

        ImGui.SetNextItemWidth(MathF.Max(120f, ImGui.GetContentRegionAvail().X - buttons));

        if (ImGui.InputTextWithHint("##project", hint, ref project, 512)) EditorPlay.Project = project.Trim();

        ImGui.SameLine();
        if (EditorWidgets.Pill(labels[0], EditorPlay.Running)) EditorPlay.Toggle();

        // The scene being edited, through the player, beside the project's own Main.
        ImGui.SameLine();
        if (EditorWidgets.Pill(labels[1], false) && EditorPlay.PlayScene() is { } refused) Console.WriteLine($"[play] {refused}");

        ImGui.SameLine();
        if (EditorWidgets.Pill(labels[2], EditorPlay.Busy && EditorPlay.Job == PlayJob.Building)) EditorPlay.Build();

        ImGui.SameLine();
        if (EditorWidgets.Pill(labels[3], false)) EditorPlay.Clear();

        ImGui.Spacing();
    }

    /// <summary>
    /// The second row: which platform to export for, how its assets ship, and Export.
    /// </summary>
    /// <remarks>
    /// Under the row that plays and builds rather than in it, since an export is asked for far less
    /// often and takes its own two choices, and the row above is already as wide as the tab.
    /// </remarks>
    /// <summary>How a way of shipping the assets reads in the export row.</summary>
    private static string ShippedLabel(ShippedAssets way) => way switch
    {
        ShippedAssets.Assembly => "In the assembly",
        ShippedAssets.Pack => "In a pack",
        _ => "As files",
    };

    /// <summary>What a way of shipping the assets gives a player, for its tooltip.</summary>
    private static string ShippedTip(ShippedAssets way) => way switch
    {
        ShippedAssets.Assembly =>
            "Compiles the assets into the game's own assembly, so a player gets no asset folder beyond "
            + "scripts and shaders. Held in memory once the game starts, so for a game of modest size.",
        ShippedAssets.Pack =>
            $"Writes the assets into {AssetPack.DefaultName} beside the game, so a player gets no asset "
            + "folder beyond scripts and shaders. Read a part at a time, so for a game of any size.",
        _ => "Ships the asset folder beside the game, which a player can open and change.",
    };

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
        ImGui.SetNextItemWidth(EditorWidgets.PillWidth("In the assembly") + ImGui.GetFrameHeight());
        if (ImGui.BeginCombo("##assets", ShippedLabel(_shipped)))
        {
            foreach (var way in Enum.GetValues<ShippedAssets>())
            {
                if (ImGui.Selectable(ShippedLabel(way), way == _shipped)) _shipped = way;
                if (ImGui.IsItemHovered()) EditorWidgets.Tip(ShippedTip(way));
            }

            ImGui.EndCombo();
        }

        if (ImGui.IsItemHovered()) EditorWidgets.Tip(ShippedTip(_shipped));

        ImGui.SameLine();
        if (EditorWidgets.Pill("Export", EditorPlay.Busy && EditorPlay.Job == PlayJob.Exporting)
            && EditorPlay.Export(targets[_target], _shipped) is { } refused)
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
