namespace Bevy;

/// <summary>Where a staircase of <see cref="EaseFunction.Steps"/> rises, as Bevy's <c>JumpAt</c> says.</summary>
/// <remarks>
/// CSS's <c>steps()</c> names the same four. <see cref="End"/> is first, so a field left alone
/// holds it, as Bevy's default is <c>End</c> too.
/// </remarks>
public enum JumpAt : byte
{
    /// <summary>The last step lands as the progress reaches one.</summary>
    End,

    /// <summary>The first step is taken as the progress leaves zero.</summary>
    Start,

    /// <summary>Neither, so the curve sits at zero for the first step and at one for the last.</summary>
    None,

    /// <summary>Both, so the curve is never at zero and only at one at the very end.</summary>
    Both,
}
