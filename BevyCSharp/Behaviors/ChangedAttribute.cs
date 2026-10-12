namespace Bevy;

/// <summary>
/// Skips entities where none of the listed components changed since the previous frame.
/// </summary>
/// <remarks>
/// Unlike <see cref="WithAttribute"/>, this is a per-entity test against Bevy's change ticks,
/// so a method carrying it is iterated on the main thread rather than fanned out in parallel.
/// That is usually the better trade anyway, because the filter exists to make the set small.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ChangedAttribute : Attribute
{
    /// <summary>The components to watch.</summary>
    public Type[] Types { get; }

    /// <summary>Requires a change in any of <paramref name="types"/>.</summary>
    public ChangedAttribute(params Type[] types) => Types = types;
}
