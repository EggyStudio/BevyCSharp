namespace Bevy;

/// <summary>
/// How a scaled picture is fitted to the size it is drawn at.
/// </summary>
/// <remarks>
/// Fitting keeps the whole picture and leaves empty edges, which is letterboxing. Filling covers
/// the whole size and loses what hangs over the edges. The rest of the name says which part is
/// kept when they do not agree.
/// </remarks>
public enum SpriteScaling
{
    /// <summary>The whole picture, centered, with empty edges where it does not reach.</summary>
    FitCenter = 0,

    /// <summary>The whole picture, against the left and top edges.</summary>
    FitStart = 1,

    /// <summary>The whole picture, against the right and bottom edges.</summary>
    FitEnd = 2,

    /// <summary>Covering the size, centered, losing what hangs over.</summary>
    FillCenter = 3,

    /// <summary>Covering the size, keeping the left and top.</summary>
    FillStart = 4,

    /// <summary>Covering the size, keeping the right and bottom.</summary>
    FillEnd = 5,
}
