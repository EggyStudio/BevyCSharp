# Ray tracing

Bevy's ray-traced lighting, and rays a game traces through its scene itself.

### Ray-traced lighting

On a GPU with ray tracing hardware, Bevy's Solari lights the scene by tracing rays instead:

<!-- compiled with:
Config config = new();
Entity camera = default, floor = default, wall = default;
AssetHandle floorMesh = default, wallMesh = default;
-->
```csharp
config.RayTracedLighting = true;                  // when the app is made

Render.SetRayTracedLighting(camera, true);
Render.SetRayTraced(floor, floorMesh);            // every mesh the rays should meet
Render.SetRayTraced(wall, wallMesh);
```

Direct light comes from every light and every emissive surface, found by rays rather than shadow
maps, so an emissive screen lights the face in front of it, and indirect light comes from every
surface taking part, so a red wall tints the floor beside it and a lamp lights the corners it
cannot see. It builds up over a few frames and follows what moves. It needs a bridge built with it
(`./bcs build --editor --solari`) and an adapter with ray queries, which the bridge asks about
before turning it on and `Render.RayTracingActive` reports; turning it on makes every one of Bevy's
materials deferred for the whole app, which is why it is asked for when the app is made.
`SetRayTraced` reshapes the mesh in place the way ray tracing structures are built from, working out
tangents where it has none, so the entity keeps drawing it as before and the rays meet the
triangles the picture shows. A mesh with no texture coordinates, as a model drawn in plain colors
often is, is given coordinates of zero, so every mesh of a loaded model can be handed over.

Bevy's ReSTIR, which reuses samples between pixels and frames, is on. Bevy leaves it off for a
denoiser to clean the picture instead, and its denoiser is DLSS's, which the bridge does not build,
so without ReSTIR the picture stays grainy. With it the noise between frames halves, for about a
millisecond more of a laptop's GPU at 720p. Bevy's other settings for it, the light samples, the
bounces and the world cache, are fields of the camera's `SolariLightingRef`:

<!-- compiled with:
EcsWorld ecs = null!;
Entity camera = default;
-->
```csharp
var lighting = ecs.Wrap<SolariLightingRef>(camera);
lighting.MaxBounces = 2;                          // fewer bounces, a cheaper frame
lighting.Restir = false;                          // the grain back, for a quarter of the rays' time
```

Bevy's path tracer draws a camera's whole picture from rays, following each pixel's light to its
end with no rasterizing and adding every frame's samples to the last, so a still view clears up over
seconds into the reference the lighting above is weighed against. It is for comparing rather than
for play, and an app asks for it beside the lighting:

<!-- compiled with:
Config config = new();
EcsWorld ecs = null!;
Entity camera = default, wall = default;
AssetHandle wallMesh = default;
-->
```csharp
config.RayTracedLighting = true;                  // when the app is made
config.PathTracer = true;

Render.SetPathTraced(camera, true);               // in the lighting's place
Render.SetRayTraced(wall, wallMesh);
ecs.Wrap<Mesh3dRef>(wall).Remove();              // drawn by the rays alone
```

Its rays meet only the meshes given to `SetRayTraced`, and what is rasterized under its picture is
drawn for nothing, so Bevy's solari example takes the raster meshes away, as the last line does.
Solari's passes and the cells its world cache holds are among Bevy's diagnostics,
`render/solari_lighting/lighting/elapsed_gpu` and `render/solari_lighting/world_cache_active_cells_count`
among them, which `Diagnostics.TryRead` reads where `Config.GpuTimings` is on.

### Tracing rays of your own

A compute shader can trace rays against the same scene, for shadows, reflections or global
illumination of a package's own. It imports `bcs_ray` and compiles to SPIR-V, since Slang writes
ray queries for SPIR-V and not for WGSL:

<!-- compiled with:
using System.Numerics;
Entity camera = default;
-->
```csharp
var shadows = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
{
    Compute = "shaders/trace_sun.slang",
    ComputeTarget = ShaderTarget.SpirV,
})).Set("toward_sun", new Vector3(0.3f, 1f, 0.2f));

Shaders.SetPrepass(camera, depth: true, normals: true);
Shaders.SetViewImages(camera, new ViewImage("lit", ShaderImageFormat.R16Float));
Shaders.SetViewDispatches(camera, ViewDispatch.PerPixel(shadows, FramePoint.AfterPrepass));
```

```slang
import bcs_pass;
import bcs_ray;

uniform float3 toward_sun;
[format("r16f")] RWTexture2D<float> lit;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    let size = bcs_pass::picture_size();
    if (id.x >= size.x || id.y >= size.y) return;

    let depth = bcs_pass::load_depth(int2(id.xy));
    if (depth <= 0.0) { lit[id.xy] = 1.0; return; }

    let uv = (float2(id.xy) + 0.5) / float2(size);
    let from = bcs_pass::world_position(uv, depth) + bcs_pass::load_normal(int2(id.xy)) * 0.01;
    lit[id.xy] = bcs_ray::visible(from, from + normalize(toward_sun) * 100.0) ? 1.0 : 0.0;
}
```

