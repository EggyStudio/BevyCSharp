using System.Globalization;

namespace Bevy;

/// <summary>
/// One of Bevy's easing functions, taking a progress from zero to one to a value that starts at
/// zero and ends at one, as Bevy's <c>EaseFunction</c> does.
/// </summary>
/// <remarks>
/// <para>
/// A struct, so a behavior keeps one as a field and an animation on an entity says how it eases,
/// as a component's field does in Bevy. Each function is <c>EaseFunction.SineIn</c> and the rest,
/// as Bevy names them, and the two that carry numbers are made by <see cref="Steps"/> and
/// <see cref="Elastic"/>.
/// </para>
/// <para>
/// Each is written as bevy_math writes it, down to its constants, so a motion here eases as the
/// same motion does in a Bevy game. <see cref="EaseKind.ElasticIn"/>, <see cref="EaseKind.BackIn"/>
/// and the rest of those two families go below zero or past one on the way, and end where the
/// others do.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var x = -6f + 12f * EaseFunction.CubicInOut.Sample(progress);
/// </code>
/// </example>
public struct EaseFunction : IEquatable<EaseFunction>
{
    /// <summary>Which function it is.</summary>
    public EaseKind Kind;

    /// <summary>How many steps a <see cref="EaseKind.Steps"/> staircase has.</summary>
    public int StepCount;

    /// <summary>Where a <see cref="EaseKind.Steps"/> staircase rises.</summary>
    public JumpAt Jump;

    /// <summary>The angular frequency of an <see cref="EaseKind.Elastic"/> spring, in radians over the whole.</summary>
    public float Omega;

    // bevy_math's rescaled exponential, (2^(10t) - 1) / 1023 written with these two, so the family
    // starts at zero and ends at one, where the textbook's 2^(10t - 10) starts a thousandth off.
    // They carry more digits than a float keeps, as bevy_math writes them.
    private const float Log2Of1023 = 9.998590429745328646459226f;
    private const float OneOver1023 = 0.00097751710654936461388074291f;

    // bevy_math's overshoot for BackInOut, its BackIn's 1.70158 with 1.525 added, where Robert
    // Penner's equations multiply the two.
    private const float BackInOutC = 1.70158f + 1.525f;

    /// <summary>The function of <paramref name="kind"/>, with no steps and no frequency.</summary>
    /// <remarks>
    /// For the kinds that carry nothing. <see cref="Steps"/> and <see cref="Elastic"/> make the two
    /// that do with their numbers.
    /// </remarks>
    public EaseFunction(EaseKind kind) => Kind = kind;

