using System.Globalization;
using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What the editor keeps that is not part of the world, as a page of rows.
/// </summary>
/// <remarks>
/// <para>
/// The other half of <see cref="EditorSettings"/>. That side is the table a game adds a line to;
/// this side draws whatever is in it, so a preference registered anywhere appears here without
/// anybody editing a panel.
/// </para>
/// <para>
/// What each row is drawn as follows from its kind, the way a component's fields follow theirs.
/// Everything a setting holds is text on both sides, so the widget here parses and writes back
/// text and the closure behind it keeps whatever it actually keeps.
/// </para>
/// </remarks>
public static class SettingsTab
{
    /// <summary>Which page is open, by name.</summary>
    private static string _page = string.Empty;

    /// <summary>Draws it.</summary>
    public static void Draw()
    {
        var pages = EditorSettings.Pages;

        if (pages.Count == 0)
        {
            ImGui.TextDisabled("Nothing to set");
            return;
        }

        if (!pages.Contains(_page)) _page = pages[0];

        // The pages as a row of tabs, and nothing at all when there is only one of them, because a
        // picker offering one choice is a row that says nothing.
        if (pages.Count > 1)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextDisabled("PAGE");

            foreach (var page in pages)
            {
                ImGui.SameLine();

                if (EditorWidgets.Pill(page, page == _page)) _page = page;
            }

            EditorTheme.Divide();
        }

        if (!EditorSurface.Region("##settings", new Vector2(0f, 0f)))
        {
            EditorSurface.EndRegion();
            return;
        }

        foreach (var entry in EditorSettings.On(_page)) Row(entry);

        EditorSurface.EndRegion();
    }

    /// <summary>One setting, drawn as what kind of thing it is.</summary>
    /// <param name="entry">The setting to draw.</param>
    private static void Row(EditorSetting entry)
    {
        if (entry.Kind == SettingKind.Heading)
        {
            EditorSurface.Heading(entry.Label);
            return;
        }

        var id = $"##{entry.Page}.{entry.Label}";
        var held = entry.Read?.Invoke() ?? string.Empty;

        // A table of its own per row, the way a component's fields are laid out, so a heading can
        // be drawn between two rows rather than inside the name column of one.
        ImGui.PushID(id);

        if (!EditorRows.Open("##row"))
        {
            ImGui.PopID();
            return;
        }

        EditorRows.Line(entry.Label);

        switch (entry.Kind)
        {
            case SettingKind.Flag:
            {
                var on = held == "1";

                if (EditorWidgets.Ticked(id, ref on)) entry.Write?.Invoke(on ? "1" : "0");

                break;
            }

            case SettingKind.Number:
            {
                var number = float.TryParse(
                    held, NumberStyles.Float, CultureInfo.InvariantCulture, out var read)
                    ? read
                    : 0f;

                // Dragged, typed into and written the way every other number in the editor is,
                // down to the face it is set in, and written back the way the file reads it, which
                // is the same whatever machine it is on.
                ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));

                var moved = ImGui.DragFloat(
                    id, ref number, 0.01f, 0f, 0f, FieldNumbers.Written(id, number),
                    FieldNumbers.Whole);

                ImGui.PopFont();

                FieldNumbers.Close(id);

                if (moved)
                {
                    entry.Write?.Invoke(number.ToString("0.###", CultureInfo.InvariantCulture));
                }

                break;
            }

            case SettingKind.Choice:
            {
                EditorWidgets.Choice(
                    id, held, entry.Options ?? [], chosen => entry.Write?.Invoke(chosen));

                break;
            }

            case SettingKind.Action:
            {
                if (ImGui.Button($"{entry.Label}{id}", EditorRows.Across()))
                {
                    entry.Write?.Invoke(string.Empty);
                }

                break;
            }

            case SettingKind.Fact:
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextDisabled(held);

                break;
            }

            default:
            {
                var typed = held;

                if (ImGui.InputText(id, ref typed, 256)) entry.Write?.Invoke(typed);

                break;
            }
        }

        EditorRows.Close();
        ImGui.PopID();
    }
}
