namespace Bevy;

/// <summary>A key as the keyboard's layout reads it, the character it types or the name it has.</summary>
/// <remarks>
/// <para>
/// Bevy's logical <c>Key</c>, read through Bevy's <c>ButtonInput&lt;Key&gt;</c> by the same calls
/// as a physical <see cref="Key"/>. A physical key is a place on the keyboard, the same whatever
/// layout is set, which suits moving with WASD. A logical key names what that place means in the
/// layout, which suits a key named by what it types, as '?' for help or '+' for zoom, wherever the
/// layout puts it:
/// </para>
/// <code>
/// if (ctx.Input.KeyPressed(LogicalKey.Character("?"))) ShowHelp();
/// if (ctx.Input.KeyPressed(LogicalKey.Enter) &amp;&amp; ctx.Input.KeyDown(LogicalKey.Control)) Submit();
/// </code>
/// <para>
/// A named key is named as Bevy names it, so one without a property here is
/// <see cref="Named(string)"/> with its name, <c>Named("MediaPlayPause")</c>.
/// </para>
/// </remarks>
public readonly record struct LogicalKey
{
    private LogicalKey(LogicalKeyKind kind, string value) => (Kind, Value) = (kind, value);

    /// <summary>What kind of key it is.</summary>
    public LogicalKeyKind Kind { get; }

    /// <summary>Its name, for a named key, or its character, for one that types one.</summary>
    public string Value { get; }

    /// <summary>The key that types <paramref name="text"/>.</summary>
    /// <param name="text">What it types, usually one character.</param>
    public static LogicalKey Character(string text) => new(LogicalKeyKind.Character, text ?? throw new ArgumentNullException(nameof(text)));

    /// <summary>The key Bevy names <paramref name="name"/>, as <c>Enter</c> or <c>ArrowLeft</c>.</summary>
    /// <param name="name">Its name, as Bevy's <c>Key</c> names its variant.</param>
    public static LogicalKey Named(string name) => new(LogicalKeyKind.Named, name ?? throw new ArgumentNullException(nameof(name)));

    /// <summary>A dead key, which changes the character the next key types.</summary>
    /// <param name="text">The character it would add, or empty where the platform does not say.</param>
    public static LogicalKey Dead(string text) => new(LogicalKeyKind.Dead, text ?? throw new ArgumentNullException(nameof(text)));

    /// <summary>Enter, or Return.</summary>
    public static LogicalKey Enter => Named("Enter");

    /// <summary>Tab.</summary>
    public static LogicalKey Tab => Named("Tab");

    /// <summary>The space bar.</summary>
    public static LogicalKey Space => Named("Space");

    /// <summary>Backspace.</summary>
    public static LogicalKey Backspace => Named("Backspace");

    /// <summary>Delete.</summary>
    public static LogicalKey Delete => Named("Delete");

    /// <summary>Escape.</summary>
    public static LogicalKey Escape => Named("Escape");

    /// <summary>Either Control, whichever side.</summary>
    public static LogicalKey Control => Named("Control");

    /// <summary>Either Shift, whichever side.</summary>
    public static LogicalKey Shift => Named("Shift");

    /// <summary>Either Alt, or Option, whichever side.</summary>
    public static LogicalKey Alt => Named("Alt");

    /// <summary>Either Super, the Windows or Command key, whichever side.</summary>
    public static LogicalKey Super => Named("Super");

    /// <summary>The up arrow.</summary>
    public static LogicalKey ArrowUp => Named("ArrowUp");

    /// <summary>The down arrow.</summary>
    public static LogicalKey ArrowDown => Named("ArrowDown");

    /// <summary>The left arrow.</summary>
    public static LogicalKey ArrowLeft => Named("ArrowLeft");

    /// <summary>The right arrow.</summary>
    public static LogicalKey ArrowRight => Named("ArrowRight");

    /// <summary>The key as Bevy's <c>Debug</c> writes it, a name alone or <c>Character("?")</c>.</summary>
    public override string ToString() => Kind switch
    {
        LogicalKeyKind.Character => $"Character(\"{Value}\")",
        LogicalKeyKind.Dead => $"Dead(\"{Value}\")",
        _ => Value,
    };
}
