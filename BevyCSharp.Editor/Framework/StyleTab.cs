using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The editor's own look, changed while it runs.
/// </summary>
/// <remarks>
/// <para>
/// A theme picked, an opacity dragged, and under them ImGui's own style editor, which is every
/// colour, rounding and spacing there is. That is what the visual designers people reach for offer,
/// without any of them: they generate C++ and cannot be part of a C# editor, but the thing they are
/// wanted for is dialling a look in and taking it away, and that is a file.
/// </para>
/// <para>
/// Saved to <c>assets/theme.txt</c>, which the editor reads at startup. A look dialled in by hand
/// survives a restart and can be shipped with the project.
/// </para>
/// </remarks>
public static class StyleTab
{
    private static string _said = string.Empty;
    private static ulong _saidOn;

    /// <summary>Draws it.</summary>
    public static void Draw()
    {
        var theme = EditorTheme.Current;

        ImGui.AlignTextToFramePadding();
        ImGui.TextDisabled("THEME");
        ImGui.SameLine();

        foreach (var offered in EditorTheme.All)
        {
            var chosen = offered.Name == theme.Name;

            if (chosen) ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));

            if (ImGui.Button($" {offered.Name} ")) EditorShell.Wear(offered);

            if (chosen) ImGui.PopStyleColor();

            ImGui.SameLine();
        }

        // How much of the scene shows through a panel, which is a decision about the look and so
        // belongs beside the rest of them. In whole percent, because that is how somebody says it.
        var behind = theme.WindowAlpha * 100f;

        ImGui.SetNextItemWidth(150f);

        if (ImGui.SliderFloat("##behind", ref behind, 20f, 100f, "panel %.0f%%"))
        {
            EditorShell.Wear(theme with { WindowAlpha = behind / 100f });
        }

        ImGui.SameLine();

        var alpha = theme.PanelAlpha * 100f;

        ImGui.SetNextItemWidth(150f);

        if (ImGui.SliderFloat("##alpha", ref alpha, 40f, 100f, "cards %.0f%%"))
        {
            EditorShell.Wear(theme with { PanelAlpha = alpha / 100f });
        }

        ImGui.SameLine();

        if (ImGui.Button("Save")) Save();

        ImGui.SameLine();

        if (ImGui.Button("Reload")) Reload();

        ImGui.SameLine();

        // The way back from a look that went wrong. A theme saved to file is read at every startup,
        // so without this the only way out of a bad one is to go and delete a file by hand, which
        // is not something an editor should ask of anybody.
        if (ImGui.Button("Reset")) Reset();

        if (_said.Length > 0 && EditorShell.Frame - _saidOn < 240)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(_said);
        }

        EditorTheme.Divide();

        // Everything else, in ImGui's own editor: it knows every field it has, and anything written
        // here would be a second list to keep in step with it.
        ImGui.PushStyleColor(ImGuiCol.ChildBg, 0u);

        var open = ImGui.BeginChild("##style", new Vector2(0f, 0f));

        ImGui.PopStyleColor();

        if (open)
        {
            ImGui.ShowStyleEditor();
        }

        ImGui.EndChild();
    }

    /// <summary>Writes what is in force to the file the editor reads at startup.</summary>
    private static void Save()
    {
        var path = EditorPaths.Asset("theme.txt");

        try
        {
            File.WriteAllText(path, Read().Describe());

            // The whole path, because it is written beside the running build rather than into the
            // project, and somebody who wants to keep it has to know where it went.
            Announce($"saved to {path}");
        }
        catch (IOException failure)
        {
            Announce(failure.Message);
        }
    }

    /// <summary>Puts the saved theme back.</summary>
    private static void Reload()
    {
        var path = EditorPaths.Asset("theme.txt");

        if (!File.Exists(path))
        {
            Announce("nothing saved yet");
            return;
        }

        try
        {
            EditorShell.Wear(EditorTheme.Restore(File.ReadAllText(path)));
            Announce("read back");
        }
        catch (IOException failure)
        {
            Announce(failure.Message);
        }
    }

    /// <summary>Puts the built-in look back, and forgets whatever was saved over it.</summary>
    private static void Reset()
    {
        EditorShell.Wear(EditorTheme.Modern);

        var path = EditorPaths.Asset("theme.txt");

        try
        {
            if (File.Exists(path)) File.Delete(path);

            Announce("back to the built-in look");
        }
        catch (IOException failure)
        {
            Announce(failure.Message);
        }
    }

    /// <summary>
    /// What is in force, with whatever ImGui's own editor has been told taken back from it.
    /// </summary>
    /// <remarks>
    /// The style editor writes straight into ImGui, so the theme is what was applied plus whatever
    /// has been dragged since. Reading the handful of values back is what makes saving mean what
    /// somebody sees rather than what they picked.
    /// </remarks>
    private static EditorTheme Read()
    {
        var style = ImGui.GetStyle();
        var theme = EditorTheme.Current;

        // Read back out of the slot each rung was painted into, and no other: reading a rung out of
        // the slot below it saves a ladder nobody dialled in, one step short at every rung, which
        // is what the file that shipped with the last build turned out to be.
        return theme with
        {
            Panel = style.Colors[(int)ImGuiCol.WindowBg],
            Card = style.Colors[(int)ImGuiCol.ChildBg],
            Group = style.Colors[(int)ImGuiCol.MenuBarBg],
            Field = style.Colors[(int)ImGuiCol.FrameBg],
            Hover = style.Colors[(int)ImGuiCol.FrameBgHovered],
            Active = style.Colors[(int)ImGuiCol.FrameBgActive],
            Line = style.Colors[(int)ImGuiCol.Border],
            Text = style.Colors[(int)ImGuiCol.Text],
            Dim = style.Colors[(int)ImGuiCol.SliderGrab],
            Faint = style.Colors[(int)ImGuiCol.TextDisabled],
            Accent = style.Colors[(int)ImGuiCol.CheckMark],

            // How far through a panel the scene shows is the alpha the panel was painted with, so
            // dragging it in ImGui's own editor is picked up here rather than ignored.
            PanelAlpha = style.Colors[(int)ImGuiCol.ChildBg].W,
            WindowAlpha = style.Colors[(int)ImGuiCol.WindowBg].W,
            WindowRounding = style.WindowRounding,
            ChildRounding = style.ChildRounding,
            FrameRounding = style.FrameRounding,
            TabRounding = style.TabRounding,
            WindowPadding = style.WindowPadding,
            FramePadding = style.FramePadding,
            ItemSpacing = style.ItemSpacing,
            Borders = style.WindowBorderSize,
        };
    }

    /// <summary>Says what happened, for a few seconds.</summary>
    private static void Announce(string what)
    {
        _said = what;
        _saidOn = EditorShell.Frame;
    }
}
