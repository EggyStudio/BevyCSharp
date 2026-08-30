using BevyCSharp.Sample.Behaviors;
using Bevy;

// Opens a window showing a rotating cube. Set this to false, or pass --headless, to run the
// same behavior scripts with no window and no GPU.
//
// A window needs a native bridge built with the render feature:
//     build/build-native.sh --render
//
// A headless bridge has no renderer compiled in, and the run below says so and stops rather
// than opening an empty window.

const bool RunInWindow = true;

// Which graphics API to ask for. Automatic already prefers Vulkan on Linux and Windows; naming
// it pins the choice, so startup fails loudly instead of quietly falling back to something else.
const GraphicsBackend Backend = GraphicsBackend.Vulkan;

// Command line wins over the constants above, so the same build can do either.
//   --window / --headless      choose the mode
//   --backend                  vulkan|dx12|metal|gl|auto
//   --frames N                 headless only: run N ticks and exit
//   --verbose                  print a progress line every 20 frames
var windowed = RunInWindow || args.Contains("--window");
if (args.Contains("--headless")) windowed = false;

Trace.Verbose = args.Contains("--verbose");

var config = windowed
    ? Config.Windowed("BevyCSharp Sample", 1280, 720, ParseBackend(args, Backend))
    : new Config { Headless = true, HeadlessFrames = ParseFrames(args), HeadlessFps = 60 };

if (windowed && !App.HasRenderer)
{
    Console.Error.WriteLine(
        "This native bridge was built without Bevy's renderer, so there is no window to open.");
    Console.Error.WriteLine("  rebuild it : build/build-native.sh --render");
    Console.Error.WriteLine("  or run     : dotnet run -- --headless --frames 120");
    return 1;
}

Console.WriteLine($"BevyCSharp sample: {config}");
Console.WriteLine($"renderer compiled in: {App.HasRenderer}");
Console.WriteLine(windowed ? "close the window to exit" : "");

return BevyApp.Run(config);

// Reads --frames N, defaulting to a short run so a plain `dotnet run` still does something.
static uint ParseFrames(string[] arguments)
{
    var index = Array.IndexOf(arguments, "--frames");
    return index >= 0
           && index + 1 < arguments.Length
           && uint.TryParse(arguments[index + 1], out var value)
        ? value
        : 120u;
}

// Reads --backend NAME, falling back to the compiled-in default.
static GraphicsBackend ParseBackend(string[] arguments, GraphicsBackend fallback)
{
    var index = Array.IndexOf(arguments, "--backend");
    if (index < 0 || index + 1 >= arguments.Length) return fallback;

    return arguments[index + 1].ToLowerInvariant() switch
    {
        "vulkan" or "vk" => GraphicsBackend.Vulkan,
        "dx12" or "d3d12" => GraphicsBackend.Direct3D12,
        "metal" => GraphicsBackend.Metal,
        "gl" or "opengl" => GraphicsBackend.OpenGL,
        "auto" or "automatic" => GraphicsBackend.Automatic,
        _ => fallback,
    };
}
