namespace Bevy;

/// <summary>
/// The display's scale factor changed.
/// </summary>
/// <remarks>
/// Sent when the window moves to a monitor with different scaling, or the setting changes. UI
/// sized in logical pixels follows on its own; anything measured in physical pixels does not.
/// </remarks>
/// <param name="ScaleFactor">Physical pixels per logical pixel.</param>
public readonly record struct WindowScaleFactorChanged(float ScaleFactor);
