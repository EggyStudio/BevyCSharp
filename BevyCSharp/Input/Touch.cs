namespace Bevy;

/// <summary>One finger on a touchscreen.</summary>
/// <param name="Id">Identifies this finger while it stays down.</param>
/// <param name="X">Position in physical window pixels.</param>
/// <param name="Y">Position in physical window pixels.</param>
/// <param name="Phase">Where the touch is in its life.</param>
public readonly record struct Touch(ulong Id, float X, float Y, TouchPhase Phase);
