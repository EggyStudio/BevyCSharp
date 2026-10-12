namespace Bevy;

/// <summary>
/// Names the <see cref="AnimationStateMachine"/> a model runs, on the entity its scene was spawned
/// or placed under, so a scene keeps which machine each placed model runs.
/// </summary>
/// <remarks>
/// <para>
/// The machine starts once the model arrives, as <see cref="Animation.Run"/> starts one, and starts
/// again from its first state when the reference names another machine or the file it names is
/// read again. Taking the component off stops the machine and leaves the clip playing as it was.
/// The game sets parameters and fires triggers on the model as it does for a machine run from code.
/// </para>
/// <para>
/// A reference naming no file, or a file that is not a machine or whose names do not hold
/// together, is said once on the log, and the model is left as it is until the reference changes.
/// </para>
/// </remarks>
[Behavior]
public partial struct AnimationMachinePlayer
{
    /// <summary>The machine, a <c>*.data.json</c> file holding an <see cref="AnimationStateMachine"/>.</summary>
    public DataRef<AnimationStateMachine> Machine;
}
