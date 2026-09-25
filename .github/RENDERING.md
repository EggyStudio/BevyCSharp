# Rendering for the high end

What the engine has to offer so that virtualized geometry, texture streaming, screen-space and
world-space global illumination, ambient occlusion and, later, reflections and radiance cascades
can be built on it as ordinary code. The README describes what exists; this is what has to exist,
why, and in what order.

## What this is for

The kind of package this engine should host already exists for Unity. NanoTech renders geometry
the way Nanite does and streams textures as pages. HTrace computes ambient occlusion, global
illumination in screen space, and global illumination in world space from voxels, a radiance cache
and hardware ray queries. Their authors are doing the hard part, the graphics research, and this
document does not repeat any of their work. It looks at what those packages ask of an engine, and
at what they had to build around because Unity would not give it to them, and turns both into
requirements.

Most of the effort in a package like that goes into getting data out of the engine and results back
into it. The GI package copies the pipeline's own compute shaders, rewrites their text and swaps
them in through reflection, only to put its result into the texture the pipeline already reads
indirect diffuse from. On the lighter pipeline it has no such texture at all, so it subtracts the
engine's ambient term from the lit picture and adds its own. The geometry package draws a quad in
front of every camera so that the pipeline's own lighting shades what it rasterized, and packs
integer ids into a float render target because that is the render target it can have.

None of that is research. It is the cost of an engine whose renderer is closed at the points where
these techniques need to reach in. This engine is not closed anywhere. The C# side, the Rust bridge
and Bevy's own renderer are all ours to shape, so every one of those workarounds can instead be a
hook, documented and tested, the way Slang made a shader's layout the shader's own business rather
than a table to fit into.

## The techniques and what they consume

### Virtualized geometry

A mesh is baked offline into clusters of a bounded size, the clusters are grouped and simplified
repeatedly into a hierarchy whose every level carries an error bound, and the result is written as
pages that stream. At run time the GPU walks that hierarchy per instance and per view, picks the
clusters whose error is under a pixel, and rasterizes only those.

What it needs from an engine:

- **An offline processing step with a place to put its output.** Clustering, graph partitioning,
  quadric simplification with locked group borders, bounding spheres, a monotonic error per level,
  quantized positions and octahedral normals, bit-packed into pages. The engine's side of that is
  an asset type holding the result, a loader, and a way to run the bake as part of importing.
- **Persistent GPU buffers of a size decided at run time, and that grow.** A page pool, a residency
  hash from (object, cluster) to where it lives, instance transforms, per-frame cluster lists.
  Growing one without losing its contents is a copy the engine should do rather than the package.
- **GPU feedback and IO.** The GPU writes which pages it wanted; a readback brings them to the CPU a
  frame or two later, a worker thread reads and inflates them, and an upload scatters them into
  pool slots. Eviction is least recently used, and the coarsest level of every object stays
  resident so something is always drawable.
- **A depth pyramid.** Occlusion culling tests bounds against a hierarchical depth buffer built
  with a single-pass downsampler, which means writing many mip levels of one texture from one
  dispatch. Two-pass occlusion culls against last frame's pyramid, draws what survived, rebuilds
  the pyramid, and tests what was rejected again.
- **GPU-driven work.** Culling runs as compute over persistent work queues or level by level with
  indirect dispatch; the number of clusters and triangles drawn is decided on the GPU and never
  seen by the CPU. Rasterizing is an indirect procedural draw that pulls vertices from the pool, or
  a compute rasterizer that writes depth and ids with atomics.
- **A visibility buffer and a material pass.** Depth plus a triangle and cluster id per pixel, then
  one pass per material that reconstructs the triangle, its barycentrics and their derivatives,
  shades, and writes depth and motion so the rest of the frame treats the result like any other
  geometry.
- **Previous transforms per instance.** Without them there are no object motion vectors, and every
  temporal effect downstream smears.
- **Shadow views as views.** A shadow map is another view to cull and rasterize for, which the
  engine has to expose as data rather than as a camera.

Bevy ships an implementation of this, `MeshletPlugin` behind the `meshlet` and `meshlet_processor`
features. It needs 64-bit texture atomics, runs on Vulkan and Metal, refuses multisampling and
wants one opaque rendering method for the whole app. It is the first thing to offer, and the
requirements above are what a package doing it differently would need beyond it.

### Texture streaming

A texture is split into tiles per mip level. The GPU reports which tiles it sampled, the CPU loads
them, and a page table says where each one lives in a physical cache texture.

What it needs:

