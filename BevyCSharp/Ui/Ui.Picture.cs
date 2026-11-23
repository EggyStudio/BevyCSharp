using Bevy.Interop;

namespace Bevy;

// The part of the window the scene's picture stands in, which a game's own interface can be kept
// over, as a heads-up display drawn on the picture and not over the bars round a fitted one.
public static unsafe partial class Ui
{
    /// <summary>
    /// Keeps a node over the part of the window the scene's picture stands in, or lets it go.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The interface takes the whole window, drawn after the picture at the window's own size, so
    /// it stays sharp whatever size the scene is drawn at (<c>Render.Picture</c>). A node kept here
    /// is placed absolutely over the picture's part of the window instead, inside the bars of a
    /// picture whose shape is kept, its left, top, width and height written in each frame the part
    /// moves, so whatever the game sets of them is overwritten while it is kept. The whole window
    /// is the picture's where the picture is the window's own or stretched over it.
    /// </para>
    /// <para>
    /// Let go, the node keeps the place it had until the game gives it another.
    /// </para>
    /// </remarks>
    /// <param name="node">A node, the root of the interface kept over the picture.</param>
    /// <param name="on">Whether to keep it there.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is no node, or this build has no renderer.
    /// </exception>
    public static void SetOnPicture(Entity node, bool on = true)
    {
        var status = NativeCore.bcs_window_node_on_picture(node.Bits, on ? 1 : 0);
        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Keeping a node over the picture");
        Native.Check(status, $"keeping {node} over the picture");
    }
}
