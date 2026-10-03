using System.Globalization;
using System.Text.Json.Nodes;
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
    /// <summary>Something with no editor, shown by name and type only.</summary>
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

    /// <summary>A reference to a loaded asset: a mesh, an image, a material, a sound.</summary>
    Asset,

    /// <summary>One of a fixed set of names, which <see cref="ComponentField.Options"/> lists.</summary>
    Enum,

    /// <summary>
    /// Any number of a fixed set of names at once.
    /// </summary>
    /// <remarks>
    /// An enum whose values are bits. It is a different kind rather than a flag on
    /// <see cref="Enum"/> because it is drawn differently, as one row that offers a choice against
    /// several that are each on or off.
    /// </remarks>
    Flags,

    /// <summary>Text, which only a component of Bevy's reflected through it can hold.</summary>
    /// <remarks>
    /// A C# component is unmanaged and so has no string to hold, but a reflected one of Bevy's can,
    /// and a data asset will.
    /// </remarks>
    String,

    /// <summary>Two numbers, drawn as one row.</summary>
    Vec2,

    /// <summary>Four numbers, drawn as one row.</summary>
    Vec4,

    /// <summary>
    /// A <see cref="Bevy.Color"/>, linear, drawn as a swatch.
    /// </summary>
    /// <remarks>
    /// One of Bevy's <c>Color</c>, <c>LinearRgba</c> or <c>Srgba</c> fields is this kind too, read
    /// and written as linear whatever space it holds.
    /// </remarks>
    Color,

    /// <summary>
    /// Any number of values of one kind, which <see cref="ComponentField.ElementKind"/> names.
    /// </summary>
    /// <remarks>
    /// Read as a <see cref="ListValue"/> of the items and written back as one, so an edit to one
    /// item is a write of the whole list. A C# component holds one as an inline list
    /// (<see cref="InlineList8{T}"/> and its siblings).
    /// </remarks>
    List,

    /// <summary>
    /// Entries found by key, the keys of the kind <see cref="ComponentField.KeyKind"/> names and the
    /// values of the kind <see cref="ComponentField.ElementKind"/> does.
    /// </summary>
    /// <remarks>
    /// Read as a <see cref="MapValue"/> and written back as one. A C# component holds one as an
    /// <see cref="EcsMap{TKey, TValue}"/>.
    /// </remarks>
    Map,

    /// <summary>
    /// A <see cref="DataRef{T}"/>, a reference to a data asset, whose type
    /// <see cref="FieldHints.Asset"/> names.
    /// </summary>
    /// <remarks>
    /// Read as the reference, boxed, which says its file's id through <see cref="IDataRef"/>, and
    /// written from a reference or from a file's id.
    /// </remarks>
    Data,

    /// <summary>
    /// A value made of fields of its own, a struct or a class, as the items of a list can be. Its
    /// fields are described by <see cref="ComponentField.Items"/>.
    /// </summary>
    /// <remarks>
    /// Only as the kind of an item or a map's value. A struct that is a field of a component is
    /// taken apart into fields of the component's own instead, which a tool draws in a fold.
    /// </remarks>
    Struct,
}

/// <summary>
/// Where a component's description came from, which says what kind of component it is.
/// </summary>
/// <remarks>
/// A tool sometimes has to tell a game's own components from the engine's: an icon that marks an
/// entity as scripted, or a file that saves what a project wrote and leaves the engine's own state
/// to the engine. The schema knows which it is, so it says so rather than each tool
/// guessing from a name.
/// </remarks>
public enum SchemaOrigin
{
    /// <summary>A C# component, described by the generator.</summary>
    Declared,

    /// <summary>One of Bevy's components with a mirror on this side, described by hand.</summary>
    Mirrored,

