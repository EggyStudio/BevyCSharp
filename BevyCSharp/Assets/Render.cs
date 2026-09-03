using Bevy.Interop;

namespace Bevy;

/// <summary>The mesh primitives the engine can build without an asset file.</summary>
public static class MeshShape
{
    /// <summary>A box, sized by width, height and depth.</summary>
    public const string Cuboid = "Cuboid";

    /// <summary>A sphere, sized by radius.</summary>
    public const string Sphere = "Sphere";

    /// <summary>A flat plane on the XZ axes, sized by width and depth.</summary>
    public const string Plane = "Plane";

    /// <summary>A capsule, sized by radius and length.</summary>
    public const string Capsule = "Capsule";
}

/// <summary>Which of Bevy's lights to spawn.</summary>
public enum LightKind
{
    /// <summary>Parallel rays, as from the sun. Intensity is illuminance in lux.</summary>
    Directional = 0,

    /// <summary>Rays from a point. Intensity is luminous power in lumens.</summary>
    Point = 1,

    /// <summary>
    /// A cone of rays, aimed down the entity's negative Z. Intensity is luminous power in lumens.
    /// </summary>
    Spot = 2,
}

/// <summary>What a material does where it is not fully opaque.</summary>
public enum AlphaMode
{
    /// <summary>Ignore alpha entirely. The cheapest, and the right default.</summary>
    Opaque = 0,

    /// <summary>
    /// Draw a pixel or skip it, deciding at <see cref="MaterialSettings.AlphaCutoff"/>.
    /// </summary>
    /// <remarks>
    /// What foliage and chain-link fences want: it keeps the depth buffer honest, so nothing has
    /// to be sorted, at the cost of a hard edge.
    /// </remarks>
    Mask = 1,

    /// <summary>Blend with what is behind.</summary>
    /// <remarks>
    /// Real transparency, and the expensive one: blended surfaces are drawn after everything
    /// else and sorted back to front, so two of them overlapping can still be drawn in the wrong
    /// order.
    /// </remarks>
    Blend = 2,

    /// <summary>Add to what is behind, which never darkens it. For fire, glows and holograms.</summary>
    Add = 3,
}

/// <summary>
/// Everything a physically based material is made of.
/// </summary>
/// <remarks>
/// <para>
/// Every value has a usable default, so setting one property and leaving the rest is the normal
/// way to use this.
/// </para>
/// <para>
/// A texture is an image handle from <see cref="AssetServer.Load"/>, and is combined with the
/// matching factor rather than replacing it: a base colour map on a white base colour shows the
/// map unchanged, and tinting it is a matter of setting a colour. The image need not have
/// finished loading, because the material holds a handle rather than pixels.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var crate = Render.CreateMaterial(new MaterialSettings
/// {
///     BaseColorTexture = AssetServer.Load(AssetKind.Image, "textures/crate.png"),
///     Roughness = 0.8f,
/// });
/// </code>
/// </example>
public sealed class MaterialSettings
{
    /// <summary>Base colour, linear RGBA. White by default, so a texture shows unchanged.</summary>
    public (float R, float G, float B, float A) BaseColor { get; set; } = (1f, 1f, 1f, 1f);

    /// <summary>Zero for a dielectric, one for a metal. Values between are rarely physical.</summary>
    public float Metallic { get; set; }

    /// <summary>Near zero for a mirror, one for a matte surface.</summary>
    public float Roughness { get; set; } = 0.5f;

    /// <summary>Light the surface gives off, which no lamp affects. Black by default.</summary>
    public (float R, float G, float B, float A) Emissive { get; set; } = (0f, 0f, 0f, 1f);

    /// <summary>What to do where the material is not fully opaque.</summary>
    public AlphaMode AlphaMode { get; set; } = AlphaMode.Opaque;

    /// <summary>Where a masked material stops drawing.</summary>
    public float AlphaCutoff { get; set; } = 0.5f;

    /// <summary>
    /// Draw back faces as well as front ones.
    /// </summary>
    /// <remarks>
    /// For anything modelled as a single sheet: a leaf, a flag, a curtain. It doubles the work
    /// for that surface, and lighting on the back face uses the front face's normal.
    /// </remarks>
    public bool DoubleSided { get; set; }

    /// <summary>Show the base colour flat, with no lighting at all.</summary>
    /// <remarks>For a skybox, a UI panel in the world, or anything meant to read as its own colour.</remarks>
    public bool Unlit { get; set; }

    /// <summary>The base colour map, which is the texture people mean by "the texture".</summary>
    public AssetHandle BaseColorTexture { get; set; } = AssetHandle.None;

    /// <summary>
    /// A tangent-space normal map, which fakes detail the geometry does not have.
    /// </summary>
    /// <remarks>Must not be loaded as sRGB; a normal map holds directions rather than colours.</remarks>
    public AssetHandle NormalMap { get; set; } = AssetHandle.None;

    /// <summary>
    /// Metallic in the blue channel and roughness in the green, as glTF packs them.
    /// </summary>
    public AssetHandle MetallicRoughnessTexture { get; set; } = AssetHandle.None;

    /// <summary>Where the surface glows, multiplied by <see cref="Emissive"/>.</summary>
    public AssetHandle EmissiveTexture { get; set; } = AssetHandle.None;

    /// <summary>Where ambient light fails to reach, as a single channel.</summary>
    public AssetHandle OcclusionTexture { get; set; } = AssetHandle.None;

    /// <summary>
    /// How many times the texture repeats across the surface.
    /// </summary>
    /// <remarks>
    /// The other half of tiling. A mesh's UVs run from zero to one however large it is, so a
    /// floor drawn with a repeating texture still shows one stretched copy until this is raised.
    /// The texture must also have been loaded with <see cref="TextureWrap.Repeat"/>, or the
    /// values past one are clamped to the edge pixel.
    /// </remarks>
    public (float U, float V) UvScale { get; set; } = (1f, 1f);

    /// <summary>Radians the texture is turned by.</summary>
    public float UvRotation { get; set; }

    /// <summary>How far the texture is shifted, in UV units.</summary>
    public (float U, float V) UvOffset { get; set; }
}

/// <summary>How a camera turns the world into a picture.</summary>
public enum CameraProjection
{
    /// <summary>Things shrink with distance, as an eye sees them.</summary>
    Perspective = 0,

    /// <summary>
    /// Parallel lines stay parallel and distance does not shrink anything, which is what an
    /// isometric or a top-down view is built on.
    /// </summary>
    Orthographic = 1,
}

/// <summary>What a camera does with the pixels it is about to draw over.</summary>
public enum ClearMode
{
    /// <summary>Clear to the world's clear colour.</summary>
    World = 0,

    /// <summary>Clear to this camera's own colour.</summary>
    Custom = 1,

    /// <summary>
    /// Clear nothing and draw over what is already there, for a camera layered on another.
    /// </summary>
    Keep = 2,
}

/// <summary>
/// How a camera should see.
/// </summary>
/// <remarks>
/// Every value has a usable default, so setting one property and leaving the rest is the normal
/// way to use this. Position and aim the camera by writing its <see cref="Transform"/>.
/// </remarks>
/// <example>
/// <code>
/// var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 60f });
/// ctx.Ecs.Add(camera, Transform.LookingAt(eye, Vec3.Zero, Vec3.UnitY));
/// </code>
/// </example>
/// <summary>
/// The curve that maps what was rendered onto what a screen can show.
/// </summary>
/// <remarks>
/// A renderer works in light, which has no upper bound; a display has one. A tonemapper decides
/// what happens to the parts brighter than the screen can be, and the choice is a look rather
/// than a correctness question. It shows most on a camera drawing in high dynamic range, which is
/// <see cref="PostSettings.Hdr"/>.
/// </remarks>
public enum Tonemapper
{
    /// <summary>Clip anything brighter than white, which is what no tonemapping means.</summary>
    None = 0,

