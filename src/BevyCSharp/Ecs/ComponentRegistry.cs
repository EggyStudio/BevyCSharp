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
/// ordinary Bevy component - it lives in tables, participates in archetypes, and is visible to
/// Bevy's own change detection.
/// </para>
/// <para>
/// Ids are per world, so the cache is generation-stamped: creating a second
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
    internal static int Register(string name, uint size, uint align)
    {
        lock (Gate)
        {
            if (_appHandle == IntPtr.Zero && !_running)
                throw new InvalidOperationException(
                    $"Cannot register component '{name}': no App exists. Create an App before "
                    + "touching component types.");

            var id = _running
                ? Native.bcs_component_register_live(name, size, align)
                : Native.bcs_component_register(_appHandle, name, size, align);

            return Native.Check(id, $"registering component '{name}'");
        }
    }
}

/// <summary>
/// The cached Bevy component id for <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">
/// A blittable struct. The <c>unmanaged</c> constraint is what makes the layout safe to hand
/// to Bevy verbatim: no references, no GC involvement, no marshalling on the hot path.
/// </typeparam>
public static class ComponentType<T> where T : unmanaged
{
    private static int _generation = -1;
    private static int _id;

    /// <summary>Size of one component in bytes.</summary>
    public static int Size => Unsafe.SizeOf<T>();

    /// <summary>
    /// Alignment of the component in bytes, measured by how far a <typeparamref name="T"/> is
    /// pushed when it follows a single byte in a sequential struct.
    /// </summary>
    public static int Alignment => Unsafe.SizeOf<AlignmentProbe>() - Unsafe.SizeOf<T>();

    /// <summary>The Bevy component id, registering the type on first use.</summary>
    public static int Id
    {
        get
        {
            if (_generation == ComponentRegistry.Generation) return _id;

            _id = ComponentRegistry.Register(
                typeof(T).FullName ?? typeof(T).Name,
                (uint)Size,
                (uint)Alignment);
            _generation = ComponentRegistry.Generation;
            return _id;
        }
    }

    /// <summary>Layout probe: the offset of <c>Value</c> is the alignment of <typeparamref name="T"/>.</summary>
    private struct AlignmentProbe
    {
#pragma warning disable CS0169 // Field is never used - it exists purely to create padding.
        private readonly byte _pad;
        private readonly T _value;
#pragma warning restore CS0169
    }
}
