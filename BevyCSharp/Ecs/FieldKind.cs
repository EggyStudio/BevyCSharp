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
