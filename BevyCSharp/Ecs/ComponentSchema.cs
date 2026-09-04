using Bevy.Interop;

namespace Bevy;

/// <summary>
/// What kind of value a component's field holds, as far as a tool showing it cares.
/// </summary>
/// <remarks>
/// Deliberately short. A tool needs to know how to draw and edit a value, and nearly every
/// blittable field a component can carry falls into one of these. Anything that does not is
/// <see cref="Opaque"/>, which is a row with a type and no editor rather than a pretence.
/// </remarks>
public enum FieldKind
{
    /// <summary>Something with no editor: shown by name and type only.</summary>
    Opaque,

    /// <summary>A checkbox.</summary>
    Bool,

    /// <summary>A whole number, of any width.</summary>
    Int,

    /// <summary>A single-precision number.</summary>
    Float,

    /// <summary>A double-precision number.</summary>
    Double,

    /// <summary>Three numbers, drawn as one row.</summary>
    Vec3,

    /// <summary>A rotation, which a tool usually shows as Euler angles.</summary>
    Quat,

    /// <summary>A reference to another entity.</summary>
    Entity,

    /// <summary>One of a fixed set of names, which <see cref="ComponentField.Options"/> lists.</summary>
    Enum,
}

/// <summary>
/// One field of a component, and how to read and write it on a live entity.
/// </summary>
/// <remarks>
/// <para>
/// Accessors rather than byte offsets. An offset would have to be paired with a size, a
/// signedness and an endianness before anything could be read through it, and all three are
/// already known to the compiler that emitted this. A closure over the typed struct is smaller,
/// safe, and correct for a field the runtime lays out differently than expected.
/// </para>
/// <para>
/// Values cross this boundary boxed. That is the price of not knowing the type, and it is paid
/// once per row per frame in a tool that draws a few dozen rows, which is nothing.
/// </para>
/// </remarks>
public sealed class ComponentField
{
    private readonly Func<EcsWorld, Entity, object?> _read;
    private readonly Func<EcsWorld, Entity, object, bool>? _write;

    /// <summary>Describes one field.</summary>
    /// <param name="name">The field's name, which is what a tool labels its row with.</param>
    /// <param name="kind">How to draw and edit it.</param>
    /// <param name="type">The declared type, shown when there is no editor for it.</param>
    /// <param name="read">Reads the field from an entity, or <see langword="null"/> when absent.</param>
    /// <param name="write">Writes it back, or <see langword="null"/> when the field is read-only.</param>
    /// <param name="options">The names an <see cref="FieldKind.Enum"/> field can take.</param>
    public ComponentField(
        string name,
        FieldKind kind,
        string type,
        Func<EcsWorld, Entity, object?> read,
        Func<EcsWorld, Entity, object, bool>? write = null,
        IReadOnlyList<string>? options = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(read);

        Name = name;
        Kind = kind;
        Type = type ?? string.Empty;
        Options = options ?? [];
        _read = read;
        _write = write;
    }

    /// <summary>The field's name.</summary>
    public string Name { get; }

    /// <summary>How a tool should draw and edit it.</summary>
    public FieldKind Kind { get; }

    /// <summary>The declared type, as it was written in the source.</summary>
    public string Type { get; }

    /// <summary>The names an <see cref="FieldKind.Enum"/> field can take, in declaration order.</summary>
    public IReadOnlyList<string> Options { get; }

    /// <summary>Whether this field can be written as well as read.</summary>
    public bool IsWritable => _write is not null;

    /// <summary>Reads the field, or <see langword="null"/> when the entity does not carry it.</summary>
    public object? Read(EcsWorld world, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(world);
        return _read(world, entity);
    }

    /// <summary>Writes the field, reporting whether it landed.</summary>
    /// <remarks>
    /// Fails rather than throws when the entity does not carry the component or the field is
    /// read-only, because a tool writing a field it read a frame ago is a race it should survive.
    /// </remarks>
    public bool Write(EcsWorld world, Entity entity, object value)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(value);
        return _write is not null && _write(world, entity, value);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Type} {Name}";
}

/// <summary>
/// One thing a component can be told to do.
/// </summary>
/// <param name="Name">The method's name, which is what the button says.</param>
/// <param name="Run">Calls it on an entity's copy of the component and writes the result back.</param>
/// <remarks>
/// A method with no arguments on a component struct. Anything else has no obvious button: a
/// method that needs values needs a form, and a method that needs the world is a system rather
/// than something a person presses once.
/// </remarks>
public sealed record ComponentMethod(string Name, Action<EcsWorld, Entity> Run);

