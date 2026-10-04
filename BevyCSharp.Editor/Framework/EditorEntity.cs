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

        // A node of an instance is renamed as an override, so the scene's next load names it the
        // same, and its path keeps the model's name so the other overrides still find it.
        var node = SceneInstances.IsFromModel(world, entity);
        Rename(world, entity, name, node);

        EditorHistory.Record(
            $"rename to {name}",
            undo => Rename(undo, EditorHistory.Resolve(entity), was, node),
            redo => Rename(redo, EditorHistory.Resolve(entity), name, node),
            $"name{entity.Bits}");
    }

    /// <summary>Names an entity, through its instance when it is a node of one.</summary>
    private static void Rename(EcsWorld world, Entity entity, string name, bool node)
    {
        if (node && name.Length > 0 && SceneInstances.Rename(world, entity, name)) return;
        world.SetName(entity, name);
    }

    /// <summary>
    /// Spawns a copy of each entity given, selects the copies, and puts the change on the undo
    /// stack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The copy is the engine's own clone, so it carries everything the original does, including
    /// the mesh and material it is drawn with and Bevy's components with no schema, rather than
    /// only what the editor can read off an entity. It sits under the same parent, and the
    /// original's children are not copied with it.
    /// </para>
    /// <para>
    /// A named entity's copy is given the next free name in the same style, "Cube 2" for "Cube", so
    /// the world list tells the two apart and a selection remembered by name finds the right one.
    /// Undoing despawns the copies and redoing makes them again from the originals, which are new
    /// entities each time, as a new entity always is.
    /// </para>
    /// </remarks>
    /// <param name="world">The world the entities are in.</param>
    /// <param name="sources">What to copy.</param>
    /// <returns>The copies, in the order of the sources, skipping any that no longer exist.</returns>
    public static IReadOnlyList<Entity> Duplicate(EcsWorld world, IReadOnlyList<Entity> sources)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(sources);

        var made = Copies(world, sources);
        if (made.Count == 0) return made;

        Choose(made);

        var held = made.ToArray();
        EditorHistory.Record(
            made.Count == 1 ? "duplicate" : $"duplicate {made.Count}",
            undo =>
            {
                foreach (var copy in held) undo.Despawn(EditorHistory.Resolve(copy));
                EditorSelection.Clear();
            },
            redo =>
            {
                held = [.. Copies(redo, [.. sources.Select(EditorHistory.Resolve)])];
                Choose(held);
            });

        return made;
    }

    /// <summary>
    /// Despawns each entity given with what is under it, and puts the change on the undo stack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What goes is written as a scene first, in memory, the way Project/Save writes the world:
    /// every component the scene format holds, and the mesh and material each is drawn with, by
    /// the file it came from or, made in memory, as a primitive's recipe, a mesh's geometry or a
    /// material's settings. Undoing reads it back, puts each top entity under the parent it had,
    /// and points every entity field outside it that named one of them at the entity that came
    /// back, so a camera following a deleted target follows it again. Redoing deletes those the
    /// same way.
    /// </para>
    /// <para>
    /// What comes back is new entities, since Bevy gives a despawned entity's id to nothing again,
    /// so the history is told which old one each stands for (<see cref="EditorHistory.CameBack"/>)
    /// and every edit on it reaches the new one. The components a scene leaves out, the engine's own
    /// bookkeeping, are worked out again by the engine as they are on a load.
    /// </para>
    /// <para>
    /// Two kinds are deleted without a way back, each with a line in the console saying so. One is
    /// a node of a placed model, which is a change recorded in the model's instance and comes back
    /// only with the model, and the other an entity drawn with a mesh or a material the scene
    /// cannot describe, which would come back with nothing to draw.
    /// </para>
    /// </remarks>
    /// <param name="world">The world the entities are in.</param>
    /// <param name="chosen">What to delete.</param>
    public static void Delete(EcsWorld world, IReadOnlyList<Entity> chosen)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(chosen);

        var alive = chosen.Where(world.IsAlive).ToHashSet();

        // A node of an instance is deleted as an override, so the next load of the scene does not
        // bring it back with the model, and is not something a scene of its own can restore.
        foreach (var node in alive.ToArray())
        {
            if (!SceneInstances.IsFromModel(world, node) || !SceneInstances.Delete(world, node)) continue;

            alive.Remove(node);
            Console.WriteLine($"[editor] {Called(world, node)} is part of a placed model, so deleting it cannot be undone");
        }

        // The top ones, since deleting one deletes what is under it.
        var tops = alive.Where(entity => !Ancestors(world, entity).Any(alive.Contains)).ToList();
        if (tops.Count == 0)
        {
            EditorSelection.Clear();
            return;
        }

        var kept = Keep(world, tops, out var lost);

        // Named while there is still something to ask.
        var title = tops.Count == 1 ? $"delete {Called(world, tops[0])}" : $"delete {tops.Count}";

        foreach (var top in tops) world.Despawn(top);
        EditorSelection.Clear();

        foreach (var what in lost) Console.WriteLine($"[editor] {what}, so deleting it cannot be undone");

        if (kept is null) return;

        var held = kept;
        List<Entity> back = [];

        EditorHistory.Record(
            title,
            undo =>
            {
                back = Restore(undo, held);
                Choose(back);
            },
            redo =>
            {
                // The ones the undo brought back, kept again as they stand, and deleted.
                var again = back.Where(redo.IsAlive).ToList();
                held = Keep(redo, again, out _) ?? held;
                foreach (var top in again) redo.Despawn(top);
                EditorSelection.Clear();
            });
    }

    /// <summary>What a delete keeps to bring back, the scene it wrote and where each top one was.</summary>
    private sealed record Kept(string Scene, (int Id, Entity Parent, Entity Was)[] Tops, Dictionary<int, Entity> Was);

    /// <summary>
    /// Writes the entities under each top one as a scene, leaving out the tops that cannot come back
    /// whole, which <paramref name="lost"/> says why of.
    /// </summary>
    private static Kept? Keep(EcsWorld world, List<Entity> tops, out List<string> lost)
    {
        lost = [];

        var whole = new List<Entity>();
        foreach (var top in tops)
        {
            var under = Subtree(world, top);

            // Drawn with something the scene cannot say how to make, which would come back as an
            // entity with nothing to draw.
            if (App.HasRenderer && under.FirstOrDefault(entity => !Drawable(world, entity)) is { IsNone: false } bare)
            {
                lost.Add($"{Called(world, bare)} is drawn with a mesh or material a scene cannot describe");
                continue;
            }

            whole.Add(top);
        }

        if (whole.Count == 0) return null;

        var members = whole.SelectMany(top => Subtree(world, top)).ToHashSet();

        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        using (var json = new System.Text.Json.Utf8JsonWriter(buffer))
            SceneFile.Write(world, json, members.Contains);

        // Writing gave each entity its id in the scene, which the one read back carries too.
        var was = members.ToDictionary(entity => IdOf(world, entity), entity => entity);
        var placed = whole.Select(top => (IdOf(world, top), world.ParentOf(top), top)).ToArray();

        return new Kept(System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan), placed, was);
    }

    /// <summary>
    /// Spawns what a delete kept, puts each top one back under its parent, and points the entity
    /// fields of everything else that named one of the old entities at the new one.
    /// </summary>
    /// <returns>The top entities that came back.</returns>
    private static List<Entity> Restore(EcsWorld world, Kept kept)
    {
        using var document = System.Text.Json.JsonDocument.Parse(kept.Scene);
        var load = SceneFile.Read(world, document.RootElement);

        var spawned = new Dictionary<int, Entity>();
        foreach (var entity in load.Entities)
        {
            if (world.TryGet<SceneId>(entity, out var id)) spawned[id.Value] = entity;
        }

        var tops = new List<Entity>();
        foreach (var (id, parent, _) in kept.Tops)
        {
            if (!spawned.TryGetValue(id, out var top)) continue;

            if (!parent.IsNone && world.IsAlive(parent)) world.SetParent(top, parent);
            tops.Add(top);
        }

        var moved = new Dictionary<Entity, Entity>();
        foreach (var (id, old) in kept.Was)
        {
            if (spawned.TryGetValue(id, out var back)) moved[old] = back;
        }

        Repoint(world, moved, load.Entities.ToHashSet());

        // And the edits recorded on the old ones, which reach the new through this.
        EditorHistory.CameBack(moved);
        return tops;
    }

    /// <summary>Points every entity field outside <paramref name="skip"/> that names an old entity at its new one.</summary>
    /// <remarks>
    /// The fields a schema lists at the top of a component, which is where a reference to another
    /// entity is held. One inside a list's item is left as it was.
    /// </remarks>
    private static void Repoint(EcsWorld world, Dictionary<Entity, Entity> moved, HashSet<Entity> skip)
    {
        if (moved.Count == 0) return;

        foreach (var entity in world.All())
        {
            if (skip.Contains(entity)) continue;

            foreach (var id in world.ComponentsOf(entity))
            {
                if (ComponentSchemas.For(id) is not { } schema) continue;

                foreach (var field in schema.Fields)
                {
                    if (field.Kind != FieldKind.Entity || !field.IsWritable) continue;
                    if (field.Read(world, entity) is Entity named && moved.TryGetValue(named, out var back))
                        field.Write(world, entity, back);
                }
            }
        }
    }

    /// <summary>The id a scene gave an entity when it was written.</summary>
    private static int IdOf(EcsWorld world, Entity entity) =>
        world.TryGet<SceneId>(entity, out var id) ? id.Value : 0;

    /// <summary>An entity and everything under it, the entity first.</summary>
    private static List<Entity> Subtree(EcsWorld world, Entity top)
    {
        var found = new List<Entity>();
        var stack = new Stack<Entity>([top]);

        while (stack.TryPop(out var entity))
        {
            // A placed model's own nodes come back with the model, as a scene file writes them.
            if (entity != top && SceneInstances.IsFromModel(world, entity)) continue;

            found.Add(entity);
            foreach (var child in world.ChildrenOf(entity)) stack.Push(child);
        }

        return found;
    }

    /// <summary>The entities above one, nearest first.</summary>
    private static IEnumerable<Entity> Ancestors(EcsWorld world, Entity entity)
    {
        for (var above = world.ParentOf(entity); !above.IsNone; above = world.ParentOf(above)) yield return above;
    }

    /// <summary>Whether a scene can say how to make what an entity is drawn with, or it is drawn with nothing.</summary>
    private static bool Drawable(EcsWorld world, Entity entity) =>
        SceneFile.CanDescribe(world, entity);

    /// <summary>An entity as a line in the console names it.</summary>
    private static string Called(EcsWorld world, Entity entity) =>
        world.IsAlive(entity) && world.NameOf(entity) is { Length: > 0 } name ? name : entity.ToString();

    /// <summary>Clones each source that still exists and names the copies.</summary>
    private static List<Entity> Copies(EcsWorld world, IReadOnlyList<Entity> sources)
    {
        var made = new List<Entity>();
        foreach (var source in sources)
        {
            var copy = world.Clone(source);
            if (copy.IsNone) continue;

            if (world.NameOf(source) is { Length: > 0 } name) world.SetName(copy, NextName(world, name));
            made.Add(copy);
        }

        return made;
    }

    /// <summary>Selects exactly the entities given.</summary>
    private static void Choose(IReadOnlyList<Entity> chosen)
    {
        EditorSelection.Select(chosen.Count > 0 ? chosen[0] : Entity.None);
        foreach (var also in chosen.Skip(1)) EditorSelection.Toggle(also);
    }

    /// <summary>
    /// The first name of the form "Cube 2", "Cube 3" that no entity has, counting on from a number
    /// the name already ends in.
    /// </summary>
    private static string NextName(EcsWorld world, string name)
    {
        var taken = world.All()
            .Select(world.NameOf)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        var space = name.LastIndexOf(' ');
        var stem = name;
        var number = 1;

        if (space > 0 && int.TryParse(name[(space + 1)..], out var counted))
        {
            stem = name[..space];
            number = counted;
        }

        string next;
        do next = $"{stem} {++number}";
        while (taken.Contains(next));

        return next;
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

        // Kept as an override on a node of an instance, and taken back out by the undo.
        SceneInstances.MarkAdded(world, entity, schema.QualifiedName);

        EditorHistory.Record(
            $"add {schema.Name}",
            undo =>
            {
                var at = EditorHistory.Resolve(entity);
                schema.Remove(undo, at);
                SceneInstances.MarkRemoved(undo, at, schema.QualifiedName);
            },
            redo =>
            {
                var at = EditorHistory.Resolve(entity);
                schema.Add(redo, at);
                SceneInstances.MarkAdded(redo, at, schema.QualifiedName);
            });
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

        SceneInstances.MarkRemoved(world, entity, schema.QualifiedName);

        EditorHistory.Record(
            $"remove {schema.Name}",
            undo =>
            {
                var at = EditorHistory.Resolve(entity);
                if (!schema.Add(undo, at)) return;

                foreach (var (field, value) in held) field.Write(undo, at, value);
                SceneInstances.MarkAdded(undo, at, schema.QualifiedName);
            },
            redo =>
            {
                var at = EditorHistory.Resolve(entity);
                schema.Remove(redo, at);
                SceneInstances.MarkRemoved(redo, at, schema.QualifiedName);
            });
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

        // The asset preview is the editor's too. It is in this world because there is one world,
        // and nobody put it there.
        if (PreviewRenderer.Owns(entity)) return true;

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
    /// Started from <see cref="SceneFile.Computed"/>, the components a scene leaves out for the same
    /// reason, so what the inspector hides and what a save skips are one list. A table rather than
    /// a constant, so a plugin that adds bookkeeping of its own can say so.
    /// </para>
    /// </remarks>
    public static readonly HashSet<string> Derived = [.. SceneFile.Computed];

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
