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

The rays leave the light grainy, a few samples a pixel each frame, and the bridge filters the grain
away with a denoiser of its own, on wherever the lighting runs, since Bevy's denoiser is DLSS's,
which the bridge does not build. It is spatiotemporal variance-guided filtering. Each frame is added
to the pixel's history where the surface there is the same one, and what noise is left is filtered
in five passes that stop at edges, what the surface sends back by its color and its shine taken off
first and put back after, so a texture stays sharp. A still view is clean within a few frames. At
each of the feature test's views a filtered frame lies 0.9 to 4.9 levels of 255 from the mean of 256
grainy frames, where a grainy frame lies 5.8 to 14.4, and a camera strafing past a pillar at six
meters a second leaves no trail, the surfaces it uncovers 14 levels from their mean where a grainy
frame's are 31 (`python3 build/denoiser-readings.py`, on an RTX 4070 Laptop GPU). On that GPU it
takes two thirds of what the lighting does, 1.9 ms at 1920 by 1080 in Bevy's own Solari scene
(`./bcs command render.timings` after `./bcs open --example solari --offscreen --size 1920x1080`),
and 2.5 ms where every pixel is filtered, which `TheDenoiserKeepsToItsBudgetAt1080p` holds within
3.2 ms. Its four settings start at the defaults of the paper that describes it (Schied and others,
2017), the history's clamp in frames and how far a neighboring pixel may differ in depth, in the way
it faces and in brightness before the filter stops taking from it:

<!-- compiled with:
-->
```csharp
Render.SetRayTracedDenoiser(true, new DenoiserSettings
{
    HistoryLimit = 16,                            // follows a changing light sooner, a little grainier
    DepthWidth = 1f,
    NormalPower = 128f,
    LuminanceWidth = 4f,
});
Render.SetRayTracedDenoiser(false);               // Solari's own picture, for a capture beside
```

