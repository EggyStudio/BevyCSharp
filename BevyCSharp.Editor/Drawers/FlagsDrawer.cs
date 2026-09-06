using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Drawers;

/// <summary>
/// Any number of a fixed set of names at once: one row per name, each with a tick.
/// </summary>
/// <remarks>
/// <para>
/// A menu is the wrong shape for this. Choosing one of five is a question with one answer and a
/// list is the right way to ask it; turning three of five on is five questions, and a list that
/// has to be reopened after each one is a list somebody fights.
/// </para>
/// <para>
/// The value is read and written as the names themselves rather than as bits. Which bit each name
/// stands for is the enum's business, and asking for the names back is the one way to be right
/// about it for an enum whose values are not the obvious powers of two.
/// </para>
/// </remarks>
public sealed class FlagsDrawer : IFieldDrawer
{
    /// <inheritdoc/>
    public bool Handles(ComponentField field) => field.Kind == FieldKind.Flags;

    /// <inheritdoc/>
    /// <remarks>The name of the field, and then one row for each of its names.</remarks>
    public int Lines(ComponentField field) => 1 + field.Options.Count;

    /// <inheritdoc/>
    public void Draw(InspectorRow row, int part, FieldTarget target)
    {
        if (part == 0)
        {
            row.Name(target.Field.Title);
            row.Unit(Summary(target));
            return;
        }

        var option = target.Field.Options[part - 1];

        row.Name(option);
        row.Tick(On(target).Contains(option));
    }

    /// <inheritdoc/>
    public void Read(InspectorRow row, int part, FieldTarget target)
    {
        if (part == 0 || !target.Field.IsWritable) return;

        var option = target.Field.Options[part - 1];
        var on = On(target);

        if (on.Contains(option) == row.Ticked) return;

        if (row.Ticked) on.Add(option);
        else on.Remove(option);

        target.Write(on.Count == 0 ? "0" : string.Join(", ", on));
    }

    /// <summary>Which names are currently on.</summary>
    private static HashSet<string> Written(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var part in text.Split(','))
        {
            var name = part.Trim();

            // A value with no names in it reads as the number it is, which is nothing set.
            if (name.Length == 0 || char.IsDigit(name[0]) || name[0] == '-') continue;

            names.Add(name);
        }

        return names;
    }

    /// <inheritdoc cref="Written"/>
    private static HashSet<string> On(FieldTarget target) => Written(target.Read());

    /// <summary>What the whole value reads as, for the row that names the field.</summary>
    private static string Summary(FieldTarget target)
    {
        var on = On(target);

        return on.Count switch
        {
            0 => "none",
            1 => on.First(),
            _ => $"{on.Count} of {target.Field.Options.Count}",
        };
    }
}
