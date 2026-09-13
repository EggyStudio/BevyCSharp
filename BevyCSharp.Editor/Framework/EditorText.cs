using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Text as a panel shows it, rather than as something else wrote it.
/// </summary>
/// <remarks>
/// A name that came from the engine is a Rust path, and a name that came from a disk is whatever
/// somebody called a file. Neither was written to be read in a column two inches wide, and both are
/// shown in several places, so how they are shortened is decided here rather than in each panel.
/// </remarks>
public static class EditorText
{
    /// <summary>
    /// What a component is called, without the path it lives at.
    /// </summary>
    /// <remarks>
    /// Every part of the name loses its path, not just the last one, because a component's name is
    /// often another component's name inside it, so cutting at the last <c>::</c> of
    /// <c>MeshMaterial3d&lt;StandardMaterial&gt;</c> leaves <c>StandardMaterial&gt;</c>.
    /// </remarks>
    /// <param name="name">The name as the engine gives it.</param>
    public static string Short(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var trimmed = new System.Text.StringBuilder();
        var start = 0;

        for (var index = 0; index <= name.Length; index++)
        {
            if (index < name.Length && name[index] is not ('<' or '>' or ',' or ' ')) continue;

            var part = name[start..index];
            var cut = part.LastIndexOf("::", StringComparison.Ordinal);

            trimmed.Append(cut >= 0 ? part[(cut + 2)..] : part);
            if (index < name.Length) trimmed.Append(name[index]);

            start = index + 1;
        }

        return trimmed.ToString();
    }

    /// <summary>
    /// As much of a name as fits, with an ellipsis where the rest was.
    /// </summary>
    /// <remarks>
    /// For anything laid out in a fixed width, where a name that ran on would run into whatever is
    /// beside it. Where the room is the rest of a row instead, the name is clipped to it, since a
    /// name cut off by an edge says it ran out of room by itself.
    /// </remarks>
    /// <param name="name">What is to be written.</param>
    /// <param name="room">How much width there is for it.</param>
    public static string Fit(string name, float room)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (ImGui.CalcTextSize(name).X <= room) return name;

        for (var length = name.Length - 1; length > 1; length--)
        {
            var cut = string.Concat(name.AsSpan(0, length), "...");
            if (ImGui.CalcTextSize(cut).X <= room) return cut;
        }

        return name;
    }
}
