namespace Bevy;

/// <summary>
/// How a node spreads its children along its own axis.
/// </summary>
/// <remarks>
/// The main axis is <see cref="UiSettings.Direction"/>. `Start` and `End` are the edges of the
/// node itself; the `Flex` pair follows the direction instead, so they swap when it is reversed.
/// </remarks>
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
