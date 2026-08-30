namespace Bevy;

/// <summary>
/// A keyboard key.
/// </summary>
/// <remarks>
/// <para>
/// These values are bit indices into the input bitsets the engine sends across each frame, not
/// scancodes. The identical list, in the identical order, is declared by the key table in the
/// native crate (<c>native/bevy_csharp/src/input.rs</c>), which maps Bevy's <c>KeyCode</c> onto
/// these positions. Adding a key means adding it to both lists at the same position.
/// </para>
/// <para>
/// Names follow Bevy's <c>KeyCode</c> so the correspondence is obvious. The aliases at the end
/// are the same values under the shorter spellings that read better in an attribute.
/// </para>
/// </remarks>
public enum Key
{

    // Letters
    /// <summary>The A key.</summary>
    A = 0,

    /// <summary>The B key.</summary>
    B = 1,

    /// <summary>The C key.</summary>
    C = 2,

    /// <summary>The D key.</summary>
    D = 3,

    /// <summary>The E key.</summary>
    E = 4,

    /// <summary>The F key.</summary>
    F = 5,

    /// <summary>The G key.</summary>
    G = 6,

    /// <summary>The H key.</summary>
    H = 7,

    /// <summary>The I key.</summary>
    I = 8,

    /// <summary>The J key.</summary>
    J = 9,

    /// <summary>The K key.</summary>
    K = 10,

    /// <summary>The L key.</summary>
    L = 11,

    /// <summary>The M key.</summary>
    M = 12,

    /// <summary>The N key.</summary>
    N = 13,

    /// <summary>The O key.</summary>
    O = 14,

    /// <summary>The P key.</summary>
    P = 15,

    /// <summary>The Q key.</summary>
    Q = 16,

    /// <summary>The R key.</summary>
    R = 17,

    /// <summary>The S key.</summary>
    S = 18,

    /// <summary>The T key.</summary>
    T = 19,

    /// <summary>The U key.</summary>
    U = 20,

    /// <summary>The V key.</summary>
    V = 21,

    /// <summary>The W key.</summary>
    W = 22,

    /// <summary>The X key.</summary>
    X = 23,

    /// <summary>The Y key.</summary>
    Y = 24,

    /// <summary>The Z key.</summary>
    Z = 25,


    // Top-row digits
    /// <summary>The 0 key on the top row.</summary>
    Digit0 = 26,

    /// <summary>The 1 key on the top row.</summary>
    Digit1 = 27,

    /// <summary>The 2 key on the top row.</summary>
    Digit2 = 28,

    /// <summary>The 3 key on the top row.</summary>
    Digit3 = 29,

    /// <summary>The 4 key on the top row.</summary>
    Digit4 = 30,

    /// <summary>The 5 key on the top row.</summary>
    Digit5 = 31,

    /// <summary>The 6 key on the top row.</summary>
    Digit6 = 32,

    /// <summary>The 7 key on the top row.</summary>
    Digit7 = 33,

    /// <summary>The 8 key on the top row.</summary>
    Digit8 = 34,

    /// <summary>The 9 key on the top row.</summary>
    Digit9 = 35,


    // Function keys
    /// <summary>The F1 key.</summary>
    F1 = 36,

    /// <summary>The F2 key.</summary>
    F2 = 37,

    /// <summary>The F3 key.</summary>
    F3 = 38,

    /// <summary>The F4 key.</summary>
    F4 = 39,

    /// <summary>The F5 key.</summary>
    F5 = 40,

    /// <summary>The F6 key.</summary>
    F6 = 41,

    /// <summary>The F7 key.</summary>
    F7 = 42,

    /// <summary>The F8 key.</summary>
    F8 = 43,

    /// <summary>The F9 key.</summary>
    F9 = 44,

    /// <summary>The F10 key.</summary>
    F10 = 45,

    /// <summary>The F11 key.</summary>
    F11 = 46,

    /// <summary>The F12 key.</summary>
    F12 = 47,


    // Editing and whitespace
    /// <summary>The Escape key.</summary>
    Escape = 48,

    /// <summary>The Enter/Return key.</summary>
    Enter = 49,

    /// <summary>The Tab key.</summary>
    Tab = 50,

    /// <summary>The spacebar.</summary>
    Space = 51,

    /// <summary>The Backspace key.</summary>
    Backspace = 52,

    /// <summary>The Delete key.</summary>
    Delete = 53,

    /// <summary>The Insert key.</summary>
    Insert = 54,

    /// <summary>The Home key.</summary>
    Home = 55,

    /// <summary>The End key.</summary>
    End = 56,

    /// <summary>The Page Up key.</summary>
    PageUp = 57,

    /// <summary>The Page Down key.</summary>
    PageDown = 58,

    /// <summary>The Caps Lock key.</summary>
    CapsLock = 59,


    // Arrows
    /// <summary>The left arrow key.</summary>
    ArrowLeft = 60,

    /// <summary>The right arrow key.</summary>
    ArrowRight = 61,

    /// <summary>The up arrow key.</summary>
    ArrowUp = 62,

    /// <summary>The down arrow key.</summary>
    ArrowDown = 63,


