using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One track of a grid, and how many times it repeats.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeGridTrack
{
    /// <summary>0 auto, 1 pixels, 2 percent, 3 a share of what is left, 4 min, 5 max.</summary>
    public int Kind;

    /// <summary>The number the kind reads, where it reads one.</summary>
    public float Value;

    /// <summary>A repeat count, -1 to fill the grid, -2 to fill it and drop the empty tracks.</summary>
    public int Repeat;
}
