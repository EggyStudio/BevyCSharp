using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bevy;

public static partial class SceneInstances
{
    /// <summary>Applies one override to the node it found, reporting whether it could.</summary>
    private static bool Applied(EcsWorld world, Entity node, InstanceOverride change)
    {
        if (change.Kind == OverrideKind.Rename)
        {
            if (change.Fields is not { Length: > 0 } name) return false;

            world.SetName(node, name);
            return true;
        }

        if (change.Kind == OverrideKind.Child)
        {
            // One still holding a file id is put in place once the file's entities are spawned.
            var child = ChildOf(change);
            if (child.IsNone) return true;
            if (!world.IsAlive(child)) return false;

            world.SetParent(child, node);
            return true;
        }

        if (change.Kind == OverrideKind.Delete)
        {
            world.Despawn(node);
            return true;
        }

        if (change.Component is null || ComponentSchemas.For(change.Component) is not { } schema) return false;

        if (change.Kind == OverrideKind.Remove)
        {
            if (schema.Origin == SchemaOrigin.Reflected) world.RemoveReflected(node, schema.QualifiedName);
            else schema.Remove(world, node);
            return true;
        }

        using var document = JsonDocument.Parse(change.Fields ?? "{}");

        if (schema.Origin == SchemaOrigin.Reflected)
        {
            // Bevy's own take a whole value, so what the node holds is read, the named fields laid
            // over it, and the result put back, which leaves the fields not named as they were.
            var held = world.GetReflected(node, schema.QualifiedName) is { } text ? JsonNode.Parse(text) as JsonObject : null;
            var merged = held ?? new JsonObject();
            Merge(merged, JsonNode.Parse(change.Fields ?? "{}")!.AsObject());

            try
            {
                world.InsertReflected(node, schema.QualifiedName, merged.ToJsonString());
                return true;
            }
            catch (Interop.BevyNativeException)
            {
                return false;
            }
        }

        if (!world.ComponentsOf(node).Contains(schema.Id))
        {
            if (!schema.Add(world, node)) return false;
        }
        else if (change.Kind == OverrideKind.Set && Locate(world, node) is var (data, at))
        {
            // What the model holds in each field about to be overwritten, remembered for a revert,
            // unless an earlier application in this run remembered it first.
            foreach (var field in schema.Fields)
            {
                var key = Data.Key(at, schema.QualifiedName, field.Name);
                if (data.Originals.ContainsKey(key) || !Holds(change, field.Name)) continue;
                if (field.Read(world, node) is { } held && Json(json => SceneValue.Write(json, field, held)) is { } original)
                    data.Originals[key] = original;
            }
        }

        SceneValue.ReadComponent(document.RootElement, schema, world, node);
        return true;
    }

    /// <summary>Whether an instance's overrides set a field of a component on a node.</summary>
    private static bool Holds(Data data, string at, string component, string field) =>
        data.Overrides.Any(change =>
            change.At == at
            && change.Kind is OverrideKind.Set or OverrideKind.Add
            && change.Component is { } named
            && (named == component || ComponentSchemas.For(named)?.QualifiedName == component)
            && Holds(change, field));

    /// <summary>Whether an override's fields name a field, by its dotted path.</summary>
    private static bool Holds(InstanceOverride change, string field)
    {
        JsonNode? at = JsonNode.Parse(change.Fields ?? "{}");
        foreach (var part in field.Split('.'))
        {
            if (at is not JsonObject inner || !inner.TryGetPropertyValue(part, out at)) return false;
        }

        return true;
    }

    /// <summary>
    /// Takes a field out of the override that sets it, and the override itself once a set names no
    /// field, since a set that changes nothing is not a change.
    /// </summary>
    private static void Unset(Data data, string at, string component, string field)
    {
        var index = data.Overrides.FindIndex(change =>
            change.At == at && change.Component == component && change.Kind is OverrideKind.Set or OverrideKind.Add);
        if (index < 0) return;

        var change = data.Overrides[index];
        var fields = JsonNode.Parse(change.Fields ?? "{}")!.AsObject();

        // Down to the field, keeping each object on the way so an emptied one can be taken too.
        var parts = field.Split('.');
        var trail = new List<JsonObject> { fields };
        foreach (var part in parts[..^1])
        {
            if (trail[^1][part] is not JsonObject next) return;
            trail.Add(next);
        }

        trail[^1].Remove(parts[^1]);
        for (var i = trail.Count - 1; i > 0 && trail[i].Count == 0; i--) trail[i - 1].Remove(parts[i - 1]);

        if (fields.Count == 0 && change.Kind == OverrideKind.Set) data.Overrides.RemoveAt(index);
        else data.Overrides[index] = change with { Fields = fields.ToJsonString() };
    }

