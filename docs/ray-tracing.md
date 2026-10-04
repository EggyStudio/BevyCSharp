# Ray tracing

Bevy's ray-traced lighting, and rays a game traces through its scene itself.

### Ray-traced lighting

On a GPU with ray tracing hardware, Bevy's Solari lights the scene by tracing rays instead:

```csharp
config.RayTracedLighting = true;                  // when the app is made

Render.SetRayTracedLighting(camera, true);
Render.SetRayTraced(floor, floorMesh);            // every mesh the rays should meet
Render.SetRayTraced(wall, wallMesh);
```

Direct light comes from every light and every emissive surface, found by rays rather than shadow
maps, so a glowing screen lights the face in front of it, and indirect light is bounced off every
surface taking part, so a red wall tints the floor beside it and a lamp lights the corners it
cannot see. It builds up over a few frames and follows what moves. It needs a bridge built with it
(`./bcs build --editor --solari`) and an adapter with ray queries, which the bridge asks about
before turning it on and `Render.RayTracingActive` reports; turning it on makes every one of Bevy's
materials deferred for the whole app, which is why it is asked for when the app is made.
`SetRayTraced` reshapes the mesh in place the way ray tracing structures are built from, working out
tangents where it has none, so the entity keeps drawing it as before and the rays meet the
triangles the picture shows.

### Tracing rays of your own

A compute shader can trace rays against the same scene, for shadows, reflections or global
illumination of a package's own. It imports `bcs_ray` and compiles to SPIR-V, since Slang writes
ray queries for SPIR-V and not for WGSL:

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
way Solari does: the position this frame and the last, the normal with the normal map applied,
texture coordinates, and the material's color, emission, roughness and metalness with its textures
read. The acceleration structure, vertices, transforms, materials and lights are Solari's, declared
in group two and bound to the bind group Solari builds each frame, so they follow the scene without
a copy. It runs where ray-traced lighting runs, in a bridge built with `--solari`, an app that set
`Config.RayTracedLighting`, and on meshes given to `SetRayTraced`. A camera's own Solari lighting
stays off unless `SetRayTracedLighting` turns it on, so the rays can be the package's alone.

A ray scene of the game's own needs no Solari. It is built over a [geometry pool](compute.md#compute), each
pool mesh once, with entities in numbered slots placed where their transforms are every frame:

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

The sample carries ray-traced ambient occlusion built this way, with no Solari: a plane, a box and
a sphere standing in for its ground, cube and lamp in a ray scene, a compute shader tracing four
short rays from every surface on screen (`BevyCSharp.Sample/assets/shaders/rtao.slang`), and the
answer written into the occlusion Bevy's own lighting reads. F6 turns it on in a window, and
`./bcs command sample.rtao show` on a running sample paints the occlusion in place of the picture.

---

Before this, [Reflections and the sky](sky-and-reflections.md).
Next, [Images and the window](window.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#3d-rendering). The [guide's contents](../README.md#guide) list every page.
