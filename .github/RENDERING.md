# Rendering for the high end

What the engine has to offer so that virtualized geometry, texture streaming, screen-space and
world-space global illumination, ambient occlusion and, later, reflections and radiance cascades can
be built on it as ordinary code. The guide under [`docs/`](../docs) describes what exists, and this
document describes what has to exist, why, and in what order.

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
features. It needs 64-bit texture atomics, runs on Vulkan and Metal, refuses multisampling and needs
one opaque rendering method for the whole app. It is the first thing to offer, and a package doing
it differently would need the requirements above beyond it.

Offering it is not a matter of adding the plugin. It ends the process, rather than failing
softly, when the adapter lacks the features it needs and when any camera is multisampled, so it is
something a game asks for before the app starts (`Config.MeshletClusters`), with the bridge asking
the GPU first and turning multisampling off on every camera while it runs. The processor that turns
a mesh into a meshlet mesh links meshoptimizer and METIS, which are C and C++ built with the bridge,
so it is an addition to a profile (`--meshlet`) rather than part of the render profile every game
gets. `Render.CreateMeshletMesh` converts on a worker and `Render.SetMeshletMesh` draws the result
with a standard material.

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

The answer has somewhere to go already. Bevy's irradiance volume is a grid of ambient cubes in a 3D
image that its materials read as diffuse light, so a world-space technique that writes its radiance
cache out into such a grid each frame lights everything drawn with a standard material, blended by
normal and filtered between points, with no change to how those materials are drawn. It is coarser
than a per-pixel answer, so screen-space GI still needs an input of its own, but it is the same
shape a probe-based GI keeps anyway.

### Reflections

Screen-space reflections share the depth pyramid and the trace with screen-space GI. What screen
space misses is answered by reflection probes, which need cube maps rendered in the engine, or by
hardware rays, which share the acceleration structure, material pool and hit shading with
world-space GI. A reflection solution is therefore mostly the GI infrastructure with a different ray
distribution and a specular input to lighting.

Both ends Bevy ships are bridged. Its screen-space reflections run over its deferred path, and a
reflection probe can capture itself, rendering the room into a cube that Bevy filters on the GPU,
live or once. A reflection package adds what those two leave out, a trace that reaches past the
screen's edge through the probes and the world-space structures, and a specular input a package's
own result can be written into, which Bevy's lighting does not have yet.

### Radiance cascades

Probes at several scales, each tracing short intervals, merged coarse to fine. 3DEngine built them
first, in three parts, a distance field of the scene to trace through, the cascades of probes over
it, and the light they gather given to the surfaces, and they come here in the same three. What
they need is on this list, 3D and array textures with storage access, compute over them every
frame, a distance representation to trace against, and an indirect diffuse input to lighting.

The distance field is the first (`Render.SetSceneField`). Its plan is 3DEngine's, worked out on the
managed side each frame from the meshes the bridge gathers, each a number and a generation the
bridge gives a mesh and moves on when Bevy says its data changed, and its three kernels are
3DEngine's Slang, the splat, the resolve and the stamp, changed only where the bridge's bindings
differ, with named values for push constants, `Atomic<uint>` for WGSL's atomics, and the still field
read as a texture. The bridge pools the meshes' triangles in one buffer, since a buffer is written
whole, and `bcs_field.slang` holds the layout a shader reads the field through. A second splat over
the same triangles paints each cell the color of the triangle whose code it kept, the material's
base color times the vertices' colors at its nearest point, and the resolve writes that color and
the way the face looks into images laid out as the field, with the still meshes' own beside them for
the stamp, which paints a box's cells its color and its face. The paint dispatch writes each cell's
emission beside its color, and a third dispatch over the triangles lends the light of an emitter
thinner than a cell to the cells within a cell of it, times the share of a cell's face it covers,
which the resolve writes into an image of light laid out as the field, as 3DEngine's do. After the
counted cascades the program may ask for a far cascade (`farCascade`), its cells as wide as reaching
that distance ahead of the camera takes, held to the camera's far plane, which the bridge's gather
reads from its projection.

