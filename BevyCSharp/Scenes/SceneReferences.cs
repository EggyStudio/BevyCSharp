using System.Buffers;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bevy;

/// <summary>
/// How a scene names what a field refers to, another entity or an asset.
/// </summary>
/// <remarks>
/// <para>
/// An entity is written as a local id, <c>{ "entity": 4 }</c>, because the entity's own handle means
/// nothing in the next run. Which id stands for which entity is the scene's to say, so a writer
/// numbers what it is writing with <see cref="Name"/>, and a reader says which entity each id
/// spawned as with <see cref="Spawned"/>. A field that refers to an entity the scene did not number
/// is written as nothing, since the reference could not be followed after a reload.
/// </para>
/// <para>
/// An asset is written as the path it was loaded from, <c>{ "path": "models/ship.glb#Mesh0" }</c>,
/// and read back by loading it as the kind of asset the field holds. An asset built in memory has no
/// path and is written as nothing. Ids that survive a file being renamed are a later step, and they
/// arrive as a field of the same object.
/// </para>
/// </remarks>
public class SceneReferences
{
    private readonly Dictionary<Entity, int> _ids = [];
    private readonly Dictionary<int, Entity> _entities = [];

    /// <summary>
    /// References with no entities numbered, which writes assets by path and entities as nothing.
    /// </summary>
    public static SceneReferences Default { get; } = new();

    /// <summary>Numbers an entity that is being written, so a field referring to it can name it.</summary>
    public void Name(Entity entity, int id)
    {
        _ids[entity] = id;
        _entities[id] = entity;
    }

    /// <summary>Says which entity a local id was spawned as, so a field naming it can be read.</summary>
    public void Spawned(int id, Entity entity) => Name(entity, id);

    /// <summary>Writes a reference to an entity, reporting whether it could be named.</summary>
    public virtual bool WriteEntity(Utf8JsonWriter json, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (!_ids.TryGetValue(entity, out var id)) return false;

        json.WriteStartObject();
        json.WriteNumber("entity", id);
        json.WriteEndObject();
        return true;
    }

    /// <summary>Reads a reference to an entity, or nothing when the id names none.</summary>
    public virtual object? ReadEntity(JsonElement json) =>
        json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty("entity", out var id)
        && _entities.TryGetValue(id.GetInt32(), out var entity)
            ? entity
            : null;

    /// <summary>
    /// Whether a reference to a file with no id gives it one, by writing a sidecar beside it.
    /// </summary>
    /// <remarks>
    /// Off unless asked for, because only something that edits the project writes a sidecar
    /// (<see cref="AssetIds"/>), and a game saving a scene at runtime may be running from a folder
    /// it cannot write. The editor asks for it, so a scene saved there refers to every file by id.
    /// </remarks>
    public bool GiveIds { get; init; }

    /// <summary>
    /// Whether what is written is read back in this same run, as a component moved to the type a
    /// script reload made of it is, so a field marked <see cref="NotSavedAttribute"/> goes along
    /// rather than starting over.
    /// </summary>
    internal bool SameRun { get; init; }

    /// <summary>Writes a reference to an asset by its file, reporting whether it has one.</summary>
    public virtual bool WriteAsset(Utf8JsonWriter json, AssetHandle asset)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (AssetServer.PathOf(asset) is not { Length: > 0 } path) return false;

        WriteFile(json, path);
        return true;
    }

    /// <summary>Reads a reference to an asset by loading its file as the kind of asset given.</summary>
    public virtual object? ReadAsset(JsonElement json, string kind) =>
        ReadFile(json) is { Length: > 0 } file ? AssetServer.Load(kind, file) : null;

    /// <summary>
    /// Writes a reference to a file under the asset root as its id and its path, or as its path
    /// alone when it has no id.
    /// </summary>
    /// <remarks>
    /// The id is the file's, so a <c>#label</c> naming a part of it, such as one mesh of a model,
    /// stays in the path, and is put back on whichever path the id is found at.
    /// </remarks>
    public void WriteFile(Utf8JsonWriter json, string path)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrEmpty(path);

        json.WriteStartObject();
        if (IdOf(path) is var id and not 0)
            json.WriteString("uid", id.ToString("x16", CultureInfo.InvariantCulture));
        json.WriteString("path", path);
        json.WriteEndObject();
    }

    /// <summary>
    /// The path a file reference names, where the file holding its id is now, with the label the
    /// reference gave, or the path it recorded when no file holds the id.
    /// </summary>
    /// <remarks>A bare string is read as a path, which is how a scene wrote a file before ids.</remarks>
    public static string? ReadFile(JsonElement json)
    {
        if (json.ValueKind == JsonValueKind.String) return json.GetString();
        if (json.ValueKind != JsonValueKind.Object) return null;

        var recorded = json.TryGetProperty("path", out var path) ? path.GetString() : null;

        if (json.TryGetProperty("uid", out var uid)
            && ulong.TryParse(uid.GetString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id)
            && AssetIds.PathOf(id) is { } moved)
        {
            var label = recorded?.IndexOf('#') is { } hash and >= 0 ? recorded[hash..] : string.Empty;
            return moved + label;
        }

        return recorded;
    }

    /// <summary>The id of the file a path names, given one when asked to, or zero.</summary>
    private ulong IdOf(string path)
    {
        // A path from another source, such as an embedded one, is not a file under the root.
        if (path.Contains("://", StringComparison.Ordinal)) return 0;

        var hash = path.IndexOf('#');
        var file = hash < 0 ? path : path[..hash];

        try
        {
            return AssetIds.IdOf(file, create: GiveIds && File.Exists(Path.Combine(AssetIds.Root, file)));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // A sidecar that cannot be written leaves the reference by path, which still loads.
            return 0;
        }
    }
}
