using System.Runtime.CompilerServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Maps C# struct types onto Bevy <c>ComponentId</c>s.
/// </summary>
/// <remarks>
/// <para>
/// Bevy normally learns component layouts at compile time from Rust types. C# types are not
/// available to it, so each blittable struct is registered at runtime with its size and
/// alignment, and Bevy hands back a real <c>ComponentId</c>. From that point the struct is an
/// ordinary Bevy component. It lives in tables, participates in archetypes, and is visible to
/// Bevy's own change detection.
/// </para>
/// <para>
/// Ids are per world, so the cache is generation-stamped and creating a second
/// <see cref="App"/> invalidates every cached id rather than handing out stale ones.
/// </para>
/// </remarks>
internal static class ComponentRegistry
{
    private static readonly object Gate = new();
    private static IntPtr _appHandle;
    private static bool _running;

    /// <summary>Bumped whenever a new app is created, invalidating cached ids.</summary>
    internal static int Generation { get; private set; }

    /// <summary>
    /// The handle to register through, or zero while the app runs, when an entry point has to go
    /// through the loaned world instead, for the reason <see cref="Register"/> gives.
    /// </summary>
    internal static IntPtr AppHandle
    {
        get { lock (Gate) return _running ? IntPtr.Zero : _appHandle; }
    }

    /// <summary>Binds the registry to a newly created app.</summary>
    internal static void BeginApp(IntPtr handle)
    {
        lock (Gate)
        {
            _appHandle = handle;
            _running = false;
            Generation++;
        }
    }

    /// <summary>Switches registration onto the world-loan route for the duration of the run.</summary>
    internal static void EnterRunning()
    {
        lock (Gate) _running = true;
    }

    /// <summary>Switches registration back to the app-handle route.</summary>
    internal static void ExitRunning()
    {
        lock (Gate) _running = false;
    }

    /// <summary>Unbinds the registry when the app is disposed.</summary>
    internal static void EndApp()
    {
        lock (Gate)
        {
            _appHandle = IntPtr.Zero;
            _running = false;
        }
    }

    /// <summary>
    /// Registers a layout and returns its component id.
    /// </summary>
    /// <remarks>
    /// Before the loop starts the app handle is free, so registration goes straight through it.
    /// Once <c>bcs_app_run</c> is on the stack that handle is mutably borrowed by Rust for the
    /// whole run, and touching it again would alias; registration then has to go through the
    /// world Bevy loaned to the running system instead.
    /// </remarks>
    internal static int Register(string name, uint size, uint align, bool sparse)
    {
        lock (Gate)
        {
            if (_appHandle == IntPtr.Zero && !_running)
                throw new InvalidOperationException(
                    $"Cannot register component '{name}': no App exists. Create an App before "
                    + "touching component types.");

            var storage = sparse ? SparseStorage : TableStorage;
            var id = _running
                ? Native.bcs_component_register_live(name, size, align, storage)
                : Native.bcs_component_register(_appHandle, name, size, align, storage);

            return Native.Check(id, $"registering component '{name}'");
        }
    }

    /// <summary>Contiguous columns, which suit almost every component.</summary>
    private const int TableStorage = 0;

    /// <summary>Cheap to add and remove, at the cost of not being iterable. See <see cref="ISparseComponent"/>.</summary>
    private const int SparseStorage = 1;
}
