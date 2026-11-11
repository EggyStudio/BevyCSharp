namespace Bevy;

/// <summary>How a Feathers button looks, Bevy's <c>ButtonVariant</c>.</summary>
public enum ButtonVariant
{
    /// <summary>The usual button.</summary>
    Normal = 0,

    /// <summary>A button in the theme's accent color, for the one a dialog or a form expects.</summary>
    Primary = 1,

    /// <summary>A button whose background shows only while the pointer is over it or it is pressed.</summary>
    Plain = 2,
}
