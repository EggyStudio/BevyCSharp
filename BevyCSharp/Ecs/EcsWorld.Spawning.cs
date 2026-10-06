namespace Bevy;

// What Bevy's examples spawn in one bundle, a mesh with its material and its place, a point light
// and a camera, and a scene's entities walked from its root, each one call here as it is one there.
public sealed partial class EcsWorld
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
        var entity = Spawn();
        Add(entity, at);
        Render.SetMesh(this, entity, mesh);
        Render.SetMaterial(this, entity, material);
        return entity;
    }

    /// <summary>
    /// Spawns a point light at a place, as Bevy's default one is, a million lumens reaching twenty
    /// units and casting no shadow unless asked.
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
        if (light != Entity.None) Add(light, Transform.At(at.X, at.Y, at.Z));
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
        if (camera != Entity.None) Add(camera, at);
        return camera;
    }

    /// <summary>
    /// Every entity under <paramref name="root"/>, nearer ones first, as Bevy's
    /// <c>iter_descendants</c> walks them.
    /// </summary>
    /// <param name="root">The entity whose children, and theirs, are walked. It is not among them.</param>
    /// <remarks>
    /// Walked as it is read, so an entity spawned under one already walked past is not found, and
    /// one despawned before it is reached is not either.
    /// </remarks>
    public IEnumerable<Entity> Descendants(Entity root)
    {
        var queue = new Queue<Entity>(ChildrenOf(root));
        while (queue.Count > 0)
        {
            var entity = queue.Dequeue();
            yield return entity;
            foreach (var child in ChildrenOf(entity)) queue.Enqueue(child);
        }
    }
}
