using Bevy;
using BevyCSharp.Editor;
using BevyCSharp.Editor.Framework;

// Opens the editor, a scene filling the window with panels floating over it, on a project of the
// game's own when given --project <folder>, editing the assets folder in it.
//
// The panels need a bridge built with the editor profile, which carries the HTML and CSS
// surface on top of the renderer:
//     build/build-native.sh --editor

// How large to draw, which matters more without a window than with one, because a window can be
// resized by hand and an image cannot, so a panel taller than the picture has no way to be seen.
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
// what lets a game be drawn on a machine with no display lets the editor be drawn there too, for a
// build server checking the interface against a capture.
var config = args.Contains("--offscreen")
    ? Config.OffscreenFor(across, down)
    : Config.Windowed("BevyCSharp Editor", across, down);

// A project of the game's own when one is named, its assets edited, played and exported, and the
// editor's own assets otherwise. Named outright, since Bevy looks beside the running executable
// otherwise, which for a .NET app is whichever host launched it.
if (args.SkipWhile(argument => argument != "--project").Skip(1).FirstOrDefault() is { } project)
{
    EditorPaths.Project = Path.GetFullPath(project);
    EditorPaths.Assets = Path.Combine(EditorPaths.Project, "assets");
    Directory.CreateDirectory(EditorPaths.Assets);
}

config.AssetRoot = EditorPaths.Assets;

// The editor's own icons under a name of their own, so they load whatever project is open.
config.AssetSources[EditorPaths.OwnSource] = EditorPaths.Own;

// Its own directory for what it keeps between runs, such as the asset browser's thumbnails, named
// outright since an offscreen run has no window title to take it from.
config.GameName = "BevyCSharp Editor";

// The panels are HTML and CSS, and the point of describing them in files is being able to change
// them without a rebuild.
config.Gui = true;
config.WatchAssets = true;

// Bevy's wireframes, which outline what is selected and draw a mesh's preview as its edges.
config.Wireframes = true;

// See-through where nothing is drawn, so the editor is the shape of its panels and its scene rather
// than a black rectangle round them. The platform decides, and it falls back to an opaque window,
// which looks as the editor always did.
config.Transparent = !args.Contains("--offscreen");

// As many frames as the machine can draw rather than as many as the display shows, so the
// statistics card measures what a scene costs rather than the refresh rate, and a change that
// makes a scene slower shows as a smaller number instead of hiding under a cap.
config.Vsync = false;

// What each render pass costs, which the Frame tab lists beside the images a camera keeps. An
// editor is where a technique is tuned, so the timestamps it costs are worth paying here.
config.GpuTimings = true;

// The same goes for shaders, and a shader edited into a shape the pipeline rejects is something to
// read about in the console and fix, rather than a reason for the editor to close.
Shaders.KeepRenderingAfterErrors = true;

// Reopens where it was closed, at the size it had, unless a size was asked for, which wins, since a
// run given one is usually a check that needs the same window every time.
config.RememberWindow = size is null;

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
