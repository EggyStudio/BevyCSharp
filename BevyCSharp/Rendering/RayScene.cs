namespace Bevy;

/// <summary>A scene rays are traced through. See <see cref="Shaders.CreateRayScene"/>.</summary>
/// <param name="Key">What the scene is known by.</param>
public readonly record struct RayScene(int Key)
{
    /// <summary>Whether this names a scene rather than being the default.</summary>
    public bool IsValid => Key > 0;
}
