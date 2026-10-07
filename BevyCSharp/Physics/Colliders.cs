using System.Diagnostics.CodeAnalysis;

namespace Bevy.Physics;

/// <summary>Works out what a <see cref="Collider"/> collides as, and draws it.</summary>
/// <remarks>
/// One answer for the simulation and the editor's gizmo alike, so a gizmo is drawn where its body collides.
/// A mesh's triangles are read back once per mesh and kept, since reading one is a copy from the
/// engine and a level holds a few meshes many times over.
/// </remarks>
public static class Colliders
{
    private static readonly object Gate = new();
    private static readonly Dictionary<AssetHandle, MeshData> Meshes = [];

    /// <summary>
    /// What an entity's collider comes to, or false while a mesh it fits has not loaded or a hull or a
    /// mesh has none to fit.
    /// </summary>
    /// <param name="world">The world the entity is in. Only valid inside a system.</param>
    /// <param name="entity">An entity carrying a <see cref="Collider"/>.</param>
    /// <param name="fit">The shape, its size and where its middle is.</param>
    public static bool TryFit(EcsWorld world, Entity entity, out ColliderFit fit)
    {
        ArgumentNullException.ThrowIfNull(world);
        fit = default;

        if (!world.TryGet<Collider>(entity, out var collider)) return false;

        var scale = world.TryGet<Transform>(entity, out var transform) ? Abs(transform.Scale) : Vec3.One;

        if (collider.Shape is ColliderShape.Hull or ColliderShape.Mesh)
        {
            if (!App.HasRenderer || !TryDrawnUnder(world, entity, out var drawn) || drawn.Positions.Length < 4) return false;

            var positions = drawn.Positions.Select(point => Times(point + collider.Offset, scale)).ToArray();
            var (low, high) = Bounds(positions);
            var shape = collider.Shape == ColliderShape.Hull ? PhysicsShape.Hull(positions) : PhysicsShape.Mesh(positions, drawn.Indices!);

            fit = new ColliderFit(shape, collider.Shape, high - low, (low + high) * 0.5f);
            return true;
        }

        // The mesh it is drawn with, or nothing for an entity drawn with none or a build with no
        // renderer, which a size given outright does not need.
        MeshData? mesh = null;
        if (collider.Size == Vec3.Zero && App.HasRenderer && Render.MeshOf(world, entity) is { IsValid: true } handle)
        {
            if (!TryMesh(handle, out mesh)) return false;
        }

        // The box the shape fits in, given or taken from the mesh, and where its middle is.
        Vec3 size, middle;
        if (collider.Size != Vec3.Zero)
        {
            size = Abs(collider.Size);
            middle = collider.Offset;
        }
        else if (mesh is not null && mesh.Positions.Length > 0)
        {
            var (low, high) = Bounds(mesh.Positions);
            size = high - low;
            middle = (low + high) * 0.5f + collider.Offset;
        }
        else
        {
            size = Vec3.One;
            middle = collider.Offset;
        }

        size = Times(size, scale);
        middle = Times(middle, scale);

        var across = MathF.Max(size.X, size.Z) * 0.5f;
        PhysicsShape primitive;
        switch (collider.Shape)
        {
            case ColliderShape.Sphere:
                var radius = MathF.Max(MathF.Max(size.X, size.Y), size.Z) * 0.5f;
                primitive = PhysicsShape.Sphere(radius);
                size = new Vec3(radius * 2f);
                break;

            case ColliderShape.Capsule:
                // The straight part between the caps, which is the height less the two caps.
                primitive = PhysicsShape.Capsule(across, MathF.Max(size.Y - across * 2f, 0f));
                size = new Vec3(across * 2f, MathF.Max(size.Y, across * 2f), across * 2f);
                break;

            case ColliderShape.Cylinder:
                primitive = PhysicsShape.Cylinder(across, size.Y);
                size = new Vec3(across * 2f, size.Y, across * 2f);
                break;

            default:
                primitive = PhysicsShape.Box(size);
                break;
        }

        fit = new ColliderFit(primitive.Moved(middle), collider.Shape, size, middle);
        return true;
    }