    /// <summary>The classic curve. Colours shift hue as they brighten.</summary>
    Reinhard = 1,

    /// <summary>The same on luminance only, so bright colours keep their hue better.</summary>
    ReinhardLuminance = 2,

    /// <summary>Film-like and high contrast, with deliberate hue shifts. Dramatic.</summary>
    AcesFitted = 3,

    /// <summary>Neutral and slightly desaturated, with almost no hue shift.</summary>
    AgX = 4,

    /// <summary>A plain transform, useful as a reference to judge the others against.</summary>
    SomewhatBoring = 5,

    /// <summary>Bevy's own: neutral, and keeps saturation in the highlights.</summary>
    TonyMcMapface = 6,

    /// <summary>Blender's filmic curve, for matching a render done there.</summary>
    BlenderFilmic = 7,
}

/// <summary>
/// The antialiasing that runs as a pass over the finished picture.
/// </summary>
/// <remarks>
/// Separate from <see cref="PostSettings.Msaa"/>, which works while the scene is rasterised and
/// smooths the edges of geometry only. A pass sees the picture instead, so it also catches edges
/// that come from a texture or a shader, at the cost of some sharpness.
/// </remarks>
public enum AntiAliasPass
{
    /// <summary>No pass. Multisampling alone, or nothing at all.</summary>
    None = 0,

    /// <summary>Cheap and slightly soft. What a game reaches for first.</summary>
    Fxaa = 1,

    /// <summary>Costlier and sharper, and better on near-horizontal edges.</summary>
    Smaa = 2,

    /// <summary>
    /// Resolved from the frames before it, which catches what the other two cannot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every frame is drawn from a slightly different point and averaged with its predecessors,
    /// so an edge is sampled many times over rather than guessed at from one. That catches the
    /// aliasing a texture or a specular highlight produces, which a pass looking at a single
    /// finished frame has no way to tell from detail.
    /// </para>
    /// <para>
    /// The cost is a trail behind anything whose motion the renderer reports wrongly, and a
    /// picture that is softer than the other two. It needs a 3D camera and
    /// <see cref="PostSettings.Msaa"/> set to 1, since there is no history to resolve from a
    /// multisampled target.
    /// </para>
    /// </remarks>
    Temporal = 3,
}

/// <summary>How hard an antialiasing pass looks for an edge.</summary>
public enum AntiAliasQuality
{
    /// <summary>Fastest, and misses edges.</summary>
    Low = 0,

    /// <summary>The usual compromise.</summary>
    Medium = 1,

    /// <summary>Catches more, costs more.</summary>
    High = 2,

    /// <summary>As much as the pass can do.</summary>
    Ultra = 3,
}

/// <summary>How bloom is mixed back into the picture.</summary>
public enum BloomMode
{
    /// <summary>
    /// The scattered light is taken out of the source, so the picture keeps its brightness.
    /// </summary>
    EnergyConserving = 0,

    /// <summary>The scattered light is added on top, which is brighter and more obvious.</summary>
    Additive = 1,
}

/// <summary>
/// What a camera does to the picture after the scene has been drawn.
/// </summary>
/// <remarks>
/// One settings object for the whole pipeline rather than a call per effect, because these are
/// decided together: bloom wants a high dynamic range target, and multisampling and an
/// antialiasing pass are two answers to the same question. Every field is applied on every call,
/// so an effect this object leaves off is taken off the camera. Turning bloom off is the same
/// call as turning it on, which is what a settings screen wants.
/// </remarks>
public sealed class PostSettings
{
    /// <summary>Which curve maps the rendered range onto the display.</summary>
    public Tonemapper Tonemapper { get; set; } = Tonemapper.TonyMcMapface;

    /// <summary>
    /// Dither before quantising to the display's bit depth.
    /// </summary>
    /// <remarks>
    /// Hides the banding a smooth gradient shows otherwise, at the cost of a little noise. Bevy
    /// leaves this on, and so does this.
    /// </remarks>
    public bool Dither { get; set; } = true;

    /// <summary>
    /// Draw into a high dynamic range target.
    /// </summary>
    /// <remarks>
    /// What lets a highlight be brighter than white instead of clipping there, and what bloom
    /// reads to decide where to scatter. Costs memory and bandwidth, so it is off unless asked
    /// for.
    /// </remarks>
    public bool Hdr { get; set; }

    /// <summary>
    /// Samples per pixel taken while the scene is rasterised: 1, 2, 4 or 8.
    /// </summary>
    /// <remarks>
    /// Smooths the edges of geometry and nothing else. Four is Bevy's own; one turns it off,
    /// which is what a game leaning on <see cref="AntiAlias"/> does, and what
    /// <see cref="AntiAliasPass.Temporal"/> requires.
    /// </remarks>
    public int Msaa { get; set; } = 4;

    /// <summary>An antialiasing pass over the finished picture.</summary>
    public AntiAliasPass AntiAlias { get; set; } = AntiAliasPass.None;

    /// <summary>
    /// How hard that pass looks for an edge.
    /// </summary>
    /// <remarks>
    /// Read by <see cref="AntiAliasPass.Fxaa"/> and <see cref="AntiAliasPass.Smaa"/>. Temporal
    /// antialiasing has no such setting: how much it catches is decided by how many frames it
    /// has to work from.
    /// </remarks>
    public AntiAliasQuality Quality { get; set; } = AntiAliasQuality.Medium;

    /// <summary>
    /// Contrast adaptive sharpening, from 0 for none to 1 for as much as it does.
    /// </summary>
    /// <remarks>Puts back some of the crispness an antialiasing pass takes away.</remarks>
    public float Sharpen { get; set; }

    /// <summary>
    /// Scatter light out of the brightest parts of the picture.
    /// </summary>
    /// <remarks>
    /// Needs <see cref="Hdr"/> to have anything to work with: without it nothing is brighter than
    /// white, so nothing is bright enough to glow. To make one object glow harder, raise its
    /// material's emissive colour rather than this.
    /// </remarks>
    public bool Bloom { get; set; }

    /// <summary>How much light is scattered.</summary>
    public float BloomIntensity { get; set; } = 0.15f;

    /// <summary>Brightness a pixel has to reach before it blooms at all.</summary>
    /// <remarks>Zero blooms everything a little, which is the physically-minded choice.</remarks>
    public float BloomThreshold { get; set; }

    /// <summary>How gradually that threshold takes effect.</summary>
    public float BloomThresholdSoftness { get; set; }

    /// <summary>How the scattered light is mixed back in.</summary>
    public BloomMode BloomMode { get; set; } = BloomMode.EnergyConserving;

    /// <summary>High dynamic range with a gentle bloom over it.</summary>
    public static PostSettings Glow => new() { Hdr = true, Bloom = true };
}

/// <summary>How the parts of a picture that are out of focus are blurred.</summary>
public enum DepthOfFieldMode
{
    /// <summary>Everything is drawn sharp, whatever its distance.</summary>
    None = 0,

    /// <summary>A plain blur, which is cheaper and reads as softness.</summary>
    Gaussian = 1,

    /// <summary>
    /// Each point of light spreads into a disc, which is what a lens does and what makes a
    /// highlight behind the subject into a circle.
    /// </summary>
    Bokeh = 2,
}