    /// <summary>The progress unchanged.</summary>
    public static EaseFunction Linear => new(EaseKind.Linear);
    /// <summary>The square of the progress.</summary>
    public static EaseFunction QuadraticIn => new(EaseKind.QuadraticIn);
    /// <summary>Slowing as the square does.</summary>
    public static EaseFunction QuadraticOut => new(EaseKind.QuadraticOut);
    /// <summary>Slow at both ends by the square.</summary>
    public static EaseFunction QuadraticInOut => new(EaseKind.QuadraticInOut);
    /// <summary>The cube of the progress.</summary>
    public static EaseFunction CubicIn => new(EaseKind.CubicIn);
    /// <summary>Slowing as the cube does.</summary>
    public static EaseFunction CubicOut => new(EaseKind.CubicOut);
    /// <summary>Slow at both ends by the cube.</summary>
    public static EaseFunction CubicInOut => new(EaseKind.CubicInOut);
    /// <summary>The fourth power of the progress.</summary>
    public static EaseFunction QuarticIn => new(EaseKind.QuarticIn);
    /// <summary>Slowing as the fourth power does.</summary>
    public static EaseFunction QuarticOut => new(EaseKind.QuarticOut);
    /// <summary>Slow at both ends by the fourth power.</summary>
    public static EaseFunction QuarticInOut => new(EaseKind.QuarticInOut);
    /// <summary>The fifth power of the progress.</summary>
    public static EaseFunction QuinticIn => new(EaseKind.QuinticIn);
    /// <summary>Slowing as the fifth power does.</summary>
    public static EaseFunction QuinticOut => new(EaseKind.QuinticOut);
    /// <summary>Slow at both ends by the fifth power.</summary>
    public static EaseFunction QuinticInOut => new(EaseKind.QuinticInOut);
    /// <summary>The first half of <see cref="SmoothStep"/>, stretched over the whole.</summary>
    public static EaseFunction SmoothStepIn => new(EaseKind.SmoothStepIn);
    /// <summary>The second half of <see cref="SmoothStep"/>, stretched over the whole.</summary>
    public static EaseFunction SmoothStepOut => new(EaseKind.SmoothStepOut);
    /// <summary>The cubic that is flat at both ends.</summary>
    public static EaseFunction SmoothStep => new(EaseKind.SmoothStep);
    /// <summary>The first half of <see cref="SmootherStep"/>, stretched over the whole.</summary>
    public static EaseFunction SmootherStepIn => new(EaseKind.SmootherStepIn);
    /// <summary>The second half of <see cref="SmootherStep"/>, stretched over the whole.</summary>
    public static EaseFunction SmootherStepOut => new(EaseKind.SmootherStepOut);
    /// <summary>Ken Perlin's quintic, flat at both ends.</summary>
    public static EaseFunction SmootherStep => new(EaseKind.SmootherStep);
    /// <summary>A quarter of a cosine, starting slow.</summary>
    public static EaseFunction SineIn => new(EaseKind.SineIn);
    /// <summary>A quarter of a sine, slowing at the end.</summary>
    public static EaseFunction SineOut => new(EaseKind.SineOut);
    /// <summary>Half a cosine, slow at both ends.</summary>
    public static EaseFunction SineInOut => new(EaseKind.SineInOut);
    /// <summary>A quarter of a circle, starting slow.</summary>
    public static EaseFunction CircularIn => new(EaseKind.CircularIn);
    /// <summary>A quarter of a circle, slowing at the end.</summary>
    public static EaseFunction CircularOut => new(EaseKind.CircularOut);
    /// <summary>Two quarters of a circle, slow at both ends.</summary>
    public static EaseFunction CircularInOut => new(EaseKind.CircularInOut);
    /// <summary>Two to the power of ten times the progress, starting very slow.</summary>
    public static EaseFunction ExponentialIn => new(EaseKind.ExponentialIn);
    /// <summary>Slowing as the exponential does.</summary>
    public static EaseFunction ExponentialOut => new(EaseKind.ExponentialOut);
    /// <summary>Slow at both ends by the exponential.</summary>
    public static EaseFunction ExponentialInOut => new(EaseKind.ExponentialInOut);
    /// <summary>A spring wound up before it lets go.</summary>
    public static EaseFunction ElasticIn => new(EaseKind.ElasticIn);
    /// <summary>A spring overshooting one and settling there.</summary>
    public static EaseFunction ElasticOut => new(EaseKind.ElasticOut);
    /// <summary>A spring at both ends.</summary>
    public static EaseFunction ElasticInOut => new(EaseKind.ElasticInOut);
    /// <summary>Drawing back below zero before it goes.</summary>
    public static EaseFunction BackIn => new(EaseKind.BackIn);
    /// <summary>Overshooting one and coming back to it.</summary>
    public static EaseFunction BackOut => new(EaseKind.BackOut);
    /// <summary>Drawing back before it goes and overshooting before it stops.</summary>
    public static EaseFunction BackInOut => new(EaseKind.BackInOut);
    /// <summary>Bouncing up from zero.</summary>
    public static EaseFunction BounceIn => new(EaseKind.BounceIn);
    /// <summary>A ball dropped onto one, bouncing lower each time.</summary>
    public static EaseFunction BounceOut => new(EaseKind.BounceOut);
    /// <summary>Bouncing up from zero and down onto one.</summary>
    public static EaseFunction BounceInOut => new(EaseKind.BounceInOut);

    /// <summary>A staircase of <paramref name="count"/> steps rising where <paramref name="jump"/> says, as Bevy's <c>EaseFunction::Steps</c>.</summary>
    /// <param name="count">How many steps, at least one being taken whatever this is.</param>
    /// <param name="jump">Where it rises, at the start, the end, both or neither.</param>
    /// <remarks>
    /// A clock's second hand that ticks rather than sweeps, or a sprite sheet's frames stepped
    /// through by an eased progress.
    /// </remarks>
    public static EaseFunction Steps(int count, JumpAt jump) => new(EaseKind.Steps) { StepCount = count, Jump = jump };

    /// <summary>A spring of angular frequency <paramref name="omega"/> settling at one, as Bevy's <c>EaseFunction::Elastic</c>.</summary>
    /// <param name="omega">How fast it swings, in radians over the whole, the higher the more swings.</param>
    /// <remarks>
    /// <see cref="ElasticOut"/> swings a fixed number of times. This one swings as often as
    /// <paramref name="omega"/> says, and settles as the square of what is left of the progress.
    /// </remarks>
    public static EaseFunction Elastic(float omega) => new(EaseKind.Elastic) { Omega = omega };

