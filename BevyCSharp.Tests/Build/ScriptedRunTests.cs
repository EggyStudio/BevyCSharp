using System.Diagnostics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// build/drive-feature-test.sh run in a checkout of its own beside stand-ins for bcs and the feature
/// test, the one for bcs keeping the settings in a file as the game keeps them and saying the player
/// stands where it was put, so the scripted run changes the settings and then fails at the hub's walk.
/// </summary>
/// <remarks>
/// Run in bash, which the workflow runs the script in on Linux, and left out on Windows, where the
/// first bash on the path can be one that reads none of the checkout's paths.
/// </remarks>
public sealed class ScriptedRunTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-scripted-run-");

    public void Dispose() => _folder.Dispose();

    [SkippableFact]
    public void ARunThatFailsPutsTheTestersSettingsBack()
    {
        const string Kept = """{"DaySpeed":1,"Hour":9,"WeatherKind":"Clear","Procedural":true,"WeatherQuality":"High","GlobalIllumination":"Medium"}""";
        var settings = _folder.File("feature-test.json");
        File.WriteAllText(settings, Kept);

        var (exit, said) = Run(settings);

        Assert.True(exit != 0, said);
        Assert.Contains("the player did not walk north", said);
        Assert.Equal(Kept, File.ReadAllText(settings));
    }

    [SkippableFact]
    public void ARunThatFindsNoSettingsLeavesNone()
    {
        var settings = _folder.File("feature-test.json");

        var (exit, said) = Run(settings);

        Assert.True(exit != 0, said);
        Assert.False(File.Exists(settings), "the run left a settings file where it found none");
    }

    // Runs the script copied into the folder beside the stand-ins, the weather's quality named Off as
    // the workflow names it, and answers its exit code and what it wrote.
    private (int Exit, string Said) Run(string settings)
    {
        Skip.If(OperatingSystem.IsWindows(), "runs the script in bash, as the workflow runs it on Linux");
        var python = Needs.Python();
        var root = _folder.File("checkout");
        Directory.CreateDirectory(Path.Combine(root, "build"));
        File.Copy(Path.Combine(CheatsheetTests.RepositoryRoot(), "build", "drive-feature-test.sh"), Path.Combine(root, "build", "drive-feature-test.sh"), overwrite: true);
        Executable(Path.Combine(root, "bcs"), $"#!/usr/bin/env {python}\n{StandIn}");
        Directory.CreateDirectory(Path.Combine(root, "program"));
        Executable(Path.Combine(root, "program", "BevyCSharp.FeatureTest"), "#!/bin/sh\nexit 0\n");
        var shots = Directory.CreateDirectory(_folder.File("shots")).FullName;

        var start = new ProcessStartInfo("bash") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("build/drive-feature-test.sh");
        start.ArgumentList.Add("program");
        start.Environment["SHOTS"] = shots;
        start.Environment["WEATHER_QUALITY"] = "Off";
        start.Environment["STANDIN_SETTINGS"] = settings;

        using var script = Process.Start(start)!;
        var output = script.StandardOutput.ReadToEndAsync();
        var errors = script.StandardError.ReadToEndAsync();
        Assert.True(script.WaitForExit(120_000), "the scripted run against stand-ins ends within seconds");
        return (script.ExitCode, output.Result + errors.Result);
    }

    private static void Executable(string path, string text)
    {
        File.WriteAllText(path, text);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    // bcs as the script asks it, the game serving at once, a setting read from the file and a change
    // written to it as the game writes each, the player standing where it was put, and every other
    // command done.
    private const string StandIn = """
        import json, os, sys

        args = sys.argv[1:]
        verb = args[0] if args else ""
        if verb == "status":
            print("ready BevyCSharp.FeatureTest")
        elif verb == "shot":
            open(args[1], "wb").close()
            print("wrote " + args[1])
        elif verb == "command":
            words = args[1:]
            if words[:1] == ["--timeout"]:
                words = words[2:]
            words = " ".join(word for word in words if word != "--").split()
            name, rest = words[0], words[1:]
            path = os.environ["STANDIN_SETTINGS"]
            if name == "setting.file":
                print(path)
            elif name == "setting":
                settings = json.load(open(path)) if os.path.exists(path) else {}
                if len(rest) > 1:
                    text = " ".join(rest[1:])
                    try:
                        settings[rest[0]] = json.loads(text)
                    except ValueError:
                        settings[rest[0]] = text
                    with open(path, "w") as file:
                        json.dump(settings, file)
                print(rest[0] + " = " + json.dumps(settings.get(rest[0], "")))
            elif name == "player":
                print("at 0 0 0, walking")
            elif name == "frame.profile":
                print("frames=30 frame_ms=16.7")
            else:
                print("done")
        """;
}
