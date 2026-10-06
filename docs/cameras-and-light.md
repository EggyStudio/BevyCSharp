# Cameras and light

Cameras and what they make of the picture, the shadows lights cast, and the lens a camera is metered
through.

### Cameras

A camera and a light take settings, and every value has a usable default:

```csharp
var camera = Render.SpawnCamera3d(new CameraSettings
{
    FieldOfView = 55f,
    Clear = ClearMode.Custom,
    ClearColor = (0.02f, 0.03f, 0.05f, 1f),
});

Render.SpawnLight(new LightSettings
{
    Kind = LightKind.Spot,
    Intensity = 40_000f,
    Color = (0.4f, 0.6f, 1f),
    OuterAngle = 0.5f,
});
```

A camera placed by a transform and a point light at a place, as Bevy's examples spawn them, are one
call each, with Bevy's defaults where nothing is said:

```csharp
ctx.Ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2.5f, 4.5f, 9f), Vec3.Zero, Vec3.UnitY));
ctx.Ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);
```

`CameraProjection.Orthographic` swaps perspective for a fixed vertical `Height`, for an isometric or
top-down view. `Order` decides which camera draws over which, and `ClearMode.Keep` layers one on
another. A light is aimed by its `Transform`, since a directional or spot light shines down its own
negative Z, which `Transform.LookingAt` produces.

`Viewport` gives a camera part of the window instead of all of it, for splitscreen, and `Layers`
decides what a camera can see at all, for a minimap:

```csharp
const uint Minimap = 1u << 1;

Render.SpawnCamera3d(new CameraSettings { Viewport = (0, 0, 640, 720) });
Render.SpawnCamera3d(new CameraSettings
{
    Viewport = (640, 0, 640, 720),
    Order = 1,
    Clear = ClearMode.Keep,     // or it would wipe out the first camera's half
    Layers = Minimap,
});

Render.SetLayers(ctx.Ecs, marker, Minimap);        // only the minimap draws it
Render.SetLayers(ctx.Ecs, player, 1u | Minimap);   // both do
```

A viewport is measured in physical pixels rather than logical ones, because a framebuffer is divided
into those. A camera draws an entity only where their layers overlap.

`Render.SetPerspective(camera, fieldOfView, near, far)` changes a camera's lens while it runs,
since `CameraSettings` only decides it when the camera is made.

The part of the window no viewport covers is the world's clear color, which `Render.SetClearColor`
sets, since Bevy clears the whole window before putting each camera's picture on its part of it.
`Render.SetRoundedCorners(camera, radius, fill)` takes a camera's corners off, antialiased, the
viewport's where it has one and the whole picture's where it does not. Both reach the desktop
on a window made with `Config.Transparent`, which is see-through wherever what is drawn has no
alpha:

```csharp
config.Transparent = true;                        // before the app is built

Render.SetClearColor((0f, 0f, 0f, 0.6f));                      // round the viewport, dimmed
Render.SetRoundedCorners(camera, 16f, (0f, 0f, 0f, 0.6f));     // corners showing the same
Render.SetRoundedCorners(fullWindowCamera, 16f);               // or clear, for a round window
```

A rounded corner shows the fill, clear unless given, so a viewport's corners can match the
clear color round them rather than being clear notches in it. A camera keeping a background of its
own there clears to it (`ClearMode.Custom`) rather than to the world's color. Not every platform can composite a window with alpha, DirectX 12 often among them,
and there the window is made opaque, what would have been clear is black, and the log says which it
got. A clear corner on an opaque window is black too, so a corner meant to show some other color is
better painted by whatever draws over it.

### Shadows

Shadows are tuned per light and sized globally:

```csharp
Render.SpawnLight(new LightSettings
{
    Kind = LightKind.Directional,
    ShadowDepthBias = 0.05f,     // against shadow acne
    ShadowNormalBias = 1.2f,     // against acne on glancing surfaces
});

Render.SetShadowMapSize(directional: 4096);
```

