using System.Text.RegularExpressions;
using Bevy;

namespace BevyCSharp.Cli;

/// <summary>The cold verbs: building, testing, a one-shot run, and why nothing works.</summary>
internal static partial class Tools
{
    /// <summary>
    /// Builds the bridge and then the managed side, in that order.
    /// </summary>
    /// <remarks>
    /// The order is the point. A rebuilt bridge is invisible until a managed build copies it beside
    /// each project's binaries, so a native-only rebuild leaves every app running the previous one,
    /// which looks exactly like a change that did nothing.
    /// </remarks>
    public static int Build(Options options, string[] arguments)
    {
        if (Repo.Root is null)
        {
            return Output.Refuse(
                options, "build", "NO_CHECKOUT", "This is not inside a BevyCSharp checkout.");
        }

        var profile = arguments.FirstOrDefault(
            argument => argument is "--render" or "--editor" or "--headless");

        var managed = !arguments.Contains("--no-managed");
        var native = !arguments.Contains("--no-native");
        var echo = !options.Json && !options.Quiet;

        Ran bridge = new(0, string.Empty);

        if (native)
        {
            // The two scripts are twins that produce identical output, and they spell their
            // profiles differently, so the flag is chosen with the script rather than passed
            // through.
            bridge = OperatingSystem.IsWindows()
                ? Shell.Run(
                    "pwsh",
                    [
                        "-File", Path.Combine(Repo.Root, "build", "build-native.ps1"),
                        .. Profile(profile, windows: true),
                    ],
                    Repo.Root,
                    echo)
                : Shell.Run(
                    Path.Combine(Repo.Root, "build", "build-native.sh"),
                    Profile(profile, windows: false),
                    Repo.Root,
                    echo);

            if (!bridge.Ok)
            {
                return Output.Print(options, Failure(
                    "build", "BUILD_FAILED",
                    $"The native bridge did not build (exit {bridge.Code}).", bridge));
            }
        }

        if (!managed)
        {
            return Output.Say(options, "build", "the bridge is built; the managed side was skipped");
        }

        var solution = Shell.Run("dotnet", ["build", "BevyCSharp.slnx", "--nologo"], Repo.Root, echo);

        if (!solution.Ok)
        {
            return Output.Print(options, Failure(
                "build", "BUILD_FAILED",
                $"The managed build failed (exit {solution.Code}).", solution));
        }

        return Output.Print(
            options,
            CliJson.Ok("build", writer =>
            {
                if (native) writer.WriteString("profile", profile?.TrimStart('-') ?? "headless");
                else writer.WriteNull("profile");

                writer.WriteBoolean("native", native);
                writer.WriteBoolean("managed", true);
            }),
            data => Console.WriteLine(
                data.GetProperty("native").GetBoolean()
                    ? $"built: {data.GetProperty("profile").GetString()} bridge, then the managed side"
                    : "built: the managed side"));
    }

    /// <summary>
    /// Runs the tests, and says which kind of failure it was.
    /// </summary>
    /// <remarks>
    /// A run that finished and reported failures exits 8. A run that never reached a verdict (a
    /// compile error, a missing bridge, a crash) keeps 6. Only the second is ever worth retrying,
    /// and a caller can only tell them apart if they are different numbers.
    /// </remarks>
    public static int Test(Options options, string[] arguments)
    {
        if (Repo.Root is null)
        {
            return Output.Refuse(
                options, "test", "NO_CHECKOUT", "This is not inside a BevyCSharp checkout.");
        }

        var line = new List<string>
        {
            "test", "BevyCSharp.Tests/BevyCSharp.Tests.csproj", "--nologo",
        };

        for (var index = 0; index < arguments.Length; index++)
        {
            if (arguments[index] is "--filter" && index + 1 < arguments.Length)
            {
                line.Add("--filter");
                line.Add(arguments[++index]);
            }
        }

        var ran = Shell.Run("dotnet", line, Repo.Root, echo: !options.Json && !options.Quiet);
        var counted = Counts().Match(ran.Output);

        // A crashed host still prints a tally, for the tests that ran before it went down. Trusting
        // it would report a green run that never covered most of the suite, which is the one
        // failure mode a test command must not have.
        if (Aborted().IsMatch(ran.Output))
        {
            return Output.Print(options, Failure(
                "test",
                "TEST_RUN_ERROR",
                "The test host crashed, so the run never covered the whole suite. Any tally it "
                + "printed is of the tests that ran before it went down. Note that a windowed "
                + "session serving at the same time can do this: 'bcs stop' first.",
                ran));
        }

        if (!counted.Success)
        {
            return Output.Print(options, Failure(
                "test",
                ran.Ok ? "NO_VERDICT" : "TEST_RUN_ERROR",
                "The test run did not report a result. That is an infrastructure failure rather "
                + "than a failing test, such as a compile error or a bridge that will not load.",
                ran));
        }

        var failed = int.Parse(counted.Groups["failed"].Value);
        var passed = int.Parse(counted.Groups["passed"].Value);
        var skipped = int.Parse(counted.Groups["skipped"].Value);

        void Tally(System.Text.Json.Utf8JsonWriter writer)
        {
            writer.WriteNumber("passed", passed);
            writer.WriteNumber("failed", failed);
            writer.WriteNumber("skipped", skipped);
        }

        if (failed == 0 && ran.Ok)
        {
            return Output.Print(
                options,
                CliJson.Ok("test", Tally),
                data => Console.WriteLine(
                    $"{data.GetProperty("passed").GetInt32()} passed, "
                    + $"{data.GetProperty("skipped").GetInt32()} skipped"));
        }

        return Output.Print(
            options,
            CliJson.Envelope(
                "test",
                success: false,
                data: writer =>
                {
                    Tally(writer);
                    writer.WriteString("tail", ran.Tail(40));
                },
                errors:
                [
                    new CliError(
                        "TESTS_FAILED",
                        $"{failed} of {failed + passed} tests failed. This is a real failure, "
                        + "not an infrastructure one. Do not retry it."),
                ]),
            data => Console.WriteLine(data.GetProperty("tail").GetString()));
    }

