namespace Bevy;

/// <summary>
/// Per-system on/off state for <see cref="ToggleKeyAttribute"/>, keyed by system name.
/// </summary>
/// <remarks>
/// Stored as a world resource so the state survives across frames and can be inspected or
/// driven from elsewhere: a debug menu, a save file, or a test.
/// </remarks>
public sealed class SystemToggleRegistry
{
    private readonly Dictionary<string, bool> _states = [];

    /// <summary>The state for <paramref name="id"/>, or <paramref name="defaultEnabled"/> if unset.</summary>
    public bool Get(string id, bool defaultEnabled = true) =>
        _states.TryGetValue(id, out var value) ? value : defaultEnabled;

    /// <summary>Sets the state for <paramref name="id"/>.</summary>
    public void Set(string id, bool enabled) => _states[id] = enabled;

    /// <summary>Flips the state for <paramref name="id"/>.</summary>
    public void Flip(string id, bool defaultEnabled = true) =>
        _states[id] = !Get(id, defaultEnabled);

    /// <summary>Every recorded toggle, for diagnostics.</summary>
    public IReadOnlyDictionary<string, bool> States => _states;
}