The cascades of probes are the second (`WorldProbes`, `bcs_gi.slang` and the kernels
`gi_trace.slang`, `gi_merge.slang` and `gi_ambient.slang`), 3DEngine's world probes with their mends
to date. A probe is moved off a surface and passed over inside a mesh, a texel of 8 a side sends a
ray from the middle of each of its quarters, the merge leans toward where the probe above met a
surface, the light past the first bounce is read from the probes a hit sees where the probes lay the
frame before, and each probe's share of that light follows its own light where that fell or rose by
much. A cascade's probes stand eight cells of its field cascade apart, eight a side, the last in the
far cascade where there is one. The bridge gathers the lights each frame (`bcs_gi_lights`), the sun,
the camera's ambient light or the world's, and the sixteen point and spot lights nearest the camera,
each given as the light a white surface facing it returns, a directional light's illuminance over pi
and a lamp's luminous power over four pi over pi again, times the camera's exposure, so the probes
hold the light in the units of the picture they are added to and a bright sun stays inside a half
float. A spot light falls off from its inner cone to its outer as Bevy's lighting has it. Three
things differ from 3DEngine's code for the bindings. A probe's held light and the partial sums are
storage buffers, since an image of half floats is not read and written in one WGSL dispatch. The
images the kernels only write are Slang's `WTexture3D`, which Slang refuses to declare read-write in
half floats. And the trace takes the sum of the 27 probes around one in every workgroup and keeps it
where the probe's share asks for it, since a barrier is reached by every invocation alike. On the
RTX 4070 Laptop GPU in the feature test's Cornell box, a cascade's trace takes 0.33 to 0.36 ms, its
merge 0.04 to 0.10 and its gather 0.03, each the mean of a frame's dispatches of the program as
`render.timings` gives it, so `Low`'s two cascades take about 0.9 ms and `High`'s four about 1.8,
the sixteen lamps' marches from each hit among it. The light from all around a ray of the last
cascade brings back is the ambient light's color and, where the camera has an environment map, the
map's light along the ray's way, as 3DEngine reads its environment map. The probes are traced before
the cameras draw, and the map is the camera's, so a dispatch on the camera copies it each frame onto
an octahedron of 64 texels a side, blurred as a surface of a quarter roughness blurs it, times the
exposure (`gi_sky.slang`), and the probes read the frame before's.

The light given to Bevy's surfaces is the third part (`gi_light.slang`), an engine draw after the
opaque surfaces blended by adding beside the contact shadows' draw, which reads the deferred
prepass's G-buffer, works out the diffuse share of the ambient light Bevy's lighting gave the
surface as Bevy works it out, from `F_AB` at a perceptual roughness of one, and adds in its place
the surface's diffuse color times the light read between the probes around it, times its occlusion.
3DEngine's model pass reads the probes inside its own lighting, and taking Bevy's term back stands
in for that until the indirect input of the table below exists. The diffuse light an environment map
gave the surface is taken back with the ambient term, as Bevy's environment_map_light works it out,
the multiple scattering and the diffuse color times what the dielectric's scattering leaves, times
the map's irradiance, and the probes' light stands in for it as for the ambient. Past the last
cascade the light from all around is 3DEngine's sky over a ground, the environment map's light from
above with the ambient light, each directional light's share of the ground's light read through its
shadow map a coarsest cell along the normal. The ambient light reaches the probes times 0.45, the
share Bevy's ambient term returns from a white surface facing the view (`F_AB(1, 1)` is 0.4524 and
-0.0024), so an open floor reads as Bevy's own lighting leaves it, which a test holds, and a red
wall's light tints a white block's side. The pass takes 0.23 ms at its least and 1.1 at its median
over 600 frames at 1280 by 720 in a Cornell box on the same GPU, where the probes take 0.9 ms at
`Low`, 1.2 at `Medium` and 1.4 at `High`. In the feature test the pass's span reads no GPU time,
where Bevy keeps no more than 256 timestamps a frame, so the pass is timed in a scene of its own.
`Render.SetGlobalIllumination` is public with it.

