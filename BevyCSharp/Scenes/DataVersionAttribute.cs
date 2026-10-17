namespace Bevy;

/// <summary>
/// Gives a type the version its files are written at, so a file written at an earlier one is
/// brought up to it before it is read.
/// </summary>
/// <remarks>
/// <para>
/// For a change a rename cannot say, such as a field split in two, a number whose unit changed, an
/// enum whose values were renumbered. The type declares a method of this shape, public or internal
/// so the registration the generator writes beside it can call it, and the generator finds it by
/// name. A load calls it with the version the file was written at and the type's JSON:
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
/// as the fields match and is not migrated. Saving it writes the later version back, with the
/// values this build did not read, so the newer build reads the file as its own. A value this
/// build changed is written in this build's shape under the later version, and the newer build
/// reads it as its own shape, so a change of meaning that an older build may still save over is
/// safer as a field under a new name.
/// </para>
/// </remarks>
/// <param name="version">The version files of the type are written at, from 1.</param>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public sealed class DataVersionAttribute(int version) : Attribute
{
    /// <summary>The version files of the type are written at.</summary>
    public int Version { get; } = version;
}
