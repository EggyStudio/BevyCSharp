using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Render
{
    /// <summary>
    /// Builds Bevy's forward decal material from <paramref name="settings"/> and returns a handle
    /// to it, for an entity made a decal with <see cref="SetForwardDecal"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A forward decal draws its material onto the surfaces inside its box, as a scorch mark or a
    /// painted sign is laid over whatever wall it meets, and fades where the surface is far from the
    /// box's middle, by <paramref name="depthFadeFactor"/>. The camera drawing it needs a depth
    /// prepass, a <c>DepthPrepassRef</c> on the camera, which the decal reads the surfaces from.
    /// </para>
    /// <para>
    /// Decals laid over each other are told apart by <paramref name="depthBias"/>, the larger drawn
    /// over the smaller, as Bevy's decal example lays three. The settings' alpha mode decides how it
    /// meets the surface, opaque, blended or added.
    /// </para>
    /// <para>
    /// Bevy's clustered decals, a <c>ClusteredDecalRef</c> on an entity, are the other kind, drawn
    /// in the lighting pass by every material Bevy lights, many at a time for little cost, where a
    /// forward decal is a draw of its own over any surface.
    /// </para>
    /// </remarks>
    /// <param name="settings">The decal's look, its texture among them, as a standard material's.</param>
    /// <param name="depthFadeFactor">How quickly the decal fades from its box's middle.</param>
    /// <param name="depthBias">Which of two overlapping decals is drawn over the other.</param>
    /// <exception cref="BevyNativeException">
    /// A texture handle names nothing, the run draws nothing, as a headless one, or this build has
    /// no renderer.
    /// </exception>
    public static AssetHandle CreateDecalMaterial(MaterialSettings settings, float depthFadeFactor = 1f, float depthBias = 0f)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = Config(settings);
        var key = Native.bcs_material_create_forward_decal(&native, depthFadeFactor, depthBias);
        if (key == NativeStatus.Unsupported)
            throw App.HasRenderer
                ? new BevyNativeException(NativeStatus.Unsupported, "Building a decal material needs a run that draws, windowed or offscreen. A headless run has no decals to draw.")
                : NoRenderer("Building a decal material");

        Native.Check(key, "building a decal material");
        return new AssetHandle(key);
    }

    /// <summary>
    /// Makes <paramref name="entity"/> a forward decal drawn with <paramref name="material"/>, its
    /// box the entity's transform, as Bevy's <c>ForwardDecal</c> with its material does. Only valid
    /// inside a system.
    /// </summary>
    /// <remarks>
    /// The box is a unit cube scaled by the transform, and the decal is drawn on the surfaces it
    /// meets looking down its Y axis, so a decal on the ground is scaled wide and placed where it
    /// lies, as Bevy's are.
    /// </remarks>
    /// <param name="entity">The entity, its transform the decal's box.</param>
    /// <param name="material">A material made with <see cref="CreateDecalMaterial"/>.</param>
    /// <exception cref="BevyNativeException">
    /// The material is no decal material, the entity does not exist, or this build has no renderer.
    /// </exception>
    public static void SetForwardDecal(Entity entity, AssetHandle material) =>
        Native.Check(Native.bcs_render_set_forward_decal(entity.Bits, material.Key), $"making {entity} a forward decal");
}
