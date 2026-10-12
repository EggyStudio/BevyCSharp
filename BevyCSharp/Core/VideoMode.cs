using Bevy.Interop;

namespace Bevy;

/// <summary>
/// One video mode a monitor can be driven at.
/// </summary>
/// <remarks>
/// A resolution, a color depth and a refresh rate together, which exclusive fullscreen takes the
/// screen over with. A monitor offers a fixed list of these and can be driven at no others, so a
/// settings screen offers what <see cref="Window.MonitorModes"/> returns rather than a pair of
/// number boxes.
/// </remarks>
/// <param name="Width">Width in physical pixels.</param>
/// <param name="Height">Height in physical pixels.</param>
/// <param name="BitDepth">Bits per pixel.</param>
/// <param name="RefreshHz">Refresh rate in hertz.</param>
public readonly record struct VideoMode(
    uint Width,
    uint Height,
    uint BitDepth,
    float RefreshHz);
