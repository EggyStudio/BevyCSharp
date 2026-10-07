# Compute

Compute shaders dispatched from C#, on their own and on a camera, reading and writing the buffers
and images a game gives them.

A program with a compute stage runs on the GPU outside of any picture, over buffers
and images that stay on the GPU between frames, so what one dispatch writes the next reads, and a
material bound to the same buffer draws it:

<!-- compiled with:
public struct Boid { public Vec2 Position, Velocity; }
Boid[] boids = new Boid[256];
ShaderProgram look = default;
-->
```csharp
// Once.
var flock = Shaders.CreateBuffer<Boid>(boids);                   // a C# struct, as it is
var step = Shaders.CreateInstance(Shaders.CreateProgram(
        new ShaderProgramSettings { Compute = "shaders/boids.slang" }))
    .SetBuffer("boids", flock);

var drawn = Shaders.CreateMaterial(look).SetBuffer("boids", flock);

// Every frame, from a system: 64 boids to a workgroup.
Shaders.Dispatch(step.Set("delta", (float)ctx.Time.DeltaSeconds), (uint)(boids.Length + 63) / 64);
```

```slang
import bcs_compute;

struct Boid { float3 position; float3 velocity; };

RWByteAddressBuffer boids;
uniform float delta;

[shader("compute")]
[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    if (id.x >= bcs_compute::count<Boid>(boids)) return;

    var boid = bcs_compute::element<Boid>(boids, id.x);
    boid.position += boid.velocity * delta;
    bcs_compute::store(boids, id.x, boid);
}
```

A raw buffer read with `bcs_compute::element` packs a struct the way a C# struct with sequential
layout does, so the same struct declared on both sides agrees on every offset. A
`StructuredBuffer<T>` is laid out with the storage rules instead, where a `float3` takes sixteen
bytes, so a struct shared with one spells its padding out.

A dispatch is asked for from a system and runs once, that frame, before any camera draws, with the
instance's values as they were when it was asked for, and dispatches run in the order they were
asked for. A compute shader declares any number of buffers, images it writes (`RWTexture2D` or
`RWTexture3D` with a `[format(...)]`), textures it reads and samplers, all set by name.
`Shaders.CreateImage` makes an image in eight-bit, half and full float and integer formats, from
`Rgba8` to `Rgba32Float`, two- or three-dimensional, and an image a compute shader writes is an
ordinary texture to a material or a pass, so a compute shader can paint a water surface or a noise
field into something drawn. `Shaders.WriteImage<T>(image, texels, x, y, width, height)` writes a
region of one from memory, at any mip level and into any slices of a 3D image, before the frame's
work runs, so a texture streamer can upload a tile into its cache without making the image again.
That cache is usually block-compressed, so `ShaderImageFormat.Bc1`, `Bc4`, `Bc5`, `Bc7`, `Bc7Srgb`
and `Bc6hFloat` make images a shader samples and never writes, whose sides are whole four by four
blocks and whose regions are written a block at a time, at a quarter or an eighth of the memory the
texels would take. A buffer's size is fixed when it is made, and `WriteBuffer` replaces its contents
in place. `BeginBufferRead` copies one back, and `TryReadBuffer<T>` hands over the elements a frame
or two later, which any readback costs. `BeginImageRead` does the same for an image a shader wrote,
its texels row after row with nothing between the rows. A program's state stays `Compiling` until its compute
pipeline has been built, so a dispatch made once it is `Ready` runs rather than being dropped.

Atomics reach a buffer through Slang's `Atomic<T>`, as in `RWStructuredBuffer<Atomic<uint>>` and
`counter[0].add(1)`, because that is the form Slang turns into WGSL's atomics. `InterlockedAdd` on a
plain buffer does not compile for WGSL. Slang's wave operations run as WGSL subgroup operations on
an adapter with subgroups, which every desktop one has. That covers `WaveActiveSum`,
`WavePrefixSum`, `WaveGetLaneIndex`, `WaveReadLaneAt` and the rest of the arithmetic and ballot
family, from which a fast prefix sum or stream compaction is built. `WaveIsFirstLane` is the
exception, since the WGSL reader Bevy uses has no subgroup election yet, and
`WaveGetLaneIndex() == 0` says the same thing. `Shaders.DispatchIndirect(instance, buffer)` runs as
many workgroups as three unsigned integers in a buffer say, read on the GPU when the dispatch runs,
so one compute shader can count the work (the pixels that need tracing, the clusters that survived
culling) and the next runs exactly that much without the count crossing back to the CPU.