The reference global illumination is measured against is 3DEngine's (`GiReference`,
`gi_reference.slang`), a path tracer through a ray scene of the meshes the camera draws, built over
a geometry pool with the engine's instance and material buffers. It lights the first face a path
meets as Bevy's lighting does, Burley's diffuse term and the specular lobe by the material's
roughness, metallic and reflectance, and every face after as the probes do, with the probes' own
lights and the light from all around where a path meets nothing, and a point light's shadow starts a
tenth of a unit from it as Bevy's shadow maps do. It runs as a dispatch on the camera, compiled to
SPIR-V for its ray queries, a share of the paths each frame, and the engine's dispatches on a camera
are kept apart from the program's as its draws are. `gi.reference <png> <samples> [bounces]` writes
it with its linear light, its regions and their names, and `gi.compare <png>` reads the camera's
light before tonemapping and says how far the view lies from it, the pixels' differences summed over
the reference's light, with each region's mean light against the reference's and a picture of the
difference. A texture counts as its material's base color and a mesh's vertices' colors are left
out, and a path that meets nothing brings back the ambient light as the probes have it and the
camera's environment map along its way, so a textured ground, or a sky drawn with no environment map
to light the scene, reads apart from its reference. The bounce rooms are a test
(`BounceRoomsTests`), the nine views at each quality held within 15% over what they read, 0.10 to
0.11 in the Cornell box, 0.12 in the thin room, 0.25 in the corridor, 0.11 to 0.12 in the window's,
0.02 among the red walls, 0.05 to 0.07 on the grazing floor and 0.06 in the carried lamp's, where
3DEngine's read 0.12 to 0.14, 0.09, 0.20 to 0.24, 0.27 to 0.30, 0.02, 0.11 to 0.12 and 0.11 to 0.12,
the strip's 0.18 to 0.19 where 3DEngine's read 0.22, and the glowing blocks' 0.05 where 0.05 to
0.06. A mend that widens no view keeps every bound, 3DEngine's rule. The Cornell box's floor reads
13.2% over its reference at `High` without the screen probes, and 15.5% over with its lamp panel
dark in both, so the excess is the probes' indirect light. In the feature test's Cornell box at 1280
by 720, 256 paths a pixel take 3.5 s through 14,260 meshes, and the walls read from 5% under to 17%
over the reference, the lamp panel's light carried as a glow light. A material's emissive alpha is
Bevy's `emissive_exposure_weight`, the share of the camera's exposure its light takes, 0 by default
as in Bevy, which the bridge gives Bevy's own field, since Bevy reads the weight there and not from
the color's alpha, and which the field's gather and the reference take as Bevy shows it. Bevy's
deferred G-buffer keeps the emissive color and not the weight, so a camera the field's shadows or
global illumination make deferred showed an exposed emissive unexposed. The bridge mends Bevy's
`deferred/functions.wesl` in the text Bevy ships (`render/mended.rs`), applying the exposure as the
G-buffer is written, as the anisotropy mend does in `pbr_fragment.wesl`, and `DeferredEmissiveTests`
holds an exposed panel drawn deferred to what forward shows and within 2% of its reference, where
unmended it read 38,165 against 38.2.

The emitters are 3DEngine's. The gather gives each mesh's emission in the picture's units as Bevy
shows it, times the camera's exposure by the share of it the emission's alpha sets, the field paints
it beside the colors, and a probe's ray that meets it brings its light back. An emitter whose box's
longest side is no more than twice the first cascade's probe spacing is carried as a glow light
(`GlowLights`, `bcs_glow.slang`), a box whose light reaches a surface in closed form from its faces,
shadowed by a march through the field, the four brightest within reach at each pixel of the pass and
the two brightest at a probe's hit, and the field marks the cells such an emitter paints, so a ray
that meets it does not take its light twice. Emitters square to the axes that touch and emit the
same light are one light, and one thinner than a cell that is not carried lends its light to the
cells near it. Two things depart from 3DEngine's. Each emitter is its box whole, every face
emitting, since the field's gather hands over no vertices, so a round emitter is carried as about
twice as bright as it is and a mesh of many emitters is not cut into its parts. And the pass marches
to the middle of a light, where 3DEngine's pixels with no screen probe near march to the halves of
its faces, since the halves read alike in the bounce rooms and took the pass from 1.3 ms to 2.0 to
3.5 at 1280 by 720 with one light over the whole view of a Cornell box. The closed form clips a face
an edge at a time with its loops unrolled, since with small arrays indexed as rolled loops ran it
took the pass from 0.19 ms to 2.9 there. With one glow light over the whole view the pass takes 1.3
ms, and a cascade's trace 0.18 ms at `High`. In the bounce rooms the strip's view reads 0.18 to 0.19
from its reference where it read 1.00 with no emitter carried, the glowing blocks' 0.05 where 0.30
and the Cornell box's 0.10 to 0.11 where 0.11, no view wider.

