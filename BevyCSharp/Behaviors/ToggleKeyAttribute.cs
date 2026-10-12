namespace Bevy;

/// <summary>
/// Binds a shortcut that switches the system on and off, with no boilerplate.
/// </summary>
/// <remarks>
/// <para>
/// Each press of the shortcut flips the state, which lives in <see cref="SystemToggleRegistry"/>.
/// Debug overlays use this, one attribute instead of a resource, a run condition and a key handler.
/// </para>
/// <para>
/// <see cref="KeyModifier"/> is a flags enum, so a shortcut can require any number of modifiers at
/// once. Each flag is side-agnostic, as a shortcut normally is; to pin one side, or to build a
/// chord out of an ordinary key, write the check yourself with <see cref="Input.AllKeysDown"/>
/// behind a <see cref="RunIfAttribute"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [ToggleKey(Key.F3)]                                       // F3
/// [ToggleKey(Key.F3, KeyModifier.Ctrl)]                     // Ctrl + F3, either Ctrl
/// [ToggleKey(Key.F3, KeyModifier.Ctrl | KeyModifier.Shift)] // Ctrl + Shift + F3
/// [ToggleKey(Key.F3, DefaultEnabled = false)]               // starts off
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ToggleKeyAttribute : Attribute
{
    /// <summary>The key whose press flips the system on or off.</summary>
    public Key Key { get; }

    /// <summary>Modifiers that must be held with <see cref="Key"/>. Combine with <c>|</c>.</summary>
    public KeyModifier Modifiers { get; }

    /// <summary>Whether the system starts enabled. Defaults to <see langword="true"/>.</summary>
    public bool DefaultEnabled { get; init; } = true;

    /// <summary>Binds <paramref name="key"/>, optionally with modifiers held.</summary>
    /// <param name="key">The key whose press flips the system.</param>
    /// <param name="modifiers">Modifiers that must be held; combine them with <c>|</c>.</param>
    public ToggleKeyAttribute(Key key, KeyModifier modifiers = KeyModifier.None)
    {
        Key = key;
        Modifiers = modifiers;
    }
}