A buffer's size is fixed until `Shaders.GrowBuffer` makes it larger, which copies what it held on
the GPU into the start of a new one and has every material and shader instance holding it bind the
new one, so a list that outgrows its buffer does not have to be handed out again.

`Shaders.CreateInstanceBuffer(capacity)` makes a buffer the engine fills every frame with entities'
transforms, this frame's and the previous frame's, once transforms have been worked out.
`Shaders.SetInstance(buffer, slot, entity)` puts an entity in a slot. A shader reads it as a
`StructuredBuffer<bcs_scene::Instance>` after `import bcs_scene;`, and culling instances on the GPU,
voxelizing a scene, or giving geometry a shader placed its motion all build on it.
`Shaders.CreateMaterialBuffer(capacity)` does the same for what entities are made of. Each slot
holds the base color, emissive color, roughness, metallic and reflectance of the entity's standard
material, as `bcs_scene::Material`, written when they change. Put an entity in the same slot of both
and a shader reaching it by index has where it is and what light bouncing off it looks like, and a
GI ray shades its hit with that. Textures are not in it, and the base color is the factor the
material multiplies them by.

The triangles themselves go in a geometry pool, for a ray traced in a compute shader or a scene
voxelized by one, which meets whatever mesh is there:

<!-- compiled with:
AssetHandle rockMesh = default;
ShaderInstance trace = default;
-->
```csharp
var pool = Shaders.CreateGeometryPool();
var rock = Shaders.AddToGeometryPool(pool, rockMesh);            // 0, its number in the pool

trace.SetBuffer("vertices", pool.Vertices).SetBuffer("indices", pool.Indices).SetBuffer("meshes", pool.Meshes);
```

Every mesh added is appended to the same three buffers: its vertices as `bcs_scene::PoolVertex`,
its triangles' vertex numbers, and an entry in the mesh table, `bcs_scene::PoolMesh`, with where
they start, how many there are, and its bounds. `bcs_scene::pool_corner` reads a triangle's corner,
`intersect_triangle` finds where a ray meets it with the weights that interpolate the corners'
normals and texture coordinates, and `intersect_box` skips a mesh whose bounds the ray misses.
With the mesh's number in the same slot as the entity's transform and material, a hit has
everything shading it takes.

### Compute on a camera

Screen-space techniques are a chain of compute and passes over one camera's frame, each reading
what the camera drew and what the chain left behind last frame. A camera takes that chain as data:
images it owns, and compute shaders it runs at a point in its frame.

<!-- compiled with:
Entity camera = default;
-->
```csharp
ShaderInstance Compute(string file) => Shaders.CreateInstance(Shaders.CreateProgram(
    new ShaderProgramSettings { Compute = file }));

var occlusion = Compute("shaders/occlusion.slang");
var accumulate = Compute("shaders/accumulate.slang");
var composite = Shaders.CreateInstance(Shaders.CreateProgram(
    new ShaderProgramSettings { Pass = "shaders/composite.slang" }));

Shaders.SetPrepass(camera, depth: true, normals: true, motion: true);

Shaders.SetViewImages(camera,
    new ViewImage("occlusion", ShaderImageFormat.R16Float, Scale: 0.5f),
    new ViewImage("accumulated", ShaderImageFormat.R16Float, History: true),
    new ViewImage("depth_pyramid", ShaderImageFormat.R32Float, Mips: 6));

Shaders.SetViewDispatches(camera,
    ViewDispatch.PerPixel(occlusion, FramePoint.AfterPrepass, scale: 0.5f),
    ViewDispatch.PerPixel(accumulate, FramePoint.AfterPrepass));

Shaders.SetPasses(camera, composite);                       // reads "accumulated" by name
```