    /// <summary>One of Bevy's components, described from Bevy's own reflection.</summary>
    Reflected,
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
    /// <param name="name">The field's name, which a tool labels its row with.</param>
    /// <param name="kind">How to draw and edit it.</param>
    /// <param name="type">The declared type, shown when there is no editor for it.</param>
    /// <param name="read">Reads the field from an entity, or <see langword="null"/> when absent.</param>
    /// <param name="write">Writes it back, or <see langword="null"/> when the field is read-only.</param>
    /// <param name="options">The names an <see cref="FieldKind.Enum"/> field can take.</param>
    /// <param name="hints">What the field's attributes asked for.</param>
    public ComponentField(
        string name,
        FieldKind kind,
        string type,
        Func<EcsWorld, Entity, object?> read,
        Func<EcsWorld, Entity, object, bool>? write = null,
        IReadOnlyList<string>? options = null,
        FieldHints? hints = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(read);

        Name = name;
        Kind = kind;
        Type = type ?? string.Empty;
        Options = options ?? [];
        Hints = hints ?? FieldHints.None;
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

    /// <summary>What the field's attributes asked for.</summary>
    public FieldHints Hints { get; }

    /// <summary>
    /// The kind of each item, for a <see cref="FieldKind.List"/> field, and
    /// <see cref="FieldKind.Opaque"/> for every other.
    /// </summary>
    /// <remarks>
    /// An item of a list of enums takes its names from <see cref="Options"/>, and the field's hints
    /// (a range, a unit) apply to every item, as an attribute on the list is read to mean.
    /// </remarks>
    public FieldKind ElementKind { get; init; }

    /// <summary>
    /// The kind of each key, for a <see cref="FieldKind.Map"/> field, and
    /// <see cref="FieldKind.Opaque"/> for every other.
    /// </summary>
    public FieldKind KeyKind { get; init; }

    /// <summary>
    /// The fields of each item, for a list or a map whose <see cref="ElementKind"/> is
    /// <see cref="FieldKind.Struct"/>, and nothing for every other.
    /// </summary>
    public ItemFields? Items { get; init; }

    /// <summary>
    /// Whether the row is a view of other state rather than state of its own.
    /// </summary>
    /// <remarks>
    /// True for a property, which reads and writes through itself and usually stands for one of
    /// the fields beside it. A tool drawing rows does not care, but a tool that copies a component,
    /// writes one to a file, or puts one back after taking it off has to write the state and not
    /// the views of it, or a setter that changes what it was derived from runs over the value that
    /// was just restored.
    /// </remarks>
    public bool Derived { get; init; }

    /// <summary>
    /// Bevy's reflect path to the field, for a field of a component described from Bevy's
    /// reflection, and <see langword="null"/> for every other.
    /// </summary>
    /// <remarks>
    /// What the field reads and writes through, kept so the description of Bevy's components the
    /// generator turns into typed wrappers names each field the way its row does.
    /// </remarks>
    internal string? ReflectPath { get; init; }

    /// <summary>
    /// The component this field belongs to, once one has claimed it.
    /// </summary>
    /// <remarks>
    /// Set by the schema that lists it rather than passed in, because a field is written before
    /// the component that holds it exists. What it is for is everything that has to look sideways
    /// from a field: the methods to call when it changes, the field a condition names, the values
    /// to put back when it is reset.
    /// </remarks>
    public ComponentSchema? Schema { get; internal set; }

    /// <summary>What to call it on screen, which is its label when it has one.</summary>
    public string Title => Hints.Label is { Length: > 0 } label ? label : Name;

    /// <summary>Whether this field can be written as well as read.</summary>
    /// <remarks>
    /// A field the attributes marked as read only answers no, whatever the generator emitted,
    /// because it is the same question from a tool's point of view, and answering it in one place
    /// means every drawer honors the attribute without knowing about it.
    /// </remarks>
    public bool IsWritable => _write is not null && !Hints.ReadOnly;

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
/// <param name="Name">The method's name, which the button shows.</param>
/// <param name="Run">Calls it on an entity's copy of the component and writes the result back.</param>
/// <remarks>
/// A method with no arguments on a component struct. Anything else has no obvious button, because
/// a method that needs values needs a form, and a method that needs the world is a system rather
/// than something a person presses once.
/// </remarks>
public sealed record ComponentMethod(string Name, Action<EcsWorld, Entity> Run)
{
    /// <summary>What the method's attributes asked for.</summary>
    public MethodHints Hints { get; init; } = MethodHints.None;

    /// <summary>What the button says, which is its label when it has one.</summary>
    public string Title => Hints.Label is { Length: > 0 } label ? label : Name;
}

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

/// <summary>
/// Every component whose fields something on this side can describe.
/// </summary>
/// <remarks>
/// <para>
/// Filled by generated module initializers, one per assembly, so a project that declares
/// behaviors contributes its schemas by existing rather than by registering them. The Bevy
/// components with a mirror are added here by hand.
/// </para>
/// <para>
/// Every other component Bevy reflects is described from Bevy's registry, the first time this is
/// asked while a system has the world on loan. Those schemas belong to one app, because their ids
/// and the types a build registers are the app's, so a new <see cref="App"/> drops them and the
/// next question inside a system describes the new one. Until then they are absent, and a tool
/// asking before the first frame sees only the C# components and the mirrors.
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

    // Kept apart from Registered so that Add, which replaces by qualified name, and the reflected
    // set, which is replaced whole for each app, never evict each other.
    private static List<ComponentSchema> _reflected = [];
    private static int _reflectedGeneration = -1;

    static ComponentSchemas() => AddBuiltIn();

    /// <summary>
    /// Every schema: the registered ones in registration order, then the reflected ones by type
    /// path.
    /// </summary>
    public static IReadOnlyList<ComponentSchema> All
    {
        get
        {
            lock (Gate)
            {
                Reflect();
                return [.. Registered, .. _reflected];
            }
        }
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
            Reflect();
            Refresh();
            return _byId.TryGetValue(componentId, out var schema) ? schema : null;
        }
    }

