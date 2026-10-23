namespace Bevy;

/// <summary>
/// The engine's debug overlay, which F3 shows over every program as the newest Minecraft's shows
/// over its world, the engine, the frame, the camera and the world in a column on the left and the
/// system, the GPU, the display and the renderer in one on the right.
/// </summary>
/// <remarks>
/// <para>
/// The overlay is the engine's own text in its own font, a line on a dark half-clear backing, with
/// no window, header or border, drawn over the program's interface. It is no ImGui window and
/// stands outside the hidden scope (<see cref="Ui.BeginHidden"/>), so F1, which hides a program's
/// interface, leaves it, and a run with the interface off draws it as well.
/// </para>
/// <para>
/// F3 shows and hides it as the key is let go, where no other key was pressed while it was held,
/// so a program's own chords on F3 stand and never open it, and nothing happens while a field of an
/// ImGui window has the caret. Its lines are written again four times a second while it shows, and
/// a run that never shows it pays nothing but a key's read a frame.
/// </para>
/// </remarks>
public static partial class Debug
{
    /// <summary>Whether the overlay shows, which <see cref="OverlayKey"/> turns over, settable so a program shows it for a picture.</summary>
    public static bool OverlayShown { get; set; }

    /// <summary>The key that shows and hides the overlay, F3 unless set, or nothing for none.</summary>
    /// <remarks>
    /// Taken on its release, where no other key went down while it was held, so F3 held with
    /// another key is the program's alone.
    /// </remarks>
    public static Key? OverlayKey { get; set; } = Key.F3;
}
