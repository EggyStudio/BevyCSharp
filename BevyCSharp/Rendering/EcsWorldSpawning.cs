namespace Bevy;

/// <summary>
/// The members the rendering gives <see cref="EcsWorld"/>, which a game calls as the world's own,
/// spawning what Bevy's examples spawn in one bundle, a mesh with its material and its place, a
/// point light and a camera, each in one call as it is one bundle there.
/// </summary>
/// <remarks>
/// They extend <see cref="EcsWorld"/> rather than being a part of it, so the world names no type of
/// the rendering's, its meshes, materials, lights and cameras.
/// </remarks>
public static class EcsWorldSpawning
{
    extension(EcsWorld ecs)
    {
        /// <summary>
        /// Spawns an entity drawn with a mesh and a material, placed by a transform, as Bevy's
        /// <c>(Mesh3d(mesh), MeshMaterial3d(material), transform)</c> bundle does.
        /// </summary>
        /// <param name="mesh">A mesh from <see cref="Render.CreateMesh(string, float, float, float)"/> or a file.</param>
        /// <param name="material">A material from <see cref="Render.CreateMaterial(MaterialSettings)"/> or a file.</param>
        /// <param name="at">Where it is, which way it faces and how large it is.</param>
        /// <exception cref="Interop.BevyNativeException">This build has no renderer, or a handle names nothing.</exception>
        public Entity SpawnMesh(AssetHandle mesh, AssetHandle material, Transform at)
        {
            var entity = ecs.Spawn();
            ecs.Add(entity, at);
            Render.SetMesh(ecs, entity, mesh);
            Render.SetMaterial(ecs, entity, material);
            return entity;
        }

        /// <summary>
        /// Spawns a point light at a place, a million lumens reaching twenty units and casting no
        /// shadow unless asked, as Bevy's own point light is, bright enough at a camera's default
        /// exposure to light a room.
        /// </summary>
        /// <param name="at">Where the light is.</param>
        /// <param name="shadows">Whether it casts shadows.</param>
        /// <param name="intensity">How bright it is, in lumens.</param>
        /// <param name="range">How far it reaches, past which it lights nothing.</param>
        /// <param name="radius">How large the light is, which softens the edges of its shadows.</param>
        /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
        public Entity SpawnPointLight(Vec3 at, bool shadows = false, float intensity = 1_000_000f, float range = 20f, float radius = 0f)
        {
            var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = intensity, Range = range, Radius = radius, Shadows = shadows });
            if (light != Entity.None) ecs.Add(light, Transform.At(at.X, at.Y, at.Z));
            return light;
        }

        /// <summary>
        /// Spawns a 3D camera placed by a transform, with Bevy's defaults or the settings given.
        /// </summary>
        /// <param name="at">Where the camera is and which way it looks, as <see cref="Transform.LookingAt"/> gives it.</param>
        /// <param name="settings">How it projects and what it draws, or none for Bevy's defaults.</param>
        /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
        public Entity SpawnCamera3d(Transform at, CameraSettings? settings = null)
        {
            var camera = settings is null ? Render.SpawnCamera3d() : Render.SpawnCamera3d(settings);
            if (camera != Entity.None) ecs.Add(camera, at);
            return camera;
        }
    }
}
