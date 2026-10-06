namespace Bevy;

/// <summary>How geometry drawn on a camera combines with the picture.</summary>
public enum DrawBlend
{
    /// <summary>Replaces what is there.</summary>
    Opaque = 0,

    /// <summary>Over what is there, by the fragment's alpha.</summary>
    Alpha = 1,

    /// <summary>Added to what is there, for anything glowing.</summary>
    Add = 2,
}
