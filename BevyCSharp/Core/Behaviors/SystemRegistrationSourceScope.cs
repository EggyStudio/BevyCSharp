namespace Bevy;

/// <summary>
/// Tags every system registered inside the scope with a provenance string.
/// </summary>
/// <remarks>
/// This makes hot-reload swappable. A reloaded generation of behaviors registers under its own tag,
/// and <see cref="App.RemoveSystemsBySource"/> retires the previous one without touching systems
/// that came from anywhere else.
/// </remarks>
public sealed class SystemRegistrationSourceScope : IDisposable
{
    [ThreadStatic] private static string? _current;

    private readonly string? _previous;

    /// <summary>The tag in force on this thread, if any.</summary>
    public static string? Current => _current;

    /// <summary>Applies <paramref name="source"/> until disposed.</summary>
    public SystemRegistrationSourceScope(string source)
    {
        _previous = _current;
        _current = source;
    }

    /// <inheritdoc/>
    public void Dispose() => _current = _previous;
}
