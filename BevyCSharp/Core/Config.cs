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
    /// calls <see cref="App.RequestExit()"/>. Tests use this to drive a fixed number of ticks.
    /// </summary>
    public uint HeadlessFrames { get; set; }

    /// <summary>
    /// Where assets are loaded from. Null uses Bevy's default of <c>assets</c> beside the
    /// executable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Worth setting, because "beside the executable" is rarely where a .NET app's assets are.
    /// Bevy resolves a relative path against the running executable, which under <c>dotnet
    /// test</c> or <c>dotnet exec</c> is the host rather than the assembly, so an <c>assets</c>
    /// directory copied next to the DLL is not found.
    /// </para>
    /// <para>
    /// For an application, <see cref="AppContext.BaseDirectory"/> is the right anchor:
    /// <code>
    /// AssetRoot = Path.Combine(AppContext.BaseDirectory, "assets")
    /// </code>
    /// Under a test runner or any other custom host it is not, because it names the directory of
    /// whatever started the process. Anchor on the assembly that owns the assets instead:
    /// <code>
    /// Path.GetDirectoryName(typeof(MyGame).Assembly.Location)
    /// </code>
    /// </para>
    /// </remarks>
    public string? AssetRoot { get; set; }

    /// <summary>
    /// The assembly whose resources carry the game's asset files, or nothing for the entry assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A file is read from <see cref="AssetRoot"/> first and from these resources when the folder
    /// has none, by the managed side's own reads (<see cref="AssetFiles"/>) and by Bevy's. A resource
    /// counts when its name starts with <c>assets/</c>, which a build gives every file under the
    /// project's asset folder when <c>BevyCSharpEmbedAssets</c> is set, so a game published that way
    /// ships as one assembly with the bridge every game shares.
    /// </para>
    /// <para>
    /// The entry assembly is the game's whenever the game starts the process, so this is set only
    /// where something else does, such as a test runner, whose entry assembly is its own host.
    /// </para>
    /// </remarks>
    public System.Reflection.Assembly? AssetAssembly { get; set; }

    /// <summary>
    /// Folders Bevy reads as asset sources of their own, by name, so <c>name://path</c> loads from
    /// the folder named.
    /// </summary>
    /// <remarks>
    /// For files that belong beside the assets rather than among them, such as a tool's own icons
    /// beside the project it opens, which the editor names <c>editor</c>. <c>user</c> is the
    /// player's directory already and cannot be named again. Read as the app is created, since
    /// Bevy builds its sources then and never again.
    /// </remarks>
    public Dictionary<string, string> AssetSources { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// A pack the game's assets are read from after <see cref="AssetRoot"/>, or nothing for
    /// <see cref="AssetPack.DefaultName"/> beside the executable when there is one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A file is read from the folder first, then from the pack, then from
    /// <see cref="AssetAssembly"/>, by the managed side's own reads and by Bevy's alike. A pack is
    /// one file read a part at a time, for a game whose assets are too large to compile into its
    /// assembly, and the Play tab's export writes one when asked to pack.
    /// </para>
    /// <para>
    /// An empty string reads no pack, which a test or a tool reading only the folder sets. A pack
    /// that cannot be opened, named here or beside the executable, is said as an error and the app
    /// runs without it, as N 2.6 of NORM.md has a bad file answered rather than thrown, so each
    /// load of a file it held fails naming the file.
    /// </para>
    /// </remarks>
    public string? AssetPack { get; set; }

    /// <summary>
    /// The name the game's own files are kept under, in the platform's data directory.
    /// </summary>
    /// <remarks>
    /// What <c>user://</c> paths, settings and saves resolve under (<see cref="UserData"/>), or
    /// <see langword="null"/> for <see cref="Title"/>. Worth setting, because a title is changed
    /// for a sequel or a translation, and every player's saves are under the old name.
    /// </remarks>
    public string? GameName { get; set; }

    /// <summary>
    /// Reload an asset when its file changes on disk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What lets a texture, a document or a stylesheet be edited while the app runs. A handle
    /// keeps pointing at the same asset, so anything holding one picks the new version up without
    /// being told.
    /// </para>
    /// <para>
    /// Needs a bridge whose profile carries the watcher, which today means the editor one. On a
    /// build without it this does nothing rather than failing, because whether a file changed is
    /// not a question the app can answer for itself. It costs a thread watching the asset
    /// directory, which is why it is off unless asked for.
    /// </para>
    /// </remarks>
    public bool WatchAssets { get; set; }

    /// <summary>
    /// Draw an interface: Dear ImGui, rasterized by the engine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What <see cref="ImGuiRuntime"/> needs. Off unless asked for, because it is not free to an
    /// app that never draws one. It brings the pass that rasterizes the interface and the buffers
    /// it draws from. The editor profile is a superset of the render one, so a game and the editor
    /// run against the same library and this tells them apart.
    /// </para>
    /// <para>
    /// Needs a bridge built with that profile, which <see cref="App.HasEditor"/> reports. On a
    /// build without it this does nothing.
    /// </para>
    /// </remarks>
    public bool Gui { get; set; }

    /// <summary>
    /// How many times a second <see cref="Stage.FixedUpdate"/> runs. Zero keeps Bevy's own
    /// default of 64.
    /// </summary>
    /// <remarks>
    /// The rate is a simulation decision rather than a performance one, because it fixes the slice
    /// of time each step covers, and so fixes the results. Change it and a replay of the same
    /// inputs diverges, which is why it belongs here rather than being tuned at runtime.
    /// </remarks>
    public double FixedHz { get; set; }

    /// <summary>
    /// Seconds each frame advances the clock by, in place of the machine's, or zero for the
    /// machine's clock.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bevy's <c>TimeUpdateStrategy::ManualDuration</c>. Every frame is then that long to the game,
    /// to <see cref="Time.DeltaSeconds"/> and to the fixed steps a frame runs, however long it took
    /// the machine, so a test counts frames rather than waiting on the clock and a capture of
    /// something moving is the same on every machine. Sixty frames of a sixtieth are a second
    /// exactly, and run as fast as the machine allows where <see cref="HeadlessFps"/> does not pace
    /// them. The first frame reads no time gone, as Bevy's first frame always does.
    /// </para>
    /// <para>
    /// <see cref="Time.FrameSeconds"/> changes it while the app runs, as for one slow frame made on
    /// purpose, and <see cref="FrameTimeVariable"/> sets it from outside. <see cref="Time.Step"/> is
    /// another thing, running frames of the length the machine gives them while the game is paused.
    /// </para>
    /// </remarks>
    public double FrameSeconds { get; set; }

    /// <summary>
    /// Find the mesh under a pointer, for what a pointer does to it (<see cref="Pointer{TEvent}"/>)
    /// and for <see cref="Picking.Drain"/>.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>MeshPickingPlugin</c>, which casts a ray at every mesh in the scene as the pointer
    /// moves. The interface's nodes and sprites are found without it, as in Bevy, whose default
    /// plugins leave meshes out for that cost and a program adds the plugin where it picks one. A
    /// bridge with the renderer is needed, and the editor has it whatever this says.
    /// </remarks>
    public bool MeshPicking { get; set; }

    /// <summary>
    /// Draw with no window, into an image a capture can be read back from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The renderer without a screen. Everything else about the run is as it would be in a window,
    /// down to the plugins that are installed and the cameras that draw. <see cref="Width"/> and
    /// <see cref="Height"/> size the image the way they would size the window, and
    /// <see cref="Render.Screenshot(string)"/> captures it.
    /// </para>
    /// <para>
    /// What it is for is a machine with no display. A windowed run needs a display server to open
    /// a window on, and a build server or a container has none, so this is the only way to see
    /// what a change draws there. It needs a bridge with the renderer compiled in, which
    /// <see cref="App.HasRenderer"/> reports, and is ignored when <see cref="Headless"/> is set,
    /// which asks for no renderer at all.
    /// </para>
    /// <para>
    /// <see cref="HeadlessFps"/> and <see cref="HeadlessFrames"/> pace and bound it, because a run
    /// with no window has no window to close and would otherwise never end.
    /// </para>
    /// </remarks>
    public bool Offscreen { get; set; }

    /// <summary>The environment variable that asks a windowed run to draw offscreen instead.</summary>
    /// <remarks>
    /// Read by every app, as <c>BCS_SERVE</c> is, so a tool running a game it did not write can
    /// keep it off the screen, as the editor's Play does when the editor itself has no window and a
    /// script does playing a game on a machine with no display. A run asked to be headless stays
    /// headless.
    /// </remarks>
    public const string OffscreenVariable = "BCS_OFFSCREEN";

    /// <summary>The environment variable that bounds a run with no window to a number of frames.</summary>
    /// <remarks>
    /// For a run kept off the screen with <see cref="OffscreenVariable"/>, which has no window to
    /// close, so a script trying a game it did not write can let it run a while and end on its own.
    /// It sets <see cref="HeadlessFrames"/> where the config left that at zero.
    /// </remarks>
    public const string FramesVariable = "BCS_FRAMES";

    /// <summary>The frames the environment bounds a run with no window to, or zero.</summary>
    public static uint FramesAsked =>
        uint.TryParse(Environment.GetEnvironmentVariable(FramesVariable), out var frames) ? frames : 0u;

    /// <summary>The environment variable that sets the seconds each frame advances the clock by.</summary>
    /// <remarks>
    /// Sets <see cref="FrameSeconds"/> where the config left it at zero, so a script capturing a
    /// game it did not write, or a session opened with <c>bcs open --frame-time</c>, moves it by
    /// frames and not by the machine's clock.
    /// </remarks>
    public const string FrameTimeVariable = "BCS_FRAME_TIME";

    /// <summary>The seconds a frame the environment sets the clock to, or zero.</summary>
    public static double FrameSecondsAsked =>
        double.TryParse(Environment.GetEnvironmentVariable(FrameTimeVariable), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var seconds) && double.IsFinite(seconds) && seconds > 0
            ? seconds
            : 0;

    /// <summary>True when the environment asks for <see cref="Offscreen"/> whatever the config says.</summary>
    public static bool OffscreenAsked =>
        Environment.GetEnvironmentVariable(OffscreenVariable) is { Length: > 0 } value
        && value is not ("0" or "off" or "false" or "no");

    /// <summary>
    /// How many world units a meter is, for every spatial sound that does not say otherwise.
    /// </summary>
    /// <remarks>
    /// How far away a sound is depends on what the world is measured in, which is a fact about the
    /// game rather than about any one sound, so it is set once here. Zero keeps Bevy's own of one,
    /// which suits a world measured in meters; a world measured in centimeters needs a hundred. A
    /// sound may still say otherwise for itself.
    /// </remarks>
    public float SpatialScale { get; set; }

    /// <summary>
    /// Play sound to the machine's device in a run with no window, headless or
    /// <see cref="Offscreen"/>, which otherwise plays every sound to none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A run with no window to look at is a test, a soak, a capture or a tool, and a sound it
    /// plays reaches somebody beside the machine who never asked for it, so by default such a run
    /// opens no device. Its sounds still run their course: each is decoded and drawn from on the
    /// app's clock as a device would draw from it, so it pauses, seeks, loops and ends when it
    /// would be heard to, a sound played with <see cref="PlaybackMode.Despawn"/> goes on time, and
    /// a game that waits on a sound's end works the same. <see cref="Audio.IsSilent"/> says which
    /// a run does.
    /// </para>
    /// <para>
    /// Set where a run with no window is meant to be heard, a game played offscreen into a stream
    /// or a tool that plays sounds for somebody listening. A run with a window always plays to the
    /// device, where the machine has one.
    /// </para>
    /// </remarks>
    public bool AudioWithoutWindow { get; set; }

    /// <summary>
    /// How many meshlet clusters the GPU keeps room for at once, which turns Bevy's meshlets on.
    /// Zero, the default, leaves them off.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Meshlets need a bridge built with them (<c>build/build-native.sh --editor --meshlet</c>) and a
    /// GPU with 64-bit texture atomics on Vulkan or Metal. The bridge asks the GPU before turning
    /// them on, and on one that lacks them says so and runs without, which
    /// <see cref="Render.MeshletsActive"/> reports.
    /// </para>
    /// <para>
    /// Each cluster costs four bytes of GPU memory, and too few shows as meshes flickering or
    /// missing parts where more clusters are in view than there is room for. A few million is room
    /// for a dense scene, and Bevy's limit is two to the twenty-fifth. While meshlets run every
    /// camera draws once a pixel, since Bevy's meshlet renderer cannot draw a multisampled picture.
    /// </para>
    /// <para>
    /// Bevy runs every meshlet pass in every shadow view, six for each point light that casts
    /// shadows and one for each cascade of a directional light, whether or not a meshlet mesh is
    /// in it. On an NVIDIA driver each such view came to hold a few hundred megabytes of the
    /// driver's own memory over its first quarter of a minute, wgpu's resources standing still,
    /// so a scene lit by many shadowed lights holds gigabytes more with meshlets than without, and
    /// its lights are better left without shadows while they run. The <c>memory</c> command's
    /// <c>gpu.</c> pairs beside the process's size tell this apart from a leak of the game's own.
    /// </para>
    /// </remarks>
    public uint MeshletClusters { get; set; }

    /// <summary>
    /// Add the weather, a sky around Bevy's atmosphere with clouds, fog, rain, snow and thunder,
    /// stars and a moon, drawn for each camera marked as a weather camera
    /// (<see cref="Bevy.Weather"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>bevy_weather</c> crate, which the bridge's render profile carries. It brings a planet
    /// with an atmosphere, and a sun and a moon of its own where the app marks none of its lights
    /// as them, and puts on each weather camera the exposure its sky is calibrated for, leaving the
    /// tonemapper and the bloom to <see cref="Render.SetPostProcessing"/>. Asked for rather than
    /// assumed, since an app without it should get none of those. A headless app has no weather,
    /// and one where meshlets run has none either, since a camera under the weather's atmosphere
    /// would end it there, which <see cref="Bevy.Weather.Active"/> reports.
    /// </para>
    /// </remarks>
    public bool Weather { get; set; }

    /// <summary>
    /// How many gigabytes of the machine's memory the process may hold before it is stopped, or
    /// zero for the cap <c>BCS_MEMORY_CAP_GB</c> names, or none where it names none either.
    /// </summary>
    /// <remarks>
    /// For a run that might take the machine's memory with it, a test, a tool driving a scene or a
    /// technique being tried, so it stops and says why where the system would otherwise end
    /// whatever it chose. A shipped game is held to none unless it asks. See
    /// <see cref="MemoryGuard"/>.
    /// </remarks>
    public double MemoryCap { get; set; }

    /// <summary>
    /// Measure how long every render pass takes, on the CPU and the GPU, for
    /// <see cref="Render.Timings"/>.
    /// </summary>
    /// <remarks>
    /// Bevy's own passes are measured, and so is every dispatch, pass and draw a shader program
    /// makes, under the program's file name, which is how a technique made of many passes is tuned
    /// one pass at a time. Off by default, since every measured pass writes timestamps and every
    /// frame reads them back. GPU times need an adapter with timestamp queries, which Vulkan and
    /// DirectX 12 have; elsewhere only CPU times arrive.
    /// </remarks>
    public bool GpuTimings { get; set; }

    /// <summary>
    /// Add Bevy's wireframe plugins, so <see cref="Render.SetWireframe"/> can draw a mesh as its
    /// edges, and Bevy's wireframe components and settings, for 3D and 2D meshes, are drawn.
    /// </summary>
    /// <remarks>
    /// Off by default, as Bevy leaves the plugins out of its defaults, which its own wireframe
    /// examples add. Each plugin looks at every mesh of its kind in the render's prepare and queue
    /// phases on every frame, whether or not any is drawn as edges, which costs about 8 ms a frame
    /// at a hundred thousand sprite meshes. An editor outlining what is selected turns it on.
    /// </remarks>
    public bool Wireframes { get; set; }

    /// <summary>
    /// Log how fast frames run once a second, by Bevy's own <c>FrameTimeDiagnosticsPlugin</c> and
    /// <c>LogDiagnosticsPlugin</c>.
    /// </summary>
    /// <remarks>
    /// What Bevy's stress tests add, so a program written again on this package and Bevy's own,
    /// measured side by side, log their frames by the same code, and the program here runs no system
    /// of its own for it. The lines go to the log Bevy writes to the console.
    /// </remarks>
    public bool LogFrameTimes { get; set; }

    /// <summary>
    /// The window's scale factor in place of the display's, or zero to keep the display's.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>WindowResolution::with_scale_factor_override</c>. A window of 1920 by 1080 on a
    /// display scaled to two is 3840 by 2160 pixels unless its scale factor is held at one, as
    /// Bevy's stress tests hold it, so the pixels a frame fills are the ones its size says. An image
    /// drawn offscreen has no display to scale it and takes none.
    /// </remarks>
    public float ScaleFactor { get; set; }

    /// <summary>
    /// Make the window see-through wherever what is drawn into it has no alpha, so the desktop
    /// shows behind it there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Decided when the window is made, since the platform builds a see-through window
    /// differently from an opaque one. What is left clear is then clear: the world's clear color
    /// where it has no alpha (<see cref="Render.SetClearColor"/>), the part of a window a camera's
    /// viewport leaves out, and the corners <see cref="Render.SetRoundedCorners"/> takes off. A
    /// window without a frame (<see cref="Window.SetStyle"/>) and with this on can be any shape.
    /// </para>
    /// <para>
    /// Not every platform can. Where the window's surface cannot be composited with alpha, which
    /// is often so under DirectX 12, the window is made opaque instead and what would have been
    /// clear is black, and the log says which it got. Ignored without a window.
    /// </para>
    /// </remarks>
    public bool Transparent { get; set; }

    /// <summary>
    /// Have the desktop draw the window's title bar and border in its own current style, on the
    /// Linux desktops that draw them only for X11 windows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// GNOME does not draw a frame for a Wayland window, so on Wayland the window draws its own,
    /// and the one it has is a re-creation of an older GNOME look that no longer matches the rest
    /// of the desktop. GNOME does draw a frame for an X11 window, in the theme everything else
    /// wears. With this on, a window opened under GNOME on Wayland is opened through XWayland, and
    /// its title bar and buttons are the desktop's own.
    /// </para>
    /// <para>
    /// Off unless asked for, since a window through XWayland is scaled by the desktop rather than
    /// drawn at the screen's own resolution, which looks soft at a fractional scale such as 125%.
    /// Does nothing on a desktop that draws Wayland frames itself (KDE, most tiling compositors),
    /// off Linux, where there is no X11 server to go through, or for a window without a frame.
    /// </para>
    /// </remarks>
    public bool DesktopTitleBar { get; set; }

    /// <summary>
    /// Reopen the window where it was last closed, at the size it had and maximized if it was.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A desktop app is expected to come back where it was left. The window's place is kept in
    /// <c>user://window.json</c> through <see cref="Persistent{T}"/>, read before the window opens
    /// so it opens there rather than opening elsewhere and jumping, and written when the place
    /// changes, checked a few times a second. A maximized window keeps the size it had before, so
    /// putting it back after the next start goes back to that size rather than to a full screen.
    /// </para>
    /// <para>
    /// Wayland never tells an app where its window is, so there the size and whether it was
    /// maximized come back and the compositor chooses the place. Off unless asked for, since a test
    /// or a tool that opens a window needs the same window every time, and ignored without one.
    /// </para>
    /// </remarks>
    public bool RememberWindow { get; set; }

    /// <summary>
    /// Make Bevy's ray-traced lighting available, which a camera then turns on with
    /// <see cref="Render.SetRayTracedLighting"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bevy's Solari traces rays against the scene with the GPU's ray tracing hardware: direct light
    /// from every light and every emissive surface, and indirect light bounced off everything, in
    /// real time. It needs a bridge built with it (<c>./bcs build --editor --solari</c>) and an
    /// adapter with ray queries, which the bridge asks about before turning it on and which
    /// <see cref="Render.RayTracingActive"/> reports.
    /// </para>
    /// <para>
    /// Turning it on makes every one of Bevy's materials deferred for the whole app, since Solari
    /// reads the G-buffer, which is why it is asked for here rather than per camera. Materials a
    /// Slang program draws are not lit by it.
    /// </para>
    /// </remarks>
    public bool RayTracedLighting { get; set; }

    /// <summary>
    /// The folder the run's log and a crash's report are written in, or nothing for <c>logs</c>
    /// beside the executable, or an empty string for neither.
    /// </summary>
    /// <remarks>
    /// Read by the first app of a process, whose folder holds for the rest of it, since a run has
    /// one log (<see cref="CrashLog"/>). A game on a player's machine keeps it, so what it said
    /// before it ended can be sent, and a test leaves it empty, as the test harness does, since a
    /// suite is many apps in one process and the console's tee would hold every test's output.
    /// </remarks>
    public string? Logs { get; set; }

    /// <summary>
    /// Answer the command line while this app runs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Opens a socket on the loopback interface and writes a session file, so <c>bcs</c> can ask
    /// the running app what it holds, run any console command against it, and capture what it
    /// draws, without stopping it. The alternative is a process per question, which is a second of
    /// startup and a fresh world for every answer.
    /// </para>
    /// <para>
    /// Off unless asked for, because it is a port. <c>BCS_SERVE</c> in the environment turns it on
    /// for a run that cannot be recompiled, and the sample and the editor both accept
    /// <c>--serve</c>.
    /// </para>
    /// </remarks>
    public bool Serve { get; set; }

    /// <summary>
    /// Rethrow exceptions escaping a system instead of logging them and continuing.
    /// </summary>
    /// <remarks>
    /// An exception must never unwind into Rust, so it is always caught at the boundary. This
    /// switch decides what happens next: log and keep the frame going (the default, which
    /// keeps a game playable through a scripting bug), or stop the app so a test fails loudly.
    /// </remarks>
    public bool FailFastOnSystemException { get; set; }

    /// <summary>A sensible default, a 1280x720 vsynced window.</summary>
    public static Config Default => new();

    /// <summary>A windowless configuration that runs <paramref name="frames"/> ticks and exits.</summary>
    public static Config HeadlessFor(uint frames) => new()
    {
        Headless = true,
        HeadlessFrames = frames,
        FailFastOnSystemException = true,
    };

    /// <summary>
    /// A configuration that draws into an image of the given size instead of a window.
    /// </summary>
    /// <remarks>
    /// Paced at 60 rather than run flat out, because what an offscreen run is usually asked for is
    /// a picture of a scene that has settled, and a loop with no frame budget spends the wait
    /// competing with the work it is waiting for.
    /// </remarks>
    /// <param name="width">Width of the image, in pixels.</param>
    /// <param name="height">Height of the image, in pixels.</param>
    /// <param name="frames">Frames to run before exiting, or 0 to run until asked to stop.</param>
    public static Config OffscreenFor(uint width = 1280, uint height = 720, uint frames = 0) => new()
    {
        Offscreen = true,
        Width = width,
        Height = height,
        HeadlessFps = 60,
        HeadlessFrames = frames,
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
    public override string ToString() => (Headless
        ? $"Config(headless, fps={HeadlessFps}, frames={HeadlessFrames}"
        : Offscreen
            ? $"Config(offscreen {Width}x{Height}, fps={HeadlessFps}, frames={HeadlessFrames}"
            : $"Config('{Title}', {Width}x{Height}, vsync={Vsync}, backend={Backend}")
        + (Serve ? ", serving)" : ")");
}
