namespace Bevy.Physics;

/// <summary>What a body collides as, in world units, centered on its entity.</summary>
/// <remarks>
/// The shape is not scaled by the entity's <see cref="Transform.Scale"/>, since a collision shape
/// is sized for what it collides as rather than for how the mesh drawn over it was modeled.
/// </remarks>
public readonly record struct PhysicsShape
{
    internal int Kind { get; private init; }
    internal Vec3 Size { get; private init; }
    internal Vec3[]? Positions { get; private init; }
    internal uint[]? Indices { get; private init; }
    internal Vec3 Offset { get; private init; }

    /// <summary>The same shape with its middle moved off the entity's origin.</summary>
    /// <param name="offset">Where its middle goes, in world units along the entity's own axes.</param>
    /// <remarks>
    /// The body turns about its middle while the entity keeps its origin, as a hull's does, so a
    /// collider fitted to a model whose origin is at its feet stands where the model does and
    /// writes the entity back to where its feet are.
    /// </remarks>
    public PhysicsShape Moved(Vec3 offset) => this with { Offset = Offset + offset };

    /// <summary>A box of the given full size along each axis.</summary>
    public static PhysicsShape Box(Vec3 size) => new() { Kind = 0, Size = size };

    /// <summary>A sphere of the given radius.</summary>
    public static PhysicsShape Sphere(float radius) => new() { Kind = 1, Size = new Vec3(radius, 0f, 0f) };

    /// <summary>
    /// A capsule standing along Y: a cylinder of <paramref name="length"/> with a half sphere of
    /// <paramref name="radius"/> at each end, the shape a character usually collides as.
    /// </summary>
    public static PhysicsShape Capsule(float radius, float length) => new() { Kind = 2, Size = new Vec3(radius, length, 0f) };

    /// <summary>A cylinder standing along Y.</summary>
    public static PhysicsShape Cylinder(float radius, float length) => new() { Kind = 3, Size = new Vec3(radius, length, 0f) };

    /// <summary>
    /// Triangles, as a level's floors and walls are, from positions and three indices a triangle.
    /// </summary>
    /// <remarks>
    /// For a static or kinematic body. A triangle collides from its front, the side its corners
    /// wind counterclockwise around as Bevy draws it, so a floor is solid from above and something
    /// below it passes up through. A dynamic mesh is allowed and costly, and its mass is spread as
    /// though the mesh were closed.
    /// </remarks>
    public static PhysicsShape Mesh(ReadOnlySpan<Vec3> positions, ReadOnlySpan<uint> indices)
    {
        if (indices.Length < 3 || indices.Length % 3 != 0)
            throw new ArgumentException("A mesh shape needs three indices a triangle, and at least one triangle.", nameof(indices));

        return new() { Kind = 4, Positions = positions.ToArray(), Indices = indices.ToArray() };
    }

    /// <summary>The same, from a mesh read back with <see cref="Render.TryReadMesh"/>.</summary>
    public static PhysicsShape Mesh(MeshData mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        var order = mesh.Indices ?? Enumerable.Range(0, mesh.Positions.Length).Select(index => (uint)index).ToArray();
        return Mesh(mesh.Positions, order);
    }

    /// <summary>
    /// The smallest convex shape holding every point, as a rock or an odd crate tumbles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a dynamic body whose shape is none of the simple ones, where a mesh shape would be
    /// slow to collide and wrong to tumble, since a mesh is a surface and a hull is a solid. The
    /// points are in the entity's own space, from its origin, and need not be on the hull, so a
    /// model's vertices serve as they are. Bepu builds the hull around its own center, and the
    /// body turns about that center while the entity keeps its origin, so the transform written
    /// back is where the entity's origin went. A joint's anchors on such a body are from the
    /// hull's center.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Fewer than four points, which hold no volume.</exception>
    public static PhysicsShape Hull(ReadOnlySpan<Vec3> points)
    {
        if (points.Length < 4)
            throw new ArgumentException("A hull needs at least four points, which is the fewest that hold a volume.", nameof(points));

        return new() { Kind = 5, Positions = points.ToArray() };
    }

    /// <summary>The same, around the vertices of a mesh read back with <see cref="Render.TryReadMesh"/>.</summary>
    public static PhysicsShape Hull(MeshData mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        return Hull(mesh.Positions);
    }
}