/// <summary>
/// The lens a camera draws through.
/// </summary>
/// <remarks>
/// <para>
/// Beside <see cref="PostSettings"/> rather than part of it, because the two are decided at
/// different times: the pipeline is what a settings screen owns, and these are what a scene does
/// for a moment, a hit, a dream, a shot pulling focus. The rule is the same, so every field is
/// applied on every call and an effect these settings leave off is taken off the camera.
/// </para>
/// <para>
/// Depth of field needs a perspective camera, since focus has no meaning without one. Auto
/// exposure needs a high dynamic range target, and gives itself one.
/// </para>
/// </remarks>
public sealed class EffectSettings
{
    /// <summary>How the depths that are out of focus are blurred.</summary>
    public DepthOfFieldMode DepthOfField { get; set; } = DepthOfFieldMode.None;

    /// <summary>Distance in metres to what is in focus.</summary>
    public float FocalDistance { get; set; } = 10f;

    /// <summary>
    /// Aperture in f-stops.
    /// </summary>
    /// <remarks>
    /// Smaller opens the lens wider, which leaves less of the scene in focus. Bevy's own is 1,
    /// which is wide, so a scene that should be mostly sharp wants a larger number.
    /// </remarks>
    public float Aperture { get; set; } = 1f;

    /// <summary>
    /// Height of the imaginary sensor, in metres.
    /// </summary>
    /// <remarks>
    /// With the camera's field of view this fixes the focal length, so it is the other half of
    /// how strong the blur is. Zero takes Bevy's own, the Super 35 cinema format.
    /// </remarks>
    public float SensorHeight { get; set; }

    /// <summary>
    /// Widest a single blur may be, in pixels. Zero takes Bevy's own.
    /// </summary>
    /// <remarks>Not physical: a cap on how slow a very out-of-focus frame is allowed to be.</remarks>
    public float MaxBlurDiameter { get; set; }

    /// <summary>
    /// Distance past which nothing is blurred any further, in metres.
    /// </summary>
    /// <remarks>
    /// The renderer puts a sky infinitely far away, which would blur it as hard as
    /// <see cref="MaxBlurDiameter"/> allows. Zero leaves it unbounded.
    /// </remarks>
    public float MaxDepth { get; set; }

    /// <summary>
    /// Fraction of a frame the shutter is open, and so how far a moving thing smears.
    /// </summary>
    /// <remarks>
    /// Zero is no motion blur. A film camera's 180 degree shutter is 0.5, which is what a
    /// cinematic look wants at 24 frames a second; at 60 the same look is about 1.25. Above 1 a
    /// thing smears further than it moved, which is a choice rather than a mistake.
    /// </remarks>
    public float ShutterAngle { get; set; }

    /// <summary>
    /// Samples taken either side of a pixel along its motion.
    /// </summary>
    /// <remarks>
    /// Bevy takes one each way and one in the middle at 1, three each way at 3. Zero also turns
    /// motion blur off, whatever the shutter angle says.
    /// </remarks>
    public uint MotionBlurSamples { get; set; } = 1;

    /// <summary>
    /// Width of the coloured fringe around edges, as a fraction of the window. Zero for none.
    /// </summary>
    /// <remarks>
    /// What a lens does when it fails to focus every colour at one point. Bevy's own strength is
    /// 0.02, and a horror game reaching for it on a hit wants more.
    /// </remarks>
    public float Aberration { get; set; }

    /// <summary>Cap on the samples the fringe is built from. Zero takes Bevy's own.</summary>
    public uint AberrationSamples { get; set; }

    /// <summary>
    /// An image the fringe takes its colours from, read across its width.
    /// </summary>
    /// <remarks>
    /// Nothing here gives the usual red, green, blue. The image is sampled down its vertical
    /// centre, so it should be one pixel tall.
    /// </remarks>
    public AssetHandle AberrationColors { get; set; } = AssetHandle.None;

    /// <summary>
    /// Strength of the lens warp. Zero leaves straight lines straight.
    /// </summary>
    /// <remarks>
    /// Positive bulges the picture outwards, which is what a wide lens does; negative pinches it
    /// inwards. Bevy's own strength is 0.5.
    /// </remarks>
    public float Distortion { get; set; }

    /// <summary>
    /// Zoom applied after warping, to crop the edges a strong warp leaves uncovered.
    /// </summary>
    public float DistortionScale { get; set; } = 1f;

    /// <summary>
    /// How much of the warp lands on each axis, for a lens that is not round.
    /// </summary>
    public (float X, float Y) DistortionAxes { get; set; } = (1f, 1f);

    /// <summary>Point the warp radiates from, in fractions of the window.</summary>
    public (float X, float Y) DistortionCenter { get; set; } = (0.5f, 0.5f);

    /// <summary>
    /// How sharply the warp bends at the edges of the picture.
    /// </summary>
    /// <remarks>Zero is the plain look, and what Bevy recommends for most scenes.</remarks>
    public float DistortionEdgeCurvature { get; set; }

    /// <summary>
    /// How dark the corners go, from 0 for no vignette to 1 for black.
    /// </summary>
    /// <remarks>
    /// What a lens does at the edges of its coverage, and what a game uses to pull the eye
    /// towards the middle or to show that the player is hurt.
    /// </remarks>
    public float Vignette { get; set; }

    /// <summary>How much of the picture is left untouched, as a fraction of the window.</summary>
    public float VignetteRadius { get; set; } = 0.75f;

    /// <summary>Width of the edge between the clear centre and the dark corners.</summary>
    public float VignetteSmoothness { get; set; } = 5f;

    /// <summary>Shape of that edge, where 1 is a circle.</summary>
    public float VignetteRoundness { get; set; } = 1f;

    /// <summary>Point the vignette is centred on, in fractions of the window.</summary>
    public (float X, float Y) VignetteCenter { get; set; } = (0.5f, 0.5f);

    /// <summary>
    /// How far the vignette is stretched to fit a window that is not square, 0 not at all and 1
    /// exactly.
    /// </summary>
    public float VignetteEdgeCompensation { get; set; } = 1f;

    /// <summary>The colour the corners are taken towards, linear. Black is the usual one.</summary>
    public (float R, float G, float B, float A) VignetteColor { get; set; } = (0f, 0f, 0f, 1f);

    /// <summary>
    /// Let the camera find its own exposure from what it can see.
    /// </summary>
    /// <remarks>
    /// A histogram of the frame's brightness is built and the exposure moved so that the average
    /// lands on middle grey, which is what an eye does walking out of a cave. The camera is given
    /// a high dynamic range target, because there is nothing to meter without one.
    /// </remarks>
    public bool AutoExposure { get; set; }

    /// <summary>Darkest and brightest luminance the metering counts, in EV-100.</summary>
    /// <remarks>Anything below is ignored and anything above counts as the brightest.</remarks>
    public (float Min, float Max) MeteringRange { get; set; } = (-8f, 8f);

    /// <summary>
    /// The part of the histogram that is averaged, as fractions from darkest to brightest.
    /// </summary>
    /// <remarks>
    /// Bevy's own throws away the darkest tenth and the brightest tenth, so a shadow in the
    /// corner and a lamp in the frame do not decide the exposure between them.
    /// </remarks>
    public (float Low, float High) MeteringFilter { get; set; } = (0.10f, 0.90f);

    /// <summary>How fast the exposure opens as a scene darkens, in f-stops per second.</summary>
    public float SpeedBrighten { get; set; } = 3f;

    /// <summary>How fast it closes as a scene brightens, in f-stops per second.</summary>
    public float SpeedDarken { get; set; } = 1f;

    /// <summary>
    /// How near the target the adaptation stops being linear, in f-stops. Zero takes Bevy's own.
    /// </summary>
    /// <remarks>
    /// Inside this distance the exposure eases in rather than tracking straight, which is what
    /// stops it jittering while the scene changes slightly from frame to frame.
    /// </remarks>
    public float ExposureTransition { get; set; }

