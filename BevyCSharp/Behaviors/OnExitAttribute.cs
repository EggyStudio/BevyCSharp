namespace Bevy;

/// <summary>
/// Runs a method once, when a state machine leaves a value.
/// </summary>
/// <remarks>
/// The counterpart to <see cref="OnEnterAttribute"/>, and where what it built is taken away.
/// Runs before the new state is entered, so the two do not overlap.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class OnExitAttribute : Attribute
{
    /// <summary>The value being left, as an enum member.</summary>
    public object State { get; }

    /// <summary>Runs the method when <paramref name="state"/> is left.</summary>
    public OnExitAttribute(object state) => State = state;
}
