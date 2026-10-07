using Bevy;
using Bevy.Physics;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The walls, floors and furniture of the zones that are drawn rather than played on, each a box
/// the player cannot walk through.
/// </summary>
/// <remarks>
/// Each block has a mesh of its own size, unscaled, and a static body whose collider is given the
/// same size, since a collider is sized in the entity's own units.
/// </remarks>
internal static class Blocks
{
    /// <summary>A static box of a size, turned about the vertical by a number of radians.</summary>
    public static Entity Static(EcsWorld ecs, string name, Vec3 center, Vec3 size, AssetHandle material, float turn = 0f)
    {
        var block = ecs.Spawn();
        Render.SetMesh(ecs, block, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
        Render.SetMaterial(ecs, block, material);
        ecs.Add(block, new Transform(center, Quat.FromRotationY(turn), Vec3.One));
        ecs.Add(block, new RigidBody { Kind = BodyKind.Static });
        ecs.Add(block, new Collider { Shape = ColliderShape.Box, Size = size });
        ecs.SetName(block, name);
        return block;
    }

    /// <summary>A static shape from a mesh, its collider fitted to the mesh's bounds.</summary>
    public static Entity Shape(EcsWorld ecs, string name, AssetHandle mesh, AssetHandle material, Transform at, ColliderShape shape = ColliderShape.Box)
    {
        var entity = ecs.Spawn();
        Render.SetMesh(ecs, entity, mesh);
        Render.SetMaterial(ecs, entity, material);
        ecs.Add(entity, at);
        ecs.Add(entity, new RigidBody { Kind = BodyKind.Static });
        ecs.Add(entity, new Collider { Shape = shape });
        ecs.SetName(entity, name);
        return entity;
    }
}