    /// <summary>Records an override, folding it into an earlier one for the same node and component.</summary>
    /// <remarks>
    /// A set after a set merges fields, a set after an add merges into the add, and a remove or a
    /// delete replaces what was recorded for what it takes away, so the list says what the
    /// instance ends up with rather than every step taken to get there.
    /// </remarks>
    private static void Record(Data data, InstanceOverride change)
    {
        var overrides = data.Overrides;

        if (change.Kind == OverrideKind.Delete)
        {
            overrides.RemoveAll(earlier => earlier.At == change.At || earlier.At.StartsWith(change.At + "/", StringComparison.Ordinal));
            overrides.Add(change);
            return;
        }

        var index = overrides.FindIndex(earlier => earlier.At == change.At && earlier.Component == change.Component);
        if (index < 0)
        {
            overrides.Add(change);
            return;
        }

        var earlier = overrides[index];
        if (change.Kind == OverrideKind.Set && earlier.Kind is OverrideKind.Set or OverrideKind.Add)
        {
            var fields = JsonNode.Parse(earlier.Fields ?? "{}")!.AsObject();
            Merge(fields, JsonNode.Parse(change.Fields ?? "{}")!.AsObject());
            overrides[index] = earlier with { Fields = fields.ToJsonString() };
            return;
        }

        // A remove after an add of a component the model never had leaves nothing to record.
        if (change.Kind == OverrideKind.Remove && earlier.Kind == OverrideKind.Add)
        {
            overrides.RemoveAt(index);
            return;
        }

        overrides[index] = change;
    }

    /// <summary>The instance a node is in and its path there, or nothing for a node in none.</summary>
    private static (Data Data, string At)? Locate(EcsWorld world, Entity node)
    {
        ArgumentNullException.ThrowIfNull(world);

        // The root's own components are the instance's, written with it like any entity's, so the
        // root is not one of the nodes an override addresses. The outermost instance, because the
        // ones nested in it are spawned from its scene and not written, so a change inside one is
        // kept by the instance that is.
        // An entity the scene placing the instance added is that scene's own, written with it.
        var root = OuterRootOf(world, node);
        if (root == node || !IsFromModel(world, node)) return null;
        if (DataOf(world, root) is not { } data || PathOf(world, root, node) is not { } at) return null;
        return (data, at);
    }

    /// <summary>
    /// What the model called each node it spawned, kept so a path names a renamed node as the
    /// model does, by entity, for the app the entities belong to.
    /// </summary>
    private static readonly Dictionary<Entity, string?> ModelNames = [];

    private static int _namesGeneration = -1;

    /// <summary>A node's name as the model gave it, or its own name for an entity the model did not spawn.</summary>
    private static string? NameOf(EcsWorld world, Entity node)
    {
        lock (ModelNames)
        {
            if (_namesGeneration != ComponentRegistry.Generation)
            {
                // Entities belong to a world, so another app's names say nothing about this one's.
                ModelNames.Clear();
                _namesGeneration = ComponentRegistry.Generation;
            }

            return ModelNames.TryGetValue(node, out var name) ? name : world.NameOf(node);
        }
    }

    /// <summary>Remembers what the model called a node it spawned.</summary>
    private static void RememberName(EcsWorld world, Entity node)
    {
        _ = NameOf(world, node);
        lock (ModelNames) ModelNames.TryAdd(node, world.NameOf(node));
    }

    /// <summary>One step of a path, a node's name numbered after the earlier siblings sharing it.</summary>
    private static string Step(EcsWorld world, Entity node)
    {
        var siblings = world.ChildrenOf(world.ParentOf(node));
        var name = NameOf(world, node);

        if (name is null)
        {
            var place = Array.IndexOf(siblings, node) + 1;
            return "#" + place;
        }

        var same = 0;
        foreach (var sibling in siblings)
        {
            if (NameOf(world, sibling) == name) same++;
            if (sibling == node) break;
        }

        return same > 1 ? $"{name}#{same}" : name;
    }

    /// <summary>Lays one object's values over another's, going into objects both have.</summary>
    private static void Merge(JsonObject into, JsonObject from)
    {
        foreach (var (name, value) in from.ToArray())
        {
            if (value is JsonObject inner && into[name] is JsonObject existing)
            {
                Merge(existing, inner);
                continue;
            }

            into[name] = value?.DeepClone();
        }
    }

    /// <summary>Puts a value at a dotted name inside nested objects, making the objects on the way.</summary>
    private static void Place(JsonObject into, string name, JsonNode? value)
    {
        var parts = name.Split('.');
        var at = into;
        foreach (var part in parts[..^1])
        {
            if (at[part] is not JsonObject next)
            {
                next = new JsonObject();
                at[part] = next;
            }

            at = next;
        }

        at[parts[^1]] = value;
    }

    /// <summary>What a writer writes, as text, or nothing when it reports it wrote nothing.</summary>
    private static string? Json(Func<Utf8JsonWriter, bool> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        bool wrote;
        using (var json = new Utf8JsonWriter(buffer)) wrote = write(json);
        return wrote ? Encoding.UTF8.GetString(buffer.WrittenSpan) : null;
    }
}
