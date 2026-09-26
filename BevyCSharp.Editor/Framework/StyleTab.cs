using System.Globalization;
using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The editor's own look, changed while it runs.
/// </summary>
/// <remarks>
/// <para>
/// A theme picked, and under it every rung of the ladder it is made of. The visual designers people
/// reach for offer the same, and this needs none of them. They generate C++ and cannot be part of a
/// C# editor, but the thing they are wanted for is dialling a look in and taking it away, and that
/// is a file.
/// </para>
/// <para>
/// The rows are the theme's own fields rather than ImGui's style, so what is dragged here is the
/// same thing that is saved. ImGui's own editor offers every slot it has, most of which no theme
/// records, and draws them with widgets that are rounded the way ImGui rounds things rather than
/// the way this editor does.
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
            // The accent, because the editor uses it everywhere else to say "this is the one in
            // force".
            if (EditorWidgets.Pill(offered.Name, offered.Name == theme.Name))
            {
                EditorShell.Wear(offered);
            }

            ImGui.SameLine();
        }

        // How much of the scene shows through a panel, which is a decision about the look and so
        // belongs beside the rest of them. In whole percent, because that is how somebody says it.
        var behind = theme.WindowAlpha * 100f;

        ImGui.SetNextItemWidth(150f);

        var moved = EditorWidgets.Sliding(
            "##behind",
            behind / 100f,
            () => ImGui.SliderFloat("##behind", ref behind, 0f, 100f, "panel %.0f%%"),
            Percent("panel", behind));

        if (moved) EditorShell.Wear(theme with { WindowAlpha = behind / 100f });

        ImGui.SameLine();

        var alpha = theme.PanelAlpha * 100f;

        ImGui.SetNextItemWidth(150f);

        var faded = EditorWidgets.Sliding(
            "##alpha",
            alpha / 100f,
            () => ImGui.SliderFloat("##alpha", ref alpha, 0f, 100f, "cards %.0f%%"),
            Percent("cards", alpha));

        if (faded) EditorShell.Wear(theme with { PanelAlpha = alpha / 100f });

        ImGui.SameLine();

        if (EditorWidgets.Pill("Save", false)) Save();

        ImGui.SameLine();

        if (EditorWidgets.Pill("Reload", false)) Reload();

        ImGui.SameLine();

        // The way back from a look that went wrong. A theme saved to file is read at every startup,
        // so without this the only way out of a bad one is to go and delete a file by hand, which
        // is not something an editor should ask of anybody.
        if (EditorWidgets.Pill("Reset", false)) Reset();

        if (_said.Length > 0 && EditorShell.Frame - _saidOn < 240)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(_said);
        }

        EditorTheme.Divide();

        if (EditorSurface.Region("##style", new Vector2(0f, 0f))) Rungs();

        EditorSurface.EndRegion();
    }

    /// <summary>Every color and number a theme is made of, as rows to change.</summary>
    private static void Rungs()
    {
        var theme = EditorTheme.Current;

        EditorSurface.Heading("Surfaces");
        Color("Ground", theme.Ground, c => theme with { Ground = c });
        Color("Panel", theme.Panel, c => theme with { Panel = c });
        Color("Card", theme.Card, c => theme with { Card = c });
        Color("Group", theme.Group, c => theme with { Group = c });
        Color("Field", theme.Field, c => theme with { Field = c });
        Color("Hover", theme.Hover, c => theme with { Hover = c });
        Color("Active", theme.Active, c => theme with { Active = c });
        Color("Line", theme.Line, c => theme with { Line = c });

        EditorSurface.Heading("Ink");
        Color("Text", theme.Text, c => theme with { Text = c });
        Color("Dim", theme.Dim, c => theme with { Dim = c });
        Color("Faint", theme.Faint, c => theme with { Faint = c });
        Color("Accent", theme.Accent, c => theme with { Accent = c });
        Color("Warn", theme.Warn, c => theme with { Warn = c });
        Color("Bad", theme.Bad, c => theme with { Bad = c });

        EditorSurface.Heading("Seen through");
        Share("Panel", theme.WindowAlpha, a => theme with { WindowAlpha = a });
        Share("Cards", theme.PanelAlpha, a => theme with { PanelAlpha = a });

        EditorSurface.Heading("Shape");
        Number("Window rounding", theme.WindowRounding, n => theme with { WindowRounding = n });
        Number("Card rounding", theme.ChildRounding, n => theme with { ChildRounding = n });
        Number("Field rounding", theme.FrameRounding, n => theme with { FrameRounding = n });
        Number("Tab rounding", theme.TabRounding, n => theme with { TabRounding = n });
        Number("Borders", theme.Borders, n => theme with { Borders = n });

        EditorSurface.Heading("Air");
        Pair("Window padding", theme.WindowPadding, v => theme with { WindowPadding = v });
        Pair("Field padding", theme.FramePadding, v => theme with { FramePadding = v });
        Pair("Item spacing", theme.ItemSpacing, v => theme with { ItemSpacing = v });
    }

    /// <summary>
    /// One rung, with its name beside whatever changes it.
    /// </summary>
    /// <remarks>
    /// A table of its own per row, the way a component's fields are laid out, so a heading can be
    /// drawn between two rows rather than inside the name column of one.
    /// </remarks>
    /// <param name="name">What the rung is called.</param>
    /// <param name="value">What changes it, drawn in the column beside the name.</param>
    private static void Rung(string name, Action value)
    {
        ImGui.PushID(name);

        if (EditorRows.Open("##row"))
        {
            EditorRows.Line(name);
            value();

            EditorRows.Close();
        }

        ImGui.PopID();
    }

    /// <summary>
    /// One color of the ladder, as a swatch the width of its row with a picker behind it.
    /// </summary>
    /// <remarks>
    /// A button rather than ImGui's own color field, which draws its swatch as a square of one
    /// row's height however wide the row is and leaves the rest of it empty.
    /// </remarks>
    private static void Color(string name, Vector4 held, Func<Vector4, EditorTheme> onto) =>
        Rung(name, () =>
        {
            var value = held;

            if (EditorWidgets.Swatch($"##{name}", ref value)) EditorShell.Wear(onto(value));
        });

    /// <summary>How far through something is seen, in whole percent, which is how somebody says it.</summary>
    private static void Share(string name, float held, Func<float, EditorTheme> onto) =>
        Rung(name, () =>
        {
            var value = held * 100f;

            var moved = EditorWidgets.Sliding(
                $"##{name}",
                value / 100f,
                () => ImGui.SliderFloat($"##{name}", ref value, 0f, 100f, "%.0f%%"),
                Percent(string.Empty, value));

            if (moved) EditorShell.Wear(onto(value / 100f));
        });

    /// <summary>One number, dragged rather than slid, because it has no end to run to.</summary>
    private static void Number(string name, float held, Func<float, EditorTheme> onto) =>
        Rung(name, () =>
        {
            var value = held;

            if (ImGui.DragFloat($"##{name}", ref value, 0.25f, 0f, 64f, "%.0f"))
            {
                EditorShell.Wear(onto(value));
            }
        });

    /// <summary>Two numbers, like every measure of air in here.</summary>
    private static void Pair(string name, Vector2 held, Func<Vector2, EditorTheme> onto) =>
        Rung(name, () =>
        {
            var value = held;

            if (ImGui.DragFloat2($"##{name}", ref value, 0.25f, 0f, 40f, "%.0f"))
            {
                EditorShell.Wear(onto(value));
            }
        });

    /// <summary>A share written the way the bars here write one, in whole percent.</summary>
    /// <param name="what">What the number is about, or nothing for the number alone.</param>
    /// <param name="value">The share, from nothing to a hundred.</param>
    private static string Percent(string what, float value)
    {
        var number = MathF.Round(value).ToString("0", CultureInfo.InvariantCulture);

        return what.Length == 0 ? $"{number}%" : $"{what} {number}%";
    }

    /// <summary>Writes what is in force to the file the editor reads at startup.</summary>
    private static void Save()
    {
        var path = EditorPaths.Asset("theme.txt");

        try
        {
            File.WriteAllText(path, EditorTheme.Current.Describe());

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

    /// <summary>Says what happened, for a few seconds.</summary>
    private static void Announce(string what)
    {
        _said = what;
        _saidOn = EditorShell.Frame;
    }
}
