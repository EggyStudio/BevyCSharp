namespace Bevy;

/// <summary>What a set of <see cref="TreeTextures"/> is for.</summary>
public enum TreeTextureKind
{
    /// <summary>A bark that tiles round a stem and along it.</summary>
    Bark,

    /// <summary>An atlas of leaves with their opacity, cut out on the cards a tree's leaves are drawn on.</summary>
    Leaves,

    /// <summary>A card of grass blades with their opacity, the blades' roots along its bottom edge, cut out on the cards grass is drawn on.</summary>
    Grass,
}
