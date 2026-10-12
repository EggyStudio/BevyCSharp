namespace Bevy;

/// <summary>
/// Names what a field or a type was called before, so a scene or a data file written under that
/// name still loads.
/// </summary>
/// <remarks>
/// <para>
/// A scene file records each component under its type's full name and each value under its
/// field's name. Renaming either would otherwise leave every saved file holding a component this
/// build does not know, or a value no field reads, and the rename would quietly drop what was
/// saved. With the old name kept here, a load reads the value from where the file has it, and the
/// next save writes it under the new name.
/// </para>
/// <para>
/// On a type, a name given alone is taken to be in the type's own namespace, since a rename within
/// one is the common case. A name holding a dot is a full name, for a type moved to another
/// namespace. On a field, it is the field's own name, without the struct it sits in. A struct
/// field renamed this way takes every path under it along.
/// </para>
/// <para>
/// The attribute may be repeated for a type renamed more than once. A name some other type has
/// since taken goes to that type, not this one.
/// </para>
/// </remarks>
/// <param name="name">What it was called.</param>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Class,
    AllowMultiple = true)]
public sealed class FormerNameAttribute(string name) : Attribute
{
    /// <summary>What it was called.</summary>
    public string Name { get; } = name;
}
