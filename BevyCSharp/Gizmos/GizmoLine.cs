namespace Bevy;

/// <summary>What a gizmo line is drawn as, along its length.</summary>
/// <remarks>
/// The way one meaning is told from another without spending a second color on it, which matters
/// where the color already says something else.
/// </remarks>
public enum GizmoLine
{
    /// <summary>One unbroken run.</summary>
    Solid = 0,

    /// <summary>A row of dots.</summary>
    Dotted = 1,

    /// <summary>Alternating runs and gaps, each measured in line widths.</summary>
    Dashed = 2,
}
