using Diagnostics;

namespace Bevy;

/// <summary>
/// The engine's debug overlay, settings panel and keys, the Diagnostics module's models over the
/// process's registry, which every program has, F1 hiding the program's interface, F2 taking a
/// screenshot, F3 showing the overlay, F4 opening the console and F5 the settings panel.
/// </summary>
/// <remarks>
/// <para>
/// The keys are the module's actions (<see cref="Keys"/>), each a setting of the registry a player
/// rebinds, <c>keys.overlay</c> and the rest, so <c>keys.overlay F6</c> typed into the console
/// moves the overlay to F6. An action is taken as its key is let go where no other key went down
/// while it was held, so a program's own chords on the keys stand and turn nothing over, and a key
/// of one character is the field's while a field has the caret. Shift held with the overlay's key
/// shows the profiler's pass timings under the overlay, or hides them.
/// </para>
/// <para>
/// The overlay is the engine's own text in its own font, a line on a dark half-clear backing, with
/// no window, header or border, drawn over the program's interface from the module's
/// <see cref="Overlay"/>. It is no ImGui window and stands outside the hidden scope
/// (<see cref="Ui.BeginHidden"/>), so F1, which hides a program's interface, leaves it, and a run
/// with the interface off draws it as well. Its lines are the sections' the engine adds, the frame,
/// the camera, the world, the system, the GPU, the display and the renderer, with the model's own
/// line of the frame's rate at the head, asked again four times a second while it shows, the strip
/// of frame times every frame, and a run that never shows it pays the keys' read and a frame's time
/// kept a frame.
/// </para>
/// <para>
/// A program adds sections of its own with <see cref="OverlayModel.AddSection"/> on
/// <see cref="Overlay"/>, a world generator's seed and its chunk, an enemy count, in any of its
/// four places after the engine's. Bevy's own FPS overlay (<see cref="DevTools.FpsOverlay"/>),
/// which the overlay's lines and strip say again at the same corner, is hidden while the overlay
/// shows and given back after.
/// </para>
/// <para>
/// The console (<see cref="ConsoleLog.Model"/>) and the panel (<see cref="Panel"/>) are drawn with
/// Dear ImGui by the module's drawers, in <see cref="Draw"/>, which a program calls in its ImGui
/// frame.
/// </para>
/// </remarks>
public static partial class Debug
{
    // Made in this order, in this file, since the keys turn over the overlay and the panel and
    // the drawers draw them.

    /// <summary>The overlay's sections and the frames' times, which F3 shows.</summary>
    public static OverlayModel Overlay { get; } = new();

    /// <summary>
    /// The settings panel, a page for each first word of the settings' names and a program's own
    /// pages, which F5 opens.
    /// </summary>
    public static PanelModel Panel { get; } = MadePanel();

    /// <summary>
    /// The five keys, each a setting of the registry a player rebinds, and what they turn over, the
    /// overlay, the panel, the console (<see cref="ConsoleLog.Model"/>), the interface the hidden
    /// scope holds (<see cref="Ui.Hidden"/>) and a screenshot (<see cref="Capture.Screenshot"/>).
    /// </summary>
    /// <remarks>
    /// Each app takes keys of its own as it is built, bound as the last app's were, so a key a
    /// player rebinds stays rebound and a key held as the last app ended, which that app never saw
    /// let go, makes no later key a chord.
    /// </remarks>
    public static DiagnosticKeys Keys { get; private set; } = MadeKeys(null);

    private static readonly ImGuiConsole DrawnConsole = new(ConsoleLog.Model);
    private static readonly ImGuiPanel DrawnPanel = new(Panel);

    // What the panel moves or runs is written to the console as a typed line's answer is, so the
    // run's log holds what a player changed, and the host follows it as it follows a typed line.
    private static PanelModel MadePanel()
    {
        var panel = new PanelModel(Registry.Process);
        panel.Answered += (name, answer) =>
        {
            if (answer.Text.Length > 0) ConsoleLog.Write(answer.Done ? ConsoleLevel.Info : ConsoleLevel.Error, answer.Text);
            ConsoleHost.Ran(name, answer);
        };
        return panel;
    }

    /// <summary>
    /// Whether the console or the settings panel is open or a field of the interface has the caret,
    /// each of which takes Escape before the program does (<see cref="Input"/>).
    /// </summary>
    /// <remarks>
    /// The overlay takes no key, so it is not counted, and a panel a host joined to
    /// <c>Keys.Panels</c> reads its keys as the program does and is not counted either.
    /// </remarks>
    internal static bool TakesEscape() =>
        Keys.Panels.Open is OpenPanels.PanelName or OpenPanels.ConsoleName || ImGuiRuntime.Typing;

    /// <summary>
    /// Says which of the program's panels is open, the settings panel with the title of its page,
    /// for a check that a key opened one, stepped it or closed it.
    /// </summary>
    [Command("panels", "Says which panel is open, the settings panel with its page, or none")]
    internal static string OpenPanel() => Keys.Panels.Open switch
    {
        null => "none",
        OpenPanels.PanelName => $"panel {Panel.Title}",
        var name => name,
    };

    /// <summary>The keys made again for an app being built, as <see cref="Keys"/> says.</summary>
    internal static void FreshKeys()
    {
        var last = Keys;
        // The last keys go first, since the registry takes no name it holds.
        last.Dispose();
        Keys = MadeKeys(last);
    }

    private static DiagnosticKeys MadeKeys(DiagnosticKeys? last)
    {
        var keys = new DiagnosticKeys(Registry.Process) { Overlay = Overlay, Panel = Panel, Console = ConsoleLog.Model };
        keys.ScreenshotAsked += Screenshot;
        if (last is not null)
            foreach (var action in Enum.GetValues<KeyAction>()) keys.KeyOf(action).TrySet(last.KeyOf(action).Value);
        return keys;
    }

    /// <summary>
    /// Draws the console and the settings panel while each is open, with Dear ImGui, the world
    /// lent to a line the player enters and an action the panel runs.
    /// </summary>
    /// <param name="ctx">The frame's context, whose world a command is lent.</param>
    /// <remarks>
    /// Called between <see cref="ImGuiRuntime.Begin"/> and <see cref="ImGuiRuntime.End"/>, and
    /// does nothing where the interface is not running, as in a build without it or a run that did
    /// not ask for it (<see cref="Config.Gui"/>). A command whose answer waits on the GPU is
    /// answered into the console in a later frame.
    /// </remarks>
    /// <example>
    /// <code>
    /// ImGuiRuntime.Begin(ctx);
    /// DrawTheGamesOwnWindows();
    /// Debug.Draw(ctx);
    /// ImGuiRuntime.End();
    /// </code>
    /// </example>
    public static void Draw(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (!ImGuiRuntime.IsRunning) return;

        ConsoleHost.AnswerLater(ctx.World);

        // The engine's assemblies loaded, so every command they declare is in the registry the
        // console completes and runs from.
        _ = ConsoleHost.Commands;

        var window = ImGuiRuntime.Size;
        using (ConsoleHost.Lend(ctx.World))
        {
            DrawnConsole.Draw(window);
            DrawnPanel.Draw(window);
        }
    }

    /// <summary>Takes a screenshot as the screenshot's key asks, in a run that draws.</summary>
    private static void Screenshot()
    {
        if (App.HasRenderer && _keysWorld is { } world && !world.Resource<Config>().Headless) Capture.Screenshot();
    }
}
