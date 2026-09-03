using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One thing a UI widget reported.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeUiEvent
{
    /// <summary>0 click, 1 value changed, 2 submit, 3 focus.</summary>
    public int Kind;

    /// <summary>The element it happened to.</summary>
    public ulong Entity;
}
