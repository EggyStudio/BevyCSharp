using Bevy.Interop;

namespace Bevy;

/// <summary>How a 2D mesh's material reads its alpha.</summary>
public enum AlphaMode2d
{
    /// <summary>Ignores alpha, drawing every pixel solid.</summary>
    Opaque = 0,

    /// <summary>Draws a pixel or leaves it out at <see cref="ColorMaterialSettings.AlphaCutoff"/>.</summary>
    Mask = 1,

    /// <summary>Blends with what is behind, drawn after the solid meshes and sorted back to front.</summary>
    Blend = 2,
}