    /// <summary>
    /// An image weighting where in the frame the metering looks.
    /// </summary>
    /// <remarks>
    /// Only the red channel is read, and it is stretched over the whole frame: black ignores a
    /// pixel, white counts it fully. Nothing here weights the whole frame alike.
    /// </remarks>
    public AssetHandle MeteringMask { get; set; } = AssetHandle.None;

    /// <summary>
    /// A curve applied to the exposure the metering arrived at.
    /// </summary>
    /// <remarks>
    /// Each point pairs a measured luminance in EV-100 with the compensation to apply there in
    /// f-stops, so a night scene can be left darker than middle grey and a desert left brighter.
    /// The points have to rise in luminance, and at most eight of them cross the boundary. Fewer
    /// than two is no compensation.
    /// </remarks>
    public IReadOnlyList<(float Luminance, float Compensation)>? ExposureCompensation { get; set; }
}

/// <summary>
/// A sky computed from sunlight scattering through the air.
/// </summary>
/// <remarks>
/// <para>
/// Not a picture of a sky but a simulation of one: the colour of every direction is worked out
/// from how far light travels through the air to reach it, so the horizon reddens, the zenith
/// stays blue, and the whole thing turns over as the sun moves. Distant geometry picks up the
/// same haze.
/// </para>
/// <para>
/// The sun is whichever directional light is in the scene, so its direction and colour are what
/// move the sky. A scene with no directional light gets a night sky.
/// </para>
/// </remarks>
public sealed class AtmosphereSettings
{
    /// <summary>
    /// How thick the air is, as a multiple of earth's.
    /// </summary>
    /// <remarks>
    /// Above one for a hazier world, below one for a thinner and darker sky. One is earth.
    /// </remarks>
    public float Density { get; set; } = 1f;

    /// <summary>
    /// How large the planet is against the scene, for a world not measured in metres.
    /// </summary>
    /// <remarks>
    /// The planet is the size of a real one and its ground sits at the origin, so a scene in
    /// metres needs nothing here. A scene in kilometres wants a smaller number, since what
    /// matters is how far the camera moves through the air.
    /// </remarks>
    public float Scale { get; set; } = 1f;

    /// <summary>
    /// How far in front of the camera the haze is computed, in metres.
    /// </summary>
    /// <remarks>
    /// What decides where distant geometry fades into the sky. Zero leaves Bevy's own distance,
    /// which suits a scene measured in metres.
    /// </remarks>
    public float HazeDistance { get; set; }
}

public sealed class CameraSettings
{
    /// <summary>Perspective or orthographic.</summary>
    public CameraProjection Projection { get; set; } = CameraProjection.Perspective;

    /// <summary>Vertical field of view in degrees. Perspective only.</summary>
    /// <remarks>
    /// Bevy's default is 45. Larger sees more and exaggerates depth; much larger distorts at the
    /// edges of the picture.
    /// </remarks>
    public float FieldOfView { get; set; } = 45f;

    /// <summary>How many world units fit vertically. Orthographic only.</summary>
    /// <remarks>The width follows from the window, so the picture does not stretch when resized.</remarks>
    public float Height { get; set; } = 10f;

    /// <summary>Nearest visible distance.</summary>
    /// <remarks>
    /// Depth precision is spent between here and <see cref="Far"/>, and mostly near this end, so
    /// a very small value is what makes distant surfaces flicker against each other.
    /// </remarks>
    public float Near { get; set; } = 0.1f;

    /// <summary>Furthest visible distance. Ignored by an orthographic camera.</summary>
    public float Far { get; set; } = 1000f;

    /// <summary>What to do with the pixels already there.</summary>
    public ClearMode Clear { get; set; } = ClearMode.World;

    /// <summary>The colour used when <see cref="Clear"/> is <see cref="ClearMode.Custom"/>.</summary>
    /// <remarks>Linear RGBA, not sRGB, so these are the numbers a shader works in.</remarks>
    public (float R, float G, float B, float A) ClearColor { get; set; } = (0f, 0f, 0f, 1f);

    /// <summary>Draw order. A camera with a higher order draws over one with a lower.</summary>
    public int Order { get; set; }

    /// <summary>
    /// The part of the window to draw into, in physical pixels, or null for all of it.
    /// </summary>
    /// <remarks>
    /// What splitscreen is made of: two cameras, each given half the window. Physical pixels
    /// rather than logical ones, because that is what a framebuffer is divided into, so half a
    /// window is half its physical width whatever the display scaling.
    /// </remarks>
    public (uint X, uint Y, uint Width, uint Height)? Viewport { get; set; }

    /// <summary>
    /// Which render layers this camera sees, as a bit per layer. Zero means the default layer.
    /// </summary>
    /// <remarks>
    /// A camera draws an entity only where their layers overlap, which is how a minimap shows
    /// different things from the main view. Put entities on layers with
    /// <see cref="Render.SetLayers"/>.
    /// </remarks>
    public uint Layers { get; set; }
}

/// <summary>
/// What kind of light to spawn and how it behaves.
/// </summary>
/// <remarks>
/// A directional light is aimed by its <see cref="Transform"/> and ignores position; a point
/// light is positioned and ignores aim; a spot light uses both.
/// </remarks>
public sealed class LightSettings
{
    /// <summary>Which light to spawn.</summary>
    public LightKind Kind { get; set; } = LightKind.Directional;

    /// <summary>Illuminance in lux for a directional light, luminous power in lumens otherwise.</summary>
    public float Intensity { get; set; } = 10_000f;

    /// <summary>Linear RGB. White by default.</summary>
    public (float R, float G, float B) Color { get; set; } = (1f, 1f, 1f);

    /// <summary>How far the light reaches, in world units. Point and spot only.</summary>
    public float Range { get; set; } = 20f;

    /// <summary>
    /// Radius of the emitting sphere. Point and spot only.
    /// </summary>
    /// <remarks>
    /// A light with no size casts a shadow with a hard edge, which reads as artificial. Giving it
    /// a radius softens the edge.
    /// </remarks>
    public float Radius { get; set; }

    /// <summary>Whether the light casts shadows.</summary>
    /// <remarks>Shadows cost a render pass per light, so this is the first thing to turn off.</remarks>
    public bool Shadows { get; set; } = true;

    /// <summary>Radians from the axis within which a spot light is at full brightness.</summary>
    public float InnerAngle { get; set; }

    /// <summary>Radians from the axis at which a spot light has fallen to nothing.</summary>
    /// <remarks>Must be under a quarter turn, and at least <see cref="InnerAngle"/>.</remarks>
    public float OuterAngle { get; set; } = MathF.PI / 8f;

    /// <summary>
    /// How far a surface is pushed away before it is tested against this light's shadow map.
    /// </summary>
    /// <remarks>
    /// The fix for shadow acne, the stippled self-shadowing a surface shows when the shadow map
    /// is too coarse to tell it apart from itself. Raising it trades that for a shadow that
    /// starts slightly away from what casts it. Bevy's default is 0.02.
    /// </remarks>
    public float ShadowDepthBias { get; set; } = 0.02f;

    /// <summary>
    /// The same, measured along the surface normal.
    /// </summary>
    /// <remarks>
    /// Handles the case depth bias alone does not: a surface lit at a glancing angle, where a
    /// small depth error covers a long distance. Bevy's default is 0.6 for a directional light
    /// and 0.6 for the others.
    /// </remarks>
    public float ShadowNormalBias { get; set; } = 0.6f;
}