    /// <summary>The value at <paramref name="t"/>, from zero at its start to one at its end.</summary>
    /// <param name="t">How far along, from zero to one.</param>
    /// <remarks>
    /// A progress outside zero to one is taken as the nearer end, as Bevy's <c>sample_clamped</c>
    /// takes it, so a timer run a little past its end eases to one and stays there.
    /// </remarks>
    public readonly float Sample(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Kind switch
        {
            EaseKind.Linear => t,

            EaseKind.QuadraticIn => t * t,
            EaseKind.QuadraticOut => 1f - (1f - t) * (1f - t),
            EaseKind.QuadraticInOut => t < 0.5f ? 2f * t * t : 1f - Pow(-2f * t + 2f, 2) / 2f,

            EaseKind.CubicIn => t * t * t,
            EaseKind.CubicOut => 1f - Pow(1f - t, 3),
            EaseKind.CubicInOut => t < 0.5f ? 4f * t * t * t : 1f - Pow(-2f * t + 2f, 3) / 2f,

            EaseKind.QuarticIn => Pow(t, 4),
            EaseKind.QuarticOut => 1f - Pow(1f - t, 4),
            EaseKind.QuarticInOut => t < 0.5f ? 8f * Pow(t, 4) : 1f - Pow(-2f * t + 2f, 4) / 2f,

            EaseKind.QuinticIn => Pow(t, 5),
            EaseKind.QuinticOut => 1f - Pow(1f - t, 5),
            EaseKind.QuinticInOut => t < 0.5f ? 16f * Pow(t, 5) : 1f - Pow(-2f * t + 2f, 5) / 2f,

            EaseKind.SmoothStepIn => ((1.5f - 0.5f * t) * t) * t,
            EaseKind.SmoothStepOut => (1.5f + (-0.5f * t) * t) * t,
            EaseKind.SmoothStep => ((3f - 2f * t) * t) * t,

            EaseKind.SmootherStepIn => (((2.5f + (-1.875f + 0.375f * t) * t) * t) * t) * t,
            EaseKind.SmootherStepOut => (1.875f + ((-1.25f + (0.375f * t) * t) * t) * t) * t,
            EaseKind.SmootherStep => (((10f + (-15f + 6f * t) * t) * t) * t) * t,

            EaseKind.SineIn => 1f - MathF.Cos(t * MathF.PI / 2f),
            EaseKind.SineOut => MathF.Sin(t * MathF.PI / 2f),
            EaseKind.SineInOut => -(MathF.Cos(MathF.PI * t) - 1f) / 2f,

            EaseKind.CircularIn => 1f - MathF.Sqrt(1f - t * t),
            EaseKind.CircularOut => MathF.Sqrt(1f - (t - 1f) * (t - 1f)),
            EaseKind.CircularInOut => t < 0.5f
                ? (1f - MathF.Sqrt(1f - Pow(2f * t, 2))) / 2f
                : (MathF.Sqrt(1f - Pow(-2f * t + 2f, 2)) + 1f) / 2f,

            EaseKind.ExponentialIn => MathF.Pow(2f, 10f * t - Log2Of1023) - OneOver1023,
            EaseKind.ExponentialOut => (OneOver1023 + 1f) - MathF.Pow(2f, -10f * t - (Log2Of1023 - 10f)),
            EaseKind.ExponentialInOut => t < 0.5f
                ? MathF.Pow(2f, 20f * t - (Log2Of1023 + 1f)) - OneOver1023 / 2f
                : (OneOver1023 / 2f + 1f) - MathF.Pow(2f, -20f * t - (Log2Of1023 - 19f)),

            EaseKind.ElasticIn => -MathF.Pow(2f, 10f * t - 10f) * MathF.Sin((t * 10f - 10.75f) * 2f * MathF.PI / 3f),
            EaseKind.ElasticOut => MathF.Pow(2f, -10f * t) * MathF.Sin((t * 10f - 0.75f) * 2f * MathF.PI / 3f) + 1f,
            EaseKind.ElasticInOut => t < 0.5f
                ? -MathF.Pow(2f, 20f * t - 10f) * MathF.Sin((t * 20f - 11.125f) * 2f * MathF.PI / 4.5f) / 2f
                : MathF.Pow(2f, -20f * t + 10f) * MathF.Sin((t * 20f - 11.125f) * 2f * MathF.PI / 4.5f) / 2f + 1f,

            EaseKind.BackIn => (1.70158f + 1f) * t * t * t - 1.70158f * t * t,
            EaseKind.BackOut => 1f + (1.70158f + 1f) * Pow(t - 1f, 3) + 1.70158f * Pow(t - 1f, 2),
            EaseKind.BackInOut => t < 0.5f
                ? Pow(2f * t, 2) * ((BackInOutC + 1f) * 2f * t - BackInOutC) / 2f
                : (Pow(2f * t - 2f, 2) * ((BackInOutC + 1f) * (2f * t - 2f) + BackInOutC) + 2f) / 2f,

            EaseKind.BounceIn => 1f - Bounce(1f - t),
            EaseKind.BounceOut => Bounce(t),
            EaseKind.BounceInOut => t < 0.5f ? (1f - Bounce(1f - 2f * t)) / 2f : (1f + Bounce(2f * t - 1f)) / 2f,

            EaseKind.Steps => StepsAt(t),
            EaseKind.Elastic => 1f - (1f - t) * (1f - t) * (2f * MathF.Sin(Omega * t) / Omega + MathF.Cos(Omega * t)),

            _ => t,
        };
    }

