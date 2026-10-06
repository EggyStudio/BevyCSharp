using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// The engine handle, built up with plugins and systems and then started with <see cref="Run"/>.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="App"/> owns a live Bevy app on the native side from construction onwards.
/// Systems and component types registered before <see cref="Run"/> go through the app handle;
/// once the loop is running, registration has to go through the world Bevy loans to the
/// active system instead, and <see cref="App"/> switches routes automatically.
/// </para>
/// <para>
/// In the common case you never touch this directly <see cref="BevyApp.Run(Config?)"/> wires
/// up the defaults and discovers every <c>[Behavior]</c> struct in your assemblies.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var app = new App(Config.Default);
/// app.AddPlugins(new DefaultPlugins());
/// app.Run();
/// </code>
/// </example>
public sealed unsafe partial class App : IDisposable
{
    private readonly Dictionary<Type, IPlugin> _plugins = [];
    private readonly List<RegisteredSystem> _systems = [];
    private IntPtr _handle;
    private bool _disposed;

    /// <summary>The managed resource world.</summary>
    public World World { get; } = new();

    /// <summary>The configuration this app was created with.</summary>
    public Config Config { get; }

    /// <summary>True once <see cref="Run"/> has been entered.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>The app whose loop is running, or none, one at a time as the bridge runs them.</summary>
    internal static App? Running { get; private set; }

    /// <summary>Raised as an app is made, before anything it does can log an error.</summary>
    /// <remarks>
    /// What lays an error to a test, which learns here which apps its test made, on the test's own
    /// flow, where an error from the app's loop or Bevy's threads cannot say whose it is.
    /// </remarks>
    internal static event Action<App>? Created;

    /// <summary>Number of frames completed, mirrored from Bevy.</summary>
    public ulong FrameCount => World.TryGetResource<Time>(out var time) ? time.FrameCount : 0;

    /// <summary>Plugin types registered on this app.</summary>
    public IReadOnlyCollection<Type> Plugins => _plugins.Keys.ToArray();

    /// <summary>Number of registered plugins.</summary>
    public int PluginCount => _plugins.Count;

    /// <summary>Number of registered systems, across every stage.</summary>
    public int SystemCount => _systems.Count;

    /// <summary>
    /// The bridge version this build of the library was written against.
    /// </summary>
    /// <remarks>
    /// Checked against what the loaded bridge reports the first time anything touches it, and a
    /// mismatch is refused there rather than crashing later. Worth reporting by a tool, because a
    /// bridge that is one rebuild behind is the usual reason an app will not start at all.
    /// </remarks>
    public static int AbiVersion => Native.ExpectedAbiVersion;

    /// <summary>True when the loaded native bridge has Bevy's renderer compiled in.</summary>
    public static bool HasRenderer => Native.bcs_has_render() != 0;

    /// <summary>True when the loaded native bridge has the HTML and CSS UI compiled in.</summary>
    /// <remarks>
    /// A separate question from <see cref="HasRenderer"/>. The editor profile is a superset of the
    /// render one, so a bridge can draw a scene without carrying the document surface, and a panel
    /// opened against one that does not is refused rather than ignored.
    /// </remarks>
    public static bool HasEditor => Native.bcs_has_editor() != 0;

    /// <summary>True when the loaded native bridge carries a game's assets compiled in.</summary>
    /// <remarks>
    /// A bridge built with <c>build-native.sh --embed</c> reads every asset Bevy loads from the
    /// folder it was built with, whatever <see cref="Config.AssetRoot"/> says. What the managed
    /// side reads for itself, such as a scene file or a data asset, still comes from the asset
    /// root on disk, so a game shipped this way ships that folder too until the managed reads go
    /// through the same bytes.
    /// </remarks>
    public static bool HasEmbeddedAssets => Native.bcs_has_embedded_assets() != 0;

    /// <summary>True when the running app installed the interface.</summary>
    /// <remarks>
    /// The third of the three questions, and the one an app drawing an interface actually needs
    /// answered. <see cref="HasRenderer"/> and <see cref="HasEditor"/> report what the bridge was
    /// built with; this reports what this run asked for, which is <see cref="Config.Gui"/>. A
    /// bridge carrying the surface still draws nothing without it, and telling those apart is the
    /// difference between rebuilding the bridge and setting a property.
    /// </remarks>
    public static bool HasInterface => Native.bcs_has_interface() != 0;