```slang
import bcs_pass;

[format("r16f")] RWTexture2D<float> accumulated;
Texture2D<float> accumulated_previous;
Texture2D<float> occlusion;
SamplerState linear;

[shader("compute")]
[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    let uv = bcs_pass::uv_of(int2(id.xy));
    let before = bcs_pass::previous_uv(uv, bcs_pass::load_motion(int2(id.xy)));
    let history = accumulated_previous.SampleLevel(linear, before, 0.0);
    accumulated[id.xy] = lerp(history, occlusion.SampleLevel(linear, uv, 0.0), 0.1);
}
```

An image a camera owns is made at a fraction of its picture, made again when the picture changes
size (starting over from zeros), and bound wherever a shader on that camera declares its name, as a
texture to read or a storage image to write. One made with `History: true` is two images that trade
places every frame, so `name_previous` holds what `name` held last frame, and one made with mips is
reachable a level at a time as `name_mip0`, `name_mip1` and on, so a depth pyramid can be built one
level from the last. Every camera has its own, however many there are. A dispatch or a pass must not
read and write the same image, which the GPU refuses, and history exists to avoid that.

An image can also be filled from the picture itself, at a point of the frame:

<!-- compiled with:
Entity camera = default;
-->
```csharp
Shaders.SetViewImages(camera, new ViewImage("lit", ShaderImageFormat.Rgba16Float, Scale: 0.5f,
    History: true, CopyAt: FramePoint.BeforeTonemapping));
```

The picture is drawn into it, scaled and point sampled, before that point's dispatches run, so
with history `lit_previous` is last frame's lit picture in its own units. That is the light a
screen-space GI ray that hits something on screen picks up, and what a temporal filter blends
toward, without a pass written only to copy it.

A shader on a camera also sees the scene's lights the way Bevy's own materials do. It has
`bcs_pass::directional_light_count()` and `lights.directional_lights[i]` with each light's color,
direction and shadow cascades, `point_light_count()` and `point_lights[i]` for point and spot
lights, while `directional_shadow` and `point_shadow` read Bevy's shadow maps with its filtering,
from zero in full shadow to one in full light, a spot light's included. `point_light_radiance` is
the light a point or spot light sends to a point before shadow, falling off with distance and, for a
spot, with the angle from its axis. A traced ray's hit is shaded with these, without drawing the
scene's lights a second time. The sky is there too. `environment_specular(direction, roughness)` is
the light the camera's environment map sends along a direction, blurred as a surface of that
roughness blurs it, and `environment_diffuse(normal)` what it sends a surface facing a way, both
black when `has_environment()` is false. A ray that leaves the scene picks up that light, turned and
scaled exactly as Bevy's own sky and lighting use the same map. And `blue_noise(pixel)` is Bevy's
spatio-temporal blue noise for this frame, four numbers from zero to one whose values are spread
evenly across the picture and from frame to frame. A technique taking a few random samples a pixel
picks them with it, so its noise blurs away rather than blotching.

What each pixel's surface is made of comes from Bevy's G-buffer. A camera asked for it with
`Shaders.SetPrepass(camera, depth: true, deferred: true)` draws Bevy's materials deferred, and a
shader on it declares `Texture2D<uint4> gbuffer;` and unpacks a texel with
`bcs_pass::surface_of`, which gives the base color, roughness, metallic, reflectance, emissive and
normal the way Bevy's own lighting reads them. That is the albedo a GI result is multiplied by and
the roughness a reflection trace chooses its rays from, without drawing the scene again to get
them. Only Bevy's materials are in it; one drawn by a Slang program is drawn forward and leaves its
pixels empty. With `previous: true` the camera keeps last frame's depth and G-buffer as well, as
`depth_previous` and `gbuffer_previous`, and comparing a pixel's surface now with what stood at the
same place last frame is how a temporal technique tells history it can reuse from a pixel that was
hidden until now.

