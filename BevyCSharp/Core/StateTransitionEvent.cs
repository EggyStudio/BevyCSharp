namespace Bevy;

/// <summary>A state moving from one value to another, Bevy's <c>StateTransitionEvent</c>.</summary>
/// <typeparam name="TState">The state's enum.</typeparam>
/// <param name="Exited">The value it left, or null where it did not exist before, as a state as it is first added or a sub-state as its parent enters its value.</param>
/// <param name="Entered">The value it entered, or null where it stopped existing.</param>
/// <remarks>
/// <para>
/// Read with <c>ctx.Read&lt;StateTransitionEvent&lt;Screen&gt;&gt;()</c>, one for each transition,
/// as Bevy's <c>log_transitions</c> reads them. A value set again is a transition too, an identity
/// one with both the same, which <c>[OnExit]</c> and <c>[OnEnter]</c> of the value run on again, as
/// Bevy's <c>NextState::set</c> has it, and <c>[OnTransition]</c> from the value to itself runs on
/// alone, so a level started again is torn down and built.
/// </para>
/// <para>
/// Bevy applies transitions after the frame's first stages, so one is read the frame after it
/// happens, when the systems of the frame it happened in have already seen the state it entered.
/// </para>
/// </remarks>
public readonly record struct StateTransitionEvent<TState>(TState? Exited, TState? Entered) where TState : struct, Enum;
