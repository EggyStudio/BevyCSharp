namespace Bevy.Physics;

/// <summary>The shape a <see cref="Collider"/> collides as.</summary>
public enum ColliderShape
{
    /// <summary>A box.</summary>
    Box,

    /// <summary>A ball as wide as the collider's widest side.</summary>
    Sphere,

    /// <summary>A cylinder with rounded ends, standing along Y, as a character is.</summary>
    Capsule,

    /// <summary>A cylinder standing along Y.</summary>
    Cylinder,

    /// <summary>The tightest convex shape around the mesh the entity is drawn with.</summary>
    Hull,

    /// <summary>
    /// The triangles of the mesh the entity is drawn with, for a static body such as a level's
    /// floor, since Bepu collides a moving body with a mesh only from outside.
    /// </summary>
    Mesh,
}

/// <summary>
/// Makes an entity a body of the simulation, with a <see cref="Collider"/> beside it saying its
/// shape.
/// </summary>
/// <remarks>
/// <para>
/// A component, so a level built in the editor holds its bodies the way it holds its lights, and a
/// scene file carries them. <see cref="PhysicsPlugin"/> makes the body once an entity has both
/// components, makes it again when either of them changes, or a static body's transform does, and
/// takes it away with them or with the entity, so a game neither adds nor removes the bodies a
/// level holds.
/// </para>
/// <para>
/// A body a game adds with <see cref="PhysicsWorld.Add"/> on an entity carrying these is left
/// alone, since one made in code is the game's to keep.
/// </para>
/// </remarks>
[Behavior]
public partial struct RigidBody
{
    /// <summary>How it moves.</summary>
    [Tooltip("Dynamic falls and is pushed, kinematic follows its transform, static never moves.")]
    public BodyKind Kind;

    /// <summary>Its mass, for a dynamic body, or zero for one.</summary>
    [Range(0, 1000), ShowIf(nameof(Kind), BodyKind.Dynamic)]
    public float Mass;

    /// <summary>Whether it only reports what touches it and stops nothing, as a trigger does.</summary>
    [Tooltip("Reports what touches it in ContactStarted and ContactEnded, and stops nothing.")]
    public bool Sensor;

    /// <summary>How much it grips what it slides on, or zero for the settings' own.</summary>
    [Range(0, 2), Foldout("Surface", Open = false)]
    public float Friction;

    /// <summary>How much of its speed it keeps bouncing off something, from none to all of it.</summary>
    [Range(0, 1), Foldout("Surface")]
    public float Bounce;
}

/// <summary>The shape a <see cref="RigidBody"/> collides as.</summary>
/// <remarks>
/// <para>
/// Sized in the entity's own units and scaled with its transform, so a cube stretched into a wall
/// has a wall's collider. A size left at zero takes the bounds of the mesh the entity is drawn
/// with, and of a unit cube where it is drawn with none, so putting a collider on what the editor
/// spawned fits it without a number typed. <see cref="Colliders.TryFit"/> says what it comes to.
/// </para>
/// <para>
/// <see cref="ColliderShape.Hull"/> and <see cref="ColliderShape.Mesh"/> are the drawn mesh itself,
/// and wait for it to have loaded.
/// </para>
/// </remarks>
[Behavior]
public partial struct Collider
{
    /// <summary>What it collides as.</summary>
    public ColliderShape Shape;

    /// <summary>Its full size along each axis before the entity's scale, or zero to fit the mesh.</summary>
    [Tooltip("Zero fits what the entity is drawn with. A sphere is as wide as the widest side, and a capsule or a cylinder stands along Y.")]
    public Vec3 Size;

    /// <summary>How far its middle is from the entity's origin, before the entity's scale.</summary>
    public Vec3 Offset;
}

