using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// Whether any camera actually saw this entity last frame, the culled half of
/// <see cref="Visibility"/>.
/// </summary>
/// <remarks>
/// <para>
/// Written by Bevy after culling and overwritten every frame. An entity can be visible in the
/// hierarchy and still be false here, because it is off-screen or behind the camera, which makes
/// this the one to read before doing work that only matters when something is on screen.
/// </para>
/// <para>
/// The byte packs two frames, bit 0 for this one and bit 1 for the one before. Bevy keeps the
/// older bit as scratch space so it can tell "no longer seen by anything" from "was never seen",
/// and only then trip change detection.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct ViewVisibility : INativeComponent
{
    /// <summary>The raw byte Bevy stores: bit 0 this frame, bit 1 the previous frame.</summary>
    public byte Bits;

    /// <summary>True when some view rendered this entity this frame.</summary>
    public readonly bool IsVisible => (Bits & 1) != 0;

    /// <summary>True when some view rendered this entity the frame before.</summary>
    public readonly bool WasVisible => (Bits & 2) != 0;

    /// <summary>The engine's name for this component, which is how the generic API finds it.</summary>
    readonly string INativeComponent.NativeName => "ViewVisibility";

    /// <inheritdoc/>
    public readonly override string ToString() => $"ViewVisibility({IsVisible})";
}
