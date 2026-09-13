using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Questions about an entity that the editor asks and the world does not.
/// </summary>
/// <remarks>
/// Chiefly one question. Is this entity part of the thing being edited, or part of the editor
/// looking at it? The interface is built out of ordinary entities in the same world as the scene, and it
/// names them, so a hierarchy that lists everything lists a row per span of text in its own title
/// bar. Nothing in the engine draws that line, so it is drawn here.
/// </remarks>
public static class EditorEntity
{
    /// <summary>
    /// Gives an entity a name, and puts the change on the undo stack.
    /// </summary>
    /// <remarks>
    /// Both places a name can be typed go through here, so a rename undoes the same way whichever
    /// of them did it, and typing one name over another is one change rather than two.
    /// </remarks>
    /// <param name="world">The world the entity is in.</param>
    /// <param name="entity">Which entity.</param>
    /// <param name="name">What to call it.</param>
    public static void Rename(EcsWorld world, Entity entity, string name)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var was = world.NameOf(entity) ?? string.Empty;
        if (was == name) return;

        world.SetName(entity, name);

        EditorHistory.Record(
            $"rename to {name}",
            undo => undo.SetName(entity, was),
            redo => redo.SetName(entity, name),
            $"name{entity.Bits}");
    }

    /// <summary>
    /// Puts a component on an entity, and puts the change on the undo stack.
    /// </summary>
    /// <param name="world">The world the entity is in.</param>
    /// <param name="entity">Which entity.</param>
    /// <param name="schema">Which component.</param>
    public static void Carry(EcsWorld world, Entity entity, ComponentSchema schema)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(schema);

        if (!schema.Add(world, entity)) return;

        EditorHistory.Record(
            $"add {schema.Name}",
            undo => schema.Remove(undo, entity),
            redo => schema.Add(redo, entity));
    }

    /// <summary>
    /// Takes a component off an entity, and puts the change on the undo stack.
    /// </summary>
    /// <remarks>
    /// What the component held is read before it goes, so taking it back puts the values back with
    /// it. A component added again empty is not the change undone; it is the same loss with the
    /// row drawn again over it.
    /// </remarks>
    /// <param name="world">The world the entity is in.</param>
    /// <param name="entity">Which entity.</param>
    /// <param name="schema">Which component.</param>
    public static void Drop(EcsWorld world, Entity entity, ComponentSchema schema)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(schema);

        var held = new List<(ComponentField Field, object Value)>();

        foreach (var field in schema.Fields)
        {
            if (!field.IsWritable) continue;

            // The state, and not the rows that are a view of it. A property written back after the
            // field it stands for is a setter running over the value just restored.
            if (field.Derived) continue;

            // A field that reads as nothing is one this side has no value for, which is not the
            // same as one holding nothing, and writing it back would be writing a guess.
            if (field.Read(world, entity) is not { } value) continue;

            held.Add((field, value));
        }

        if (!schema.Remove(world, entity)) return;

        EditorHistory.Record(
            $"remove {schema.Name}",
            undo =>
            {
                if (!schema.Add(undo, entity)) return;

                foreach (var (field, value) in held) field.Write(undo, entity, value);
            },
            redo => schema.Remove(redo, entity));
    }

    /// <summary>What a component of the interface is called, in part.</summary>
    /// <remarks>
    /// Matched on the component's own name rather than on the entity's, because a widget is
    /// named as freely as a cube is and the name is no help. What separates them is that a widget
    /// is a UI node and nothing in the world being edited is.
    /// </remarks>
    private static readonly string[] Marks = ["bevy_ui::"];

    /// <summary>What the engine calls the camera it draws the interface through.</summary>
    /// <remarks>
    /// A camera like any other as far as the world is concerned, which is exactly why it has to be
    /// left out of a list of what is in the world, because nobody put it there and nobody can
    /// edit it.
    /// </remarks>
    private const string InterfaceCamera = "Interface camera";

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

        if (world.NameOf(entity) == InterfaceCamera) return true;

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
    /// what gives them away is that there is nothing on them. A thing in the world being edited
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

    /// <summary>
    /// The components the engine keeps for itself, by their short names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every one of these is either worked out from something else (a global transform from a
    /// local one, a view's visibility from an inherited one, a bounding box from a mesh) or a note
    /// the engine leaves itself about what it has already done. They are on nearly every entity,
    /// none of them can be usefully changed by hand, and a strip of them in front of an inspector
    /// is a wall between somebody and the two or three components they came to read.
    /// </para>
    /// <para>
    /// A table rather than a constant, so a plugin that adds bookkeeping of its own can say so.
    /// </para>
    /// </remarks>
    public static readonly HashSet<string> Derived =
    [
        "GlobalTransform",
        "PreviousGlobalTransform",
        "TransformTreeChanged",
        "InheritedVisibility",
        "ViewVisibility",
        "VisibilityClass",
        "Aabb",
        "Name",
        "SyncToRenderWorld",
        "MainEntity",
        "RenderEntity",
        "Children",
        "ChildOf",

        // What picking leaves on anything it has raycast, which is every mesh in the scene.
        "PickingInteraction",
        "Pickable",
    ];

    /// <summary>Whether a component is one the engine keeps for itself.</summary>
    /// <remarks>
    /// Answered once per component id. The answer cannot change while the program runs, and the
    /// question is asked for every component of every entity an inspector draws.
    /// </remarks>
    public static bool IsDerived(EcsWorld world, int id)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (Bookkept.TryGetValue(id, out var answer)) return answer;

        var name = world.ComponentName(id) ?? string.Empty;
        var cut = name.LastIndexOf(':');

        answer = Derived.Contains(cut < 0 ? name : name[(cut + 1)..]);
        Bookkept[id] = answer;
        return answer;
    }

    /// <summary>Which ids are the engine's own. Held for the same reason as the marks.</summary>
    private static readonly Dictionary<int, bool> Bookkept = [];

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