With `pyramid: true` the camera builds Bevy's hierarchical depth, and a shader on it declares
`Texture2D<float> depth_pyramid;` and reads any level with `Load`. Each texel holds the farthest
depth of the ones under it, starting from the depth rounded down to a power of two, which a GPU
culling instances or clusters tests a box against. It turns Bevy's occlusion culling on for the
camera as well, since that builds it. A screen-space trace needs the nearest depth instead, which a
camera image with mip levels, built a level at a time, gives.

A dispatch on a camera runs every frame at one of four points: `AfterPrepass`, once depth, normals,
motion, shadows and Bevy's own ambient occlusion exist and before anything is lit; `AfterOpaque`, between opaque and transparent
geometry; and `BeforeTonemapping` or `AfterTonemapping`, ahead of the passes on the same side. Its
workgroups cover a fraction of the picture (`PerPixel`), are fixed (`Fixed`), or come from a buffer
(`Indirect`). It reads the same inputs a pass does through `import bcs_pass;`, and a compute shader
importing `bcs_compute` runs on a camera as well, reading only time.

**Drawing on a camera.** A program with a `DrawVertex` and a `DrawFragment` stage draws into a
camera's picture at a frame point, tested against its depth, out of buffers rather than a mesh.
Its vertex shader is handed no vertices, only `SV_VertexID` and `SV_InstanceID`, and places what is
drawn from what it declares, which is how particles a compute shader moves, clusters a culling pass
chose, or any number of instances whose count a buffer holds are drawn:

<!-- compiled with:
Entity camera = default;
AssetHandle positions = default, counts = default;
ShaderInstance clusters = default;
-->
```csharp
var sparks = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
{
    DrawVertex = "shaders/sparks.slang",
    DrawFragment = "shaders/sparks.slang",
})).SetBuffer("sparks", positions);

Shaders.SetViewDraws(camera,
    ViewDraw.Fixed(sparks, FramePoint.AfterOpaque, vertices: 6, instances: 1000, DrawBlend.Add),
    ViewDraw.Indirect(clusters, FramePoint.AfterOpaque, counts));   // counts a dispatch wrote
```

A draw's count is fixed or read from four unsigned integers in a buffer when it runs (vertices,
instances, first vertex, first instance), so a dispatch at the same point, which runs first, can
decide it. It blends as `Opaque`, `Alpha` or `Add`, and writes depth or only tests against it. It
reads the camera's inputs through `import bcs_pass;`, all but the picture, since it draws into that.

A draw can go into one of the camera's images instead of the picture. A visibility buffer is made
that way, as geometry drawn into an unsigned integer image, each pixel keeping which cluster and
triangle is nearest, for a later pass to shade.

<!-- compiled with:
Entity camera = default;
AssetHandle counts = default;
ShaderInstance clusters = default;
-->
```csharp
Shaders.SetViewImages(camera, new ViewImage("visibility", ShaderImageFormat.R32UInt, ClearEachFrame: true));
Shaders.SetViewDraws(camera, ViewDraw.Indirect(clusters, FramePoint.AfterPrepass, counts) with { Into = "visibility" });
```

The fragment shader returns what the image holds, a `uint` for an integer image. The draw is tested
against the camera's depth, and writes it if asked, where the image is the picture's size and the
camera draws once a pixel, so the geometry and Bevy's scene hide each other properly. An image made
with `ClearEachFrame` starts every frame as zeros, before anything on the camera runs, so zero is
"nothing drawn here". An integer image cannot be blended, so a draw into one replaces what is there.
`Targets = ["ids", "barycentrics"]` draws into several of the camera's images at once, one for each
of the fragment shader's outputs in order (`SV_Target0`, `SV_Target1` and on), for a visibility
buffer with more than an id, or a G-buffer of a package's own. The prepass's own `motion` and
`normals` can be targets too, for a draw at `AfterPrepass` on a camera that draws them. The pass
resolving a visibility buffer writes the motion and normals of what it resolved there, and its depth
through `SV_Depth`, so Bevy's temporal antialiasing and motion blur see that geometry move, and what
Bevy draws afterward is hidden behind it. A draw writing one of them reads a stand-in for it, since
nothing reads and writes the same texture in one pass.

