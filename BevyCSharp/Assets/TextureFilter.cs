namespace Bevy;

/// <summary>How a texture is filtered when it does not land on pixel boundaries.</summary>
public enum TextureFilter
{
    /// <summary>Take the nearest pixel. Bevy's default, and suited to pixel art.</summary>
    Nearest = 0,

    /// <summary>Blend between pixels, which suits everything else.</summary>
    Linear = 1,
}
