namespace Bevy;

/// <summary>
/// What kind of light to spawn and how it behaves.
/// </summary>
/// <remarks>
/// A directional light is aimed by its <see cref="Transform"/> and ignores position; a point
/// light is positioned and ignores aim; a spot light uses both.
/// </remarks>
public sealed class LightSettings
{
    /// <summary>Which light to spawn.</summary>
    public LightKind Kind { get; set; } = LightKind.Directional;

    /// <summary>Illuminance in lux for a directional light, luminous power in lumens otherwise.</summary>
    public float Intensity { get; set; } = 10_000f;

    /// <summary>Linear RGB. White by default.</summary>
    public (float R, float G, float B) Color { get; set; } = (1f, 1f, 1f);

    /// <summary>How far the light reaches, in world units. Point and spot only.</summary>
    public float Range { get; set; } = 20f;

    /// <summary>
    /// Radius of the emitting sphere. Point and spot only.
    /// </summary>
    /// <remarks>
    /// A light with no size casts a shadow with a hard edge, which reads as artificial. Giving it
    /// a radius softens the edge.
    /// </remarks>
    public float Radius { get; set; }

    /// <summary>Whether the light casts shadows.</summary>
    /// <remarks>Shadows cost a render pass per light, so this is the first thing to turn off.</remarks>
    public bool Shadows { get; set; } = true;

    /// <summary>Whether the light casts contact shadows as well, on cameras that draw them.</summary>
    /// <remarks>
    /// A contact shadow is traced a short way through the depth buffer toward the light, for the
    /// shadow a shadow map is too coarse to hold where two things touch. It is drawn only by a
    /// camera given <see cref="Render.SetContactShadows"/>, and needs no shadow map of its own.
    /// </remarks>
    public bool ContactShadows { get; set; }

    /// <summary>Radians from the axis within which a spot light is at full brightness.</summary>
    public float InnerAngle { get; set; }

    /// <summary>Radians from the axis at which a spot light has fallen to nothing.</summary>
    /// <remarks>Must be under a quarter turn, and at least <see cref="InnerAngle"/>.</remarks>
    public float OuterAngle { get; set; } = MathF.PI / 8f;

    /// <summary>
    /// How far a surface is pushed away before it is tested against this light's shadow map.
    /// </summary>
    /// <remarks>
    /// The fix for shadow acne, the stippled self-shadowing a surface shows when the shadow map
    /// is too coarse to tell it apart from itself. Raising it trades that for a shadow that
    /// starts slightly away from what casts it. Bevy's default is 0.02.
    /// </remarks>
    public float ShadowDepthBias { get; set; } = 0.02f;

    /// <summary>
    /// The same, measured along the surface normal.
    /// </summary>
    /// <remarks>
    /// Handles what depth bias alone does not, a surface lit at a glancing angle, where a
    /// small depth error covers a long distance. Bevy's default is 0.6 for a directional light
    /// and 0.6 for the others.
    /// </remarks>
    public float ShadowNormalBias { get; set; } = 0.6f;
}
