using System.Buffers;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// Moves the components an assembly loaded again declares onto the new types it declares them as,
/// and puts back the ones a scene held before any type described them.
/// </summary>
/// <remarks>
/// <para>
/// A behavior script compiled again while an app runs is a new assembly, so its <c>Wallet</c> is a
/// new type with the old one's name, registered with Bevy as a component of its own. The entities a
/// level put the old one on would carry it still, and every system of the new script would query
/// for the new one and find none of them, so editing a script of a game being played would stop
/// it dead.
/// </para>
/// <para>
/// Each old component is written the way a scene writes it and read into the new type the way a
/// scene is read, so a field kept keeps its value, a field added starts at its default, one taken
/// away is dropped, and a version raised with a migration runs it. A field referring to an entity
/// or an asset is carried as the reference itself, so a mesh made in code and never saved survives,
/// and a list is copied into a list of the new component's own before the old one gives its slot
/// back. Nothing a game would see as an add or a removal runs.
/// </para>
/// </remarks>
public static class ReloadedComponents
{
    /// <summary>The schemas there are, taken before the new assembly registers its own over them.</summary>
    public static IReadOnlyList<ComponentSchema> Before() =>
        [.. ComponentSchemas.All.Where(schema => schema.Origin != SchemaOrigin.Reflected && schema.CanAdd)];

    /// <summary>
    /// Moves every component of a type registered again since <paramref name="before"/> onto the
    /// type registered in its place.
    /// </summary>
    /// <param name="world">The world, on loan to the system the reload runs in.</param>
    /// <param name="before">What <see cref="Before"/> returned before the new assembly was loaded.</param>
    /// <returns>How many components were moved.</returns>
    public static int Carry(EcsWorld world, IReadOnlyList<ComponentSchema> before)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(before);

        // An old schema whose name a new one has taken, and both ids, keyed by the old id an entity
        // carries. The new schema's id is asked for here, which registers the new type with Bevy.
        var moved = new Dictionary<int, (ComponentSchema From, ComponentSchema To)>();
        foreach (var from in before)
        {
            if (ComponentSchemas.For(from.QualifiedName) is not { } to || ReferenceEquals(to, from) || !to.CanAdd) continue;

            var old = from.Id;
            if (old != to.Id) moved[old] = (from, to);
        }

        if (moved.Count == 0) return 0;

        var carried = 0;
        foreach (var entity in world.All())
        {
            foreach (var id in world.ComponentsOf(entity))
            {
                if (!moved.TryGetValue(id, out var pair)) continue;

                Move(world, entity, pair.From, pair.To);
                carried++;
            }
        }

        return carried;
    }

    /// <summary>
    /// Puts on their entities the components a scene held that no type described when it was
    /// loaded and one does now.
    /// </summary>
    /// <param name="world">The world, on loan to the system the load runs in.</param>
    /// <returns>How many components were put back.</returns>
    /// <remarks>
    /// A scene loaded before the scripts declaring its components were compiled keeps each of them
    /// as the JSON the file had, so a save writes them back, and they are not components until a
    /// type for them exists. The editor opens a level that way, since it compiles the project's
    /// scripts once the level is up, and without this its walls would be walls only in the file.
    /// An entity a field names is found by the id it had in its scene, as a load finds it.
    /// </remarks>
    public static int Revive(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var holding = world.All().Where(entity => world.Has<SceneKept>(entity)).ToArray();
        if (holding.Length == 0) return 0;

        var references = new SceneReferences();
        foreach (var entity in world.All())
        {
            if (world.TryGet<SceneId>(entity, out var id) && id.Value > 0) references.Spawned(id.Value, entity);
        }

        var revived = 0;
        foreach (var entity in holding)
        {
            if (world.GetRef<SceneKept>(entity).Value is not { } kept) continue;

            for (var i = kept.Components.Count - 1; i >= 0; i--)
            {
                var (name, raw) = kept.Components[i];
                if (ComponentSchemas.For(name) is not { Origin: not SchemaOrigin.Reflected, CanAdd: true } schema) continue;

                using var document = JsonDocument.Parse(raw);
                var value = SceneValue.Migrated(document.RootElement, schema);
                schema.Add(world, entity);
                SceneValue.ReadComponent(value, schema, world, entity, references);

                kept.Components.RemoveAt(i);
                revived++;
            }

            // Nothing left to keep, so nothing for the entity to carry.
            if (kept.IsEmpty) world.Remove<SceneKept>(entity);
        }

        return revived;
    }

    private static void Move(EcsWorld world, Entity entity, ComponentSchema from, ComponentSchema to)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer))
            SceneValue.WriteComponent(json, from, world, entity, InPlace.Instance);

        using var document = JsonDocument.Parse(buffer.WrittenMemory);

        // The old one first, whose remove hook gives back what it held, now that its values are
        // written down, and then the new one filled in from them.
        from.Remove(world, entity);
        to.Add(world, entity);
        SceneValue.ReadComponent(SceneValue.Migrated(document.RootElement, to), to, world, entity, InPlace.Instance);
    }

    /// <summary>References written as the entity or the asset itself, which stay in the same world.</summary>
    private sealed class InPlace : SceneReferences
    {
        public static readonly InPlace Instance = new();

        public override bool WriteEntity(Utf8JsonWriter json, Entity entity)
        {
            json.WriteStartObject();
            json.WriteNumber("bits", entity.Bits);
            json.WriteEndObject();
            return true;
        }

        public override object? ReadEntity(JsonElement json) =>
            json.ValueKind == JsonValueKind.Object && json.TryGetProperty("bits", out var bits) ? new Entity(bits.GetUInt64()) : null;

        public override bool WriteAsset(Utf8JsonWriter json, AssetHandle asset)
        {
            json.WriteStartObject();
            json.WriteNumber("key", asset.Key);
            json.WriteEndObject();
            return true;
        }

        public override object? ReadAsset(JsonElement json, string kind) =>
            json.ValueKind == JsonValueKind.Object && json.TryGetProperty("key", out var key) ? new AssetHandle(key.GetInt32()) : null;
    }
}
