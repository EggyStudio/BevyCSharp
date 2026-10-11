namespace Bevy;

/// <summary>
/// What <see cref="App.DetectAmbiguities"/> does with the pairs of systems it finds, Bevy's
/// <c>LogLevel</c> of a schedule's build settings.
/// </summary>
public enum AmbiguityReport
{
    /// <summary>Nothing, Bevy's own setting for a schedule.</summary>
    Ignore,

    /// <summary>A warning in the log as the app starts, naming each pair.</summary>
    Warn,

    /// <summary>The app refuses to start, its exception naming each pair.</summary>
    Error,
}
