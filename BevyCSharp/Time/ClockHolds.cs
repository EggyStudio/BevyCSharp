namespace Bevy;

/// <summary>
/// What holds a set clock still, for a run that has to land on the same moment of its clock on
/// every device, which a session asks for with <c>bcs open --hold</c> or the <c>app.hold</c>
/// command.
/// </summary>
/// <remarks>
/// <para>
/// A held frame runs its systems, draws and reads its input as any other, and passes no time to
/// the game, to the fixed steps or to Bevy's real clock. So a frame counted on the clock,
/// <see cref="Time.ClockFrames"/>, is the same moment of the game on a device that spent more
/// frames loading or waiting than another did.
/// </para>
/// <para>
/// Only a set clock (<see cref="Config.FrameSeconds"/>) is held. The machine's clock reads the time
/// since the last frame it counted, so one held would hand the whole of the hold to the frame after.
/// </para>
/// </remarks>
[Flags]
internal enum ClockHolds
{
    /// <summary>Nothing holds the clock.</summary>
    None = 0,

    /// <summary>
    /// While an asset the app asked for, or one of the assets it depends on, is loading.
    /// </summary>
    /// <remarks>
    /// A model's animation started once the model has loaded starts in whichever frame the load
    /// ends, which a device drawing slowly reaches in fewer frames, so a picture taken at a frame
    /// caught it in another pose on each. Held, every device starts it at the same moment of the
    /// clock. An asset built in memory is never loading, and one that failed has stopped.
    /// </remarks>
    Loads = 1,

    /// <summary>
    /// Between the commands of a session served to <c>bcs</c>, so the clock moves only while a
    /// command waits, as <c>frames.wait</c> and <c>input.hold</c> do.
    /// </summary>
    /// <remarks>
    /// A script's commands reach the app a few frames apart on a device drawing slowly and many on
    /// a fast one, so on a running clock each lands at another moment of the game. Held, a script
    /// walking a player on to a swinging platform finds it at the same point of its swing on both.
    /// </remarks>
    Commands = 2,
}

/// <summary>The words <see cref="ClockHolds"/> are named by, on the command line and in the environment.</summary>
internal static class ClockHoldNames
{
    /// <summary>
    /// Reads <c>loads</c>, <c>commands</c>, both with a comma between, or <c>off</c> for neither.
    /// </summary>
    /// <returns>Whether every word named a hold.</returns>
    public static bool TryParse(string? words, out ClockHolds holds)
    {
        holds = ClockHolds.None;
        if (string.IsNullOrWhiteSpace(words)) return false;

        foreach (var word in words.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (word.ToLowerInvariant())
            {
                case "loads": holds |= ClockHolds.Loads; break;
                case "commands": holds |= ClockHolds.Commands; break;
                case "off" or "none": break;
                default: return false;
            }
        }

        return true;
    }

    /// <summary>The holds as the words <see cref="TryParse"/> reads, or <c>off</c>.</summary>
    public static string Name(ClockHolds holds) => holds switch
    {
        ClockHolds.None => "off",
        ClockHolds.Loads => "loads",
        ClockHolds.Commands => "commands",
        _ => "loads,commands",
    };
}
