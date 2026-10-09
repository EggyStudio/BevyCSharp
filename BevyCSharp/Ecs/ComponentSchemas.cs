using System.Globalization;
using Bevy.Interop;

namespace Bevy;

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
    /// <see cref="FormerNameAttribute"/>, or a path one of Bevy's components had before it moved,
    /// is tried last, so a type that has since taken that name wins over the one that gave it up.
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
                ?? Registered.FirstOrDefault(schema => schema.FormerNames.Contains(name))
                ?? _reflected.FirstOrDefault(schema => schema.FormerNames.Contains(name));
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
