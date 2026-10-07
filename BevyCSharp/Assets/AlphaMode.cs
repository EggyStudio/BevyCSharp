namespace Bevy;

/// <summary>What a material does where it is not fully opaque.</summary>
public enum AlphaMode
{
    /// <summary>Ignore alpha entirely. The cheapest, and the right default.</summary>
    Opaque = 0,

    /// <summary>
    /// Draw a pixel or skip it, deciding at <see cref="MaterialSettings.AlphaCutoff"/>.
    /// </summary>
    /// <remarks>
    /// Suits foliage and chain-link fences. It keeps the depth buffer honest, so nothing has to be
    /// sorted, at the cost of a hard edge.
    /// </remarks>
    Mask = 1,

    /// <summary>Blend with what is behind.</summary>
    /// <remarks>
    /// Real transparency, and the expensive one. Blended surfaces are drawn after everything
    /// else and sorted back to front, so two of them overlapping can still be drawn in the wrong
    /// order.
    /// </remarks>
    Blend = 2,

    /// <summary>Add to what is behind, which never darkens it. For fire, glows and holograms.</summary>
    Add = 3,

    /// <summary>Multiply what is behind, which never lightens it. For stained glass and tints.</summary>
    Multiply = 4,

    /// <summary>Blend with color that has already been multiplied by its alpha.</summary>
    /// <remarks>
    /// Suits a texture exported premultiplied, and lets one material be partly additive. A pixel
    /// with color and zero alpha adds, and one with full alpha covers.
    /// </remarks>
    Premultiplied = 5,
}
