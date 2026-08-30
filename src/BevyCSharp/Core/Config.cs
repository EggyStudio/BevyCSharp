namespace Bevy;

/// <summary>
/// How the engine should start: window shape, presentation and headless behaviour.
/// </summary>
/// <remarks>
/// The native bridge ships in two profiles. A <c>render</c> build installs Bevy's
/// <c>DefaultPlugins</c> and opens a real window; a <c>headless</c> build installs
/// <c>MinimalPlugins</c> and drives the loop with Bevy's schedule runner. Setting
/// <see cref="Headless"/> forces the second path even on a render build, which is how tests
/// and dedicated servers run the exact same behaviour code without a display.
/// </remarks>
public sealed class Config
{
    /// <summary>Window title.</summary>
    public string Title { get; set; } = "BevyCSharp";

    /// <summary>Requested window width in logical pixels.</summary>
    public uint Width { get; set; } = 1280;

    /// <summary>Requested window height in logical pixels.</summary>
    public uint Height { get; set; } = 720;

    /// <summary>Present with vsync.</summary>
    public bool Vsync { get; set; } = true;

    /// <summary>Run without creating a window.</summary>
    public bool Headless { get; set; }

    /// <summary>Frame cap for headless runs. Zero runs as fast as the machine allows.</summary>
    public uint HeadlessFps { get; set; }

    /// <summary>
    /// Number of frames to run before exiting, for headless runs. Zero runs until something
    /// calls <see cref="App.RequestExit"/>. Tests use this to drive a fixed number of ticks.
    /// </summary>
    public uint HeadlessFrames { get; set; }

    /// <summary>
    /// Rethrow exceptions escaping a system instead of logging them and continuing.
    /// </summary>
    /// <remarks>
    /// An exception must never unwind into Rust, so it is always caught at the boundary. This
    /// switch decides what happens next: log and keep the frame going (the default, which
    /// keeps a game playable through a scripting bug), or stop the app so a test fails loudly.
    /// </remarks>
    public bool FailFastOnSystemException { get; set; }

    /// <summary>A sensible default: a 1280x720 vsynced window.</summary>
    public static Config Default => new();

    /// <summary>A windowless configuration that runs <paramref name="frames"/> ticks and exits.</summary>
    public static Config HeadlessFor(uint frames) => new()
    {
        Headless = true,
        HeadlessFrames = frames,
        FailFastOnSystemException = true,
    };

    /// <inheritdoc/>
    public override string ToString() => Headless
        ? $"Config(headless, fps={HeadlessFps}, frames={HeadlessFrames})"
        : $"Config('{Title}', {Width}x{Height}, vsync={Vsync})";
}
