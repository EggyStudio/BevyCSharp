using System.Numerics;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// A list of named values, as a name in one column and what it holds in the other.
/// </summary>
/// <remarks>
/// <para>
/// Not <see cref="RoundedRows"/>, which is about what a row of a menu or a list looks like under
/// the pointer. This is about where a name and its value sit.
/// </para>
/// <para>
/// The shape every page of settings takes, whether what it lists is a theme's colors or an
/// editor's preferences. Written once so that two such pages are the same two columns at the same
/// widths rather than two tables that happen to agree today.
/// </para>
/// <para>
/// Weighted toward the value, for the same reason a component's fields are. A name that runs out
/// of room is still readable from its first half, and a value that runs out of room is a different
/// value.
/// </para>
/// </remarks>
public static class EditorRows
{
    /// <summary>How much of the width the name takes, the value taking the rest.</summary>
    /// <remarks>
    /// Under a third, because a name that runs out of room is still readable from its first half
    /// and a number that runs out of room is a different number.
    /// </remarks>
    private const float Names = 0.3f;

    /// <summary>Opens a list, which has to be closed with <see cref="Close"/> when it opens.</summary>
    /// <param name="id">What to call it.</param>
    /// <param name="size">How large, or nothing for whatever room there is.</param>
    /// <returns>Whether it opened.</returns>
    public static bool Open(string id, Vector2 size = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        var flags = ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings;

        if (!ImGui.BeginTable(id, 2, flags, size)) return false;

        ImGui.TableSetupColumn("##name", ImGuiTableColumnFlags.WidthStretch, Names);
        ImGui.TableSetupColumn("##value", ImGuiTableColumnFlags.WidthStretch, 1f - Names);

        return true;
    }

    /// <summary>Closes it.</summary>
    public static void Close() => ImGui.EndTable();

    /// <summary>
    /// One row, with its name written and the cursor left where its value goes.
    /// </summary>
    /// <param name="name">What the row is called.</param>
    /// <param name="tip">What to say when the pointer rests on the name, if anything.</param>
    /// <param name="differs">
    /// Whether the things selected hold different values here, which dims the name.
    /// </param>
    /// <param name="unit">
    /// What the value is measured in, written after the name in parentheses and dimmed. Beside the
    /// name rather than after the value, so every box in a column ends at the same edge and the
    /// unit is read with the name it qualifies.
    /// </param>
    /// <param name="overridden">
    /// Whether the value is an instance's own rather than its model's, which colors the name.
    /// </param>
    /// <param name="revert">
    /// What puts the model's value back, offered when the name is right-clicked, or nothing when
    /// there is nothing to put back.
    /// </param>
    public static void Line(
        string name,
        string? tip = null,
        bool differs = false,
        string? unit = null,
        bool overridden = false,
        Action? revert = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        ImGui.AlignTextToFramePadding();

        Name(name, differs, overridden, revert);

        var hovered = ImGui.IsItemHovered();

        if (unit is { Length: > 0 })
        {
            Unit(unit);
            hovered |= ImGui.IsItemHovered();
        }

        if (hovered)
        {
            var says = tip is { Length: > 0 } ? tip : null;

            if (differs)
            {
                says = says is null
                    ? "These differ. Editing sets them all."
                    : says + "\n\nThese differ. Editing sets them all.";
            }

            if (overridden)
            {
                var changed = revert is null
                    ? "Changed on this instance."
                    : "Changed on this instance. Right-click to put the model's value back.";
                says = says is null ? changed : says + "\n\n" + changed;
            }

            if (says is not null) EditorWidgets.Tip(says);
        }

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1f);
    }

    /// <summary>
    /// A field's name: dimmed when the selection disagrees about it, in the accent when an instance
    /// overrides it, with a revert on a right-click when there is one.
    /// </summary>
    /// <remarks>
    /// The accent rather than a mark beside the name, because the name column is narrow and a
    /// colored name is read at a glance down a column of plain ones, as Unity's bold override
    /// names are.
    /// </remarks>
    public static void Name(string name, bool differs = false, bool overridden = false, Action? revert = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        var colored = differs || overridden;
        if (colored)
            ImGui.PushStyleColor(ImGuiCol.Text, differs ? EditorTheme.Current.Dim : EditorTheme.Current.Accent);

        ImGui.TextUnformatted(name);

        if (colored) ImGui.PopStyleColor();

        if (revert is null || !EditorWidgets.FlyoutHere("##revert")) return;

        RoundedRows.Rows(() =>
        {
            if (ImGui.MenuItem("Revert to the model's value")) revert();
            RoundedRows.Row();
        });

        EditorWidgets.EndFlyout();
    }

    /// <summary>A unit after the name just written, in parentheses and dimmed.</summary>
    /// <remarks>
    /// Its own call, since a field whose name sits on a line of its own writes the name itself and
    /// needs the same suffix after it.
    /// </remarks>
    /// <param name="unit">What the value is measured in.</param>
    public static void Unit(string unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        ImGui.SameLine(0f, ImGui.CalcTextSize(" ").X);
        ImGui.PushStyleColor(ImGuiCol.Text, EditorTheme.Current.Dim);
        ImGui.TextUnformatted($"({unit})");
        ImGui.PopStyleColor();
    }

    /// <summary>The whole width of a value's column, for something drawn rather than laid out.</summary>
    public static Vector2 Across() => new(ImGui.GetContentRegionAvail().X, 0f);
}