    /// <summary>The schema for a component's full name, or <see langword="null"/>.</summary>
    /// <remarks>
    /// The name route needs no world, so a tool listing what it could show before an app exists has
    /// to use it. It also matches on the short name, because Bevy reports its own components by a
    /// path this side does not share. A name a type had before, kept on it with
    /// <see cref="FormerNameAttribute"/>, is tried last, so a type that has since taken that name
    /// wins over the one that gave it up.
    /// </remarks>
    public static ComponentSchema? For(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        lock (Gate)
        {
            Reflect();
            return Registered.FirstOrDefault(schema => schema.QualifiedName == name)
                ?? _reflected.FirstOrDefault(schema => schema.QualifiedName == name)
                ?? Registered.FirstOrDefault(schema =>
                    name.EndsWith("::" + schema.Name, StringComparison.Ordinal)
                    || name == schema.Name)
                ?? _reflected.FirstOrDefault(schema => name == schema.Name)
                ?? Registered.FirstOrDefault(schema => schema.FormerNames.Contains(name));
        }
    }

    /// <summary>Rebuilds the id map when the world changed under it.</summary>
    private static void Refresh()
    {
        if (_generation == ComponentRegistry.Generation) return;

        var map = new Dictionary<int, ComponentSchema>();
        foreach (var schema in Registered.Concat(_reflected))
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

    /// <summary>
    /// Describes the running app's reflected components, once per app, when a world is on loan.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Asked on every lookup, and cheap once answered, because the generation matches. Before that,
    /// and outside a system, the bridge answers that no world is lent, and the attempt is made again
    /// on the next lookup.
    /// </para>
    /// <para>
    /// The previous app's schemas are dropped before the attempt rather than after it succeeds.
    /// Their ids belong to a world that is gone, and a lookup between the two apps would otherwise
    /// map a new component to an old description.
    /// </para>
    /// </remarks>
    private static void Reflect()
    {
        var generation = ComponentRegistry.Generation;

        // No app has been created, so the bridge may not even be loaded, and there is nothing to
        // describe anyway.
        if (generation == 0 || _reflectedGeneration == generation) return;

        if (_reflected.Count > 0)
        {
            _reflected = [];
            _generation = -1;
        }

        string? description;
        try
        {
            description = EcsWorld.DescribeReflected();
        }
        catch (BevyNativeException)
        {
            return;
        }

        if (description is null) return;

        // The ids the mirrors resolve to stay theirs, so a light's transform is still written
        // through the Transform mirror rather than through reflection.
        var taken = new HashSet<int>();
        foreach (var schema in Registered)
        {
            try
            {
                taken.Add(schema.Id);
            }
            catch (Exception error) when (error is BevyNativeException or InvalidOperationException)
            {
            }
        }

        _reflected = ReflectedSchemas.Build(
            description,
            taken,
            Registered.Select(schema => schema.Name).ToHashSet(StringComparer.Ordinal));
        _reflectedGeneration = generation;
        _generation = -1;
    }

    /// <summary>What a value of three numbers drawn beside each other asked for.</summary>
    private static readonly FieldHints Across = new(Inline: true);

    /// <summary>Describes the few Bevy components this side mirrors byte for byte.</summary>
    private static void AddBuiltIn()
    {
        Registered.Add(new ComponentSchema(
            "Transform",
            "Bevy.Transform",
            static () => ComponentType<Transform>.Id,
            [
                // Across the line rather than down it. A transform is the one component every
                // entity has and the one somebody looks at most, and nine numbers down a panel is
                // most of the room a panel has for one thing that everybody can already read in
                // three lines.
                Mirror<Transform, Vec3>(
                    "Translation", FieldKind.Vec3, "Vec3",
                    static (in Transform t) => t.Translation,
                    static (ref Transform t, Vec3 v) => t.Translation = v,
                    hints: Across),
                Mirror<Transform, Quat>(
                    "Rotation", FieldKind.Quat, "Quat",
                    static (in Transform t) => t.Rotation,
                    static (ref Transform t, Quat v) => t.Rotation = v,
                    hints: Across),
                Mirror<Transform, Vec3>(
                    "Scale", FieldKind.Vec3, "Vec3",
                    static (in Transform t) => t.Scale,
                    static (ref Transform t, Vec3 v) => t.Scale = v,
                    hints: Across),
            ],
            add: static (world, entity) => world.Add(entity, Transform.Identity),
            remove: static (world, entity) => world.Remove<Transform>(entity))
        {
            Origin = SchemaOrigin.Mirrored,
        });

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
            remove: static (world, entity) => world.Remove<Visibility>(entity))
        {
            Origin = SchemaOrigin.Mirrored,
        });

