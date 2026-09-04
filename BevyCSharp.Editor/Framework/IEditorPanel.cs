using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What the shell needs from a panel, and what the generator writes for one.
/// </summary>
/// <remarks>
/// Every member here is generated from the attributes on a panel class, so a panel author
/// implements none of it. It exists so the shell can drive a panel without knowing its type, and
/// so a panel that wants to do something the attributes cannot describe can implement it by hand.
/// </remarks>
public interface IEditorPanel
{
    /// <summary>The window this panel is showing in, once it has been opened.</summary>
    EditorWindow? Window { get; }

    /// <summary>
    /// What the panel declared about itself: its root element, where it starts, what dismisses it.
    /// </summary>
    /// <remarks>
    /// True before the panel is open and unchanged while it is, which is what lets the shell
    /// decide how to open a panel before opening it. Where a panel currently <em>is</em> belongs
    /// to <see cref="EditorLayout"/> instead, because that changes.
    /// </remarks>
    PanelChrome Chrome { get; }

    /// <summary>Opens the panel's document.</summary>
    void Open();

    /// <summary>Closes it again.</summary>
    void Close();

    /// <summary>
    /// Brings the panel's own values up to date, before any of them are written out.
    /// </summary>
    /// <remarks>
    /// The hook a panel showing the world needs: a hierarchy reads the entities here, an
    /// inspector reads the selection's components, and both then have ordinary values for the
    /// bindings to write. Separate from <see cref="Pull"/> so that reading the world and writing
    /// the screen stay two things, and so a panel that shows only its own state has nothing here.
    /// </remarks>
    void Refresh();

    /// <summary>
    /// Writes the panel's own values out to its elements.
    /// </summary>
    /// <remarks>
    /// The direction that keeps the screen agreeing with the program. Called every frame, and
    /// each binding writes only when its value differs from what the element already holds, so a
    /// field nothing touched costs a comparison rather than a write.
    /// </remarks>
    void Pull();

    /// <summary>
    /// Reads one element back into the panel, after that element reported a change.
    /// </summary>
    /// <returns>Whether the element belonged to this panel.</returns>
    bool Push(Entity element);

    /// <summary>
    /// Runs whatever a click on <paramref name="element"/> is bound to.
    /// </summary>
    /// <returns>Whether anything was bound to it.</returns>
    bool Invoke(Entity element);

    /// <summary>
    /// Offers whatever a secondary click on <paramref name="element"/> is bound to.
    /// </summary>
    /// <returns>Whether anything was bound to it.</returns>
    /// <remarks>
    /// Separate from <see cref="Invoke"/> so that a row can be selected by a left click and
    /// offer a menu on a right one, which is the gesture every editor uses for the same thing.
    /// </remarks>
    bool Context(Entity element);

    /// <summary>
    /// Tells the panel that a frame's edits have all been read back into it.
    /// </summary>
    /// <remarks>
    /// Called once per frame in which <see cref="Push"/> accepted something, so a panel acts on
    /// the whole frame's edits rather than once per element. A panel with no
    /// <c>[OnChange]</c> method does nothing here.
    /// </remarks>
    void Changed();
}
