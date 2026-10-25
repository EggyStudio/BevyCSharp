# Reflections and the sky

Reflections on the screen and in the world, the sky a camera draws behind the scene, and the light
probes that light a scene from around it.

### Reflections

A camera can reflect what it sees in the surfaces it draws:

<!-- compiled with:
Entity camera = default;
-->
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

<!-- compiled with:
Entity camera = default;
-->
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

<!-- compiled with:
Entity camera = default, sun = default;
-->
```csharp
Render.SetAtmosphere(camera, new AtmosphereSettings
{
    Planet = SkyPlanet.Mars,
    DustPhase = AssetServer.Load(AssetKind.Image, "textures/mars_mie_phase.ktx2"),
    Method = SkyMethod.Raymarched,
});
Render.SetSunDisk(sun, SunDisk.Mars);
```

`Planet` is earth's air unless set, or Mars's, thin and dusty, whose dust scatters light by the
picture `DustPhase` names, Bevy's `textures/mars_mie_phase.ktx2` among its assets. Changing it moves
the planet so its ground stays at the origin. `Method` works the sky out from lookup textures, Bevy's
quick default for a scene inside the air, or by marching rays, slower and right from far off, a
planet seen from orbit included, with sharp shadows in fog. `SetSunDisk` sets how wide and bright
the sun a directional light stands for looks in the sky, earth's unless set, Mars's smaller, and
`SunDisk.Off` none, without changing what the light lights.

`Medium` gives the planet an air of the game's own in place of earth's or Mars's, Bevy's
`ScatteringMedium::new`, a list of terms, each a gas or a dust with the red, green and blue light it
absorbs and scatters a meter where it is densest, how it thins with height, linearly, exponentially
or as a layer about one height, and how it scatters by direction, evenly, as a gas or as a dust
leaning forward. The planet keeps its size from `Planet`, and an air of the game's own needs no
`DustPhase`. A term that absorbs or scatters less than nothing, or a dust that sends all its light
one way, is refused with its reason.

<!-- compiled with:
Entity camera = default;
-->
```csharp
// A gas that scatters red more than blue, under a sky the color of rust.
Render.SetAtmosphere(camera, new AtmosphereSettings
{
    Medium =
    [
        new ScatteringTerm
        {
            Scattering = new Vec3(33.1e-6f, 13.6e-6f, 5.8e-6f),
            Falloff = ScatteringFalloff.Exponential,
            FalloffScale = 0.13f,
            Phase = ScatteringPhase.Rayleigh,
        },
    ],
});
```

`ScatteringTerm.Earth()` writes earth's air out as such a list, Bruneton's amounts as Hillaire's sky
takes them, each color read at one wavelength, for a game that bends it a little, and
`ScatteringTerm.EarthArc()` is the same air with the gas's and the ozone's amounts weighed over the
whole visible spectrum, as the Atmosphere Rendering Parameter Calculator works them out. The two
read alike by day, and at sunset Bruneton's is orange under purple where the calculator's is gold
under a gray green, as a spectral reference of the sky is. The sky worked out from lookup textures
lies within a level of the one raymarched for every pixel at every height of the sun.

<!-- compiled with:
Entity camera = default;
-->
```csharp
Render.SetAtmosphere(camera, new AtmosphereSettings { Medium = ScatteringTerm.EarthArc() });
```

Bevy dims and reddens a directional light through the air the atmosphere describes as it lights the
scene, and sets it behind the planet, so a sun under an atmosphere is given as it shines above the
air, at its noon strength and white, and a day's cycle moves it alone. A curve that dims it toward
the horizon as well counts the air twice and leaves the sunset no light to scatter.

`SetSkyLighting` derives an environment map from that atmosphere each frame, so a surface picks up
the color of what is around it rather than only what a lamp points at it, and the light follows the
sun without anything being animated. The size it generates is a square cubemap resolution and has to
be a power of two. `ClearSkyLighting` takes it off.

The distance fades into the sky with fog drawn in the sky's own color:

