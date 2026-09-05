using Bevy.Interop;

namespace Bevy;

/// <summary>What a widget reported.</summary>
public enum UiEventKind
{
    /// <summary>The element was clicked.</summary>
    Click = 0,

    /// <summary>The element's value changed.</summary>
    Change = 1,

    /// <summary>A form was submitted. Not reported yet.</summary>
    Submit = 2,

    /// <summary>The element took focus.</summary>
    Focus = 3,

    /// <summary>
    /// The documents were rebuilt after one changed on disk.
    /// </summary>
    /// <remarks>
    /// Reported against no element, because every element is a new one: the rebuild respawns
    /// them, so any entity held from before this point names nothing. Anything caching an
    /// element has to look it up again.
    /// </remarks>
    Reloaded = 4,

    /// <summary>
    /// A document changed on disk and a rebuild is coming.
    /// </summary>
    /// <remarks>
    /// The widgets are about to be despawned, so anything holding an element should stop reading
    /// it until <see cref="Reloaded"/> says the new ones are up. The two are several frames
    /// apart, and reading in between is reading the dead.
    /// </remarks>
    Reloading = 5,

    /// <summary>
    /// The element was clicked with the secondary button.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="Click"/> because asking what can be done to a thing is not the
    /// same gesture as doing the thing, and a tool that treats them alike has no way to offer a
    /// context menu.
    /// </remarks>
    Context = 6,
}

/// <summary>
/// One thing a widget reported, and which element it happened to.
/// </summary>
/// <remarks>
/// Only an element with a CSS id reports, since an id is how it is addressed and an element that
/// cannot be named is one nothing asked about. A click on something inside an element, the text
/// of a button rather than the button, is reported against the nearest element that has one.
/// </remarks>
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
/// A rectangle on screen, in logical pixels, measured from the top left of the window.
/// </summary>
/// <param name="X">The left edge.</param>
/// <param name="Y">The top edge.</param>
/// <param name="Width">How wide.</param>
/// <param name="Height">How tall.</param>
public readonly record struct UiRect(float X, float Y, float Width, float Height)
{
    /// <summary>The right edge.</summary>
    public float Right => X + Width;

    /// <summary>The bottom edge.</summary>
    public float Bottom => Y + Height;

    /// <summary>Whether a point is inside, edges included.</summary>
    public bool Contains(float x, float y) => x >= X && x <= Right && y >= Y && y <= Bottom;
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
    /// <c>panels/theme.css</c> on disk. It cannot climb out of that directory either: Bevy
    /// refuses an asset path containing <c>..</c>, so a shared stylesheet lives beside the
    /// documents that link it rather than above them.
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

    // -- Placement
    //
    // Where an element sits, as something a tool decides rather than something its stylesheet
    // does. A stylesheet is the right place for what a panel looks like and the wrong place for
    // where it is: a layout that can be described, saved and rearranged has to be data the
    // editor holds. The chrome stays in CSS; the rectangle comes from here.

    /// <summary>
    /// Places an element at an absolute rectangle, in logical pixels.
    /// </summary>
    /// <remarks>
    /// Any of the four may be <see cref="float.NaN"/>, which leaves that edge to the layout: a
    /// width of <c>NaN</c> is a panel as wide as its contents.
    /// </remarks>
    /// <exception cref="BevyNativeException">The element is gone or is not laid out.</exception>
    public static void SetRect(Entity element, float left, float top, float width, float height) =>
        Native.Check(
            Native.bcs_xui_set_rect(element.Bits, left, top, width, height),
            $"placing {element}");

    /// <summary>
    /// Shows or hides an element and everything under it.
    /// </summary>
    /// <remarks>
    /// A hidden element takes no space, so its neighbours close up, which is what a dismissed
    /// flyout should look like rather than a hole where it was.
    /// </remarks>
    /// <exception cref="BevyNativeException">The element is gone or is not laid out.</exception>
    public static void SetVisible(Entity element, bool visible) => Native.Check(
        Native.bcs_xui_set_visible(element.Bits, visible ? 1 : 0), $"showing {element}");

    /// <summary>
    /// Points an image element at a file, relative to the asset root.
    /// </summary>
    /// <remarks>
    /// What lets a picture be a decision the program makes rather than one the document does: a
    /// toolbar whose buttons come from a table needs to say which icon each one draws. Passing
    /// nothing clears it.
    /// </remarks>
    /// <exception cref="BevyNativeException">The element is gone or draws no image.</exception>
    public static void SetImage(Entity element, string? path) => Native.Check(
        Native.bcs_xui_set_image(element.Bits, path ?? string.Empty),
        $"setting the image of {element}");

    /// <summary>
    /// Whether an element is on screen.
    /// </summary>
    /// <remarks>
    /// Worth asking rather than remembering: the interface reapplies a widget's stylesheet
    /// whenever it restyles one, which puts its display back to what the CSS says, so what was
    /// last written is not what is necessarily in force.
    /// </remarks>
    public static bool IsVisible(Entity element)
    {
        int visible;
        return Native.bcs_xui_get_visible(element.Bits, &visible) >= 0 && visible != 0;
    }

    /// <summary>Puts an element in front of or behind its siblings.</summary>
    /// <exception cref="BevyNativeException">The element is gone.</exception>
    public static void SetLayer(Entity element, int layer) => Native.Check(
        Native.bcs_xui_set_layer(element.Bits, layer), $"layering {element}");

    /// <summary>
    /// Where an element ended up, in logical pixels.
    /// </summary>
    /// <remarks>
    /// The rectangle the layout produced rather than the one that was asked for, which is the
    /// only one worth testing a cursor against, and in the same units the cursor is reported in.
    /// </remarks>
    /// <exception cref="BevyNativeException">The element is gone or has not been laid out yet.</exception>
    public static UiRect Rect(Entity element)
    {
        var rect = stackalloc float[4];
        Native.Check(Native.bcs_xui_rect(element.Bits, rect), $"measuring {element}");
        return new UiRect(rect[0], rect[1], rect[2], rect[3]);
    }

    /// <summary>
    /// Where an element ended up, or <see langword="false"/> when it has no rectangle yet.
    /// </summary>
    /// <remarks>
    /// The frames between a document being asked for and its widgets being laid out are ordinary
    /// rather than exceptional, and a tool arranging its panels asks every frame, so this is the
    /// form that arranging uses.
    /// </remarks>
    public static bool TryRect(Entity element, out UiRect rect)
    {
        var values = stackalloc float[4];
        if (Native.bcs_xui_rect(element.Bits, values) < 0)
        {
            rect = default;
            return false;
        }

        rect = new UiRect(values[0], values[1], values[2], values[3]);
        return true;
    }

    /// <summary>
    /// The element the keyboard is going to, or <see cref="Entity.None"/>.
    /// </summary>
    /// <remarks>
    /// What a panel showing live values has to ask before writing text out: a panel that writes
    /// its values every frame and does it to the field somebody is typing in replaces what they
    /// have typed with what the program still says.
    /// </remarks>
    public static Entity Focused() =>
        App.HasEditor ? new Entity(Native.bcs_xui_focused()) : Entity.None;

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
