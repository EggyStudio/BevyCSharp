using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>What kind of light to spawn and how it behaves.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeLightConfig
{
    /// <summary>0 directional, 1 point, 2 spot.</summary>
    public int Kind;

    /// <summary>Lux for a directional light, lumens for the other two.</summary>
    public float Intensity;

    /// <summary>Linear red.</summary>
    public float ColorR;

    /// <summary>Linear green.</summary>
    public float ColorG;

    /// <summary>Linear blue.</summary>
    public float ColorB;

    /// <summary>How far the light reaches. Point and spot only.</summary>
    public float Range;

    /// <summary>Radius of the emitting sphere. Point and spot only.</summary>
    public float Radius;

    /// <summary>Non-zero to cast shadows.</summary>
    public int Shadows;

    /// <summary>Radians of full brightness about the axis. Spot only.</summary>
    public float InnerAngle;

    /// <summary>Radians at which a spot light has fallen to nothing.</summary>
    public float OuterAngle;

    /// <summary>Depth bias applied before the shadow test.</summary>
    public float ShadowDepthBias;

    /// <summary>Bias along the surface normal.</summary>
    public float ShadowNormalBias;
}
