using System.Runtime.CompilerServices;
using Bevy.Interop;
using Diagnostics;

namespace Bevy;

/// <summary>
/// The first window's settings in the Diagnostics module's registry, on the settings panel's
/// <c>window</c> page, how it covers its screen, whether its edges resize it, its size, the monitor
/// it is on, whether a frame waits for the display and how a picture drawn at the size is brought
/// to the screen.
/// </summary>
/// <remarks>
/// <para>
/// Each acts on the window at once and is kept by the settings file, which an app being made reads
/// for them before its window is (<see cref="Open"/>), so the window opens in the mode, at the size
/// and on the monitor the player left, before its first frame is shown. The file is read whole
/// once more as the app starts, which finds them set.
/// </para>
/// <para>
/// There are four modes. Windowed is a window with the platform's frame, at the size set and where
/// it was. Borderless is a window without the frame at the size set, in the middle of its screen
/// with the desktop round it and drawn pixel for pixel, and a size as large as the screen's or
/// larger covers the screen, which is the only way it covers. Borderless fullscreen covers the
/// screen at the screen's own mode, with the picture drawn at the size set and brought to the
/// screen as <c>window.scaling</c> says, so a smaller size is softer and costs less. Fullscreen
/// takes the screen's mode, which Wayland gives no window and a screen that lists no mode at its
/// own size and rate cannot give, so a fullscreen window is refused there with the reason and keeps
/// the mode it holds.
/// </para>
/// <para>
/// The size is one in every mode, the window's as a window and as a borderless one and the
/// picture's in borderless fullscreen, kept while fullscreen and given back on leaving it, since
/// the platform gives back a size of its own choosing. Its place as a window is kept the same way.
/// The monitor is a number from 0 in the order <see cref="Window.MonitorCount"/> counts in, and
/// only reads where there is one, and a window stays where it is where the platform places windows
/// itself, as Wayland does, which the set says, so a borderless window there stands where the
/// compositor puts it.
/// </para>
/// <para>
/// A run whose window is not shown, a headless one, one drawn offscreen and one with no window of
/// its own, has the page only reading, with the reason, and the file's lines of the page kept as
/// they stand for a run that shows one. What the system shows is read as each row is, so a mode
/// reads as asked once the system has taken it, on Wayland a frame or two after the set. A
/// borderless window is a window to the system, so it reads borderless where the settings made it
/// so, and a window a game made without the frame, as the editor's is, reads as a window and keeps
/// the frame it has, the settings giving back only a frame they took.
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
        /// <summary>A window with the platform's frame.</summary>
        Windowed,

        /// <summary>
        /// A window without a frame at the size set, in the middle of its screen, covering the
        /// screen at the screen's size or larger.
        /// </summary>
        Borderless,

        /// <summary>
        /// The screen at its own mode, the picture drawn at the size set and scaled to it.
        /// </summary>
        BorderlessFullscreen,

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

        internal WindowScaling Scaling { get; set; }

        /// <summary>
        /// The mode Bevy opens the window in, a borderless window being a window without a frame,
        /// which the first frame covers its screen with where the size reaches the screen's.
        /// </summary>
        internal WindowMode WindowMode => Mode switch
        {
            Mode.BorderlessFullscreen => WindowMode.BorderlessFullscreen,
            Mode.Fullscreen => WindowMode.Fullscreen,
            _ => WindowMode.Windowed,
        };
    }

    private const string NoApp = "No app is running to take it.";
    private const string NotMade = "The system has not made the window yet.";
    private const string OneScreen = "There is one screen, so the window stays on it.";

    // What the settings write into while an app is being made, in place of its window.
    private static Start? _opening;

    // The size set, the window's as a window and as a borderless one and its picture's in
    // borderless fullscreen, kept while it is fullscreen.
    private static (uint Width, uint Height)? _windowed;

    // Where the window was as a window, from the desktop's corner, kept while it is in another
    // mode.
    private static (int X, int Y)? _place;

    // How borderless fullscreen brings its picture to the screen.
    private static WindowScaling _scaling;

    // Whether the settings made the window borderless, and whether they took the platform's frame
    // away for it, which they give back on its return to a window.
    private static bool _borderless;
    private static bool _tookFrame;

    // Whether the window opened borderless and is yet to be placed in its screen's middle, or to
    // cover it, once winit has listed the screens, which it does after the window is made.
    private static bool _settle;
    private static int? _settleOn;

    // How many frames the size and place given to the window as it changes mode are held to,
    // against the size a window manager restores of its own a few frames after the mode is set, as
    // mutter does for an X11 window.
    private const int GiveBackFrames = 30;

    // The size and place given to the window as it changed mode, the mode it changed to, and the
    // frames left to hold them.
    private sealed record Held(uint Width, uint Height, (int X, int Y)? At, Mode Mode, int Frames);

    private static Held? _held;

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
        (_windowed, _place, _seen, _held, _settle, _settleOn) = (null, null, null, null, false, null);
        (_scaling, _borderless, _tookFrame) = (WindowScaling.Linear, false, false);
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

        (_windowed, _scaling) = ((start.Width, start.Height), start.Scaling);
        (_settle, _settleOn) = (start.Mode == Mode.Borderless, start.Monitor);
        _borderless = _tookFrame = start.Mode == Mode.Borderless;
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

    // The mode the window is in, a window the settings made borderless borderless, and a
    // borderless one over its screen borderless fullscreen where its picture is drawn at a size of
    // its own.
    private static Mode Of(WindowShown shown) => shown.Mode switch
    {
        WindowMode.BorderlessFullscreen => shown.Scaled ? Mode.BorderlessFullscreen : Mode.Borderless,
        WindowMode.Fullscreen => Mode.Fullscreen,
        _ => _borderless ? Mode.Borderless : Mode.Windowed,
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

    // The size set, the window's own while it is a window and the one kept in every other mode, or
    // the config's default where neither is known. A window whose picture is scaled is in
    // borderless fullscreen from the set of the mode, which the system takes a frame or more later.
    private static (uint Width, uint Height) Windowed(Config made)
    {
        if (_opening is { } start) return (start.Width, start.Height);
        if (Look() is { Scaled: false } shown && Of(shown) == Mode.Windowed && Size() is { } size) return size;
        return _windowed ?? (made.Width, made.Height);
    }

    private static IEnumerable<Setting> Page()
    {
        var made = new Config();

        yield return new Setting<Mode>("window.mode",
            "How the window covers its screen, as a window, as a window without a frame at the size set in the screen's middle that covers the screen at the screen's size or larger, over the screen with the picture drawn at the size set and scaled to it, or fullscreen taking the screen's mode.",
            Mode.Windowed, () => _opening?.Mode ?? (Look() is { } shown ? Of(shown) : Mode.Windowed), apply: mode => SetMode(made, mode))
        { Lock = Unshown };

        yield return new Setting<bool>("window.resizable", "Whether the window's edges can be dragged to resize it.", true,
            () => _opening?.Resizable ?? Look()?.Resizable ?? true, apply: SetResizable)
        { Lock = Unshown };

        yield return new Setting<int>("window.width",
            "The width of the window as a window and as a borderless one, and of the picture in borderless fullscreen, which a window takes back on leaving fullscreen.", (int)made.Width,
            () => (int)Windowed(made).Width, apply: width => Resize(made, width, null))
        { Low = 0, High = 16384, Step = 16, Lock = Unshown };

        yield return new Setting<int>("window.height",
            "The height of the window as a window and as a borderless one, and of the picture in borderless fullscreen, which a window takes back on leaving fullscreen.", (int)made.Height,
            () => (int)Windowed(made).Height, apply: height => Resize(made, null, height))
        { Low = 0, High = 16384, Step = 16, Lock = Unshown };

        yield return new Setting<int>("window.monitor",
            "The screen the window is on, by its number from 0, where there is more than one, the window centered on another as it moves there.", 0,
            () => _opening?.Monitor ?? Math.Max(0, Look()?.Monitor ?? 0), apply: screen => SetMonitor(made, screen))
        { Low = 0, High = 15, Step = 1, Lock = () => Unshown() ?? (_opening is null && App.Running is not null && Window.MonitorCount() < 2 ? OneScreen : null) };

        yield return new Setting<bool>("window.vsync", "Whether a frame waits for the display, the window's surface made again before the next frame.", made.Vsync,
            () => _opening?.Vsync ?? Look()?.Vsync ?? made.Vsync, apply: SetVsync)
        { Lock = Unshown };

        yield return new Setting<WindowScaling>("window.scaling",
            "How borderless fullscreen brings its picture to the screen, filtered and soft, from the nearest picture pixel and sharp, which is even only where the screen is a whole number of times the picture's size, or sharp and even at any ratio, a picture pixel's edge blended in one screen pixel at most.",
            WindowScaling.Linear, () => _opening?.Scaling ?? _scaling, apply: SetScaling)
        { Lock = Unshown };
    }

    // Asks for the mode, refusing it with the reason where the system would not give it, so the
    // window keeps the one it holds. The size and place as a window are kept on leaving one and
    // given back on returning to one.
    private static string? SetMode(Config made, Mode mode)
    {
        if (_opening is { } start)
        {
            start.Mode = mode;
            return null;
        }
        if (Unreached() is { } why) return why;
        if (Look() is not { } shown) return NotMade;

        var before = Of(shown);
        if (mode == before) return null;
        var size = Windowed(made);
        var asked = mode switch
        {
            Mode.Fullscreen => WindowMode.Fullscreen,
            Mode.BorderlessFullscreen => WindowMode.BorderlessFullscreen,
            Mode.Borderless when Covers(size, shown.Monitor) => WindowMode.BorderlessFullscreen,
            _ => WindowMode.Windowed,
        };
        var refused = Window.ModeRefusal(asked) switch
        {
            WindowRefusal.NoScreenModes => $"Wayland gives no window a mode of the screen's, so the window stays {Word(before)}. Borderless fullscreen covers the screen at its own mode.",
            WindowRefusal.NoScreen => $"The system names no screen under the window, so it stays {Word(before)}.",
            WindowRefusal.NoScreenMode => $"The screen lists no mode at its own size and rate for a fullscreen window to take, so the window stays {Word(before)}. Borderless fullscreen covers the screen at its own mode.",
            _ => null,
        };
        if (refused is not null) return refused;

        if (before == Mode.Windowed)
        {
            _windowed = size;
            if (Window.Place() is { HasPosition: true } place) _place = (place.X, place.Y);
        }
        (_held, _borderless) = (null, false);
        Window.SetScaling(mode == Mode.BorderlessFullscreen ? size : null, _scaling);
        switch (mode)
        {
            case Mode.Windowed:
                Window.SetMode(WindowMode.Windowed);
                if (_tookFrame) Window.SetDecorations(true);
                _tookFrame = false;
                Window.SetSize(size.Width, size.Height);
                if (_place is { } at) Window.SetPosition(at.X, at.Y);
                _held = new Held(size.Width, size.Height, _place, Mode.Windowed, GiveBackFrames);
                break;
            case Mode.Borderless:
                Borderless(size, shown.Monitor);
                break;
            default:
                Window.SetMode(asked);
                break;
        }
        return null;
    }

    // The screen by its number, or the first where the window names none, or none where the
    // system lists none.
    private static MonitorInfo? Screen(int monitor)
    {
        var count = Window.MonitorCount();
        if (count == 0) return null;
        return Window.Monitor(monitor >= 0 && monitor < count ? monitor : 0);
    }

    // Whether a borderless window of the size covers the screen, its size reaching the screen's in
    // both directions in the screen's pixels.
    private static bool Covers((uint Width, uint Height) size, int monitor)
    {
        if (Screen(monitor) is not { } screen) return false;
        var scale = Window.Scale();
        return Math.Round(size.Width * scale) >= screen.Width && Math.Round(size.Height * scale) >= screen.Height;
    }

    // Makes the window borderless on the screen, a window without the platform's frame at the size
    // in the screen's middle, or the window covering the screen where the size reaches the
    // screen's. The place and size are held for a few frames, as a returning window's are.
    private static void Borderless((uint Width, uint Height) size, int monitor)
    {
        var shown = Look();
        _borderless = true;
        if (Covers(size, monitor))
        {
            _held = null;
            if (shown?.Mode != WindowMode.BorderlessFullscreen) Window.SetMode(WindowMode.BorderlessFullscreen);
            if (monitor >= 0 && shown?.Monitor != monitor) Window.SetMonitor(monitor);
            return;
        }

        if (shown?.Mode != WindowMode.Windowed) Window.SetMode(WindowMode.Windowed);
        if (shown is not { Decorated: false })
        {
            Window.SetDecorations(false);
            _tookFrame = true;
        }
        Window.SetSize(size.Width, size.Height);
        (int X, int Y)? at = null;
        if (Screen(monitor) is { } screen)
        {
            var scale = Window.Scale();
            var (width, height) = ((int)Math.Round(size.Width * scale), (int)Math.Round(size.Height * scale));
            at = (screen.X + ((int)screen.Width - width) / 2, screen.Y + ((int)screen.Height - height) / 2);
            Window.SetPosition(at.Value.X, at.Value.Y);
        }
        _held = new Held(size.Width, size.Height, at, Mode.Borderless, GiveBackFrames);
    }

    /// <summary>
    /// Places a window opened borderless once the screens are listed, and holds the size and place
    /// given to the window as it changes mode for a few frames, against the size a window manager
    /// restores of its own once the mode has changed.
    /// </summary>
    /// <remarks>
    /// Called each frame from the engine's frame sync, as <see cref="WindowMemory.Tick"/> is.
    /// Winit lists the screens only after it has made the window, so a window opened borderless
    /// opens without its frame in the middle of the screen Bevy finds for it and is placed again,
    /// or made to cover its screen, in the first frame the screens are known. The size and place
    /// are set in the frame the mode is, which a platform with no window manager keeps, and a
    /// manager answers the change of mode a few frames on with the size it saved, which mutter
    /// gives an X11 window shorter than the one it had. So they are set again in each frame they
    /// differ until the frames run out, and a resize or a move after them is the player's.
    /// </remarks>
    internal static void Tick()
    {
        if (_settle && Look() is { } opened && Window.MonitorCount() > 0)
        {
            _settle = false;
            if (Of(opened) == Mode.Borderless && _windowed is { } size) Borderless(size, _settleOn ?? opened.Monitor);
        }

        if (_held is not { } held) return;
        _held = held.Frames > 1 ? held with { Frames = held.Frames - 1 } : null;
        if (Look() is not { Mode: WindowMode.Windowed } shown || Of(shown) != held.Mode) return;
        if (Size() is { } now && now != (held.Width, held.Height)) Window.SetSize(held.Width, held.Height);
        if (held.At is { } at && Window.Place() is { HasPosition: true } place && (place.X, place.Y) != at) Window.SetPosition(at.X, at.Y);
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

    // Sets the size, at once where the window is a window, borderless in its screen's middle,
    // covering its screen where the size reaches the screen's or standing in the middle again where
    // it falls short, the picture's in borderless fullscreen, and kept for the window's return
    // where it is fullscreen. A window is a pixel across at the least.
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
        if (Look() is not { } shown) return null;

        // A scaled picture is the window's from the set of the mode, which the system takes a
        // frame or more later.
        switch (shown.Scaled ? Mode.BorderlessFullscreen : Of(shown))
        {
            case Mode.Windowed:
                Window.SetSize(size.Item1, size.Item2);
                break;
            case Mode.Borderless:
                Borderless(size, shown.Monitor);
                break;
            case Mode.BorderlessFullscreen:
                Window.SetScaling(size, _scaling);
                break;
        }
        return null;
    }

    private static string? SetMonitor(Config made, int screen)
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
        if (Of(shown) == Mode.Borderless) Borderless(Windowed(made), screen);
        else Window.SetMonitor(screen);
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

    // Sets how borderless fullscreen brings its picture to the screen, at once where the window's
    // picture is scaled, from the set of the mode, and kept for when it is where it is not.
    private static string? SetScaling(WindowScaling scaling)
    {
        if (_opening is { } start)
        {
            start.Scaling = scaling;
            return null;
        }
        if (Unreached() is { } why) return why;
        _scaling = scaling;
        if (Look() is { Scaled: true } && _windowed is { } size) Window.SetScaling(size, scaling);
        return null;
    }
}