`CastsShadows = true` draws it into the shadow maps as well, depth alone, after Bevy's own casters:
the camera's directional cascades, every spot light's map and every face of every point light's
cube, so it shadows Bevy's geometry and its own. Its vertex shader runs once for each of those views
with that view in `bcs_pass::view`, so a shader placing geometry from the view places it as the
light sees it without knowing it is drawing a shadow.

**Watching what a camera keeps.** The images a chain writes live on the GPU in formats a picture
cannot show, so `Shaders.Watch(camera, "occlusion", 320, 180, scale: 1f)` draws one, every frame
once the camera is done, into an ordinary eight-bit image, each value times a scale plus an offset:
one channel as gray, two as red and green, more as color. Anything a shader on the camera reads by
name can be watched, and the prepass's `depth`, `normals` and `motion`, and the `gbuffer`, whose
packed bits show as noise but show where it was drawn. Which of those a camera has depends on
settings made in several places, so `Shaders.DrawnViewImageNames(camera)` asks the renderer and
answers the names a watch would find, as of the last frame drawn. The editor's Frame tab lists the
scene camera's names, dims the ones it does not draw with what turns each on, and watches the one
picked, which is where a broken link in a chain shows.

**How long each pass takes.** An app made with `Config.GpuTimings` measures every render pass on the
CPU that records it and the GPU that runs it, and `Render.Timings()` answers the last frames' times,
smoothed. Bevy's own passes are there under Bevy's names, and every dispatch, pass and draw a shader
program makes is there as `shader` and its file's name, so a technique's passes sit in the same list
as the shadows and the tonemapping they are weighed against. The console and `./bcs` have it as
`render.timings`, slowest first. GPU times need timestamp queries, which Vulkan and DirectX 12 have;
elsewhere only CPU times arrive.

**Into Bevy's lighting.** Ambient occlusion is the one input to the lighting of Bevy's own materials
a chain can write so far. `Render.SetAmbientOcclusion(camera, AmbientOcclusionQuality.Low)` turns
Bevy's own on, and while it is on a compute shader on the camera sees the texture Bevy's lighting
reads as `ambient_occlusion`, so one run at `AfterPrepass` that writes it replaces Bevy's answer
with its own, and the ambient light (`Render.SetAmbientLight`) is darkened by it where a surface is
hemmed in. Diffuse light that varies through space goes in through an irradiance volume a compute
shader writes (see [Light probes](sky-and-reflections.md#light-probes)). Bevy's lighting has no per-pixel indirect input,
so a screen-space GI result is added to the picture instead, and done this way it lands where the
lighting would have put it: a draw of one triangle over the whole picture at `AfterOpaque`, blended
`Add`, whose fragment shader returns the GI result times the surface's color from the `gbuffer`.
That is after opaque geometry is lit and before transparent geometry, bloom and tonemapping, so
glass in front is drawn over it and the tonemapper sees it as light like any other. With the
camera's ambient light off (`Render.SetAmbientLight(camera, (0, 0, 0), 0)`) it replaces ambient
rather than adding to it. [.github/RENDERING.md](../.github/RENDERING.md) has what a true input inside
Bevy's lighting would add.

The sample carries a small screen-space GI written this way, as a reference for how the pieces
fit rather than a technique to ship: `BevyCSharp.Sample/Behaviors/ScreenSpaceLight.cs` and the three
`gi_*.slang` files beside the sample's other shaders. F5 turns it on in a window, and
`./bcs command sample.gi "on 4"` on a running sample turns it on with the bounce exaggerated four
times, which is how its share of the picture is told apart from the rest.

A storage image may be declared in any format the adapter can write, `[format("r16f")]` included,
although core WGSL has fewer. Slang writes the nearest core format and the bridge puts the declared
one back from Slang's reflection. An image the shader only writes is bound write-only, which more
formats allow than reading and writing at once. Slang itself refuses a `RWTexture2D` in a format
WGSL cannot read and write, such as `rgba16f`, and its `WTexture2D`, written with `Store`, is the
write-only image for those.

---

Before this, [Shaders](shaders.md).
Next, [Cameras and light](cameras-and-light.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#shaders). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#shaders). The [guide's contents](../README.md#guide) list every page.
