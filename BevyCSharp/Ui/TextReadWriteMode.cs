namespace Bevy;

/// <summary>
/// Whether a text field takes changes, for <see cref="UiEditableTextSettings.Mode"/>, as Bevy's
/// <c>TextReadWriteMode</c>.
/// </summary>
public enum TextReadWriteMode
{
    /// <summary>Typed into, as a field usually is.</summary>
    Editable = 0,

    /// <summary>
    /// Its cursor moved and its text selected and copied, with nothing typed, pasted or cut
    /// changing it, for a value the player reads and copies from.
    /// </summary>
    ReadOnly = 1,

    /// <summary>
    /// Shown alone, its cursor and selection still, as Bevy's number input shows its value while
    /// it is dragged.
    /// </summary>
    Static = 2,
}
