using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>A sky computed from light scattering through the air.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeAtmosphereConfig
{
    /// <summary>Non-zero to draw the sky from this camera.</summary>
    public int Enabled;

    /// <summary>Density multiplier for the air; 0 for earth's own.</summary>
    public float Density;

    /// <summary>Planet scale against the scene; 0 for one.</summary>
    public float Scale;

    /// <summary>How far the haze in front of the scene is computed, in meters; 0 for Bevy's.</summary>
    public float HazeDistance;

    /// <summary>How finely the sky is computed: 0 Bevy's own, 1 cheaper, 2 finer.</summary>
    public int Quality;

    /// <summary>How much light the ground bounces back, or 0 for the medium's own.</summary>
    public float GroundAlbedo;

    /// <summary>Whose air: 0 earth's, 1 Mars's.</summary>
    public int Planet;

    /// <summary>The asset key of the picture Mars's dust scatters by.</summary>
    public int DustPhase;

    /// <summary>How the sky is computed: 0 from lookup textures, 1 by marching rays.</summary>
    public int Method;
}
