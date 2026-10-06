namespace Bevy;

/// <summary>
/// The lens a camera draws through.
/// </summary>
/// <remarks>
/// <para>
/// Beside <see cref="PostSettings"/> rather than part of it, because the two are decided at
/// different times. A settings screen owns the pipeline, and a scene sets these for a moment, a
/// hit, a dream, a shot pulling focus. The rule is the same, so every field is applied on every
/// call and an effect these settings leave off is taken off the camera.
/// </para>
/// <para>
/// Depth of field needs a perspective camera, since focus has no meaning without one. Auto
/// exposure needs a high dynamic range target, and gives itself one.
/// </para>
/// </remarks>
public sealed class EffectSettings
{
    /// <summary>How the depths that are out of focus are blurred.</summary>
    public DepthOfFieldMode DepthOfField { get; set; } = DepthOfFieldMode.None;

    /// <summary>
    /// Takes the depth of field's aperture and sensor from a lens, the one
    /// <see cref="Render.SetLens"/> meters the same camera from.
    /// </summary>
    /// <returns>These settings, for setting more after.</returns>
    public EffectSettings Through(PhysicalLens lens)
    {
        ArgumentNullException.ThrowIfNull(lens);

        Aperture = lens.Aperture;
        SensorHeight = lens.SensorHeight;
        return this;
    }

    /// <summary>Distance in meters to what is in focus.</summary>
    public float FocalDistance { get; set; } = 10f;

    /// <summary>
    /// Aperture in f-stops.
    /// </summary>
    /// <remarks>
    /// Smaller opens the lens wider, which leaves less of the scene in focus. Bevy's own is 1,
    /// which is wide, so a scene that should be mostly sharp needs a larger number.
    /// </remarks>
    public float Aperture { get; set; } = 1f;

    /// <summary>
    /// Height of the imaginary sensor, in meters.
    /// </summary>
    /// <remarks>
    /// With the camera's field of view this fixes the focal length, so it is the other half of
    /// how strong the blur is. Zero takes Bevy's own, the Super 35 cinema format.
    /// </remarks>
    public float SensorHeight { get; set; }

    /// <summary>
    /// Widest a single blur may be, in pixels. Zero takes Bevy's own.
    /// </summary>
    /// <remarks>Not physical. It caps how slow a very out-of-focus frame is allowed to
    /// be.</remarks>
    public float MaxBlurDiameter { get; set; }

    /// <summary>
    /// Distance past which nothing is blurred any further, in meters.
    /// </summary>
    /// <remarks>
    /// The renderer puts a sky infinitely far away, which would blur it as hard as
    /// <see cref="MaxBlurDiameter"/> allows. Zero leaves it unbounded.
    /// </remarks>
    public float MaxDepth { get; set; }

    /// <summary>
    /// Fraction of a frame the shutter is open, and so how far a moving thing smears.
    /// </summary>
    /// <remarks>
    /// Zero is no motion blur. A film camera's 180 degree shutter is 0.5, which gives a cinematic
    /// look at 24 frames a second; at 60 the same look is about 1.25. Above 1 a thing smears
    /// further than it moved, which is a choice rather than a mistake.
    /// </remarks>
    public float ShutterAngle { get; set; }

    /// <summary>
    /// Samples taken either side of a pixel along its motion.
    /// </summary>
    /// <remarks>
    /// Bevy takes one each way and one in the middle at 1, three each way at 3. Zero also turns
    /// motion blur off, whatever the shutter angle says.
    /// </remarks>
    public uint MotionBlurSamples { get; set; } = 1;

    /// <summary>
    /// Width of the colored fringe around edges, as a fraction of the window. Zero for none.
    /// </summary>
    /// <remarks>
    /// What a lens does when it fails to focus every color at one point. Bevy's own strength is
    /// 0.02, and a horror game reaching for it on a hit needs more.
    /// </remarks>
    public float Aberration { get; set; }

    /// <summary>Cap on the samples the fringe is built from. Zero takes Bevy's own.</summary>
    public uint AberrationSamples { get; set; }

    /// <summary>
    /// An image the fringe takes its colors from, read across its width.
    /// </summary>
    /// <remarks>
    /// Nothing here gives the usual red, green, blue. The image is sampled down its vertical
    /// center, so it should be one pixel tall.
    /// </remarks>
    public AssetHandle AberrationColors { get; set; } = AssetHandle.None;

    /// <summary>
    /// Strength of the lens warp. Zero leaves straight lines straight.
    /// </summary>
    /// <remarks>
    /// Positive bulges the picture outwards, as a wide lens does; negative pinches it inwards.
    /// Bevy's own strength is 0.5.
    /// </remarks>
    public float Distortion { get; set; }

