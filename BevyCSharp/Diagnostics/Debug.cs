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
/// ImGui window has the caret. Shift held with it shows the profiler's pass timings under the
/// overlay, or hides them. Its lines are written again four times a second while it shows, the
/// strip of frame times every frame, and a run that never shows it pays a key's read and a frame's
/// time kept a frame.
/// </para>
/// <para>
/// A program adds sections of its own with <see cref="AddOverlaySection"/>, a world generator's
/// seed and its chunk, an enemy count, drawn in the left column under the engine's in the order
/// added. Bevy's own FPS overlay (<see cref="DevTools.FpsOverlay"/>), which the overlay's lines and
/// strip say again at the same corner, is hidden while the overlay shows and given back after.
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

    /// <summary>
    /// Whether the profiler's pass timings show under the overlay, the slowest first, which Shift
    /// with <see cref="OverlayKey"/> turns over, the overlay shown with them.
    /// </summary>
    /// <remarks>
    /// The passes are those <see cref="Render.Timings"/> measures, so they need
    /// <see cref="Config.GpuTimings"/> asked for when the app is made, and the overlay says so where
    /// it was not.
    /// </remarks>
    public static bool ProfilerShown { get; set; }

    /// <summary>
    /// Adds a section of a program's own to the overlay's left column, under the engine's lines and
    /// any section added before, in place of one of the same name.
    /// </summary>
    /// <param name="name">The section's heading, which it is taken away by.</param>
    /// <param name="lines">What it says, asked four times a second while the overlay shows, from the engine's tick.</param>
    /// <remarks>
    /// A section whose lines throw says why in a line of its own rather than stopping the overlay.
    /// The sections last while the process runs, across apps, as the keys do.
    /// </remarks>
    /// <example>
    /// <code>
    /// Debug.AddOverlaySection("World", () => [$"seed {seed}", $"chunk {chunk.X} {chunk.Z}"]);
    /// </code>
    /// </example>
    public static void AddOverlaySection(string name, Func<IEnumerable<string>> lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(lines);

        lock (Sections)
        {
            var at = Sections.FindIndex(section => section.Name == name);
            if (at >= 0) Sections[at] = (name, lines);
            else Sections.Add((name, lines));
        }
    }

    /// <summary>Takes a section of the overlay away by its name.</summary>
    /// <param name="name">The heading it was added under.</param>
    /// <returns>Whether there was one of that name.</returns>
    public static bool RemoveOverlaySection(string name)
    {
        lock (Sections) return Sections.RemoveAll(section => section.Name == name) > 0;
    }

    private static readonly List<(string Name, Func<IEnumerable<string>> Lines)> Sections = [];
}
