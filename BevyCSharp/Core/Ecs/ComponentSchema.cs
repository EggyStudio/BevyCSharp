using System.Text.Json.Nodes;

namespace Bevy;

/// <summary>
/// The fields of one component type, and the id the engine knows it by.
/// </summary>
/// <remarks>
/// This turns <see cref="EcsWorld.ComponentsOf"/>, which answers in ids, into something an
/// inspector can draw. The generator emits one of these per <c>[Behavior]</c> struct that has
/// fields. Bevy's own components are described from Bevy's reflection once an app is running,
/// except the few with a mirror on this side, which are described by hand so the inspector writes
/// them through the mirror. <see cref="Origin"/> says which of the three a schema is.
/// </remarks>
public sealed class ComponentSchema
{
    private readonly Func<int> _id;

    private readonly Action<EcsWorld, Entity>? _add;
    private readonly Action<EcsWorld, Entity>? _remove;

    /// <summary>Describes one component type.</summary>
    /// <param name="name">The short name, which a tool puts on the header.</param>
    /// <param name="qualifiedName">The full name, as the engine reports it.</param>
    /// <param name="id">Resolves the engine's component id, registering the type if needed.</param>
    /// <param name="fields">The fields, in declaration order.</param>
    /// <param name="methods">What the component can be told to do, if anything.</param>
    /// <param name="add">Puts a default one on an entity, if that makes sense for this type.</param>
    /// <param name="remove">Takes it off again.</param>
    public ComponentSchema(
        string name,
        string qualifiedName,
        Func<int> id,
        IReadOnlyList<ComponentField> fields,
        IReadOnlyList<ComponentMethod>? methods = null,
        Action<EcsWorld, Entity>? add = null,
        Action<EcsWorld, Entity>? remove = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(fields);

        Name = name;
        QualifiedName = qualifiedName ?? name;
        _id = id;
        Fields = fields;
        Methods = methods ?? [];

        foreach (var field in fields) field.Schema ??= this;
        _add = add;
        _remove = remove;
    }

    /// <summary>What the component can be told to do.</summary>
    public IReadOnlyList<ComponentMethod> Methods { get; }

    /// <summary>Whether this type can be put on an entity that has none.</summary>
    public bool CanAdd => _add is not null;

    /// <summary>Puts a default one on an entity.</summary>
    /// <returns>Whether this type can be added at all.</returns>
    public bool Add(EcsWorld world, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (_add is null) return false;

        _add(world, entity);
        return true;
    }

    /// <summary>Takes it off again.</summary>
    /// <returns>Whether this type can be removed at all.</returns>
    public bool Remove(EcsWorld world, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (_remove is null) return false;

        _remove(world, entity);
        return true;
    }

    /// <summary>The short name.</summary>
    public string Name { get; }

    /// <summary>The full name, as the engine reports it.</summary>
    public string QualifiedName { get; }

    /// <summary>The fields, in declaration order.</summary>
    public IReadOnlyList<ComponentField> Fields { get; }

    /// <summary>Where the description came from.</summary>
    /// <remarks>
    /// <see cref="SchemaOrigin.Declared"/> unless set, since the generator writes every schema a
    /// project contributes and has no reason to name the default.
    /// </remarks>
    public SchemaOrigin Origin { get; init; }

    /// <summary>The full names the type had before, which an old file may still use.</summary>
    /// <remarks>
    /// Filled from <see cref="FormerNameAttribute"/>, with a name given alone placed in the type's
    /// own namespace. A file naming one of these is read as this type and written back under its
    /// current name.
    /// </remarks>
    public IReadOnlyList<string> FormerNames { get; init; } = [];

    /// <summary>The version a file of the type is written at, from <see cref="DataVersionAttribute"/>.</summary>
    /// <remarks>Zero for a type with none, which writes no version and reads every file as it is.</remarks>
    public int Version { get; init; }

    /// <summary>
    /// Brings the JSON of a file written at an earlier <see cref="Version"/> up to the current one,
    /// or nothing for a type with no migration.
    /// </summary>
    /// <remarks>The type's own <c>Migrate</c> method, which the generator finds and wraps.</remarks>
    public Func<int, JsonObject, JsonObject>? Migrate { get; init; }

    /// <summary>Whether a save game writes this component's fields, from <see cref="PersistAttribute"/>.</summary>
    /// <remarks>
    /// <see cref="SaveGame.Persisted"/> opts in a component this side did not declare, such as
    /// Bevy's transform.
    /// </remarks>
    public bool Persisted { get; init; }

    /// <summary>The engine's component id, resolved on demand.</summary>
    /// <remarks>Resolving registers the component with the world if it was not known yet.</remarks>
    public int Id => _id();

    /// <summary>Finds a field by name, or <see langword="null"/>.</summary>
    public ComponentField? Field(string name) =>
        Fields.FirstOrDefault(field => field.Name == name);

    /// <summary>Finds a method by name, or <see langword="null"/>.</summary>
    public ComponentMethod? Method(string name) =>
        Methods.FirstOrDefault(method => method.Name == name);

    /// <summary>Reads one field by name, or <see langword="null"/> when there is no such field.</summary>
    public object? Read(EcsWorld world, Entity entity, string field) =>
        Field(field)?.Read(world, entity);

    /// <summary>Writes one field by name, reporting whether it landed.</summary>
    public bool Write(EcsWorld world, Entity entity, string field, object value) =>
        Field(field)?.Write(world, entity, value) ?? false;

    /// <inheritdoc/>
    public override string ToString() => $"{Name} ({Fields.Count} fields)";
}
