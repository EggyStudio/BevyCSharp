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
    v.position.y += sin(bcs::prepass_globals.time * 3.0 + v.position.x * 4.0) * height;
    return bcs::prepass_output(v.instance_index, v.position, v.normal, v.uv);
}
```

The prepass draws depth for shadows, and normals and motion for the effects that read them. It binds
less than the main pass and its time elsewhere, so a prepass stage reads `bcs::prepass_globals`
where the main pass reads `bcs::globals`, and a pipeline given the other refuses to build. A
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

### Pipeline constants

A constant the shader declares with `[SpecializationConstant]` is compiled into the material's
pipelines rather than read as it draws, which WGSL calls an `override` and which lets the driver
fold the value into the code. A material sets it by name with one number, as it sets any value, and
the shader's own value stands where it sets none:

```slang
import bcs2d;

[SpecializationConstant]
const float LEVELS = 4.0;

[shader("fragment")]
float4 fragment(bcs2d::VertexOutput mesh) : SV_Target
{
    let t = floor(mesh.uv.x * LEVELS) / LEVELS;
    return float4(t, t * 0.4, 1.0 - t, 1.0);
}
```

<!-- compiled with:
ShaderProgram posterize = default;
-->
```csharp
var coarse = Shaders.CreateMaterial2d(posterize).Set("LEVELS", 2f);
var fine = Shaders.CreateMaterial2d(posterize).Set("LEVELS", 8f);
```

Materials with different values have pipelines of their own, made when each is first drawn and
again whenever a value changes, so a constant suits what is chosen once rather than what moves
every frame, which a uniform is for. A pass or a dispatch has one pipeline for every instance of
its program, so it runs with the shader's own value, and setting a constant on an instance is
refused.

### Every entity's own values

A material shared by many entities can still give each its own values, read from a buffer at the
entity's mesh tag, as Bevy's `GpuComponentArrayBuffer` does. `app.AddComponentArray<T>()` keeps
every entity's `T` in one storage buffer, written again at the end of each frame where a component
was added, changed or taken off, and gives each entity a mesh tag holding its place, moving the
last entry into a place given up so the array stays packed:

<!-- compiled with:
using System.Numerics;
ShaderProgram tinted = default;
-->
```csharp
[Behavior]
public partial struct Tint
{
    public Vector4 Color;
}

var tints = app.AddComponentArray<Tint>();

app.Startup(ctx =>
{
    var material = Shaders.CreateMaterial(tinted).SetBuffer("tints", tints.Buffer);
    var cube = ctx.Ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), material, Transform.Identity);
    ctx.Ecs.Add(cube, new Tint { Color = new Vector4(1f, 0.5f, 0f, 1f) });
});
```

```slang
import bcs;

struct Tint
{
    float4 color;
};

StructuredBuffer<Tint> tints;

[shader("fragment")]
float4 fragment(bcs::VertexOutput mesh) : SV_Target
{
    return tints[bcs::tag(mesh.instance_index)].color;
}
```

The component crosses as C# lays it out, and a structured buffer gives a `float3` sixteen bytes, so
a struct shared with one spells its padding out. The tag is the array's, so an entity in it has no
tag of its own to give, and `bcs2d::tag` reads it the same way on a 2D mesh.

### On a 2D mesh

A program draws a 2D mesh with its 2D stages, and a 2D camera draws the material as Bevy draws a
`Material2d`. Its fragment shader imports `bcs2d`, Bevy's 2D view and mesh, and takes the
`bcs2d::VertexOutput` Bevy's own 2D vertex shader hands on, which puts its values elsewhere than
the 3D one does. Its own values are declared and set by name as a 3D material's are, and a 2D
vertex shader of the program's own places the mesh where Bevy's would leave it:

<!-- compiled with:
Entity square = default;
AssetHandle icon = default;
-->
```csharp
var program = Shaders.CreateProgram(new ShaderProgramSettings { Fragment2d = "shaders/tinted.slang" });
var material = Shaders.CreateMaterial2d(program, AlphaMode2d.Blend)
    .Set("tint", new Vector4(0f, 0f, 1f, 1f))
    .SetTexture("picture", icon);
Render2d.SetMesh(ctx.Ecs, square, Render.CreateMesh(MeshShape.Rectangle, 1f, 1f));
Render2d.SetMaterial(ctx.Ecs, square, material);
```

```slang
import bcs2d;

uniform float4 tint;
Texture2D picture;
SamplerState picture_sampler;