<!-- compiled with:
Entity camera = default;
-->
```csharp
Render.SetSkyLighting(camera);
Render.SetSkyFog(camera, new SkyFogSettings { Visibility = 700f, Height = 40f });
```

An atmosphere's haze is earth's, too faint at a few hundred meters to blend a level's far ground
into the horizon, so games draw fog of their own over it. This fog is a layer over the ground,
densest at `Base` and thinning by e every `Height` above it, which takes away 95% of a point's light
`Visibility` away at its base, so a hill's top stands clear of a valley's haze. Its color along each
way is the sky's a few degrees above the horizon there, read from the environment map
`SetSkyLighting` makes, warm toward the sun, blue away from it, and following the hour. Below the
horizon, where an atmosphere draws the planet's ground as an even gray, it draws the horizon's
color, darkening downward over `BandDegrees` to `GroundShade` of it. `Color` gives it a color of its
own in place of the sky's, which a camera with no environment map needs. It reads the camera's
depth, so it needs a camera drawn once a pixel, and `ClearSkyFog` takes it off. The console's
`sky.fog` sets it while the app runs.

Near the camera the same fog can be worked out in froxels, cells of the view's frustum:

<!-- compiled with:
Entity camera = default;
-->
```csharp
Render.SetSkyFog(camera, new SkyFogSettings
{
    Visibility = 700f,
    Height = 40f,
    Froxels = new FroxelFogSettings { Quality = FroxelQuality.Medium, Distance = 250f },
});
```

Each froxel holds the layer's density, made lumpy by a noise the wind carries (`Lumps`, `LumpSize`,
`Wind`) and thickened inside any `FogBox` the program names, and the light the fog scatters there.
That light is the sky's along the way, as the closed form colors it, split into the sky's own, which
reaches every point, and the sun's share, which the sun's shadow takes away, so a wall's shadow is a
bluer, darker shaft through the fog and a pillar between the eye and the sun darkens the whole way
to it. Point and spot lights add their own light, so a lamp glows in the fog. The froxels are added
up from the eye and laid over the picture as far as `Distance`, and past it the closed form goes on,
so the two meet with no seam. Over the sky only the shadows, the lamps and the boxes are laid, since
the sky already holds the layer's haze. Each froxel's point moves through it from frame to frame and
blends with what it held last frame, which smooths the slices over a few frames. At 1280 by 720 the
froxels take about 0.1 ms of the GPU at `Low`, 0.2 at `Medium` and 0.5 at `High`. The console's
`sky.froxels` turns them on and off while the app runs.

Clouds fill the sky above the atmosphere's camera:

<!-- compiled with:
Entity camera = default, sun = default;
-->
```csharp
Render.SetClouds(camera, new CloudSettings { Coverage = 0.45f, Quality = CloudQuality.Medium, ShadowLight = sun });
```

They lie in a layer between `Bottom` and `Top` over the planet, shaped as Schneider's clouds for
Horizon Zero Dawn are. A weather map says where the sky has cloud and what kind, flat heaps or
towers, `Coverage` how much of it does, tiling noise `ShapeSize` across cuts each body and finer
noise `DetailSize` across wears its edges, and `Wind` carries all of it. `Stratus` turns the cover
from heaps into a flat deck low in the layer, its edge ragged where the weather map thins it, which an
overcast or a rainy sky is. The sun's light reaches
them through the atmosphere, so a sunset's clouds glow gold from below, a forward lobe silvers an
edge against the sun, and scattering many times over keeps a thick cloud's sides white. A far cloud
fades into the sky's color, and the sun's disc does not show through a cloud before it. The clouds
are marched at half the picture's size and settle over a few frames, and at 1280 by 720 take about
0.3 ms of the GPU at `Low`, 0.5 at `Medium` and 1 at `High`. They lie behind every piece of geometry
and in front of the sky's fog. With `ShadowLight` they shadow the sun, how much of its light gets
through worked out over a square `ShadowSize` wide about the camera and laid over the light as its
picture, so their shadows drift over the ground and the fog in froxels darkens in them, for under a
tenth of a millisecond more. The light's global transform takes the square's scale and middle each
frame and its turn stays the program's. On a camera lit by the sky (`SetSkyLighting`), the clouds
are laid over the sky its lighting is filtered from as well, marched each frame into a small cube of
their own after Bevy's atmosphere has drawn the sky, so an overcast grays and dims the light the
ground and the rain are lit by and a clear patch lets its blue through, for some 0.18 ms more. The
console's `sky.clouds` sets them while the app runs.

