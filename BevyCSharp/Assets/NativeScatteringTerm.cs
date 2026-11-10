using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One gas or dust of an air of the caller's own, as the bridge reads it.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeScatteringTerm
{
    /// <summary>How much red, green and blue light it absorbs a meter at its densest.</summary>
    public Vec3 Absorption;

    /// <summary>How much red, green and blue light it scatters a meter at its densest.</summary>
    public Vec3 Scattering;

    /// <summary>How it thins with height, 0 linearly, 1 exponentially and 2 as a tent.</summary>
    public int Falloff;

    /// <summary>The exponential falloff's scale, or the tent's center.</summary>
    public float FalloffA;

    /// <summary>The tent's width.</summary>
    public float FalloffB;

    /// <summary>How it scatters by direction, 0 evenly, 1 as a gas and 2 as a dust.</summary>
    public int Phase;

    /// <summary>For a dust, how far its scattering leans forward.</summary>
    public float Asymmetry;
}