    /// <summary>
    /// True when running this app will actually create a window.
    /// </summary>
    /// <remarks>
    /// Both halves matter. Asking for a window on a bridge with no renderer compiled in gets a
    /// headless run instead, so the config alone does not settle it.
    /// </remarks>
    public bool WillOpenWindow => !Config.Headless && !Config.Offscreen && HasRenderer;

    /// <summary>
    /// What the project says about itself, read from <see cref="ProjectSettings.FileName"/> in the
    /// assets as the app is created, every setting at its default where there is no such file.
    /// </summary>
    /// <remarks>
    /// Its fixed step is taken where <see cref="Config.FixedHz"/> is zero. A game reads the rest,
    /// such as <see cref="ProjectSettings.StartupScene"/> to know what to load first.
    /// </remarks>
    public ProjectSettings Project { get; } = new();

    /// <summary>The states, sub-states and computed states this app added, by their enum.</summary>
    private readonly HashSet<Type> _addedStates = new(SameState.Instance);

    /// <summary>Creates the engine and its native Bevy app.</summary>
    /// <param name="config">Startup configuration; <see cref="Config.Default"/> when omitted.</param>
    /// <exception cref="BevyNativeException">The native app could not be created.</exception>
    public App(Config? config = null)
    {
        Config = config ?? Config.Default;
        Created?.Invoke(this);

        // A window asked from outside to be an image instead, as the editor's Play asks when the
        // editor has no window either, so a game's own Main need not take an option for it.
        if (Config.OffscreenAsked && !Config.Headless && !Config.Offscreen)
        {
            Config.Offscreen = true;
            if (Config.HeadlessFps == 0) Config.HeadlessFps = 60;
        }

        if ((Config.Offscreen || Config.Headless) && Config.HeadlessFrames == 0 && Config.FramesAsked > 0)
            Config.HeadlessFrames = Config.FramesAsked;

        var titleBytes = Encoding.UTF8.GetBytes(Config.Title + "\0");
        var assetRootBytes = Config.AssetRoot is null
            ? null
            : Encoding.UTF8.GetBytes(Config.AssetRoot + "\0");

        // The player's directory, made before Bevy is told of it so the source it builds over the
        // directory finds one there. Named first, so a game's files go under its own name.
        UserData.Name = Config.GameName ?? Config.Title;
        var userRootBytes = Encoding.UTF8.GetBytes(UserData.Root + "\0");
        try
        {
            Directory.CreateDirectory(UserData.Root);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // A directory that cannot be made leaves user:// reads failing as a missing file does,
            // which is no reason to refuse to start.
        }

        // The files the game's assembly carries, found before the native app is built and handed to
        // the bridge, since Bevy builds its asset sources as the app is built and never again.
        AssetFiles.Use(Config.AssetAssembly ?? System.Reflection.Assembly.GetEntryAssembly(), OpenPack(Config));
        AssetFiles.Serve();

        // Any folder named as a source of its own, forgetting the last app's first.
        Native.Check(Native.bcs_asset_source_add(null, null), "forgetting the last app's asset sources");
        foreach (var (name, folder) in Config.AssetSources)
            Native.Check(Native.bcs_asset_source_add(name, Path.GetFullPath(folder)), $"naming {folder} as the asset source {name}");

        // Where Bevy reads assets from, and where a streamed read's path starts too, is the
        // directory asked for, or `assets` beside the executable, which is Bevy's own default.
        // Known before the native app is, so the project's settings can be read from there first.
        Streaming.AssetRoot = string.IsNullOrEmpty(Config.AssetRoot)
            ? Path.Combine(AppContext.BaseDirectory, "assets")
            : Path.GetFullPath(Config.AssetRoot);

        // What the project says about itself, whose fixed step stands where the config left it
        // to Bevy. A project file that does not read is a broken install, which is said once and
        // run without rather than refused, since every setting in it has a default.
        try
        {
            Project = ProjectSettings.Read();
        }
        catch (InvalidDataException error)
        {
            EngineLog.Error(this, "project", $"[BevyCSharp] {ProjectSettings.FileName} was not read: {error.Message}", error);
        }

        var fixedHz = Config.FixedHz > 0 ? Config.FixedHz : Project.FixedHz;

        // Where the window was left, read before it opens, so it opens there.
        var opening = WindowMemory.Open(Config);

        fixed (byte* title = titleBytes)
        fixed (byte* assetRoot = assetRootBytes)
        fixed (byte* userRoot = userRootBytes)
        {
            var native = new NativeConfig
            {
                Title = title,
                Width = opening.Width,
                Height = opening.Height,
                Vsync = Config.Vsync ? 1u : 0u,
                Headless = Config.Headless ? 1u : 0u,
                HeadlessFps = Config.HeadlessFps,
                HeadlessFrames = Config.HeadlessFrames,
                Backend = (uint)Config.Backend,
                FixedHz = fixedHz,
                AssetRoot = assetRoot,
                WatchAssets = Config.WatchAssets ? 1u : 0u,
                Gui = Config.Gui ? 1u : 0u,
                Offscreen = Config.Offscreen ? 1u : 0u,
                SpatialScale = Config.SpatialScale,
                MeshletClusters = Config.MeshletClusters,
                GpuTimings = Config.GpuTimings ? 1u : 0u,
                RayTracedLighting = Config.RayTracedLighting ? 1u : 0u,
                Transparent = Config.Transparent ? 1u : 0u,
                DesktopTitleBar = Config.DesktopTitleBar ? 1u : 0u,
                UserRoot = userRoot,
                HasPosition = opening.HasPosition ? 1u : 0u,
                X = opening.X,
                Y = opening.Y,
                Wireframes = Config.Wireframes ? 1u : 0u,
                LogFrameTimes = Config.LogFrameTimes ? 1u : 0u,
                ScaleFactor = Config.ScaleFactor,
            };
            _handle = Native.bcs_app_create(&native);
        }

        if (_handle == IntPtr.Zero) throw CreationFailed();

        ComponentRegistry.BeginApp(_handle);

        // The ids the scenes, data assets and other files the managed side reads carry, from the
        // folder or, for a game that compiled them into itself, from its own assembly.
        AssetIds.Reindex();

        // A data file changed on disk is read again when assets are, and not in a shipped game.
        DataAssets.Watching = Config.WatchAssets;

        // Sounds and buses belong to the app that played them, and meshes to the app that made them.
        Audio.ResetMixer();
        Render.ForgetMade();
        Render.Wireframes = Config.Wireframes;
        MaterialFiles.Forget();
        MeshFiles.Forget();
        Bevy.Physics.Colliders.Forget();

        // A game in progress is one app's, and its entities would name others in the next.
        SaveGame.Forget();

        World.InsertResource(Config);
        World.InsertResource(Project);
        World.InsertResource(new Time());
        World.InsertResource(new Input());
        World.InsertResource(new EcsWorld { Owner = World });
        World.InsertResource(new EcsCommands());
        World.InsertResource(new MessageBus());

        RegisterEngineSystems();
    }