- **A feedback path.** Either a pixel shader writing requested tiles into a buffer, or a compute
  pass reconstructing them from a visibility buffer. Both need a storage buffer writable from the
  shading stage or from compute over the view.
- **A page table and a physical cache that the GPU manages.** Allocation, ages and eviction can all
  run in compute, leaving the CPU only the IO.
- **Tile upload without a texture per tile.** A write into a region of a large texture, from bytes
  that arrived on a worker thread.
- **Sampling in the material.** A function a material calls instead of `Sample`, which walks mips
  until a resident page is found and falls back to a low-resolution atlas that is always resident.
  In Slang that is a module the material imports.
- **Compressed tiles.** Block-compressed formats keep the cache small, which needs the formats and
  a copy of compressed blocks into a region.

### Ambient occlusion

Horizon-based or visibility-bitmask occlusion traced in screen space, at full, half or quarter
resolution, then accumulated over time and filtered in space. Ray-traced occlusion is the same
pipeline with a different trace.

What it needs:

- **Depth, a depth pyramid, normals.** Normals come from a prepass when the renderer is forward,
  and the package should never have to redraw the scene to get them.
- **Motion vectors for camera and objects**, and a way to tell a pixel that belongs to something
  moving, so history is rejected where it would ghost.
- **History that belongs to the camera.** Last frame's result, sample counts and a packed depth and
  normal, sized to the view, surviving from frame to frame, and made again when the view changes
  size. Every camera has its own, however many there are.
- **Previous view matrices and camera positions.**
- **Blue noise.**
- **A way into lighting.** Ambient occlusion is worth most when the renderer applies it to indirect
  light, and optionally to direct light and specular, rather than when it darkens the finished
  picture. That needs a supported input the lighting reads.

### Screen-space global illumination

Rays marched in screen space against the depth pyramid, hits shaded from last frame's lit picture,
resampled with ReSTIR, then denoised in time and space and upscaled.

What it needs, beyond what ambient occlusion needs:

- **Last frame's lit color**, before post-processing, ideally with mips.
- **Albedo and a metallic or specular term per pixel**, to separate diffuse from specular and to
  multiply the result by the surface color. In a forward renderer that is a thin G-buffer the
  prepass can write.
- **Compaction and indirect dispatch.** Checkerboarding and adaptive resolution decide on the GPU
  which pixels trace, then dispatch exactly that many.
- **Global reductions.** Luminance moments over the whole frame, computed with atomics and read by
  the next pass on the GPU.
- **A fallback for rays that leave the screen**, which is the sky or a probe volume, so both have to
  be readable from compute.
- **An indirect diffuse input to lighting**, so the result replaces ambient light rather than being
  composited on top of a picture that already has some.

### World-space global illumination

Rays that screen space cannot answer are traced against the world. That takes either voxels (a
volume following the camera, written by rasterizing the scene with atomics or by stamping voxels
baked per mesh, with an occupancy pyramid for skipping empty space) or hardware ray queries against
an acceleration structure. Hits are shaded against the lights, and a world-space radiance cache,
a hash of cells at several scales, keeps the answers so each frame traces only a fraction.

What it needs:

- **3D textures written with atomics, and their mips.** Integer formats, storage access to each mip
  level, and a scrolling update so moving the volume does not revoxelize it.
- **Voxelization.** Either a way to draw the scene into no render target with a fragment shader
  writing storage (with a geometry or vertex stage choosing the projection axis), or a per-mesh bake
  and a GPU stamp. The second needs only compute and is the one that fits wgpu.
- **The lights, as data.** A list of every light with its color, range and shadow map index, and
  the shadow maps themselves, readable from compute. A package should never have to match lights by
  position against an engine buffer.
- **The sky**, as a cube map or spherical harmonics readable from compute.
- **Materials, as data.** Albedo and emission per object or per triangle, taken from the material
  rather than guessed from property names.
- **Hardware ray tracing, where the adapter has it.** An acceleration structure the engine keeps
  current with the scene, instance masks and culling, and a way to fetch vertex attributes and
  material data at a hit, which means geometry and material pools addressable by instance.
- **Large hash tables in storage buffers**, with atomics and a frame budget.

### Reflections

Screen-space reflections share the depth pyramid and the trace with screen-space GI. What screen
space misses is answered by reflection probes, which want cube maps rendered in the engine, or by
hardware rays, which share the acceleration structure, material pool and hit shading with
world-space GI. A reflection solution is therefore mostly the GI infrastructure with a different
ray distribution and a specular input to lighting.

### Radiance cascades