    // Bevy's JumpAt::eval, the step the progress is on, moved up one where the staircase rises at
    // its start, over a count of steps grown or shrunk by one where it rises at both ends or
    // neither.
    private readonly float StepsAt(float t)
    {
        var (lift, extra) = Jump switch
        {
            JumpAt.Start => (1f, 0),
            JumpAt.None => (0f, -1),
            JumpAt.Both => (1f, 1),
            _ => (0f, 0),
        };
        return Math.Clamp((MathF.Floor(t * StepCount) + lift) / Math.Max(StepCount + extra, 1), 0f, 1f);
    }

    // bevy_math's bounce_out, four arcs of a ball dropped onto one, each lower than the last.
    private static float Bounce(float t) =>
        t < 4f / 11f ? 121f * t * t / 16f
        : t < 8f / 11f ? 363f / 40f * t * t - 99f / 10f * t + 17f / 5f
        : t < 9f / 10f ? 4356f / 361f * t * t - 35442f / 1805f * t + 16061f / 1805f
        : 54f / 5f * t * t - 513f / 25f * t + 268f / 25f;

    // Raising by a whole power with multiplications, as bevy_math's squared and cubed do, so the
    // polynomial families come out as Bevy's rather than by MathF.Pow's logarithms.
    private static float Pow(float x, int n)
    {
        var result = 1f;
        for (var i = 0; i < n; i++) result *= x;
        return result;
    }

    /// <summary>The function as Bevy prints it, <c>SineIn</c>, <c>Steps(4, End)</c> or <c>Elastic(50.0)</c>.</summary>
    /// <remarks>Bevy's examples label a function by its debug form, and this gives the same words.</remarks>
    public readonly override string ToString() => Kind switch
    {
        EaseKind.Steps => $"Steps({StepCount.ToString(CultureInfo.InvariantCulture)}, {Jump})",
        EaseKind.Elastic => $"Elastic({RustFloat(Omega)})",
        _ => Kind.ToString(),
    };

    // A float as Rust's debug form writes it, the shortest digits that read back the same, with a
    // point and a zero where the number is whole.
    private static string RustFloat(float value)
    {
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        return text.Contains('.') || text.Contains('E') || text.Contains("Infinity") || text == "NaN" ? text : text + ".0";
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Two functions of the same kind are equal whatever numbers they carry that the kind does not
    /// read, so a <see cref="SineIn"/> left with a step count is still <see cref="SineIn"/>.
    /// </remarks>
    public readonly bool Equals(EaseFunction other) => Kind == other.Kind && Kind switch
    {
        EaseKind.Steps => StepCount == other.StepCount && Jump == other.Jump,
        EaseKind.Elastic => Omega.Equals(other.Omega),
        _ => true,
    };

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is EaseFunction other && Equals(other);

    /// <inheritdoc/>
    public readonly override int GetHashCode() => Kind switch
    {
        EaseKind.Steps => HashCode.Combine(Kind, StepCount, Jump),
        EaseKind.Elastic => HashCode.Combine(Kind, Omega),
        _ => Kind.GetHashCode(),
    };

    /// <summary>Whether two are the same function.</summary>
    public static bool operator ==(EaseFunction a, EaseFunction b) => a.Equals(b);

    /// <summary>Whether two are different functions.</summary>
    public static bool operator !=(EaseFunction a, EaseFunction b) => !a.Equals(b);
}
