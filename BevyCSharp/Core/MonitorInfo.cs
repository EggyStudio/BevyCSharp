using Bevy.Interop;

namespace Bevy;

/// <summary>One of the monitors the platform reports.</summary>
/// <remarks>
/// The name is read separately, with <see cref="Window.MonitorName"/>, because it is text and the
/// rest is not.
/// </remarks>
/// <param name="Width">Width in physical pixels.</param>
/// <param name="Height">Height in physical pixels.</param>
/// <param name="X">Where its left edge sits in the desktop's coordinate space.</param>
/// <param name="Y">The same, vertically.</param>
/// <param name="RefreshHz">Refresh rate in hertz, or zero when the platform does not report one.</param>
/// <param name="ScaleFactor">Physical pixels per logical pixel.</param>
public readonly record struct MonitorInfo(
    uint Width,
    uint Height,
    int X,
    int Y,
    float RefreshHz,
    float ScaleFactor);
