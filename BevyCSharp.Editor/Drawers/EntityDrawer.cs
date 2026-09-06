using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Drawers;

/// <summary>
/// A reference to another entity: its name, and a way to go to it or change it.
/// </summary>
/// <remarks>
/// A number is the wrong answer here. An entity's index says nothing about what it is, and the
/// question somebody has about a reference is always "which one is that", which is answered by
/// its name and by being able to jump to it.
/// </remarks>
public sealed class EntityDrawer : IFieldDrawer
{
    /// <inheritdoc/>
    public bool Handles(ComponentField field) => field.Kind == FieldKind.Entity;

    /// <inheritdoc/>
    public void Draw(InspectorRow row, int part, FieldTarget target)
    {
        row.Name(target.Field.Title);
        row.Button(Named(target.World, target.Read() as Entity? ?? Entity.None));
    }

    /// <inheritdoc/>
    public void Read(InspectorRow row, int part, FieldTarget target)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Pressing it offers what the reference could be, with the entity it already names first.
    /// Nothing else in the panel can pick an entity, and a field that can only be read is a field
    /// somebody has to leave the editor to set.
    /// </remarks>
    public void Press(InspectorRow row, int part, FieldTarget target)
    {
        var field = target.Field;
        var owner = target.Entity;
        var world = target.World;
        var held = target.Read() as Entity? ?? Entity.None;

        var items = new List<MenuItem>();

        if (!held.IsNone)
        {
            var jump = held;

            items.Add(new MenuItem(
                "Go to " + Named(world, held),
                MenuKind.Command,
                _ => EditorSelection.Select(jump),
                Icon: "icons/ui/select.png"));
        }

        if (field.IsWritable)
        {
            items.Add(new MenuItem("Nothing", MenuKind.Command, w =>
                EditorFields.Change(w, owner, field, Entity.None)));

            foreach (var candidate in world.All())
            {
                if (candidate == owner) continue;
                if (world.NameOf(candidate) is not { Length: > 0 } name) continue;
                if (EditorEntity.IsInterface(world, candidate)) continue;
                if (items.Count > 24) break;

                var chosen = candidate;

                items.Add(new MenuItem(
                    name,
                    MenuKind.Command,
                    w => EditorFields.Change(w, owner, field, chosen)));
            }
        }

        if (items.Count == 0) items.Add(new MenuItem("nothing to point at", MenuKind.Separator));

        var (x, y) = row.Below;
        EditorShell.ShowMenu(field.Title, items, x, y);
    }

    /// <summary>What an entity is called, or what it is when it has no name.</summary>
    private static string Named(EcsWorld world, Entity entity)
    {
        if (entity.IsNone) return "none";

        return world.NameOf(entity) is { Length: > 0 } name ? name : $"entity {entity.Index}";
    }
}
