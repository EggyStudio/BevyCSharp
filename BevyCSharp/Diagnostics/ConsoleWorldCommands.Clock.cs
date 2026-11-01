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

    /// <summary>Stops or starts the game's clock.</summary>
    /// <remarks>
    /// The window, the interface and this console go on while the game is paused, so a stopped
    /// world can be looked at, changed with <c>entity.set</c> and stepped a frame at a time.
    /// </remarks>
    [Command("app.pause", "Stops the game's clock, or starts it again: app.pause [on|off]")]
    internal static string Pause(string line)
    {
        var time = ConsoleHost.Time;
        var wanted = line.Trim() switch
        {
            "on" or "true" or "1" => true,
            "off" or "false" or "0" => false,
            _ => !time.Paused,
        };

        if (wanted) time.Pause();
        else time.Resume();

        return wanted ? "paused" : "running";
    }

    /// <summary>Runs a paused game's clock for a few frames and stops it again.</summary>
    [Command("app.step", "Runs the game's clock for some frames, then stops it: app.step [frames]")]
    internal static string Step(string line)
    {
        var frames = int.TryParse(line.Trim(), out var asked) ? Math.Clamp(asked, 1, 10_000) : 1;
        ConsoleHost.Time.Step(frames);
        return frames == 1 ? "stepping a frame" : $"stepping {frames} frames";
    }

    /// <summary>Sets how fast the game's clock runs.</summary>
    [Command("app.speed", "How fast the game's clock runs, one being real time: app.speed <times>")]
    internal static string Speed(string line)
    {
        var time = ConsoleHost.Time;
        if (line.Trim().Length == 0) return $"speed {time.Speed:0.###}";

        if (!float.TryParse(line.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var speed) || speed < 0f)
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{line.Trim()}' is not a speed of zero or more.");
            return $"'{line.Trim()}' is not a speed";
        }

        time.SetSpeed(speed);
        return $"speed {speed:0.###}";
    }

    /// <summary>Sets the seconds each frame advances the clock by, or lets the machine's clock run.</summary>
    /// <remarks>
    /// So a session is scripted in frames, as a test is, and <c>frames.wait 60</c> after
    /// <c>app.frametime 0.016666</c> is a second of the game whatever the machine took.
    /// </remarks>
    [Command("app.frametime", "Seconds each frame advances the clock by, or off for the machine's clock: app.frametime [seconds|off]")]
    internal static string FrameTime(string line)
    {
        var time = ConsoleHost.Time;
        var asked = line.Trim();
        if (asked.Length == 0) return Clock(time.FrameSeconds);

        var seconds = asked is "off" or "0" ? 0.0 : double.NaN;
        if (double.IsNaN(seconds)
            && (!double.TryParse(asked, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds)
                || !double.IsFinite(seconds) || seconds <= 0.0))
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{asked}' is not a length of time more than zero, or off.");
            return $"'{asked}' is not a length of time";
        }

        time.FrameSeconds = seconds;
        return Clock(seconds);

        static string Clock(double seconds) => seconds > 0.0
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"each frame is {seconds:0.######} seconds")
            : "the machine's clock";
    }
}
