namespace Bevy;

/// <summary>How a reflection probe renders what is around it.</summary>
public sealed class ProbeCaptureSettings
{
    /// <summary>
    /// Texels along a side of each face. A power of two, since Bevy's filter needs one, and the
    /// sharpness of what a polished surface reflects.
    /// </summary>
    public uint Size { get; set; } = 256;

    /// <summary>
    /// How bright the captured light is, in candelas per square meter. The faces are drawn at
    /// Bevy's default exposure, which divides light by about a thousand, and the default undoes it.
    /// </summary>
    public float Intensity { get; set; } = 1000f;

    /// <summary>The fraction of the box faded across on each axis, as for a baked probe.</summary>
    public Vec3 Falloff { get; set; }

    /// <summary>Whether to capture every frame, or once until asked again.</summary>
    public bool Live { get; set; } = true;

    /// <summary>
    /// How far from the center the cameras start seeing, so a probe inside a small object does not
    /// capture the inside of it.
    /// </summary>
    public float Near { get; set; } = 0.05f;
}
