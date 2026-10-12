namespace Bevy;

/// <summary>
/// Calls the named methods when the field is changed.
/// </summary>
/// <remarks>
/// <para>
/// For a value something else is derived from: a radius a collider is rebuilt from, a count a pool
/// is resized to. Without it the derived thing is only right after whatever recomputes it happens
/// to run, which in an editor with nothing playing may be never.
/// </para>
/// <para>
/// The methods are named on the same component and take nothing. They are called after the write
/// has landed, so what they read is the new value.
/// </para>
/// </remarks>
/// <param name="methods">The methods to call.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class OnValueChangedAttribute(params string[] methods) : Attribute
{
    /// <summary>The methods to call.</summary>
    public string[] Methods { get; } = methods ?? [];
}
