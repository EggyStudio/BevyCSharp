using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// One panel that is open on screen.
/// </summary>
/// <remarks>
/// <para>
/// A window is a document and the elements inside it. Where it sits, how large it is and what it
/// looks like are all CSS on the document, not properties here: a window that wants rounded
/// corners, a shadow and a margin says so in a stylesheet, and swapping the stylesheet restyles
/// every window without touching any code. Putting position and size on this object would make
/// half the appearance answerable in C# and half in CSS, and the half in C# would be the half
/// nobody could change without a rebuild.
/// </para>
/// <para>
/// Elements are looked up once and kept, because resolving a CSS id walks every element that
/// exists and a panel reads its own elements every frame.
/// </para>
/// </remarks>
public sealed class EditorWindow
{
    private readonly Dictionary<string, Entity> _elements = [];
    private ulong _builtAt = ulong.MaxValue;
    private ulong _settledAt;
    private (float X, float Y, float Width, float Height)? _placed;
    private bool? _shown;
    private int? _layered;

    private EditorWindow(string path, PanelChrome chrome, UiDocument document)
    {
        Path = path;
        Chrome = chrome;
        Document = document;
    }

    /// <summary>The asset path the document was loaded from.</summary>
    public string Path { get; }

    /// <summary>What the panel declared about itself.</summary>
    public PanelChrome Chrome { get; }

    /// <summary>The document behind this window.</summary>
    public UiDocument Document { get; }

    /// <summary>Whether the document is still open.</summary>
    public bool IsOpen { get; private set; } = true;

    /// <summary>
    /// The element the window is placed by, or <see cref="Entity.None"/>.
    /// </summary>
    /// <remarks>
    /// The outermost element of the panel rather than the document's body, because the body is
    /// the whole screen: every open document lays its body over the window, and placing that
    /// would move every panel at once.
    /// </remarks>
    public Entity Root => Chrome.Root is { } root ? Element(root) : Entity.None;

    /// <summary>The element a person drags the window by, or <see cref="Entity.None"/>.</summary>
    public Entity Handle => Chrome.Handle is { } handle ? Element(handle) : Entity.None;

    /// <summary>Opens a document as a window.</summary>
    /// <exception cref="Bevy.Interop.BevyNativeException">This build has no editor profile.</exception>
    public static EditorWindow Open(string path, PanelChrome chrome) =>
        new(path, chrome, Xui.Open(path));

    /// <summary>
    /// The element carrying <paramref name="cssId"/>, or <see cref="Entity.None"/>.
    /// </summary>
    /// <remarks>
    /// Resolved on first ask and kept afterwards. An id that resolved to nothing is not kept, so
    /// a panel that asks before its document has finished loading gets an answer on a later
    /// frame rather than a permanent nothing.
    /// </remarks>
    public Entity Element(string cssId)
    {
        // The interface says how many times it has rebuilt everything, so a cache full of dead
        // entities is noticed exactly rather than waited out. This was a fixed number of frames
        // started by whoever opened or closed a panel, which is wrong twice over: a rebuild
        // nobody here caused went unnoticed, and two rebuilds overlapping ended the wait early and
        // left every panel holding handles to widgets that no longer existed.
        if (_builtAt != Generation)
        {
            _builtAt = Generation;
            _settledAt = Frame + RebuildFrames;
            Forget();
        }

        // Nothing is kept while the new widgets are still arriving: the entity that answers this
        // frame may be replaced by the next, and caching one would hold a dead element for as long
        // as the panel is open.
        if (Frame < _settledAt) return Xui.Element(cssId);

        if (_elements.TryGetValue(cssId, out var known)) return known;

        var found = Xui.Element(cssId);
        if (!found.IsNone) _elements[cssId] = found;

        return found;
    }

    /// <summary>The frame being drawn, so a rebuild can be waited out without being told.</summary>
    internal static ulong Frame { get; set; }

    /// <summary>How many times the interface has rebuilt every open document.</summary>
    /// <remarks>Read once a frame by the shell, because every window asks and the answer is one.</remarks>
    internal static ulong Generation { get; set; }

    /// <summary>How long the widgets of a rebuild take to arrive, in frames.</summary>
    private const ulong RebuildFrames = 24;

    /// <summary>Whether an element of this window is the one an event happened to.</summary>
    public bool Owns(Entity element) => _elements.ContainsValue(element);

    /// <summary>
    /// Stops the window reading its elements, because they are about to be replaced.
    /// </summary>
    /// <remarks>
    /// For a couple of dozen frames, every lookup is made afresh rather than remembered, so a
    /// panel keeps working through the rebuild and never holds on to an element that died in it.
    /// <see cref="Resume"/> ends it early when the interface says the new widgets are up.
    /// </remarks>
    public void Suspend()
    {
        _settledAt = Frame + RebuildFrames;
        Forget();
    }

    /// <summary>
    /// Starts reading again, against whatever the rebuild produced.
    /// </summary>
    /// <remarks>
    /// The elements are looked up afresh, since every widget is a new entity. What the panel
    /// holds is untouched, so the values go straight back onto the new elements. The waiting
    /// window is not cut short: a second rebuild may already be under way, and ending the wait on
    /// the first one's report is how every panel ends up holding a dead element.
    /// </remarks>
    public void Resume() => Forget();