/// <summary>
/// Builds renderable assets and attaches them to entities.
/// </summary>
/// <remarks>
/// <para>
/// Everything here needs a native build with the renderer compiled in. On a headless build the
/// calls report that rather than failing obscurely, so the same behavior code runs either way and
/// draws nothing.
/// </para>
/// <para>
/// Meshes and materials are Rust values that have to be constructed rather than described by a
/// layout, and the components carrying them hold a typed handle that raw bytes cannot represent.
/// That is why these are named operations rather than a component written through
/// <see cref="EcsWorld.Add{T}"/>, the way <see cref="Transform"/> is.
/// </para>
/// </remarks>
public static unsafe class Render
{
    /// <summary>
    /// Builds a mesh primitive and returns a handle to it.
    /// </summary>
    /// <param name="shape">One of the constants on <see cref="MeshShape"/>.</param>
    /// <param name="a">Width for a cuboid or plane, radius for a sphere or capsule.</param>
    /// <param name="b">Height for a cuboid, depth for a plane, length for a capsule.</param>
    /// <param name="c">Depth, for a cuboid.</param>
    public static AssetHandle CreateMesh(string shape, float a = 1f, float b = 1f, float c = 1f)
    {
        ArgumentException.ThrowIfNullOrEmpty(shape);

        var key = Native.bcs_mesh_create(shape, a, b, c);
        if (key == NativeStatus.Unsupported) throw NoRenderer("Building a mesh");
        if (key == NativeStatus.NoComponent)
            throw new BevyNativeException(
                NativeStatus.NoComponent,
                $"'{shape}' is not a mesh primitive the engine can build. Use one of the "
                + "constants on MeshShape.");

        Native.Check(key, $"building a {shape} mesh");
        return new AssetHandle(key);
    }

    /// <summary>
    /// Builds a physically based material and returns a handle to it.
    /// </summary>
    /// <param name="red">Linear sRGB red, from zero to one.</param>
    /// <param name="green">Linear sRGB green, from zero to one.</param>
    /// <param name="blue">Linear sRGB blue, from zero to one.</param>
    /// <param name="alpha">Opacity, from zero to one.</param>
    /// <param name="metallic">Zero for a dielectric, one for a metal.</param>
    /// <param name="roughness">Near zero for a mirror, one for a matte surface.</param>
    public static AssetHandle CreateMaterial(
        float red,
        float green,
        float blue,
        float alpha = 1f,
        float metallic = 0f,
        float roughness = 0.5f) =>
        CreateMaterial(new MaterialSettings
        {
            BaseColor = (red, green, blue, alpha),
            Metallic = metallic,
            Roughness = roughness,
        });

    /// <summary>Builds a material from <paramref name="settings"/> and returns a handle to it.</summary>
    /// <remarks>
    /// A texture the settings leave at <see cref="AssetHandle.None"/> is one the material does
    /// without. A handle that names nothing, which is what a released one becomes, is refused
    /// instead: drawing the surface untextured and reporting success would leave the caller with
    /// a wrong picture and nothing pointing at why.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// A texture handle names nothing, or this build has no renderer.
    /// </exception>
    public static AssetHandle CreateMaterial(MaterialSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeMaterialConfig
        {
            BaseR = settings.BaseColor.R,
            BaseG = settings.BaseColor.G,
            BaseB = settings.BaseColor.B,
            BaseA = settings.BaseColor.A,
            Metallic = settings.Metallic,
            Roughness = settings.Roughness,
            EmissiveR = settings.Emissive.R,
            EmissiveG = settings.Emissive.G,
            EmissiveB = settings.Emissive.B,
            EmissiveA = settings.Emissive.A,
            AlphaMode = (int)settings.AlphaMode,
            AlphaCutoff = settings.AlphaCutoff,
            DoubleSided = settings.DoubleSided ? 1 : 0,
            Unlit = settings.Unlit ? 1 : 0,
            BaseColorTexture = Key(settings.BaseColorTexture),
            NormalMap = Key(settings.NormalMap),
            MetallicRoughnessTexture = Key(settings.MetallicRoughnessTexture),
            EmissiveTexture = Key(settings.EmissiveTexture),
            OcclusionTexture = Key(settings.OcclusionTexture),
            UvScaleX = settings.UvScale.U,
            UvScaleY = settings.UvScale.V,
            UvRotation = settings.UvRotation,
            UvOffsetX = settings.UvOffset.U,
            UvOffsetY = settings.UvOffset.V,
        };

        var key = Native.bcs_material_create(&native);
        if (key == NativeStatus.Unsupported) throw NoRenderer("Building a material");

        Native.Check(key, "building a material");
        return new AssetHandle(key);

        // An unset handle is -1, which is what the bridge reads as "no texture here".
        static int Key(AssetHandle handle) => handle.IsValid ? handle.Key : -1;
    }

    /// <summary>
    /// Gives an entity a mesh to draw.
    /// </summary>
    /// <remarks>
    /// Inserting this also pulls in the components Bevy requires alongside it, such as
    /// <see cref="Transform"/> and visibility, so an entity needs nothing else to be drawable
    /// beyond a material.
    /// </remarks>
    public static void SetMesh(EcsWorld world, Entity entity, AssetHandle mesh) =>
        Attach(world, entity, "Mesh3d", mesh, "a mesh");

    /// <summary>Gives an entity a material to draw its mesh with.</summary>
    public static void SetMaterial(EcsWorld world, Entity entity, AssetHandle material) =>
        Attach(world, entity, "MeshMaterial3d", material, "a material");

    /// <summary>
    /// Spawns a 3D camera and returns it.
    /// </summary>
    /// <remarks>
    /// Bevy draws nothing without a camera. The new one sits at the origin looking down negative
    /// Z; position it by writing its <see cref="Transform"/> like any other entity.
    /// </remarks>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnCamera3d() => new(Native.bcs_render_spawn_camera_3d(null));

    /// <summary>Spawns a 3D camera set up by <paramref name="settings"/>.</summary>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnCamera3d(CameraSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeCameraConfig
        {
            Projection = (int)settings.Projection,
            FovDegrees = settings.FieldOfView,
            OrthoHeight = settings.Height,
            Near = settings.Near,
            Far = settings.Far,
            ClearMode = (int)settings.Clear,
            ClearR = settings.ClearColor.R,
            ClearG = settings.ClearColor.G,
            ClearB = settings.ClearColor.B,
            ClearA = settings.ClearColor.A,
            Order = settings.Order,
            HasViewport = settings.Viewport is null ? 0 : 1,
            ViewportX = settings.Viewport?.X ?? 0,
            ViewportY = settings.Viewport?.Y ?? 0,
            ViewportWidth = settings.Viewport?.Width ?? 0,
            ViewportHeight = settings.Viewport?.Height ?? 0,
            Layers = settings.Layers,
        };

