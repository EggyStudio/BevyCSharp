using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>Something a pointer did, as the bridge reports it, every kind in one shape.</summary>
/// <remarks>
/// The bridge's <c>BcsPointerEvent</c>, field for field, which <c>NativePointerEventLayoutTests</c>
/// holds to the offsets the bridge asserts. A field a kind has no use for is zero.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct NativePointerEvent
{
    /// <summary>The entity it happened to.</summary>
    public ulong Entity;

    /// <summary>Which finger for a touch, the last eight bytes of the identifier for a pointer of the game's own.</summary>
    public ulong PointerNumber;

    /// <summary>The camera the hit was seen through.</summary>
    public ulong HitCamera;

    /// <summary>The entity dragged, or the one dropped.</summary>
    public ulong Other;

    /// <summary>How long a click's button was held, in seconds.</summary>
    public double Duration;

    /// <summary>Which of the seventeen kinds, in <c>PointerEvents</c>' order.</summary>
    public int Kind;

    /// <summary>Mouse, touch or a pointer of the game's own, as <see cref="PointerKind"/> numbers them.</summary>
    public int PointerKind;

    /// <summary>The button, as <see cref="PointerButton"/> numbers them.</summary>
    public int Button;

    /// <summary>The count of a press or a click.</summary>
    public uint Count;

    /// <summary>Where the pointer is.</summary>
    public float PositionX, PositionY;

    /// <summary>How deep the hit is.</summary>
    public float HitDepth;

    /// <summary>One where the hit has a position, two where it has a normal, both added.</summary>
    public uint HitFlags;

    /// <summary>Where the hit is.</summary>
    public float HitPositionX, HitPositionY, HitPositionZ;

    /// <summary>Which way the surface hit faces.</summary>
    public float HitNormalX, HitNormalY, HitNormalZ;

    /// <summary>How far the pointer moved.</summary>
    public float DeltaX, DeltaY;

    /// <summary>How far a drag has gone.</summary>
    public float DistanceX, DistanceY;

    /// <summary>How far a scroll went.</summary>
    public float ScrollX, ScrollY;

    /// <summary>What a scroll is counted in, as <see cref="ScrollUnit"/> numbers them.</summary>
    public int ScrollUnit;

    /// <summary>Whether an enter is, or a leave was, in the entity's own bounds.</summary>
    public uint InBounds;
}
