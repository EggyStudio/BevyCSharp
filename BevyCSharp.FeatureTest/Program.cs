using Bevy;
using BevyCSharp.FeatureTest;
using BevyCSharp.FeatureTest.Behaviors;

// The feature test, every feature of the engine on one map, a hub with signposts to its zones, a
// free camera, an admin panel (F1), a debug overlay (F3) and a console (the key under Escape),
// for a tester to try a build with and for the workflow to run. Set this to false, or pass
// --headless, to run the same behavior scripts with no window and no GPU.
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

// The memory cap a tool starting it would give, eight gigabytes or a quarter of the machine's where
// that is less, unless BCS_MEMORY_CAP_GB names another, since a scene or a technique tried here can
// take the machine's memory faster than anyone watching notices, and a machine that ran out ended
// the terminal it was started from. Past it the run stops, saying so (MemoryGuard).
MemoryGuard.DefaultTheEnvironment();

// The panel's settings, read before the window opens, since vsync is the window's from its start.
var saved = Settings.Current;

// Three ways to run the same behaviors: onto a screen, into an image, or with no renderer at all. A
// machine with no display can still see the picture from the middle one.
var config = windowed
    ? Config.Windowed(Settings.GameName, 1280, 720, ParseBackend(args, Backend))
    : offscreen
        ? Config.OffscreenFor(1280, 720, ParseFrames(args))
        : new Config { Headless = true, HeadlessFrames = ParseFrames(args), HeadlessFps = 60 };

// Bevy looks beside the running executable otherwise, which for a .NET app is whichever host
// launched it rather than the directory the assets were copied to.
config.AssetRoot = Path.Combine(AppContext.BaseDirectory, "assets");

// The player's folder and the window under the program's own name, and the window waiting for the
// display or not as the panel last left it.
config.GameName = Settings.GameName;
config.Vsync = saved.Vsync;

// Bevy's ray-traced lighting where the panel asked for it, which a bridge built without Solari or
// a GPU that traces no rays leaves off.
config.RayTracedLighting = saved.RayTraced && (windowed || offscreen);

// Bevy's meshlets where the panel asked for them, room for four million clusters, which Sponza's
// heaviest meshes need a small part of; a bridge built without them or a GPU without 64-bit
// texture atomics leaves them off.
config.MeshletClusters = saved.Meshlets && (windowed || offscreen) ? 1u << 22 : 0;

// The GPU's time for each pass, with the meshlets, so Sponza's page compares the GPU's work with
// meshlets and without rather than frame times a display or the offscreen pace holds alike.
config.GpuTimings = config.MeshletClusters > 0;

// Asks for the interface the panel, the overlay and the console are drawn with, offscreen as well,
// where bcs drives them with the keys it sends and a capture shows them. A bridge without it
// compiled in ignores this and they are not drawn.
config.Gui = windowed || offscreen;

// The plugins that draw a mesh as its edges, which the panel's debug page turns on.
config.Wireframes = windowed || offscreen;

// Answers `bcs` while it runs. A headless run that serves is worth pairing with --frames 0, which
// runs until something asks it to stop rather than counting down to an exit.
config.Serve = args.Contains("--serve");

// The desktop's own title bar where it draws one only for X11 windows, which is GNOME on Wayland,
// so the program's window wears the frame every other window on the desktop does.
config.DesktopTitleBar = true;

if ((windowed || offscreen) && !App.HasRenderer)
{
    Console.Error.WriteLine(
        "This native bridge was built without Bevy's renderer, so it can neither open a window "
        + "nor draw into an image.");
    Console.Error.WriteLine("Rebuild it with build/build-native.sh --render.");
    Console.Error.WriteLine("Or run without one: dotnet run -- --headless --frames 120");
    return 1;
}

Console.WriteLine($"BevyCSharp feature test: {config}");
Console.WriteLine($"renderer compiled in: {App.HasRenderer}");
Console.WriteLine(windowed ? "close the window to exit" : string.Empty);

// Rigid bodies, from the physics package, for the crates F7 drops (see Behaviors/Crates.cs).
return BevyApp.Run(app => app.AddPlugin(new Bevy.Physics.PhysicsPlugin()), config);

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
