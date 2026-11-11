using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One transition a state slot made. Mirrors <c>BcsStateTransition</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeStateTransition
{
    /// <summary>The slot, in the numbering every state entry point takes.</summary>
    public int Slot;

    /// <summary>One where it left a value, which <see cref="Exited"/> holds, and two where it entered one.</summary>
    public uint Flags;

    /// <summary>The value it left.</summary>
    public int Exited;

    /// <summary>The value it entered.</summary>
    public int Entered;
}