The glossy term is 3DEngine's present one. The pass takes back the light from all around Bevy's
lighting reflected, its ambient term's and its environment map's along the mirror way, and gives it
again as 3DEngine's model pass does. Below a roughness of 0.5 the reflection is traced through the
picture's depth in steps that lengthen as they go, 12 over 8 units at `Low`, 16 over 12 at `Medium`
and 24 over 16 at `High`, and past the picture through the field, a surface met on the picture
reflecting the light the frame before's picture showed where it was, which the camera keeps copied
before tonemapping as an image of its own, and one past it the light the probes shade a hit with,
fading to Bevy's reflection from a quarter of the roughness to a half. What is not traced is Bevy's
reflection times the share of the sky the probes around the surface see along the mirror way
(`skyAt`), so a room sealed from the sky reflects none of it. Two things depart from 3DEngine's.
Whether the frame before's picture showed the surface the reflection met is not asked, which needs
the frame before's depth, so a reflection of a surface the moving camera uncovered takes what stood
there before. And the picture is read at its own size, where 3DEngine blurs it down its mips as the
surface is rough. A metal floor polished to a mirror shows a sunlit red wall standing on it at 244,
115, 107 with global illumination on and 2, 2, 2 off, and a rough metal floor in a closed room under
an environment map reflects 0, 0, 14 on where 78, 58, 119 off. In the Cornell box at 1280 by 720
with one glow light the pass takes 1.5 ms with a metal floor polished to a mirror where it takes 1.3
with every surface rough, as before the glossy term. The bounce rooms read as they did, their
surfaces at a roughness of 0.5 and lit by no sky.

The screen probes are 3DEngine's (`WorldProbes.Screen.cs` and the kernels `gi_screen.slang`,
`gi_screen_filter.slang` and `gi_screen_glow.slang`), dispatches on the camera after its prepass
that write the camera's images, a texel a probe, its light, its surface and what it sees of each
glow light, then its light blended with the probes around and with the frame before's, which the
camera keeps with its history, then the glow lights' light on images half the picture's size. The
pass blends the four probes around a pixel that stand on a surface like its own, fading to the
world's probes across the second cascade's outer band, and reads the glow lights' texels around it
where some are lit. Five things depart from 3DEngine's. The camera's depth is read whole, where
3DEngine reads a depth half the window's size, each probe on the texel under its tile's middle. What
a probe sees of each glow light is four images of four codes a texel and an image of the fainter
lights' light, where 3DEngine keeps a buffer of twenty codes a probe, since the camera's images take
the picture's size. The march through the depth finds a face's way from the depth, since a dispatch
reads no G-buffer. The pass finds how the surface moves from pixel to pixel from its nearer
neighbors in the depth, since a pass over the whole view has no derivative of a mesh's position. And
3DEngine's command that reads one probe's rays is not taken. In the bounce rooms the Cornell box's
floor reads 3.5% over its reference with them where 12.8% without, and the thin walls' seams, which
the world's probes lit, go dark. At 160 by 90 their tiles are few, ten across at `Low`, and their
light lies in blotches a tile wide, which widen the window's, the strip's and the carried lamp's
views, all within 3DEngine's own bounds at the same size, so the rooms keep their bounds with the
probes traced and left unread, the old path, and a second test holds them on within 3DEngine's. Left
out, the probes stand for the glow lights alone wherever one is lit, which at 160 by 90 reads the
glowing blocks' room 0.06 to 0.07 from its reference where each pixel's march reads 0.05. In a
Cornell box at 1280 by 720 with one glow light on the RTX 4070 Laptop GPU, shared with two other
programs drawing, the probes take 1.4 ms at `Low` and 1.6 at `High`, their blend 0.02 and the glow
lights' texels 0.12, and the pass 0.54 ms at its median, where left out the probes standing for the
glow light take 0.1 ms and the pass 0.73 to 0.83. In the feature test's Cornell box their light lies
in streaks a tile wide on the back wall at `High`, the blotches' term at the size of a tile.