Bias is per light because one light's acne is another's floating shadow. Size is one number for
every directional light and one for every point and spot light, because that is how Bevy keeps it,
and raising it costs memory and fill rate on every shadow-casting light at once.

A directional light covers the whole scene, so one shadow map stretched over all of it is coarse
near the camera, which is where it is looked at closest. `Render.SetShadowCascades` splits the
range into a few maps, each covering a nearer and smaller slice, so a shadow that looks blocky at
arm's length is this rather than a resolution. Every number it takes keeps Bevy's own when it is
left at zero.

`Render.SetLightCookie` shapes a spot light's beam with a picture, the way a gobo shapes a stage
light, so the shadow of a window frame falls on the floor without a window being there. Only the red
channel is read, so the picture says how much light gets through rather than what color it is, and
its border should be black or the light leaks past the edge of it. A point light takes a cube of six
such pictures and a directional light one tiled across the ground, through their wrappers:

```csharp
using Bevy.Reflected;

var faces = ctx.Ecs.Insert<PointLightTextureRef>(lamp);
faces.Image = AssetServer.Load(AssetKind.Image, "lightmaps/faces.png");
faces.CubemapLayout = PointLightTextureRef.CubemapLayoutVariant.CrossVertical;

var caustics = ctx.Ecs.Insert<DirectionalLightTextureRef>(sun);
(caustics.Image, caustics.Tiled) = (AssetServer.Load(AssetKind.Image, "lightmaps/caustics.png"), true);
```

A light's picture is drawn with Bevy's clustered decals, which need a GPU that binds arrays of
textures, so on one that does not the light shines as though it had none.

A shadow map is drawn at a resolution of its own, so the shadow right where a foot meets the floor
is lost in a texel or two. Contact shadows fill that in, traced from each pixel toward the light a
short way through the depth buffer:

```csharp
Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, ContactShadows = true });
Render.SetContactShadows(camera, new ContactShadowSettings(Steps: 16, Thickness: 0.1f, Length: 0.3f));
```

Only lights that cast them and cameras that draw them take part, and only what is on screen casts
one.

A real light has a size, so its shadow is sharp where an object touches the ground and blurs as it
stretches away. `Render.SetSoftShadows(light, size)` gives a light that size, the radius of a point
or spot light in world units, and Bevy widens each shadow's penumbra with the distance to what casts
it. It is noisy on its own, so it suits a camera given
`Render.SetShadowFiltering(camera, ShadowFiltering.Temporal)` with temporal antialiasing on.

### The picture the camera makes

The picture the camera makes is one call, describing the whole pipeline rather than one change
to it:

```csharp
Render.SetPostProcessing(camera, new PostSettings
{
    Hdr = true,                          // highlights brighter than white, which bloom reads
    Bloom = true,
    BloomIntensity = 0.3f,
    Tonemapper = Tonemapper.AgX,
    AntiAlias = AntiAliasPass.Fxaa,
    Msaa = 1,
    Sharpen = 0.4f,
});
```

Every effect is applied on every call, so an effect the settings leave off is taken off the camera,
which means turning bloom off is the same call as turning it on. Only a camera takes these, since it
is the camera's render graph that reads them.

A tonemapper is the curve from what was rendered, which has no upper bound, to what a display can
show, which does. All eight of Bevy's are there, from `None` through `Reinhard` to `AgX` and Bevy's
own `TonyMcMapface`; the choice is a look rather than a correctness question, and it shows most with
`Hdr` on. `Msaa` smooths the edges of geometry while the scene is rasterized, while `AntiAlias` runs
a pass over the finished picture and so also catches edges that come from a texture or a shader.
`Fxaa` is the cheap one and `Smaa` the sharper one; `Temporal` resolves each frame from the ones
before it, so it sees an edge sampled many times over, at the cost of a trail behind anything whose
motion the renderer reports wrongly. It needs a 3D camera and `Msaa = 1`, and asking for it
alongside multisampling throws rather than quietly drawing nothing. Cameras drawing to the same
target draw into one picture and have to agree on `Msaa`, so each is given the fewest samples any of
them asked for, with a line in the log for each camera lowered. The interface's overlay draws with
one, so a game with an interface draws its window with one too. Bloom scatters light out of
whatever is brighter than white, so it needs `Hdr` and something emissive to work on. To make one
object glow harder, raise its material's emissive color rather than the bloom.

