using System.Runtime.CompilerServices;
using Bevy.Interop;
using Diagnostics;

namespace Bevy;

/// <summary>
/// The first window's settings in the Diagnostics module's registry, on the settings panel's
/// <c>window</c> page, how it covers its screen, whether its edges resize it, its size as a window,
/// the monitor it is on and whether a frame waits for the display.
/// </summary>
/// <remarks>
/// <para>
/// Each acts on the window at once and is kept by the settings file, which an app being made reads
/// for them before its window is (<see cref="Open"/>), so the window opens in the mode, at the size
/// and on the monitor the player left, before its first frame is shown. The file is read whole
/// once more as the app starts, which finds them set.
/// </para>
/// <para>
/// Borderless covers the window's screen with no change of the screen's own mode, and fullscreen
/// takes the screen's mode, which Wayland gives no window and a screen that lists no mode at its
/// own size and rate cannot give, so a fullscreen window is refused there with the reason and keeps
/// the mode it holds. The size is the window's as a window, kept while it covers its screen and
/// given back as it leaves it, since the platform gives back a size of its own choosing. The
/// monitor is a number from 0 in the order <see cref="Window.MonitorCount"/> counts in, and only
/// reads where there is one, and a window stays where it is where the platform places windows
/// itself, as Wayland does, which the set says.
/// </para>
/// <para>
/// A run whose window is not shown, a headless one, one drawn offscreen and one with no window of
/// its own, has the page only reading, with the reason, and the file's lines of the page kept as
/// they stand for a run that shows one. What the system shows is read as each row is, so a mode
/// reads as asked once the system has taken it, on Wayland a frame or two after the set.
/// </para>
/// </remarks>
internal static class WindowSettings
{
    internal static IReadOnlyList<Setting> All { get; } = [.. Page()];