    /// <summary>
    /// The pack <see cref="Config.AssetPack"/> names, or the one beside the executable, or nothing.
    /// </summary>
    /// <exception cref="InvalidDataException">The pack named cannot be read as one.</exception>
    /// <exception cref="FileNotFoundException">The pack named is not there.</exception>
    private static AssetPack? OpenPack(Config config)
    {
        if (config.AssetPack is { Length: 0 }) return null;

        if (config.AssetPack is { } named)
        {
            var full = Path.GetFullPath(named);
            if (!File.Exists(full)) throw new FileNotFoundException($"No asset pack at {full}.", full);

            return AssetPack.Open(full);
        }

        // Beside the executable, which is where an export puts it. One there that does not open is
        // as much a broken install as one named outright, so it fails the same way.
        var beside = Path.Combine(AppContext.BaseDirectory, AssetPack.DefaultName);
        return File.Exists(beside) ? AssetPack.Open(beside) : null;
    }

    /// <summary>Explains, as specifically as possible, why the engine would not start.</summary>
    private BevyNativeException CreationFailed()
    {
        if (!Config.Headless && !HasRenderer)
            return new BevyNativeException(
                NativeStatus.InvalidState,
                "The native Bevy bridge has no renderer compiled in, so it can neither open a "
                + "window nor draw into an image. Set Config.Headless to run the behaviors without "
                + "one, or rebuild the bridge with build/build-native.sh --render.");

        if (Config.Backend != GraphicsBackend.Automatic)
            return new BevyNativeException(
                NativeStatus.InvalidState,
                $"The renderer could not start on {Config.Backend}. That backend was requested "
                + "explicitly, so nothing else was tried. This machine may have no driver for "
                + "it. Set Config.Backend to GraphicsBackend.Automatic to let wgpu choose, or "
                + "see stderr for the error the renderer reported.");

        return new BevyNativeException(
            NativeStatus.InvalidState,
            "The native Bevy bridge failed to create an app. See stderr for the error it "
            + "reported during startup.");
    }

