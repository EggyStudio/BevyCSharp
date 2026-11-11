namespace Bevy;

/// <summary>
/// Runs a method once, when a state machine enters a value.
/// </summary>
/// <remarks>
/// <para>
/// Where a level is built, a menu is shown, or a screen's entities are spawned. Unlike a stage
/// attribute this is not per frame, because it runs once per transition, so anything it creates
/// lasts until <see cref="OnExitAttribute"/> takes it away.
/// </para>
/// <para>
/// The state must have been added with <see cref="App.AddState{TState}"/> before the app runs,
/// because the schedule is keyed on the value and has to exist by then.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [OnEnter(Screen.Playing)]
/// public static void BuildLevel(BehaviorContext ctx) { }
///
/// [OnExit(Screen.Playing)]
/// public static void TearDown(BehaviorContext ctx) { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class OnEnterAttribute : Attribute
{
    /// <summary>The value being entered, as an enum member.</summary>
    public object State { get; }

    /// <summary>Runs the method when <paramref name="state"/> is entered.</summary>
    public OnEnterAttribute(object state) => State = state;
}