    /// <summary>
    /// Zoom applied after warping, to crop the edges a strong warp leaves uncovered.
    /// </summary>
    public float DistortionScale { get; set; } = 1f;

    /// <summary>
    /// How much of the warp lands on each axis, for a lens that is not round.
    /// </summary>
    public (float X, float Y) DistortionAxes { get; set; } = (1f, 1f);

    /// <summary>Point the warp radiates from, in fractions of the window.</summary>
    public (float X, float Y) DistortionCenter { get; set; } = (0.5f, 0.5f);

    /// <summary>
    /// How sharply the warp bends at the edges of the picture.
    /// </summary>
    /// <remarks>Zero is the plain look, and what Bevy recommends for most scenes.</remarks>
    public float DistortionEdgeCurvature { get; set; }

    /// <summary>
    /// How dark the corners go, from 0 for no vignette to 1 for black.
    /// </summary>
    /// <remarks>
    /// What a lens does at the edges of its coverage, and what a game uses to pull the eye
    /// toward the middle or to show that the player is hurt.
    /// </remarks>
    public float Vignette { get; set; }

    /// <summary>How much of the picture is left untouched, as a fraction of the window.</summary>
    public float VignetteRadius { get; set; } = 0.75f;

    /// <summary>Width of the edge between the clear center and the dark corners.</summary>
    public float VignetteSmoothness { get; set; } = 5f;

    /// <summary>Shape of that edge, where 1 is a circle.</summary>
    public float VignetteRoundness { get; set; } = 1f;

    /// <summary>Point the vignette is centered on, in fractions of the window.</summary>
    public (float X, float Y) VignetteCenter { get; set; } = (0.5f, 0.5f);

    /// <summary>
    /// How far the vignette is stretched to fit a window that is not square, 0 not at all and 1
    /// exactly.
    /// </summary>
    public float VignetteEdgeCompensation { get; set; } = 1f;

    /// <summary>The color the corners are taken toward, linear. Black is the usual one.</summary>
    public (float R, float G, float B, float A) VignetteColor { get; set; } = (0f, 0f, 0f, 1f);

    /// <summary>
    /// Let the camera find its own exposure from what it can see.
    /// </summary>
    /// <remarks>
    /// A histogram of the frame's brightness is built and the exposure moved so that the average
    /// lands on middle gray, as an eye adjusts walking out of a cave. The camera is given a high
    /// dynamic range target, because there is nothing to meter without one.
    /// </remarks>
    public bool AutoExposure { get; set; }

    /// <summary>Darkest and brightest luminance the metering counts, in EV-100.</summary>
    /// <remarks>Anything below is ignored and anything above counts as the brightest.</remarks>
    public (float Min, float Max) MeteringRange { get; set; } = (-8f, 8f);

    /// <summary>
    /// The part of the histogram that is averaged, as fractions from darkest to brightest.
    /// </summary>
    /// <remarks>
    /// Bevy's own throws away the darkest tenth and the brightest tenth, so a shadow in the
    /// corner and a lamp in the frame do not decide the exposure between them.
    /// </remarks>
    public (float Low, float High) MeteringFilter { get; set; } = (0.10f, 0.90f);

    /// <summary>How fast the exposure opens as a scene darkens, in f-stops per second.</summary>
    public float SpeedBrighten { get; set; } = 3f;

    /// <summary>How fast it closes as a scene brightens, in f-stops per second.</summary>
    public float SpeedDarken { get; set; } = 1f;

    /// <summary>
    /// How near the target the adaptation stops being linear, in f-stops. Zero takes Bevy's own.
    /// </summary>
    /// <remarks>
    /// Inside this distance the exposure eases in rather than tracking straight, which stops it
    /// jittering while the scene changes slightly from frame to frame.
    /// </remarks>
    public float ExposureTransition { get; set; }

    /// <summary>
    /// An image weighting where in the frame the metering looks.
    /// </summary>
    /// <remarks>
    /// Only the red channel is read, and it is stretched over the whole frame, so black ignores a
    /// pixel and white counts it fully. Nothing here weights the whole frame alike.
    /// </remarks>
    public AssetHandle MeteringMask { get; set; } = AssetHandle.None;

    /// <summary>
    /// A curve applied to the exposure the metering arrived at.
    /// </summary>
    /// <remarks>
    /// Each point pairs a measured luminance in EV-100 with the compensation to apply there in
    /// f-stops, so a night scene can be left darker than middle gray and a desert left brighter.
    /// The points have to rise in luminance, and at most eight of them cross the boundary. Fewer
    /// than two is no compensation.
    /// </remarks>
    public IReadOnlyList<(float Luminance, float Compensation)>? ExposureCompensation { get; set; }
}
