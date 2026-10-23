using System.Globalization;
using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The feature test's section of the engine's debug overlay, what the program holds as the
/// <c>memory</c> command says it, its names and numbers a few to a line, bytes in megabytes.
/// </summary>
/// <remarks>
/// <para>
/// The engine's overlay says the entities, the assets and the process's memory, and the section
/// says all the command reads, the objects the last full collection kept and the heap it left, the
/// collections, the process's peak, the handles held and the bridge's allocations by kind, which a
/// soak watches climb. They are a few to a line, as many as fit in about sixty letters, since a
/// line each ran past the bottom of a window 720 pixels high.
/// </para>
/// <para>
/// The command is asked with the app's world lent, since it counts the handles only with one, and
/// the overlay asks its sections from inside a frame, where the world is the app's. The section
/// is taken away as the app ends, so it never lends a world after its app.
/// </para>
/// </remarks>
[Behavior]
public partial struct Held
{
    /// <summary>The section's heading, the command's own words for what it says.</summary>
    public const string Section = "What the program holds";

    /// <summary>Adds the section as the app starts.</summary>
    [OnStartup]
    public static void Add(BehaviorContext ctx)
    {
        var world = ctx.World;
        Debug.AddOverlaySection(Section, () => Lines(world));
    }

    /// <summary>Takes it away as the app ends.</summary>
    [OnCleanup]
    public static void Remove(BehaviorContext ctx) => Debug.RemoveOverlaySection(Section);

    /// <summary>The memory command's answer, its names and numbers a few to a line, bytes in megabytes.</summary>
    internal static string[] Lines(World world)
    {
        string answer;
        using (ConsoleHost.Lend(world)) answer = ConsoleCommands.Run("memory") ?? string.Empty;

        var words = answer.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var line = "";
        for (var i = 0; i + 1 < words.Length; i += 2)
        {
            if (!long.TryParse(words[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)) continue;

            var name = words[i];
            var bytes = name is "live" or "heap" or "process" or "peak" or "nativeBytes";
            var pair = bytes
                ? string.Create(CultureInfo.InvariantCulture, $"{name} {number / (1024.0 * 1024.0):0.0} MB")
                : string.Create(CultureInfo.InvariantCulture, $"{name} {number}");

            if (line.Length > 0 && line.Length + 2 + pair.Length > 60)
            {
                lines.Add(line);
                line = "";
            }

            line = line.Length == 0 ? pair : $"{line}, {pair}";
        }

        if (line.Length > 0) lines.Add(line);
        return [.. lines];
    }
}
