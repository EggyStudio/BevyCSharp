using Bevy;
using BevyCSharp.Player;

// Plays a scene file in a window of its own. The editor's Play tab runs it to play the scene being
// edited rather than the project's own Main.
//
//   --scene <file>           the scene to play, required
//   --assets <dir>           the asset root it was made against, scripts and all; beside the
//                            executable when not given
//   --view x,y,z,qx,qy,qz,qw where a camera goes when the scene has none
//   --size WxH               how large the window or the image is
//   --offscreen              draw into an image rather than a window
//   --frames N               how many frames an offscreen run lasts, zero for until stopped
//   --serve                  answer `bcs` while it runs
var scene = Value("--scene");
if (scene is null)
{
    Console.Error.WriteLine("Name a scene to play: --scene <file>");
    return 2;
}

if (!File.Exists(scene))
{
    Console.Error.WriteLine($"There is no scene at {scene}.");
    return 2;
}

var (width, height) = Size(Value("--size"));
var offscreen = args.Contains("--offscreen");

var config = offscreen
    ? Config.OffscreenFor(width, height, uint.TryParse(Value("--frames"), out var frames) ? frames : 0u)
    : Config.Windowed(Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(scene)), width, height);

config.AssetRoot = Path.GetFullPath(Value("--assets") ?? Path.Combine(AppContext.BaseDirectory, "assets"));
config.GameName = "BevyCSharp Player";
config.Serve = args.Contains("--serve");

if (!App.HasRenderer)
{
    Console.Error.WriteLine("This native bridge has no renderer, so there is nothing to play the scene on.");
    return 1;
}

PlayerBoot.Scene = Path.GetFullPath(scene);
PlayerBoot.View = View(Value("--view"));

return BevyApp.Run(
    app =>
    {
        // Scripts are compiled while the app runs, so their systems arrive after the schedule is
        // Bevy's, and the dispatchers have to be there first.
        app.EnableDynamicSystems();
        PlayerBoot.Host = app;
    },
    config);

// What follows an option, or nothing when it is not given.
string? Value(string option)
{
    var index = Array.IndexOf(args, option);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

// A window's size as WxH, or the editor's own default.
static (uint Width, uint Height) Size(string? text) =>
    text?.Split('x') is [var wide, var tall] && uint.TryParse(wide, out var w) && uint.TryParse(tall, out var h)
        ? (Math.Clamp(w, 320u, 7680u), Math.Clamp(h, 240u, 4320u))
        : (1280u, 720u);

// A camera's place as seven numbers, a position and a rotation, or nothing when they do not read.
static Transform? View(string? text)
{
    var numbers = text?.Split(',').Select(part => float.TryParse(part, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : float.NaN).ToArray();
    if (numbers is not { Length: 7 } || numbers.Any(float.IsNaN)) return null;

    return new Transform(
        new Vec3(numbers[0], numbers[1], numbers[2]),
        new Quat(numbers[3], numbers[4], numbers[5], numbers[6]),
        Vec3.One);
}