    /// <summary>Wires the two internal systems that bracket every frame.</summary>
    private void RegisterEngineSystems()
    {
        // Refresh Time and Input from Bevy before any user system observes them.
        AddSystem(Stage.FrameSync, new SystemDescriptor(static world =>
        {
            NativeFrameState state;
            Native.Check(Native.bcs_frame_state(&state), "bcs_frame_state");
            world.Resource<Time>().Update(state.Time);
            world.Resource<Input>().Update(state.Input);
            world.Resource<Input>().UpdateGamepads(world.Resource<MessageBus>());

            // A new frame's worth of streamed bytes to hand over.
            Streaming.BeginFrame();

            // Posted before the swap, so what the window reported at the top of this frame is
            // readable during it rather than during the next one.
            PostWindowMessages(world.Resource<MessageBus>());
            PostFileDrops(world.Resource<MessageBus>());
            PostIme(world.Resource<MessageBus>());
            PostAssetFailures(world.Resource<MessageBus>());
            if (HasRenderer) Animation.PostFinished(world.Resource<MessageBus>());
            DataAssets.PostChanges(world.Resource<MessageBus>());
            SaveGame.PostLoaded(world.Resource<MessageBus>());
            MaterialFiles.ReloadTouched();
            WindowMemory.Tick();

            // After the scenes Bevy spawned last frame are in the world, so an instance's overrides
            // find their nodes, and before anything reads that the instance is ready.
            SceneInstances.PostReady(world.Resource<EcsWorld>(), world.Resource<MessageBus>());

            // What Bevy logged as an error since the last frame, heard while the run goes on
            // rather than only once it ends.
            if (Running is { } app) EngineLog.TakeBevys(app);

            // Swapped here so the whole frame reads one complete, unchanging set.
            world.Resource<MessageBus>().Swap();
        }, "Engine.FrameSync"));

        // Apply everything queued during the frame, after all user PostUpdate work.
        AddSystem(Stage.CommandFlush, new SystemDescriptor(static world =>
        {
            world.Resource<EcsCommands>().Apply(world.Resource<EcsWorld>());
        }, "Engine.CommandFlush"));
    }

    /// <summary>
    /// Moves what the window reported onto the message bus, as ordinary messages.
    /// </summary>
    /// <remarks>
    /// Bevy reports these as buffered messages read through a cursor, which a C# system cannot
    /// hold. Draining them here and posting them to the bus means a reader uses the same
    /// <c>ctx.Read</c> for an engine message as for one another system sent.
    /// </remarks>
    private static void PostWindowMessages(MessageBus bus)
    {
        // Sized for a frame's worth. A burst larger than this is not lost. The bridge leaves the
        // rest queued and hands them over on the next call.
        const int Capacity = 16;

        NativeWindowEvent* buffer = stackalloc NativeWindowEvent[Capacity];
        var count = Native.bcs_window_events(buffer, Capacity);
        if (count <= 0) return;

        for (var i = 0; i < count; i++)
        {
            var e = buffer[i];
            switch (e.Kind)
            {
                case 0:
                    bus.Send(new WindowResized(e.A, e.B));
                    break;
                case 1:
                    bus.Send(new WindowFocusChanged(e.A != 0f));
                    break;
                case 2:
                    bus.Send(new WindowCloseRequested());
                    break;
                case 3:
                    bus.Send(new WindowScaleFactorChanged(e.A));
                    break;
                case 4:
                    bus.Send(new CursorEntered());
                    break;
                case 5:
                    bus.Send(new CursorLeft());
                    break;
            }
        }
    }