The sky has a clock, and behind the atmosphere the stars, the galaxy and the moon stand by it:

<!-- compiled with:
Entity camera = default, sun = default, moon = default;
float delta = default;
-->
```csharp
var clock = new SkyClock { Latitude = 50f, DayOfYear = 172f, Hour = 21f };
Render.SetNightSky(camera, new NightSkySettings { Clock = clock });
Render.SetSunDisk(moon, SunDisk.Off);              // the night sky draws the moon's disc

clock.Advance(delta / 60f);                        // an hour a minute, every frame
```

`SkyClock` works out where the sun, the moon and the stars stand at an hour of a day of the year for
a latitude, by the usual spherical astronomy, with north along negative Z and east along positive X.
`SunDirection` and `MoonDirection` aim a directional light, the day of the year moves the sun by the
earth's `AxialTilt`, so summer days are long, and a tilt of none keeps every day the equinox's. The
moon goes about the sky once a synodic month behind the sun, so `MoonPhase` and `MoonLit` follow the
days, and `Phase` says whether it is night, dawn, day or dusk. `Render.SetNightSky` adds the stars,
the galaxy's band and the moon's disc over the sky through as much of their light as the atmosphere
lets through, so they fade and redden toward the horizon. The stars and the galaxy turn about the
pole star with the clock's sidereal time and come out as the sun goes down, and the moon's disc is
lit from the sun's way, so its phase is the clock's. The clouds and the fog lie over all of it, and
they are lit by whichever directional light gets most through the air, so at night the moon lights
them.

Rain and snow fall about the camera:

<!-- compiled with:
Entity camera = default;
-->
```csharp
Render.SetPrecipitation(camera, new PrecipitationSettings { Rain = 0.6f, Wind = new Vec3(3f, 0f, 1f) });
```

The drops and the flakes are points moved on the GPU every frame in two boxes held in the world
about the camera, a near one holding most of them and a far one `Reach` to each side, so walking
moves through the rain and a drop leaving one side comes in at the other. A drop falls as fast as
one its size does, the drops larger the harder it rains, and is drawn as the streak it makes over a
camera's shutter. A flake drifts down at about a meter a second, fluttering, and both drift on
`Wind`. Those in front of the camera are counted into an indirect draw with the light each sends
toward the eye, the sun's through the air, its shadows and the clouds', the sky's and the lamps', so
rain glints toward a low sun and shows in a lamp's light at night. They are drawn at
`FramePoint.AfterTemporalAntialiasing`, since temporal antialiasing would average away a speck
moving with no motion of its own in the prepass. With `Roofs`, a camera of its own looking down
draws what stands over the ground about the camera, 96 meters across, into a map of heights, again
whenever the camera has moved some 12 meters, and the rain keeps out from under a roof, a tree or a
bridge and splashes where it lands close by. Whatever moves is drawn into the map where it stood, so
a game puts what moves on a layer of its own and leaves it out of `RoofLayers`. At 1280 by 720 the
rain takes some 0.05 ms of the GPU at `Medium`, and the map some 0.02 ms on the frames it is drawn,
beside the scene drawn again from above.

Lightning strikes in a storm about the camera:

<!-- compiled with:
Entity camera = default;
LightningStrike? last = null;
-->
```csharp
Render.SetThunder(camera, new ThunderSettings { Rate = 4f, Struck = strike => last = strike });
```

