namespace Bevy;

/// <summary>What a scroll is counted in, as Bevy's <c>MouseScrollUnit</c>.</summary>
public enum ScrollUnit
{
    /// <summary>Lines, as a mouse wheel turns in notches.</summary>
    Line,

    /// <summary>Pixels, as a touchpad scrolls smoothly.</summary>
    Pixel,
}
