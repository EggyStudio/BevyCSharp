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

    /// <summary>Opens the panel's document.</summary>
    void Open();

    /// <summary>Closes it again.</summary>
    void Close();

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
}
