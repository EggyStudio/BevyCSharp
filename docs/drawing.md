# Drawing

Meshes and materials can be built without an asset file, and attached to an entity to make it
drawable. This needs a render build; on a headless one every call here refuses and says which build
would support it, rather than silently doing nothing. Guard with `App.HasRenderer` to write one
behavior that runs either way, as `games/FeatureTest/Behaviors/Scene.cs` does.

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

`MeshShape` names Bevy's primitives, what the three numbers mean depending on the shape, as each
constant's documentation says. `MeshShape.Ring` makes a band along the inside of a flat shape's
outline and `MeshShape.Extrusion` pushes a flat shape out into a solid, so
`MeshShape.Extrusion(MeshShape.Ring(MeshShape.Rectangle, 0.1f))` is a square tube, its walls a
tenth thick. `Render.CreateExtrusion` pushes out a convex polygon from corners the game gives.

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
a mesh's shadow casting and receiving off, draw it alone rather than batched with the meshes it
shares a mesh and a material with, and leave it to the GPU's culling alone, and
`Render.SetCameraFlags` turns a camera's indirect drawing and CPU culling off, the paths Bevy draws
a crowd quickly by, which a stress test turns off to measure what they save.

A camera with Bevy's `OcclusionCulling` and a depth prepass also leaves out on the GPU what the last
frame's depth says is hidden, as behind a wall. The GPU writes what is left into the draws
themselves, so the CPU never learns how much was culled, and `Render.CountDrawnMeshes(true)` reads
the opaque pass's draws back, as Bevy's occlusion_culling example does, for a count a frame or two
old:

<!-- compiled with:
Entity camera = default, status = default;
-->
```csharp
ctx.Ecs.Insert<DepthPrepassRef>(camera);
ctx.Ecs.Insert<OcclusionCullingRef>(camera);
Render.CountDrawnMeshes(true);

if (Render.DrawnMeshes(out var drawn) == DrawnMeshCount.Known)
    Ui.SetText(status, $"{drawn} meshes drawn");
```

Counting costs two copies and a readback a frame, so it is asked for. `DrawnMeshes` says too why
there is no count, for one still on its way, a GPU whose culling cannot be counted, or one that does
not cull, where every mesh in view is drawn.

A vertex can carry a value of the game's own beside its position, a stream as Bevy's
`custom_vertex_attribute` carries one, for a vertex shader of the program's own to read, such as a
voxel's face and texture packed into four bytes, a blend weight, a wind phase. Each stream is a
name, one of five formats (`UInt`, `UInt2`, `Float`, `Float4` and `UNorm8x4`, four bytes read as 0
to 1), a location from 8 to 15, and its values:

<!-- compiled with:
using System.Numerics;
Vec3[] corners = [];
uint[] packed = [];
-->
```csharp
var chunk = Render.CreateMesh(new MeshData
{
    Positions = corners,
    Streams = [MeshStream.From<uint>("Voxel", MeshStreamFormat.UInt, 8, packed)],
});
```

A name is one stream for the running app, so every mesh carrying it carries it in one format at one
location, and a name given again in another shape is refused. Positions stay, since Bevy places and
culls a mesh by them. `ComputeNormals = false` leaves a mesh given no normals without them, for a
shader that unpacks its normal from the stream, so a position and four packed bytes make a vertex
sixteen bytes. Such a mesh draws and casts its shadow, and a lit material of Bevy's own on it reads
a normal of zero. The shader declares the stream at its location as the
[shaders guide](shaders.md#a-vertex-stream-of-the-games-own) shows. A scene file keeps a mesh's
streams with it.

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
a mesh was cut and which level of detail is drawn where. Its seed, `CreateClusterMaterial(3)`, is
mixed into the colors, so meshes side by side can be told apart, and a mesh keeps its colors
whatever else the world holds.

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
