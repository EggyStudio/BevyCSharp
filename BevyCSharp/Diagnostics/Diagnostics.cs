using System.Globalization;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Bevy's diagnostics store: named measures, a frame's time or a count of enemies, each with its
/// history, which Bevy's own plugins measure into and a game registers its own in.
/// </summary>
/// <remarks>
/// <para>
/// A diagnostic is a path, such as <c>fps</c> or <c>game/enemies</c>, and the measurements taken of
/// it, from which the store keeps the last, a value smoothed over time and the average of the
/// history it keeps. Bevy's plugins (<see cref="Config.DiagnosticPlugins"/>) measure frames,
/// entities and render passes, and Bevy's log of them prints every diagnostic once a second, a
/// game's own among them, with its suffix after the number.
/// </para>
/// <para>
/// A game's diagnostic is registered before it is measured, since a measurement of a path the
/// store does not have is dropped, as Bevy's own are. Each call reaches the world, so it is valid
/// only inside a system.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// app.Startup(ctx => Diagnostics.Register("game/enemies", " enemies"));
/// app.Update(ctx => Diagnostics.Measure("game/enemies", ctx.Ecs.EntitiesWith&lt;Enemy&gt;().Length));
/// </code>
/// </example>
public static unsafe class Diagnostics
{
    /// <summary>Frames a second, which <see cref="DiagnosticPlugins.FrameTime"/> measures.</summary>
    public const string Fps = "fps";

    /// <summary>Frames since the app started, which <see cref="DiagnosticPlugins.FrameTime"/> measures.</summary>
    public const string FrameCount = "frame_count";

    /// <summary>
    /// Milliseconds a frame takes, which <see cref="DiagnosticPlugins.FrameTime"/> measures.
    /// </summary>
    public const string FrameTime = "frame_time";

    /// <summary>
    /// How many entities the world has, which <see cref="DiagnosticPlugins.EntityCount"/> measures.
    /// </summary>
    public const string EntityCount = "entity_count";

    /// <summary>Registers a diagnostic, so measurements of it are kept. Only valid inside a system.</summary>
    /// <remarks>
    /// Registering a path again starts its history over. A path is made of names apart by slashes,
    /// with none empty, so it neither begins nor ends with one, as Bevy has it.
    /// </remarks>
    /// <param name="path">What it is called, such as <c>game/enemies</c>.</param>
    /// <param name="suffix">What Bevy's log prints after its numbers, such as <c>" ms"</c>.</param>
    /// <param name="history">How many measurements are kept for the average, or zero for Bevy's own number.</param>
    /// <exception cref="ArgumentException">The path is one Bevy refuses.</exception>
    public static void Register(string path, string suffix = "", int history = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(suffix);
        ArgumentOutOfRangeException.ThrowIfNegative(history);

        var answer = Native.bcs_diagnostic_register(path, suffix, (uint)history);
        if (answer == NativeStatus.InvalidState)
            throw new ArgumentException($"\"{path}\" is not a diagnostic's path, which is names apart by single slashes.", nameof(path));

        Native.Check(answer, $"registering the diagnostic {path}");
    }

    /// <summary>Adds a measurement to a diagnostic, taken now. Only valid inside a system.</summary>
    /// <returns>
    /// Whether it was kept, which it is not for a path nobody registered or one turned off
    /// (<see cref="SetEnabled"/>).
    /// </returns>
    public static bool Measure(string path, double value)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var answer = Native.bcs_diagnostic_measure(path, value);
        if (answer is NativeStatus.NotPresent or NativeStatus.InvalidState) return false;

        Native.Check(answer, $"measuring the diagnostic {path}");
        return true;
    }

    /// <summary>What a diagnostic holds now. Only valid inside a system.</summary>
    /// <returns>Whether the store has the path, and so whether the reading is there.</returns>
    public static bool TryRead(string path, out DiagnosticReading reading)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        reading = default;

        NativeDiagnostic read;
        var answer = Native.bcs_diagnostic_read(path, &read);
        if (answer == NativeStatus.NotPresent) return false;
        Native.Check(answer, $"reading the diagnostic {path}");

        reading = new DiagnosticReading(path, "", read.Enabled != 0, (int)read.History, Some(read.Value), Some(read.Smoothed), Some(read.Average));
        return true;

        static double? Some(double value) => double.IsNaN(value) ? null : value;
    }

    /// <summary>
    /// Turns a diagnostic's measuring and its line in Bevy's log on or off, its history kept. Only
    /// valid inside a system.
    /// </summary>
    /// <returns>Whether the store has the path.</returns>
    public static bool SetEnabled(string path, bool on)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var answer = Native.bcs_diagnostic_set_enabled(path, on ? 1 : 0);
        if (answer == NativeStatus.NotPresent) return false;

        Native.Check(answer, $"turning the diagnostic {path} {(on ? "on" : "off")}");
        return true;
    }

    /// <summary>Every diagnostic in the store, by path. Only valid inside a system.</summary>
    /// <remarks>
    /// Bevy's own and a game's alike, with the render passes' where they are measured, so it is
    /// the list a panel showing them all reads, once a frame at most.
    /// </remarks>
    public static IReadOnlyList<DiagnosticReading> All()
    {
        var text = Native.ReadText((buffer, capacity) => Native.bcs_diagnostics_list(buffer, capacity), "listing diagnostics");
        var readings = new List<DiagnosticReading>();

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');
            if (parts.Length < 7) continue;

            readings.Add(new DiagnosticReading(
                parts[0],
                parts[1],
                parts[2] == "1",
                int.Parse(parts[3], CultureInfo.InvariantCulture),
                Number(parts[4]),
                Number(parts[5]),
                Number(parts[6])));
        }

        return readings;

        // Rust writes an infinity as inf, which .NET reads by another name.
        static double? Number(string text) => text switch
        {
            "" => null,
            "inf" => double.PositiveInfinity,
            "-inf" => double.NegativeInfinity,
            _ => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
        };
    }

    /// <summary>
    /// Has Bevy's log print only these diagnostics, or every one when given nothing. Only valid
    /// inside a system.
    /// </summary>
    /// <remarks>
    /// An empty list prints none, which is how a game silences the log while keeping its measures.
    /// The log is there where the app asks for it (<see cref="DiagnosticPlugins.Log"/>).
    /// </remarks>
    /// <param name="paths">The paths to print, or null for all of them.</param>
    /// <exception cref="InvalidOperationException">The app has no log of its diagnostics.</exception>
    public static void SetLogFilter(IEnumerable<string>? paths)
    {
        var answer = Native.bcs_diagnostics_set_log_filter(paths is null ? null : string.Join('\n', paths));
        if (answer == NativeStatus.InvalidState)
            throw new InvalidOperationException("The app logs no diagnostics, which Config.DiagnosticPlugins with DiagnosticPlugins.Log asks for.");

        Native.Check(answer, "filtering the log of diagnostics");
    }
}
