namespace Bevy;

public sealed class CameraSettings
{
    /// <summary>Perspective or orthographic.</summary>
    public CameraProjection Projection { get; set; } = CameraProjection.Perspective;

    /// <summary>Vertical field of view in degrees. Perspective only.</summary>
    /// <remarks>
    /// Bevy's default is 45. Larger sees more and exaggerates depth; much larger distorts at the
    /// edges of the picture.
    /// </remarks>
    public float FieldOfView { get; set; } = 45f;

    /// <summary>How many world units fit vertically. Orthographic only.</summary>
    /// <remarks>The width follows from the window, so the picture does not stretch when resized.</remarks>
    public float Height { get; set; } = 10f;

    /// <summary>Nearest visible distance.</summary>
    /// <remarks>
    /// Depth precision is spent between here and <see cref="Far"/>, and mostly near this end, so a
    /// very small value makes distant surfaces flicker against each other.
    /// </remarks>
    public float Near { get; set; } = 0.1f;

    /// <summary>Furthest visible distance. Ignored by an orthographic camera.</summary>
    public float Far { get; set; } = 1000f;

    /// <summary>What to do with the pixels already there.</summary>
    public ClearMode Clear { get; set; } = ClearMode.World;

    /// <summary>The color used when <see cref="Clear"/> is <see cref="ClearMode.Custom"/>.</summary>
    /// <remarks>Linear RGBA, not sRGB, so these are the numbers a shader works in.</remarks>
    public (float R, float G, float B, float A) ClearColor { get; set; } = (0f, 0f, 0f, 1f);

    /// <summary>Draw order. A camera with a higher order draws over one with a lower.</summary>
    public int Order { get; set; }

    /// <summary>
    /// The part of the window to draw into, in physical pixels, or null for all of it.
    /// </summary>
    /// <remarks>
    /// Splitscreen is two cameras, each given half the window. Physical pixels rather than logical
    /// ones, because a framebuffer is divided into those, so half a window is half its physical
    /// width whatever the display scaling.
    /// </remarks>
    public (uint X, uint Y, uint Width, uint Height)? Viewport { get; set; }

    /// <summary>
    /// Which render layers this camera sees, as a bit per layer. Zero means the default layer.
    /// </summary>
    /// <remarks>
    /// A camera draws an entity only where their layers overlap, which is how a minimap shows
    /// different things from the main view. Put entities on layers with
    /// <see cref="Render.SetLayers"/>.
    /// </remarks>
    public uint Layers { get; set; }
}
