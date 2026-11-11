namespace Bevy;

/// <summary>
/// Shown only while another field of the same component says so.
/// </summary>
/// <remarks>
/// <para>
/// What keeps a component with three modes from showing the settings of all three at once. With a
/// value it compares, so a field is shown only while a mode is one particular one. Without, it asks
/// whether the other field is on, which is the same question of a flag.
/// </para>
/// <para>
/// Several of them can sit on one field, and all of them have to hold. A condition naming a field
/// that is not there is ignored rather than obeyed, because a row that vanishes because an
/// attribute has a typo in it is worse than one that should not have been there.
/// </para>
/// </remarks>
/// <param name="field">The field that decides.</param>
/// <param name="value">
/// What it has to read as, or nothing for "is on". An enum is named by its name, and everything
/// else compares as it is written.
/// </param>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ShowIfAttribute(string field, object? value = null) : Attribute
{
    /// <summary>The field that decides.</summary>
    public string Field { get; } = field;

    /// <summary>What it has to read as, or nothing for "is on".</summary>
    public object? Value { get; } = value;

    /// <summary>Whether the sense is reversed, so the row shows while the other does not.</summary>
    public bool Not { get; init; }
}