The faults 3DEngine read in its screen probes were read here against its findings. The blotches are
its term, a small emitter's near field or a bright patch narrower than a probe's texel read by a few
fixed rays, and its one mend that held, a hit within a cell of a glow light taking the light's mean
over a cell's square about it, is `glowLightsAt`'s here too. The edge leak is its mend. Two blocks
that meet along an edge alone leave the field's cells about the edge inside on one diagonal and
outside on the other, the distance never below zero along it, and a glow light's march passed
through, the floor about the foot of a stone wall's halves 15 sRGB levels at its brightest past the
edge where 3 with the edge closed. The resolve holds such a pinched cell inside, which reads its
neighbors' codes, so the codes are cleared by a dispatch of their own after each resolve, where the
resolve had cleared each cell's as it read it, and the floor reads 3 either way. The stepped edges
3DEngine found were a lamp's shadow stepping where each pixel marches to the glow light's halves,
which the glow lights' texels read smooth, and need no mend here, where each pixel marches to the
light's middle.

## What Unity made them work around, and the hook that replaces each

| built around | supported here as |
|---|---|
| Copying the pipeline's compute shaders, rewriting their text and swapping them in through reflection, to reach the indirect diffuse and occlusion textures | Named lighting inputs a pass or a compute shader writes, which are ambient occlusion, indirect diffuse and indirect specular |
| Subtracting the engine's ambient term from the lit picture and adding GI back | Turning the engine's ambient off per camera when an indirect input is provided |
| Re-rendering every opaque to get normals and albedo in forward | A prepass that writes normals, and a thin G-buffer (albedo, metallic, roughness) on request |
| Drawing its own object motion buffer, and marking moving pixels in the stencil | Motion vectors from the prepass, with previous transforms for every instance, bound to passes |
| Copying last frame's color, and caching previous matrices and positions by hand | Last frame's color and the previous view uniform bound to passes and per-view compute |
| A history cache keyed by camera hash, capped at sixteen cameras, with a time-to-live | History images owned by the camera, made and resized by the engine |
| Hidden volumes and renderer features per pass, rewritten for each pipeline and each engine version | One frame with named points (after the prepass, before the main pass, before transparency, before and after tonemapping) that passes and compute attach to |
| Matching lights by position against the engine's light buffer | The light list and shadow maps readable from compute |
| Guessing albedo from shader property names | Material data the engine writes, by object |
| Hidden cameras to approximate shadow frusta | Shadow views exposed as views, which draws render into |
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
| Compute per camera, with the view, at a chosen point in the frame | Has (`render/views.rs`, `Shaders.SetViewDispatches`) |
| Points after the prepass, between opaque and transparent geometry, and before and after tonemapping | Has, for compute and draws (`FramePoint`), and for passes at every point with a picture (`ShaderPass.At`) |
| Passes writing several targets, or depth | Has, for draws on a camera, the picture or any number of the camera's images at once (`ViewDraw.Targets`), with depth. A full-screen pass writes the picture alone, and a draw of one triangle over the picture into several images does the rest |
| Async compute | Missing. wgpu has one queue per device, so this waits on wgpu |

### Per view

| need | status |
|---|---|
| Depth and normals for passes | Has, single-sampled cameras only |
| Motion vectors for passes | Has (`Shaders.SetPrepass(camera, motion: true)`, `passMotionAt`) |
| Albedo, roughness and metallic per pixel | Has, from Bevy's G-buffer on a camera drawing deferred (`gbuffer`, `passSurfaceOf`) |
| Last frame's depth and G-buffer | Has (`Shaders.SetPrepass(..., previous: true)`, `depth_previous`, `gbuffer_previous`) |
| Previous view matrices for passes and compute | Has, on 3D cameras (`passPreviousView`) |
| Last frame's lit color | Has (`ViewImage(..., History: true, CopyAt: FramePoint.BeforeTonemapping)`, read as `name_previous`), scaled to the image and point sampled |
| History images owned by the camera | Has (`Shaders.SetViewImages`, `History: true`) |
| The view uniform in compute | Has, for compute on a camera |
| A depth pyramid | Has, Bevy's farthest-depth pyramid for culling (`Shaders.SetPrepass(..., pyramid: true)`, `depth_pyramid`). A nearest-depth one for tracing is built by the package, since a camera's images have mip levels reachable one at a time |
| Blue noise | Has, Bevy's spatio-temporal blue noise, for shaders on a camera (`passBlueNoise`) |

### Scene data on the GPU

