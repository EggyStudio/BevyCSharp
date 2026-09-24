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
/// Weighted towards the value, for the same reason a component's fields are. A name that runs out
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
    public static void Line(string name, string? tip = null, bool differs = false)
    {
        ArgumentNullException.ThrowIfNull(name);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        ImGui.AlignTextToFramePadding();

        // Dimmed when the things selected disagree about it, because the box beside it can only
        // show one of their values and the name is the only room left to say so.
        if (differs) ImGui.PushStyleColor(ImGuiCol.Text, EditorTheme.Current.Dim);

        ImGui.TextUnformatted(name);

        if (differs) ImGui.PopStyleColor();

        if (ImGui.IsItemHovered())
        {
            var says = tip is { Length: > 0 } ? tip : null;

            if (differs)
            {
                says = says is null
                    ? "These differ. Editing sets them all."
                    : says + "\n\nThese differ. Editing sets them all.";
            }

            if (says is not null) EditorWidgets.Tip(says);
        }

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1f);
    }

    /// <summary>The whole width of a value's column, for something drawn rather than laid out.</summary>
    public static Vector2 Across() => new(ImGui.GetContentRegionAvail().X, 0f);
}
