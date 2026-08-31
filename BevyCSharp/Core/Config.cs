namespace Bevy;

/// <summary>
/// How the engine should start: window shape, presentation and headless behavior.
/// </summary>
/// <remarks>
/// The native bridge ships in two profiles. A <c>render</c> build installs Bevy's
/// <c>DefaultPlugins</c> and opens a real window; a <c>headless</c> build installs
/// <c>MinimalPlugins</c> and drives the loop with Bevy's schedule runner. Setting
/// <see cref="Headless"/> forces the second path even on a render build, which is how tests
/// and dedicated servers run the exact same behavior code without a display.
/// </remarks>
public sealed class Config
{
    /// <summary>Window title.</summary>
    public string Title { get; set; } = "BevyCSharp";

    /// <summary>
    /// Which graphics API the renderer should use.
    /// </summary>
    /// <remarks>
    /// Ignored in a headless run. <see cref="GraphicsBackend.Automatic"/> lets wgpu pick, which
    /// on Linux and Windows already prefers Vulkan; naming one explicitly is for pinning the
    /// choice rather than improving it. Ask for a backend the machine cannot provide and startup
    /// fails rather than silently falling back.
    /// </remarks>
    public GraphicsBackend Backend { get; set; } = GraphicsBackend.Automatic;

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
    /// Where assets are loaded from. Null uses Bevy's default of <c>assets</c> beside the
    /// executable.
    /// </summary>
    /// <remarks>
    /// Worth setting, because "beside the executable" is rarely where a .NET app's assets are.
    /// Bevy resolves a relative path against the running executable, which under <c>dotnet
    /// test</c> or <c>dotnet exec</c> is the host rather than the assembly, so an
    /// <c>assets</c> directory copied next to the DLL is not found. Naming it outright is the
    /// only way to be sure:
    /// <code>
    /// AssetRoot = Path.Combine(AppContext.BaseDirectory, "assets")
    /// </code>
    /// </remarks>
    public string? AssetRoot { get; set; }

    /// <summary>
    /// How many times a second <see cref="Stage.FixedUpdate"/> runs. Zero keeps Bevy's own
    /// default of 64.
    /// </summary>
    /// <remarks>
    /// The rate is a simulation decision rather than a performance one: it fixes the slice of
    /// time each step covers, and so fixes the results. Change it and a replay of the same
    /// inputs diverges, which is why it belongs here rather than being tuned at runtime.
    /// </remarks>
    public double FixedHz { get; set; }

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

    /// <summary>A window of the given size, drawn with <paramref name="backend"/>.</summary>
    public static Config Windowed(
        string title,
        uint width = 1280,
        uint height = 720,
        GraphicsBackend backend = GraphicsBackend.Automatic) => new()
    {
        Title = title,
        Width = width,
        Height = height,
        Backend = backend,
    };

    /// <inheritdoc/>
    public override string ToString() => Headless
        ? $"Config(headless, fps={HeadlessFps}, frames={HeadlessFrames})"
        : $"Config('{Title}', {Width}x{Height}, vsync={Vsync}, backend={Backend})";
}

/// <summary>
/// A graphics API the renderer can be pinned to.
/// </summary>
/// <remarks>
/// These map onto wgpu's <c>Backends</c> flags, which is what Bevy's renderer is built on.
/// Only backends the host platform supports are meaningful: Direct3D 12 is Windows-only and
/// Metal is Apple-only.
/// </remarks>
public enum GraphicsBackend
{
    /// <summary>Let wgpu choose. Prefers Vulkan on Linux and Windows, Metal on Apple.</summary>
    Automatic = 0,

    /// <summary>Vulkan. Available on Linux, Windows and Android.</summary>
    Vulkan = 1,

    /// <summary>Direct3D 12. Windows only.</summary>
    Direct3D12 = 2,

    /// <summary>Metal. macOS and iOS only.</summary>
    Metal = 3,

    /// <summary>OpenGL or OpenGL ES. A fallback for machines with no modern driver.</summary>
    OpenGL = 4,
}