Probes at several scales, each tracing short intervals, merged coarse to fine. In three dimensions
it is still being made fast enough, and it is deliberately left until then. What it will need is
already on this list: 3D and array textures with storage access, compute over them every frame, a
voxel or distance representation to trace against, and an indirect diffuse input to lighting.

## What Unity made them work around, and the hook that replaces each

| built around | supported here as |
|---|---|
| Copying the pipeline's compute shaders, rewriting their text and swapping them in through reflection, to reach the indirect diffuse and occlusion textures | Named lighting inputs a pass or a compute shader writes: ambient occlusion, indirect diffuse, indirect specular |
| Subtracting the engine's ambient term from the lit picture and adding GI back | Turning the engine's ambient off per camera when an indirect input is provided |
| Re-rendering every opaque to get normals and albedo in forward | A prepass that writes normals, and a thin G-buffer (albedo, metallic, roughness) on request |
| Drawing its own object motion buffer, and marking moving pixels in the stencil | Motion vectors from the prepass, with previous transforms for every instance, bound to passes |
| Copying last frame's color, and caching previous matrices and positions by hand | Last frame's color and the previous view uniform bound to passes and per-view compute |
| A history cache keyed by camera hash, capped at sixteen cameras, with a time-to-live | History images owned by the camera, made and resized by the engine |
| Hidden volumes and renderer features per pass, rewritten for each pipeline and each engine version | One frame with named points (after the prepass, before the main pass, before transparency, before and after tonemapping) that passes and compute attach to |
| Matching lights by position against the engine's light buffer | The light list and shadow maps readable from compute |
| Guessing albedo from shader property names | Material data the engine writes, by object |
| Hidden cameras to approximate shadow frusta | Shadow views exposed as views |
| Quads in front of each camera so the pipeline's lighting shades a visibility buffer | A material pass over a visibility buffer, writing depth and motion |
| Integer ids in a float render target | Integer render targets and storage images |
| Buffers that cannot grow, copied in chunks | Buffers the engine grows, keeping their contents |
| Mip chains built through a flat buffer because one dispatch cannot bind every mip | A storage view per mip level of one image |
| Shaders shipped per platform and permuted by keywords | Slang, compiled once, with defines where a variant is meant |
| First frames skipped to avoid timeouts, dummy textures bound in case a pass did not run | Stand-ins the engine binds for anything not produced this frame, with a reason in the log |

## What the engine needs

Grouped by what it is, each with where it stands. **Has** means usable from C# today; **Bevy**
means Bevy provides it and the bridge does not reach it yet; **Missing** means neither does.

### The frame

| need | status |
|---|---|
| Full-screen passes over a camera's picture, before or after tonemapping | Has (`render/passes.rs`) |
| Compute before any camera draws | Has (`render/compute.rs`) |
| Compute per camera, with the view, at a chosen point in the frame | Missing |
| Points after the prepass, before the main pass and before transparency | Missing. Bevy's `Core3d` schedule has `Prepass`, `MainPass`, `EarlyPostProcess` and `PostProcess` sets to attach to |
| Passes writing several targets, or depth | Missing |
| Async compute | Missing. wgpu has one queue per device, so this waits on wgpu |

### Per view

| need | status |
|---|---|
| Depth and normals for passes | Has, single-sampled cameras only |
| Motion vectors for passes | Bevy (the prepass writes them for TAA and motion blur, and passes do not see them) |
| Previous view matrices for passes and compute | Bevy (`PreviousViewUniforms`) |
| Last frame's lit color | Missing |
| History images owned by the camera | Missing |
| The view uniform in compute | Missing |
| A depth pyramid | Missing as a service. Buildable by a package once images have mip views |
| Blue noise | Bevy, behind `bluenoise_texture`, which is off |

### Scene data on the GPU

| need | status |
|---|---|
| Transforms per instance, current and previous | Bevy keeps both per mesh; not exposed as a buffer a package reads |
| Lights and shadow maps readable from compute | Bevy has clustered lights and shadow atlases; not exposed |
| The sky as a cube map or spherical harmonics | Bevy generates environment maps; not exposed to compute |
| Material data by object | Missing |
| Geometry and material pools addressable at a ray hit | Missing |

### GPU-driven work

| need | status |
|---|---|
| Storage buffers of any size, read back to the CPU | Has |
| Indirect dispatch | Missing (buffers already carry the indirect usage) |
| Indirect draws whose count the GPU decides | Missing |
| Buffers that grow keeping their contents | Missing |
| Atomics, including 64-bit texture atomics | Available where the adapter has them, since the bridge asks wgpu for every feature the adapter offers |
| Subgroup operations | Same |

