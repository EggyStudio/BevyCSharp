namespace Bevy;

internal static partial class ConsoleWorldCommands
{
    /// <summary>Holds a set clock still while a load runs, between commands, both, or neither.</summary>
    /// <remarks>
    /// <para>
    /// <c>loads</c> holds it while an asset the app asked for, or one it depends on, is loading,
    /// and <c>commands</c> between the commands of this session, so it moves only while one waits,
    /// as <c>frames.wait</c> and <c>input.hold</c> do. A script whose commands reach the app a few
    /// frames apart on a slow device and many on a fast one then finds the game at the same moment
    /// of its clock after each. <c>off</c> lets it run, and nothing given says what holds it.
    /// </para>
    /// <para>
    /// A hold keeps a set clock alone still (<c>app.frametime</c>), and the machine's clock runs on.
    /// <c>bcs open --hold</c> asks for one from the app's first frame.
    /// </para>
    /// </remarks>
    [Command("app.hold", "Holds a set clock still while a load runs or between commands: app.hold [loads|commands|loads,commands|off]")]
    internal static string HoldClock(string line)
    {
        var time = ConsoleHost.Time;
        var asked = line.Trim();
        if (asked.Length == 0) return Held(time.Holds);

        if (!ClockHoldNames.TryParse(asked, out var holds))
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{asked}' is not loads, commands, both with a comma between, or off.");
            return $"'{asked}' names no hold";
        }

        time.Holds = holds;
        return Held(holds);

        static string Held(ClockHolds holds) => holds == ClockHolds.None
            ? "nothing holds the clock"
            : $"the clock is held for {ClockHoldNames.Name(holds)}";
    }
}
