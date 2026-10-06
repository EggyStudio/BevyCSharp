namespace Bevy;

/// <summary>What happens outside a texture's zero-to-one range.</summary>
public enum TextureWrap
{
    /// <summary>Hold the edge pixel. Bevy's default, and suited to a decal or a skybox.</summary>
    Clamp = 0,

    /// <summary>Start over, so the texture tiles.</summary>
    Repeat = 1,

    /// <summary>Tile, flipping every other repeat so the seams line up.</summary>
    Mirror = 2,
}
