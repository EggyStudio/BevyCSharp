using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Drawers;

/// <summary>
/// One of a fixed set of names: a button that offers the rest.
/// </summary>
/// <remarks>
/// A button rather than a list on the row. A row is twenty pixels tall and an enum has as many
/// values as it likes, so the values go where a list of things to choose from already goes, which
/// is the menu.
/// </remarks>
public sealed class ChoiceDrawer : IFieldDrawer
{
    /// <inheritdoc/>
    public bool Handles(ComponentField field) => field.Kind == FieldKind.Enum;

    /// <inheritdoc/>
    public void Draw(InspectorRow row, int part, FieldTarget target)
    {
        row.Name(target.Field.Title);
        row.Button(target.Read()?.ToString() ?? string.Empty);
    }

    /// <inheritdoc/>
    public void Read(InspectorRow row, int part, FieldTarget target)
    {
    }

    /// <inheritdoc/>
    public void Press(InspectorRow row, int part, FieldTarget target)
    {
        if (!target.Field.IsWritable) return;

        var field = target.Field;
        var entity = target.Entity;
        var items = new List<MenuItem>();

        foreach (var option in field.Options)
        {
            var chosen = option;

            items.Add(new MenuItem(
                option,
                MenuKind.Command,
                world => EditorFields.Change(world, entity, field, chosen)));
        }

        var (x, y) = row.Below;
        EditorShell.ShowMenu(field.Name, items, x, y);
    }
}
