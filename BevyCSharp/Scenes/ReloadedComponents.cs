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
    /// <exception cref="InvalidDataException">A migration threw, before any component was moved.</exception>
    public static int Carry(EcsWorld world, IReadOnlyList<ComponentSchema> before)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(before);

        return Move(world, Moves(world, before));
    }

    /// <summary>
    /// <see cref="Carry"/> and <see cref="Revive"/> together, every migration of both run before
    /// any component is moved or put back.
    /// </summary>
    /// <param name="world">The world, on loan to the system the reload runs in.</param>
    /// <param name="before">What <see cref="Before"/> returned before the new assembly was loaded.</param>
    /// <returns>How many components were moved and put back.</returns>
    /// <remarks>
    /// So a migration that throws leaves every entity as it was, and the reload that asked can be
    /// taken back whole, its generation unloaded and the running one's components on their entities
    /// with their values, where <see cref="Carry"/> followed by <see cref="Revive"/> has moved every
    /// component by the time a revived one's migration throws.
    /// </remarks>
    /// <exception cref="InvalidDataException">A migration threw, before anything was changed.</exception>
    public static int CarryAndRevive(EcsWorld world, IReadOnlyList<ComponentSchema> before)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(before);

        var moves = Moves(world, before);
        var revivals = Revivals(world);
        return Move(world, moves) + PutOn(world, revivals);
    }

    /// <summary>
    /// Puts on their entities the components a scene held that no type described when it was
    /// loaded and one has described since.
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
    /// <exception cref="InvalidDataException">A migration threw, before any component was put back.</exception>
    public static int Revive(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        return PutOn(world, Revivals(world));
    }

    /// <summary>A component to move, its value written down and brought up to the new type's version.</summary>
    private readonly record struct Moving(Entity Entity, ComponentSchema From, ComponentSchema To, JsonElement Value);

    /// <summary>A component a scene held to put on its entity, its place in what the entity keeps and its value brought up to its type's version.</summary>
    private readonly record struct Reviving(Entity Entity, SceneKept.Kept Kept, int Index, ComponentSchema Schema, JsonElement Value);

    /// <summary>The components of the types registered again, each written down and migrated.</summary>
    private static List<Moving> Moves(EcsWorld world, IReadOnlyList<ComponentSchema> before)
    {
        // An old schema whose name a new one has taken, and both ids, keyed by the old id an entity
        // carries. The new schema's id is asked for here, which registers the new type with Bevy.
        var moved = new Dictionary<int, (ComponentSchema From, ComponentSchema To)>();
        foreach (var from in before)
        {
            if (ComponentSchemas.For(from.QualifiedName) is not { } to || ReferenceEquals(to, from) || !to.CanAdd) continue;

            var old = from.Id;
            if (old != to.Id) moved[old] = (from, to);
        }

        var moves = new List<Moving>();
        if (moved.Count == 0) return moves;

        foreach (var entity in world.All())
        {
            foreach (var id in world.ComponentsOf(entity))
            {
                if (!moved.TryGetValue(id, out var pair)) continue;

                var buffer = new ArrayBufferWriter<byte>();
                using (var json = new Utf8JsonWriter(buffer))
                    SceneValue.WriteComponent(json, pair.From, world, entity, InPlace.Instance);

                using var document = JsonDocument.Parse(buffer.WrittenMemory);
                moves.Add(new Moving(entity, pair.From, pair.To, SceneValue.Migrated(document.RootElement, pair.To).Clone()));
            }
        }

        return moves;
    }

    private static int Move(EcsWorld world, List<Moving> moves)
    {
        // The old one first, whose remove hook gives back what it held, once its values are
        // written down, and then the new one filled in from them.
        foreach (var (entity, from, to, value) in moves)
        {
            from.Remove(world, entity);
            to.Add(world, entity);
            SceneValue.ReadComponent(value, to, world, entity, InPlace.Instance);
        }

        return moves.Count;
    }

    /// <summary>The components scenes held that a type describes, each brought up to its version.</summary>
    private static List<Reviving> Revivals(EcsWorld world)
    {
        var revivals = new List<Reviving>();
        foreach (var entity in world.All())
        {
            if (!world.Has<SceneKept>(entity) || world.GetRef<SceneKept>(entity).Value is not { } kept) continue;

            // From the last kept component down, so taking one out leaves the places of those still
            // to come.
            for (var i = kept.Components.Count - 1; i >= 0; i--)
            {
                var (name, raw) = kept.Components[i];
                if (ComponentSchemas.For(name) is not { Origin: not SchemaOrigin.Reflected, CanAdd: true } schema) continue;

                using var document = JsonDocument.Parse(raw);
                revivals.Add(new Reviving(entity, kept, i, schema, SceneValue.Migrated(document.RootElement, schema).Clone()));
            }
        }

        return revivals;
    }

    private static int PutOn(EcsWorld world, List<Reviving> revivals)
    {
        if (revivals.Count == 0) return 0;

        var references = new SceneReferences { SharesAssets = true };
        foreach (var entity in world.All())
        {
            if (world.TryGet<SceneId>(entity, out var id) && id.Value > 0) references.Spawned(id.Value, entity);
        }

        foreach (var (entity, kept, index, schema, value) in revivals)
        {
            schema.Add(world, entity);
            SceneValue.ReadComponent(value, schema, world, entity, references);
            kept.Components.RemoveAt(index);

            // Nothing left to keep, so nothing for the entity to carry.
            if (kept.IsEmpty) world.Remove<SceneKept>(entity);
        }

        return revivals.Count;
    }

    /// <summary>References written as the entity or the asset itself, which stay in the same world.</summary>
    private sealed class InPlace : SceneReferences
    {
        public static readonly InPlace Instance = new() { SameRun = true };

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
