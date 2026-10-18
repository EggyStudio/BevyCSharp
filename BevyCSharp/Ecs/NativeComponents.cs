using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Bevy's own components, reachable from C#.
/// </summary>
/// <remarks>
/// <para>
/// These are the raw ids. Reaching for one is rarely necessary, because every component listed here
/// has a C# type implementing <see cref="INativeComponent"/>, and those resolve to the same ids
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
    /// <see cref="Bevy.Transform"/>, because propagation overwrites this one every frame.
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

    /// <summary>Bevy's <c>WorldInstance</c>: present once a scene has been spawned.</summary>
    /// <remarks>The id behind <see cref="Bevy.WorldInstance"/>.</remarks>
    public static int WorldInstance => ComponentType<Bevy.WorldInstance>.Id;

    /// <summary>Bevy's <c>Atmosphere</c>: the planet whose air a sky is scattered through.</summary>
    /// <remarks>
    /// Render builds only. The id behind <see cref="Bevy.Atmosphere"/>, which is how the planet
    /// <see cref="Render.SetAtmosphere"/> keeps is found or counted.
    /// </remarks>
    public static int Atmosphere => ComponentType<Bevy.Atmosphere>.Id;

    /// <summary>Bevy's <c>Interaction</c>: how the pointer stands on a UI node.</summary>
    /// <remarks>
    /// Render builds only. The id behind <see cref="Bevy.Interaction"/>, which a <c>[With]</c>
    /// filter needs to find the nodes that react. Its value comes from
    /// <see cref="Ui.InteractionOf"/>.
    /// </remarks>
    public static int Interaction => ComponentType<Bevy.Interaction>.Id;

    /// <summary>
    /// Resolves a component by name, verifying the layout when C# mirrors it.
    /// </summary>
    /// <remarks>
    /// <see cref="ComponentType{T}"/> calls this for a type implementing
    /// <see cref="INativeComponent"/>, in place of registering a fresh component from its layout.
    /// </remarks>
    /// <param name="name">The Bevy type's name, as the bridge knows it.</param>
    /// <param name="expectedSize">
    /// The size C#'s mirror occupies, or 0 when there is no mirror to check.
    /// </param>
    /// <param name="layout">
    /// For a generated mirror, the layout it was generated from, which the bridge's reading has to
    /// match, or nothing for a mirror written by hand.
    /// </param>
    /// <exception cref="BevyNativeException">
    /// The component is unknown to this build, or its layout does not match the mirror.
    /// </exception>
    internal static int Resolve(string name, int expectedSize, string? layout = null)
    {
        lock (Gate)
        {
            // Ids belong to a world, so a second App has to resolve them again.
            if (_generation != ComponentRegistry.Generation)
            {
                Cache.Clear();
                _generation = ComponentRegistry.Generation;
            }

            // A mirror is cached under its layout as well as its name, so a second mirror of the same
            // component, generated from another layout or written by hand, is checked for itself.
            var key = layout is null ? name : name + "\n" + layout;
            if (Cache.TryGetValue(key, out var cached)) return cached;

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

            // A mirror the description holds the layout of is held to the bridge's reading of every
            // field, and one written without it, as a game may write one, to the component's size.
            if (layout is not null) VerifyMirror(name, layout);
            else if (expectedSize > 0) VerifyLayout(name, id, expectedSize);

            Cache[key] = id;
            return id;
        }
    }

    /// <summary>
    /// Fails loudly if a mirror's layout is not the one the bridge reads for its component.
    /// </summary>
    /// <remarks>
    /// A mirror's fields stand where the description says the compiler put them when the
    /// description was written, a generated mirror's and the library's own transform and visibility
    /// types' alike. The bridge reads the layout again from a value of the component
    /// (<see cref="ReflectedLayouts"/>), at the same paths, and every offset, size and enum number
    /// has to agree, so a Bevy release that moved a field refuses the mirror here rather than letting
    /// it read one field's bytes as another's.
    /// </remarks>
    private static void VerifyMirror(string name, string layout)
    {
        var measured = ReflectedLayouts.Measure(name) is { } read
            ? ReflectedLayouts.Signature(read, ReflectedLayouts.PathsOf(layout))
            : null;
        if (measured == layout) return;

        throw new BevyNativeException(
            NativeStatus.InvalidState,
            $"Bevy lays '{name}' out as {measured ?? "something without one of its fields"}, but this build of "
            + $"BevyCSharp mirrors it as {layout}. Using the mirror would read a field from another's bytes, "
            + "so it is refused. The engine's layout has changed since the description was written, and "
            + "the schema dump writes it again (BUILDING.md).");
    }

    /// <summary>
    /// Fails loudly if C#'s mirror of a Bevy struct is the wrong size.
    /// </summary>
    /// <remarks>
    /// These mirrors are easy to get subtly wrong. <c>Quat</c> is SIMD-backed and sixteen-byte
    /// aligned on most targets, which pads <c>Transform</c> to 48 bytes rather than the 40 its
    /// three fields suggest, and on a target without that alignment it would be 40 after all.
    /// Reading a mismatched layout would not throw; it would return plausible nonsense and
    /// corrupt neighboring components on write. Asking the engine for the real number costs one
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
