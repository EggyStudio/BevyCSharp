namespace Bevy.Scripting;

/// <summary>
/// The behavior scripts an app compiles while it runs, the resource <see cref="ScriptsPlugin"/>
/// adds, which says each build in the log and builds again when a script changes.
/// </summary>
/// <remarks>
/// <para>
/// Every build is said, the first and each after it, with the files compiled, the behaviors
/// registered and the components carried onto the new types. A build that does not compile is said
/// as a warning with the compiler's errors, and the generation running before it goes on running,
/// so a file saved half typed costs a line in the log and nothing else.
/// </para>
/// <para>
/// A change under the folder is noticed on a thread of the file system's choosing and built at the
/// next frame on the main thread, once the file has sat still (<see cref="ScriptWatcher"/>), since a
/// build registers systems and touches the world. A build asked for by <c>scripts.reload</c> or by
/// <see cref="Reload"/> is made at once, inside the system that asked.
/// </para>
/// <para>
/// Each build after the first is posted as <see cref="AssetReloaded"/> of the kind <c>Scripts</c>,
/// the folder's path under the asset root as its path, as every file the app reads again is.
/// </para>
/// </remarks>
public sealed class Scripts : IDisposable
{
    private readonly App _app;
    private readonly ScriptHost _host;
    private readonly ScriptWatcher? _watcher;

    /// <summary>The folder as a reload names it, under the asset root where it is there.</summary>
    private readonly string _named;

    internal Scripts(App app, string directory, string assets, bool watching)
    {
        _app = app;
        _host = new ScriptHost(app, directory);

        // A folder the system will not watch, its limit on watchers met or the folder not to be
        // made, leaves the scripts built on request alone, which is no reason to refuse to start.
        try
        {
            if (watching) _watcher = new ScriptWatcher(directory);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"{directory} cannot be watched, so a changed script is compiled by scripts.reload alone: {error.Message}");
        }

        var under = Path.GetRelativePath(assets, directory).Replace('\\', '/');
        _named = under.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(under) ? directory : under;
    }

    /// <summary>The folder the scripts are compiled from.</summary>
    public string Directory => _host.Directory;

    /// <summary>Whether a change to a script builds them again, as it does where the app watches its assets.</summary>
    public bool Watching => _watcher is not null;

    /// <summary>Whether the system's watcher stopped, its folder removed, until it is started again.</summary>
    public bool WatcherStopped => _watcher?.Stopped == true;

    /// <summary>How many behaviors the running generation registered.</summary>
    public int Registered => _host.Registered;

    /// <summary>How many script files the running generation was compiled from.</summary>
    public int Files => _host.Files;

    /// <summary>How many components the last build moved onto the new generation's types.</summary>
    public int Carried => _host.Carried;

    /// <summary>What went wrong with the last build, or nothing where it compiled.</summary>
    public string? LastError => _host.LastError;

    /// <summary>How many builds compiled and were put in place, the first among them.</summary>
    public int Builds { get; private set; }

    /// <summary>Raised on the main thread after each build that compiled and was put in place.</summary>
    /// <remarks>For a program saying more of a build than the log line does, as the editor says the states the scripts declare.</remarks>
    public event Action<Scripts>? Built;

    /// <summary>Compiles the scripts again and puts them in place of the running generation.</summary>
    /// <returns>Whether they compiled, or a folder emptied of scripts stopped them. Where they did not, the running generation stays.</returns>
    /// <remarks>A change waiting to settle is taken in by this build, so it is not built again.</remarks>
    public bool Reload()
    {
        _watcher?.Forget();
        return Build("on request");
    }

    /// <summary>Builds when a script changed and has since sat still.</summary>
    internal void Poll()
    {
        if (_watcher is null) return;

        if (_watcher.TakeLost() is { } lost)
            Log.Warn($"The watcher of {Directory} lost track of its changes, so every script is compiled again: {lost}");

        if (_watcher.TakeChange() && !Build("after a change") && _host.Unread) _watcher.Again();
    }

    /// <summary>Compiles the scripts and says how it went, <paramref name="why"/> naming what asked.</summary>
    internal bool Build(string why)
    {
        var again = Builds > 0;

        // A folder with nothing in it yet, as a new project's, is no failure to warn of, and one
        // emptied of its scripts stops what they ran, as removing one of two stops that one's.
        if (!System.IO.Directory.Exists(Directory) || !System.IO.Directory.EnumerateFiles(Directory, "*.cs", SearchOption.AllDirectories).Any())
        {
            if (_host.Running)
            {
                _host.Clear();
                Log.Info($"No scripts are left in {Directory}, so the behaviors they ran stop.");
                _app.World.Resource<MessageBus>().Send(new AssetReloaded(_named, "Scripts"));
                return true;
            }

            Log.Info(again ? $"No scripts are left in {Directory}."
                : Watching ? $"No scripts in {Directory} yet, and one written there is compiled." : $"No scripts in {Directory}.");
            return false;
        }

        if (!_host.Reload())
        {
            Log.Warn(again
                ? $"The scripts in {Directory} did not compile {why}, so the ones running stay: {_host.LastError}"
                : $"The scripts in {Directory} did not compile, so none run: {_host.LastError}");
            return false;
        }

        Builds++;
        var carried = _host.Carried > 0 ? $", {Count(_host.Carried, "component")} carried onto the new types" : string.Empty;
        Log.Info(again
            ? $"Compiled the scripts in {Directory} again {why}, {Count(Files, "file")} and {Count(Registered, "behavior")}{carried}."
            : $"Compiled the scripts in {Directory}, {Count(Files, "file")} and {Count(Registered, "behavior")}{carried}.");

        if (again) _app.World.Resource<MessageBus>().Send(new AssetReloaded(_named, "Scripts"));

        Built?.Invoke(this);
        return true;
    }

    /// <summary>A count with its noun, made plural past one.</summary>
    internal static string Count(int count, string noun) => count == 1 ? $"one {noun}" : $"{count} {noun}s";

    /// <summary>Stops watching and lets go of the running generation, whose systems go with the app.</summary>
    public void Dispose()
    {
        _watcher?.Dispose();
        _host.Release();
    }
}