        return new Entity(Native.bcs_render_spawn_camera_3d(&native));
    }

    /// <summary>
    /// Spawns a light and returns it.
    /// </summary>
    /// <param name="kind">Which light to spawn.</param>
    /// <param name="intensity">
    /// Illuminance in lux for a directional light, luminous power in lumens for a point light.
    /// </param>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnLight(LightKind kind, float intensity) =>
        SpawnLight(new LightSettings { Kind = kind, Intensity = intensity });

    /// <summary>Spawns a light set up by <paramref name="settings"/>.</summary>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnLight(LightSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeLightConfig
        {
            Kind = (int)settings.Kind,
            Intensity = settings.Intensity,
            ColorR = settings.Color.R,
            ColorG = settings.Color.G,
            ColorB = settings.Color.B,
            Range = settings.Range,
            Radius = settings.Radius,
            Shadows = settings.Shadows ? 1 : 0,
            InnerAngle = settings.InnerAngle,
            OuterAngle = settings.OuterAngle,
            ShadowDepthBias = settings.ShadowDepthBias,
            ShadowNormalBias = settings.ShadowNormalBias,
        };

        return new Entity(Native.bcs_render_spawn_light(&native));
    }

    /// <summary>
    /// Sets what a camera does to the picture after the scene has been drawn.
    /// </summary>
    /// <remarks>
    /// The whole pipeline in one call: an effect the settings leave off is removed from the
    /// camera, so the same call turns something on and off again. Only a camera can be given
    /// these, since it is the camera's render graph that reads them.
    /// </remarks>
    /// <param name="camera">A camera entity from <see cref="SpawnCamera3d()"/> or
    /// <see cref="Render2d.SpawnCamera2d"/>.</param>
    /// <param name="settings">What the camera should do.</param>
    /// <exception cref="ArgumentException">
    /// <see cref="AntiAliasPass.Temporal"/> is asked for together with multisampling.
    /// </exception>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// var camera = Render.SpawnCamera3d();
    ///
    /// Render.SetPostProcessing(camera, new PostSettings
    /// {
    ///     Hdr = true,
    ///     Bloom = true,
    ///     BloomIntensity = 0.2f,
    ///     Tonemapper = Tonemapper.AgX,
    ///     AntiAlias = AntiAliasPass.Fxaa,
    ///     Msaa = 1,
    /// });
    /// </code>
    /// </example>
    public static void SetPostProcessing(Entity camera, PostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Temporal antialiasing resolves the picture from the frames before it, and a
        // multisampled target has no such history. Bevy answers the pair by warning once a frame
        // and drawing nothing, which is the kind of quiet failure worth refusing outright.
        if (settings.AntiAlias == AntiAliasPass.Temporal && settings.Msaa is 2 or 4 or 8)
        {
            throw new ArgumentException(
                "temporal antialiasing cannot run on a multisampled target, and Msaa is "
                    + $"{settings.Msaa}; set Msaa to 1 to use it",
                nameof(settings));
        }

        var native = new NativePostConfig
        {
            Tonemapping = (int)settings.Tonemapper,
            Dither = settings.Dither ? 1 : 0,
            Hdr = settings.Hdr ? 1 : 0,
            Msaa = settings.Msaa,
            AntiAlias = (int)settings.AntiAlias,
            AntiAliasQuality = (int)settings.Quality,
            Sharpen = settings.Sharpen,
            Bloom = settings.Bloom ? 1 : 0,
            BloomIntensity = settings.BloomIntensity,
            BloomThreshold = settings.BloomThreshold,
            BloomThresholdSoftness = settings.BloomThresholdSoftness,
            BloomMode = (int)settings.BloomMode,
        };

        Native.Check(
            Native.bcs_render_set_post(camera.Bits, &native),
            $"setting the post processing on {camera}");
    }

    /// <summary>
    /// Sets the lens a camera draws through.
    /// </summary>
    /// <remarks>
    /// Beside <see cref="SetPostProcessing"/>: that call is the pipeline a settings screen owns,
    /// and this is what a scene does for a moment. The whole set in one call either way, so an
    /// effect these settings leave off is taken off the camera.
    /// </remarks>
    /// <param name="camera">A camera entity from <see cref="SpawnCamera3d()"/> or
    /// <see cref="Render2d.SpawnCamera2d"/>.</param>
    /// <param name="settings">The lens to draw through.</param>
    /// <exception cref="ArgumentException">
    /// <see cref="EffectSettings.ExposureCompensation"/> does not rise in luminance.
    /// </exception>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, an image named in the settings is not loaded, or
    /// this build has no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// Render.SetEffects(camera, new EffectSettings
    /// {
    ///     DepthOfField = DepthOfFieldMode.Bokeh,
    ///     FocalDistance = 8f,
    ///     Aperture = 1.4f,
    ///     ShutterAngle = 0.5f,
    ///     Vignette = 0.4f,
    /// });
    /// </code>
    /// </example>
    public static void SetEffects(Entity camera, EffectSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeEffectsConfig
        {
            DofMode = (int)settings.DepthOfField,
            FocalDistance = settings.FocalDistance,
            ApertureFStops = settings.Aperture,
            SensorHeight = settings.SensorHeight,
            MaxBlurDiameter = settings.MaxBlurDiameter,
            MaxDepth = settings.MaxDepth,
            ShutterAngle = settings.ShutterAngle,
            MotionBlurSamples = settings.MotionBlurSamples,
            Aberration = settings.Aberration,
            AberrationSamples = settings.AberrationSamples,
            AberrationLut = Key(settings.AberrationColors),
            Distortion = settings.Distortion,
            DistortionScale = settings.DistortionScale,
            DistortionAxisX = settings.DistortionAxes.X,
            DistortionAxisY = settings.DistortionAxes.Y,
            DistortionCenterX = settings.DistortionCenter.X,
            DistortionCenterY = settings.DistortionCenter.Y,
            DistortionEdgeCurvature = settings.DistortionEdgeCurvature,
            Vignette = settings.Vignette,
            VignetteRadius = settings.VignetteRadius,
            VignetteSmoothness = settings.VignetteSmoothness,
            VignetteRoundness = settings.VignetteRoundness,
            VignetteCenterX = settings.VignetteCenter.X,
            VignetteCenterY = settings.VignetteCenter.Y,
            VignetteEdgeCompensation = settings.VignetteEdgeCompensation,
            VignetteColorR = settings.VignetteColor.R,
            VignetteColorG = settings.VignetteColor.G,
            VignetteColorB = settings.VignetteColor.B,
            VignetteColorA = settings.VignetteColor.A,
            AutoExposure = settings.AutoExposure ? 1 : 0,
            MeteringMin = settings.MeteringRange.Min,
            MeteringMax = settings.MeteringRange.Max,
            MeteringLow = settings.MeteringFilter.Low,
            MeteringHigh = settings.MeteringFilter.High,
            SpeedBrighten = settings.SpeedBrighten,
            SpeedDarken = settings.SpeedDarken,
            ExposureTransition = settings.ExposureTransition,
            MeteringMask = Key(settings.MeteringMask),
        };

        var curve = settings.ExposureCompensation;
        if (curve is not null)
        {
            var count = Math.Min(curve.Count, NativeEffectsConfig.CompensationPoints);
            native.CompensationCount = (uint)count;

            for (var i = 0; i < count; i++)
            {
                // The curve is read by looking a measured brightness up in it, so it has to rise
                // in luminance. Caught here rather than on the far side, where the only answer
                // the bridge can give is a status code.
                if (i > 0 && curve[i].Luminance <= curve[i - 1].Luminance)
                {
                    throw new ArgumentException(
                        $"exposure compensation point {i} is at luminance {curve[i].Luminance}, "
                            + $"which is not above the {curve[i - 1].Luminance} before it",
                        nameof(settings));
                }

                native.CompensationCurve[i * 2] = curve[i].Luminance;
                native.CompensationCurve[(i * 2) + 1] = curve[i].Compensation;
            }
        }

        Native.Check(
            Native.bcs_render_set_effects(camera.Bits, &native),
            $"setting the effects on {camera}");

        static int Key(AssetHandle handle) => handle.IsValid ? handle.Key : -1;
    }

    /// <summary>
    /// Draws the sky the air scatters, seen from a camera.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two things make a sky: a planet, which is an entity the size of a world, and a camera told
    /// to sample it. This call keeps at most one planet in the world and points the camera at it,
    /// so calling it for a second camera adds a viewer rather than a second sky.
    /// </para>
    /// <para>
    /// The camera is given a high dynamic range target, which the sky needs: a sun scattered
    /// through air is far brighter than white. Pair it with
    /// <see cref="SetPostProcessing"/> for a tonemapper to bring that range back down.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera that should see the sky.</param>
    /// <param name="settings">How thick the air is, and at what scale.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// var camera = Render.SpawnCamera3d();
    /// var sun = Render.SpawnLight(LightKind.Directional, 10_000f);
    /// ctx.Ecs.Add(sun, Transform.LookingAt(new Vec3(1f, 0.6f, 0f), Vec3.Zero, Vec3.UnitY));
    ///
    /// Render.SetAtmosphere(camera, new AtmosphereSettings());
    /// Render.SetPostProcessing(camera, new PostSettings { Hdr = true });
    /// </code>
    /// </example>
    public static void SetAtmosphere(Entity camera, AtmosphereSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeAtmosphereConfig
        {
            Enabled = 1,
            Density = settings.Density,
            Scale = settings.Scale,
            HazeDistance = settings.HazeDistance,
        };

        Native.Check(
            Native.bcs_render_set_atmosphere(camera.Bits, &native),
            $"setting the atmosphere on {camera}");
    }

    /// <summary>
    /// Stops a camera drawing the sky.
    /// </summary>
    /// <remarks>
    /// The planet stays where it is. Nothing is computed for it until a camera asks again, so
    /// leaving it costs a component and no work.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    public static void ClearAtmosphere(Entity camera)
    {
        var native = new NativeAtmosphereConfig();

        Native.Check(
            Native.bcs_render_set_atmosphere(camera.Bits, &native),
            $"clearing the atmosphere on {camera}");
    }

    /// <summary>
    /// Sets how large a shadow map each kind of light gets, in pixels on a side.
    /// </summary>
    /// <remarks>
    /// One size for every directional light and one for every point and spot light, because Bevy
    /// keeps these globally rather than per light. Larger is sharper and costs memory and fill
    /// rate on every shadow-casting light at once. Bevy's defaults are 2048 and 1024. Zero leaves
    /// that kind as it is, so one can be changed without knowing the other.
    /// </remarks>
    /// <param name="directional">Size for directional lights, or 0 to leave it.</param>
    /// <param name="point">Size for point and spot lights, or 0 to leave it.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static void SetShadowMapSize(uint directional = 0, uint point = 0)
    {
        var status = Native.bcs_render_set_shadow_maps(directional, point);
        if (status == NativeStatus.Unsupported) throw NoRenderer("Setting the shadow map size");

        Native.Check(status, "setting the shadow map size");
    }

    /// <summary>
    /// Puts an entity on a set of render layers, as a bit per layer.
    /// </summary>
    /// <remarks>
    /// A camera draws an entity only where their layers overlap. Zero takes the entity back to
    /// Bevy's default layer, which is what every camera sees unless it says otherwise.
    /// </remarks>
    /// <example>
    /// <code>
    /// const uint Minimap = 1u &lt;&lt; 1;
    ///
    /// Render.SetLayers(ctx.Ecs, marker, Minimap);          // only the minimap camera sees it
    /// Render.SetLayers(ctx.Ecs, player, 1u | Minimap);     // both cameras do
    /// </code>
    /// </example>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static void SetLayers(EcsWorld world, Entity entity, uint layers)
    {
        ArgumentNullException.ThrowIfNull(world);

        var status = Native.bcs_render_set_layers(entity.Bits, layers);
        if (status == NativeStatus.Unsupported) throw NoRenderer("Setting render layers");

        Native.Check(status, $"setting the render layers of {entity}");
    }

    /// <summary>Attaches a handle through one of the components that carry one.</summary>
    private static void Attach(
        EcsWorld world,
        Entity entity,
        string component,
        AssetHandle handle,
        string described)
    {
        ArgumentNullException.ThrowIfNull(world);

        var status = Native.bcs_ecs_insert_asset(entity.Bits, component, handle.Key);
        if (status == NativeStatus.Unsupported) throw NoRenderer($"Attaching {described}");
        if (status == NativeStatus.NoEntity)
            throw new BevyNativeException(
                NativeStatus.NoEntity,
                $"Cannot attach {described} to {entity}: either the entity is no longer alive, "
                + "or the handle has been released.");
        if (status == NativeStatus.NoComponent)
            throw new BevyNativeException(
                NativeStatus.NoComponent,
                $"That handle does not point at {described}. Check that the handle came from the "
                + "matching Create call.");

        Native.Check(status, $"attaching {described}");
    }

    /// <summary>The error for asking a headless build to do something graphical.</summary>
    private static BevyNativeException NoRenderer(string attempted) =>
        new(NativeStatus.Unsupported,
            $"{attempted} needs a native build with the renderer compiled in. Rebuild the bridge "
            + "with build/build-native.sh --render, or guard the call with App.HasRenderer.");
}

