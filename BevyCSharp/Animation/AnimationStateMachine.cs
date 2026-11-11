namespace Bevy;

/// <summary>
/// The states a model's clips are played in and the conditions that move it from one to the next,
/// run on a model by <see cref="Animation.Run"/>.
/// </summary>
/// <remarks>
/// <para>
/// A character idles, walks once its speed passes a value, runs past a higher one, and jumps when
/// a trigger fires, landing back in whichever it was in once the jump's clip ends. Each of those
/// is a state naming a clip, and each move between them a transition naming a condition on a
/// parameter the game sets (<see cref="Animation.SetParameter(Entity, string, float)"/>,
/// <see cref="Animation.Trigger"/>), so the game says what the character is doing and the machine
/// says which clip shows it.
/// </para>
/// <para>
/// It is a data asset, so a machine kept in a <c>*.data.json</c> file is edited in the editor as
/// a list of states and one of transitions, read with <see cref="DataAssets.Get{T}"/>, and shared
/// by every model that runs it. One made in code runs the same way. Transitions are tried in the
/// order they are listed each frame, and the first whose condition holds is taken, so a list
/// puts the more particular before the more general.
/// </para>
/// </remarks>
[DataAsset]
public sealed class AnimationStateMachine
{
    /// <summary>The state a model starts in, by name, or nothing for the first of <see cref="States"/>.</summary>
    public string Start = string.Empty;

    /// <summary>The states, each a clip and how it plays.</summary>
    public List<AnimationMachineState> States = [];

    /// <summary>The ways from one state to another, tried in order.</summary>
    public List<AnimationMachineTransition> Transitions = [];
}
