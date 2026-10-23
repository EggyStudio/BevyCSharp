namespace Bevy;

/// <summary>
/// Where the scene's distance field darkens a directional light's contact shadows, which
/// <see cref="Render.SetSceneFieldShadows"/> chooses.
/// </summary>
public enum FieldShadows
{
    /// <summary>
    /// Past the shadow map's reach alone, so a shadow the map holds keeps its hard edge and the
    /// ground beyond the map is still shadowed, the default.
    /// </summary>
    PastShadowMap = 0,

    /// <summary>Everywhere the field reaches, beside the shadow map's own shadows.</summary>
    Everywhere = 1,

    /// <summary>Nowhere, the shadow map's shadows alone.</summary>
    Off = 2,
}