/// <summary>
/// The fields of one component type, and the id the engine knows it by.
/// </summary>
/// <remarks>
/// This is what turns <see cref="EcsWorld.ComponentsOf"/>, which answers in ids, into something
/// an inspector can draw. The generator emits one of these per <c>[Behavior]</c> struct that has
/// fields; a handful of Bevy's own components are described by hand, because a general answer for
/// those would need a byte-compatible mirror on this side and that is written per type anyway.
/// </remarks>
public sealed class ComponentSchema
{
    private readonly Func<int> _id;

    private readonly Action<EcsWorld, Entity>? _add;
    private readonly Action<EcsWorld, Entity>? _remove;

    /// <summary>Describes one component type.</summary>
    /// <param name="name">The short name, which is what a tool puts on the header.</param>
    /// <param name="qualifiedName">The full name, which is what the engine reports for it.</param>
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

    /// <summary>The engine's component id, resolved on demand.</summary>
    /// <remarks>Resolving registers the component with the world if it was not known yet.</remarks>
    public int Id => _id();

    /// <summary>Finds a field by name, or <see langword="null"/>.</summary>
    public ComponentField? Field(string name) =>
        Fields.FirstOrDefault(field => field.Name == name);

    /// <summary>Reads one field by name, or <see langword="null"/> when there is no such field.</summary>
    public object? Read(EcsWorld world, Entity entity, string field) =>
        Field(field)?.Read(world, entity);

    /// <summary>Writes one field by name, reporting whether it landed.</summary>
    public bool Write(EcsWorld world, Entity entity, string field, object value) =>
        Field(field)?.Write(world, entity, value) ?? false;

    /// <inheritdoc/>
    public override string ToString() => $"{Name} ({Fields.Count} fields)";
}

/// <summary>
/// Every component whose fields something on this side can describe.
/// </summary>
/// <remarks>
/// <para>
/// Filled by generated module initialisers, one per assembly, so a project that declares
/// behaviors contributes its schemas by existing rather than by registering them. Bevy's own
/// components are added here.
/// </para>
/// <para>
/// The id map is rebuilt whenever a new <see cref="App"/> invalidates component ids, since an id
/// belongs to a world. A schema whose type the current build has no component for, such as a
/// render component in a headless run, is skipped rather than allowed to throw.
/// </para>
/// </remarks>
public static class ComponentSchemas
{
    private static readonly object Gate = new();
    private static readonly List<ComponentSchema> Registered = [];
    private static Dictionary<int, ComponentSchema> _byId = [];
    private static int _generation = -1;

    static ComponentSchemas() => AddBuiltIn();

    /// <summary>Every registered schema, in registration order.</summary>
    public static IReadOnlyList<ComponentSchema> All
    {
        get { lock (Gate) return Registered.ToArray(); }
    }

    /// <summary>Registers a schema, replacing any earlier one for the same type.</summary>
    public static void Add(ComponentSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        lock (Gate)
        {
            Registered.RemoveAll(existing => existing.QualifiedName == schema.QualifiedName);
            Registered.Add(schema);
            _generation = -1;
        }
    }

    /// <summary>The schema for a component id, or <see langword="null"/> when none describes it.</summary>
    public static ComponentSchema? For(int componentId)
    {
        lock (Gate)
        {
            Refresh();
            return _byId.TryGetValue(componentId, out var schema) ? schema : null;
        }
    }

    /// <summary>The schema for a component's full name, or <see langword="null"/>.</summary>
    /// <remarks>
    /// The name route needs no world, which is what a tool listing what it could show before an
    /// app exists has to use. It also matches on the short name, because Bevy reports its own
    /// components by a path this side does not share.
    /// </remarks>
    public static ComponentSchema? For(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        lock (Gate)
        {
            return Registered.FirstOrDefault(schema => schema.QualifiedName == name)
                ?? Registered.FirstOrDefault(schema =>
                    name.EndsWith("::" + schema.Name, StringComparison.Ordinal)
                    || name == schema.Name);
        }
    }

    /// <summary>Rebuilds the id map when the world changed under it.</summary>
    private static void Refresh()
    {
        if (_generation == ComponentRegistry.Generation) return;

        var map = new Dictionary<int, ComponentSchema>();
        foreach (var schema in Registered)
        {
            // A schema for a component this build has none of, such as a render component in a
            // headless run, cannot resolve an id. That is a fact about the build rather than a
            // fault, so it drops out of the map instead of taking the rest of it down.
            try
            {
                map[schema.Id] = schema;
            }
            catch (BevyNativeException)
            {
            }
            catch (InvalidOperationException)
            {
                // No app yet, so no ids at all. Leave the map empty and try again next time.
                return;
            }
        }

        _byId = map;
        _generation = ComponentRegistry.Generation;
    }

