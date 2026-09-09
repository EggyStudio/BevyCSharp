namespace Bevy.Interop;

/// <summary>
/// Something the page reported, and the element it happened to.
/// </summary>
/// <remarks>
/// Laid out to match <c>BcsDomEvent</c> in <c>native/bevy_csharp/src/dom.rs</c>.
/// </remarks>
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct NativeDomEvent
{
    /// <summary>Which kind of thing happened, as <see cref="Bevy.DomEventKind"/> names them.</summary>
    public int Kind;

    /// <summary>The element it happened to.</summary>
    public ulong Node;
}
