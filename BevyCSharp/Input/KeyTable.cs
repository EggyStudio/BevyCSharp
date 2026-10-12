namespace Bevy;

/// <summary>Facts about the key table shared with the native bridge.</summary>
public static class KeyTable
{
    /// <summary>
    /// Number of distinct keys with a reserved bit. Must equal <c>KEY_COUNT</c> in the native
    /// crate, or the two sides will disagree about which bit means which key.
    /// </summary>
    public const int Count = 104;

    /// <summary>True when <paramref name="key"/> has a bit in the input bitsets.</summary>
    public static bool IsMapped(Key key) => (int)key >= 0 && (int)key < Count;
}
