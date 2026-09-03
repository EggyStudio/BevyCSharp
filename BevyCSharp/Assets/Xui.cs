using Bevy.Interop;

namespace Bevy;

/// <summary>What a widget reported.</summary>
public enum UiEventKind
{
    /// <summary>The element was clicked.</summary>
    Click = 0,

    /// <summary>The element's value changed.</summary>
    Change = 1,

    /// <summary>A form was submitted.</summary>
    Submit = 2,

    /// <summary>The element took focus.</summary>
    Focus = 3,
}

/// <summary>One thing a widget reported, and which element it happened to.</summary>
public readonly record struct UiEvent(UiEventKind Kind, Entity Element);

/// <summary>
/// A document that is open on screen. Closing it takes it off again.
/// </summary>
public readonly record struct UiDocument(int Id)
{
    /// <summary>Whether this names a document at all.</summary>
    public bool IsOpen => Id > 0;

    /// <summary>Takes the document off the screen.</summary>
    public void Close() => Xui.Close(this);
}

/// <summary>
/// User interface described in HTML and CSS.
/// </summary>
/// <remarks>
/// <para>
/// A separate surface from <see cref="Ui"/>, which builds Bevy's own nodes from a settings
/// object. This one loads documents: the structure is HTML, the appearance is CSS, and both are
/// ordinary assets that can be edited without touching the program. What comes back is a handle
/// to a document and entities for the elements inside it.
/// </para>
/// <para>
/// Needs a bridge built with the editor profile, which <see cref="App.HasEditor"/> reports.
/// Everything here refuses on a bridge without it rather than doing nothing.
/// </para>
/// </remarks>
public static unsafe class Xui
{
    /// <summary>
    /// Opens an HTML document.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The path is relative to the asset root. A stylesheet the document links is resolved
    /// relative to the document rather than to the asset root, so
    /// <c>&lt;link href="theme.css"&gt;</c> beside <c>panels/thing.html</c> is
    /// <c>panels/theme.css</c> on disk.
    /// </para>
    /// <para>
    /// The document needs a <c>&lt;meta name="..."&gt;</c> tag in its head. A document without
    /// one is refused by the parser, which says so on the log rather than through this call.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">This build has no editor profile.</exception>
    public static UiDocument Open(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return new UiDocument(Native.Check(Native.bcs_xui_open(path), $"opening {path}"));
    }

    /// <summary>Takes a document off the screen.</summary>
    /// <exception cref="BevyNativeException">The document is not open, or there is no profile.</exception>
    public static void Close(UiDocument document) =>
        Native.Check(Native.bcs_xui_close(document.Id), $"closing document {document.Id}");

    /// <summary>
    /// The element carrying a CSS id, or <see cref="Entity.None"/> when nothing does.
    /// </summary>
    /// <remarks>
    /// Ids are looked up across every open document rather than within one, so they have to be
    /// unique across all of them. That is the rule HTML already has for ids, and the whole open
    /// interface is one document as far as the parser is concerned.
    /// </remarks>
    public static Entity Element(string cssId)
    {
        ArgumentException.ThrowIfNullOrEmpty(cssId);

        return new Entity(Native.bcs_xui_element(cssId));
    }

    /// <summary>
    /// The text an element carries: what was typed into an input, or the inner text of anything
    /// else.
    /// </summary>
    /// <exception cref="BevyNativeException">The element is gone or carries no text.</exception>
    public static string GetText(Entity element) => Native.ReadText(
        (buffer, capacity) => Native.bcs_xui_get_text(element.Bits, buffer, capacity),
        $"reading the text of {element}");

    /// <summary>Replaces an element's text.</summary>
    /// <exception cref="BevyNativeException">The element is gone or carries no text.</exception>
    public static void SetText(Entity element, string text) => Native.Check(
        Native.bcs_xui_set_text(element.Bits, text ?? string.Empty),
        $"setting the text of {element}");

    /// <summary>The number an element carries, which today means a slider's value.</summary>
    /// <exception cref="BevyNativeException">The element is gone or carries no number.</exception>
    public static float GetNumber(Entity element)
    {
        float value;
        Native.Check(
            Native.bcs_xui_get_number(element.Bits, &value), $"reading the value of {element}");
        return value;
    }

    /// <summary>Moves a slider, clamped to the range the document gave it.</summary>
    /// <exception cref="BevyNativeException">The element is gone or carries no number.</exception>
    public static void SetNumber(Entity element, float value) => Native.Check(
        Native.bcs_xui_set_number(element.Bits, value), $"setting the value of {element}");

    /// <summary>Whether an element is ticked, which covers a checkbox, a switch and a toggle.</summary>
    /// <exception cref="BevyNativeException">The element is gone or cannot be ticked.</exception>
    public static bool GetFlag(Entity element)
    {
        int value;
        Native.Check(
            Native.bcs_xui_get_flag(element.Bits, &value), $"reading the state of {element}");
        return value != 0;
    }

    /// <summary>Ticks or unticks an element.</summary>
    /// <exception cref="BevyNativeException">The element is gone or cannot be ticked.</exception>
    public static void SetFlag(Entity element, bool value) => Native.Check(
        Native.bcs_xui_set_flag(element.Bits, value ? 1 : 0), $"setting the state of {element}");

    /// <summary>How many events one call carries at most.</summary>
    /// <remarks>
    /// Anything past this stays queued and arrives on the next call, so a frame that produced a
    /// burst loses none of it.
    /// </remarks>
    private const int BatchSize = 32;

    /// <summary>
    /// Takes what the widgets reported since the last call.
    /// </summary>
    /// <remarks>
    /// Drained rather than subscribed to, because a C# system is handed the world rather than a
    /// set of parameters and cannot hold an observer. Call once a frame and the queue stays
    /// short.
    /// </remarks>
    public static UiEvent[] Drain()
    {
        if (!App.HasEditor) return [];

        var drained = new List<UiEvent>();
        var buffer = stackalloc NativeUiEvent[BatchSize];

        int count;
        do
        {
            count = Native.Check(
                Native.bcs_xui_events(buffer, BatchSize), "draining the interface events");

            for (var i = 0; i < count; i++)
                drained.Add(new UiEvent((UiEventKind)buffer[i].Kind, new Entity(buffer[i].Entity)));
        }
        while (count == BatchSize);

        return [.. drained];
    }
}