| need | status |
|---|---|
| Transforms per instance, current and previous | Has (`Shaders.CreateInstanceBuffer`, `Instance`) for the entities put in its slots |
| Lights and shadow maps readable from compute | Has, for compute and passes on a camera (`passLights`, `passPointLights`, `passDirectionalShadow`, `passPointShadow` for point and spot lights, `passPointLightRadiance`) |
| The sky as a cube map or spherical harmonics | Has, as the camera's environment map, for compute and passes on a camera (`passEnvironmentSpecular`, `passEnvironmentDiffuse`) |
| Material data by object | Has, as constants from each entity's standard material (`Shaders.CreateMaterialBuffer`, `Material`). Textures and per-triangle data are missing |
| Geometry and material pools addressable at a ray hit | Has (`Shaders.CreateGeometryPool`, `AddToGeometryPool`, `PoolVertex`, `intersectTriangle`), with material constants by slot. An acceleration structure over the pool is left to the package, in compute |

### GPU-driven work

| need | status |
|---|---|
| Storage buffers of any size, read back to the CPU | Has |
| Indirect dispatch | Has (`Shaders.DispatchIndirect`, `ViewDispatch.Indirect`) |
| Indirect draws whose count the GPU decides | Has, for geometry drawn on a camera out of buffers (`Shaders.SetViewDraws`, `ViewDraw.Indirect`) |
| Draws into integer targets, for a visibility buffer | Has (`ViewDraw.Into`, a camera image made with `ClearEachFrame`), tested against the camera's depth |
| Shadows from geometry drawn out of buffers | Has (`ViewDraw.CastsShadows`), into every directional cascade, spot light map and point light face, each drawn with its own view |
| A visibility buffer's resolve writing depth, motion and normals | Has (`SV_Depth`, and the prepass's `motion` and `normals` as draw targets) |
| Buffers that grow keeping their contents | Has (`Shaders.GrowBuffer`), rebinding everything that held them |
| Atomics | Has, on buffers through Slang's `Atomic<T>`, which is the form Slang turns into WGSL atomics. 64-bit and texture atomics depend on the adapter and on Slang's WGSL output reaching them |
| Subgroup operations | Has, through Slang's wave operations, on adapters with subgroups. naga has no subgroup election, so `WaveIsFirstLane` is written as `WaveGetLaneIndex() == 0` |

### Lighting integration

| need | status |
|---|---|
| Ambient occlusion applied by the renderer | Has (`Render.SetAmbientOcclusion`), and replaceable by a package's own through the `ambient_occlusion` texture a compute shader on the camera writes |
| Indirect diffuse and specular inputs | In part. A world-space technique answers into an irradiance volume a compute shader writes (`Render.SetIrradianceVolume`, `irradianceTexel`), which Bevy's materials read as their diffuse light. A per-pixel result is added after opaque geometry by an additive draw weighted by the G-buffer's albedo, before transparency and tonemapping, which is right for diffuse. What that cannot do is feed Bevy's specular term or the materials a Slang program draws, which is a change to Bevy's lighting or a fork point |
| Screen-space reflections | Has (`Render.SetScreenSpaceReflections`), Bevy's, over its G-buffer |
| Light probes and irradiance volumes | Has (`Render.SetReflectionProbe`, `Render.SetIrradianceVolume`, and `Render.SetProbeCapture` for a reflection probe that renders itself), with the volume an ordinary 3D image a compute shader can write every frame |
| Deferred rendering and a G-buffer | Has for Bevy's materials (`Render.SetDeferredRendering`), and readable by a camera's passes and compute as `gbuffer`, unpacked by `passSurfaceOf` (`Shaders.SetPrepass(..., deferred: true)`). Materials a Slang program draws are forward and leave it empty |
| Ray-traced lighting | Has, Bevy's Solari, in a bridge built with `--solari` and an app that sets `Config.RayTracedLighting` (`Render.SetRayTracedLighting`, `SetRayTraced`). A package's own shaders tracing rays is the ray query row above |

### Resources and formats

