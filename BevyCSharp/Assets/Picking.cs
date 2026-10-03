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
/// Needs a bridge built with the editor profile, which <see cref="App.HasEditor"/> reports.
/// Drained rather than subscribed to, for the same reason the interface events are, which is that a
/// C# system is handed the world and cannot hold an observer.
/// </para>
/// </remarks>
public static unsafe class Picking
{
    /// <summary>How many picks one call carries at most.</summary>
    private const int BatchSize = 16;

    /// <summary>Takes the scene entities clicked since the last call.</summary>
    public static Entity[] Drain()
    {
        if (!App.HasEditor) return [];

        var drained = new List<Entity>();
        var buffer = stackalloc ulong[BatchSize];

        int count;
        do
        {
            count = Native.Check(
                Native.bcs_pick_events(buffer, BatchSize), "draining the scene picks");

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
    /// Needs a bridge built with the editor profile, which carries Bevy's mesh picking, and answers
    /// false on any other.
    /// </para>
    /// </remarks>
    /// <param name="origin">Where the ray starts, in world space.</param>
    /// <param name="direction">Which way it goes, of any length but zero.</param>
    /// <param name="entity">The mesh it met.</param>
    /// <param name="point">Where it met it, in world space.</param>
    /// <param name="normal">Which way the surface faces there, of length one.</param>
    /// <returns>Whether it met anything.</returns>
    public static bool TryCast(Vec3 origin, Vec3 direction, out Entity entity, out Vec3 point, out Vec3 normal)
    {
        entity = Entity.None;
        point = Vec3.Zero;
        normal = Vec3.Zero;

        if (!App.HasEditor || direction == Vec3.Zero) return false;

        var from = stackalloc float[3] { origin.X, origin.Y, origin.Z };
        var towards = stackalloc float[3] { direction.X, direction.Y, direction.Z };
        var at = stackalloc float[3];
        var facing = stackalloc float[3];
        ulong hit;

        if (Native.bcs_pick_ray(from, towards, &hit, at, facing) != 0) return false;

        entity = new Entity(hit);
        point = new Vec3(at[0], at[1], at[2]);
        normal = new Vec3(facing[0], facing[1], facing[2]);
        return true;
    }
}
