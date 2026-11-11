namespace Bevy;

/// <summary>When an <see cref="AnimationMachineTransition"/> is taken.</summary>
public enum AnimationCondition
{
    /// <summary>
    /// When the state's clip ends, or comes round to its start for one that repeats, since the
    /// state was entered.
    /// </summary>
    Finished,

    /// <summary>When the parameter is true, which is any value but zero.</summary>
    True,

    /// <summary>When the parameter is false, or has not been set.</summary>
    False,

    /// <summary>When the parameter is above the transition's value.</summary>
    Above,

    /// <summary>When the parameter is below the transition's value.</summary>
    Below,

    /// <summary>
    /// When the parameter has been triggered with <see cref="Animation.Trigger"/>, which taking the
    /// transition uses up.
    /// </summary>
    Triggered,
}
