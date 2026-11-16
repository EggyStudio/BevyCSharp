namespace Bevy;

/// <summary>
/// Per-system on/off state for <see cref="ToggleKeyAttribute"/>, keyed by system name.
/// </summary>
/// <remarks>
/// Stored as a world resource so the state survives across frames and can be inspected or
/// set from elsewhere, such as a debug menu, a save file or a test.
/// </remarks>
public sealed class SystemToggleRegistry
{
    private readonly Dictionary<string, bool> _states = [];

    /// <summary>The frame each toggle a key flipped was last flipped on.</summary>
    private readonly Dictionary<string, ulong> _flipped = [];

    /// <summary>The state for <paramref name="id"/>, or <paramref name="defaultEnabled"/> if unset.</summary>
    public bool Get(string id, bool defaultEnabled = true) =>
        _states.TryGetValue(id, out var value) ? value : defaultEnabled;

    /// <summary>Sets the state for <paramref name="id"/>.</summary>
    public void Set(string id, bool enabled) => _states[id] = enabled;

    /// <summary>Flips the state for <paramref name="id"/>.</summary>
    public void Flip(string id, bool defaultEnabled = true) =>
        _states[id] = !Get(id, defaultEnabled);

    /// <summary>
    /// Flips the state for <paramref name="id"/> where it was not flipped on <paramref name="frame"/>
    /// already, as a key's press flips it.
    /// </summary>
    /// <remarks>
    /// Once a frame, since every system of one name asks its toggle, two behaviors of one name in
    /// two namespaces or one method name in two stages, and a fixed step may ask it more than once
    /// a frame, where a flip at each ask would turn it off and on again.
    /// </remarks>
    internal void FlipOnce(string id, bool defaultEnabled, ulong frame)
    {
        if (_flipped.TryGetValue(id, out var last) && last == frame) return;
        _flipped[id] = frame;
        Flip(id, defaultEnabled);
    }

    /// <summary>Every recorded toggle, for diagnostics.</summary>
    public IReadOnlyDictionary<string, bool> States => _states;
}
