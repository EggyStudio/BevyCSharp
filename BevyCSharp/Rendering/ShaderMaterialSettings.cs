namespace Bevy;

/// <summary>How a shader material is drawn, apart from its values.</summary>
public sealed class ShaderMaterialSettings
{
    /// <summary>The program that draws it. Required.</summary>
    public ShaderProgram Program { get; set; }

    /// <summary>What the renderer does where the material is not opaque.</summary>
    public AlphaMode Alpha { get; set; } = AlphaMode.Opaque;

    /// <summary>Where <see cref="AlphaMode.Mask"/> stops drawing.</summary>
    public float AlphaCutoff { get; set; } = 0.5f;

    /// <summary>Which faces are left undrawn.</summary>
    public CullMode Cull { get; set; } = CullMode.Back;

    /// <summary>How far toward the camera the depth is pushed, which stops a decal flickering.</summary>
    public float DepthBias { get; set; }
}
