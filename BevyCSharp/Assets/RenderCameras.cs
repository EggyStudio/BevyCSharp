namespace Bevy;

/// <summary>How a camera turns the world into a picture.</summary>
public enum CameraProjection
{
    /// <summary>Things shrink with distance, as an eye sees them.</summary>
    Perspective = 0,

    /// <summary>
    /// Parallel lines stay parallel and distance does not shrink anything, as an isometric or a
    /// top-down view needs.
    /// </summary>
    Orthographic = 1,
}

/// <summary>What a camera does with the pixels it is about to draw over.</summary>
public enum ClearMode
{
    /// <summary>Clear to the world's clear color.</summary>
    World = 0,

    /// <summary>Clear to this camera's own color.</summary>
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
    /// <summary>Clip anything brighter than white, as no tonemapping does.</summary>
    None = 0,

    /// <summary>The classic curve. Colors shift hue as they brighten.</summary>
    Reinhard = 1,

    /// <summary>The same on luminance only, so bright colors keep their hue better.</summary>
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
/// Separate from <see cref="PostSettings.Msaa"/>, which works while the scene is rasterized and
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
/// decided together. Bloom needs a high dynamic range target, and multisampling and an antialiasing
/// pass are two answers to the same question. Every field is applied on every call, so an effect
/// this object leaves off is taken off the camera. Turning bloom off is the same call as turning it
/// on, which suits a settings screen.
/// </remarks>
public sealed class PostSettings
{
    /// <summary>Which curve maps the rendered range onto the display.</summary>
    public Tonemapper Tonemapper { get; set; } = Tonemapper.TonyMcMapface;

    /// <summary>
    /// Dither before quantizing to the display's bit depth.
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
    /// Samples per pixel taken while the scene is rasterized: 1, 2, 4 or 8.
    /// </summary>
    /// <remarks>
    /// Smooths the edges of geometry and nothing else. Four is Bevy's own; one turns it off, as a
    /// game leaning on <see cref="AntiAlias"/> does, and <see cref="AntiAliasPass.Temporal"/>
    /// requires it.
    /// </remarks>
    public int Msaa { get; set; } = 4;

    /// <summary>An antialiasing pass over the finished picture.</summary>
    public AntiAliasPass AntiAlias { get; set; } = AntiAliasPass.None;

    /// <summary>
    /// How hard that pass looks for an edge.
    /// </summary>
    /// <remarks>
    /// Read by <see cref="AntiAliasPass.Fxaa"/> and <see cref="AntiAliasPass.Smaa"/>. Temporal
    /// antialiasing has no such setting, because how much it catches is decided by how many frames
    /// it has to work from.
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
    /// Needs <see cref="Hdr"/> to have anything to work with, because without it nothing is
    /// brighter than white, so nothing is bright enough to glow. To make one object glow harder,
    /// raise its material's emissive color rather than this.
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
    /// Each point of light spreads into a disc, as a lens spreads it, which makes a highlight
    /// behind the subject into a circle.
    /// </summary>
    Bokeh = 2,
}

/// <summary>
/// The lens a camera draws through.
/// </summary>
/// <remarks>
/// <para>
/// Beside <see cref="PostSettings"/> rather than part of it, because the two are decided at
/// different times. A settings screen owns the pipeline, and a scene sets these for a moment, a
/// hit, a dream, a shot pulling focus. The rule is the same, so every field is applied on every
/// call and an effect these settings leave off is taken off the camera.
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

    /// <summary>Distance in meters to what is in focus.</summary>
    public float FocalDistance { get; set; } = 10f;

    /// <summary>
    /// Aperture in f-stops.
    /// </summary>
    /// <remarks>
    /// Smaller opens the lens wider, which leaves less of the scene in focus. Bevy's own is 1,
    /// which is wide, so a scene that should be mostly sharp needs a larger number.
    /// </remarks>
    public float Aperture { get; set; } = 1f;

