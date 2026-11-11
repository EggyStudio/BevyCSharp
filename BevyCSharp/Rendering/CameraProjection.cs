namespace Bevy;

/// <summary>How a camera turns the world into a picture.</summary>
public enum CameraProjection
{
    /// <summary>Things shrink with distance, as an eye sees them.</summary>
    Perspective = 0,

    /// <summary>
    /// Parallel lines stay parallel and distance does not shrink anything, as an isometric or a
    /// top-down view needs.
    /// </summary>
    Orthographic = 1,
}