    /// <summary>Runs an app once, cold, and passes its output straight through.</summary>
    /// <remarks>
    /// For when a fresh world really is what is wanted: a headless run of a fixed number of frames,
    /// which is what a test or a capture of a first frame needs. Driving a running app is cheaper
    /// for everything else.
    /// </remarks>
    public static int Run(Options options, string[] arguments)
    {
        if (Repo.Root is null)
        {
            return Output.Refuse(
                options, "run", "NO_CHECKOUT", "This is not inside a BevyCSharp checkout.");
        }

        var project = arguments.Contains("--editor") ? "BevyCSharp.Editor" : "BevyCSharp.Sample";
        var rest = arguments.Where(argument => argument is not ("--editor" or "--sample")).ToList();

        if (rest.Count == 0) rest = ["--headless", "--frames", "120"];

        var ran = Shell.Run(
            "dotnet",
            ["run", "--project", project, "--nologo", "--", .. rest],
            Repo.Root,
            echo: !options.Json);

        if (ran.Ok)
        {
            return Output.Print(
                options,
                CliJson.Ok("run", writer =>
                {
                    writer.WriteString("project", project);
                    writer.WriteString("output", ran.Tail(40));
                }),
                _ => { });
        }

        return Output.Print(options, Failure(
            "run", "RUN_FAILED", $"{project} exited with {ran.Code}.", ran));
    }

    /// <summary>
    /// Says why nothing is working.
    /// </summary>
    /// <remarks>
    /// The three things that go wrong here are a bridge that was never built, a bridge built
    /// without the renderer, and a session file left behind by an app that died. All three look
    /// like a broken tool from the outside, so all three are checked in one place.
    /// </remarks>
    public static int Doctor(Options options)
    {
        var swept = CliSessionFile.Prune();
        var sessions = Sessions.Live();

        string bridge;
        var renderer = false;
        var editor = false;
        var abi = 0;

        try
        {
            renderer = App.HasRenderer;
            editor = App.HasEditor;
            abi = App.AbiVersion;
            bridge = "loaded";
        }
        catch (Exception error)
        {
            bridge = error.Message.Replace('\n', ' ');
        }

        var ok = bridge is "loaded";

        var envelope = CliJson.Envelope(
            "doctor",
            ok,
            writer =>
            {
                writer.WriteString("root", Repo.Root ?? "(not in a checkout)");
                writer.WriteString("bridge", bridge);
                writer.WriteNumber("abi", abi);
                writer.WriteBoolean("renderer", renderer);
                writer.WriteBoolean("editor", editor);
                writer.WriteBoolean("dotnet", Shell.Exists("dotnet"));
                writer.WriteBoolean("cargo", Shell.Exists("cargo"));
                writer.WriteNumber("sessions", sessions.Count);
                writer.WriteNumber("swept", swept);
                writer.WriteString("sessionDirectory", CliSessionFile.Directory);
            },
            errors: ok
                ? null
                :
                [
                    new CliError(
                        "NO_BRIDGE",
                        "The native bridge did not load, so no app can start. Build it with "
                        + "'bcs build --render' (or build/build-native.sh --render), then run "
                        + "'dotnet build' so the library lands beside each project."),
                ]);

        return Output.Print(options, envelope, data =>
        {
            Console.WriteLine($"checkout    {data.GetProperty("root").GetString()}");
            Console.WriteLine(
                $"bridge      {data.GetProperty("bridge").GetString()} "
                + $"(abi {data.GetProperty("abi").GetInt32()})");
            Console.WriteLine(
                $"renderer    {data.GetProperty("renderer").GetBoolean()}   "
                + $"interface {data.GetProperty("editor").GetBoolean()}");
            Console.WriteLine(
                $"sessions    {data.GetProperty("sessions").GetInt32()} live, "
                + $"{data.GetProperty("swept").GetInt32()} stale files swept");
        });
    }

    /// <summary>The flag a build profile becomes, in the spelling that script understands.</summary>
    private static string[] Profile(string? profile, bool windows) => profile switch
    {
        "--render" => windows ? ["-Render"] : ["--render"],
        "--editor" => windows ? ["-Editor"] : ["--editor"],

        // Neither script takes a flag for the headless profile, which is what it builds by default.
        _ => [],
    };

    /// <summary>A failure that carries what the child process said.</summary>
    private static string Failure(string command, string code, string message, Ran ran) =>
        CliJson.Envelope(
            command,
            success: false,
            data: writer =>
            {
                writer.WriteNumber("exitCode", ran.Code);
                writer.WriteString("tail", ran.Tail(30));
            },
            errors: [new CliError(code, message)]);

    /// <summary>How a run says it never finished.</summary>
    [GeneratedRegex(@"Test Run Aborted|Test host process crashed|The active test run was aborted")]
    private static partial Regex Aborted();

    /// <summary>What a finished test run says about itself.</summary>
    [GeneratedRegex(
        @"Failed:\s*(?<failed>\d+),\s*Passed:\s*(?<passed>\d+),\s*Skipped:\s*(?<skipped>\d+)")]
    private static partial Regex Counts();
}
