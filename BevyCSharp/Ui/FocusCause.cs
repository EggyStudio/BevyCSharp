namespace Bevy;

/// <summary>How an entity came to have the input focus, as Bevy's <c>FocusCause</c>.</summary>
public enum FocusCause
{
    /// <summary>Navigated to, by the keyboard, a pad or the game.</summary>
    Navigated,

    /// <summary>Pressed into with the pointer's primary button.</summary>
    Pressed,
}
