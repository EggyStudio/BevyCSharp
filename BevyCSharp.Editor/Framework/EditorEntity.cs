using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Questions about an entity that the editor asks and the world does not.
/// </summary>
/// <remarks>
/// Chiefly one: is this entity part of the thing being edited, or part of the editor looking at
/// it? The interface is built out of ordinary entities in the same world as the scene, and it
/// names them, so a hierarchy that lists everything lists a row per span of text in its own title
/// bar. Nothing in the engine draws that line, so it is drawn here.
/// </remarks>
public static class EditorEntity
{
    /// <summary>What a component of the interface is called, in part.</summary>
    /// <remarks>
    /// Matched on the component's own name rather than on the entity's, because a widget is
    /// named as freely as a cube is and the name is no help. What separates them is that a widget
    /// is a UI node and nothing in the world being edited is.
    /// </remarks>
    private static readonly string[] Marks = ["bevy_ui::", "extended_ui"];

    /// <summary>
    /// Whether a component id belongs to the interface, remembered once per id.
    /// </summary>
    /// <remarks>
    /// Naming a component crosses the ABI and copies a string, and the answer for an id never
    /// changes while an app runs, so it is asked once. Without this a hierarchy would ask it
    /// thousands of times a frame.
    /// </remarks>
    /// <remarks>
    /// Ids belong to a world, so this holds only while one app does. The editor is one app for
    /// the life of the process; a second would need this cleared with the rest of the ids.
    /// </remarks>
    private static readonly Dictionary<int, bool> Known = [];

    /// <summary>Whether an entity is part of the editor's interface rather than the world.</summary>
    public static bool IsInterface(EcsWorld world, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(world);

        foreach (var id in world.ComponentsOf(entity))
        {
            if (!Known.TryGetValue(id, out var isInterface))
            {
                var name = world.ComponentName(id);
                isInterface = Marks.Any(mark => name.Contains(mark, StringComparison.Ordinal));
                Known[id] = isInterface;
            }

            if (isInterface) return true;
        }

        return false;
    }

    /// <summary>
    /// Whether an entity is one of the engine's own, carrying a name and nothing else.
    /// </summary>
    /// <remarks>
    /// The renderers gizmo drawing spawns, the entities an observer is held on, and whatever else
    /// a plugin names for its own logs. They are not the interface and they are not the scene, and
    /// what gives them away is that there is nothing on them: a thing in the world being edited
    /// has at least one component that says what it is.
    /// </remarks>
    public static bool IsBookkeeping(EcsWorld world, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(world);

        var only = false;

        foreach (var id in world.ComponentsOf(entity))
        {
            if (!IsName(world, id)) return false;
            only = true;
        }

        return only;
    }

    /// <summary>The engine's name component, which this side has no type for.</summary>
    private const string NameComponent = "bevy_ecs::name::Name";

    /// <summary>Whether a component id is the name, remembered once per id.</summary>
    private static bool IsName(EcsWorld world, int id)
    {
        if (Named.TryGetValue(id, out var answer)) return answer;

        answer = world.ComponentName(id) == NameComponent;
        Named[id] = answer;
        return answer;
    }

    /// <summary>Which ids are the name component. Held for the same reason as the marks.</summary>
    private static readonly Dictionary<int, bool> Named = [];
}