[shader("fragment")]
float4 fragment(bcs2d::VertexOutput mesh) : SV_Target
{
    return tint * picture.Sample(picture_sampler, mesh.uv);
}
```

A 2D material is opaque, masked or blended, which is every way Bevy's 2D pipeline draws, and casts
no shadow, since 2D has none. A blended one is mixed with what is behind it by its alpha, and a
masked one is drawn in the alpha-masked pass and discards where its fragment shader does, as Bevy's
own 2D materials do. One program may have 3D stages and 2D stages both, and draws each kind of
material with its own.

### On a sprite

A 2D material given to a sprite draws the sprite, as Bevy's `SpriteMaterial` does. Bevy draws a
sprite as a quad it sizes to the image and moves by the anchor, and the sprite keeps that quad, so
the program brings a 2D fragment shader and no vertex shader. The fragment shader imports
`bcs_sprite` beside `bcs2d` for what the sprite says, its image, color, flipping, tiling, slicing
and atlas, with Bevy's own functions for them. `sample_final_color` draws the sprite as Bevy would,
and `sample_sprite_texture` and `get_final_color` are its two halves, the image where the sprite
puts it and then the sprite's color and alpha mode:

<!-- compiled with:
Entity bird = default;
-->
```csharp
var program = Shaders.CreateProgram(new ShaderProgramSettings { Fragment2d = "shaders/fade.slang" });
var fade = Shaders.CreateMaterial2d(program).Set("amount", 0.5f);
Render2d.SetSprite(ctx.Ecs, bird, AssetServer.Load(AssetKind.Image, "branding/bevy_bird_dark.png"));
Render2d.SetMaterial(ctx.Ecs, bird, fade);
```

```slang
import bcs2d;
import bcs_sprite;

uniform float amount;

[shader("fragment")]
float4 fragment(bcs2d::VertexOutput input) : SV_Target
{
    let color = bcs_sprite::sample_sprite_texture(input.uv);
    return bcs_sprite::get_final_color(float4(color.rgb, color.a * (1.0 - amount)));
}
```

A material made without an alpha mode draws the sprite as the sprite's own alpha mode says, as a
Bevy sprite material naming none does, and one made with one draws by it, `get_final_color` applying
its mask cutoff. Sprites with the same image and settings share one copy of the material, which
keeps them in one batch, and a value set on the material reaches them in the next frame, as it does
in Bevy. A program reading `bcs_sprite` draws only sprites, and one with a 2D vertex shader of its
own draws none.

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

Bevy's lighting is its own WESL over bindings that change with every feature a camera turns on, so
it is not written again in Slang. The two calls reach functions by fixed names that the bridge puts
in front of the compiled shader, over Bevy's own `apply_pbr_lighting` and
`main_pass_post_lighting_processing`, only where the shader calls them, and Bevy composes the two
together as it composes its own shaders. A lit material is drawn in the forward pass, even under a
camera rendering deferred, and the surface receives shadows and takes fog as a standard material
does by default.

### Drawn deferred

A material whose program has a deferred stage writes its surface into Bevy's deferred buffers, and
Bevy's deferred lighting pass lights it with the rest of the scene. Screen-space reflections read
those buffers, so water that reflects what stands around it is drawn this way. Like Bevy's own
deferred materials it is drawn by a camera that draws deferred and by no other. The stage returns
`bcs::deferred(surface, mesh)` from the prepass's vertex output, which a prepass vertex shader of
the program's own writes whole:

```csharp
var water = Shaders.CreateMaterial(Shaders.CreateProgram(new ShaderProgramSettings
{
    PrepassVertex = new ShaderStage("shaders/water.slang", "prepass_vertex"),
    Deferred = "shaders/water.slang",                // its entry point is called deferred
}));
```

```slang
[shader("vertex")]
bcs::PrepassVertexOutput prepass_vertex(bcs::PrepassVertex v)
{
    return bcs::prepass_output(v.instance_index, v.position, v.normal, v.uv);
}

[shader("fragment")]
bcs::Deferred deferred(bcs::PrepassVertexOutput mesh)
{
    var surface = bcs::surface(mesh);
    surface.roughness = 0.05;
    surface.normal = ripples(mesh);                     // a normal of the shader's own
    return bcs::deferred(surface, mesh);
}
```

The surface is packed as Bevy packs a standard material's, by WESL the bridge puts in front of the
compiled stage over Bevy's own deferred functions, and the normal and motion a camera's prepass
draws are written beside it.

Geometry a camera draws itself writes into the same buffers from inside the prepass, which is how
a surface found by marching a ray rather than drawn from a mesh is lit by Bevy (see the deferred
buffers under [Compute on a camera](compute.md#compute-on-a-camera)).

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

They reach Bevy's own walk through the decals over a point, its `ClusteredDecalIterator`, by WESL
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
