namespace Bevy;

/// <summary>
/// A way from one state of an <see cref="AnimationStateMachine"/> to another, taken when its
/// condition holds.
/// </summary>
public sealed class AnimationMachineTransition
{
    /// <summary>The state it leaves, by name, or nothing for any state but <see cref="To"/>.</summary>
    /// <remarks>
    /// A transition from any state is how a hit or a fall interrupts whatever a character is doing,
    /// written once rather than from each state. It is not taken from the state it goes to, which
    /// would start that state's clip over every frame its condition holds.
    /// </remarks>
    public string From = string.Empty;

    /// <summary>The state it enters, by name.</summary>
    public string To = string.Empty;

    /// <summary>When it is taken.</summary>
    public AnimationCondition When;

    /// <summary>The parameter the condition reads, by name, which <see cref="AnimationCondition.Finished"/> needs none of.</summary>
    public string Parameter = string.Empty;

    /// <summary>What <see cref="AnimationCondition.Above"/> and <see cref="AnimationCondition.Below"/> compare the parameter with.</summary>
    public float Value;

    /// <summary>Seconds over which the state left fades out as the one entered fades in, or zero for a cut.</summary>
    public float Blend = 0.2f;
}
