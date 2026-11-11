namespace Bevy;

/// <summary>
/// The fields of a struct or a class that a list or a map holds as its items, read and written on
/// one item at a time.
/// </summary>
/// <remarks>
/// <para>
/// A list of loot entries is a list of values with fields of their own, which no single editor or
/// JSON form covers. The generator describes the item type the way it describes a data asset, as
/// <see cref="ComponentField"/> rows over a box holding one value, so an item is drawn as rows, and
/// written and read as an object, by the same code a component's fields go through.
/// </para>
/// <para>
/// <see cref="Bind"/> puts an item in a box of its own, a copy where the item is a class, so writing
/// its fields never changes the item a list already holds. What comes back from
/// <see cref="Bound.Value"/> is the changed item, which goes back into the list as a whole, so an
/// edit is one write of the list as every other list edit is.
/// </para>
/// </remarks>
/// <param name="type">The item type's name, as a tool shows it.</param>
/// <param name="create">Makes an item at its defaults, as a new item added to the list.</param>
/// <param name="bind">Binds the fields to a copy of an item.</param>
public sealed class ItemFields(string type, Func<object> create, Func<object, ItemFields.Bound> bind)
{
    /// <summary>The item type's name, as a tool shows it.</summary>
    public string Type { get; } = type;

    /// <summary>An item at its defaults.</summary>
    public object Create() => create();

    /// <summary>The fields of a copy of an item, and the copy as the fields leave it.</summary>
    public Bound Bind(object item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return bind(item);
    }

    /// <summary>An item's fields, bound to a copy of it.</summary>
    /// <param name="Schema">The fields, which ignore the world and entity they are given.</param>
    /// <param name="Value">The copy as its fields have left it.</param>
    public sealed record Bound(ComponentSchema Schema, Func<object> Value);
}