/// <summary>
/// How a sprite is drawn.
/// </summary>
/// <remarks>
/// A sprite is a picture in the world rather than on the screen: it carries a
/// <see cref="Transform"/> like anything else, and a 2D camera decides what a world unit is worth
/// in pixels. For something pinned to the screen, use <see cref="Ui"/>.
/// </remarks>
/// <summary>
/// How a sprite's picture meets the size it is drawn at.
/// </summary>
public enum SpriteImageMode
{
    /// <summary>
    /// The picture's own size, stretched to <see cref="SpriteSettings.Size"/> when one is given.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Cut into nine, so the corners keep their size while the middle stretches.
    /// </summary>
    /// <remarks>A health bar or a dialogue box drawn at any width from one small image.</remarks>
    Sliced = 1,

    /// <summary>Repeated across the sprite rather than stretched.</summary>
    Tiled = 2,
}

/// <summary>
/// Where a sprite's transform sits on the picture.
/// </summary>
/// <remarks>
/// A sprite is centred on its transform unless told otherwise, which is awkward for anything
/// standing on the ground: its feet are then half a sprite below where it was placed. The
/// coordinates run from <c>-0.5</c> to <c>0.5</c> on each axis, with y upwards, so any point in
/// between is expressible as well as the nine named here.
/// </remarks>
public static class SpriteAnchor
{
    /// <summary>The middle, which is Bevy's own default.</summary>
    public static (float X, float Y) Center => (0f, 0f);

    /// <summary>The bottom left corner.</summary>
    public static (float X, float Y) BottomLeft => (-0.5f, -0.5f);

    /// <summary>The middle of the bottom edge, for anything standing on the ground.</summary>
    public static (float X, float Y) BottomCenter => (0f, -0.5f);

    /// <summary>The bottom right corner.</summary>
    public static (float X, float Y) BottomRight => (0.5f, -0.5f);

    /// <summary>The middle of the left edge.</summary>
    public static (float X, float Y) CenterLeft => (-0.5f, 0f);

    /// <summary>The middle of the right edge.</summary>
    public static (float X, float Y) CenterRight => (0.5f, 0f);

    /// <summary>The top left corner.</summary>
    public static (float X, float Y) TopLeft => (-0.5f, 0.5f);

    /// <summary>The middle of the top edge.</summary>
    public static (float X, float Y) TopCenter => (0f, 0.5f);

    /// <summary>The top right corner.</summary>
    public static (float X, float Y) TopRight => (0.5f, 0.5f);
}

public sealed class SpriteSettings
{
    /// <summary>Tint, multiplied with the image. White leaves it unchanged.</summary>
    public (float R, float G, float B, float A) Color { get; set; } = (1f, 1f, 1f, 1f);

    /// <summary>
    /// Width and height in world units, or null to use the image's own dimensions.
    /// </summary>
    public (float Width, float Height)? Size { get; set; }

    /// <summary>
    /// The part of the image to draw, in pixels, or null for all of it.
    /// </summary>
    /// <remarks>
    /// What a sprite sheet needs: one image holding many frames, each drawn by naming its
    /// rectangle rather than by loading a separate file.
    /// </remarks>
    public (float Left, float Top, float Right, float Bottom)? Rect { get; set; }

    /// <summary>Mirror horizontally, which is how one walk cycle faces both ways.</summary>
    public bool FlipX { get; set; }

