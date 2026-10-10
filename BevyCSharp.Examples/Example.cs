using Bevy;

namespace BevyCSharp.Examples;

/// <summary>One of Bevy's examples written here, by Bevy's name.</summary>
/// <param name="Name">Bevy's name for it, which is also its file's.</param>
/// <param name="Build">What it adds to the app, as Bevy's <c>main</c> adds its systems.</param>
/// <param name="Configure">What it changes about the app before it runs, where it changes anything.</param>
/// <param name="Prints">
/// For an example with nothing to draw, how many frames it runs headless, and what it prints in
/// them is its capture. Zero for one that draws.
/// </param>
/// <param name="Returned">What runs once the app has stopped and the program is back in its own code.</param>
/// <param name="Drive">
/// Input given to an example that waits for it, a few keys or buttons at set frames, added when it
/// is run with <c>--drive</c> as a capture runs it, so one that prints what it is given has
/// something to print.
/// </param>
/// <remarks>
/// Bevy's examples of the ECS mostly print to the console, some in a window left empty, and one
/// that prints is run here with no window at all, so it needs no renderer and its output is
/// the same on any machine.
/// </remarks>
internal sealed record Example(
    string Name,
    Action<App> Build,
    Action<Config>? Configure = null,
    uint Prints = 0,
    Action? Returned = null,
    Action<App>? Drive = null);

/// <summary>What drives an example for its capture, beside what the package says for it.</summary>
internal static class Scene
{
    /// <summary>
    /// Runs each step on its frame, counted from the first update, for input pretended where a
    /// person would give it.
    /// </summary>
    public static App Script(this App app, params (int Frame, Action Step)[] steps)
    {
        var frame = 0;
        return app.Update(_ =>
        {
            frame++;
            foreach (var (at, step) in steps)
                if (at == frame) step();
        }, "Example.Script");
    }
}
