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
    /// <para>
    /// Any of the four may be <see cref="float.NaN"/>, which leaves that edge untouched, so a
    /// width of <c>NaN</c> keeps whatever the stylesheet said.
    /// </para>
    /// <para>
    /// <see cref="Auto"/> is the third answer: it puts a field back to being decided by the
    /// contents. Something once given a height measures that height ever after, so without a way
    /// to undo it there is no asking what its contents want a second time.
    /// </para>
    /// <para>
    /// Naming a corner takes the element out of the flow, which is what placing a panel means.
    /// Naming neither (both <paramref name="left"/> and <paramref name="top"/> left alone) says
    /// only how large, and the element stays where its parent's layout put it.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">The element is gone or is not laid out.</exception>
    public static void SetRect(Entity element, float left, float top, float width, float height) =>
        Native.Check(
            Native.bcs_xui_set_rect(element.Bits, left, top, width, height),
            $"placing {element}");

    /// <summary>Hands a dimension back to the contents, where <see cref="float.NaN"/> leaves it.</summary>
    public const float Auto = float.PositiveInfinity;

    /// <summary>
    /// Caps how large an element may get, without saying how large it is.
    /// </summary>
    /// <remarks>
    /// What a panel that is as tall as its contents up to a limit needs. A height decides the
    /// measurement; a maximum leaves the measurement to the contents and only stops it running
    /// past the room there is. <see cref="float.NaN"/> leaves a limit alone and
    /// <see cref="Auto"/> removes it.
    /// </remarks>
    /// <exception cref="BevyNativeException">The element is gone or is not laid out.</exception>
    public static void SetLimits(Entity element, float maxWidth, float maxHeight) =>
        Native.Check(
            Native.bcs_xui_set_limits(element.Bits, maxWidth, maxHeight),
            $"limiting {element}");

    /// <summary>
    /// Takes the keyboard away from whatever has it.
    /// </summary>
    /// <remarks>
    /// There is no other way out of a text field. A widget takes focus when it is clicked and
    /// loses it when another widget is clicked, and a click on the scene is not a click on a
    /// widget, so somebody who types in a search box and then goes back to the viewport leaves the
    /// box holding the keyboard, and every key an editor binds is a letter going into it.
    /// </remarks>
    /// <exception cref="BevyNativeException">This build has no editor profile.</exception>
    public static void Blur() => Native.Check(Native.bcs_xui_blur(), "clearing the focus");

    /// <summary>
    /// How many times the set of open documents has been rebuilt.
    /// </summary>
    /// <remarks>
    /// Every widget of every open document is respawned when the list changes, so every element
    /// handle anything holds becomes a dead entity at that moment. Remembering this number beside
    /// the handles and throwing them away when it moves is exact, where waiting a fixed number of
    /// frames and hoping was a guess.
    /// </remarks>
    public static ulong Generation => App.HasEditor ? Native.bcs_xui_generation() : 0;

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
    /// How many live elements carry a CSS id.
    /// </summary>
    /// <remarks>
    /// One, for a document that is behaving. More than one means two widget trees are alive under
    /// the same names, and everything written by name reaches only one of them.
    /// </remarks>
    public static int Count(string cssId) => App.HasEditor ? Native.bcs_xui_count(cssId) : 0;

    /// <summary>
    /// Where an element sits in the drawing order, or -1 when it is not drawn.
    /// </summary>
    /// <remarks>
    /// Everything on screen is sorted into one list and drawn in that order, so of two elements
    /// the one with the larger answer is the one in front. What it is for is telling a layer that
    /// took from one that did not.
    /// </remarks>
    public static int StackOf(Entity element)
    {
        if (!App.HasEditor) return -1;

        var index = Native.bcs_xui_stack(element.Bits);
        return index < 0 ? -1 : index;
    }

    /// <summary>
    /// Gives an element a CSS class, replacing whatever it had.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What a document cannot say, because it is decided while the program runs: which row is
    /// selected, which button is armed, which field holds something that will not parse. The
    /// interface applies the stylesheet again when it notices, so the element takes on everything
    /// the new class says.
    /// </para>
    /// <para>
    /// One class rather than a list, because the interface matches only the first class an element
    /// has. Passing nothing leaves the element with none.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">The element is gone.</exception>
    public static void SetClass(Entity element, string? cssClass) => Native.Check(
        Native.bcs_xui_set_class(element.Bits, cssClass),
        $"classing {element}");

    /// <summary>
    /// Paints an element's background.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a color that depends on what the element is showing rather than on what it is, which
    /// is the one thing a stylesheet cannot say.
    /// </para>
    /// <para>
    /// Safe to mix with hiding, which it once was not. Painting an element makes the interface
    /// restyle it, and a restyle used to put back both the display property and the color that
    /// whoever was driving the panel had just decided. What a panel decides is now kept apart from
    /// what the stylesheet says and applied after it, so a painted element stays painted and still
    /// hides when it is told to.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">The element is gone.</exception>
    public static void SetColor(Entity element, float red, float green, float blue, float alpha = 1f)
        => Native.Check(
            Native.bcs_xui_set_color(element.Bits, red, green, blue, alpha),
            $"painting {element}");

    /// <summary>
    /// Draws an element, or stops drawing it while leaving it where it is.
    /// </summary>
    /// <remarks>
    /// Not the same as hiding it. A hidden element is taken out of the layout and takes no space,
    /// so it also stops having a size, and a panel that has to know its own size before it can be
    /// put in the right place would never find one out. This leaves the space and stops the paint,
    /// which is what something waiting a frame to be measured wants.
    /// </remarks>
    /// <exception cref="BevyNativeException">The element is gone.</exception>
    public static void SetDrawn(Entity element, bool drawn) => Native.Check(
        Native.bcs_xui_set_drawn(element.Bits, drawn ? 1 : 0), $"drawing {element}");

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
    /// The interface's own answer rather than what was last written to it, which is what makes it
    /// worth asking at all: an element inside a hidden parent is not on screen whatever anyone
    /// decided about the element itself.
    /// </remarks>
    public static bool IsVisible(Entity element)
    {
        int visible;
        return Native.bcs_xui_get_visible(element.Bits, &visible) >= 0 && visible != 0;
    }

    /// <summary>
    /// Paints an element, as one number: red, green, blue and alpha, a byte each.
    /// </summary>
    /// <remarks>
    /// The way a color is written down everywhere else, so a color that came from a stylesheet, a
    /// file or a field does not have to be taken apart to be used. <c>0xFF0000FF</c> is red.
    /// </remarks>
    /// <exception cref="BevyNativeException">The element is gone.</exception>
    public static void SetColor(Entity element, uint rgba) => SetColor(
        element,
        ((rgba >> 24) & 0xFF) / 255f,
        ((rgba >> 16) & 0xFF) / 255f,
        ((rgba >> 8) & 0xFF) / 255f,
        (rgba & 0xFF) / 255f);

    /// <summary>Puts an element in front of or behind its siblings.</summary>
    /// <exception cref="BevyNativeException">The element is gone.</exception>
    public static void SetLayer(Entity element, int layer) => Native.Check(
        Native.bcs_xui_set_layer(element.Bits, layer), $"layering {element}");

    /// <summary>
    /// How much of the room left over an element takes, against its neighbours.
    /// </summary>
    /// <remarks>
    /// The one part of a flex layout that has to be decided while the program runs: how many
    /// things share a row, and how wide each of them is against the others, is a question about
    /// what is being shown rather than about what the document looks like. A negative weight puts
    /// the element back to whatever the stylesheet said.
    /// </remarks>
    /// <exception cref="BevyNativeException">The element is gone.</exception>
    public static void SetWeight(Entity element, float weight) => Native.Check(
        Native.bcs_xui_set_weight(element.Bits, weight), $"weighting {element}");

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
