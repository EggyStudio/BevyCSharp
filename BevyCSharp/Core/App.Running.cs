using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;

namespace Bevy;

public sealed unsafe partial class App : IDisposable
{
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
        // crashes inside AppKit rather than anywhere that points back here. The constraint belongs
        // to windowing, not to the engine, because a headless run creates no window and no event
        // loop, so it is free to run anywhere, and a test runner can drive it from its own worker
        // threads. The bridge answers yes on every platform but Apple, so this costs one call and
        // only ever fires where it genuinely matters.
        if (WillOpenWindow && Native.bcs_is_main_thread() == 0)
            throw new InvalidOperationException(
                "App.Run must be called from the process main thread when it opens a window. "
                + "macOS requires the window event loop to own the main thread, so starting a "
                + "windowed app from a task or a background thread crashes inside the platform "
                + "layer. Set Config.Headless to run the same behaviors without a window.");

        // The states a script declared on its enum and a system of this app uses, added as the
        // last thing before running, once every system that could use one has registered.
        foreach (var (state, add) in StateRegistry.DeclaredFor(_addedStates))
        {
            if (!_addedStates.Contains(state)) add(this);
        }

        ApplyOrder();

        IsRunning = true;
        Running = this;
        ComponentRegistry.EnterRunning();
        try
        {
            return Native.bcs_app_run(_handle);
        }
        finally
        {
            ComponentRegistry.ExitRunning();
            ReportThrownTotals();
            EngineLog.TakeBevys(this);
            Running = null;
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
    /// initialized, so <see cref="Stage.Startup"/> at the earliest.
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
    /// <remarks>
    /// A system that throws in every frame wrote its whole trace sixty times a second, into a
    /// player's log as into a test's, so the first of each type a system throws is logged whole, and
    /// after it a line at the 10th, the 100th, the 1,000th and so on, and how many in all as the run
    /// ends, as 3DEngine's schedule does.
    /// </remarks>
    internal void OnSystemException(SystemDescriptor descriptor, Exception exception)
    {
        long count;
        lock (_thrown)
        {
            var key = (descriptor.Name, exception.GetType());
            _thrown.TryGetValue(key, out count);
            _thrown[key] = ++count;
        }

        if (count == 1)
        {
            EngineLog.Error(this, "system",
                $"[BevyCSharp] System '{descriptor.Name}' threw {exception.GetType().Name}: "
                + $"{exception.Message}{Environment.NewLine}{exception.StackTrace}", exception);
        }
        else if (IsPowerOfTen(count))
        {
            EngineLog.Error(this, "system",
                $"[BevyCSharp] System '{descriptor.Name}' has thrown {exception.GetType().Name} {count} times, the last: {exception.Message}",
                exception);
        }

        if (!Config.FailFastOnSystemException) return;

        // Rethrowing here would unwind into Rust, so stop the loop instead and let Run return.
        try
        {
            // Discarded on purpose. The loop is being stopped because a system already threw, and
            // there is nothing left to do about a request to stop that the bridge refuses.
            _ = Native.bcs_app_request_exit();
        }
        catch (Exception)
        {
            // The app is already tearing down; nothing useful left to do.
        }
    }

    /// <summary>How often each system has thrown each type of exception, in this app's run.</summary>
    private readonly Dictionary<(string System, Type Exception), long> _thrown = [];

    private static bool IsPowerOfTen(long count)
    {
        while (count >= 10 && count % 10 == 0) count /= 10;
        return count == 1;
    }

    /// <summary>Logs, for each system that threw the same type more than once, how many times in all.</summary>
    private void ReportThrownTotals()
    {
        KeyValuePair<(string System, Type Exception), long>[] thrown;
        lock (_thrown) thrown = [.. _thrown.Where(entry => entry.Value > 1)];

        foreach (var ((system, type), count) in thrown)
            EngineLog.Error(this, "system", $"[BevyCSharp] System '{system}' threw {type.Name} {count} times in all");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var system in _systems) system.Dispose();
        _systems.Clear();

        if (World.TryGetResource<EcsWorld>(out var ecs)) ecs.ForgetObservers();
        World.Dispose();

        // The rules this app's computed states were worked out by, and what they captured.
        ComputedRules.Forget();

        if (_handle != IntPtr.Zero)
        {
            Native.bcs_app_destroy(_handle);
            _handle = IntPtr.Zero;

            // What Bevy said as the app came down, laid to it.
            EngineLog.TakeBevys(this);
        }

        ComponentRegistry.EndApp();
    }

    /// <summary>
    /// A registered system and the pinned handle Bevy calls back through.
    /// </summary>
    /// <remarks>
    /// The native side stores a raw function pointer plus an opaque <c>user</c> word. A normal GC
    /// handle turns that word back into a managed object; it is pinned for the life of the app
    /// because Bevy's schedule holds the pointer for exactly that long.
    /// </remarks>
    private sealed class RegisteredSystem : IDisposable
    {
        private GCHandle _handle;

        internal App Owner { get; }
        internal SystemDescriptor Descriptor { get; }
        internal Stage Stage { get; }
        internal bool IsRemoved { get; set; }
        internal IntPtr UserData => GCHandle.ToIntPtr(_handle);

        /// <summary>The number the bridge gave it, which orders it, or -1 for one Bevy does not schedule as a stage's.</summary>
        internal int NativeId { get; set; } = -1;

        internal RegisteredSystem(App owner, SystemDescriptor descriptor, Stage stage)
        {
            Owner = owner;
            Descriptor = descriptor;
            Stage = stage;
            _handle = GCHandle.Alloc(this, GCHandleType.Normal);
        }

        /// <summary>
        /// The one entry point Bevy calls. Nothing may escape it, because an exception crossing
        /// back into Rust is undefined behavior, so everything is caught and reported here.
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
                        EngineLog.Error(null, "system", $"[BevyCSharp] System callback failed: {ex}", ex);
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