    // Modifiers
    /// <summary>The left Shift key.</summary>
    ShiftLeft = 64,

    /// <summary>The right Shift key.</summary>
    ShiftRight = 65,

    /// <summary>The left Ctrl key.</summary>
    ControlLeft = 66,

    /// <summary>The right Ctrl key.</summary>
    ControlRight = 67,

    /// <summary>The left Alt key.</summary>
    AltLeft = 68,

    /// <summary>The right Alt key.</summary>
    AltRight = 69,

    /// <summary>The left Super/Windows/Command key.</summary>
    SuperLeft = 70,

    /// <summary>The right Super/Windows/Command key.</summary>
    SuperRight = 71,


    // Punctuation
    /// <summary>The - key.</summary>
    Minus = 72,

    /// <summary>The = key.</summary>
    Equal = 73,

    /// <summary>The [ key.</summary>
    BracketLeft = 74,

    /// <summary>The ] key.</summary>
    BracketRight = 75,

    /// <summary>The backslash key.</summary>
    Backslash = 76,

    /// <summary>The ; key.</summary>
    Semicolon = 77,

    /// <summary>The apostrophe key.</summary>
    Quote = 78,

    /// <summary>The backtick key.</summary>
    Backquote = 79,

    /// <summary>The , key.</summary>
    Comma = 80,

    /// <summary>The . key.</summary>
    Period = 81,

    /// <summary>The / key.</summary>
    Slash = 82,


    // Numpad
    /// <summary>The numpad 0 key.</summary>
    Numpad0 = 83,

    /// <summary>The numpad 1 key.</summary>
    Numpad1 = 84,

    /// <summary>The numpad 2 key.</summary>
    Numpad2 = 85,

    /// <summary>The numpad 3 key.</summary>
    Numpad3 = 86,

    /// <summary>The numpad 4 key.</summary>
    Numpad4 = 87,

    /// <summary>The numpad 5 key.</summary>
    Numpad5 = 88,

    /// <summary>The numpad 6 key.</summary>
    Numpad6 = 89,

    /// <summary>The numpad 7 key.</summary>
    Numpad7 = 90,

    /// <summary>The numpad 8 key.</summary>
    Numpad8 = 91,

    /// <summary>The numpad 9 key.</summary>
    Numpad9 = 92,

    /// <summary>The numpad + key.</summary>
    NumpadAdd = 93,

    /// <summary>The numpad - key.</summary>
    NumpadSubtract = 94,

    /// <summary>The numpad * key.</summary>
    NumpadMultiply = 95,

    /// <summary>The numpad / key.</summary>
    NumpadDivide = 96,

    /// <summary>The numpad . key.</summary>
    NumpadDecimal = 97,

    /// <summary>The numpad Enter key.</summary>
    NumpadEnter = 98,

    /// <summary>The Num Lock key.</summary>
    NumLock = 99,


    // Miscellaneous
    /// <summary>The Print Screen key.</summary>
    PrintScreen = 100,

    /// <summary>The Scroll Lock key.</summary>
    ScrollLock = 101,

    /// <summary>The Pause/Break key.</summary>
    Pause = 102,

    /// <summary>The context-menu key.</summary>
    ContextMenu = 103,

    // Aliases: the same values under shorter names.

    /// <summary>Alias for <see cref="ControlLeft"/>.</summary>
    LCtrl = ControlLeft,

    /// <summary>Alias for <see cref="ControlRight"/>.</summary>
    RCtrl = ControlRight,

    /// <summary>Alias for <see cref="ShiftLeft"/>.</summary>
    LShift = ShiftLeft,

    /// <summary>Alias for <see cref="ShiftRight"/>.</summary>
    RShift = ShiftRight,

    /// <summary>Alias for <see cref="AltLeft"/>.</summary>
    LAlt = AltLeft,

    /// <summary>Alias for <see cref="AltRight"/>.</summary>
    RAlt = AltRight,

    /// <summary>Alias for <see cref="Enter"/>.</summary>
    Return = Enter,

    /// <summary>Alias for <see cref="ArrowLeft"/>.</summary>
    Left = ArrowLeft,

    /// <summary>Alias for <see cref="ArrowRight"/>.</summary>
    Right = ArrowRight,

    /// <summary>Alias for <see cref="ArrowUp"/>.</summary>
    Up = ArrowUp,

    /// <summary>Alias for <see cref="ArrowDown"/>.</summary>
    Down = ArrowDown,
}

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

/// <summary>A mouse button.</summary>
/// <remarks>Values are bit indices, matching the mapping in the native bridge.</remarks>
public enum MouseButton
{
    /// <summary>The primary, usually left, button.</summary>
    Left = 0,

    /// <summary>The secondary, usually right, button.</summary>
    Right = 1,

    /// <summary>The middle button, usually the scroll wheel.</summary>
    Middle = 2,

    /// <summary>The "back" thumb button.</summary>
    Back = 3,

    /// <summary>The "forward" thumb button.</summary>
    Forward = 4,
}

/// <summary>Modifier keys that must be held for a shortcut to fire.</summary>
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

    /// <summary>Either Super/Windows/Command key must be held.</summary>
    Super = 1 << 3,
}
