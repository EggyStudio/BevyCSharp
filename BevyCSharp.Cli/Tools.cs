using System.Text.Json;
using Bevy;

namespace BevyCSharp.Cli;

/// <summary>The cold verbs: building, testing, a one-shot run, and why nothing works.</summary>
internal static class Tools
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

        var meshlet = arguments.Contains("--meshlet");
        var solari = arguments.Contains("--solari");

        var managed = !arguments.Contains("--no-managed");
        var native = !arguments.Contains("--no-native");
        var echo = !options.Json && !options.Quiet;

        Ran bridge = new(0, string.Empty);

        // Reported but not fatal, because the bridge and the managed side build without it and
        // only a shader needs it.
        FetchSlang(echo);

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
                        .. (meshlet ? ["-Meshlet"] : Array.Empty<string>()),
                        .. (solari ? ["-Solari"] : Array.Empty<string>()),
                    ],
                    Repo.Root,
                    echo)
                : Shell.Run(
                    Path.Combine(Repo.Root, "build", "build-native.sh"),
                    [
                        .. Profile(profile, windows: false),
                        .. (meshlet ? ["--meshlet"] : Array.Empty<string>()),
                        .. (solari ? ["--solari"] : Array.Empty<string>()),
                    ],
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
    /// Fetches the Slang compiler into <c>build/tools</c> when it is not there yet.
    /// </summary>
    /// <remarks>
    /// Every shader a game draws with is compiled by it, so a checkout has one before anything
    /// builds or tests shaders. The bridge finds it there on its own, walking up from the running
    /// program, so nothing is put on the PATH.
    /// </remarks>
    private static void FetchSlang(bool echo)
    {
        var fetched = OperatingSystem.IsWindows()
            ? Shell.Run("pwsh", ["-File", Path.Combine(Repo.Root!, "build", "fetch-slang.ps1")], Repo.Root!, echo)
            : Shell.Run(Path.Combine(Repo.Root!, "build", "fetch-slang.sh"), [], Repo.Root!, echo);

        if (!fetched.Ok && echo)
        {
            Console.Error.WriteLine(
                "slangc could not be fetched, so shaders will not compile in this checkout. Run "
                + "build/fetch-slang.sh by hand, or set BCS_SLANGC.");
        }
    }

    /// <summary>
    /// Runs the tests through <c>build/test.py</c>, and says which kind of failure it was.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Through the script CI runs, so a run here is counted as CI's is. A host that is lost, to a
    /// crash, a hang or the memory cap MemoryGuard holds it to, still prints a tally of the tests
    /// that ran before it went, which <c>dotnet test</c> alone shows as a pass. The script says
    /// such a host was lost, runs the suite again in parts, and counts every listed test without a
    /// result.
    /// </para>
    /// <para>
    /// A run that finished and reported failures exits 8. A run that never reached a verdict (a
    /// compile error, a missing bridge, a host lost before every test had a result) keeps 6. Only
    /// the second is ever worth retrying, and a caller can only tell them apart if they are
    /// different numbers.
    /// </para>
    /// </remarks>
    public static int Test(Options options, string[] arguments)
    {
        if (Repo.Root is null)
        {
            return Output.Refuse(
                options, "test", "NO_CHECKOUT", "This is not inside a BevyCSharp checkout.");
        }

        var line = new List<string> { Path.Combine("build", "test.py"), "suite" };

        for (var index = 0; index < arguments.Length; index++)
        {
            if (arguments[index] is "--filter" && index + 1 < arguments.Length)
            {
                line.Add("--filter");
                line.Add(arguments[++index]);
            }
        }

        // The picture tests compile Slang, and without a compiler they would fail as though the
        // shaders were wrong.
        FetchSlang(!options.Json && !options.Quiet);

        // The page goes first, so a script that never started leaves none behind it, rather than an
        // earlier run's to be read as this one's. Asked after first, since Windows refuses to
        // delete a file in a folder that is not there, as a checkout's first run has none.
        var page = Path.Combine(Repo.Root, "BevyCSharp.Tests", "TestResults", "digest.json");
        if (File.Exists(page)) File.Delete(page);

        var python = OperatingSystem.IsWindows() ? "python" : "python3";
        var ran = Shell.Run(python, line, Repo.Root, echo: !options.Json && !options.Quiet);

        JsonElement digest;

        try
        {
            using var read = JsonDocument.Parse(File.ReadAllText(page));
            digest = read.RootElement.Clone();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
                                          or JsonException)
        {
            return Output.Print(options, Failure(
                "test",
                "TEST_RUN_ERROR",
                $"build/test.py wrote no page of the run, so it never reached a verdict. Is {python} "
                + "on the PATH?",
                ran));
        }

        var failed = digest.GetProperty("failed").GetInt32();
        var passed = digest.GetProperty("passed").GetInt32();
        var skipped = digest.GetProperty("skipped").GetInt32();
        var unrun = digest.GetProperty("no_result").GetInt32();
        var lost = digest.GetProperty("processes").EnumerateArray()
            .Where(process => process.GetProperty("lost").ValueKind == JsonValueKind.String)
            .Select(process => process.GetProperty("summary").GetString())
            .ToList();

        void Tally(Utf8JsonWriter writer)
        {
            writer.WriteNumber("passed", passed);
            writer.WriteNumber("failed", failed);
            writer.WriteNumber("skipped", skipped);
            writer.WriteNumber("withoutResult", unrun);
        }

        // A host that went before every test had a result covered less than the suite, whatever
        // its tally says, so the run has no verdict even where every test that ran passed.
        if (lost.Count > 0 || unrun > 0)
        {
            return Output.Print(options, Failure(
                "test",
                "TEST_RUN_ERROR",
                (lost.Count > 0 ? $"Lost: {string.Join("; ", lost)}. " : "")
                + $"{unrun} listed tests have no result, so the run never covered the whole suite. "
                + "The page is BevyCSharp.Tests/TestResults/digest.md. A windowed session serving "
                + "at the same time can do this: 'bcs stop' first.",
                ran));
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
    /// For when a fresh world really is wanted, a headless run of a fixed number of frames, as a
    /// test or a capture of a first frame needs. Driving a running app is cheaper for everything
    /// else.
    /// </remarks>
    public static int Run(Options options, string[] arguments)
    {
        if (Repo.Root is null)
        {
            return Output.Refuse(
                options, "run", "NO_CHECKOUT", "This is not inside a BevyCSharp checkout.");
        }

        var project = arguments.Contains("--editor") ? "BevyCSharp.Editor" : "BevyCSharp.FeatureTest";
        var rest = arguments.Where(argument => argument is not ("--editor" or "--feature-test" or "--sample")).ToList();

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

        // Neither script takes a flag for the headless profile, which it builds by default.
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
}
