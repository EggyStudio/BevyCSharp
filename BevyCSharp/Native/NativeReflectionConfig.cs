namespace Bevy.Interop;

/// <summary>How screen-space reflections march. Mirrors <c>BcsReflectionConfig</c>.</summary>
public struct NativeReflectionConfig
{
    /// <summary>Roughness where reflections start to fade in.</summary>
    public float MinRoughnessStart;

    /// <summary>Roughness where they are whole.</summary>
    public float MinRoughnessFull;

    /// <summary>Roughness where they start to fade out.</summary>
    public float MaxRoughnessStart;

    /// <summary>Roughness where they are gone.</summary>
    public float MaxRoughnessEnd;

    /// <summary>Where they stop at the edge of the picture.</summary>
    public float EdgeGone;

    /// <summary>Where they are whole at the edge of the picture.</summary>
    public float EdgeFull;

    /// <summary>How thick what the depth buffer holds is taken to be.</summary>
    public float Thickness;

    /// <summary>Steps of the first march.</summary>
    public uint LinearSteps;

    /// <summary>How the steps spread out.</summary>
    public float LinearExponent;

    /// <summary>Steps of the bisection after a hit.</summary>
    public uint BisectionSteps;

    /// <summary>Non-zero to refine the hit with the secant method.</summary>
    public int UseSecant;
}