/// <summary>
/// Makes a dynamic body a character, walked at the velocity a game asks for rather than pushed
/// about, which walls stop and slopes hold.
/// </summary>
/// <remarks>
/// <para>
/// Beside a dynamic <see cref="RigidBody"/> and its <see cref="Collider"/>, a capsule standing
/// along Y being the shape a character usually has. Before every step the body is walked toward
/// <see cref="Move"/> along the ground it stands on. It slides along a wall it meets rather than
/// sticking to it, since a character's contacts have no friction, rides over an edge lower than
/// about half its radius on the round of its foot, climbs a step up to <see cref="StepHeight"/>,
/// and stands still on a slope up to <see cref="MaxSlope"/> where it would slide off a steeper
/// one. It never turns over or falls asleep, and it pushes what is lighter, being a body like any
/// other.
/// </para>
/// <para>
/// The body stays upright whatever the entity's rotation, and the step writes back only where the
/// entity is, so a game turns its character to face the way it walks by setting the rotation,
/// which no step undoes.
/// </para>
/// <para>
/// A game writes <see cref="Move"/> and <see cref="Jump"/>, and reads <see cref="Grounded"/> and
/// <see cref="GroundNormal"/>, which each step writes back. Asked of a body that is not dynamic,
/// none of it does anything.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Behavior]
/// public partial struct Player
/// {
///     [OnUpdate]
///     public void Walk(BehaviorContext ctx, ref CharacterController character)
///     {
///         var x = (ctx.Input.KeyDown(Key.D) ? 1f : 0f) - (ctx.Input.KeyDown(Key.A) ? 1f : 0f);
///         character.Move = new Vec3(x * 4f, 0f, 0f);
///         if (character.Grounded &amp;&amp; ctx.Input.KeyPressed(Key.Space)) character.Jump = 5f;
///     }
/// }
/// </code>
/// </example>
[Behavior]
public partial struct CharacterController
{
    /// <summary>
    /// The velocity it walks at, in units a second, followed along the ground it stands on. The
    /// vertical part is ignored, since gravity and the ground decide how it falls.
    /// </summary>
    [Tooltip("How fast and which way it walks, followed along the ground. Up and down are left to gravity.")]
    public Vec3 Move;

    /// <summary>
    /// How fast it leaves the ground upward at the next step, if it stands on any then. Set back to
    /// zero by that step whether it jumped or not, so a jump asked for in the air is not saved up
    /// for the landing.
    /// </summary>
    [Unit("m/s")]
    public float Jump;

    /// <summary>The steepest ground it stands and walks on, in degrees, or zero for 45.</summary>
    [Range(0, 89), Unit("deg"), Tooltip("Steeper ground than this it slides off. Zero is 45 degrees.")]
    public float MaxSlope;

    /// <summary>The highest step it climbs onto walking into it, or zero for its radius.</summary>
    [Range(0, 2), Tooltip("The highest ledge it steps up onto. Zero is its radius.")]
    public float StepHeight;

    /// <summary>Whether it stood on ground no steeper than <see cref="MaxSlope"/> at the last step.</summary>
    [ReadOnly]
    public bool Grounded;

    /// <summary>Which way the ground under it faces, or straight up where it stands on nothing.</summary>
    [ReadOnly]
    public Vec3 GroundNormal;
}

/// <summary>What a <see cref="Collider"/> comes to on its entity, in world units.</summary>
/// <param name="Shape">The shape, ready for <see cref="PhysicsWorld.Add"/>.</param>
/// <param name="Kind">Which shape it is.</param>
/// <param name="Size">Its full size along each of the entity's axes, after the entity's scale.</param>
/// <param name="Center">Where its middle is from the entity's origin, in the entity's turned frame.</param>
public readonly record struct ColliderFit(PhysicsShape Shape, ColliderShape Kind, Vec3 Size, Vec3 Center);

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
    /// What an entity's collider comes to, or false while the mesh it fits has not loaded.
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

        // The mesh it is drawn with, or nothing for an entity drawn with none or a build with no
        // renderer, which a size given outright does not need.
        MeshData? mesh = null;
        var needsMesh = collider.Shape is ColliderShape.Hull or ColliderShape.Mesh || collider.Size == Vec3.Zero;
        if (needsMesh && App.HasRenderer && Render.MeshOf(world, entity) is { IsValid: true } handle)
        {
            if (!TryMesh(handle, out mesh)) return false;
        }

        if (collider.Shape is ColliderShape.Hull or ColliderShape.Mesh)
        {
            if (mesh is null || mesh.Positions.Length < 4) return false;

            var positions = mesh.Positions.Select(point => Times(point + collider.Offset, scale)).ToArray();
            var (low, high) = Bounds(positions);
            var shape = collider.Shape == ColliderShape.Hull
                ? PhysicsShape.Hull(positions)
                : PhysicsShape.Mesh(positions, mesh.Indices ?? [.. Enumerable.Range(0, positions.Length).Select(i => (uint)i)]);

            fit = new ColliderFit(shape, collider.Shape, high - low, (low + high) * 0.5f);
            return true;
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
