using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>Counts of the things the sample cares about, reported at shutdown.</summary>
public sealed class SampleReport
{
    /// <summary>Frames observed.</summary>
    public ulong Frames { get; set; }

    /// <summary>Entities still alive at the last count.</summary>
    public int Spinners { get; set; }

    /// <summary>Temporary entities still alive at the last count.</summary>
    public int Lifetimes { get; set; }

    /// <summary>Entities that reached the floor.</summary>
    public int Grounded { get; set; }
}

/// <summary>
/// Reports what the world looks like, on a key the user can toggle.
/// </summary>
/// <remarks>
/// <c>[ToggleKey]</c> is the whole implementation of "press F3 to show the overlay": no
/// resource to declare, no key handler to write, no condition to wire up. The state lives in
/// <see cref="SystemToggleRegistry"/>, so it survives across frames and can be driven from a
/// menu or a test as well as from the keyboard.
/// </remarks>
[Behavior]
public partial struct Hud
{
    /// <summary>Samples the world each frame while the overlay is enabled.</summary>
    [OnRender]
    [ToggleKey(Key.F3)]
    public static void Sample(BehaviorContext ctx)
    {
        var report = ctx.World.GetOrInsertResource(static () => new SampleReport());

        report.Frames = ctx.Time.FrameCount;
        report.Spinners = ctx.Ecs.Count<Spinner>();
        report.Lifetimes = ctx.Ecs.Count<Lifetime>();
        report.Grounded = ctx.Ecs.Count<Grounded>();
    }

    /// <summary>Prints the final state once the loop has exited.</summary>
    [OnCleanup]
    public static void Report(BehaviorContext ctx)
    {
        if (!ctx.TryRes<SampleReport>(out var report))
        {
            Console.WriteLine("[Hud] no samples were taken");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("=== final state ===");
        Console.WriteLine($"  frames     : {report.Frames}");
        Console.WriteLine($"  spinners   : {report.Spinners}");
        Console.WriteLine($"  lifetimes  : {report.Lifetimes} (started at 4)");
        Console.WriteLine($"  grounded   : {report.Grounded}");
    }
}

/// <summary>
/// Prints a line every so often, but only while the app says it is verbose.
/// </summary>
/// <remarks>
/// Shows <c>[RunIf]</c> pointing at a static bool on the same struct. The generator checks that
/// the member exists at compile time, so a rename cannot silently disable the system.
/// </remarks>
[Behavior]
public partial struct Trace
{
    /// <summary>Whether trace output is wanted. Set from <c>Program</c>.</summary>
    public static bool Verbose;

    /// <summary>Prints a progress line every 20 frames.</summary>
    [OnLast]
    [RunIf(nameof(Verbose))]
    public static void Print(BehaviorContext ctx)
    {
        if (ctx.Time.FrameCount % 20 != 0) return;

        Console.WriteLine(
            $"  frame {ctx.Time.FrameCount,4}  "
            + $"elapsed {ctx.Time.ElapsedSeconds,6:F3}s  "
            + $"spinners {ctx.Ecs.Count<Spinner>()}  "
            + $"lifetimes {ctx.Ecs.Count<Lifetime>()}");
    }
}
