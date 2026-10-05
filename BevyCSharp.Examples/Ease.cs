namespace BevyCSharp.Examples;

/// <summary>
/// Bevy's easing functions, each from nought to one over nought to one, written as bevy_math writes
/// them, for the examples that draw or move by them.
/// </summary>
/// <remarks>
/// Bevy's are a part of its math rather than of anything the bridge reaches, so they are here for
/// the examples as a game would write the ones it uses.
/// </remarks>
internal static class Ease
{
    /// <summary>Where a stepped curve jumps, as Bevy's <c>JumpAt</c> says.</summary>
    public enum JumpAt { Start, End, None, Both }

    // Bevy's, more precise than a float holds, as it writes them.
    private const float Log2Of1023 = 9.998590429745328646459226f;
    private const float OneOver1023 = 0.00097751710654936461388074291f;

    public static float Linear(float t) => t;

    public static float QuadraticIn(float t) => t * t;
    public static float QuadraticOut(float t) => 1f - (1f - t) * (1f - t);
    public static float QuadraticInOut(float t) => t < 0.5f ? 2f * t * t : 1f - Pow(-2f * t + 2f, 2) / 2f;

    public static float CubicIn(float t) => t * t * t;
    public static float CubicOut(float t) => 1f - Pow(1f - t, 3);
    public static float CubicInOut(float t) => t < 0.5f ? 4f * t * t * t : 1f - Pow(-2f * t + 2f, 3) / 2f;

    public static float QuarticIn(float t) => Pow(t, 4);
    public static float QuarticOut(float t) => 1f - Pow(1f - t, 4);
    public static float QuarticInOut(float t) => t < 0.5f ? 8f * Pow(t, 4) : 1f - Pow(-2f * t + 2f, 4) / 2f;

    public static float QuinticIn(float t) => Pow(t, 5);
    public static float QuinticOut(float t) => 1f - Pow(1f - t, 5);
    public static float QuinticInOut(float t) => t < 0.5f ? 16f * Pow(t, 5) : 1f - Pow(-2f * t + 2f, 5) / 2f;

    public static float SmoothStepIn(float t) => ((1.5f - 0.5f * t) * t) * t;
    public static float SmoothStepOut(float t) => (1.5f + (-0.5f * t) * t) * t;
    public static float SmoothStep(float t) => ((3f - 2f * t) * t) * t;

    public static float SmootherStepIn(float t) => (((2.5f + (-1.875f + 0.375f * t) * t) * t) * t) * t;
    public static float SmootherStepOut(float t) => (1.875f + ((-1.25f + (0.375f * t) * t) * t) * t) * t;
    public static float SmootherStep(float t) => (((10f + (-15f + 6f * t) * t) * t) * t) * t;

    public static float SineIn(float t) => 1f - MathF.Cos(t * MathF.PI / 2f);
    public static float SineOut(float t) => MathF.Sin(t * MathF.PI / 2f);
    public static float SineInOut(float t) => -(MathF.Cos(MathF.PI * t) - 1f) / 2f;

    public static float CircularIn(float t) => 1f - MathF.Sqrt(1f - t * t);
    public static float CircularOut(float t) => MathF.Sqrt(1f - (t - 1f) * (t - 1f));
    public static float CircularInOut(float t) => t < 0.5f
        ? (1f - MathF.Sqrt(1f - Pow(2f * t, 2))) / 2f
        : (MathF.Sqrt(1f - Pow(-2f * t + 2f, 2)) + 1f) / 2f;

    public static float ExponentialIn(float t) => MathF.Pow(2f, 10f * t - Log2Of1023) - OneOver1023;
    public static float ExponentialOut(float t) => (OneOver1023 + 1f) - MathF.Pow(2f, -10f * t - (Log2Of1023 - 10f));
    public static float ExponentialInOut(float t) => t < 0.5f
        ? MathF.Pow(2f, 20f * t - (Log2Of1023 + 1f)) - OneOver1023 / 2f
        : (OneOver1023 / 2f + 1f) - MathF.Pow(2f, -20f * t - (Log2Of1023 - 19f));

    public static float ElasticIn(float t) => -MathF.Pow(2f, 10f * t - 10f) * MathF.Sin((t * 10f - 10.75f) * 2f * MathF.PI / 3f);
    public static float ElasticOut(float t) => MathF.Pow(2f, -10f * t) * MathF.Sin((t * 10f - 0.75f) * 2f * MathF.PI / 3f) + 1f;
    public static float ElasticInOut(float t)
    {
        var c = 2f * MathF.PI / 4.5f;
        return t < 0.5f
            ? -MathF.Pow(2f, 20f * t - 10f) * MathF.Sin((t * 20f - 11.125f) * c) / 2f
            : MathF.Pow(2f, -20f * t + 10f) * MathF.Sin((t * 20f - 11.125f) * c) / 2f + 1f;
    }

    public static float BackIn(float t) => (1.70158f + 1f) * t * t * t - 1.70158f * t * t;
    public static float BackOut(float t) => 1f + (1.70158f + 1f) * Pow(t - 1f, 3) + 1.70158f * Pow(t - 1f, 2);
    public static float BackInOut(float t)
    {
        var c = 1.70158f + 1.525f;
        return t < 0.5f
            ? Pow(2f * t, 2) * ((c + 1f) * 2f * t - c) / 2f
            : (Pow(2f * t - 2f, 2) * ((c + 1f) * (2f * t - 2f) + c) + 2f) / 2f;
    }

    public static float BounceIn(float t) => 1f - BounceOut(1f - t);
    public static float BounceOut(float t) =>
        t < 4f / 11f ? 121f * t * t / 16f
        : t < 8f / 11f ? 363f / 40f * t * t - 99f / 10f * t + 17f / 5f
        : t < 9f / 10f ? 4356f / 361f * t * t - 35442f / 1805f * t + 16061f / 1805f
        : 54f / 5f * t * t - 513f / 25f * t + 268f / 25f;
    public static float BounceInOut(float t) => t < 0.5f ? (1f - BounceOut(1f - 2f * t)) / 2f : (1f + BounceOut(2f * t - 1f)) / 2f;

    /// <summary>Bevy's <c>Steps</c>, a staircase of <paramref name="steps"/> rising where <paramref name="jump"/> says.</summary>
    public static float Steps(int steps, JumpAt jump, float t)
    {
        var (a, b) = jump switch
        {
            JumpAt.Start => (1f, 0),
            JumpAt.End => (0f, 0),
            JumpAt.None => (0f, -1),
            _ => (1f, 1),
        };
        return Math.Clamp((MathF.Floor(t * steps) + a) / Math.Max(steps + b, 1), 0f, 1f);
    }

    /// <summary>Bevy's <c>Elastic</c>, a spring of angular frequency <paramref name="omega"/> settling at one.</summary>
    public static float Elastic(float omega, float t) =>
        1f - (1f - t) * (1f - t) * (2f * MathF.Sin(omega * t) / omega + MathF.Cos(omega * t));

    private static float Pow(float x, int n)
    {
        var result = 1f;
        for (var i = 0; i < n; i++) result *= x;
        return result;
    }
}
