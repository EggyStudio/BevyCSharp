namespace Bevy;

/// <summary>
/// Bevy's camera controllers an app adds, through <see cref="Config.CameraControllers"/>.
/// </summary>
/// <remarks>
/// Each is a plugin Bevy's default plugins leave out, which moves only a camera given its
/// controller, so an app asks for those it uses. A bridge with the renderer is needed.
/// </remarks>
[Flags]
public enum CameraControllers
{
    /// <summary>None of them.</summary>
    None = 0,

    /// <summary>
    /// The free camera, moved with the keys and turned with the mouse, which
    /// <see cref="Render.SetFreeCamera"/> puts on a camera (<c>FreeCameraPlugin</c>).
    /// </summary>
    Free = 1,

    /// <summary>
    /// The pan camera for a 2D view, moved and turned with the keys, zoomed with the wheel and
    /// dragged with a button, which <see cref="Render.SetPanCamera"/> puts on a camera
    /// (<c>PanCameraPlugin</c>).
    /// </summary>
    Pan = 2,

    /// <summary>
    /// The pan-orbit camera, which pans with the left button, orbits with the right and zooms
    /// with the wheel about the point under the pointer, given to a camera as the reflected
    /// <c>PanOrbitCameraRef</c> (<c>DefaultPanOrbitCameraPlugins</c>).
    /// </summary>
    /// <remarks>
    /// The point under the pointer is found by picking, so <see cref="Config.MeshPicking"/> lets
    /// the camera orbit the mesh the pointer is over, and without it the camera orbits a point at
    /// the depth it last orbited at. The pointer's input is the mapping Bevy's own examples carry.
    /// </remarks>
    PanOrbit = 4,
}
