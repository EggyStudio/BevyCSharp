namespace Bevy;

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
    /// For a list of values with fields of their own, which the inspector shows as its JSON, the
    /// Rust type path of its items, and <see langword="null"/> for every other field.
    /// </summary>
    /// <remarks>
    /// The description of Bevy's components the generator turns into typed wrappers describes the
    /// items by their own rows, under this name, so a wrapper reads and writes such a list as
    /// records.
    /// </remarks>
    internal string? ItemType { get; init; }

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
