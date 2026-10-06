namespace Bevy;

/// <summary>Which box a node that clips its overflow clips at.</summary>
/// <remarks>
/// The three boxes a node has: the content it holds, that plus its padding, and that plus its
/// border. Which one is clipped at decides where a scrolling list's rows disappear.
/// </remarks>
public enum UiClipBox
{
    /// <summary>Clip at the content box, inside the padding.</summary>
    Content = 0,

    /// <summary>Clip at the padding box, which is Bevy's own answer.</summary>
    Padding = 1,

    /// <summary>Clip at the border box, so the border itself is inside what is kept.</summary>
    Border = 2,
}