    /// <summary>Draws an entity's collider as a gizmo, where it collides.</summary>
    /// <param name="world">The world the entity is in. Only valid inside a system.</param>
    /// <param name="entity">An entity carrying a <see cref="Collider"/>.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <returns>Whether there was anything to draw.</returns>
    /// <remarks>
    /// From the entity's world transform, so a collider on a child is drawn where its parent has
    /// put it. A hull or a mesh is drawn as the box around it, since its triangles would hide the
    /// mesh it was taken from.
    /// </remarks>
    public static bool Draw(EcsWorld world, Entity entity, (float R, float G, float B, float A) color)
    {
        if (!TryFit(world, entity, out var fit)) return false;

        var placed = world.TryGet<GlobalTransform>(entity, out var global) ? global.ToTransform() : world.GetOrDefault<Transform>(entity);
        var center = placed.Translation + placed.Rotation * fit.Center;

        switch (fit.Kind)
        {
            case ColliderShape.Sphere:
                Gizmos.Sphere(center, fit.Size.X * 0.5f, color, inFront: false);
                break;
            case ColliderShape.Capsule:
                Gizmos.Capsule(center, placed.Rotation, fit.Size.X * 0.5f, MathF.Max(fit.Size.Y - fit.Size.X, 0f), color, inFront: false);
                break;
            case ColliderShape.Cylinder:
                Gizmos.Cylinder(center, placed.Rotation, fit.Size.X * 0.5f, fit.Size.Y * 0.5f, color, inFront: false);
                break;
            default:
                Gizmos.Box(center, placed.Rotation, fit.Size, color, inFront: false);
                break;
        }

        return true;
    }

    /// <summary>Forgets every mesh read, for an app starting, whose handles are its own.</summary>
    internal static void Forget()
    {
        lock (Gate) Meshes.Clear();
    }

    /// <summary>Forgets a mesh read whose handle was released, so its key given again reads afresh.</summary>
    internal static void Forget(AssetHandle mesh)
    {
        lock (Gate) Meshes.Remove(mesh);
    }

    /// <summary>
    /// The meshes an entity and those under it are drawn with, as one, each placed in the entity's
    /// own space by the transforms between them, or false while one of them is loading or none of
    /// them is drawn with any.
    /// </summary>
    /// <remarks>
    /// A model is spawned under the entity that places it, its parts each a child drawn with a mesh
    /// of its own and the entity with none, so a hull or a mesh taken from the entity alone would
    /// wait forever. A mesh of lines or points is passed over, having no triangles. Each point is
    /// carried up through the transforms of the entities between it and the entity, which together
    /// place a part in its model, and the entity's own scale is put on afterwards with the
    /// collider's. Walked from the entity downward each time it is asked, since a body is made once
    /// and the walk is only repeated while it waits.
    /// </remarks>
    private static bool TryDrawnUnder(EcsWorld world, Entity entity, [NotNullWhen(true)] out MeshData? drawn)
    {
        drawn = null;
        var positions = new List<Vec3>();
        var indices = new List<uint>();

        // Each entity with the transforms that place it under the entity, its own first.
        var pending = new Stack<(Entity Entity, Transform[] Up)>();
        pending.Push((entity, []));
        while (pending.Count > 0)
        {
            var (next, up) = pending.Pop();
            foreach (var child in world.ChildrenOf(next))
            {
                pending.Push((child, [world.TryGet<Transform>(child, out var placed) ? placed : Transform.Identity, .. up]));
            }

            if (Render.MeshOf(world, next) is not { IsValid: true } handle) continue;
            if (!TryMesh(handle, out var mesh) || mesh is null) return false;
            if (mesh.Topology != MeshTopology.Triangles) continue;

            var first = (uint)positions.Count;
            foreach (var point in mesh.Positions) positions.Add(Placed(point, up));

            if (mesh.Indices is { } own)
            {
                foreach (var index in own) indices.Add(first + index);
            }
            else
            {
                for (var index = 0u; index < mesh.Positions.Length; index++) indices.Add(first + index);
            }
        }

        if (positions.Count == 0) return false;

        drawn = new MeshData { Positions = [.. positions], Indices = [.. indices] };
        return true;
    }

    // A point carried up through transforms, the nearest first, each scaling, turning and moving it.
    private static Vec3 Placed(Vec3 point, Transform[] up)
    {
        foreach (var transform in up) point = transform.Translation + transform.Rotation * Times(point, transform.Scale);
        return point;
    }

    private static bool TryMesh(AssetHandle handle, out MeshData? mesh)
    {
        lock (Gate)
        {
            if (Meshes.TryGetValue(handle, out mesh)) return true;
        }

        if (!Render.TryReadMesh(handle, out mesh) || mesh is null) return false;

        lock (Gate) Meshes[handle] = mesh;
        return true;
    }

    private static (Vec3 Low, Vec3 High) Bounds(IReadOnlyList<Vec3> points)
    {
        if (points.Count == 0) return (Vec3.Zero, Vec3.Zero);

        var low = points[0];
        var high = points[0];
        foreach (var point in points)
        {
            low = new Vec3(MathF.Min(low.X, point.X), MathF.Min(low.Y, point.Y), MathF.Min(low.Z, point.Z));
            high = new Vec3(MathF.Max(high.X, point.X), MathF.Max(high.Y, point.Y), MathF.Max(high.Z, point.Z));
        }

        return (low, high);
    }

    private static Vec3 Times(Vec3 a, Vec3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

    private static Vec3 Abs(Vec3 value) => new(MathF.Abs(value.X), MathF.Abs(value.Y), MathF.Abs(value.Z));
}
