using Bevy.Interop;

namespace Bevy;

/// <summary>
/// What was clicked in the scene.
/// </summary>
/// <remarks>
/// <para>
/// A click on a mesh, resolved by Bevy's own picking, which raycasts the scene against the pointer.
/// The interface is not reported here, because a click that landed on a panel belongs to that
/// panel, and the interface answers for it.
/// </para>
/// <para>
/// Needs a bridge with the renderer, which <see cref="App.HasRenderer"/> reports, and an app with
/// <see cref="Config.MeshPicking"/>, which the editor has. A game observes what a pointer does to
/// an entity with <see cref="Pointer{TEvent}"/>, as Bevy's do, and this queue is the editor's
/// older way of hearing a click.
/// </para>
/// </remarks>
public static unsafe class Picking
{
    /// <summary>How many picks one call carries at most.</summary>
    private const int BatchSize = 16;

    /// <summary>Takes the scene entities clicked since the last call, none where meshes are not picked.</summary>
    public static Entity[] Drain()
    {
        if (!App.HasRenderer) return [];

        var drained = new List<Entity>();
        var buffer = stackalloc ulong[BatchSize];

        int count;
        do
        {
            // An app that did not ask for meshes to be picked has no queue, and nothing in it.
            count = Native.bcs_pick_events(buffer, BatchSize);
            if (count == NativeStatus.Unsupported) return [];
            Native.Check(count, "draining the scene picks");

            for (var i = 0; i < count; i++) drained.Add(new Entity(buffer[i]));
        }
        while (count == BatchSize);

        return [.. drained];
    }

    /// <summary>
    /// The nearest mesh a ray meets, where, and which way the surface there faces.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tested against each mesh's triangles rather than its bounds, so a ray past the corner of a
    /// box or through the hole of a torus meets what is behind it. Only meshes on the default
    /// render layer are met, so a picture a tool draws on a layer of its own is not in the way.
    /// With <see cref="Render.TryRay"/>, which turns a point on the viewport into a ray, it says
    /// what is under the pointer, which is where a model dropped on the scene belongs.
    /// </para>
    /// <para>
    /// Needs a bridge with the renderer, which carries Bevy's mesh picking, and answers false on any
    /// other. The app need not have asked for meshes to be picked under the pointer.
    /// </para>
    /// </remarks>
    /// <param name="origin">Where the ray starts, in world space.</param>
    /// <param name="direction">Which way it goes, of any length but zero.</param>
    /// <param name="entity">The mesh it met.</param>
    /// <param name="point">Where it met it, in world space.</param>
    /// <param name="normal">Which way the surface faces there, of length one.</param>
    /// <returns>Whether it met anything.</returns>
    public static bool TryCast(Vec3 origin, Vec3 direction, out Entity entity, out Vec3 point, out Vec3 normal) =>
        TryCast(origin, direction, out entity, out point, out normal, out _);

    /// <summary>
    /// The nearest mesh a ray meets, as <see cref="TryCast(Vec3, Vec3, out Entity, out Vec3, out Vec3)"/>
    /// says, and where on its texture.
    /// </summary>
    /// <remarks>
    /// The texture coordinate is the mesh's own at that point, from zero to one across its image,
    /// so a pointer moved over an interface drawn into the image with <see cref="MovePointer"/> is
    /// at <c>uv</c> times the image's size. Nothing for a mesh with no texture coordinates.
    /// </remarks>
    /// <param name="origin">Where the ray starts, in world space.</param>
    /// <param name="direction">Which way it goes, of any length but zero.</param>
    /// <param name="entity">The mesh it met.</param>
    /// <param name="point">Where it met it, in world space.</param>
    /// <param name="normal">Which way the surface faces there, of length one.</param>
    /// <param name="uv">Where on the mesh's texture it met it.</param>
    /// <returns>Whether it met anything.</returns>
    public static bool TryCast(Vec3 origin, Vec3 direction, out Entity entity, out Vec3 point, out Vec3 normal, out Vec2? uv)
    {
        entity = Entity.None;
        point = Vec3.Zero;
        normal = Vec3.Zero;
        uv = null;

        if (!App.HasRenderer || direction == Vec3.Zero) return false;

        var from = stackalloc float[3] { origin.X, origin.Y, origin.Z };
        var towards = stackalloc float[3] { direction.X, direction.Y, direction.Z };
        var at = stackalloc float[3];
        var facing = stackalloc float[3];
        var texel = stackalloc float[2];
        ulong hit;

        if (Native.bcs_pick_ray(from, towards, &hit, at, facing, texel) != 0) return false;

        entity = new Entity(hit);
        point = new Vec3(at[0], at[1], at[2]);
        normal = new Vec3(facing[0], facing[1], facing[2]);
        if (!float.IsNaN(texel[0])) uv = new Vec2(texel[0], texel[1]);
        return true;
    }

    /// <summary>
    /// Makes a pointer of the game's own, which it moves and presses itself. Only valid inside a
    /// system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bevy's <c>PointerId::Custom</c>. Bevy finds what such a pointer is over as it finds what the
    /// mouse is over, and an entity hears what it does through <see cref="Pointer{TEvent}"/> with
    /// this as <see cref="Pointer{TEvent}.PointerId"/>. It is for a pointer that is not the mouse,
    /// as a game's own cursor moving over an interface drawn into a texture on a screen in the
    /// world, put where a ray from the mouse meets the screen (<see cref="TryCast(Vec3, Vec3, out Entity, out Vec3, out Vec3, out Vec2?)"/>).
    /// </para>
    /// </remarks>
    /// <returns>The pointer, to move and press.</returns>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static PointerId SpawnPointer()
    {
        ulong number;
        Native.Check(Native.bcs_pointer_spawn(&number), "making a pointer");
        return new PointerId(PointerKind.Custom, number);
    }

