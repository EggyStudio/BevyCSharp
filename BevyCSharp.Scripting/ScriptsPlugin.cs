namespace Bevy.Scripting;

/// <summary>
/// Compiles the behavior scripts in the asset folder's <c>scripts</c> folder into the app as it is
/// built, and again whenever one changes while the app watches its assets.
/// </summary>
/// <remarks>
/// <para>
/// The first build is made before the app runs, so a state a script declares on its enum is added
/// with the rest and a scene loaded at startup finds the scripts' components as types. A change is
/// built while the app runs (<see cref="Scripts"/>), so the app enables dynamic systems, whose
/// dispatchers have to be in the schedule before Bevy takes it.
/// </para>
/// <para>
/// Changes are watched where <see cref="Config.WatchAssets"/> is on, as they are for the other
/// assets, and the folder is made where it is watched and missing, so a first script written into
/// it is seen. <c>scripts.reload</c> builds again on request and <c>scripts.status</c> says what
/// runs, watched or not.
/// </para>
/// <para>
/// This project carries Roslyn, which the package leaves out, since a shipped game compiles its
/// behaviors as it is built and needs no compiler to run. The editor and the Player add this, and a
/// game that references the project can add it too.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// app.AddPlugin(new ScriptsPlugin());
///
/// // Later, in a behavior:
/// var scripts = ctx.Res&lt;Scripts&gt;();
/// if (scripts.LastError is { } error) Log.Warn(error);
/// </code>
/// </example>
/// <param name="directory">The folder the scripts are in, or nothing for <c>scripts</c> under the asset folder.</param>
public sealed class ScriptsPlugin(string? directory = null) : IPlugin
{
    /// <inheritdoc/>
    public void Build(App app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.EnableDynamicSystems();

        // The asset root the app settled on as it was made, its config's, a program built in Debug's
        // project's assets folder, or the copy beside the program.
        var assets = Streaming.AssetRoot;
        var scripts = new Scripts(app, Path.GetFullPath(directory ?? Path.Combine(assets, "scripts")), Path.GetFullPath(assets), app.Config.WatchAssets);

        // A resource, so the world disposes it with the app and the watcher stops with it, and the
        // commands find the app's own scripts and no other app's.
        app.World.InsertResource(scripts);
        scripts.Build(string.Empty);

        if (scripts.Watching)
            app.AddSystem(Stage.First, new SystemDescriptor(world => world.Resource<Scripts>().Poll(), "Scripts.Watch"));
    }
}
