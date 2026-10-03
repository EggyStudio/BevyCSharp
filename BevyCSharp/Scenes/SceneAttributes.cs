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

/// <summary>
/// Gives a type the version its files are written at, so a file written at an earlier one is
/// brought up to it before it is read.
/// </summary>
/// <remarks>
/// <para>
/// For a change a rename cannot say: a field split in two, a number whose unit changed, an enum
/// whose values were renumbered. The type declares a method of this shape, public or internal so
/// the registration the generator writes beside it can call it, and the generator finds it by name.
/// A load calls it with the version the file was written at and the type's JSON:
/// </para>
/// <code>
/// [Behavior, DataVersion(3)]
/// public partial struct Health
/// {
///     public float Current;
///     public float Most;
///
///     public static JsonObject Migrate(int from, JsonObject value)
///     {
///         if (from &lt; 2) // 2 kept the most in tenths
///             value["Most"] = (float?)value["Most"] * 10;
///
///         if (from &lt; 3 &amp;&amp; value["Max"] is { } max) // 3 split the most out of a pair
///         {
///             value["Most"] = max.DeepClone();
///             value.Remove("Max");
///         }
///
///         return value;
///     }
/// }
/// </code>
/// <para>
/// One method for every step, written as a run of checks on <c>from</c>, so a file several
/// versions behind passes through each change in order. A file written before the type had a
/// version is version 1, the version with no attribute, and a file records its version beside the
/// fields once it is past 1. A type at a later version with no method is warned about (BCS008),
/// since its old files would be read as the new shape.
/// </para>
/// <para>
/// A file from a later version than this build's, such as one a newer build wrote, is read as far
/// as the fields match. Saving it writes this build's version, with the values it did not read
/// kept, so the newer build migrates it again from there. A migration that does nothing to a value
/// already in the newer shape survives that.
/// </para>
/// </remarks>
/// <param name="version">The version files of the type are written at, from 1.</param>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public sealed class DataVersionAttribute(int version) : Attribute
{
    /// <summary>The version files of the type are written at.</summary>
    public int Version { get; } = version;
}