    /// <summary>Describes the few Bevy components this side mirrors byte for byte.</summary>
    private static void AddBuiltIn()
    {
        Registered.Add(new ComponentSchema(
            "Transform",
            "Bevy.Transform",
            static () => ComponentType<Transform>.Id,
            [
                Mirror<Transform, Vec3>(
                    "Translation", FieldKind.Vec3, "Vec3",
                    static (in Transform t) => t.Translation,
                    static (ref Transform t, Vec3 v) => t.Translation = v),
                Mirror<Transform, Quat>(
                    "Rotation", FieldKind.Quat, "Quat",
                    static (in Transform t) => t.Rotation,
                    static (ref Transform t, Quat v) => t.Rotation = v),
                Mirror<Transform, Vec3>(
                    "Scale", FieldKind.Vec3, "Vec3",
                    static (in Transform t) => t.Scale,
                    static (ref Transform t, Vec3 v) => t.Scale = v),
            ],
            add: static (world, entity) => world.Add(entity, Transform.Identity),
            remove: static (world, entity) => world.Remove<Transform>(entity)));

        Registered.Add(new ComponentSchema(
            "Visibility",
            "Bevy.Visibility",
            static () => ComponentType<Visibility>.Id,
            [
                Mirror<Visibility, VisibilityMode>(
                    "Mode", FieldKind.Enum, "VisibilityMode",
                    static (in Visibility v) => v.Mode,
                    static (ref Visibility v, VisibilityMode mode) => v.Mode = mode,
                    Enum.GetNames<VisibilityMode>()),
            ],
            add: static (world, entity) => world.Add(entity, Visibility.Inherited),
            remove: static (world, entity) => world.Remove<Visibility>(entity)));
    }

    /// <summary>Reads a field out of a component value.</summary>
    private delegate TField Getter<TComponent, out TField>(in TComponent component)
        where TComponent : unmanaged;

    /// <summary>Writes a field into a component value.</summary>
    private delegate void Setter<TComponent, in TField>(ref TComponent component, TField value)
        where TComponent : unmanaged;

    /// <summary>Builds a field description from a typed getter and setter.</summary>
    /// <remarks>
    /// Written once here rather than at each call site, so the read-modify-write shape, which is
    /// what makes Bevy's change detection fire, is stated in a single place.
    /// </remarks>
    private static ComponentField Mirror<TComponent, TField>(
        string name,
        FieldKind kind,
        string type,
        Getter<TComponent, TField> get,
        Setter<TComponent, TField> set,
        IReadOnlyList<string>? options = null)
        where TComponent : unmanaged
        where TField : struct
        => new(
            name,
            kind,
            type,
            (world, entity) =>
                world.TryGet<TComponent>(entity, out var component) ? get(in component) : null,
            (world, entity, value) =>
            {
                if (!world.TryGet<TComponent>(entity, out var component)) return false;
                if (!TryCoerce<TField>(value, out var coerced)) return false;

                set(ref component, coerced);
                world.Set(entity, component);
                return true;
            },
            options);

    /// <summary>
    /// Turns a boxed value from a tool into the field's own type, reporting whether it fits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Called by generated field setters, and public for that reason rather than as a general
    /// utility. A tool holds a value it read from a text box or a slider, so a float field being
    /// handed a double, or an enum field an integer, is the ordinary case rather than a fault.
    /// </para>
    /// <para>
    /// Reports failure instead of throwing, because the value came from somewhere a person was
    /// typing and a half-typed number should leave the world alone rather than end the frame.
    /// </para>
    /// </remarks>
    public static bool TryCoerce<TField>(object value, out TField coerced) where TField : struct
    {
        if (value is TField exact)
        {
            coerced = exact;
            return true;
        }

        coerced = default;
        if (value is null) return false;

        var target = typeof(TField);

        try
        {
            if (target.IsEnum)
            {
                coerced = value is string name
                    ? (TField)Enum.Parse(target, name, ignoreCase: true)
                    : (TField)Enum.ToObject(target, Convert.ToInt64(value));
                return true;
            }

            if (value is not IConvertible) return false;

            coerced = (TField)Convert.ChangeType(value, target);
            return true;
        }
        catch (Exception error) when (
            error is FormatException or InvalidCastException or OverflowException
                or ArgumentException)
        {
            return false;
        }
    }
}
