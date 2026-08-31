using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Bevy's own components, reachable from C#.
/// </summary>
/// <remarks>
/// <para>
/// These are the raw ids. Reaching for one is rarely necessary: every component listed here has
/// a C# type implementing <see cref="INativeComponent"/>, and those resolve to the same ids
/// through the ordinary generic API, so <c>ctx.Ecs.Add(entity, Transform.Identity)</c> writes
/// Bevy's own <c>Transform</c>. The ids are still here for the id-shaped entry points such as
/// <see cref="EcsWorld.HasById"/>, and for passing an explicit component to
/// <see cref="EcsWorld.Chunks{T}(int, ReadOnlySpan{int}, ReadOnlySpan{int}, bool)"/>.
/// </para>
/// <para>
/// Only a component whose layout C# can mirror byte for byte can be read or written. Anything
/// holding a <c>String</c>, a <c>Vec</c> or an asset handle is exposed as a name-only handle,
/// good for filtering and counting, and its C# type says so by reporting
/// <see cref="INativeComponent.MirrorsLayout"/> as <see langword="false"/>.
/// </para>
/// </remarks>
public static class NativeComponents
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, int> Cache = [];
    private static int _generation = -1;

    /// <summary>Bevy's <c>Transform</c>: local position, rotation and scale.</summary>
    /// <remarks>The id behind <see cref="Bevy.Transform"/>, which the generic API resolves for you.</remarks>
    public static int Transform => ComponentType<Bevy.Transform>.Id;

    /// <summary>
    /// Bevy's <c>GlobalTransform</c>: the world-space result of propagation.
    /// </summary>
    /// <remarks>
    /// The id behind <see cref="Bevy.GlobalTransform"/>. Read it and write
    /// <see cref="Bevy.Transform"/>: propagation overwrites this one every frame.
    /// </remarks>
    public static int GlobalTransform => ComponentType<Bevy.GlobalTransform>.Id;

    /// <summary>Bevy's <c>ChildOf</c> relationship, for filtering on "has a parent".</summary>
    /// <remarks>
    /// Use <see cref="EcsWorld.SetParent"/> to change it. Writing the bytes directly would set
    /// the field without maintaining the matching <c>Children</c> list.
    /// </remarks>
    public static int ChildOf => ComponentType<Bevy.ChildOf>.Id;

    /// <summary>Bevy's <c>Children</c> list, for filtering on "has children".</summary>
    public static int Children => ComponentType<Bevy.Children>.Id;

    /// <summary>Bevy's <c>Visibility</c>: whether an entity asks to be drawn.</summary>
    /// <remarks>Render builds only. The id behind <see cref="Bevy.Visibility"/>.</remarks>
    public static int Visibility => ComponentType<Bevy.Visibility>.Id;

    /// <summary>Bevy's <c>InheritedVisibility</c>: the propagated answer.</summary>
    /// <remarks>Render builds only. The id behind <see cref="Bevy.InheritedVisibility"/>.</remarks>
    public static int InheritedVisibility => ComponentType<Bevy.InheritedVisibility>.Id;

    /// <summary>Bevy's <c>ViewVisibility</c>: whether a camera actually saw the entity.</summary>
    /// <remarks>Render builds only. The id behind <see cref="Bevy.ViewVisibility"/>.</remarks>
    public static int ViewVisibility => ComponentType<Bevy.ViewVisibility>.Id;

    /// <summary>
    /// Resolves a component by name, verifying the layout when C# mirrors it.
    /// </summary>
    /// <remarks>
    /// This is what <see cref="ComponentType{T}"/> calls for a type implementing
    /// <see cref="INativeComponent"/>, in place of registering a fresh component from its layout.
    /// </remarks>
    /// <param name="name">The Bevy type's name, as the bridge knows it.</param>
    /// <param name="expectedSize">
    /// The size C#'s mirror occupies, or 0 when there is no mirror to check.
    /// </param>
    /// <exception cref="BevyNativeException">
    /// The component is unknown to this build, or its layout does not match the mirror.
    /// </exception>
    internal static int Resolve(string name, int expectedSize)
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
            if (id == NativeStatus.NoWorld)
                throw new BevyNativeException(
                    NativeStatus.NoWorld,
                    $"Resolving Bevy's '{name}' component failed: "
                    + NativeStatus.Describe(NativeStatus.NoWorld) + ".");

            if (id < 0)
                throw new BevyNativeException(
                    NativeStatus.NoComponent,
                    $"The native bridge does not expose Bevy's '{name}' component. It may belong "
                    + "to a feature this build was compiled without, such as the renderer.");

            if (expectedSize > 0) VerifyLayout(name, id, expectedSize);
            ExtraCheck(name);

            Cache[name] = id;
            return id;
        }
    }

    /// <summary>
    /// Runs the further check a mirror needs when its size alone does not pin its layout down.
    /// </summary>
    /// <remarks>
    /// Keyed on the engine's name rather than passed in by the caller, so the check runs whichever
    /// route asked for the id: the property below, or <see cref="ComponentType{T}"/> resolving a
    /// type that implements <see cref="INativeComponent"/>.
    /// </remarks>
    private static void ExtraCheck(string name)
    {
        switch (name)
        {
            case "Transform":
                VerifyTransform();
                break;
            case "GlobalTransform":
                VerifyGlobalTransform();
                break;
            case "Visibility":
                VerifyVisibility();
                break;
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
    /// Fails loudly if C#'s mirror of <see cref="Bevy.GlobalTransform"/> is packed wrongly.
    /// </summary>
    /// <remarks>
    /// The same class of mistake as <see cref="VerifyTransform"/>, from a different cause. Each of
    /// the four vectors is a sixteen-byte-aligned <c>Vec3A</c>, so it occupies four floats and
    /// uses three. A mirror packing them tightly is 48 bytes rather than 64, which the size check
    /// would catch; one that pads them but pads them differently would not.
    /// </remarks>
    private static unsafe void VerifyGlobalTransform()
    {
        uint size;
        uint xAxis;
        uint yAxis;
        uint zAxis;
        uint translation;
        Native.Check(
            Native.bcs_global_transform_layout(&size, &xAxis, &yAxis, &zAxis, &translation),
            "reading the layout of 'GlobalTransform'");

        Expect("size", size, Bevy.GlobalTransform.NativeSize);
        Expect("x axis", xAxis, Bevy.GlobalTransform.XAxisOffset);
        Expect("y axis", yAxis, Bevy.GlobalTransform.YAxisOffset);
        Expect("z axis", zAxis, Bevy.GlobalTransform.ZAxisOffset);
        Expect("translation", translation, Bevy.GlobalTransform.TranslationOffset);

        static void Expect(string part, uint actual, int expected)
        {
            if (actual == (uint)expected) return;

            throw new BevyNativeException(
                NativeStatus.InvalidState,
                $"Bevy places GlobalTransform's {part} at {actual}, but this build of BevyCSharp "
                + $"mirrors it at {expected}. Using the mirror would read the world-space "
                + "position from the wrong place, so it is refused. The engine's field layout "
                + "has changed.");
        }
    }

    /// <summary>
    /// Fails loudly if C# has <see cref="Bevy.VisibilityMode"/>'s numbers wrong.
    /// </summary>
    /// <remarks>
    /// The struct mirrors are checked by size and offset. This one is a fieldless enum, where what
    /// has to match is which number stands for which variant, and a one-byte mirror looks equally
    /// healthy whichever way they are numbered. Rust promises no particular order for a
    /// default-representation enum, so getting it wrong would leave <c>Hidden</c> quietly meaning
    /// something else.
    /// </remarks>
    private static unsafe void VerifyVisibility()
    {
        uint size;
        uint inherited;
        uint hidden;
        uint visible;
        Native.Check(
            Native.bcs_visibility_layout(&size, &inherited, &hidden, &visible),
            "reading the layout of 'Visibility'");

        Expect("sizes Visibility at", size, sizeof(Bevy.VisibilityMode));
        Expect("numbers Inherited", inherited, (int)Bevy.VisibilityMode.Inherited);
        Expect("numbers Hidden", hidden, (int)Bevy.VisibilityMode.Hidden);
        Expect("numbers Visible", visible, (int)Bevy.VisibilityMode.Visible);

        static void Expect(string part, uint actual, int expected)
        {
            if (actual == (uint)expected) return;

            throw new BevyNativeException(
                NativeStatus.InvalidState,
                $"Bevy {part} {actual}, but this build of BevyCSharp mirrors it as {expected}. "
                + "Using the mirror would ask for the wrong visibility, so it is refused. The "
                + "engine's definition has changed.");
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

/// <summary>
/// Bevy's <c>ChildOf</c> relationship, naming an entity's parent.
/// </summary>
/// <remarks>
/// A name-only handle, for filtering on "has a parent". Use
/// <see cref="EcsWorld.SetParent"/> to change it and <see cref="EcsWorld.ParentOf"/> to read it:
/// writing the bytes directly would set the field without maintaining the matching
/// <see cref="Children"/> list.
/// </remarks>
public readonly struct ChildOf : INativeComponent
{
    readonly string INativeComponent.NativeName => "ChildOf";
    readonly bool INativeComponent.MirrorsLayout => false;
}

/// <summary>
/// Bevy's <c>Children</c> list.
/// </summary>
/// <remarks>
/// A name-only handle, for filtering on "has children". The component owns a <c>Vec</c>, which
/// is not safe to read as raw bytes; use <see cref="EcsWorld.ChildrenOf"/> for the contents.
/// </remarks>
public readonly struct Children : INativeComponent
{
    readonly string INativeComponent.NativeName => "Children";
    readonly bool INativeComponent.MirrorsLayout => false;
}