    /// <summary>
    /// Height of the imaginary sensor, in meters.
    /// </summary>
    /// <remarks>
    /// With the camera's field of view this fixes the focal length, so it is the other half of
    /// how strong the blur is. Zero takes Bevy's own, the Super 35 cinema format.
    /// </remarks>
    public float SensorHeight { get; set; }

    /// <summary>
    /// Widest a single blur may be, in pixels. Zero takes Bevy's own.
    /// </summary>
    /// <remarks>Not physical. It caps how slow a very out-of-focus frame is allowed to
    /// be.</remarks>
    public float MaxBlurDiameter { get; set; }

    /// <summary>
    /// Distance past which nothing is blurred any further, in meters.
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
    /// Zero is no motion blur. A film camera's 180 degree shutter is 0.5, which gives a cinematic
    /// look at 24 frames a second; at 60 the same look is about 1.25. Above 1 a thing smears
    /// further than it moved, which is a choice rather than a mistake.
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
    /// Width of the colored fringe around edges, as a fraction of the window. Zero for none.
    /// </summary>
    /// <remarks>
    /// What a lens does when it fails to focus every color at one point. Bevy's own strength is
    /// 0.02, and a horror game reaching for it on a hit needs more.
    /// </remarks>
    public float Aberration { get; set; }

    /// <summary>Cap on the samples the fringe is built from. Zero takes Bevy's own.</summary>
    public uint AberrationSamples { get; set; }

    /// <summary>
    /// An image the fringe takes its colors from, read across its width.
    /// </summary>
    /// <remarks>
    /// Nothing here gives the usual red, green, blue. The image is sampled down its vertical
    /// center, so it should be one pixel tall.
    /// </remarks>
    public AssetHandle AberrationColors { get; set; } = AssetHandle.None;

    /// <summary>
    /// Strength of the lens warp. Zero leaves straight lines straight.
    /// </summary>
    /// <remarks>
    /// Positive bulges the picture outwards, as a wide lens does; negative pinches it inwards.
    /// Bevy's own strength is 0.5.
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
    /// toward the middle or to show that the player is hurt.
    /// </remarks>
    public float Vignette { get; set; }

    /// <summary>How much of the picture is left untouched, as a fraction of the window.</summary>
    public float VignetteRadius { get; set; } = 0.75f;

    /// <summary>Width of the edge between the clear center and the dark corners.</summary>
    public float VignetteSmoothness { get; set; } = 5f;

    /// <summary>Shape of that edge, where 1 is a circle.</summary>
    public float VignetteRoundness { get; set; } = 1f;

    /// <summary>Point the vignette is centered on, in fractions of the window.</summary>
    public (float X, float Y) VignetteCenter { get; set; } = (0.5f, 0.5f);

    /// <summary>
    /// How far the vignette is stretched to fit a window that is not square, 0 not at all and 1
    /// exactly.
    /// </summary>
    public float VignetteEdgeCompensation { get; set; } = 1f;

    /// <summary>The color the corners are taken toward, linear. Black is the usual one.</summary>
    public (float R, float G, float B, float A) VignetteColor { get; set; } = (0f, 0f, 0f, 1f);

    /// <summary>
    /// Let the camera find its own exposure from what it can see.
    /// </summary>
    /// <remarks>
    /// A histogram of the frame's brightness is built and the exposure moved so that the average
    /// lands on middle gray, as an eye adjusts walking out of a cave. The camera is given a high
    /// dynamic range target, because there is nothing to meter without one.
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
    /// Inside this distance the exposure eases in rather than tracking straight, which stops it
    /// jittering while the scene changes slightly from frame to frame.
    /// </remarks>
    public float ExposureTransition { get; set; }

    /// <summary>
    /// An image weighting where in the frame the metering looks.
    /// </summary>
    /// <remarks>
    /// Only the red channel is read, and it is stretched over the whole frame, so black ignores a
    /// pixel and white counts it fully. Nothing here weights the whole frame alike.
    /// </remarks>
    public AssetHandle MeteringMask { get; set; } = AssetHandle.None;

