namespace Bevy;

/// <summary>How two gizmo lines meet at a corner.</summary>
/// <remarks>
/// Only visible on a shape whose lines meet, which is every closed shape and no single segment. At
/// a thin width the corners are too small to tell apart, so this is for the thick lines an overlay
/// drawn to be read at a glance uses.
/// </remarks>
public enum GizmoJoint
{
    /// <summary>Nothing is drawn, so a thick corner has a notch out of it.</summary>
    None = 0,

    /// <summary>Both lines are carried on until they meet at a point.</summary>
    Miter = 1,

    /// <summary>A rounded corner, drawn with as many triangles as it is given.</summary>
    Round = 2,

    /// <summary>A straight line across the gap between the two ends.</summary>
    Bevel = 3,
}
