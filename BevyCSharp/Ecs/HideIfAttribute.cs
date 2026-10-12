namespace Bevy;

/// <summary>Hidden while another field says so, which is <see cref="ShowIfAttribute"/> reversed.</summary>
/// <remarks>
/// The same thing written the way somebody means it. Half of these conditions are naturally
/// phrased as "not while", and spelling that as a show with a flag on it reads backwards at the
/// point where it matters.
/// </remarks>
/// <param name="field">The field that decides.</param>
/// <param name="value">What it has to read as for the row to go away, or nothing for "is on".</param>
[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public sealed class HideIfAttribute(string field, object? value = null) : Attribute
{
    /// <summary>The field that decides.</summary>
    public string Field { get; } = field;

    /// <summary>What it has to read as for the row to go away.</summary>
    public object? Value { get; } = value;
}