    /// <summary>
    /// A curve applied to the exposure the metering arrived at.
    /// </summary>
    /// <remarks>
    /// Each point pairs a measured luminance in EV-100 with the compensation to apply there in
    /// f-stops, so a night scene can be left darker than middle gray and a desert left brighter.
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
/// Not a picture of a sky but a simulation of one. The color of every direction is worked out
/// from how far light travels through the air to reach it, so the horizon reddens, the zenith
/// stays blue, and the whole thing turns over as the sun moves. Distant geometry picks up the
/// same haze.
/// </para>
/// <para>
/// The sun is whichever directional light is in the scene, so its direction and color move the sky.
/// A scene with no directional light gets a night sky.
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
    /// How large the planet is against the scene, for a world not measured in meters.
    /// </summary>
    /// <remarks>
    /// The planet is the size of a real one and its ground sits at the origin, so a scene in meters
    /// needs nothing here. A scene in kilometers needs a smaller number, since what matters is how
    /// far the camera moves through the air.
    /// </remarks>
    public float Scale { get; set; } = 1f;

    /// <summary>
    /// How far in front of the camera the haze is computed, in meters.
    /// </summary>
    /// <remarks>
    /// What decides where distant geometry fades into the sky. Zero leaves Bevy's own distance,
    /// which suits a scene measured in meters.
    /// </remarks>
    public float HazeDistance { get; set; }

    /// <summary>
    /// How finely the sky is computed.
    /// </summary>
    /// <remarks>
    /// One number rather than the dozen Bevy exposes, because every one of them trades the same
    /// thing and setting them apart is tuning a renderer rather than describing a sky. The sky is
    /// the same either way, and what changes is banding in a gradient and how much of a frame it
    /// costs.
    /// </remarks>
    public SkyQuality Quality { get; set; } = SkyQuality.Default;

    /// <summary>
    /// How much light the ground bounces back into the air, from zero to one.
    /// </summary>
    /// <remarks>
    /// What makes the underside of the haze bright over snow and dark over sea. Earth's is 0.3 and
    /// Mars's is 0.1. Zero leaves the one that belongs to the medium.
    /// </remarks>
    public float GroundAlbedo { get; set; }
}

/// <summary>How finely the sky is computed.</summary>
/// <remarks>
/// Every one of these draws the same sky. What changes is how smooth a gradient across it is and
/// how much of a frame it costs, so this is a graphics setting rather than an artistic one.
/// </remarks>
public enum SkyQuality
{
    /// <summary>What Bevy chose, which suits most scenes.</summary>
    Default = 0,

    /// <summary>Half the samples, for a machine that needs the frame back.</summary>
    Cheap = 1,

    /// <summary>Twice the samples, for a sky that fills the screen and has to be smooth.</summary>
    Fine = 2,
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
    /// Depth precision is spent between here and <see cref="Far"/>, and mostly near this end, so a
    /// very small value makes distant surfaces flicker against each other.
    /// </remarks>
    public float Near { get; set; } = 0.1f;

    /// <summary>Furthest visible distance. Ignored by an orthographic camera.</summary>
    public float Far { get; set; } = 1000f;

    /// <summary>What to do with the pixels already there.</summary>
    public ClearMode Clear { get; set; } = ClearMode.World;

    /// <summary>The color used when <see cref="Clear"/> is <see cref="ClearMode.Custom"/>.</summary>
    /// <remarks>Linear RGBA, not sRGB, so these are the numbers a shader works in.</remarks>
    public (float R, float G, float B, float A) ClearColor { get; set; } = (0f, 0f, 0f, 1f);

    /// <summary>Draw order. A camera with a higher order draws over one with a lower.</summary>
    public int Order { get; set; }

    /// <summary>
    /// The part of the window to draw into, in physical pixels, or null for all of it.
    /// </summary>
    /// <remarks>
    /// Splitscreen is two cameras, each given half the window. Physical pixels rather than logical
    /// ones, because a framebuffer is divided into those, so half a window is half its physical
    /// width whatever the display scaling.
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
