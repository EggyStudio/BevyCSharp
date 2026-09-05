using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Drawers;

/// <summary>
/// Anything with no better editor: a box with the value written in it.
/// </summary>
/// <remarks>
/// The last drawer asked, and the one that answers for a field the editor has never heard of.
/// Showing what a value reads as is a truthful answer; showing nothing is not, and a field that
/// disappears because the editor lacks a drawer for it is how somebody loses a component without
/// noticing.
/// </remarks>
public sealed class TextDrawer : IFieldDrawer
{
    /// <inheritdoc/>
    public bool Handles(ComponentField field) => true;

    /// <inheritdoc/>
    public void Draw(InspectorRow row, int part, FieldTarget target)
    {
        row.Name(target.Field.Name);
        row.Box(Written(target.Read()));
    }

    /// <inheritdoc/>
    public void Read(InspectorRow row, int part, FieldTarget target)
    {
        if (!target.Field.IsWritable) return;

        var typed = row.Typed.Trim();
        if (typed.Length == 0) return;
        if (Written(target.Read()) == typed) return;

        target.Write(typed);
    }

    /// <summary>What a value reads as in a box.</summary>
    public static string Written(object? value) => value switch
    {
        null => string.Empty,
        float number => EditorFields.Text(number),
        double number => EditorFields.Text(number),
        Entity entity => entity.IsNone ? "none" : entity.Index.ToString(),
        bool flag => flag ? "true" : "false",
        _ => value.ToString() ?? string.Empty,
    };
}
