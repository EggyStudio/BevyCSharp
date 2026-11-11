using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One curve to add to a clip, the bridge's <c>BcsAnimationCurve</c>, field for field.</summary>
/// <remarks><c>AnimationClipTests</c> holds it to the offsets the bridge asserts.</remarks>
[StructLayout(LayoutKind.Sequential)]
public struct NativeAnimationCurve
{
    /// <summary>What it moves, as <see cref="AnimatedProperty"/> numbers it.</summary>
    public int Property;

    /// <summary>Zero for sampled values, one for two values eased between.</summary>
    public int Kind;

    /// <summary>How many values there are.</summary>
    public int Count;

    /// <summary>The easing, as <see cref="EaseKind"/> numbers it.</summary>
    public int Ease;

    /// <summary>A staircase's steps.</summary>
    public int EaseSteps;

    /// <summary>Where a staircase rises, as <see cref="JumpAt"/> numbers it.</summary>
    public int EaseJump;

    /// <summary>A spring's angular frequency.</summary>
    public float EaseOmega;

    /// <summary>How long an eased curve takes.</summary>
    public float Duration;

    /// <summary>One where an eased curve goes back to its start.</summary>
    public uint PingPong;
}
