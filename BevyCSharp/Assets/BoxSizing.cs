namespace Bevy;

/// <summary>
/// What the sizes on a node measure.
/// </summary>
/// <remarks>
/// Bevy's default is the border box, unlike the web's, because a node told to be a hundred pixels
/// wide and given a border is easier to place if it stays a hundred pixels wide.
/// </remarks>
public enum BoxSizing
{
    /// <summary>The size includes the padding and the border.</summary>
    BorderBox = 0,

    /// <summary>The size is the room left for the contents, with padding and border outside
    /// it.</summary>
    ContentBox = 1,
}