    /// <summary>Moves a pointer from <see cref="SpawnPointer"/> to a place on an image. Only valid inside a system.</summary>
    /// <param name="pointer">The pointer.</param>
    /// <param name="image">The image it is on, which a camera draws into, from <see cref="Render.CreateTarget"/>.</param>
    /// <param name="position">Where on it, in its pixels from the top left.</param>
    /// <exception cref="BevyNativeException">The pointer or the image is not there, or this build has no renderer.</exception>
    public static void MovePointer(PointerId pointer, AssetHandle image, Vec2 position) => Send(pointer, image, position, 0, PointerButton.Primary);

    /// <summary>Presses a button of a pointer from <see cref="SpawnPointer"/> where it is put. Only valid inside a system.</summary>
    /// <param name="pointer">The pointer.</param>
    /// <param name="image">The image it is on, which a camera draws into, from <see cref="Render.CreateTarget"/>.</param>
    /// <param name="position">Where on it, in its pixels from the top left.</param>
    /// <param name="button">Which button.</param>
    /// <exception cref="BevyNativeException">The pointer or the image is not there, or this build has no renderer.</exception>
    public static void PressPointer(PointerId pointer, AssetHandle image, Vec2 position, PointerButton button = PointerButton.Primary) =>
        Send(pointer, image, position, 1, button);

    /// <summary>Lets a button of a pointer from <see cref="SpawnPointer"/> go where it is put. Only valid inside a system.</summary>
    /// <param name="pointer">The pointer.</param>
    /// <param name="image">The image it is on, which a camera draws into, from <see cref="Render.CreateTarget"/>.</param>
    /// <param name="position">Where on it, in its pixels from the top left.</param>
    /// <param name="button">Which button.</param>
    /// <exception cref="BevyNativeException">The pointer or the image is not there, or this build has no renderer.</exception>
    public static void ReleasePointer(PointerId pointer, AssetHandle image, Vec2 position, PointerButton button = PointerButton.Primary) =>
        Send(pointer, image, position, 2, button);

    /// <summary>
    /// Locks a pointer to an entity, so the entity is all the pointer is over until the capture is
    /// released or the pointer's button is let go. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bevy's <c>PointerCaptureMap::capture</c>. A slider's thumb dragged across other widgets
    /// keeps the pointer this way, so they do not light up as the pointer passes over them, and
    /// the drag goes on reaching the thumb wherever the pointer strays:
    /// </para>
    /// <code>
    /// ecs.Observe&lt;Pointer&lt;DragStart&gt;&gt;(thumb, on =&gt;
    ///     Picking.CapturePointer(on.Event.PointerId, on.Event.Entity, on.Event.Event.Hit));
    /// ecs.Observe&lt;Pointer&lt;DragEnd&gt;&gt;(thumb, on =&gt; Picking.ReleaseCapture(on.Event.PointerId));
    /// </code>
    /// <para>
    /// The pointer reports the hit given here as what it meets while it is held, usually the one
    /// the event that started the drag carried. A pointer of the game's own is one from
    /// <see cref="SpawnPointer"/>.
    /// </para>
    /// </remarks>
    /// <param name="pointer">The pointer.</param>
    /// <param name="entity">What it is locked to.</param>
    /// <param name="hit">What it reports it meets while it is held.</param>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static void CapturePointer(PointerId pointer, Entity entity, PointerHit hit)
    {
        var position = hit.Position ?? default;
        var normal = hit.Normal ?? default;
        var at = stackalloc float[] { position.X, position.Y, position.Z };
        var facing = stackalloc float[] { normal.X, normal.Y, normal.Z };

        Native.Check(
            Native.bcs_pointer_capture(
                (int)pointer.Kind,
                pointer.Number,
                entity.Bits,
                hit.Camera.Bits,
                hit.Depth,
                hit.Position is null ? null : at,
                hit.Normal is null ? null : facing),
            $"locking {pointer} to {entity}");
    }

    /// <summary>
    /// Releases what a pointer was locked to by <see cref="CapturePointer"/>, which does nothing
    /// where it was not locked. Only valid inside a system.
    /// </summary>
    /// <remarks>Bevy's <c>PointerCaptureMap::release</c>.</remarks>
    /// <param name="pointer">The pointer.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static void ReleaseCapture(PointerId pointer) =>
        Native.Check(
            Native.bcs_pointer_release_capture((int)pointer.Kind, pointer.Number),
            $"releasing {pointer}");

    private static void Send(PointerId pointer, AssetHandle image, Vec2 position, int action, PointerButton button)
    {
        if (pointer.Kind != PointerKind.Custom)
            throw new ArgumentException("Only a pointer of the game's own, from SpawnPointer, is moved and pressed here.", nameof(pointer));

        Native.Check(
            Native.bcs_pointer_input(pointer.Number, image.Key, position.X, position.Y, action, (int)button),
            $"moving {pointer} on {image}");
    }
}