    /// <summary>Mirror vertically.</summary>
    public bool FlipY { get; set; }

    /// <summary>
    /// The atlas layout naming the frames of a sheet, or none to draw the whole image.
    /// </summary>
    /// <remarks>
    /// Built by <see cref="Render2d.CreateAtlas"/>. With one of these a frame is named by
    /// <see cref="Frame"/> rather than by its pixel rectangle, so stepping an animation is
    /// counting rather than arithmetic.
    /// </remarks>
    public AssetHandle Atlas { get; set; } = AssetHandle.None;

    /// <summary>Which frame of <see cref="Atlas"/> to draw, counting across then down.</summary>
    public uint Frame { get; set; }

    /// <summary>
    /// Where the transform sits on the sprite, or null to leave it centred.
    /// </summary>
    /// <remarks><see cref="SpriteAnchor"/> names the nine usual points.</remarks>
    public (float X, float Y)? Anchor { get; set; }

    /// <summary>How the picture meets the size the sprite is drawn at.</summary>
    public SpriteImageMode Mode { get; set; } = SpriteImageMode.Auto;

    /// <summary>
    /// How far in from each edge the nine-slice cuts are, in pixels of the source image.
    /// </summary>
    /// <remarks>Read only when <see cref="Mode"/> is <see cref="SpriteImageMode.Sliced"/>.</remarks>
    public (float Left, float Top, float Right, float Bottom) SliceBorder { get; set; }

    /// <summary>How far a sliced corner may be scaled up.</summary>
    public float CornerScale { get; set; } = 1f;

    /// <summary>Repeat horizontally when tiled.</summary>
    public bool TileX { get; set; } = true;

    /// <summary>Repeat vertically when tiled.</summary>
    public bool TileY { get; set; } = true;

    /// <summary>
    /// How far the picture is drawn before a tile repeats, as a multiple of its own size.
    /// </summary>
    public float TileStretch { get; set; } = 1f;
}

/// <summary>
/// Draws in two dimensions: a camera that measures in pixels, and sprites to put under it.
/// </summary>
/// <remarks>
/// Separate from <see cref="Render"/> because the two are different ways of looking at the world
/// rather than different things to draw. Entities, transforms and parenting are the same
/// underneath, so a sprite can carry any component and be parented to anything.
/// </remarks>
/// <example>
/// <code>
/// Render2d.SpawnCamera2d();
///
/// var badge = ctx.Ecs.Spawn();
/// Render2d.SetSprite(ctx.Ecs, badge, AssetServer.Load(AssetKind.Image, "ui/badge.png"));
/// ctx.Ecs.Add(badge, Transform.At(120f, -80f, 0f));
/// </code>
/// </example>
public static unsafe class Render2d
{
    /// <summary>
    /// Spawns a 2D camera and returns it.
    /// </summary>
    /// <remarks>
    /// One world unit is one pixel, and the origin is the middle of the window, so a sprite at
    /// <c>(100, 50)</c> sits a hundred pixels right and fifty up from the centre.
    /// </remarks>
    /// <param name="order">
    /// Draw order. Leave at zero for a 2D-only game. Above a 3D camera's order it draws over the
    /// scene without clearing it, which is how a 2D overlay is layered on a 3D one.
    /// </param>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnCamera2d(int order = 0) =>
        new(Native.bcs_render_spawn_camera_2d(order));

    /// <summary>
    /// Builds an atlas layout over a grid of equal tiles and returns it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The layout is a list of rectangles and nothing else: it says where each frame sits, while
    /// the image it describes stays a separate asset. No image is passed here for that reason,
    /// and one layout serves every sheet cut the same way.
    /// </para>
    /// <para>
    /// Frames are numbered across each row and then down, starting at zero.
    /// </para>
    /// </remarks>
    /// <param name="tileWidth">Width of one frame, in pixels.</param>
    /// <param name="tileHeight">Height of one frame, in pixels.</param>
    /// <param name="columns">How many frames across.</param>
    /// <param name="rows">How many frames down.</param>
    /// <param name="padding">Gap between neighbouring frames, in pixels.</param>
    /// <param name="offset">Margin before the first frame, in pixels.</param>
    /// <exception cref="BevyNativeException">A dimension is zero, or no app is running.</exception>
    /// <example>
    /// <code>
    /// var sheet = AssetServer.Load(AssetKind.Image, "sprites/walk.png");
    /// var frames = Render2d.CreateAtlas(32, 32, columns: 8, rows: 1);
    ///
    /// Render2d.SetSprite(ctx.Ecs, walker, sheet, new SpriteSettings
    /// {
    ///     Atlas = frames,
    ///     Frame = step % 8,
    ///     Anchor = SpriteAnchor.BottomCenter,
    /// });
    /// </code>
    /// </example>
    public static AssetHandle CreateAtlas(
        uint tileWidth,
        uint tileHeight,
        uint columns,
        uint rows,
        (uint X, uint Y) padding = default,
        (uint X, uint Y) offset = default)
    {
        var key = Native.bcs_atlas_create(
            tileWidth, tileHeight, columns, rows, padding.X, padding.Y, offset.X, offset.Y);
        Native.Check(key, "building an atlas layout");

        return new AssetHandle(key);
    }

    /// <summary>Attaches a sprite to an entity, or replaces the one it has.</summary>
    /// <exception cref="BevyNativeException">The handle names no image, or the entity is gone.</exception>
    public static void SetSprite(EcsWorld world, Entity entity, AssetHandle image) =>
        SetSprite(world, entity, image, new SpriteSettings());

    /// <summary>Attaches a sprite drawn as <paramref name="settings"/> describes.</summary>
    /// <exception cref="BevyNativeException">The handle names no image, or the entity is gone.</exception>
    public static void SetSprite(
        EcsWorld world,
        Entity entity,
        AssetHandle image,
        SpriteSettings settings)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeSpriteConfig
        {
            Image = image.Key,
            ColorR = settings.Color.R,
            ColorG = settings.Color.G,
            ColorB = settings.Color.B,
            ColorA = settings.Color.A,
            HasSize = settings.Size is null ? 0 : 1,
            SizeX = settings.Size?.Width ?? 0f,
            SizeY = settings.Size?.Height ?? 0f,
            HasRect = settings.Rect is null ? 0 : 1,
            RectLeft = settings.Rect?.Left ?? 0f,
            RectTop = settings.Rect?.Top ?? 0f,
            RectRight = settings.Rect?.Right ?? 0f,
            RectBottom = settings.Rect?.Bottom ?? 0f,
            FlipX = settings.FlipX ? 1 : 0,
            FlipY = settings.FlipY ? 1 : 0,
            Atlas = settings.Atlas.Key,
            AtlasIndex = settings.Frame,
            HasAnchor = settings.Anchor is null ? 0 : 1,
            AnchorX = settings.Anchor?.X ?? 0f,
            AnchorY = settings.Anchor?.Y ?? 0f,
            Mode = (int)settings.Mode,
            SliceLeft = settings.SliceBorder.Left,
            SliceTop = settings.SliceBorder.Top,
            SliceRight = settings.SliceBorder.Right,
            SliceBottom = settings.SliceBorder.Bottom,
            CornerScale = settings.CornerScale,
            TileX = settings.TileX ? 1 : 0,
            TileY = settings.TileY ? 1 : 0,
            TileStretch = settings.TileStretch,
        };

        var status = Native.bcs_render_set_sprite(entity.Bits, &native);
        if (status == NativeStatus.Unsupported)
            throw new BevyNativeException(
                NativeStatus.Unsupported,
                "Attaching a sprite failed: this native build has no renderer. Rebuild the "
                + "bridge with build/build-native.sh --render.");

        Native.Check(status, $"attaching a sprite to {entity}");
    }
}
