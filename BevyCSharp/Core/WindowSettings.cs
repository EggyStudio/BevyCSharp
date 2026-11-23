using System.Globalization;
using System.Runtime.CompilerServices;
using Bevy.Interop;
using Diagnostics;

namespace Bevy;

/// <summary>
/// The first window's settings in the Diagnostics module's registry, on the settings panel's
/// <c>window</c> page, how it covers its screen, its resolution, whether its edges resize it, the
/// monitor it is on and whether a frame waits for the display.
/// </summary>
/// <remarks>
/// <para>
/// Each acts on the window at once and is kept by the settings file, which an app being made reads
/// for them before its window is (<see cref="Open"/>), so the window opens in the mode, at the size
/// and on the monitor the player left, before its first frame is shown. The file is read whole
/// once more as the app starts, which finds them set.
/// </para>
/// <para>
/// There are four modes. Windowed is a window with the platform's frame, at the resolution and
/// where it was. Borderless is a window without the frame at the resolution, in the middle of its
/// screen with the desktop round it, and a resolution as large as the screen's covers the screen.
/// Borderless fullscreen always covers the screen at the screen's own mode. Fullscreen takes a mode
/// of the screen's, which Wayland gives no window and a screen that lists no mode at its own size
/// and rate cannot give, so a fullscreen window is refused there with the reason and keeps the mode
/// it holds. The picture the scene is drawn at is the render page's (<see cref="PictureSettings"/>)
/// in every mode.
/// </para>
/// <para>
/// The resolution is in the screen's pixels, kept by its text, <c>native</c> or <c>2560x1440</c>.
/// It is the window's size as a window and as a borderless one, the screen's mode in fullscreen and
/// unused in borderless fullscreen. Native is the game's own window size, the one it opens at and a
/// player's drag gives it, and the screen's own mode in fullscreen. The panel offers the sizes the
/// screen lists, the console takes any size up to the screen's for a window, and fullscreen takes
/// only a listed one, so a size the screen cannot take is refused with the reason, and one the file
/// holds falls to native at start with a warning, the file keeping it for a screen that takes it.
/// The window's size and place as a window are kept while it is in another mode and given back on
/// its return, since the platform gives back a size of its own choosing.
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
        /// A window without a frame at the resolution, in the middle of its screen, covering the
        /// screen at the screen's size.
        /// </summary>
        Borderless,

        /// <summary>The screen at its own mode, covered by a window without a frame.</summary>
        BorderlessFullscreen,

        /// <summary>The screen, at the mode of the screen's the resolution names.</summary>
        Fullscreen,
    }

    /// <summary>
    /// What an app's first window opens with, from its config and the settings file.
    /// </summary>
    internal sealed class Start
    {
        internal Mode Mode { get; set; }

        internal bool Resizable { get; set; } = true;

        /// <summary>The size the window opens at, in its logical pixels.</summary>
        internal uint Width { get; set; }

        /// <summary>See <see cref="Width"/>.</summary>
        internal uint Height { get; set; }

        /// <summary>The resolution in the screen's pixels, or none for native.</summary>
        internal (uint Width, uint Height)? Resolution { get; set; }

        /// <summary>
        /// The monitor by its index, or none for where the platform puts the window.
        /// </summary>
        internal int? Monitor { get; set; }

        internal bool Vsync { get; set; }

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
    private const string Native = "native";

    // What the settings write into while an app is being made, in place of its window.
    private static Start? _opening;

    // The resolution in the screen's pixels, none for native.
    private static (uint Width, uint Height)? _resolution;

    // The window's size as a window in its logical pixels, kept while it is in another mode, and
    // the game's own, the size it opened at.
    private static (uint Width, uint Height)? _windowed;
    private static (uint Width, uint Height) _native;

    // Where the window was as a window, from the desktop's corner, kept while it is in another
    // mode.
    private static (int X, int Y)? _place;

    // Whether the settings made the window borderless, and whether they took the platform's frame
    // away for it, which they give back on its return to a window.
    private static bool _borderless;
    private static bool _tookFrame;

    // Whether the window is yet to be given its resolution in the screen's pixels and, where it
    // opened borderless, its place in its screen's middle, once winit has listed the screens, which
    // it does after the window is made.
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
    /// left at (<see cref="WindowMemory"/>), with the settings file's lines of the page and of the
    /// render page over them where the run shows a window.
    /// </summary>
    /// <remarks>
    /// Each line of the two pages is given to its setting in the file's order as a start gives it,
    /// as the read of the whole file gives them as the app starts, so the two leave each setting at
    /// the same value, and a line the setting refuses is refused here and warned of then. The
    /// settings write the start meanwhile in place of a window, which winit makes only as the app
    /// runs, and the resolution is held to the screen once winit lists the screens
    /// (<see cref="Tick"/>).
    /// </remarks>
    internal static Start Open(Config config, WindowPlace place)
    {
        (_resolution, _windowed, _place, _seen, _held, _settle, _settleOn) = (null, null, null, null, null, false, null);
        (_native, _borderless, _tookFrame) = ((place.Width, place.Height), false, false);
        PictureSettings.Reset();
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
                if (line.Rest.Length > 0 && All.Concat(PictureSettings.All).FirstOrDefault(setting => setting.Name == line.Name) is { } setting)
                    setting.TryStart(line.Words.Count == 1 ? line.Words[0] : line.Rest);
        }
        finally
        {
            _opening = null;
        }

        _resolution = start.Resolution;
        (_settle, _settleOn) = (true, start.Monitor);
        _borderless = _tookFrame = start.Mode == Mode.Borderless;
        return start;
    }

    /// <summary>Whether a start is giving the window its settings from the file.</summary>
    internal static bool Opening => _opening is not null;

    /// <summary>
    /// Why the pages only read in a run whose window is not shown, or none where one is, or while
    /// a start gives the window its settings.
    /// </summary>
    internal static string? Unshown()
    {
        if (_opening is not null || App.Running?.World.TryGetResource<Config>(out var config) != true) return null;
        if (!App.HasRenderer) return "The bridge has no window to show, as its headless profile has none.";
        if (config.Headless) return "The program has no window and draws no frames, as a headless run does.";
        if (config.Offscreen) return "The program draws into images of its own with no window, as an offscreen run does.";
        return config.PrimaryWindow ? null : "The program opens no window of its own.";
    }

    /// <summary>Why a set reaches no window, or none where it does.</summary>
    internal static string? Unreached() => Unshown() ?? (App.Running is null ? NoApp : null);

    // What the system shows of the window, or what it showed when last asked where no world is
    // lent, or none where no window is shown.
    private static WindowShown? Look()
    {
        if (_opening is not null || App.Running is null || Unshown() is not null) return null;
        if (Window.Shown() is { } shown) _seen = shown;
        return _seen;
    }

    // The mode the window is in, a window the settings made borderless borderless, and a window
    // over its screen without a frame borderless fullscreen where the settings did not make it a
    // borderless one that covers its screen.
    private static Mode Of(WindowShown shown) => shown.Mode switch
    {
        WindowMode.BorderlessFullscreen => _borderless ? Mode.Borderless : Mode.BorderlessFullscreen,
        WindowMode.Fullscreen => Mode.Fullscreen,
        _ => _borderless ? Mode.Borderless : Mode.Windowed,
    };

    private static string Word(Mode mode) => mode.ToString().ToLowerInvariant();

    private static string Text((uint Width, uint Height)? size) => size is { } s ? $"{s.Width}x{s.Height}" : Native;

    // The resolution a text names, none for native, or false where it names none.
    private static bool TryParse(string text, out (uint Width, uint Height)? size)
    {
        size = null;
        var trimmed = text.Trim();
        if (string.Equals(trimmed, Native, StringComparison.OrdinalIgnoreCase)) return true;
        var sides = trimmed.Split('x', 'X');
        if (sides.Length != 2
            || !uint.TryParse(sides[0].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var width)
            || !uint.TryParse(sides[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var height)
            || width is 0 or > 16384 || height is 0 or > 16384)
            return false;
        size = (width, height);
        return true;
    }

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

    // A size in the screen's pixels as the window's logical ones, which its scale divides them
    // into.
    private static (uint Width, uint Height) Logical((uint Width, uint Height) size)
    {
        var scale = Math.Max(Window.Scale(), 0.01f);
        return ((uint)Math.Max(1, Math.Round(size.Width / scale)), (uint)Math.Max(1, Math.Round(size.Height / scale)));
    }

    // The window's size as a window in its logical pixels, the resolution's where one is set, and
    // otherwise its own while it is a window and the one kept in every other mode, or the game's
    // own where neither is known.
    private static (uint Width, uint Height) Windowed()
    {
        if (_opening is { } start) return (start.Width, start.Height);
        if (_resolution is { } size) return Logical(size);
        if (Look() is { } shown && Of(shown) == Mode.Windowed && Size() is { } now) return now;
        return _windowed ?? _native;
    }

    // The screen's number the system lists, the first where the window names none or one past the
    // last.
    private static int Index(int monitor, int count) => monitor >= 0 && monitor < count ? monitor : 0;

    // The sizes the screen lists, each once, the largest first, or none where it lists none.
    private static List<(uint Width, uint Height)> Listed(int monitor)
    {
        var count = Window.MonitorCount();
        if (count == 0) return [];
        return [.. Window.MonitorModes(Index(monitor, count)).Select(mode => (mode.Width, mode.Height)).Distinct()
            .OrderByDescending(size => (ulong)size.Width * size.Height).ThenByDescending(size => size.Width)];
    }

    // The place in the screen's list of its mode at the size with the highest rate, or none where
    // it lists none at the size.
    private static int? ModeAt(int monitor, (uint Width, uint Height) size)
    {
        var count = Window.MonitorCount();
        if (count == 0) return null;
        var modes = Window.MonitorModes(Index(monitor, count));
        int? best = null;
        for (var i = 0; i < modes.Length; i++)
            if ((modes[i].Width, modes[i].Height) == size && (best is not { } at || modes[i].RefreshHz > modes[at].RefreshHz))
                best = i;
        return best;
    }

    // Native and the sizes the window's screen lists, or native alone where no window is shown.
    private static IReadOnlyList<string> Offered()
    {
        if (_opening is not null || Unreached() is not null) return [Native];
        try
        {
            return [Native, .. Listed(Look()?.Monitor ?? 0).Select(size => Text(size))];
        }
        catch (BevyNativeException)
        {
            return [Native];
        }
    }

    // Why the screen does not take the resolution in the mode, or none where it does. Fullscreen
    // takes a size the screen lists, and the other modes any size up to the screen's.
    private static string? Refusal((uint Width, uint Height) size, Mode mode, int monitor)
    {
        if (mode == Mode.Fullscreen)
            return Listed(monitor).Contains(size) ? null : $"The screen lists no mode of {Text(size)} for fullscreen, which takes one of the sizes the panel offers.";
        if (Screen(monitor) is { } screen && (size.Width > screen.Width || size.Height > screen.Height))
            return $"The screen is {screen.Width}x{screen.Height}, so a window takes no size larger than it.";
        return null;
    }

    private static IEnumerable<Setting> Page()
    {
        var made = new Config();

        // Each carries its place on the page, the mode and the size first, since the panel lists a
        // page's settings by name where none is given.
        yield return new Setting<Mode>("window.mode",
            "How the window covers its screen, as a window, as a window without a frame in the screen's middle that covers the screen at the screen's size, over the screen at its own mode without a frame, or fullscreen at the mode the resolution names.",
            Mode.Windowed, () => _opening?.Mode ?? (Look() is { } shown ? Of(shown) : Mode.Windowed), apply: SetMode)
        { Order = 0, Lock = Unshown };

        yield return new Setting<string>("window.resolution",
            "The window's size in the screen's pixels as a window and as a borderless one, and the screen's mode in fullscreen, native for the game's own window size and the screen's own mode, or one of the sizes the screen lists, the console taking any size up to the screen's for a window.",
            Native, () => Text(_opening is { } start ? start.Resolution : _resolution), apply: SetResolution)
        { Order = 1, Offer = Offered, Lock = Unshown };

        yield return new Setting<bool>("window.resizable", "Whether the window's edges can be dragged to resize it.", true,
            () => _opening?.Resizable ?? Look()?.Resizable ?? true, apply: SetResizable)
        { Order = 2, Lock = Unshown };

        yield return new Setting<int>("window.monitor",
            "The screen the window is on, by its number from 0, where there is more than one, the window centered on another as it moves there.", 0,
            () => _opening?.Monitor ?? Math.Max(0, Look()?.Monitor ?? 0), apply: SetMonitor)
        { Order = 3, Low = 0, High = 15, Step = 1, Lock = () => Unshown() ?? (_opening is null && App.Running is not null && Window.MonitorCount() < 2 ? OneScreen : null) };

        yield return new Setting<bool>("window.vsync", "Whether a frame waits for the display, the window's surface made again before the next frame.", made.Vsync,
            () => _opening?.Vsync ?? Look()?.Vsync ?? made.Vsync, apply: SetVsync)
        { Order = 4, Lock = Unshown };
    }

    // Asks for the mode, refusing it with the reason where the system would not give it, so the
    // window keeps the one it holds. The size and place as a window are kept on leaving one and
    // given back on returning to one, and fullscreen takes the resolution's mode where the screen
    // lists it and its own otherwise.
    private static string? SetMode(Mode mode)
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
        var size = Windowed();
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
            if (_resolution is null && Size() is { } now) _windowed = now;
            if (Window.Place() is { HasPosition: true } place) _place = (place.X, place.Y);
        }
        (_held, _borderless) = (null, false);
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
            case Mode.Fullscreen when _resolution is { } listed && ModeAt(shown.Monitor, listed) is { } index:
                Window.SetVideoMode(Index(shown.Monitor, Window.MonitorCount()), index);
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
        return Window.Monitor(Index(monitor, count));
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
    /// Gives a newly opened window its resolution and place once the screens are listed, and holds
    /// the size and place given to the window as it changes mode for a few frames, against the
    /// size a window manager restores of its own once the mode has changed.
    /// </summary>
    /// <remarks>
    /// Called each frame from the engine's frame sync, as <see cref="WindowMemory.Tick"/> is.
    /// Winit lists the screens only after it has made the window, so a window opens at its
    /// resolution taken as logical pixels, and a fullscreen one at the screen's own mode, and in
    /// the first frame the screens are known it is given the resolution in the screen's pixels, or
    /// native with a warning where the screen cannot take it, and a window opened borderless is
    /// placed in its screen's middle or made to cover it. The size and place are set in the frame
    /// the mode is, which a platform with no window manager keeps, and a manager answers the change
    /// of mode a few frames on with the size it saved, which mutter gives an X11 window shorter
    /// than the one it had. So they are set again in each frame they differ until the frames run
    /// out, and a resize or a move after them is the player's.
    /// </remarks>
    internal static void Tick()
    {
        if (_settle && Look() is { } opened && Window.MonitorCount() > 0)
        {
            _settle = false;
            Settle(opened, _settleOn ?? opened.Monitor);
        }

        if (_held is not { } held) return;
        _held = held.Frames > 1 ? held with { Frames = held.Frames - 1 } : null;
        if (Look() is not { Mode: WindowMode.Windowed } shown || Of(shown) != held.Mode) return;
        if (Size() is { } now && now != (held.Width, held.Height)) Window.SetSize(held.Width, held.Height);
        if (held.At is { } at && Window.Place() is { HasPosition: true } place && (place.X, place.Y) != at) Window.SetPosition(at.X, at.Y);
    }

    // Gives a newly opened window its resolution in the screen's pixels, or native with a warning
    // where the screen cannot take it, the file keeping it for a screen that does.
    private static void Settle(WindowShown opened, int monitor)
    {
        var mode = Of(opened);
        if (_resolution is { } size && Refusal(size, mode, monitor) is { } refused)
        {
            Log.Warn($"[BevyCSharp] window.resolution {Text(size)} is not taken at start. {refused} The window opens at native.");
            _resolution = null;
        }
        switch (mode)
        {
            case Mode.Windowed when _resolution is { } asked:
                var logical = Logical(asked);
                if (Size() != logical) Window.SetSize(logical.Width, logical.Height);
                break;
            case Mode.Borderless:
                Borderless(Windowed(), monitor);
                break;
            case Mode.Fullscreen when _resolution is { } listed && ModeAt(monitor, listed) is { } index:
                Window.SetVideoMode(Index(monitor, Window.MonitorCount()), index);
                break;
        }
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

    // Sets the resolution, the window's size at once as a window and as a borderless one, the
    // screen's mode at once in fullscreen, and kept for a window's return in borderless fullscreen,
    // or refuses one the screen cannot take with the reason, the window keeping the one it has.
    // Native gives a window the game's own size and fullscreen the screen's own mode.
    private static string? SetResolution(string text)
    {
        if (!TryParse(text, out var size))
            return $"window.resolution takes native or a width by height such as 1920x1080, not {CommandLine.Quote(text)}.";
        if (_opening is { } start)
        {
            start.Resolution = size;
            (start.Width, start.Height) = size ?? _native;
            return null;
        }
        if (Unreached() is { } why) return why;
        if (Look() is not { } shown) return NotMade;

        var mode = Of(shown);
        if (size is { } wanted && Refusal(wanted, mode, shown.Monitor) is { } refused)
            return $"{refused} The window stays at {Text(_resolution)}.";
        _resolution = size;
        var window = size is { } asked ? Logical(asked) : _native;
        switch (mode)
        {
            case Mode.Windowed:
                Window.SetSize(window.Width, window.Height);
                break;
            case Mode.Borderless:
                Borderless(window, shown.Monitor);
                break;
            case Mode.Fullscreen when size is { } listed && ModeAt(shown.Monitor, listed) is { } index:
                Window.SetVideoMode(Index(shown.Monitor, Window.MonitorCount()), index);
                break;
            case Mode.Fullscreen:
                Window.SetMode(WindowMode.Fullscreen);
                break;
        }
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
        if (Of(shown) == Mode.Borderless) Borderless(Windowed(), screen);
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
}
