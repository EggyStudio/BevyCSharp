namespace Bevy;

/// <summary>
/// One line of a run drawn by <see cref="Gizmos.Lines"/>.
/// </summary>
/// <remarks>
/// Its own two ends and its own color, because a run is usually a path or a wireframe where each
/// segment is somewhere different and some of them mean something different. <see cref="Fading"/>
/// gives it a second color, for a line running out to a horizon.
/// </remarks>
public readonly struct GizmoSegment
{
    /// <summary>A line of one color.</summary>
    /// <param name="start">Where it begins, in world space.</param>
    /// <param name="end">Where it ends.</param>
    /// <param name="color">Linear RGBA.</param>
    public GizmoSegment(Vec3 start, Vec3 end, (float R, float G, float B, float A) color)
    {
        Start = start;
        End = end;
        Color = color;
        EndColor = color;
    }

    private GizmoSegment(
        Vec3 start,
        Vec3 end,
        (float R, float G, float B, float A) from,
        (float R, float G, float B, float A) to)
    {
        Start = start;
        End = end;
        Color = from;
        EndColor = to;
        Fades = true;
    }

    /// <summary>Where it begins, in world space.</summary>
    public Vec3 Start { get; }

    /// <summary>Where it ends.</summary>
    public Vec3 End { get; }

    /// <summary>The color at the start, linear RGBA.</summary>
    public (float R, float G, float B, float A) Color { get; }

    /// <summary>The color at the end, which is the same one unless it fades.</summary>
    public (float R, float G, float B, float A) EndColor { get; }

    /// <summary>Whether the two colors are meant to be different.</summary>
    /// <remarks>
    /// Kept rather than compared, because a line fading from a color to the same color is a
    /// reasonable thing to ask for and would otherwise be silently turned into a plain one.
    /// </remarks>
    public bool Fades { get; }

    /// <summary>A line that fades from one color to another.</summary>
    /// <param name="start">Where it begins, in world space.</param>
    /// <param name="end">Where it ends.</param>
    /// <param name="from">The color at the start, linear RGBA.</param>
    /// <param name="to">The color at the end. Transparent makes a line run out.</param>
    public static GizmoSegment Fading(
        Vec3 start,
        Vec3 end,
        (float R, float G, float B, float A) from,
        (float R, float G, float B, float A) to) => new(start, end, from, to);
}
