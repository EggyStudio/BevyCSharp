namespace Bevy;

/// <summary>
/// The managed systems' times while a frame profile measures, which every system's run counts and
/// the profile (<c>FrameProfile</c>) takes.
/// </summary>
/// <remarks>
/// They stand in Core, where every system runs, so a system of any area counts its time here, and
/// the profile, which reads the bridge's and the renderer's counts as well, stands with the
/// diagnostics.
/// </remarks>
internal static class SystemTimes
{
    private static readonly Lock Gate = new();
    private static readonly Dictionary<string, (long Ticks, long Runs)> BySystem = new(StringComparer.Ordinal);

    /// <summary>Whether a measurement is running.</summary>
    internal static bool On { get; private set; }

    /// <summary>Starts counting, from nothing.</summary>
    internal static void Start()
    {
        lock (Gate) BySystem.Clear();
        On = true;
    }

    /// <summary>Stops counting.</summary>
    internal static void Stop() => On = false;

    /// <summary>Counts a managed system's run. Called by <see cref="SystemDescriptor.Invoke"/>.</summary>
    internal static void Ran(string name, long ticks)
    {
        lock (Gate)
        {
            BySystem.TryGetValue(name, out var was);
            BySystem[name] = (was.Ticks + ticks, was.Runs + 1);
        }
    }

    /// <summary>Each system's time and runs since counting started or the last take, which starts the count over.</summary>
    internal static List<(string Name, long Ticks, long Runs)> Take()
    {
        lock (Gate)
        {
            List<(string, long, long)> taken = [.. BySystem.Select(pair => (pair.Key, pair.Value.Ticks, pair.Value.Runs))];
            BySystem.Clear();
            return taken;
        }
    }
}
