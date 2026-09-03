using Bevy;
using BevyCSharp.Editor;

// Opens the editor: a scene filling the window, with panels floating over it.
//
// The panels need a bridge built with the editor profile, which carries the HTML and CSS
// surface on top of the renderer:
//     build/build-native.sh --editor

var config = Config.Windowed("BevyCSharp Editor", 1600, 900);

// Bevy looks beside the running executable otherwise, which for a .NET app is whichever host
// launched it rather than the directory the assets were copied to.
config.AssetRoot = Path.Combine(AppContext.BaseDirectory, "assets");

// The panels are HTML and CSS, and the point of describing them in files is being able to change
// them without a rebuild.
config.HtmlUi = true;
config.WatchAssets = true;

if (!App.HasRenderer)
{
    Console.Error.WriteLine("This native bridge has no renderer, so there is no window to open.");
    Console.Error.WriteLine("  rebuild it : build/build-native.sh --editor");
    return 1;
}

if (!App.HasEditor)
{
    Console.Error.WriteLine(
        "This native bridge has the renderer but not the HTML and CSS surface, so the panels "
        + "cannot open.");
    Console.Error.WriteLine("  rebuild it : build/build-native.sh --editor");
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