Strikes come at random, `Rate` a minute on average with no beat to them, between `Nearest` and
`Farthest`, two in three as a branching bolt from the clouds' base to the ground and the rest inside
the cloud. Each flickers for half a second, as a real strike's return strokes come down its channel
one after another, and while it does, a point light at the bolt lights the scene and the fog in
froxels, the camera's clouds glow from within about it, and the bolt is added to the picture after
temporal antialiasing, a jagged channel split seven times over with a few dimmer branches, fading
into the clouds' base. The thunder is made as it plays from noise, a crack and a rumble rolling for
seconds, sharper and louder from a near strike and lower and longer from a far one, and is heard
once sound at 343 meters a second has come from the strike. `Struck` is told of each strike with its
place, its distance and the thunder's delay, `Render.StrikeLightning` strikes at once, at a place or
where the storm chooses, and the console's `sky.strike` does the same while the app runs. A bolt
costs some 0.02 ms of the GPU while it flashes.

A weather moves the clouds, the fog, the rain and the lightning together:

<!-- compiled with:
Entity camera = default, sun = default;
SkyClock clock = new();
-->
```csharp
var weather = new WeatherState { Clock = clock, Forecast = true, Climate = WeatherClimate.Maritime };
Render.SetWeather(camera, weather, new CloudSettings { ShadowLight = sun }, new SkyFogSettings { Visibility = 900f }, new PrecipitationSettings(), new ThunderSettings());

weather.Forecast = false;
weather.Set(WeatherPreset.Storm);                  // eased into over a minute or so
```

`WeatherState` holds the conditions of the moment in a `WeatherConditions`, how much cloud and how
much of it a deck, the clouds' base and top, how hard it rains or snows, how thick the fog is, the
wind and the temperature, and eases them toward a target by half the way every `HalfLife` seconds,
so a front comes in over a minute rather than at once. `Set` gives it one of the presets, or
conditions of the program's own, to ease toward or to take at once. With `Forecast` on it writes the
target itself from the clock's day and hour, weather systems passing `SystemsPerDay` times a day
between clear skies and thunderstorms as stormy as the `Climate` makes them, colder in winter and
at night, the rain falling as snow below a degree and a mist lying on calm mornings, the same for
the same `Seed` on every machine. `Wind` is the wind of the moment, gusting by the conditions'
`Gusts`, and `Wetness` and `SnowCover` say how wet the ground is and how much of it lies under snow,
for a program's materials to darken and whiten by. `Render.SetWeather` moves the state on every
frame by the game's clock and lays its conditions over the cloud, fog, rain and storm settings given
beside it, which take the place of the camera's own `SetClouds`, `SetSkyFog`, `SetPrecipitation` and
`SetThunder`. The
clouds take the cover, the deck, the base, the top, the density and the wind, the fog thins toward
some eighty meters as the weather's fog thickens, and its froxels drift on the same wind, and it
rains and snows as hard as the weather does, on the gusting wind, and lightning strikes as often as
its `Thunder` says. Any set of settings can be none, and that part stays the program's. The console's `sky.weather` gives the cameras a preset, or
the forecast, while the app runs.

A painted sky is a cubemap instead:

<!-- compiled with:
Entity camera = default;
-->
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

<!-- compiled with:
Entity camera = default;
-->
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

<!-- compiled with:
Entity camera = default;
AssetHandle diffuse = default, specular = default;
-->
```csharp
Render.SetEnvironmentMap(camera, diffuse, specular, intensity: 3000f);
```

The first is the blurred map a rough surface reflects and the second the sharp one a polished
surface reflects. It costs nothing at startup, which suits a shipped game and an environment too
large to filter again. Passing `AssetHandle.None` for either takes the lighting off, since a baked
map is the pair and half of one is not a weaker version of it.

### The weather

The weather is a sky around Bevy's atmosphere, with clouds marched through a shell over the planet
and their shadows on the ground, fog, rain, snow and thunder, stars, a galaxy and a moon with its
phase, from the `bevy_weather` crate the bridge's render profile carries, kept in the bridge's own
source. `Config.Weather` asks for it, and a camera sees it once it carries `WeatherCameraRef`:

