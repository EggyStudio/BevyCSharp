using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>What one of Bevy's diagnostics holds, as the bridge reads it, NaN for a value it has none of.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeDiagnostic
{
    /// <summary>The last measurement.</summary>
    public double Value;

    /// <summary>The measurements smoothed over time.</summary>
    public double Smoothed;

    /// <summary>The mean of the measurements kept.</summary>
    public double Average;

    /// <summary>How many measurements are kept.</summary>
    public uint History;

    /// <summary>Non-zero where it is measured and logged.</summary>
    public uint Enabled;
}
