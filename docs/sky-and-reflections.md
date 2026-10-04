# Reflections and the sky

Reflections on the screen and in the world, the sky a camera draws behind the scene, and the light
probes that light a scene from around it.

### Reflections

A camera can reflect what it sees in the surfaces it draws:

```csharp
Render.SetScreenSpaceReflections(camera, new ReflectionSettings());
```

Each pixel of a shiny surface marches a ray through the depth buffer until it passes behind
something, and takes the color already drawn there. It is cheap and it is exact where it works, but
it can only reflect what is on screen, so a reflection fades out toward the edge of the picture and
anything behind the camera is never in it. A reflection probe or a traced reflection fills that in.

Bevy reads what a surface is from its G-buffer, so turning reflections on also turns on
`Render.SetDeferredRendering(true)` and gives the camera the depth and deferred prepasses. Deferred
rendering applies to Bevy's own materials; one drawn by a Slang program writes a color rather than a
description of its surface, so it is drawn forward either way and is reflected without reflecting.
The camera has to draw once a pixel (`Msaa = 1`).

Which surfaces reflect is decided by roughness. Bevy leaves a surface smoother than 0.08 without a
reflection and fades reflections in until 0.12, because a mirror-smooth surface shows every flaw of
a screen-space trace, and fades them out again past 0.55. `FadeInRoughness` and `FadeOutRoughness`
move both ends, `Thickness` is how deep a surface in the depth buffer is taken to be, which decides
whether a ray passing behind it hit it, and `Steps` and `RefineSteps` trade the cost of the march
against how finely a hit is found. `null` takes reflections off, and deferred rendering stays on
until it is asked off, since other cameras may be reading it.

### The sky

The sky can be scattered rather than painted:

```csharp
Render.SetAtmosphere(camera, new AtmosphereSettings());
Render.SetPostProcessing(camera, new PostSettings { Hdr = true });
Render.SetSkyLighting(camera, intensity: 1f);      // and let it light the scene
```

Bevy computes the color of every direction from how far sunlight travels through the air to reach
it, so the horizon reddens, the zenith stays pale, and the whole sky turns over as the sun moves.
Distant geometry picks up the same haze. The sun is whichever directional light is in the scene, so
pointing that light differently moves the sky, and a scene with no directional light gets a night
sky.

The sky is a planet-sized entity that the camera looks out from, and `SetAtmosphere` keeps at most
one of them, so calling it for a second camera adds a viewer rather than a second sky. The planet is
measured in meters with its ground at the origin, which is why a scene measured in something else
sets `Scale` rather than moving anything. `Density` thickens or thins the air, `HazeDistance`
decides how far ahead the haze is computed, `GroundAlbedo` is how much light the ground bounces
back into it, and `ClearAtmosphere` takes the sky off a camera again. `Quality` is one number over
the dozen Bevy exposes, because every one of them trades the same thing; the sky is the same at
each setting, and what changes is banding in a gradient and how much of a frame it costs.
The camera is given a high dynamic range target either way, because a sun scattered through air is
far brighter than white.

`SetSkyLighting` derives an environment map from that atmosphere each frame, so a surface picks up
the color of what is around it rather than only what a lamp points at it, and the light follows the
sun without anything being animated. The size it generates is a square cubemap resolution and has to
be a power of two. `ClearSkyLighting` takes it off.

A painted sky is a cubemap instead:

```csharp
Render.SetSkybox(camera, AssetServer.Load(AssetKind.Image, "sky.png"), brightness: 1500f);
```

The file is six square faces, in a column, which is the layout most cubemap textures ship in, in a
row, or in a horizontal or vertical cross, as cubemaps exported from a painting tool often are. The
shape says which, and it is turned into a cube once it has decoded. A cubemap shipped as six files
is put together with `Render.CubemapFromFaces(px, nx, py, ny, pz, nz)`, whose handle can be given
to the skybox at once. `brightness` is in candelas per square meter like the rest
of the lighting, so the useful numbers are in the hundreds or thousands; a brightness of one is a
night sky and comes out black. A skybox is seen behind the scene and does not light it.

The same file can light it, though, for a scene lit from a photograph of a real place:

```csharp
Render.SetImageLighting(camera, AssetServer.Load(AssetKind.Image, "sky.png"), intensity: 3000f);
```

The cubemap is filtered on the GPU into the blurred versions a surface reflects, so a rough material
picks up the average color around it and a polished one picks up a recognizable reflection, with no
bake step and no second file. `rotation` turns the environment without touching the scene. Each face
has to be square and a power of two, and the light waits for the image to decode before it is
applied, so a handle asked for in the same frame the camera is spawned works.

