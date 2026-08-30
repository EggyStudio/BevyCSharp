using Bevy;
using BevyCSharp.Sample.Behaviors;

// The whole of the setup. Every [Behavior] struct in this assembly has already been turned
// into Bevy systems by the source generator and is discovered here automatically - there is no
// registration list to keep up to date.
//
// Run with --frames N to run headless for a fixed number of ticks, which is what CI does.

uint headlessFrames = ParseFrames(args);
Trace.Verbose = args.Contains("--verbose");

var config = headlessFrames > 0
    ? new Config { Headless = true, HeadlessFrames = headlessFrames, HeadlessFps = 60 }
    : new Config { Title = "BevyCSharp Sample", Width = 1280, Height = 720 };

if (!config.Headless && !App.HasRenderer)
{
    Console.WriteLine(
        "This native bridge was built without Bevy's renderer, so there is no window to open.");
    Console.WriteLine(
        "Rebuild it with 'build/build-native.sh --render', or run headless: --frames 120");
    return 1;
}

Console.WriteLine($"BevyCSharp sample - {config}");
Console.WriteLine($"renderer compiled in: {App.HasRenderer}");
Console.WriteLine();

return BevyApp.Run(config);

// Reads --frames N from the command line, defaulting to a short headless run so that a plain
// `dotnet run` does something useful even on a machine with no display.
static uint ParseFrames(string[] arguments)
{
    var index = Array.IndexOf(arguments, "--frames");
    if (index >= 0 && index + 1 < arguments.Length && uint.TryParse(arguments[index + 1], out var value))
        return value;

    return arguments.Contains("--window") ? 0u : 120u;
}
