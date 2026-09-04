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
    private bool _suspended;
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
        // Nothing is worth resolving while the widgets are being replaced: the old ones are dead
        // or dying, and a lookup now would cache one of them for good.
        if (_suspended) return Entity.None;

        if (_elements.TryGetValue(cssId, out var known)) return known;

        var found = Xui.Element(cssId);
        if (!found.IsNone) _elements[cssId] = found;

        return found;
    }

    /// <summary>Whether an element of this window is the one an event happened to.</summary>
    public bool Owns(Entity element) => _elements.ContainsValue(element);

    /// <summary>
    /// Stops the window reading its elements, because they are about to be replaced.
    /// </summary>
    /// <remarks>
    /// Every lookup answers <see cref="Entity.None"/> until <see cref="Resume"/>, and every
    /// binding does nothing with that, so the frames between a document changing on disk and its
    /// new widgets standing up pass without anything reading a despawned entity.
    /// </remarks>
    public void Suspend()
    {
        _suspended = true;
        Forget();
    }

    /// <summary>
    /// Starts reading again, against whatever the rebuild produced.
    /// </summary>
    /// <remarks>
    /// The elements are looked up afresh, since every widget is a new entity. What the panel
    /// holds is untouched, so the values go straight back onto the new elements.
    /// </remarks>
    public void Resume()
    {
        _suspended = false;
        Forget();
    }

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

    /// <summary>Shows or hides the whole window.</summary>
    public void Show(bool visible)
    {
        var root = Root;
        if (root.IsNone) return;
        if (_shown == visible) return;

        Xui.SetVisible(root, visible);
        _shown = visible;
    }

    /// <summary>Puts the window in front of or behind the others.</summary>
    public void Layer(int layer)
    {
        var root = Root;
        if (root.IsNone) return;
        if (_layered == layer) return;

        Xui.SetLayer(root, layer);
        _layered = layer;
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

    /// <summary>Whether two lengths agree, counting two <c>NaN</c>s as agreeing.</summary>
    private static bool Near(float a, float b) =>
        (float.IsNaN(a) && float.IsNaN(b)) || MathF.Abs(a - b) < 0.5f;

    /// <summary>Takes the window off the screen.</summary>
    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        _elements.Clear();
        Document.Close();
    }
}