    /// <summary>Drops every element and everything written to one.</summary>
    /// <remarks>
    /// The placement has to go with the elements. What was written was written to entities that
    /// are gone, so remembering it would leave the new ones unplaced until something moved.
    /// </remarks>
    private void Forget()
    {
        _elements.Clear();
        _placed = null;
        _shown = null;
        _layered = null;
    }

    // -- Placement
    //
    // What the layout does to a window. Every write compares against what was last written, so a
    // panel that has not moved costs a comparison rather than a call across the ABI, and a frame
    // in which nothing moved touches nothing.

    /// <summary>
    /// Puts the window's root at a rectangle, in logical pixels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="float.NaN"/> width or height leaves that dimension alone, which is what keeps
    /// the stylesheet's answer: how wide a panel is belongs in CSS and where it is belongs here.
    /// </para>
    /// <para>
    /// <paramref name="measured"/> is where the node actually ended up. It is compared as well as
    /// the last request, because the request is not the only thing that writes the node: the
    /// interface restyles an element after it spawns, which puts it back where the stylesheet
    /// says. Remembering only what was asked for would leave a panel that got restyled unplaced
    /// for good, since nothing would ever ask again.
    /// </para>
    /// </remarks>
    public void PlaceAt(float x, float y, float width, float height, UiRect? measured = null)
    {
        var root = Root;
        if (root.IsNone) return;

        var wanted = (x, y, width, height);
        var settled = measured is not { } rect || (Near(rect.X, x) && Near(rect.Y, y));

        if (_placed is { } already && Same(already, wanted) && settled) return;

        Xui.SetRect(root, x, y, width, height);
        _placed = wanted;
    }

    /// <summary>
    /// Shows or hides the whole window.
    /// </summary>
    /// <remarks>
    /// Read before written, and not remembered. The interface puts an element's display back to
    /// the stylesheet's answer whenever it restyles the widget, so a window told once that it is
    /// hidden reappears the next time anything is written into it. Asking what it is now costs one
    /// call and is the only answer that stays true.
    /// </remarks>
    public void Show(bool visible)
    {
        var root = Root;
        if (root.IsNone) return;
        if (Xui.IsVisible(root) == visible) return;

        Xui.SetVisible(root, visible);
        _shown = visible;
    }

    /// <summary>
    /// Puts the window in front of or behind the others.
    /// </summary>
    /// <remarks>
    /// Written every time the panels are arranged rather than once, because the interface writes
    /// this component too: a widget restyled after we set it goes back to the stylesheet's answer,
    /// and a menu that was on top ends up under the panel it opened from. One call per panel per
    /// frame is nothing beside being drawn in the wrong order.
    /// </remarks>
    public void Layer(int layer)
    {
        var root = Root;
        if (root.IsNone) return;

        Xui.SetLayer(root, layer);
        _layered = layer;
    }

    /// <summary>
    /// Caps how large the window may get, without saying how large it is.
    /// </summary>
    /// <remarks>
    /// How a panel is as tall as its contents up to the room its column has. Told as a maximum
    /// rather than as a height, because a height decides the measurement: a panel given one
    /// measures that height ever after and can never be asked what its contents want again, which
    /// is a hierarchy that grows a dozen rows inside a panel that stays the size it was.
    /// </remarks>
    public void LimitTo(float maxWidth, float maxHeight)
    {
        var root = Root;
        if (root.IsNone) return;

        // Written every time the panels are arranged rather than remembered, for the same reason
        // as the layering: the interface reapplies the stylesheet whenever it restyles a widget,
        // and a stylesheet that says nothing about a maximum puts the maximum back to none. A
        // remembered write is a cap that quietly stops holding, which is a panel that grows past
        // the room it has and stays there. One call per panel per frame is nothing beside that.
        Xui.SetLimits(root, maxWidth, maxHeight);
    }

    /// <summary>Where the window ended up, or nothing when it has not been laid out yet.</summary>
    public UiRect? Measure()
    {
        var root = Root;
        return !root.IsNone && Xui.TryRect(root, out var rect) ? rect : null;
    }

    /// <summary>Whether a point is over this window.</summary>
    public bool Covers(float x, float y) => Measure() is { } rect && rect.Contains(x, y);

    /// <summary>Whether two rectangles are the same as far as a layout is concerned.</summary>
    /// <remarks>
    /// A measured size is a float that has been through a layout pass, so the number that comes
    /// back is not reliably the number that went in. Comparing exactly would make every frame a
    /// write, and a window written to every frame is one that cannot be dragged.
    /// </remarks>
    private static bool Same(
        (float X, float Y, float Width, float Height) a,
        (float X, float Y, float Width, float Height) b) =>
        Near(a.X, b.X) && Near(a.Y, b.Y) && Near(a.Width, b.Width) && Near(a.Height, b.Height);

    /// <summary>
    /// Whether two lengths agree, counting two of the same non-number as agreeing.
    /// </summary>
    /// <remarks>
    /// Both sentinels have to compare equal to themselves or every frame writes again: <c>NaN</c>
    /// for a dimension left alone, and infinity for one handed back to the contents. Subtracting
    /// either from itself does not give zero.
    /// </remarks>
    private static bool Near(float a, float b) => a.Equals(b) || MathF.Abs(a - b) < 0.5f;

    /// <summary>Takes the window off the screen.</summary>
    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        _elements.Clear();
        Document.Close();
    }
}
