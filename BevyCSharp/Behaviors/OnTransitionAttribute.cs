namespace Bevy;

/// <summary>
/// Runs a method once, when a state machine moves from one value to a particular other.
/// </summary>
/// <remarks>
/// <para>
/// Where what happens depends on where the state came from as well as where it went, as Bevy's
/// <c>OnTransition</c> has it. Entering play from the menu builds the level, and entering it from
/// the pause does not, so the build is a move from the menu to play rather than an entry to play.
/// </para>
/// <para>
/// It runs after <see cref="OnExitAttribute"/> methods of the value left and before
/// <see cref="OnEnterAttribute"/> methods of the value entered. Both values are of the same
/// enum, and a method naming two enums is reported when it is compiled (BCS009).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [OnTransition(Screen.Menu, Screen.Playing)]
/// public static void BuildLevel(BehaviorContext ctx) { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class OnTransitionAttribute : Attribute
{
    /// <summary>The value being left, as an enum member.</summary>
    public object From { get; }

    /// <summary>The value being entered, as an enum member.</summary>
    public object To { get; }

    /// <summary>Runs the method when the state moves from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public OnTransitionAttribute(object from, object to)
    {
        From = from;
        To = to;
    }
}