    // Added as the assembly loads, as the engine's other settings are, so a start finds them.
    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255",
        Justification = "The settings join the registry the areas' settings are added to from their initializers.")]
    internal static void Register()
    {
        foreach (var setting in All) Registry.Process.Add(setting);
    }

    /// <summary>How the window covers its screen, the choices of <c>window.mode</c>.</summary>
    internal enum Mode
    {
        /// <summary>A window.</summary>
        Windowed,

        /// <summary>A window with no border over its screen, at the screen's own mode.</summary>
        Borderless,

        /// <summary>The screen, at a mode of the screen's the window takes.</summary>
        Fullscreen,
    }

    /// <summary>
    /// What an app's first window opens with, from its config and the settings file.
    /// </summary>
    internal sealed class Start
    {
        internal Mode Mode { get; set; }

        internal bool Resizable { get; set; } = true;

        internal uint Width { get; set; }

        internal uint Height { get; set; }

        /// <summary>
        /// The monitor by its index, or none for where the platform puts the window.
        /// </summary>
        internal int? Monitor { get; set; }

        internal bool Vsync { get; set; }
    }

    private const string NoApp = "No app is running to take it.";
    private const string NotMade = "The system has not made the window yet.";
    private const string OneScreen = "There is one screen, so the window stays on it.";

    // What the settings write into while an app is being made, in place of its window.
    private static Start? _opening;

    // The window's size as a window, kept while it covers its screen.
    private static (uint Width, uint Height)? _windowed;

    // How many frames the size given back as the window leaves a mode covering its screen is held
    // to, against the size a window manager restores of its own a few frames after the mode is
    // set, as mutter does for an X11 window.
    private const int GiveBackFrames = 30;

    // The size given back as the window left a mode covering its screen, and the frames left to
    // hold it.
    private static (uint Width, uint Height, int Frames)? _givenBack;

    // What the system showed of the window when last asked, for a read where no world is lent.
    private static WindowShown? _seen;

    /// <summary>
    /// What an app's first window opens with, the config's vsync and the size and place it was
    /// left at (<see cref="WindowMemory"/>), with the settings file's lines of the page over them
    /// where the run shows a window.
    /// </summary>
    /// <remarks>
    /// Each line of the page is given to its setting in the file's order as a start gives it, as
    /// the read of the whole file gives them as the app starts, so the two leave each setting at
    /// the same value, and a line the setting refuses is refused here and warned of then. The
    /// settings write the start meanwhile in place of a window, which winit makes only as the app
    /// runs.
    /// </remarks>
    internal static Start Open(Config config, WindowPlace place)
    {
        (_windowed, _seen, _givenBack) = (null, null, null);
        var start = new Start { Width = place.Width, Height = place.Height, Vsync = config.Vsync };
        if (!App.HasRenderer || config.Headless || config.Offscreen || !config.PrimaryWindow || SettingsPath.For(config) is not { } path)
            return start;

        string[] lines;
        try
        {
            lines = File.Exists(path) ? File.ReadAllLines(path) : [];
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Warned of as the app starts, where the same read fails again.
            return start;
        }

        _opening = start;
        try
        {
            foreach (var line in lines.Select(CommandLine.Parse))
                if (line.Rest.Length > 0 && All.FirstOrDefault(setting => setting.Name == line.Name) is { } setting)
                    setting.TryStart(line.Words.Count == 1 ? line.Words[0] : line.Rest);
        }
        finally
        {
            _opening = null;
        }

        _windowed = (start.Width, start.Height);
        return start;
    }

    // Why the page only reads in a run whose window is not shown, or none where one is, or while a
    // start gives the window its settings.
    private static string? Unshown()
    {
        if (_opening is not null || App.Running?.World.TryGetResource<Config>(out var config) != true) return null;
        if (!App.HasRenderer) return "The bridge has no window to show, as its headless profile has none.";
        if (config.Headless) return "The program has no window and draws no frames, as a headless run does.";
        if (config.Offscreen) return "The program draws into images of its own with no window, as an offscreen run does.";
        return config.PrimaryWindow ? null : "The program opens no window of its own.";
    }

    // Why a set reaches no window, or none where it does.
    private static string? Unreached() => Unshown() ?? (App.Running is null ? NoApp : null);

    // What the system shows of the window, or what it showed when last asked where no world is
    // lent, or none where no window is shown.
    private static WindowShown? Look()
    {
        if (_opening is not null || App.Running is null || Unshown() is not null) return null;
        if (Window.Shown() is { } shown) _seen = shown;
        return _seen;
    }

    private static Mode Of(WindowMode mode) => mode switch
    {
        WindowMode.BorderlessFullscreen => Mode.Borderless,
        WindowMode.Fullscreen => Mode.Fullscreen,
        _ => Mode.Windowed,
    };

    private static string Word(Mode mode) => mode.ToString().ToLowerInvariant();

    // The window's size as Bevy holds it, or none where no world is lent to ask in.
    private static (uint Width, uint Height)? Size()
    {
        try
        {
            return Window.Size();
        }
        catch (BevyNativeException)
        {
            return null;
        }
    }

    // The window's size as a window, its own while it is one and the one kept while it covers its
    // screen, or the config's default where neither is known.
    private static (uint Width, uint Height) Windowed(Config made)
    {
        if (_opening is { } start) return (start.Width, start.Height);
        if (Look() is { Mode: WindowMode.Windowed } && Size() is { } size) return size;
        return _windowed ?? (made.Width, made.Height);
    }

    private static IEnumerable<Setting> Page()
    {
        var made = new Config();

        yield return new Setting<Mode>("window.mode",
            "How the window covers its screen, as a window, with no border over the screen at the screen's own mode, or fullscreen taking the screen's mode.",
            Mode.Windowed, () => _opening?.Mode ?? (Look() is { } shown ? Of(shown.Mode) : Mode.Windowed), apply: SetMode)
        { Lock = Unshown };

        yield return new Setting<bool>("window.resizable", "Whether the window's edges can be dragged to resize it.", true,
            () => _opening?.Resizable ?? Look()?.Resizable ?? true, apply: SetResizable)
        { Lock = Unshown };

        yield return new Setting<int>("window.width",
            "The width of the window as a window, which it takes on leaving a mode that covers its screen.", (int)made.Width,
            () => (int)Windowed(made).Width, apply: width => Resize(made, width, null))
        { Low = 0, High = 16384, Step = 16, Lock = Unshown };

        yield return new Setting<int>("window.height",
            "The height of the window as a window, which it takes on leaving a mode that covers its screen.", (int)made.Height,
            () => (int)Windowed(made).Height, apply: height => Resize(made, null, height))
        { Low = 0, High = 16384, Step = 16, Lock = Unshown };

        yield return new Setting<int>("window.monitor",
            "The screen the window is on, by its number from 0, where there is more than one, the window centered on another as it moves there.", 0,
            () => _opening?.Monitor ?? Math.Max(0, Look()?.Monitor ?? 0), apply: SetMonitor)
        { Low = 0, High = 15, Step = 1, Lock = () => Unshown() ?? (_opening is null && App.Running is not null && Window.MonitorCount() < 2 ? OneScreen : null) };

        yield return new Setting<bool>("window.vsync", "Whether a frame waits for the display, the window's surface made again before the next frame.", made.Vsync,
            () => _opening?.Vsync ?? Look()?.Vsync ?? made.Vsync, apply: SetVsync)
        { Lock = Unshown };
    }

    // Asks for the mode, refusing it with the reason where the system would not give it, so the
    // window keeps the one it holds. The size as a window is kept on leaving one and given back on
    // returning to one.
    private static string? SetMode(Mode mode)
    {
        if (_opening is { } start)
        {
            start.Mode = mode;
            return null;
        }
        if (Unreached() is { } why) return why;
        if (Look() is not { } shown) return NotMade;

        var before = Of(shown.Mode);
        if (mode == before) return null;
        var asked = (WindowMode)(int)mode;
        var refused = Window.ModeRefusal(asked) switch
        {
            WindowRefusal.NoScreenModes => $"Wayland gives no window a mode of the screen's, so the window stays {Word(before)}. Borderless covers the screen at its own mode.",
            WindowRefusal.NoScreen => $"The system names no screen under the window, so it stays {Word(before)}.",
            WindowRefusal.NoScreenMode => $"The screen lists no mode at its own size and rate for a fullscreen window to take, so the window stays {Word(before)}. Borderless covers the screen at its own mode.",
            _ => null,
        };
        if (refused is not null) return refused;

        if (before == Mode.Windowed) _windowed = Size() ?? _windowed;
        Window.SetMode(asked);
        _givenBack = null;
        if (mode == Mode.Windowed && _windowed is { } size)
        {
            Window.SetSize(size.Width, size.Height);
            _givenBack = (size.Width, size.Height, GiveBackFrames);
        }
        return null;
    }

    /// <summary>
    /// Holds the size given back as the window leaves a mode covering its screen for a few frames,
    /// against the size a window manager restores of its own once the mode has changed.
    /// </summary>
    /// <remarks>
    /// Called each frame from the engine's frame sync, as <see cref="WindowMemory.Tick"/> is. The
    /// size is set in the frame the mode is, which a platform with no window manager keeps, and a
    /// manager answers the change of mode a few frames on with the size it saved, which mutter
    /// gives an X11 window shorter than the one it had. So the size is set again in each frame it
    /// differs until the frames run out, and a resize after them is the player's.
    /// </remarks>
    internal static void Tick()
    {
        if (_givenBack is not { } back) return;
        _givenBack = back.Frames > 1 ? (back.Width, back.Height, back.Frames - 1) : null;
        if (Look() is { Mode: WindowMode.Windowed } && Size() is { } size && size != (back.Width, back.Height))
            Window.SetSize(back.Width, back.Height);
    }

    private static string? SetResizable(bool on)
    {
        if (_opening is { } start)
        {
            start.Resizable = on;
            return null;
        }
        if (Unreached() is { } why) return why;
        Window.SetResizable(on);
        return null;
    }

    // Sets the size as a window, at once where the window is one and kept for its return to one
    // where it covers its screen. A window is a pixel across at the least.
    private static string? Resize(Config made, int? width, int? height)
    {
        if (_opening is { } start)
        {
            start.Width = (uint)Math.Max(1, width ?? (int)start.Width);
            start.Height = (uint)Math.Max(1, height ?? (int)start.Height);
            return null;
        }
        if (Unreached() is { } why) return why;

        var (w, h) = Windowed(made);
        var size = ((uint)Math.Max(1, width ?? (int)w), (uint)Math.Max(1, height ?? (int)h));
        _windowed = size;
        if (Look() is { Mode: WindowMode.Windowed }) Window.SetSize(size.Item1, size.Item2);
        return null;
    }

    private static string? SetMonitor(int screen)
    {
        if (_opening is { } start)
        {
            start.Monitor = screen;
            return null;
        }
        if (Unreached() is { } why) return why;

        var count = Window.MonitorCount();
        if (screen >= count) return $"There is no screen {screen}, the screens being numbered from 0 to {count - 1}.";
        if (Look() is not { } shown) return NotMade;
        if (shown.Monitor == screen) return null;
        if (shown.Mode == WindowMode.Windowed && !Window.Place().HasPosition)
            return $"The system places windows itself, as Wayland does, so the window stays on screen {Math.Max(0, shown.Monitor)}.";
        Window.SetMonitor(screen);
        return null;
    }

    private static string? SetVsync(bool on)
    {
        if (_opening is { } start)
        {
            start.Vsync = on;
            return null;
        }
        if (Unreached() is { } why) return why;
        Window.SetVsync(on);
        return null;
    }
}
