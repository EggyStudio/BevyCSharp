namespace Bevy;

/// <summary>Which of a node's colors the theme sets, for <see cref="Feathers.ThemeColor"/>.</summary>
public enum ThemedPart
{
    /// <summary>Its background, Bevy's <c>ThemeBackgroundColor</c>.</summary>
    Background = 0,

    /// <summary>Its border, every side alike, Bevy's <c>ThemeBorderColor</c>.</summary>
    Border = 1,

    /// <summary>The text of its children, Bevy's <c>InheritableThemeTextColor</c>.</summary>
    Text = 2,

    /// <summary>Its own text, set on a span directly, Bevy's <c>ThemeTextColor</c>.</summary>
    OwnText = 3,
}
