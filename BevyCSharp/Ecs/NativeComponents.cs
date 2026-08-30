using System.Runtime.CompilerServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Bevy's own components, reachable from C#.
/// </summary>
/// <remarks>
/// <para>
/// A struct C# declares is registered with Bevy from its layout, because Bevy has never heard of
/// it. Bevy's own components are the opposite problem: they are Rust types the managed side has
/// no handle on, so they are asked for by name and come back as the same kind of id. Everything
/// downstream is keyed on ids rather than types, so once you have one, all the usual operations
/// work on it: <see cref="EcsWorld.AddNative{T}"/>, <see cref="EcsWorld.GetNativeRef{T}"/>,
/// chunked iteration, change detection.
/// </para>
/// <para>
/// Only components whose layout C# can mirror byte for byte are here. Anything holding a
/// <c>String</c>, a <c>Vec</c> or an asset handle is not safe to read as raw bytes and needs a
/// purpose-built bridge instead.
/// </para>
/// </remarks>
public static class NativeComponents
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, int> Cache = [];
    private static int _generation = -1;

    /// <summary>Bevy's <c>Transform</c>: local position, rotation and scale.</summary>
    public static int Transform => Lookup("Transform", Unsafe.SizeOf<Transform>(), VerifyTransform);

    /// <summary>
    /// Bevy's <c>GlobalTransform</c>: the world-space result of propagation.
    /// </summary>
    /// <remarks>
    /// Returned for filtering and counting only. It is a 3x4 affine matrix rather than the
    /// position/rotation/scale triple <see cref="Bevy.Transform"/> uses, and C# has no mirror for
    /// it, so reading it as a <see cref="Bevy.Transform"/> would be wrong. Write to
    /// <see cref="Bevy.Transform"/> and let Bevy propagate.
    /// </remarks>
    public static int GlobalTransform => Lookup("GlobalTransform", expectedSize: 0);

    /// <summary>Bevy's <c>ChildOf</c> relationship, for filtering on "has a parent".</summary>
    /// <remarks>
    /// Use <see cref="EcsWorld.SetParent"/> to change it. Writing the bytes directly would set
    /// the field without maintaining the matching <c>Children</c> list.
    /// </remarks>
    public static int ChildOf => Lookup("ChildOf", expectedSize: 0);

    /// <summary>Bevy's <c>Children</c> list, for filtering on "has children".</summary>
    public static int Children => Lookup("Children", expectedSize: 0);

    /// <summary>
    /// Resolves a component by name, verifying the layout when C# mirrors it.
    /// </summary>
    /// <param name="name">The Bevy type's name, as the bridge knows it.</param>
    /// <param name="expectedSize">
    /// The size C#'s mirror occupies, or 0 when there is no mirror to check.
    /// </param>
    /// <param name="extraCheck">
    /// A further check for a mirror whose size alone does not pin its layout down.
    /// </param>
    /// <exception cref="BevyNativeException">
    /// The component is unknown to this build, or its layout does not match the mirror.
    /// </exception>
    private static int Lookup(string name, int expectedSize, Action? extraCheck = null)
    {
        lock (Gate)
        {
            // Ids belong to a world, so a second App has to resolve them again.
            if (_generation != ComponentRegistry.Generation)
            {
                Cache.Clear();
                _generation = ComponentRegistry.Generation;
            }

            if (Cache.TryGetValue(name, out var cached)) return cached;

            var id = Native.bcs_component_id_of(name);
            if (id < 0)
                throw new BevyNativeException(
                    NativeStatus.NoComponent,
                    $"The native bridge does not expose Bevy's '{name}' component. It may belong "
                    + "to a feature this build was compiled without, such as the renderer.");

            if (expectedSize > 0) VerifyLayout(name, id, expectedSize);
            extraCheck?.Invoke();

            Cache[name] = id;
            return id;
        }
    }

    /// <summary>
    /// Fails loudly if C#'s mirror of <see cref="Bevy.Transform"/> puts a field in the wrong place.
    /// </summary>
    /// <remarks>
    /// The size check below is necessary but not sufficient. Bevy's <c>Transform</c> uses Rust's
    /// default representation, so the compiler reorders its fields to save padding, and the
    /// reordered layout happens to be the same total size as the source order. Only the offsets
    /// distinguish them, and getting them wrong reads every value from the wrong place while
    /// looking entirely healthy.
    /// </remarks>
    private static unsafe void VerifyTransform()
    {
        uint size;
        uint rotation;
        uint translation;
        uint scale;
        Native.Check(
            Native.bcs_transform_layout(&size, &rotation, &translation, &scale),
            "reading the layout of 'Transform'");

        Expect("size", size, Bevy.Transform.NativeSize);
        Expect("rotation", rotation, Bevy.Transform.RotationOffset);
        Expect("translation", translation, Bevy.Transform.TranslationOffset);
        Expect("scale", scale, Bevy.Transform.ScaleOffset);

        static void Expect(string part, uint actual, int expected)
        {
            if (actual == (uint)expected) return;

            throw new BevyNativeException(
                NativeStatus.InvalidState,
                $"Bevy places Transform's {part} at {actual}, but this build of BevyCSharp "
                + $"mirrors it at {expected}. Using the mirror would read every field from the "
                + "wrong place, so it is refused. The engine's field layout has changed.");
        }
    }

    /// <summary>
    /// Fails loudly if C#'s mirror of a Bevy struct is the wrong size.
    /// </summary>
    /// <remarks>
    /// These mirrors are easy to get subtly wrong. <c>Quat</c> is SIMD-backed and sixteen-byte
    /// aligned on most targets, which pads <c>Transform</c> to 48 bytes rather than the 40 its
    /// three fields suggest, and on a target without that alignment it would be 40 after all.
    /// Reading a mismatched layout would not throw; it would return plausible nonsense and
    /// corrupt neighbouring components on write. Asking the engine for the real number costs one
    /// call, once, and turns the whole class of mistake into a startup error.
    /// </remarks>
    private static unsafe void VerifyLayout(string name, int id, int expectedSize)
    {
        uint actualSize;
        uint actualAlign;
        Native.Check(
            Native.bcs_component_layout(id, &actualSize, &actualAlign),
            $"reading the layout of '{name}'");

        if (actualSize != (uint)expectedSize)
            throw new BevyNativeException(
                NativeStatus.InvalidState,
                $"Bevy's '{name}' is {actualSize} bytes, but this build of BevyCSharp mirrors it "
                + $"as {expectedSize}. The mirror is wrong for this target and using it would "
                + "corrupt memory, so it is refused. This usually means the engine changed the "
                + "struct, or the target aligns its fields differently.");
    }
}
