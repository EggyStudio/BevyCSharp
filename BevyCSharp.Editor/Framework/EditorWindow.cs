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

    private EditorWindow(string path, UiDocument document)
    {
        Path = path;
        Document = document;
    }

    /// <summary>The asset path the document was loaded from.</summary>
    public string Path { get; }

    /// <summary>The document behind this window.</summary>
    public UiDocument Document { get; }

    /// <summary>Whether the document is still open.</summary>
    public bool IsOpen { get; private set; } = true;

    /// <summary>Opens a document as a window.</summary>
    /// <exception cref="Bevy.Interop.BevyNativeException">This build has no editor profile.</exception>
    public static EditorWindow Open(string path) => new(path, Xui.Open(path));

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
        _elements.Clear();
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
        _elements.Clear();
    }

    /// <summary>Takes the window off the screen.</summary>
    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        _elements.Clear();
        Document.Close();
    }
}
