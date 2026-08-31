using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// The engine handle: build it up with plugins and systems, then <see cref="Run"/> it.
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
public sealed unsafe class App : IDisposable
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

    /// <summary>Number of frames completed, mirrored from Bevy.</summary>
    public ulong FrameCount => World.TryGetResource<Time>(out var time) ? time.FrameCount : 0;

    /// <summary>Plugin types registered on this app.</summary>
    public IReadOnlyCollection<Type> Plugins => _plugins.Keys.ToArray();

    /// <summary>Number of registered plugins.</summary>
    public int PluginCount => _plugins.Count;

    /// <summary>Number of registered systems, across every stage.</summary>
    public int SystemCount => _systems.Count;

    /// <summary>True when the loaded native bridge has Bevy's renderer compiled in.</summary>
    public static bool HasRenderer => Native.bcs_has_render() != 0;

    /// <summary>
    /// True when running this app will actually create a window.
    /// </summary>
    /// <remarks>
    /// Both halves matter. Asking for a window on a bridge with no renderer compiled in gets a
    /// headless run instead, so the config alone does not settle it.
    /// </remarks>
    public bool WillOpenWindow => !Config.Headless && HasRenderer;

    /// <summary>Creates the engine and its native Bevy app.</summary>
    /// <param name="config">Startup configuration; <see cref="Config.Default"/> when omitted.</param>
    /// <exception cref="BevyNativeException">The native app could not be created.</exception>
    public App(Config? config = null)
    {
        Config = config ?? Config.Default;

        var titleBytes = Encoding.UTF8.GetBytes(Config.Title + "\0");
        var assetRootBytes = Config.AssetRoot is null
            ? null
            : Encoding.UTF8.GetBytes(Config.AssetRoot + "\0");

        fixed (byte* title = titleBytes)
        fixed (byte* assetRoot = assetRootBytes)
        {
            var native = new NativeConfig
            {
                Title = title,
                Width = Config.Width,
                Height = Config.Height,
                Vsync = Config.Vsync ? 1u : 0u,
                Headless = Config.Headless ? 1u : 0u,
                HeadlessFps = Config.HeadlessFps,
                HeadlessFrames = Config.HeadlessFrames,
                Backend = (uint)Config.Backend,
                FixedHz = Config.FixedHz,
                AssetRoot = assetRoot,
            };
            _handle = Native.bcs_app_create(&native);
        }

        if (_handle == IntPtr.Zero) throw CreationFailed();

        ComponentRegistry.BeginApp(_handle);

        World.InsertResource(Config);
        World.InsertResource(new Time());
        World.InsertResource(new Input());
        World.InsertResource(new EcsWorld());
        World.InsertResource(new EcsCommands());
        World.InsertResource(new MessageBus());

        RegisterEngineSystems();
    }

    /// <summary>Explains, as specifically as possible, why the engine would not start.</summary>
    private BevyNativeException CreationFailed()
    {
        if (!Config.Headless && !HasRenderer)
            return new BevyNativeException(
                NativeStatus.InvalidState,
                "The native Bevy bridge has no renderer compiled in, so it cannot open a window. "
                + "Set Config.Headless, or rebuild the bridge with build/build-native.sh --render.");

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

            // Swapped here so the whole frame reads one complete, unchanging set.
            world.Resource<MessageBus>().Swap();
        }, "Engine.FrameSync"));

        // Apply everything queued during the frame, after all user PostUpdate work.
        AddSystem(Stage.CommandFlush, new SystemDescriptor(static world =>
        {
            world.Resource<EcsCommands>().Apply(world.Resource<EcsWorld>());
        }, "Engine.CommandFlush"));
    }

    // -- System registration

    /// <summary>Registers a system function in <paramref name="stage"/>.</summary>
    public App AddSystem(Stage stage, SystemFn system) =>
        AddSystem(stage, new SystemDescriptor(system));

    /// <summary>Registers a system function with a run condition.</summary>
    public App AddSystem(Stage stage, SystemFn system, Func<World, bool> runCondition) =>
        AddSystem(stage, new SystemDescriptor(system).RunIf(runCondition));

    /// <summary>Registers a described system in <paramref name="stage"/>.</summary>
    /// <exception cref="InvalidOperationException">The app is already running.</exception>
    public App AddSystem(Stage stage, SystemDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsRunning)
            throw new InvalidOperationException(
                $"Cannot register system '{descriptor.Name}': the app is already running. "
                + "Register systems from a plugin's Build method or before calling Run.");

        descriptor.Source ??= SystemRegistrationSourceScope.Current;

        var registration = new RegisteredSystem(this, descriptor, stage);
        _systems.Add(registration);

        Native.Check(
            Native.bcs_app_add_system(
                _handle,
                (int)stage,
                &RegisteredSystem.Trampoline,
                registration.UserData),
            $"registering system '{descriptor.Name}'");

        return this;
    }

    /// <summary>
    /// Removes every system tagged with <paramref name="source"/>.
    /// </summary>
    /// <remarks>
    /// Bevy has no API for pulling a system back out of a built schedule, so the descriptors
    /// stay registered and are neutered instead: a removed system's callback returns
    /// immediately. That keeps hot-reload swapping generations correctly at the cost of an
    /// empty call per removed system per frame.
    /// </remarks>
    /// <returns>How many systems were removed.</returns>
    public int RemoveSystemsBySource(string source)
    {
        var removed = 0;
        foreach (var system in _systems)
        {
            if (system.Descriptor.Source != source || system.IsRemoved) continue;
            system.IsRemoved = true;
            removed++;
        }

        return removed;
    }

    /// <summary>The descriptors registered for <paramref name="stage"/>, in registration order.</summary>
    public IReadOnlyList<SystemDescriptor> SystemsIn(Stage stage) =>
        _systems.Where(s => s.Stage == stage && !s.IsRemoved)
            .Select(s => s.Descriptor)
            .ToArray();

    // -- States

    /// <summary>
    /// Adds a state machine over <typeparamref name="TState"/>, starting at
    /// <paramref name="initial"/>.
    /// </summary>
    /// <remarks>
    /// Before the run, because adding a state also adds the systems that apply its transitions,
    /// and a schedule cannot be added to once the loop owns it. Adding the same enum twice keeps
    /// the first slot and re-inserts the initial value.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// The app is already running, or every state slot is taken.
    /// </exception>
    public App AddState<TState>(TState initial) where TState : struct, Enum
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsRunning)
            throw new InvalidOperationException(
                $"Cannot add state {typeof(TState).Name}: the app is already running. Add states "
                + "from a plugin's Build method or before calling Run.");

        Native.Check(
            Native.bcs_state_add(_handle, StateRegistry.Claim<TState>(), StateRegistry.ToInt(initial)),
            $"adding state {typeof(TState).Name}");

        return this;
    }

    /// <summary>The current value of <typeparamref name="TState"/>. Only valid inside a system.</summary>
    public static TState State<TState>() where TState : struct, Enum =>
        StateRegistry.Current<TState>();

    /// <summary>Queues a transition of <typeparamref name="TState"/>. Only valid inside a system.</summary>
    public static void SetState<TState>(TState value) where TState : struct, Enum =>
        StateRegistry.Set(value);

    // -- Plugins

    /// <summary>Adds a plugin, building it immediately. Adding the same type twice is a no-op.</summary>
    /// <exception cref="PluginOrderException">A declared dependency is not yet registered.</exception>
    public App AddPlugin(IPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var type = plugin.GetType();
        if (_plugins.ContainsKey(type)) return this;

        foreach (var dependency in plugin.Dependencies)
            if (!_plugins.ContainsKey(dependency))
                throw new PluginOrderException(type.Name, dependency.Name);

        _plugins[type] = plugin;
        plugin.Build(this);
        return this;
    }

    /// <summary>Adds every plugin in a group, in <c>Order</c> order.</summary>
    public App AddPlugins(IPluginGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        foreach (var (plugin, _) in group.GetPlugins().OrderBy(p => p.Order))
            AddPlugin(plugin);

        return this;
    }

    /// <summary>True when a plugin of type <typeparamref name="T"/> is registered.</summary>
    public bool HasPlugin<T>() where T : IPlugin => _plugins.ContainsKey(typeof(T));

    // -- Execution

    /// <summary>
    /// Runs the engine. Blocks until the window closes or <see cref="RequestExit"/> is called,
    /// then runs the <see cref="Stage.Cleanup"/> systems.
    /// </summary>
    /// <returns>The process exit code Bevy reported; 0 for a clean shutdown.</returns>
    public int Run()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsRunning)
            throw new InvalidOperationException("This app has already been run.");

        // macOS insists the *window* event loop owns the main thread, and breaking that rule
        // crashes inside AppKit rather than anywhere that points back here. The constraint
        // belongs to windowing, not to the engine: a headless run creates no window and no
        // event loop, so it is free to run anywhere, which is what lets a test runner drive it
        // from its own worker threads. The bridge answers yes on every platform but Apple, so
        // this costs one call and only ever fires where it genuinely matters.
        if (WillOpenWindow && Native.bcs_is_main_thread() == 0)
            throw new InvalidOperationException(
                "App.Run must be called from the process main thread when it opens a window. "
                + "macOS requires the window event loop to own the main thread, so starting a "
                + "windowed app from a task or a background thread crashes inside the platform "
                + "layer. Set Config.Headless to run the same behaviors without a window.");

        IsRunning = true;
        ComponentRegistry.EnterRunning();
        try
        {
            return Native.bcs_app_run(_handle);
        }
        finally
        {
            ComponentRegistry.ExitRunning();
        }
    }

    /// <summary>Asks the engine to shut down after the current frame.</summary>
    public static void RequestExit() =>
        Native.Check(Native.bcs_app_request_exit(), "bcs_app_request_exit");

    /// <summary>
    /// Describes the graphics adapter the renderer actually chose, or <see langword="null"/> in
    /// a headless run.
    /// </summary>
    /// <remarks>
    /// This is how you confirm which backend you really got. Asking for
    /// <see cref="GraphicsBackend.Vulkan"/> and reading "Vulkan | ..." back is the difference
    /// between believing and knowing. Only valid from inside a system, once the renderer has
    /// initialised, so <see cref="Stage.Startup"/> at the earliest.
    /// </remarks>
    public static string? DescribeAdapter()
    {
        var needed = Native.bcs_render_adapter(null, 0);
        if (needed <= 0) return null;

        var buffer = new byte[needed];
        fixed (byte* target = buffer)
        {
            if (Native.bcs_render_adapter(target, needed) != needed) return null;
        }

        return Encoding.UTF8.GetString(buffer);
    }

    /// <summary>Handles an exception that escaped a system, per <see cref="Config"/>.</summary>
    internal void OnSystemException(SystemDescriptor descriptor, Exception exception)
    {
        Console.Error.WriteLine(
            $"[BevyCSharp] System '{descriptor.Name}' threw {exception.GetType().Name}: "
            + $"{exception.Message}{Environment.NewLine}{exception.StackTrace}");

        if (!Config.FailFastOnSystemException) return;

        // Rethrowing here would unwind into Rust, so stop the loop instead and let Run return.
        try
        {
            Native.bcs_app_request_exit();
        }
        catch (Exception)
        {
            // The app is already tearing down; nothing useful left to do.
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var system in _systems) system.Dispose();
        _systems.Clear();

        World.Dispose();

        if (_handle != IntPtr.Zero)
        {
            Native.bcs_app_destroy(_handle);
            _handle = IntPtr.Zero;
        }

        ComponentRegistry.EndApp();
    }

    /// <summary>
    /// A registered system and the pinned handle Bevy calls back through.
    /// </summary>
    /// <remarks>
    /// The native side stores a raw function pointer plus an opaque <c>user</c> word. A normal
    /// GC handle is what turns that word back into a managed object; it is pinned for the life
    /// of the app because Bevy's schedule holds the pointer for exactly that long.
    /// </remarks>
    private sealed class RegisteredSystem : IDisposable
    {
        private GCHandle _handle;

        internal App Owner { get; }
        internal SystemDescriptor Descriptor { get; }
        internal Stage Stage { get; }
        internal bool IsRemoved { get; set; }
        internal IntPtr UserData => GCHandle.ToIntPtr(_handle);

        internal RegisteredSystem(App owner, SystemDescriptor descriptor, Stage stage)
        {
            Owner = owner;
            Descriptor = descriptor;
            Stage = stage;
            _handle = GCHandle.Alloc(this, GCHandleType.Normal);
        }

        /// <summary>
        /// The one entry point Bevy calls. Nothing may escape it: an exception crossing back
        /// into Rust is undefined behavior, so everything is caught and reported here.
        /// </summary>
        [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        internal static void Trampoline(IntPtr user)
        {
            try
            {
                if (GCHandle.FromIntPtr(user).Target is not RegisteredSystem system) return;
                if (system.IsRemoved) return;
                system.Descriptor.Invoke(system.Owner.World);
            }
            catch (Exception ex)
            {
                try
                {
                    if (GCHandle.FromIntPtr(user).Target is RegisteredSystem system)
                        system.Owner.OnSystemException(system.Descriptor, ex);
                    else
                        Console.Error.WriteLine($"[BevyCSharp] System callback failed: {ex}");
                }
                catch (Exception)
                {
                    // Reporting must never throw across the boundary either.
                }
            }
        }

        public void Dispose()
        {
            if (_handle.IsAllocated) _handle.Free();
        }
    }
}