        // Written as sixteen hex digits, as a file's id is, since a 64-bit number is past what a
        // JSON reader is sure to keep exact.
        Registered.Add(new ComponentSchema(
            "SaveId",
            "Bevy.SaveId",
            static () => ComponentType<SaveId>.Id,
            [
                new ComponentField(
                    "Id",
                    FieldKind.String,
                    "string",
                    static (world, entity) => world.TryGet<SaveId>(entity, out var id) ? id.ToString() : null,
                    static (world, entity, value) =>
                    {
                        if (!world.Has<SaveId>(entity) || value is not string text || SaveId.Parse(text) is not { } id)
                            return false;

                        world.Set(entity, id);
                        return true;
                    },
                    hints: new FieldHints(Tooltip: "What a save game finds this entity by, unique in the game.")),
            ],
            add: static (world, entity) => world.Add(entity, SaveId.New()),
            remove: static (world, entity) => world.Remove<SaveId>(entity)));
    }

    /// <summary>Reads a field out of a component value.</summary>
    private delegate TField Getter<TComponent, out TField>(in TComponent component)
        where TComponent : unmanaged;

    /// <summary>Writes a field into a component value.</summary>
    private delegate void Setter<TComponent, in TField>(ref TComponent component, TField value)
        where TComponent : unmanaged;

    /// <summary>Builds a field description from a typed getter and setter.</summary>
    /// <remarks>
    /// Written once here rather than at each call site, so the read-modify-write shape, which makes
    /// Bevy's change detection fire, is stated in a single place.
    /// </remarks>
    private static ComponentField Mirror<TComponent, TField>(
        string name,
        FieldKind kind,
        string type,
        Getter<TComponent, TField> get,
        Setter<TComponent, TField> set,
        IReadOnlyList<string>? options = null,
        FieldHints? hints = null)
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
            options,
            hints);

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
        // Arithmetic, not text somebody is reading, so the machine's regional settings have no
        // business in it. Left to the current culture, the same saved file reads differently on
        // two machines.
        var plain = CultureInfo.InvariantCulture;

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
                    : (TField)Enum.ToObject(target, Convert.ToInt64(value, plain));
                return true;
            }

            if (value is not IConvertible) return false;

            coerced = (TField)Convert.ChangeType(value, target, plain);
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
