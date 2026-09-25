using Bevy;
using BevyCSharp.Editor;

// Opens the editor: a scene filling the window, with panels floating over it.
//
// The panels need a bridge built with the editor profile, which carries the HTML and CSS
// surface on top of the renderer:
//     build/build-native.sh --editor

// How large to draw, which matters more without a window than with one: a window can be resized
// by hand and an image cannot, so a panel taller than the picture has no way to be seen.
var size = args.SkipWhile(argument => argument != "--size").Skip(1).FirstOrDefault();
var across = 1600u;
var down = 900u;

if (size is { Length: > 0 } && size.Split('x') is [var wide, var tall]
    && uint.TryParse(wide, out var parsedWidth)
    && uint.TryParse(tall, out var parsedHeight))
{
    across = Math.Clamp(parsedWidth, 320u, 7680u);
    down = Math.Clamp(parsedHeight, 240u, 4320u);
}

// A window by default, and an image when asked for one. The editor is an app like any other, so
// what lets a game be drawn on a machine with no display lets the editor be drawn there too, which
// is what a build server checking the interface against a capture needs.
var config = args.Contains("--offscreen")
    ? Config.OffscreenFor(across, down)
    : Config.Windowed("BevyCSharp Editor", across, down);

// Bevy looks beside the running executable otherwise, which for a .NET app is whichever host
// launched it rather than the directory the assets were copied to.
config.AssetRoot = Path.Combine(AppContext.BaseDirectory, "assets");

// The panels are HTML and CSS, and the point of describing them in files is being able to change
// them without a rebuild.
config.Gui = true;
config.WatchAssets = true;

// The same goes for shaders, and a shader edited into a shape the pipeline rejects is something to
// read about in the console and fix, rather than a reason for the editor to close.
Shaders.KeepRenderingAfterErrors = true;

// Answers `bcs` while it runs: what is in the world, what a click does, what the window looks
// like, without stopping to ask. BCS_SERVE does the same for a run that cannot be given arguments.
config.Serve = args.Contains("--serve");

if (!App.HasRenderer)
{
    Console.Error.WriteLine("This native bridge has no renderer, so there is nothing to draw with.");
    Console.Error.WriteLine("Rebuild it with build/build-native.sh --editor.");
    return 1;
}

if (!App.HasEditor)
{
    Console.Error.WriteLine(
        "This native bridge has the renderer but not the HTML and CSS surface, so the panels "
        + "cannot open.");
    Console.Error.WriteLine("Rebuild it with build/build-native.sh --editor.");
    return 1;
}

Console.WriteLine($"BevyCSharp editor: {config}");
Console.WriteLine($"adapter: {App.DescribeAdapter() ?? "not reported yet"}");

return BevyApp.Run(
    app =>
    {
        // Behavior scripts are compiled while the app runs, so their systems arrive after the
        // schedule is Bevy's. The dispatchers have to be in place before that.
        app.EnableDynamicSystems();
        EditorBoot.Host = app;
    },
    config);
