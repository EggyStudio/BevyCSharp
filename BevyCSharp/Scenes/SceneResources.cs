using System.Text.Json;

namespace Bevy;

/// <summary>
/// The resources of a game's own a scene file writes with its entities, the types marked
/// <see cref="SceneResourceAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// Bevy writes a world's resources into a scene beside its entities. A resource here is a value the
/// app's <see cref="World"/> holds by type, so the generator registers each marked type with the
/// fields a data asset of it would have, read and written through a box holding the value, and
/// <see cref="SceneFile"/> writes the ones the world holds as it saves and puts them in as it loads.
/// </para>
/// <para>
/// A field is written by its kind as a component's is, its <see cref="FormerNameAttribute"/> read
/// and its <see cref="NotSavedAttribute"/> left out, and a type's
/// <see cref="DataVersionAttribute"/> brings an older scene's resource up to the version first.
/// </para>
/// </remarks>
public static class SceneResources
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, Kind> Kinds = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Kind> Former = new(StringComparer.Ordinal);

    /// <summary>
    /// A world standing in for the one a component's field reads through, which a resource's fields
    /// ignore, since they read the box.
    /// </summary>
    private static readonly EcsWorld Detached = new();

    /// <summary>One registered resource type.</summary>
    /// <param name="Name">Its full name, as the scene records it.</param>
    /// <param name="Create">Makes a box holding a value at the type's defaults.</param>
    /// <param name="Held">A box holding the world's value, or nothing where the world has none.</param>
    /// <param name="Bind">Describes the fields of one box.</param>
    /// <param name="Insert">Puts the value a box holds into the world, over any it held.</param>
    private sealed record Kind(
        string Name,
        Func<object> Create,
        Func<World, object?> Held,
        Func<object, ComponentSchema> Bind,
        Action<World, object> Insert);

    /// <summary>Registers a resource type a scene writes. Called by generated module initializers.</summary>
    /// <param name="name">The type's full name, which scenes record.</param>
    /// <param name="create">Makes a value at its defaults.</param>
    /// <param name="fields">Describes the fields of a boxed value, reading and writing through the box.</param>
    /// <param name="formerNames">The full names the type had before, which an old scene may still record.</param>
    /// <param name="version">The version its values are written at, or zero for none.</param>
    /// <param name="migrate">Brings an older scene's fields up to that version, or nothing.</param>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Register<T>(
        string name,
        Func<T> create,
        Func<DataBox<T>, IReadOnlyList<ComponentField>> fields,
        IReadOnlyList<string>? formerNames = null,
        int version = 0,
        Func<int, System.Text.Json.Nodes.JsonObject, System.Text.Json.Nodes.JsonObject>? migrate = null)
        where T : notnull
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var shortName = name[(name.LastIndexOf('.') + 1)..];
        var kind = new Kind(
            name,
            () => new DataBox<T> { Value = create() },
            world => world.TryGetResource<T>(out var value) ? new DataBox<T> { Value = value } : null,
            box => new ComponentSchema(shortName, name, static () => -1, fields((DataBox<T>)box))
            {
                Version = version,
                Migrate = migrate,
            },
            (world, box) => world.SetResource(((DataBox<T>)box).Value));

        lock (Gate)
        {
            Kinds[name] = kind;
            foreach (var former in formerNames ?? []) Former[former] = kind;
        }
    }

    /// <summary>The full names of every registered resource type.</summary>
    public static IReadOnlyList<string> Types
    {
        get
        {
            lock (Gate) return [.. Kinds.Keys.Order(StringComparer.Ordinal)];
        }
    }

    /// <summary>
    /// Writes the registered resources <paramref name="world"/> holds as a <c>worldResources</c> array,
    /// or nothing where it holds none, so a scene with none reads as it did before.
    /// </summary>
    internal static void Write(Utf8JsonWriter json, World? world)
    {
        if (world is null) return;

        List<Kind> kinds;
        lock (Gate) kinds = [.. Kinds.Values.OrderBy(kind => kind.Name, StringComparer.Ordinal)];

        var written = false;
        foreach (var kind in kinds)
        {
            if (kind.Held(world) is not { } box) continue;

            if (!written)
            {
                json.WriteStartArray("worldResources");
                written = true;
            }

            json.WriteStartObject();
            json.WriteString("type", kind.Name);
            json.WritePropertyName("fields");
            SceneValue.WriteComponent(json, kind.Bind(box), Detached, Entity.None);
            json.WriteEndObject();
        }

        if (written) json.WriteEndArray();
    }

    /// <summary>
    /// Puts the resources a scene's <c>worldResources</c> array holds into <paramref name="world"/>,
    /// each over the one it held, a field the scene leaves out kept as the world had it.
    /// </summary>
    /// <returns>How many were put in. One of a type this build does not know is skipped.</returns>
    internal static int Read(JsonElement scene, World? world)
    {
        if (world is null || !scene.TryGetProperty("worldResources", out var resources) || resources.ValueKind != JsonValueKind.Array)
            return 0;

        var read = 0;
        foreach (var resource in resources.EnumerateArray())
        {
            var name = resource.TryGetProperty("type", out var type) ? type.GetString() : null;
            Kind? kind;
            lock (Gate) kind = name is null ? null : Kinds.GetValueOrDefault(name) ?? Former.GetValueOrDefault(name);
            if (kind is null) continue;

            var box = kind.Held(world) ?? kind.Create();
            var schema = kind.Bind(box);
            if (resource.TryGetProperty("fields", out var fields))
                SceneValue.ReadComponent(SceneValue.Migrated(fields, schema), schema, Detached, Entity.None);

            kind.Insert(world, box);
            read++;
        }

        return read;
    }
}
