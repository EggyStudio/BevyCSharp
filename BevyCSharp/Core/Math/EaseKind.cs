namespace Bevy;

/// <summary>Which of Bevy's easing functions an <see cref="EaseFunction"/> is.</summary>
/// <remarks>
/// In Bevy's order, so a field left alone holds <see cref="Linear"/>, the first.
/// <see cref="Steps"/> and <see cref="Elastic"/> read the parameters an <see cref="EaseFunction"/>
/// carries beside its kind.
/// </remarks>
public enum EaseKind : byte
{
    /// <summary>The progress unchanged.</summary>
    Linear,

    /// <summary>The square of the progress, starting slow.</summary>
    QuadraticIn,

    /// <summary>Starting fast and slowing as the square does.</summary>
    QuadraticOut,

    /// <summary>Slow at both ends by the square.</summary>
    QuadraticInOut,

    /// <summary>The cube of the progress.</summary>
    CubicIn,

    /// <summary>Slowing as the cube does.</summary>
    CubicOut,

    /// <summary>Slow at both ends by the cube.</summary>
    CubicInOut,

    /// <summary>The fourth power of the progress.</summary>
    QuarticIn,

    /// <summary>Slowing as the fourth power does.</summary>
    QuarticOut,

    /// <summary>Slow at both ends by the fourth power.</summary>
    QuarticInOut,

    /// <summary>The fifth power of the progress.</summary>
    QuinticIn,

    /// <summary>Slowing as the fifth power does.</summary>
    QuinticOut,

    /// <summary>Slow at both ends by the fifth power.</summary>
    QuinticInOut,

    /// <summary>The first half of <see cref="SmoothStep"/>, stretched over the whole.</summary>
    SmoothStepIn,

    /// <summary>The second half of <see cref="SmoothStep"/>, stretched over the whole.</summary>
    SmoothStepOut,

    /// <summary>The cubic that is flat at both ends, the shader language's <c>smoothstep</c>.</summary>
    SmoothStep,

    /// <summary>The first half of <see cref="SmootherStep"/>, stretched over the whole.</summary>
    SmootherStepIn,

    /// <summary>The second half of <see cref="SmootherStep"/>, stretched over the whole.</summary>
    SmootherStepOut,

    /// <summary>Ken Perlin's quintic, flat at both ends in its slope and its curvature.</summary>
    SmootherStep,

    /// <summary>A quarter of a cosine, starting slow.</summary>
    SineIn,

    /// <summary>A quarter of a sine, slowing at the end.</summary>
    SineOut,

    /// <summary>Half a cosine, slow at both ends.</summary>
    SineInOut,

    /// <summary>A quarter of a circle, starting slow.</summary>
    CircularIn,

    /// <summary>A quarter of a circle, slowing at the end.</summary>
    CircularOut,

    /// <summary>Two quarters of a circle, slow at both ends.</summary>
    CircularInOut,

    /// <summary>Two to the power of ten times the progress, starting very slow.</summary>
    ExponentialIn,

    /// <summary>Slowing as the exponential does.</summary>
    ExponentialOut,

    /// <summary>Slow at both ends by the exponential.</summary>
    ExponentialInOut,

    /// <summary>A spring wound up before it lets go, dipping below zero.</summary>
    ElasticIn,

    /// <summary>A spring overshooting one and settling there.</summary>
    ElasticOut,

    /// <summary>A spring at both ends, dipping below zero and overshooting one.</summary>
    ElasticInOut,

    /// <summary>Drawing back below zero before it goes.</summary>
    BackIn,

    /// <summary>Overshooting one and coming back to it.</summary>
    BackOut,

    /// <summary>Drawing back before it goes and overshooting before it stops.</summary>
    BackInOut,

    /// <summary>Bouncing up from zero, the reverse of <see cref="BounceOut"/>.</summary>
    BounceIn,

    /// <summary>A ball dropped onto one, bouncing lower each time.</summary>
    BounceOut,

    /// <summary>Bouncing up from zero and down onto one.</summary>
    BounceInOut,

    /// <summary>A staircase, its steps and where it jumps carried by the function.</summary>
    Steps,

    /// <summary>A spring settling at one, its angular frequency carried by the function.</summary>
    Elastic,
}