![A grainy frame of the Cornell box beside the denoiser's](../.github/assets/denoiser.webp)

`Render.RayTracedDenoiserActive` says whether it is filtering. Bevy's ReSTIR, which reuses samples
between pixels and frames, is on as well, where Bevy leaves it off for a denoiser, since it leaves
the denoiser less noise to take away. Measured before the denoiser, it halved the noise between
frames, for about a millisecond more of a laptop's GPU at 720p. Bevy's other settings for the
lighting, the light samples, the bounces and the world cache, are fields of the camera's
`SolariLightingRef`:

<!-- compiled with:
EcsWorld ecs = null!;
Entity camera = default;
-->
```csharp
var lighting = ecs.Wrap<SolariLightingRef>(camera);
lighting.MaxBounces = 2;                          // fewer bounces, a cheaper frame
lighting.Restir = false;                          // more grain left, for a quarter of the rays' time
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
    let size = passPictureSize();
    if (id.x >= size.x || id.y >= size.y) return;

    let depth = passLoadDepth(int2(id.xy));
    if (depth <= 0.0) { lit[id.xy] = 1.0; return; }

    let uv = (float2(id.xy) + 0.5) / float2(size);
    let from = passWorldPosition(uv, depth) + passLoadNormal(int2(id.xy)) * 0.01;
    lit[id.xy] = rayVisible(from, from + normalize(toward_sun) * 100.0) ? 1.0 : 0.0;
}
```

`rayTrace` answers the nearest triangle a ray meets, and `raySurfaceAt` unpacks it the
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
`rayTraceIn(scene, ...)` or `rayVisibleIn`. A hit's `instance` is the slot and its `mesh` is
the pool mesh, so an instance buffer or a material buffer with the same entities in the same slots
describes what was hit, and `poolCorner` reads the triangle. A mesh a compute shader
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
Render.SetSceneField(4, farCascade: 400f);            // and a far one reaching 400 meters ahead
```

It lies in cascades of 64 cells a side around the camera the view is drawn by, each twice as coarse
and as wide as the one before, so four cascades of a quarter meter reach 16 meters across in the
finest and 128 in the fourth. A distance is exact within four cells of a surface and held at four
beyond. Each cell is painted the color of its nearest surface, its material's base color times its
vertices' colors, with the way that surface faces, which global illumination reads a ray's hit by,
so a cascade takes 5 MB of the GPU's memory. The cascades sit a third of their width ahead of the
camera and move a few of their cells at a time as it moves. A far cascade after them, where asked,
has cells as wide as reaching its distance ahead of the camera takes, at least twice the last
counted cascade's, held to the camera's far plane, so a ray or a contact shadow that leaves the
counted cascades meets what lies far off.

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

The field darkens the sun's light too, each directional light's, where it closes a surface off from
the light, traced toward the light from a cell and a half out and softened by how near a surface the
trace passes, so a shadow is soft where what casts it is far and hard at its foot:

```csharp
Render.SetSceneFieldShadows(FieldShadows.PastShadowMap);   // the default, or Everywhere or Off
```

Past the shadow map's reach alone, by default, the ground beyond the map's last cascade is still
shadowed and the map's own shadows keep their edges, and everywhere the two darken a surface
together. Bevy's lighting takes no share of a light from outside, so the light Bevy gave a surface
is worked out again from the camera's G-buffer as Bevy works it out and taken back from the lit
picture before anything transparent is drawn. That needs the G-buffer and a picture in high dynamic
range, so while the shadows are on the camera the field lies around draws Bevy's materials deferred
and keeps its picture in floats. A surface's clearcoat and anisotropy, Bevy's atmosphere and a
light's texture are not worked out again, so where a surface has them the light taken back differs.

`Render.DrawSceneFieldWindow()`, called each frame inside a system while ImGui runs, draws the field
in a window of its own, with its settings, the contact shadows, a slice across X, Y or Z of a cascade
through the eye or a chosen cell, and what `gi.state` says. In the slice a point inside a mesh is
red, its surface black, and the open space gray in bands a cell apart, so a wall, a gap the field
closes and a mesh left out all show. The feature test's debug page turns it on, here at its gallery
seen from above in the second cascade, the Cornell box at the top and the wall of spheres beside
it:

![The scene field's window at the feature test's gallery](../.github/assets/scene-field.webp)

### Global illumination

Global illumination lights each surface with the light the others reflect onto it, a red wall's
color on the side of a white block beside it, traced each frame through the scene's distance field
on any GPU, as 3DEngine's radiance cascades trace it:

```csharp
Render.SetGlobalIllumination(GlobalIllumination.Medium);   // Off, the default, Low, Medium or High
```

Probes stand in cascades around the camera, eight a side in each of the field's cascades, each
tracing an octahedron of directions over a stretch of distance, the near light by many close probes
and the far light by fewer wide ones, and the cascades are merged from the last down. A ray that
meets a surface brings back the color the field painted the cell with times the sun's light, the
sixteen nearest point and spot lights' and the light that reached it the frame before, so every
frame adds a bounce, and a ray that meets nothing brings back the camera's ambient light, or the
world's. `Low` traces two cascades, `Medium` three and `High` four, finer in direction after the
first, and where the program asked for no field one is built at four cascades of a quarter meter.
Nothing is baked, so lights and meshes move freely.

Bevy's lighting takes no indirect light from outside, so a pass of the library's own after the
opaque surfaces takes back the diffuse share of the ambient light Bevy gave each surface and adds
the surface's diffuse color times the light the probes around it hold. Past the last cascade the
light from all around is the ambient light over a ground lit by the sun. The ambient light reaches
the probes as Bevy's lighting returns it from a surface facing the view, so an open floor reads as
it does without global illumination. As the contact shadows above do, the pass needs the G-buffer
and a picture in floats, so the camera draws Bevy's materials deferred while it is on. Bevy's
screen-space ambient occlusion and an environment map's light are left as Bevy gives them, so a
camera with an environment map has its light on top of the probes'.

In a Cornell box on an RTX 4070 Laptop GPU the probes take about 0.9 ms at `Low`, 1.2 at `Medium`
and 1.4 at `High`, and the pass 0.23 ms at its least at 1280 by 720 and 1.1 at its median.
`./bcs command gi.state` says what the probes hold, `gi.set` changes the quality on a running app,
and the feature test's graphics page has a row for it.

`./bcs command gi.reference shots/reference.png 256` path traces the camera's picture through the
GPU's rays where it has them, and `gi.compare shots/reference.png` says how far the picture lies
from it, region by region, with a picture of the difference beside it.

---

Before this, [Reflections and the sky](sky-and-reflections.md).
Next, [Images and the window](window.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#3d-rendering). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#drawing). The [guide's contents](../README.md#guide) list every page.
