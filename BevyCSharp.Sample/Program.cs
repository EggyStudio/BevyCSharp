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
//   --offscreen                draw with no window, into an image a capture reads back
//   --backend                  vulkan|dx12|metal|gl|auto
//   --frames N                 how many ticks to run, for a run with no window
//   --serve                    answer `bcs` while it runs
//   --verbose                  print a progress line every 20 frames
var offscreen = args.Contains("--offscreen");
var windowed = !offscreen && (RunInWindow || args.Contains("--window"));
if (args.Contains("--headless")) windowed = false;

Trace.Verbose = args.Contains("--verbose");

// Three ways to run the same behaviors: onto a screen, into an image, or with no renderer at all. A
// machine with no display can still see the picture from the middle one.
var config = windowed
    ? Config.Windowed("BevyCSharp Sample", 1280, 720, ParseBackend(args, Backend))
    : offscreen
        ? Config.OffscreenFor(1280, 720, ParseFrames(args))
        : new Config { Headless = true, HeadlessFrames = ParseFrames(args), HeadlessFps = 60 };

// Bevy looks beside the running executable otherwise, which for a .NET app is whichever host
// launched it rather than the directory the assets were copied to.
config.AssetRoot = Path.Combine(AppContext.BaseDirectory, "assets");

// Asks for the document and stylesheet interface, which the sample's own panel is built from. A
// bridge without it compiled in ignores this and the panel is not opened. An offscreen run leaves
// it off, because the panel is driven by a pointer and a keyboard that a run with no window never
// receives.
config.Gui = windowed;

// Answers `bcs` while it runs. A headless run that serves is worth pairing with --frames 0, which
// runs until something asks it to stop rather than counting down to an exit.
config.Serve = args.Contains("--serve");

if ((windowed || offscreen) && !App.HasRenderer)
{
    Console.Error.WriteLine(
        "This native bridge was built without Bevy's renderer, so it can neither open a window "
        + "nor draw into an image.");
    Console.Error.WriteLine("Rebuild it with build/build-native.sh --render.");
    Console.Error.WriteLine("Or run without one: dotnet run -- --headless --frames 120");
    return 1;
}

Console.WriteLine($"BevyCSharp sample: {config}");
Console.WriteLine($"renderer compiled in: {App.HasRenderer}");
Console.WriteLine(windowed ? "close the window to exit" : string.Empty);

return BevyApp.Run(config);

// Reads --frames N, defaulting to a short run so a plain `dotnet run` still does something. Zero
// runs until something asks the app to stop, as a session driven from `bcs` needs.
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
