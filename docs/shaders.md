# Shaders

Materials and passes over the picture written in Slang, compiled as the game runs and compiled again
when a file changes.

### A shader of your own

Shaders are written in [Slang](https://shader-slang.org), and a shader
declares what it needs as ordinary globals, in any combination and at any size. That covers numbers
and arrays of them, structs, constant buffers, textures of every shape and arrays of them, samplers,
and storage buffers and images. The bridge has no table of what is allowed. It asks the compiler
how the shader was laid out and builds the bind group from that, so the only limits are the GPU's,
and C# sets each value by the name the shader gave it.

```slang
import bcs;

uniform float4 tint;
uniform float weights[1000];
Texture2D layers[64];
TextureCube skies[16];
SamplerState linear;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    let sky = skies[3].Sample(linear, mesh.world_normal);
    return tint * layers[7].Sample(linear, mesh.uv) * weights[999] + sky;
}
```

<!-- compiled with:
using System.Numerics;
float[] weights = [1f, 0.5f];
AssetHandle rock = default, dusk = default;
Entity pond = default;
-->
```csharp
var layered = Shaders.CreateProgram("shaders/layered.slang");     // the fragment shader alone

var material = Shaders.CreateMaterial(layered)
    .Set("tint", new Vector4(1f, 0.5f, 0.2f, 1f))
    .Set("weights", weights)                                       // as long as the shader's array, or shorter
    .SetTexture("layers", rock, 7)
    .SetTexture("skies", dusk, 3)
    .SetSampler("linear", SamplerSettings.Clamped);

Render.SetMaterial(ctx.Ecs, pond, material);                      // as many as the game needs
```

A name is a global's own (`tint`), a field of a struct or a constant buffer (`sun.color`), or an
element of an array of structs (`lights[3].color`), and a texture, buffer or sampler in an array is
its name and an index. Numbers are checked for kind and shape, so a `float3` takes a `Vector3`, an
`int` an `int`, a `float4x4` a `Matrix4x4`, an array a span of its elements, and `SetNumbers` covers
the shapes C# has no type for, such as `int2` or `float3x3`. A struct can be set whole with
`SetStruct` or `SetBytes`, laid out the way the shader lays it out. A value that does not fit is
refused with an `ArgumentException` that lists what the shader does declare. Anything not set
reads as zero, a texture as a stand-in of its shape, and a sampler as linear and repeating, so a
shader draws something whatever has been set. A matrix crosses row by row, which is how
`System.Numerics` holds one, so `mul(m, v)` in the shader is `m` applied to `v`.

`material.Parameters` lists what the shader declares, and the editor's inspector draws a widget for
each of them, and `program.Layout` (or `shader.layout` in the console) says where every name is,
down to the byte offset.

Bevy asks a material's *type* for its bind group layout, and C# cannot declare a type, so the bridge
has one material type that gives every material the layout its program's shaders declared, and
the same queue, specialization and draw Bevy's own materials go through. That is also what lets a
material change program while it is drawn (`material.Program = other`), keeping every value by
name.

A program can name four stages, each a file and an entry point, so one file can hold all of them:

```csharp
var grass = Shaders.CreateProgram(new ShaderProgramSettings
{
    Vertex = "shaders/grass.slang",                                // moves the blades
    Fragment = "shaders/grass.slang",
    PrepassVertex = new ShaderStage("shaders/grass.slang", "prepass_vertex"),
    Defines = { ["BLADES"] = 3, ["WIND"] = true },
});
```

`import bcs;` gives a shader Bevy's view, time and mesh transforms, and vertex structs that line up
with Bevy's own, so a fragment shader works after Bevy's vertex shader, and `bcs::standard_vertex`
does what Bevy's does for a vertex shader that starts from it. `bcs::tag(mesh.instance_index)` reads
the number an entity's `MeshTag` gives it, set with `ecs.Insert<MeshTagRef>(entity).Value`, so
meshes sharing one material are told apart in the shader, each picking its layer of an array
texture (loaded with `TextureSettings.Layers`) or its entry of a buffer:

```slang
import bcs;

uniform float height;

[shader("vertex")]
bcs::VertexOutput vertex(bcs::Vertex v)
{
    v.position.y += sin(bcs::globals.time * 3.0 + v.position.x * 4.0) * height;
    return bcs::standard_vertex(v);
}

[shader("vertex")]
bcs::PrepassVertexOutput prepass_vertex(bcs::PrepassVertex v)
{
    v.position.y += sin(bcs::globals.time * 3.0 + v.position.x * 4.0) * height;
    return bcs::prepass_output(v.instance_index, v.position, v.normal, v.uv);
}
```

The prepass draws depth for shadows, and normals and motion for the effects that read them. A
material that moves its vertices needs a prepass vertex shader moving them the same way, or it casts
the shadow of the mesh it started from, and one that discards pixels needs a prepass fragment shader
discarding the same ones. Both read the material's values like the main stages do. A vertex stage
left out is Bevy's own. Defines reach the shader as `-D`, and a program with different defines is a
different program compiled on its own.

A stage can also be Slang handed over as text, for a shader worked out at run time, whether a node
graph produced it or a player typed it. It may `import bcs;` and any module under the asset root.
Different text is a different program, so there is nothing to reload, and a change is a new program
put on the material:

<!-- compiled with:
ShaderMaterial material = default;
string generated = "";
-->
```csharp
material.Program = Shaders.CreateProgram(ShaderStage.Slang(generated));
```

### Lit by Bevy

A material's fragment shader writes the color drawn, and is lit by nothing unless it lights itself.
`bcs::lit` lights it as Bevy lights its standard material, with every light, shadow, environment
map and fog of the view, from a `bcs::Surface` the shader fills in, as Bevy's own `ExtendedMaterial`
does:

```slang
import bcs;

uniform float4 tint;
Texture2D rust;
SamplerState linear;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    var surface = bcs::surface(mesh);              // Bevy's default standard material here
    surface.base_color = tint * rust.Sample(linear, mesh.uv);
    surface.roughness = 0.8;
    return bcs::lit(surface, mesh);
}
```

A surface holds what a standard material does: its color, the light it gives off, how metal and
rough it is, its reflectance, how much ambient light reaches it, and the normal it is lit by, which
a normal map bends. `bcs::lit` is two steps, `bcs::light` and then `bcs::finish`, and a shader that
changes the lit color, cutting it into steps or tinting it, calls them apart and changes it between,
before the fog and the tonemapping a camera without high dynamic range does in the shader:

```slang
var color = bcs::light(surface, mesh);
color = floor(color * 4.0) / 4.0;
return bcs::finish(color, mesh);
```

Bevy's lighting is its own WGSL over bindings that change with every feature a camera turns on, so
it is not written again in Slang. The two calls reach WGSL functions by fixed names that the bridge
puts in front of the compiled shader, over Bevy's own `apply_pbr_lighting` and
`main_pass_post_lighting_processing`, only where the shader calls them. A lit material is drawn in
the forward pass, even under a camera rendering deferred, and the surface receives shadows and takes
fog as a standard material does by default.

### Clustered decals

Bevy projects a clustered decal onto whatever lies inside its box, and its standard material lays
the decal's textures on. A surface here has them laid on with `bcs::decals` before it is lit, the
color over the surface's by its alpha, how metal and rough it is, its normal map and the light it
gives off, as the standard material lays them. A shader that does something of its own with each
decal walks them itself, by `bcs::decal_count`, the tag a game gave each, which Bevy hands over and
does nothing with, and each of its textures, as Bevy's clustered_decals example tints its decals by
their tags:

```slang
var surface = bcs::decals(bcs::surface(mesh), mesh);

for (uint index = 0; index < bcs::decal_count(mesh); index++)
{
    if (bcs::decal_tag(mesh, index) == 1)
        surface.emissive += bcs::decal_sample(mesh, index, bcs::DecalMap.Color);
}

return bcs::lit(surface, mesh);
```

They reach Bevy's own walk through the decals over a point, its `ClusteredDecalIterator`, by WGSL
the bridge puts in front of a shader that calls them, as it does for the lighting. A device that
cannot have clustered decals, which Bevy decides by whether it can bind arrays of textures, has none
over any point.

### Irradiance volumes

`bcs::light` takes the irradiance volumes over a surface into account as the standard material
does. `bcs::irradiance(mesh, normal)` is the light they give a surface facing `normal` there, by
Bevy's own `irradiance_volume_light`, at each volume's intensity, for a shader that shows that light
itself, as Bevy's irradiance_volumes example draws each voxel of its volume as a cube in the light
it holds.

### The compiler

`slangc` compiles each stage to WGSL in the background. `./bcs build` and
`./bcs test` fetch a pinned release into `build/tools/slang` (`build/fetch-slang.sh` does it on its
own), and the bridge looks for it in `BCS_SLANGC`, then on the `PATH`, then there. Every successful
compile is cached under the asset root in `.slang-cache` with its layout, keyed by the file, the
defines and a hash of everything the file imported. A machine without `slangc` reads the cache
instead, so a game shipped with it needs no compiler, and an entry whose sources have changed is
never used. The cache is ignored by git in this repository, since every checkout fetches `slangc`
and fills it again on its first run. `Shaders.SlangAvailable` says whether edits can be compiled.

### Reloading shaders

An edit to a shader file, or to any file it imports, reaches the screen
within a quarter of a second, in every profile. An edit that changes what the shader declares gives
the program a new layout, and every material keeps its values by name, so a parameter the edit added
starts at zero and the rest keep what they held. A file that fails to compile leaves the last version
that compiled in use and says why in the log, in `program.Diagnostics`, and through `shader.errors`
in the console. One that has never compiled draws magenta. A shader reading an input its vertex
shader never wrote is a validation error, which closes the app;
`Shaders.KeepRenderingAfterErrors` logs it and drops the frames it breaks instead, and the editor
sets it.

### Passes over the picture

A camera runs any number of passes over what it drew, each a shader run
once per pixel, reading the picture so far and writing the next one. A pass is an instance of a
program with a pass stage, and an instance holds values by name the way a material does, so one
program can run twice with different values:

<!-- compiled with:
Entity camera = default;
float strength = 0.5f;
-->
```csharp
var crt = Shaders.CreateInstance(Shaders.CreateProgram(
    new ShaderProgramSettings { Pass = "shaders/crt.slang" }));

crt.Set("strength", 0.3f);
Shaders.SetPasses(camera, new ShaderPass(crt, AfterTonemapping: true));

crt.Set("strength", strength);                  // while it runs
```

```slang
import bcs_pass;

uniform float strength;
Texture2D grain;
SamplerState grain_sampler;

[shader("fragment")]
float4 fragment(bcs_pass::Input input) : SV_Target
{
    let shift = bcs_pass::pixel_size() * strength * 4.0;
    let color = float3(
        bcs_pass::sample(input.uv + shift).r,
        bcs_pass::sample(input.uv).g,
        bcs_pass::sample(input.uv - shift).b);
    let lines = 0.85 + 0.15 * sin(input.position.y * 3.14159);
    return float4(color * lines * grain.Sample(grain_sampler, input.uv).r, 1.0);
}
```

`import bcs_pass;` gives a pass the picture, time, the view and the previous frame's, and the
camera's depth, normals and motion vectors, which the bridge binds itself. Everything else a pass
declares is its own. Depth, normals and motion come from a prepass the camera draws when asked, with
`Shaders.SetPrepass(camera, depth: true, normals: true, motion: true)`, and an outline, a fog or
anything reusing the previous frame is built from them. `bcs_pass::distance_at` turns depth into
world units, `world_position` turns a pixel back into a point in the world, and `previous_uv_of`
finds where a point was on the previous frame. A camera that draws none of them binds the far plane,
white normals and no motion, and so does a multisampled one, whose prepass a pass cannot bind. A
pass before tonemapping sees the linear picture, which may be brighter than white, and suits
anything about light; one after sees what the screen will show, and suits anything about the picture
as a picture. `At: FramePoint.AfterOpaque` runs one on the lit opaque geometry, before transparent
geometry is drawn over it. Something about the lit surfaces goes there, so glass in front is not
under a fog or a reflection meant for what is behind it, and it needs a camera drawn once a pixel. A
pass still compiling is skipped rather than drawn wrong, and passes run in the order given, each
over what the last wrote.

---

Before this, [Materials and textures](materials.md).
Next, [Compute](compute.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#shaders). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#shaders). The [guide's contents](../README.md#guide) list every page.
