namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Rebuilds a <see cref="ScriptHost"/> when anything under its directory changes.
/// </summary>
/// <remarks>
/// <para>
/// The watcher only raises a flag. The rebuild happens on the main thread when
/// <see cref="TakeChange"/> is next asked, because compiling and registering systems touches the
/// app, and the watcher's callback arrives on a thread of the file system's choosing.
/// </para>
/// <para>
/// An editor writes a file in several operations, so one save can arrive as three events. The
/// flag is a flag rather than a count for that reason, and a short settling time keeps a rebuild
/// from starting on a file that is still half written.
/// </para>
/// </remarks>
public sealed class ScriptWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly Lock _gate = new();

    private DateTime _changedAt = DateTime.MinValue;
    private bool _changed;

    /// <summary>How long a file has to sit still before it is rebuilt.</summary>
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(250);

    /// <summary>Starts watching <paramref name="directory"/> for changes to C# files.</summary>
    public ScriptWatcher(string directory)
    {
        Directory.CreateDirectory(directory);

        _watcher = new FileSystemWatcher(directory, "*.cs")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnChanged;
    }

    /// <summary>
    /// Whether something changed and has since settled, clearing the flag if so.
    /// </summary>
    public bool TakeChange()
    {
        lock (_gate)
        {
            if (!_changed || DateTime.UtcNow - _changedAt < Settle) return false;

            _changed = false;
            return true;
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        lock (_gate)
        {
            _changed = true;
            _changedAt = DateTime.UtcNow;
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _watcher.Dispose();
}
