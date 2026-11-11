using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>What <see cref="Animation.StateOf"/> reads, in the bridge's layout.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeAnimationState
{
    /// <summary>The clip's number, or -1.</summary>
    public int Clip;

    /// <summary>Seconds into it.</summary>
    public float Seconds;

    /// <summary>How fast it plays.</summary>
    public float Speed;

    /// <summary>Non-zero while held.</summary>
    public int Paused;

    /// <summary>Non-zero once a clip that plays once has ended.</summary>
    public int Finished;

    /// <summary>How many times it has come round.</summary>
    public uint Completions;
}