    /// <summary>Moves what was dropped on the window onto the message bus.</summary>
    /// <remarks>
    /// Separate from the other window messages because each path is text, which crosses the
    /// boundary one call at a time. The drain reports how many there are, then each is read by
    /// index.
    /// </remarks>
    private static void PostFileDrops(MessageBus bus)
    {
        var count = Native.bcs_file_drops_drain();
        if (count <= 0) return;

        for (var i = 0; i < count; i++)
        {
            var index = i;

            // A one-element array rather than a local, because a lambda cannot take the address
            // of a local but can pin an array inside itself.
            var kind = new int[1];
            var path = Native.ReadText(
                (buffer, capacity) =>
                {
                    fixed (int* target = kind)
                        return Native.bcs_file_drop_path(index, target, buffer, capacity);
                },
                "reading a dropped file's path");

            switch (kind[0])
            {
                case 0:
                    bus.Send(new FileDropped(path));
                    break;
                case 1:
                    bus.Send(new FileHovered(path));
                    break;
                case 2:
                    bus.Send(new FileHoverCanceled());
                    break;
            }
        }
    }

    /// <summary>Moves what the platform's input method said onto the message bus.</summary>
    /// <remarks>The same drain and read by index as the dropped files, since each carries text.</remarks>
    private static void PostIme(MessageBus bus)
    {
        var count = Native.bcs_ime_drain();
        if (count <= 0) return;

        for (var i = 0; i < count; i++)
        {
            var index = i;

            // Arrays rather than locals, because a lambda cannot take the address of a local but
            // can pin an array inside itself.
            var kind = new int[1];
            var caret = new int[2];
            var text = Native.ReadText(
                (buffer, capacity) =>
                {
                    fixed (int* kindAt = kind)
                    fixed (int* caretAt = caret)
                        return Native.bcs_ime_read(index, kindAt, caretAt, buffer, capacity);
                },
                "reading what the input method said");

            switch (kind[0])
            {
                case 0:
                    bus.Send(new ImeComposing(text, caret[0], caret[1]));
                    break;
                case 1:
                    bus.Send(new ImeCommit(text));
                    break;
                case 2:
                    bus.Send(new ImeEnabled());
                    break;
                case 3:
                    bus.Send(new ImeDisabled());
                    break;
            }
        }
    }

    /// <summary>
    /// Moves the assets that failed to load onto the message bus.
    /// </summary>
    /// <remarks>
    /// Three texts per failure, each read the way every text crosses the boundary, which is to ask
    /// with nothing to learn the length and then ask again with a buffer that size. Failures are
    /// rare, so the reads cost nothing and a frame with none costs one call that answers zero.
    /// </remarks>
    private static void PostAssetFailures(MessageBus bus)
    {
        var count = Native.bcs_asset_failures_drain();
        if (count <= 0) return;

        for (var i = 0; i < count; i++)
        {
            var index = i;

            var path = Native.ReadText(
                (buffer, capacity) => Native.bcs_asset_failure_path(index, buffer, capacity),
                "reading the path of an asset that failed to load");

            var reason = Native.ReadText(
                (buffer, capacity) => Native.bcs_asset_failure_reason(index, buffer, capacity),
                "reading why an asset failed to load");

            // What kind it was, which the path alone does not say once a loader is picked by
            // content rather than by extension.
            var kind = Native.ReadText(
                (buffer, capacity) => Native.bcs_asset_failure_kind(index, buffer, capacity),
                "reading what kind of asset failed to load");

            bus.Send(new AssetLoadFailed(path, reason, kind));
        }
    }
}
