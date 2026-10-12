namespace Bevy;

/// <summary>How a camera traces contact shadows. See <see cref="Render.SetContactShadows"/>.</summary>
/// <param name="Steps">How many steps a ray takes. More finds thinner gaps and costs more.</param>
/// <param name="Thickness">How thick, in world units, a surface in the depth buffer is taken to be.</param>
/// <param name="Length">How far, in world units, a ray goes toward the light.</param>
public sealed record ContactShadowSettings(uint Steps = 16, float Thickness = 0.1f, float Length = 0.3f);
