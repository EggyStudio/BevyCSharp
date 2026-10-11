using Bevy.Interop;

namespace Bevy;

public sealed unsafe partial class App
{
    /// <summary>
    /// Puts a system in a schedule of the game's own, which runs it when a system asks with
    /// <see cref="BehaviorContext.TryRunSchedule"/>, as Bevy's <c>add_systems</c> with a label of
    /// the game's does.
    /// </summary>
    /// <param name="schedule">The schedule, made by the first system put in it.</param>
    /// <param name="descriptor">The system, its run condition kept.</param>
    /// <remarks>
    /// The systems of one schedule run in an order Bevy picks, as a stage's do, and one that asks
    /// to be ordered stops the app as it starts, since a schedule of the game's own is in no stage
    /// to order it within.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The app is already running.</exception>
    public App AddSystem(ScheduleLabel schedule, SystemDescriptor descriptor)
    {
        ArgumentException.ThrowIfNullOrEmpty(schedule.Name);
        ArgumentNullException.ThrowIfNull(descriptor);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsRunning)
            throw new InvalidOperationException(
                $"Cannot put system '{descriptor.Name}' in schedule '{schedule.Name}': the app is already running. "
                + "Register systems from a plugin's Build method or before calling Run.");

        descriptor.Source ??= SystemRegistrationSourceScope.Current;

        // The stage is only a label here, as for a state's transition, since the schedule runs
        // when it is asked to rather than in a frame's stage.
        var registration = new RegisteredSystem(this, descriptor, Stage.Startup);
        _systems.Add(registration);

        Native.Check(
            Native.bcs_app_add_schedule_system(
                _handle,
                schedule.Name,
                descriptor.Name,
                &RegisteredSystem.Trampoline,
                registration.UserData),
            $"putting system '{descriptor.Name}' in schedule '{schedule.Name}'");

        return this;
    }

    /// <summary>
    /// Runs <paramref name="run"/> in Bevy's transition of <typeparamref name="TState"/>, at
    /// <paramref name="step"/>, with the move the state made, in every frame it makes one.
    /// </summary>
    /// <param name="step">Among the exits, the moves or the entries of the state's transition.</param>
    /// <param name="run">What to do, handed the frame's context and the move.</param>
    /// <param name="name">What the system is called.</param>
    /// <remarks>
    /// <para>
    /// Bevy's <c>last_transition</c> piped into a system in one of the state's three sets of
    /// <c>StateTransition</c>, which Bevy's custom_transitions runs its own schedules from, so a
    /// game can say what runs at a transition beyond a value's entry and exit. It sees every move,
    /// one to the value the state holds among them, and the state's first value entered from none.
    /// </para>
    /// <para>
    /// It runs beside the state's own entry or exit systems, in an order Bevy picks, and a schedule
    /// of the game's own run from it with <see cref="BehaviorContext.TryRunSchedule"/> runs there
    /// too, inside the transition.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// The app is already running, or the state was never added.
    /// </exception>
    public App OnStateTransition<TState>(TransitionStep step, Action<BehaviorContext, StateTransitionEvent<TState>> run, string name)
        where TState : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsRunning)
            throw new InvalidOperationException(
                $"Cannot register system '{name}': the app is already running. "
                + "Register systems from a plugin's Build method or before calling Run.");

        var descriptor = new SystemDescriptor(world =>
        {
            NativeStateTransition made;
            if (Native.bcs_state_step_transition(&made) != NativeStatus.Ok) return;

            run(new BehaviorContext(world), new StateTransitionEvent<TState>(
                (made.Flags & 1) != 0 ? StateRegistry.FromInt<TState>(made.Exited) : null,
                (made.Flags & 2) != 0 ? StateRegistry.FromInt<TState>(made.Entered) : null));
        }, name);
        descriptor.Source ??= SystemRegistrationSourceScope.Current;

        var registration = new RegisteredSystem(this, descriptor, Stage.Startup);
        _systems.Add(registration);

        Native.Check(
            Native.bcs_state_add_step(
                _handle,
                StateRegistry.SlotForRegistration<TState>(),
                (int)step,
                &RegisteredSystem.Trampoline,
                registration.UserData),
            $"registering system '{name}' in the transition of {typeof(TState).Name}");

        return this;
    }
}