Either side of that are two more calls. `SetExposure` sets the exposure the scene is metered at, in
EV-100, the photographer's number, around 15 for sunlight, 12 for an overcast day and 7 indoors.
`SetColorGrading` is the look applied after tonemapping, in the three tonal ranges a colorist works
in:

```csharp
Render.SetExposure(camera, 12f);

// Or the same thing as a lens, the way a real camera is written down, which the depth of field
// reads as well, so the aperture that brightens the picture is the one that blurs it.
var lens = new PhysicalLens(Aperture: 2.8f, Shutter: 1f / 250f, Sensitivity: 400f);
Render.SetLens(camera, lens);
Render.SetEffects(camera, new EffectSettings { DepthOfField = DepthOfFieldMode.Bokeh, FocalDistance = 6f }.Through(lens));

Render.SetColorGrading(camera, new GradingSettings
{
    Temperature = -0.15f,                                   // cooler overall
    Shadows = new GradingSection { Lift = 0.02f },          // lifted blacks
    Highlights = new GradingSection { Saturation = 0.9f },  // calmer highlights
});
```

`Render.SetSortedTransparency` sorts transparent fragments rather than whole objects. Ordinary
alpha blending sorts by distance between objects, so two panes of glass crossing each other, or one
mesh whose own faces overlap, come out right from some angles and wrong from others, and no
reordering of the scene fixes both. It costs a buffer the size of the screen times the layer count,
which is why it is per camera and off unless asked for.

`MidtonesRange` says which luminances count as the middle, so it decides how much of the picture
each of the three sections has to work on. The terms inside a section are the standard ASC CDL ones,
so a grade written for a film pipeline carries across unchanged. Passing `null` puts the camera back
to the engine's own grading.

### The lens

The lens is a second call, because it is decided at a different time. A settings screen owns the
pipeline above, and a scene sets these for a moment.

```csharp
Render.SetEffects(camera, new EffectSettings
{
    DepthOfField = DepthOfFieldMode.Bokeh,   // focus, and a disc around every highlight past it
    FocalDistance = 8f,
    Aperture = 1.4f,
    ShutterAngle = 0.5f,                     // a film camera's 180 degree shutter
    Aberration = 0.02f,                      // colored fringes on the edges
    Distortion = 0.3f,                       // a wide lens bulging the picture outwards
    Vignette = 0.4f,                         // corners going dark
    AutoExposure = true,                     // the camera metering the frame for itself
});
```

The whole lens in one call, so an effect the settings leave off is taken off the camera. Depth of
field needs a perspective camera, since focus has no meaning without one, and `Aperture` is in
f-stops, so a smaller number is a wider lens and less of the scene in focus. Motion blur reads where
each pixel moved, which costs a second pass over the scene, and that pass goes away again when the
shutter angle does. `AberrationColors` swaps the red, green, blue fringe for any image, read across
its width. Auto exposure builds a histogram of the frame and moves the exposure so the average lands
on middle gray, as an eye adjusts walking out of a cave; `MeteringMask` weights where in the frame
it looks, and `ExposureCompensation` bends the result so a night scene can stay dark.

---

Before this, [Compute](compute.md).
Next, [Reflections and the sky](sky-and-reflections.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#3d-rendering). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#drawing). The [guide's contents](../README.md#guide) list every page.