### Lighting integration

| need | status |
|---|---|
| Ambient occlusion applied by the renderer | Bevy (`ScreenSpaceAmbientOcclusion`), not bridged, and not replaceable by a package's own |
| Indirect diffuse and specular inputs | Missing. Bevy's PBR has no screen-space indirect input, so this is a change to its lighting or a fork point |
| Screen-space reflections | Bevy (`ScreenSpaceReflections`, deferred only), not bridged |
| Light probes and irradiance volumes | Bevy (`LightProbe`), not bridged |
| Deferred rendering and a G-buffer | Bevy, not bridged, and BevyCSharp materials draw forward |
| Ray-traced lighting | Bevy (`bevy_solari`), not compiled |

### Resources and formats

| need | status |
|---|---|
| Storage images, 2D and 3D, in float and integer formats | Has (`Shaders.CreateImage`) |
| Images and render targets in formats other than eight-bit RGBA | Missing for `Render.CreateImage` and render targets |
| A storage view of one mip level | Missing |
| Arrays of textures of any length | Has, where the adapter supports binding arrays |
| Cube maps rendered into | Missing |
| Block-compressed textures | Missing (`ktx2` is in, its payload formats are not) |
| Writing a region of a texture from bytes | Missing |

### Assets, baking and IO

| need | status |
|---|---|
| Meshes from vertices | Has |
| A processing step at import that writes a derived asset | Missing. Bevy has asset processors, off in this build |
| Streaming reads on a worker thread with a frame budget | Missing as a service |
| Bevy's meshlet asset and processor | Bevy, not compiled |

### Ray tracing

| need | status |
|---|---|
| Ray queries in shaders | Missing. wgpu has experimental ray queries, and naga's WGSL accepts them behind an `enable wgpu_ray_query` extension. Slang writes ray queries for SPIR-V and not for WGSL, so the gap is between the two, and closing it is either a Slang change or a SPIR-V path |
| An acceleration structure kept current with the scene | Missing. Bevy's Solari manages one |

### Debugging and tooling

| need | status |
|---|---|
| Hot reload of every shader, with the last good version kept | Has |
| What a program declares, by name and offset | Has (`shader.layout`) |
| Buffers and images shown in the editor | Missing. A package wants to show its intermediate textures and counters in a panel |
| GPU timings per pass | Missing. wgpu has timestamp queries where the adapter has them |

### C# and Slang

| need | status |
|---|---|
| Values set by name, any layout | Has |
| One shader source for every backend | Has, through Slang |
| Slang modules a package ships and a material imports | Has, anything under the asset root can be imported |
| SPIR-V output for what Slang's WGSL cannot express | Missing. Needed for ray queries and some atomics, and a large change to how the bridge hands shaders to Bevy |

## Order of work

Each phase unblocks a class of package, and none needs a later one.

1. **Foundations for screen-space work.** Motion vectors and the previous view for passes, history
   images owned by the camera, compute per camera at named points with the view, indirect dispatch,
   image formats and mip views. With these an ambient occlusion or screen-space GI package can be
   written today, composited over the picture.
2. **Lighting inputs.** Bevy's own ambient occlusion, reflections and light probes bridged, then a
   supported way for a package to supply ambient occlusion, then indirect diffuse and specular
   inputs, which is where a change inside Bevy's lighting is weighed against keeping a fork.
3. **GPU-driven geometry.** Indirect draws, instance data with previous transforms, buffers that
   grow, and Bevy's meshlets as a profile of their own. Then what a different virtualized geometry
   package needs beyond Bevy's: a visibility buffer material pass and shadow views as views.
4. **Streaming.** Import-time processing, worker-thread IO with a budget, region uploads and
   compressed formats. Texture streaming and geometry streaming share all of it.
5. **World space.** Scene data readable from compute (lights, shadow maps, sky, materials), 3D
   images with scrolling, then hardware ray tracing once shaders can reach it.
6. **Reflections and radiance cascades**, built on what the phases before provide.

## What to watch

- **Bevy's meshlets and Solari**, since each release moves both, and either may become what this
  engine offers out of the box.
- **wgpu ray queries and acceleration structures**, and Slang's WGSL output gaining them, which
  decides whether ray tracing can stay on the WGSL path.
- **Mesh shaders in wgpu**, which would change how a cluster rasterizer is best written.
- **64-bit atomics and subgroups**, available on some adapters now and needed as fallbacks where
  they are not.
- **Bevy's render graph as schedules**, which is what lets a pass attach to a named point in the
  frame, and which keeps changing shape between releases.
