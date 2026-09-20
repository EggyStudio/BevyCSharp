using Bevy;

namespace BevyCSharp.Cli;

/// <summary>What this tool is, in one screen.</summary>
internal static class Help
{
    /// <summary>Prints the usage.</summary>
    public static int Print()
    {
        Console.WriteLine(
            """
            bcs - drives a running BevyCSharp app from the command line.

            Live session
              status                     What is running, and whether it is answering
              open [--editor|--sample]   Start one serving, detached, and wait until it is ready
                [--offscreen]            Draw into an image instead of a window, with no display
              list                       Every command the connected app offers, with its arguments
              command <name> [args]      Run one against the live world
              shot <path>                Capture the window and wait for the file
              logs [-n N] [--follow]     What a launched app has been saying
              stop                       Ask it to close

            Cold
              build [--render|--editor]  The native bridge, then the managed side, in that order
              test [--filter F]          The test suite; exit 8 means tests failed, 6 means it never ran
              run [-- args]              One headless run of the sample
              doctor                     Why nothing works: the bridge, the ABI, stale sessions

            Anywhere
              --json                     Print the envelope instead of a sentence
              --name <entry>             Which app, when more than one is serving
              --session <pid>            Which app, by process id
              --project <path>           Which app, by the directory it was started from
              --timeout <seconds>        How long to wait for an answer (default 30)
              --quiet                    Say only what was asked for
              --                         Everything after this is passed through untouched

            Every verb answers with one JSON envelope on stdout under --json:
              { "success": true, "command": "...", "data": {...}, "errors": [], "warnings": [] }
            Branch on "success"; errors[0].code is the stable token. Exit codes: 0 ok, 2 bad
            arguments, 4 nothing to talk to, 6 it failed, 8 tests failed.
            """);

        return Exit.Ok;
    }

    /// <summary>Says which version this is.</summary>
    public static int Version(Options options)
    {
        var version = typeof(Help).Assembly.GetName().Version?.ToString() ?? "0.0.0";

        return options.Json
            ? Output.Print(options, CliJson.Ok(
                "version", writer => writer.WriteString("version", version)))
            : Output.Say(options, "version", $"bcs {version}");
    }

    /// <summary>Says a verb is not one.</summary>
    public static int Unknown(Options options, string verb) => Output.Refuse(
        options,
        "bcs",
        "UNKNOWN_VERB",
        $"'{verb}' is not a bcs verb. Run 'bcs help' for the list. To run a command against a "
        + $"live app, that is 'bcs command {verb}'.");
}
