using Bevy;
using BevyCSharp.Examples;

// Bevy's examples written in C#, each under Bevy's own name and in a folder named as Bevy's is, so
// the two can be read side by side. One is picked by name:
//
//   dotnet run --project BevyCSharp.Examples -- 3d_scene
//   ./bcs open --example 3d_scene --offscreen
//
//   --offscreen    draws into an image with no window, as a capture does
//   --frames N     how many frames an offscreen run draws, where zero runs until it is stopped
//   --size WxH     the window's or the image's size, 1280x720 unless given
//   --serve        answers bcs while it runs
//   --list         prints every example's name
//   --printing     prints the names of those that print rather than draw, which run headless
//   --window       opens an empty window for one that prints, so one that reads keys has them
//
// .github/EXAMPLES.md has a row for every one of Bevy's examples, these among them.
if (args.Length == 0 || args[0] is "--list" or "--printing")
{
    foreach (var known in Catalog.All.Where(known => args.Length == 0 || args[0] == "--list" || known.Prints > 0))
        Console.WriteLine(known.Name);
    return args.Length == 0 ? 2 : 0;
}

if (!Catalog.TryFind(args[0], out var example))
{
    Console.Error.WriteLine($"There is no example called '{args[0]}'. --list prints them.");
    return 2;
}

var offscreen = args.Contains("--offscreen");
var (width, height) = Size(args);
Scene.Size = (width, height);
var config = example.Prints > 0 && !args.Contains("--window")
    ? new Config { Headless = true, HeadlessFrames = Frames(args) is > 0 and var frames ? frames : example.Prints, HeadlessFps = 60 }
    : offscreen
        ? Config.OffscreenFor(width, height, Frames(args))
        : Config.Windowed($"{example.Name} (BevyCSharp)", width, height);

// Beside the program rather than beside whichever host launched it, which for a .NET app is not
// the directory the assets were copied to.
config.AssetRoot = Path.Combine(AppContext.BaseDirectory, "assets");
config.Serve |= args.Contains("--serve");
example.Configure?.Invoke(config);

if ((example.Prints == 0 || args.Contains("--window")) && !App.HasRenderer)
{
    Console.Error.WriteLine("This native bridge was built without Bevy's renderer, so an example has nothing to draw with.");
    Console.Error.WriteLine("Rebuild it with build/build-native.sh --render.");
    return 1;
}

var code = BevyApp.Run(example.Build, config);
example.Returned?.Invoke();
return code;

// Reads --frames N, which is zero, running until stopped, when it is not given.
static uint Frames(string[] arguments)
{
    var index = Array.IndexOf(arguments, "--frames");
    return index >= 0 && index + 1 < arguments.Length && uint.TryParse(arguments[index + 1], out var frames) ? frames : 0u;
}

// Reads --size WxH, which is 1280 by 720 when it is not given or cannot be read.
static (uint Width, uint Height) Size(string[] arguments)
{
    var index = Array.IndexOf(arguments, "--size");
    if (index < 0 || index + 1 >= arguments.Length) return (1280u, 720u);

    var parts = arguments[index + 1].Split('x');
    return parts.Length == 2 && uint.TryParse(parts[0], out var width) && uint.TryParse(parts[1], out var height) && width > 0 && height > 0
        ? (width, height)
        : (1280u, 720u);
}
