using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>What one of Bevy's widgets reported, as the bridge reports it, every kind in one shape.</summary>
/// <remarks>
/// The bridge's <c>BcsWidgetEvent</c>, field for field, which <c>WidgetTests</c> holds to the
/// offsets the bridge asserts. A field a kind has no use for is zero.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct NativeWidgetEvent
{
    /// <summary>The widget it happened to.</summary>
    public ulong Entity;

    /// <summary>The value of a change that names an entity, the radio button a group's choice moved to.</summary>
    public ulong Other;

    /// <summary>Which of the five kinds, in <c>WidgetEvents</c>' order.</summary>
    public int Kind;

    /// <summary>The value of a change that is a number, a slider's.</summary>
    public float Value;

    /// <summary>The value of a change that is yes or no, one or zero.</summary>
    public uint Flag;

    /// <summary>One where the change is the last of its interaction.</summary>
    public uint IsFinal;

    /// <summary>What a menu is asked to do, as <see cref="MenuAction"/> numbers it.</summary>
    public int Action;

    /// <summary>For a menu opened, which item takes the focus, as <see cref="NavAction"/> numbers it.</summary>
    public int Navigation;
}