| need | status |
|---|---|
| Storage images, 2D and 3D, in float and integer formats | Has (`Shaders.CreateImage`) |
| Images from data in formats other than eight-bit RGBA | Has (`Shaders.CreateImage<T>(..., texels)`). Render targets are eight-bit sRGB or half floats (`Render.CreateTarget(..., TargetFormat.Rgba16Float)`), which keep light brighter than white |
| A storage view of one mip level | Has, for a camera's images (`name_mip1`) and for images made with `Shaders.CreateImage(..., mips:)` (`SetTexture(name, image, mip: 1)`) |
| Arrays of textures of any length | Has, where the adapter supports binding arrays |
| Cube maps rendered into | Has, for a reflection probe capturing itself (`Render.SetProbeCapture`), and for cameras of the game's own, each pointed at one layer of a cube or array target (`Render.CreateTarget(..., layers: 6)`, `SetCameraTarget(camera, target, layer)`) |
| Block-compressed textures | Has, for images filled from memory (`ShaderImageFormat.Bc1` to `Bc6hFloat`, written a block at a time). Files in those formats load through `ktx2` where the adapter decodes them |
| Writing a region of a texture from bytes | Has (`Shaders.WriteImage<T>`), at any mip level and into slices of a 3D image |

### Assets, baking and IO

| need | status |
|---|---|
| Meshes from vertices | Has |
| A processing step at import that writes a derived asset | Missing. Bevy has asset processors, off in this build |
| Streaming reads on a worker thread with a frame budget | Has (`Streaming.Read`, `TryTake`, `BytesPerFrame`), with priorities |
| Bevy's meshlet asset and processor | Has, in a bridge built with `--meshlet` and an app that sets `Config.MeshletClusters` (`Render.CreateMeshletMesh`, `SetMeshletMesh`), including baking to a `.meshlet_mesh` file and loading it back (`saveTo:`, `AssetKind.MeshletMesh`) |

### Ray tracing

| need | status |
|---|---|
| Ray queries in shaders | Has, in compute shaders compiled to SPIR-V (`ShaderTarget.SpirV`), through `rayTrace` and `visible`, in a bridge built with `--solari` and an app running Solari |
| An acceleration structure kept current with the scene | Has, Solari's, over the meshes given to `SetRayTraced`, with their vertices, transforms, materials and lights, which `bcs_ray` declares as group two of a dispatch and `raySurfaceAt` shades a hit from. A package's own is a ray scene over a geometry pool (`Shaders.CreateRayScene`), traced with `rayTraceIn`, which needs no Solari |
| A denoiser over the ray-traced lighting | Has, a node of the bridge's own in the `solari` profile, spatiotemporal variance-guided filtering over Solari's lit result, since Bevy's only denoiser for it is DLSS Ray Reconstruction, which the bridge does not build. On wherever the lighting runs, turned off and set by `Render.SetRayTracedDenoiser` |

The route runs around what each tool lacks. Slang writes ray queries only for SPIR-V, and naga's
SPIR-V reader has no ray query instructions, so a shader of this kind never passes through naga.
The bridge compiles it to SPIR-V, renumbers its descriptor sets in the binary, and hands it to
Bevy, which gives it to the Vulkan driver untouched (`spirv_shader_passthrough`). Nothing reads
the binary on the way, so its layout comes from Slang's reflection, which names every binding and
says whether the entry point uses it. The scene is Solari's `RaytracingSceneBindings`, whose layout
the `bcs_ray` module declares in the same order, so the bind group Solari builds each frame is the
one the dispatch binds.

Solari writes its direct and indirect light into the view target as one color, with the surface's
color and its emissive in it, and leaves the noise to a denoiser. The bridge's node,
`render/denoiser`, runs in Bevy's `Core3d` schedule after the main opaque pass, which comes after
Solari's own system, private to its crate, and before the transparent pass. It dispatches five
stages of spatiotemporal variance-guided filtering over the view target, as `denoiser.wgsl` holds
them. The light is demodulated, the G-buffer's emissive taken off and the rest divided by the share
the surface sends back, its diffuse color and what its specular reflection returns at the angle it
is seen at, since a dark surface seen at a slant sends back more by the second, and divided by its
color alone its light stood above its lighter neighbors' and the filter spread it among them. It is
accumulated in time, reprojected by the motion vectors onto the previous frame's history and kept
where the previous depth and G-buffer, which Solari double-buffers with ReSTIR on, agree with this
frame's, with the mean of its luminance and the variance about it beside it. The luminance's
variance comes from those, or from a seven by seven neighborhood while the history is short, worked
out in the same dispatch. Five à-trous passes at steps 1 to 16 weigh each tap by its depth, normal
and luminance, the first pass's light kept as the next frame's history, and the last remodulates
into the view target. A workgroup of eight by eight whose light all varies by under a hundredth is
passed through, since its taps would move it by under a level. Each view keeps its history, the
moments in two textures read and written by turns, and the passes' light and variance ping-ponged
between two more, all half floats, and each pixel's distance and packed normal in eight bytes, which
a tap reads in place of the depth and the G-buffer's sixteen, since the stages' time goes to moving
the picture through memory. A pixel with no G-buffer texel, the sky or a forward material, is left
as it was.

