namespace Bevy;

/// <summary>
/// A schedule of the game's own, by its name, which runs its systems when a system asks with
/// <see cref="BehaviorContext.TryRunSchedule"/>, as a schedule Bevy's <c>ScheduleLabel</c> names
/// does.
/// </summary>
/// <param name="Name">
/// What the schedule is called, unique among the game's schedules. One for each value of a state
/// carries the value in its name, as <c>OnReenter(InGame)</c>, which Bevy's custom_transitions keys
/// by the state's value.
/// </param>
/// <remarks>
/// Its systems are put in with <see cref="App.AddSystem(ScheduleLabel, SystemDescriptor)"/>, the
/// first making it, and run in an order Bevy picks, since a schedule of the game's own is in no
/// stage to order them within.
/// </remarks>
public readonly record struct ScheduleLabel(string Name);