`bcs_ray::trace` answers the nearest triangle a ray meets, and `bcs_ray::surface_at` unpacks it the
way Solari does, the position this frame and the last, the normal with the normal map applied,
texture coordinates, and the material's color, emission, roughness and metalness with its textures
read. The acceleration structure, vertices, transforms, materials and lights are Solari's, declared
in group two and bound to the bind group Solari builds each frame, so they follow the scene without
a copy. It runs where ray-traced lighting runs, in a bridge built with `--solari`, an app that set
`Config.RayTracedLighting`, and on meshes given to `SetRayTraced`, once the scene holds a light,
which Solari waits for before it binds the scene. A camera's own Solari lighting stays off unless
`SetRayTracedLighting` turns it on, so the rays can be the package's alone.

A ray scene of the game's own needs no Solari. It is built over a [geometry pool](compute.md#compute), each
pool mesh once, with entities in numbered slots placed where their transforms are every frame:

<!-- compiled with:
AssetHandle rockProxyMesh = default;
Entity boulder = default;
ShaderInstance occlusion = default;
-->
```csharp
var pool = Shaders.CreateGeometryPool();
var rock = Shaders.AddToGeometryPool(pool, rockProxyMesh);    // what the rays meet, not what is drawn

var scene = Shaders.CreateRayScene(pool, capacity: 1024);
Shaders.SetRaySceneInstance(scene, slot: 0, boulder, rock);   // follows the entity as it moves

occlusion.SetRayScene("scene", scene);
```

The shader declares `RaytracingAccelerationStructure scene;` and traces it with
`bcs_ray::trace_in(scene, ...)` or `visible_in`. A hit's `instance` is the slot and its `mesh` is
the pool mesh, so an instance buffer or a material buffer with the same entities in the same slots
describes what was hit, and `bcs_scene::pool_corner` reads the triangle. A mesh a compute shader
deforms in the pool is traced as it was until `Shaders.RebuildRayScene(scene, mesh)` builds it
again. `Shaders.SupportsRayQueries`
says whether the device can build one, which takes ray tracing hardware on Vulkan.

SPIR-V passed through reaches the driver without the checks WGSL gets, so a shader reading past a
buffer's end reads whatever is there. The bridge builds the layout from Slang's reflection, so
values are still set by name. The target works for any compute shader, whether or not it traces
rays, and on a backend other than Vulkan the SPIR-V is translated by naga, which reads ordinary
compute and not ray queries.

The feature test carries ray-traced ambient occlusion built this way, with no Solari. A plane, a box
and a sphere stand in for its ground, cube and lamp in a ray scene, a compute shader traces four
short rays from every surface on screen (`games/FeatureTest/assets/shaders/rtao.slang`), and the
answer is written into the occlusion Bevy's own lighting reads. Its panel's graphics page turns it
on, and `./bcs command feature.rtao show` on a running one paints the occlusion in place of the
picture.

### The scene's distance field

A distance field of the scene around the camera holds, for each cell of a grid, how far its middle
is from the nearest surface of the meshes that cast shadows, below zero inside a mesh that culls its
back faces. A ray steps through it by the distance each point holds, never past a surface, on any
GPU, which is how 3DEngine's global illumination traces its rays, and the field is the first of that
illumination's three parts:

```csharp
Render.SetSceneField(4, cellSize: 0.25f, budget: 1);  // four cascades, a quarter meter the finest
```

It lies in cascades of 64 cells a side around the camera the view is drawn by, each twice as coarse
and as wide as the one before, so four cascades of a quarter meter reach 16 meters across in the
finest and 128 in the fourth, each cascade 1 MB of the GPU's memory twice over. A distance is exact
within four cells of a surface and held at four beyond. The cascades sit a third of their width
ahead of the camera and move a few of their cells at a time as it moves.

A mesh drawn in the same place for eight frames is built into the cascades around it from its
triangles on the GPU, and one that moves, or is skinned, is stamped each frame as the box around
it, a frame behind it. A cascade is built again where the camera has gone past it or a mesh came
or went, as many a frame as the budget allows, the finest first, so a scene that changes a lot takes
a larger budget and a slow GPU a smaller one. A closed wall thinner than a cell, or a mesh drawn
with both its faces, is held half a cell thick on either side of its middle, so a ray between the
cells on either side of it still meets it, and a tunnel one cell wide stays open along its middle.

The library builds it with compute shaders of its own, compiled as a game's are, so the first run on
a machine with `slangc` fills the shader cache a shipped game reads. The debug overlay's renderer
lines say how many cascades are built and what it holds, and `./bcs command gi.state` says where
each cascade lies, what the frame built and stamped, and the memory it takes.

---

Before this, [Reflections and the sky](sky-and-reflections.md).
Next, [Images and the window](window.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#3d-rendering). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#drawing). The [guide's contents](../README.md#guide) list every page.
