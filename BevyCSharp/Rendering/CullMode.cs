namespace Bevy;

/// <summary>Which faces a material leaves undrawn.</summary>
public enum CullMode
{
    /// <summary>The ones facing away, which a closed mesh never shows. The usual choice.</summary>
    Back = 0,

    /// <summary>The ones facing the camera, which draws the inside of a closed mesh.</summary>
    Front = 1,

    /// <summary>Neither, for anything modeled as a single sheet.</summary>
    None = 2,
}