A scene Solari does not keep, such as stand-ins or a lower level of detail for rays, is a ray
scene over a geometry pool. The render world builds each pool mesh into a bottom-level structure
once and the top-level structure every frame from where the entities are, and the dispatch binds
it by name like any other value. Every slot holds an instance, an empty one masked so no ray meets
it, because wgpu packs the instances it builds and a gap would shift every slot after it.

### Debugging and tooling

| need | status |
|---|---|
| Hot reload of every shader, with the last good version kept | Has |
| What a program declares, by name and offset | Has (`shader.layout`) |
| Buffers and images shown in the editor | Images, yes (`Shaders.Watch`, the editor's Frame tab). Buffers are read as numbers by `shader.buffer`, in the console and from the command line, and in code with `Shaders.BeginBufferRead` |
| GPU timings per pass | Has (`Config.GpuTimings`, `Render.Timings`, `render.timings`), Bevy's passes and every shader program's dispatches, passes and draws by file name |

### C# and Slang

| need | status |
|---|---|
| Values set by name, any layout | Has |
| One shader source for every backend | Has, through Slang |
| Slang modules a package ships and a material imports | Has, anything under the asset root can be imported |
| SPIR-V output for what Slang's WGSL cannot express | Has, for compute stages (`ShaderProgramSettings.ComputeTarget`), passed through to Vulkan untouched and translated by naga elsewhere |

## Order of work

Each phase unblocks a class of package, and none needs a later one.

1. **Foundations for screen-space work.** Motion vectors and the previous view for passes, history
   images owned by the camera, compute per camera at named points with the view, indirect dispatch,
   image formats and mip views. These are in, so an ambient occlusion or screen-space GI package can
   be written now, composited over the picture by a pass, reading the G-buffer, last frame's depth
   and last frame's lit picture. The phase is complete.
2. **Lighting inputs.** Bevy's own ambient occlusion is bridged and a package can supply its own
   through it. Bevy's screen-space reflections are bridged over its deferred path, and its light
   probes, including an irradiance volume a compute shader writes, which is how a world-space GI
   package lights Bevy's materials today. A per-pixel indirect diffuse and specular input is next,
   which is where a change inside Bevy's lighting is weighed against keeping a fork.
3. **GPU-driven geometry.** Indirect draws on a camera, instance data with previous transforms and
   buffers that grow are in, and so are draws into integer targets for a visibility buffer and
   Bevy's meshlets as an opt-in. A pass resolving a visibility buffer can write depth, motion and
   normals, since a draw after the prepass targets the prepass's motion and normals and writes
   depth, and a draw casts shadows by being drawn into every shadow view, each directional
   cascade, spot light map and point light face, so shadow views are views. The phase's
   foundations are in.
4. **Streaming.** Region uploads into an image, block-compressed images and worker-thread reads
   with a budget are in. Import-time processing is next. Texture streaming and geometry streaming
   share all of it.
5. **World space.** Scene data readable from compute (lights, shadow maps and the sky are, and
   materials, per pixel through the G-buffer and by object as constants), 3D images with
   scrolling, and hardware ray queries, which a compute shader compiled to SPIR-V makes through
   `bcs_ray` against the scene Solari keeps or a ray scene of its own over a geometry pool.
   A mesh a compute shader deforms in the pool is built again when asked
   (`Shaders.RebuildRayScene`).
6. **Reflections and radiance cascades**, built on what the phases before provide.

## What to watch

- **Bevy's meshlets and Solari**, since each release moves both, and either may move from a
  feature the bridge is built with on request into every build of it.
- **wgpu ray queries and acceleration structures**, and Slang's WGSL output gaining them, which
  decides whether ray tracing can stay on the WGSL path.
- **Mesh shaders in wgpu**, which would change how a cluster rasterizer is best written.
- **64-bit atomics and subgroups**, available on some adapters now and needed as fallbacks where
  they are not.
- **Bevy's render graph as schedules**, which lets a pass attach to a named point in the frame, and
  which keeps changing shape between releases.
