using Bevy;

namespace BevyCSharp.Examples.Transforms;

/// <summary>
/// The scene Bevy's transform examples share, a white cube at the middle seen from above and in
/// front, under a directional light.
/// </summary>
internal static class CubeScene
{
    /// <summary>Spawns the camera, the light and the cube, placed by <paramref name="at"/>, and returns the cube.</summary>
    public static Entity Spawn(EcsWorld ecs, Transform at)
    {
        var cube = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid), Render.CreateMaterial((1f, 1f, 1f, 1f)), at);
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 10f, 20f), Vec3.Zero, Vec3.UnitY));

        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = false });
        ecs.Add(sun, Transform.LookingAt(new Vec3(3f, 3f, 3f), Vec3.Zero, Vec3.UnitY));
        return cube;
    }
}