<!-- compiled with:
Entity camera = default;
-->
```csharp
ctx.Ecs.Insert<WeatherCameraRef>(camera);
var clock = ctx.Ecs.Resource<WeatherTimeRef>()!.Value;
clock.Latitude = 52f;
clock.TimeOfDay = 0.75f;                                        // six in the evening
var forecast = ctx.Ecs.Resource<ProceduralWeatherRef>()!.Value;
forecast.Enabled = false;                                       // so a preset holds
Weather.SetPreset(WeatherPreset.Rain);                          // eased into
```

The weather brings a planet with its atmosphere, so a weather camera needs no `SetAtmosphere`, and a
sun and a moon, made where the app has marked none of its own lights with `SunLightRef` or
`MoonLightRef`. A light marked so is steered across the sky from then on. The clock runs a day in
`DayLengthSecs` unless it is `Paused`, `TimeOfDay` running from midnight at zero through noon at a
half, and the forecast writes the conditions from it while `ProceduralWeatherRef.Enabled` is on, so
the same clock gives the same sky on every machine. With the forecast off, a preset or the
conditions themselves (`WeatherRef.TargetCloudCoverage` and the rest) say what the weather eases
toward. `WeatherConfigRef.Quality` is one tier, from `Potato` to `Ultra`, over the clouds' samples,
the fog and the particles, with a switch for each part beside it.

The weather puts on each weather camera the exposure its sky is calibrated for, and leaves the
tonemapper and the bloom to `SetPostProcessing`. A headless app has no weather, and an app where
meshlets run has none either, since a camera under its atmosphere would end the app there, which
`Weather.Active` reports. What it costs is in [Compared with Bevy](compared-with-bevy.md).

### Light probes

A camera's environment lights everything it sees the same way, which is wrong the moment the scene
has a room in it. A hall should reflect its own walls rather than the sky outside, and a corner by a
red carpet should be warmer than the ceiling. A light probe is a box in the scene that lights what
is inside it instead.

<!-- compiled with:
EcsWorld ecs = ctx.Ecs;
AssetHandle diffuse = default, specular = default;
-->
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

<!-- compiled with:
Entity hall = default;
-->
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

    let world = box_center + irradiancePointInBox(point, grid) * box_size;

    for (uint side = 0; side < 6; side++)
    {
        light.Store(irradianceTexel(point, side, grid), trace(world, side));
    }
}
```

A grid `x` by `y` by `z` points is an image `x` wide, twice `y` high and three times `z` deep, one
region for each sign of each axis, and `irradianceTexel` finds the texel for a point and a
direction so a shader never deals with that packing. The sides are the way a surface faces, so
`IrradiancePositiveY` lights a floor. Bevy samples the image filtered, which is why it is
`Rgba16Float`, and the shader says so with `[format("rgba16f")]`, since Slang would otherwise assume
a wider format than the image has. A baked volume can come from a file instead. A camera is refused
as a probe, since a camera is lit by `SetEnvironmentMap`, and `AssetHandle.None` takes either kind
off.

A reflection probe can also render its own maps rather than being given them:

<!-- compiled with:
Entity hall = default;
-->
```csharp
Render.SetProbeCapture(hall, new ProbeCaptureSettings { Size = 256, Live = true });
```

Six cameras at the probe's center draw the scene into the faces of a cube, and Bevy filters the cube
into the probe's light on the GPU, so a mirror in the hall shows the hall as it is now, including
whatever walks through it. That costs six more drawings of the scene a frame, which is why
`Live = false` captures once instead, over the first frames after the scene has finished compiling,
and `Render.RecaptureProbe` takes it again when the room changes. The faces are drawn at Bevy's
default exposure and the default `Intensity` undoes it. The cameras see the probe's own light, so
each capture reflects the last one and adds a reflection of a reflection each frame, which settles
at that intensity and brightens without end well above it.

---

Before this, [Cameras and light](cameras-and-light.md).
Next, [Ray tracing](ray-tracing.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#3d-rendering). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#drawing). The [guide's contents](../README.md#guide) list every page.
