# Drawing

Meshes and materials can be built without an asset file, and attached to an entity to make it
drawable. This needs a render build; on a headless one every call here refuses and says which build
would support it, rather than silently doing nothing. Guard with `App.HasRenderer` to write one
behavior that runs either way, as `BevyCSharp.Sample/Behaviors/Scene.cs` does.

```csharp
var camera = Render.SpawnCamera3d();
ctx.Ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 6f, 12f), Vec3.Zero, Vec3.UnitY));

Render.SpawnLight(LightKind.Directional, 10_000f);

var mesh = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
var material = Render.CreateMaterial(0.25f, 0.55f, 0.85f);

var entity = ctx.Ecs.Spawn();
Render.SetMesh(ctx.Ecs, entity, mesh);
Render.SetMaterial(ctx.Ecs, entity, material);
```

Handles are references, so one mesh and one material can be shared by any number of entities.
Attaching a mesh goes through Bevy's own insert rather than a byte copy, which pulls in the
components Bevy requires alongside it, so an entity needs nothing further to be drawn.

### A mesh of your own

A shape no primitive describes is built from its vertices, in any
profile, since a mesh is data until something draws it:

```csharp
var ramp = Render.CreateMesh(new MeshData
{
    Positions = [new(0f, 0f, 0f), new(4f, 0f, 0f), new(4f, 1f, -2f), new(0f, 1f, -2f)],
    Uvs = [0f, 1f, 1f, 1f, 1f, 0f, 0f, 0f],             // two floats a vertex
    Indices = [0, 1, 2, 0, 2, 3],
});
```

A triangle mesh given no normals has them worked out, smooth where it is indexed and flat where it
is not. `Topology` also takes lines, points and strips. A mesh whose vertices a shader moves far
from where they were built, such as ten thousand squares a vertex shader places from a buffer, needs
`Render.SetMeshFlags(ctx.Ecs, entity, MeshFlags.NoFrustumCulling)`, because Bevy culls by the bounds
it worked out from the mesh and those say nothing about where the shader put it. The same flags turn
a mesh's shadow casting and receiving off.

### Meshlets

A mesh of millions of triangles can be drawn as Bevy's meshlets instead, which cut it into small
clusters that the GPU culls and picks a level of detail for, so it costs what the triangles
covering the screen cost rather than what the mesh holds:

<!-- compiled with:
Entity entity = default;
AssetHandle marble = default;
-->
```csharp
var config = Config.Default;
config.MeshletClusters = 1 << 22;                   // room for this many clusters at once

// Later, in a system:
var statue = Render.CreateMeshletMesh(AssetServer.Load(AssetKind.Mesh, "statue.glb#Mesh0/Primitive0"));
Render.SetMeshletMesh(ctx.Ecs, entity, statue);
Render.SetMaterial(ctx.Ecs, entity, marble);
```

They need a bridge built with them (`./bcs build --editor --meshlet`) and a GPU with 64-bit texture
atomics on Vulkan or Metal. The bridge asks the GPU before turning them on and runs without them
where it cannot, which `Render.MeshletsActive` reports. Cutting a mesh into clusters takes seconds
for a large one, so `CreateMeshletMesh` does it on a worker once the mesh has loaded, and the
handle it answers draws nothing until then. The mesh has to be indexed triangles with texture
coordinates. A meshlet mesh is drawn with a standard material, and while meshlets run every camera
draws once a pixel, since Bevy's meshlet renderer cannot draw a multisampled picture.
`Render.CreateClusterMaterial()` instead draws each cluster in a color of its own, which shows how
a mesh was cut and which level of detail is drawn where.

Converting is the slow part, so a game does it once. `saveTo:` writes the finished mesh as a file
under the asset root, and that file loads as fast as it reads:

<!-- compiled with:
AssetHandle statueMesh = default;
-->
```csharp
Render.CreateMeshletMesh(statueMesh, saveTo: "baked/statue.meshlet_mesh");   // once, in a tool

var statue = AssetServer.Load(AssetKind.MeshletMesh, "baked/statue.meshlet_mesh");   // after
```

---

Before this, [Assets and models](assets-and-models.md).
Next, [Materials and textures](materials.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#3d-rendering). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#drawing). The [guide's contents](../README.md#guide) list every page.