`Render.SetEnvironmentMap` is the other end of that, taking the two maps a baking tool already
produced rather than filtering one at startup:

```csharp
Render.SetEnvironmentMap(camera, diffuse, specular, intensity: 3000f);
```

The first is the blurred map a rough surface reflects and the second the sharp one a polished
surface reflects. It costs nothing at startup, which suits a shipped game and an environment too
large to filter again. Passing `AssetHandle.None` for either takes the lighting off, since a baked
map is the pair and half of one is not a weaker version of it.

### Light probes

A camera's environment lights everything it sees the same way, which is wrong the moment the scene
has a room in it. A hall should reflect its own walls rather than the sky outside, and a corner by a
red carpet should be warmer than the ceiling. A light probe is a box in the scene that lights what
is inside it instead.

```csharp
var hall = ecs.Spawn();
ecs.Add(hall, new Transform { Translation = new Vec3(0f, 2f, 0f), Rotation = Quat.Identity, Scale = new Vec3(12f, 4f, 20f) });

Render.SetReflectionProbe(hall, diffuse, specular, intensity: 3000f, falloff: new Vec3(0.2f));
```

The box is the entity's transform, a unit cube before its scale. A reflection probe takes the pair
of maps `SetEnvironmentMap` takes, captured from inside the room, and a surface inside the box
reflects them, corrected for where in the box it stands so a wall close by looks close. `falloff`
fades the probe's influence across that fraction of the box on each axis, so overlapping probes
blend into each other as something walks from one room to the next.

An irradiance volume is the other kind, and the one global illumination is built on:

```csharp
const uint grid = 16;
var light = Shaders.CreateImage(grid, grid * 2, ShaderImageFormat.Rgba16Float, depth: grid * 3);

Render.SetIrradianceVolume(hall, light, intensity: 1000f);
```

It holds, for a grid of points across the box, the light a surface facing each of the six axis
directions receives there, and a surface blends the points around it by its normal. That is light
that varies through space, which a map the same everywhere cannot be, and Bevy's materials take it
as their diffuse light in place of the environment's. The image is an ordinary 3D image, so a
compute shader can write it every frame and whatever it works out lights everything drawn with a
material from `Render.CreateMaterial`, with no change to those materials:

```slang
import bcs_compute;
import bcs_scene;

uniform uint3 grid;
uniform float3 box_center;
uniform float3 box_size;
[format("rgba16f")] WTexture3D<float4> light;

// The technique itself: the light a surface at `world` facing `side` receives.
float4 trace(float3 world, uint side);

[shader("compute")]
[numthreads(4, 4, 4)]
void main(uint3 point : SV_DispatchThreadID)
{
    if (any(point >= grid)) return;

    let world = box_center + bcs_scene::irradiance_point_in_box(point, grid) * box_size;

    for (uint side = 0; side < 6; side++)
    {
        light.Store(bcs_scene::irradiance_texel(point, side, grid), trace(world, side));
    }
}
```

A grid `x` by `y` by `z` points is an image `x` wide, twice `y` high and three times `z` deep, one
region for each sign of each axis, and `irradiance_texel` finds the texel for a point and a
direction so a shader never deals with that packing. The sides are the way a surface faces, so
`bcs_scene::POSITIVE_Y` lights a floor. Bevy samples the image filtered, which is why it is
`Rgba16Float`, and the shader says so with `[format("rgba16f")]`, since Slang would otherwise assume
a wider format than the image has. A baked volume can come from a file instead. A camera is refused
as a probe, since a camera is lit by `SetEnvironmentMap`, and `AssetHandle.None` takes either kind
off.

A reflection probe can also render its own maps rather than being given them:

```csharp
Render.SetProbeCapture(hall, new ProbeCaptureSettings { Size = 256, Live = true });
```

Six cameras at the probe's center draw the scene into the faces of a cube, and Bevy filters the
cube into the probe's light on the GPU, so a mirror in the hall shows the hall as it is now,
including whatever walks through it. That costs six more drawings of the scene a frame, which is
why `Live = false` captures once instead, over the first frames after the scene has finished
compiling, and `Render.RecaptureProbe` takes it again when the room changes. The faces are drawn at
Bevy's default exposure and the default `Intensity` undoes it. The cameras see the probe's own
light, so each capture reflects the last one and light bounces further each frame, which settles
at that intensity and brightens without end well above it.

---

Before this, [Cameras and light](cameras-and-light.md).
Next, [Ray tracing](ray-tracing.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#3d-rendering). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#drawing). The [guide's contents](../README.md#guide) list every page.
