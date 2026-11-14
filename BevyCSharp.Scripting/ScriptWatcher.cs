namespace Bevy.Scripting;

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
/// <para>
/// The system's watcher can lose changes, its buffer overflowing under many at once, or stop, its
/// folder removed. Either raises the flag too, so every script is compiled again, and is kept for
/// the main thread to say. A watcher that stopped is started again once its folder is there.
/// </para>
/// </remarks>
public sealed class ScriptWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly Lock _gate = new();

    private DateTime _changedAt = DateTime.MinValue;
    private bool _changed;
    private string? _lost;

    /// <summary>How long a file has to sit still before it is rebuilt.</summary>
    private static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(250);

    /// <summary>Starts watching <paramref name="directory"/> for changes to C# files.</summary>
    /// <exception cref="IOException">The system would not watch the folder, its limit on watchers met.</exception>
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
        _watcher.Error += OnError;
    }

    /// <summary>Whether the system's watcher stopped and has not been started again.</summary>
    public bool Stopped => !_watcher.EnableRaisingEvents;

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

    /// <summary>
    /// What the system's watcher said as it lost changes or stopped, once, clearing it, and the
    /// watcher started again where it stopped and its folder is there.
    /// </summary>
    public string? TakeLost()
    {
        string? lost;
        lock (_gate)
        {
            lost = _lost;
            _lost = null;
        }

        if (Stopped && Directory.Exists(_watcher.Path))
        {
            try
            {
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // Tried again at the next poll.
            }
        }

        return lost;
    }

    /// <summary>Raises the flag again, for a build that could not read a script to be tried once it has settled.</summary>
    public void Again()
    {
        lock (_gate)
        {
            _changed = true;
            _changedAt = DateTime.UtcNow;
        }
    }

    /// <summary>Forgets a change waiting to settle, as a build asked for meanwhile takes it in.</summary>
    public void Forget()
    {
        lock (_gate) _changed = false;
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        lock (_gate)
        {
            _changed = true;
            _changedAt = DateTime.UtcNow;
            _lost = e.GetException().Message;
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
