using Bevy.Interop;
using System.Text.Json.Serialization;

namespace Bevy;

/// <summary>
/// Keeps where the window was left between runs, for <see cref="Config.RememberWindow"/>.
/// </summary>
/// <remarks>
/// <para>
/// Read once before the window opens, since a window told its place as it is made opens there,
/// and one moved after it opens is seen to jump. Then checked every half second of frames and
/// written when what it would keep has changed, rather than once on the way out, because a game
/// that crashes or is killed never gets that far and would come back where it was the run before.
/// </para>
/// <para>
/// One app at a time owns a window, so the state is static, set up by <see cref="Open"/> and read
/// by <see cref="Tick"/>, which an app without the option leaves untouched.
/// </para>
/// </remarks>
internal static class WindowMemory
{
    /// <summary>How many frames pass between looks at the window.</summary>
    private const int Every = 30;

    private static Persistent<WindowPlace>? _kept;
    private static bool _maximize;
    private static int _frames;

    /// <summary>
    /// Where the window opens and how large: the place kept from the last run when the app asks to
    /// remember it and there is one, otherwise the size the config asks for.
    /// </summary>
    internal static WindowPlace Open(Config config)
    {
        _kept = null;
        _maximize = false;
        _frames = 0;

        var asked = new WindowPlace(false, 0, 0, config.Width, config.Height, false);
        if (!config.RememberWindow || config.Headless || config.Offscreen) return asked;

        _kept = new Persistent<WindowPlace>("window", WindowJson.Default.WindowPlace, () => asked);

        var kept = _kept.Value;
        if (kept.Width == 0 || kept.Height == 0) return asked;

        // Maximized once the window is there, since a window has to exist to be maximized, and it
        // opens at the size it goes back to.
        _maximize = kept.Maximized;
        return kept;
    }

    /// <summary>Looks at the window now and then, and keeps its place when it has changed.</summary>
    internal static void Tick()
    {
        if (_kept is null) return;

        if (_maximize)
        {
            _maximize = false;
            Window.SetMaximized(true);
        }

        if (++_frames % Every != 0) return;

        WindowPlace now;
        try
        {
            now = Window.Place();
        }
        catch (BevyNativeException)
        {
            // Closed, or closing, between frames. The place kept last is the one to keep.
            return;
        }

        var next = Next(_kept.Value, now);
        if (next == _kept.Value) return;

        _kept.Set(next);
        _kept.Persist();
    }

    /// <summary>What to keep, given what was kept and where the window is now.</summary>
    /// <remarks>
    /// <para>
    /// A maximized window keeps the size and place it had before, with a mark that it was
    /// maximized, so the next run opens it at that size and maximizes it, and putting it back
    /// gives the window somebody chose rather than one the size of the screen.
    /// </para>
    /// <para>
    /// Where the platform says nothing of the place, as on Wayland, the place kept before stays,
    /// which on such a platform is none, and a window not yet moved keeps the one it opened at.
    /// </para>
    /// </remarks>
    internal static WindowPlace Next(WindowPlace kept, WindowPlace now)
    {
        if (now.Maximized) return kept with { Maximized = true };
        if (now.Width == 0 || now.Height == 0) return kept;

        return now.HasPosition
            ? now
            : now with { HasPosition = kept.HasPosition, X = kept.X, Y = kept.Y };
    }
}

/// <summary>How a <see cref="WindowPlace"/> is written, generated so nothing reflects.</summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(WindowPlace))]
internal sealed partial class WindowJson : JsonSerializerContext;
