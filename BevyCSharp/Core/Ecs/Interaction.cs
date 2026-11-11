using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Marks a UI node the pointer is reported over.
/// </summary>
/// <remarks>
/// A name-only handle, so <c>Has&lt;Interaction&gt;()</c> and <c>[With]</c> filters pick out the
/// nodes that react to the pointer, and a <c>[Changed]</c> filter sees the pointer come over a
/// node, leave it, press or let go. It names the bridge's own record of how the pointer stands on
/// the node, kept from Bevy's hover and press each frame, since Bevy's own interaction is
/// deprecated, and its value is read through <see cref="Ui.InteractionOf"/>. Spawn a node with
/// <see cref="UiSettings.Interactive"/> to give it one.
/// </remarks>
public readonly struct Interaction : INativeComponent
{
    readonly string INativeComponent.NativeName => "Interaction";
    readonly bool INativeComponent.MirrorsLayout => false;
}
