namespace Bevy;

/// <summary>
/// Which gizmo groups a setting applies to.
/// </summary>
/// <remarks>
/// <para>
/// The first two are the split a shape's <c>inFront</c> chooses between, seen from the other end.
/// A handle, an outline or a marker is drawn about the scene and has to be reachable, and a grid, a
/// path or a wireframe is drawn in it and has to be behind what is in front of it. Because the two
/// kinds of drawing already fall on opposite sides of that line, it doubles as the category a game
/// turns one kind of debug drawing off by, or draws thicker than the other.
/// </para>
/// <para>
/// The rest are Bevy's own groups for what it draws itself, the shapes of lights and the bounding
/// boxes, set apart so that their lines can be set without touching a game's.
/// </para>
/// </remarks>
public enum GizmoGroup
{
    /// <summary>Both of the groups shapes are drawn in here, <see cref="Behind"/> and <see cref="InFront"/>.</summary>
    Both = 0,

    /// <summary>The group the scene can hide, which is where a grid or a path is drawn.</summary>
    Behind = 1,

    /// <summary>The group nothing can hide, which is where a handle or a marker is drawn.</summary>
    InFront = 2,

    /// <summary>Bevy's own group for the shapes of lights, which <see cref="Gizmos.ShowLights"/> turns on.</summary>
    Lights = 3,

    /// <summary>Bevy's own group for bounding boxes, which <see cref="Gizmos.ShowBounds"/> turns on.</summary>
    Bounds = 4,

    /// <summary>Every group there is, these and any of Bevy's own, for a setting such as a depth bias toggled for everything.</summary>
    All = 5,
}
