namespace Bevy;

/// <summary>
/// Where in Bevy's transition of a state a system <see cref="App.OnStateTransition{TState}"/> adds
/// runs, Bevy's three sets of a state's transition schedules.
/// </summary>
public enum TransitionStep
{
    /// <summary>
    /// Among the exits, Bevy's <c>ExitSchedules</c>, after a state that depends on this one has
    /// left its value and before the moves.
    /// </summary>
    Exit,

    /// <summary>Among the moves, Bevy's <c>TransitionSchedules</c>, between the exits and the entries.</summary>
    Transition,

    /// <summary>
    /// Among the entries, Bevy's <c>EnterSchedules</c>, after the moves and before a state that
    /// depends on this one enters its value.
    /// </summary>
    Enter,
}
