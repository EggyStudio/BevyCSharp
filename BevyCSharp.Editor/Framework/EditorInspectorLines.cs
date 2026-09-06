using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The lines the editor itself adds to an inspector, which are not fields of anything.
/// </summary>
/// <remarks>
/// Registered through the same passes a game would use. The point is not that these three lines
/// are special: it is that the panel has no idea they exist, so anything else can add its own the
/// same way and get the same rows.
/// </remarks>
public static class EditorInspectorLines
{
    /// <summary>Puts the editor's own lines into every inspector.</summary>
    public static void Register()
    {
        if (_registered) return;

        _registered = true;

        EditorInspector.OnBefore(static plan =>
        {
            if (plan.World.ParentOf(plan.Entity) is { IsNone: false } parent)
                plan.Add(new ParentLine(plan.World, plan.Entity, parent));
        });

        // What hangs from this one, as a list. Not a field of anything either, and the same
        // arrangement any other list gets: a fold that says how many there are, a fold per element,
        // and the elements drawn by whoever knows what they are.
        EditorInspector.OnAfter(static plan =>
        {
            var children = plan.World.ChildrenOf(plan.Entity);
            if (children.Length == 0) return;

            InspectorList.Add(
                plan,
                "entity/children",
                "Children",
                children.Length,
                (into, index) => into.Add(new ChildLine(into.World, children[index])));
        });
    }

    /// <summary>Whether the lines have been put in, so a second call does nothing.</summary>
    private static bool _registered;
}

/// <summary>
/// Which entity this one hangs from, with a way to go there.
/// </summary>
/// <remarks>
/// Parenting is not a component with fields, so nothing in the field tables can show it, and it
/// is the first thing somebody wants to know about an entity that is not where they left it. The
/// button goes to the parent; the row's own menu takes the entity out of the tree.
/// </remarks>
/// <param name="World">The world they live in.</param>
/// <param name="Entity">The entity being inspected.</param>
/// <param name="Parent">What it hangs from.</param>
public sealed record ParentLine(EcsWorld World, Entity Entity, Entity Parent) : IInspectorLine
{
    /// <inheritdoc/>
    public void Draw(InspectorRow row)
    {
        row.Name("Parent");
        row.Button(World.NameOf(Parent) is { Length: > 0 } name ? name : $"entity {Parent.Index}");
    }

    /// <inheritdoc/>
    public void Press(InspectorRow row)
    {
        var entity = Entity;
        var parent = Parent;

        EditorShell.ShowMenu(
            "Parent",
            [
                new MenuItem(
                    "Go to it",
                    MenuKind.Command,
                    _ => EditorSelection.Select(parent),
                    Icon: "icons/ui/select.png"),
                new MenuItem(
                    "Unparent",
                    MenuKind.Command,
                    world =>
                    {
                        world.ClearParent(entity);
                        EditorHistory.Record(
                            "unparent",
                            undo => undo.SetParent(entity, parent),
                            redo => redo.ClearParent(entity));
                    },
                    Icon: "icons/ui/remove.png"),
            ],
            row.Below.X,
            row.Below.Y);
    }
}

/// <summary>
/// One entity that hangs from the one being inspected.
/// </summary>
/// <remarks>
/// A row inside the list of children. Pressing it goes there, which is what somebody who opened the
/// list wanted: the list answers "what is under this", and the answer is only useful if it can be
/// followed.
/// </remarks>
/// <param name="World">The world they live in.</param>
/// <param name="Child">The entity the row stands for.</param>
public sealed record ChildLine(EcsWorld World, Entity Child) : IInspectorLine
{
    /// <inheritdoc/>
    public void Draw(InspectorRow row)
    {
        row.Wide();
        row.Button(World.NameOf(Child) is { Length: > 0 } name ? name : $"entity {Child.Index}");
    }

    /// <inheritdoc/>
    public void Press(InspectorRow row) => EditorSelection.Select(Child);
}
