namespace Bevy;

/// <summary>
/// Modifier keys that must be held for a shortcut to fire. Combine them with <c>|</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is a flags enum, so a shortcut can require any number of modifiers at once, as
/// <c>KeyModifier.Ctrl | KeyModifier.Shift</c>.
/// </para>
/// <para>
/// Each flag is side-agnostic, so <see cref="Ctrl"/> is satisfied by either Ctrl key, which
/// is almost always what a shortcut means. That matches winit's <c>ModifiersState</c>, the
/// layer Bevy's own windowing sits on. Bevy itself has no modifier type at all, exposing
/// only the individual <c>KeyCode</c>s, so requiring one specific side means naming that key
/// directly with <see cref="Input.AllKeysDown"/>.
/// </para>
/// </remarks>
[Flags]
public enum KeyModifier
{
    /// <summary>No modifier required.</summary>
    None = 0,

    /// <summary>Either Ctrl key must be held.</summary>
    Ctrl = 1 << 0,

    /// <summary>Either Shift key must be held.</summary>
    Shift = 1 << 1,

    /// <summary>Either Alt key must be held.</summary>
    Alt = 1 << 2,

    /// <summary>Either Super, Windows or Command key must be held.</summary>
    Super = 1 << 3,
}
