//! The structs the render's calls take from the managed side, which mirrors each field for field:
//! cameras, reflections, post-processing and effects, the sky, gizmos, grading, sprites, images,
//! materials and lights.

/// How a camera should see, passed from C# when one is spawned.
///
/// A struct rather than an argument list because the list was already going to be ten long, and a
/// camera gains parameters as the renderer is bridged further.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsCameraConfig {
    /// `0` for perspective, `1` for orthographic.
    pub projection: i32,
    /// Vertical field of view in degrees. Perspective only.
    pub fov_degrees: f32,
    /// How much of the world fits vertically, in world units. Orthographic only.
    pub ortho_height: f32,
    /// Nearest visible distance.
    pub near: f32,
    /// Furthest visible distance. Ignored by an orthographic camera, which has no horizon.
    pub far: f32,
    /// `0` uses the world's clear color, `1` the one below, `2` draws over what is already there.
    pub clear_mode: i32,
    /// Clear color, used when `clear_mode` is `1`.
    pub clear: [f32; 4],
    /// Draw order. A camera with a higher order draws over one with a lower.
    pub order: i32,
    /// Non-zero to draw into part of the window rather than all of it.
    pub has_viewport: i32,
    /// Left, top, width and height of that part, in physical pixels.
    pub viewport: [u32; 4],
    /// Which render layers this camera sees, as a bit per layer. `0` means the default layer.
    pub layers: u32,
}

/// How a camera's screen-space reflections march. Mirrors the managed `ReflectionSettings`.
///
/// Roughness ranges run from where reflections start to fade in to where they are whole, and from
/// where they start to fade out to where they are gone, so a rough surface reflects nothing.
#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct BcsReflectionConfig {
    pub min_roughness_start: f32,
    pub min_roughness_full: f32,
    pub max_roughness_start: f32,
    pub max_roughness_end: f32,
    /// Where reflections stop at the edge of the picture, and where they are whole, as fractions
    /// of it.
    pub edge_gone: f32,
    pub edge_full: f32,
    /// How thick what the depth buffer holds is taken to be, in world units.
    pub thickness: f32,
    pub linear_steps: u32,
    pub linear_exponent: f32,
    pub bisection_steps: u32,
    pub use_secant: i32,
}

/// What a camera does to the picture after the scene has been drawn.
///
/// One config rather than a component per effect, because these are decided together. Bloom needs a
/// high dynamic range target, and an antialiasing pass and multisampling are two answers to the
/// same question. Every field is applied on every call, so a setting left alone is a setting turned
/// off, and one call describes the whole pipeline.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsPostConfig {
    /// Which curve maps the rendered range onto the display: `0` none, `1` Reinhard,
    /// `2` Reinhard luminance, `3` ACES fitted, `4` AgX, `5` somewhat boring, `6` TonyMcMapface,
    /// `7` Blender filmic.
    pub tonemapping: i32,
    /// Non-zero to dither before quantizing, which hides banding across a gradient.
    pub dither: i32,
    /// Non-zero to draw into a high dynamic range target, so a highlight can be brighter than
    /// white, and bloom reads it.
    pub hdr: i32,
    /// Samples per pixel taken while rasterizing: `1` off, or `2`, `4`, `8`.
    pub msaa: i32,
    /// The antialiasing that runs as a pass over the finished picture: `0` none, `1` FXAA,
    /// `2` SMAA, `3` temporal, which needs `msaa` off and a 3D camera.
    pub antialias: i32,
    /// How hard FXAA or SMAA looks for an edge: `0` low, `1` medium, `2` high, `3` ultra.
    pub antialias_quality: i32,
    /// Strength of contrast adaptive sharpening, `0` to leave the picture unsharpened.
    pub sharpen: f32,
    /// Non-zero to scatter light from the brightest parts of the picture.
    pub bloom: i32,
    /// How much is scattered.
    pub bloom_intensity: f32,
    /// Brightness a pixel has to reach before it blooms at all.
    pub bloom_threshold: f32,
    /// How gradually that threshold takes effect.
    pub bloom_threshold_softness: f32,
    /// `0` energy conserving, `1` additive, which is the older and brighter look.
    pub bloom_mode: i32,
}

/// The lens effects a camera can be given, beside the ones on [`BcsPostConfig`].
///
/// A second config rather than more fields on the first, because the two are decided at different
/// times. The pipeline is a settings screen, while a scene sets these for a moment, a hit, a dream,
/// a shot pulling focus. Both share the rule that every field is applied on every call, so an
/// effect a config leaves off is taken off the camera.
///
/// Depth of field, lens distortion and the vignette are switched by a mode or an intensity
/// rather than by a flag of their own, since each carries a value that means nothing happens.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsEffectsConfig {
    /// How out-of-focus depths are blurred: `0` not at all, `1` gaussian, `2` bokeh.
    pub dof_mode: i32,
    /// Distance in meters to what is in focus.
    pub focal_distance: f32,
    /// Aperture in f-stops. Smaller opens the lens, which is a shallower depth of field.
    pub aperture_f_stops: f32,
    /// Height of the imaginary sensor, in meters. With the field of view this fixes the focal
    /// length. `0` takes Bevy's own, which is the Super 35 cinema format.
    pub sensor_height: f32,
    /// Widest a blur may be, in pixels. `0` takes Bevy's own.
    pub max_blur_diameter: f32,
    /// Distance past which nothing is blurred any further, in meters. `0` leaves it unbounded,
    /// which blurs a sky as hard as the cap allows.
    pub max_depth: f32,
    /// Fraction of a frame the shutter is open, `0` for no motion blur. `0.5` is a film camera's
    /// 180 degree shutter; above `1` smears further than anything moved.
    pub shutter_angle: f32,
    /// Samples taken either side of a pixel along its motion. `0` also turns motion blur off.
    pub motion_blur_samples: u32,
    /// Width of the colored fringe, as a fraction of the window. `0` for none.
    pub aberration: f32,
    /// Cap on the samples the fringe is built from. `0` takes Bevy's own.
    pub aberration_samples: u32,
    /// Asset key of the image the fringe takes its colors from, or `-1` for red, green, blue.
    pub aberration_lut: i32,
    /// Strength of the lens warp: positive bulges outwards, negative pinches inwards, `0` for a
    /// straight picture.
    pub distortion: f32,
    /// Zoom applied after warping, to crop the edges a strong warp leaves uncovered.
    pub distortion_scale: f32,
    /// How much of the warp lands on each axis, `[1, 1]` for a round one.
    pub distortion_axes: [f32; 2],
    /// Point the warp radiates from, in fractions of the window.
    pub distortion_center: [f32; 2],
    /// How sharply the warp bends at the edges of the picture.
    pub distortion_edge_curvature: f32,
    /// How dark the corners go, `0` for no vignette and `1` for black.
    pub vignette: f32,
    /// How much of the picture is left untouched, as a fraction of the window.
    pub vignette_radius: f32,
    /// Width of the edge between the clear center and the dark corners.
    pub vignette_smoothness: f32,
    /// Shape of that edge, `1` for a circle.
    pub vignette_roundness: f32,
    /// Point the vignette is centered on, in fractions of the window.
    pub vignette_center: [f32; 2],
    /// How far the vignette is stretched to fit a window that is not square, `0` not at all and
    /// `1` exactly.
    pub vignette_edge_compensation: f32,
    /// Linear color the corners are taken toward, usually black.
    pub vignette_color: [f32; 4],
    /// Non-zero to let the camera find its own exposure from what it can see.
    pub auto_exposure: i32,
    /// Darkest luminance the metering counts, in EV-100.
    pub metering_min: f32,
    /// Brightest luminance the metering counts, in EV-100.
    pub metering_max: f32,
    /// Fraction of the darkest samples thrown away before averaging.
    pub metering_low: f32,
    /// Fraction below which the brightest samples are kept, so `0.9` throws away the top tenth.
    pub metering_high: f32,
    /// How fast the exposure opens when a scene darkens, in f-stops per second.
    pub speed_brighten: f32,
    /// How fast it closes when a scene brightens, in f-stops per second.
    pub speed_darken: f32,
    /// How near the target the adaptation stops being linear, in f-stops.
    pub exposure_transition: f32,
    /// Asset key of an image weighting where in the frame the metering looks, or `-1` to weight
    /// the whole frame alike. Only the red channel is read.
    pub metering_mask: i32,
    /// How many of [`Self::compensation_curve`] are used, `0` for no compensation. Two or more
    /// points make a curve.
    pub compensation_points: u32,
    /// Pairs of measured luminance in EV-100 and the compensation to apply there in f-stops, rising
    /// in luminance. The flat config carries eight points.
    pub compensation_curve: [f32; 16],
}

/// A sky computed from the light scattering through a planet's air.
///
/// The atmosphere itself is a planet-sized entity that the camera looks out from, so this config
/// describes both: which planet, and how the camera should sample it.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsAtmosphereConfig {
    /// Non-zero to draw the sky, zero to stop this camera drawing it.
    pub enabled: i32,
    /// Multiplies the density of the air, thickening or thinning the haze. `0` takes earth's.
    pub density: f32,
    /// Scales the planet against the scene, for a world that is not measured in meters. `0`
    /// takes one.
    pub scale: f32,
    /// How far the scattering in front of the scene is computed for, in meters. `0` takes Bevy's
    /// own distance.
    pub haze_distance: f32,
    /// How finely the sky is computed: `0` Bevy's own, `1` cheaper, `2` finer. Every one of them
    /// draws the same sky, and the difference is banding in a gradient and how much of a frame it
    /// costs.
    pub quality: i32,
    /// How much light the ground bounces back into the air, where earth's is `0.3` and Mars's is
    /// `0.1`. `0` takes the one that belongs to the medium.
    pub ground_albedo: f32,
}

/// One debug shape to draw this frame.
///
/// Which fields matter depends on `kind`, because the three shapes take different arguments and
/// one struct crossing the boundary is simpler than three exports.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsGizmoConfig {
    /// Which shape to draw.
    ///
    /// `0` line, `1` sphere, `2` axes, `3` a line fading from one color to another, `4` rectangle,
    /// `5` circle, `6` arc, `7` arrow, `8` grid, `9` box, `10` capsule, `11` cone, `12` cylinder,
    /// `13` torus, `20` conical frustum. `14` to `19` are the flat rectangle, circle, fading line,
    /// arrow, arc and grid a 2D camera draws, which read the same numbers with the third one
    /// dropped. What the fields below mean depends on this, because every shape is described by
    /// the same handful of numbers.
    pub kind: i32,
    /// Where the shape sits: a line's start, or the center of everything else.
    pub start: [f32; 3],
    /// The second set of numbers, read differently by each shape that needs one.
    ///
    /// A line's or an arrow's far end, a rectangle's width and height, an arc's angle in radians,
    /// a grid's cell counts across and down, a box's width, height and depth, or the one further
    /// number a capsule, a cone, a cylinder or a torus needs beside its radius.
    pub end: [f32; 3],
    /// Which way the shape faces, as a quaternion. A line and an arrow have two ends instead.
    pub rotation: [f32; 4],
    /// How large: a sphere's or a circle's radius, an axis arm's length, or a grid cell's side.
    pub radius: f32,
    /// Linear RGBA. Axes color themselves red, green and blue.
    pub color: [f32; 4],
    /// What a line fades to at its far end. Only read by kind `3`.
    pub end_color: [f32; 4],
    /// Whether the scene can hide it. `0` is depth tested; anything else draws over everything.
    pub in_front: i32,
}

/// One tonal range's part of a color grade.
///
/// The four multipliers are the standard ASC CDL terms, so a grade written for a film pipeline
/// carries across: `out = (i * gain + lift) ^ gamma`, with saturation applied around it.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsGradingSection {
    /// Below one drains color toward gray, above one spreads it out. One leaves it alone.
    pub saturation: f32,
    /// Below one pulls toward neutral gray, above one pushes away from it.
    pub contrast: f32,
    /// The exponent, which mostly moves the top of the range.
    pub gamma: f32,
    /// The multiplier, which mostly moves the middle of the range.
    pub gain: f32,
    /// The offset, which mostly moves the bottom of the range.
    pub lift: f32,
}

/// How a camera grades the picture after it has been tonemapped.
///
/// Three tonal ranges plus what applies to all of them, which is the shape a colorist works in and
/// the shape Bevy keeps it in. Flat rather than nested ranges, because a range is two numbers and a
/// struct that crosses the ABI is easier to mirror when everything in it is a float.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsGradingConfig {
    /// Stops of exposure applied before anything else. `0` leaves it alone.
    pub exposure: f32,
    /// White balance, toward blue below zero and toward orange above it.
    pub temperature: f32,
    /// White balance the other way, toward green and toward magenta.
    pub tint: f32,
    /// Hue rotation in degrees.
    pub hue: f32,
    /// Saturation applied to everything, after the three sections.
    pub post_saturation: f32,
    /// Where the midtones begin, as a luminance. Below this is shadow.
    pub midtones_from: f32,
    /// Where they end. Above this is highlight.
    pub midtones_to: f32,
    /// The darkest range.
    pub shadows: BcsGradingSection,
    /// The middle range.
    pub midtones: BcsGradingSection,
    /// The brightest range.
    pub highlights: BcsGradingSection,
}

/// How a sprite is drawn.
///
/// A sprite is a picture in the world rather than on the screen. It has a `Transform` like any
/// other entity, and a 2D camera decides what a world unit is worth in pixels.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsSpriteConfig {
    /// Asset key of the image to draw.
    pub image: i32,
    /// Tint, multiplied with the image. Linear RGBA, white for the image unchanged.
    pub color: [f32; 4],
    /// Non-zero to draw at `size` rather than at the image's own dimensions.
    pub has_size: i32,
    /// Width and height in world units, used when `has_size` is set.
    pub size: [f32; 2],
    /// Non-zero to draw only the part of the image `rect` names.
    pub has_rect: i32,
    /// Left, top, right and bottom of that part, in pixels.
    pub rect: [f32; 4],
    /// Non-zero to mirror horizontally.
    pub flip_x: i32,
    /// Non-zero to mirror vertically.
    pub flip_y: i32,
    /// Asset key of the atlas layout naming the frames, or a negative for a whole image.
    pub atlas: i32,
    /// Which frame of that layout to draw.
    pub atlas_index: u32,
    /// Non-zero to move the sprite's origin to `anchor` rather than leaving it centered.
    pub has_anchor: i32,
    /// Where the transform sits on the sprite, from `-0.5` to `0.5` on each axis.
    pub anchor: [f32; 2],
    /// How the picture meets `size`: `0` its own, `1` sliced, `2` tiled, `3` scaled to fit.
    pub mode: i32,
    /// How a scaled picture fits: `0` fit centered, `1` fit at the start, `2` fit at the end,
    /// `3` fill centered, `4` fill at the start, `5` fill at the end. Read only when `mode` is `3`.
    pub scaling: i32,
    /// Left, top, right and bottom insets of the nine-slice border, in pixels.
    pub slice_border: [f32; 4],
    /// How far a sliced corner may be scaled up. `0` takes Bevy's default of one.
    pub corner_scale: f32,
    /// Non-zero to repeat horizontally when tiled.
    pub tile_x: i32,
    /// Non-zero to repeat vertically when tiled.
    pub tile_y: i32,
    /// How far the picture stretches before a tile repeats. `0` takes Bevy's default of one.
    pub tile_stretch: f32,
    /// Which parts of a sliced picture tile rather than stretch, as a mask: `1` the sides, `2` the
    /// middle, `3` both, `0` neither. Read only when the picture is sliced, and the repeat is
    /// measured by `tile_stretch` the way a tiled picture's is.
    pub slice_tiling: i32,
}

/// How an image should be sampled, and how its bytes should be read.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsImageConfig {
    /// `0` clamp to edge, `1` repeat, `2` mirror. Applied to U.
    pub address_u: i32,
    /// The same, for V.
    pub address_v: i32,
    /// `0` nearest, `1` linear. Used when the texture is drawn larger than it is.
    pub mag_filter: i32,
    /// The same, for when it is drawn smaller.
    pub min_filter: i32,
    /// The same, for blending between mip levels.
    pub mipmap_filter: i32,
    /// Maximum anisotropic samples. `1` disables it.
    pub anisotropy: u32,
    /// Non-zero to read the file as sRGB, which is right for color and wrong for data.
    pub srgb: i32,
    /// How many layers a loaded file is cut into from the top down, which makes it an array
    /// texture. `0` and `1` leave it one picture. A sampler being set reads none of it.
    pub layers: u32,
}

/// Everything a physically based material is made of.
///
/// Texture fields are asset keys, or `-1` for none. An image bound here is used as-is; the renderer
/// combines one with the matching factor, so a white base color with a base color map shows the map
/// unchanged.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsMaterialConfig {
    /// Base color, as linear sRGB with alpha.
    pub base_color: [f32; 4],
    /// How metallic the surface is, from dielectric at zero to metal at one.
    pub metallic: f32,
    /// How rough, from a mirror near zero to fully diffuse at one.
    pub roughness: f32,
    /// Light the surface gives off, which is not affected by any lamp. The first three are
    /// luminance in nits, and the fourth decides whether the camera's exposure is applied to
    /// them: `1` scales them the way it scales everything else, `0` leaves them as written.
    /// Nothing here is drawn at all when `unlit` is set, because Bevy adds the emission inside
    /// the lighting it is skipping.
    pub emissive: [f32; 4],
    /// `0` opaque, `1` cut out at `alpha_cutoff`, `2` blended, `3` added to what is behind.
    pub alpha_mode: i32,
    /// Where a cut-out material stops drawing, used when `alpha_mode` is `1`.
    pub alpha_cutoff: f32,
    /// Non-zero to draw back faces as well as front ones.
    pub double_sided: i32,
    /// Non-zero to show the base color flat, with no lighting at all.
    pub unlit: i32,
    /// Asset key of the base color map, or `-1`.
    pub base_color_texture: i32,
    /// Asset key of the tangent-space normal map, or `-1`.
    pub normal_map: i32,
    /// Asset key of the combined metallic and roughness map, or `-1`.
    pub metallic_roughness_texture: i32,
    /// Asset key of the emissive map, or `-1`.
    pub emissive_texture: i32,
    /// Asset key of the ambient occlusion map, or `-1`.
    pub occlusion_texture: i32,
    /// How many times the texture repeats across the surface, in U and V.
    pub uv_scale: [f32; 2],
    /// Radians the texture is turned by.
    pub uv_rotation: f32,
    /// How far the texture is shifted, in UV units.
    pub uv_offset: [f32; 2],
    /// How much light a dielectric reflects head on, where `0.5` is Bevy's default and four
    /// percent, which most materials are.
    pub reflectance: f32,
    /// How strong a clear varnish over the surface is, from none at zero.
    pub clearcoat: f32,
    /// How rough that varnish is, from a mirror near zero.
    pub clearcoat_roughness: f32,
    /// How much light passes straight through, as through glass, from none at zero. A camera
    /// draws it only with screen-space transmission steps set.
    pub specular_transmission: f32,
    /// How much light passes through and scatters, as through a leaf or wax, from none at zero.
    pub diffuse_transmission: f32,
    /// How thick the material is where light passes through, in world units.
    pub thickness: f32,
    /// How much light bends passing in, `1.5` for glass and Bevy's default.
    pub ior: f32,
    /// How far light travels inside before it takes on `attenuation_color`, in world units, or
    /// infinity for a clear material.
    pub attenuation_distance: f32,
    /// The color light takes on inside, linear RGBA.
    pub attenuation_color: [f32; 4],
    /// How much the highlight stretches along the surface, as brushed metal's does, from none at
    /// zero to one.
    pub anisotropy_strength: f32,
    /// Radians the stretch is turned by, from the tangent.
    pub anisotropy_rotation: f32,
    /// Asset key of the clearcoat strength map, or `-1`.
    pub clearcoat_texture: i32,
    /// Asset key of the clearcoat roughness map, or `-1`.
    pub clearcoat_roughness_texture: i32,
    /// Asset key of the clearcoat's own normal map, or `-1`.
    pub clearcoat_normal_texture: i32,
    /// Asset key of the straight transmission map, or `-1`.
    pub specular_transmission_texture: i32,
    /// Asset key of the scattered transmission map, or `-1`.
    pub diffuse_transmission_texture: i32,
    /// Asset key of the thickness map, or `-1`.
    pub thickness_texture: i32,
    /// Asset key of the anisotropy direction and strength map, or `-1`.
    pub anisotropy_texture: i32,
    /// What a baked lightmap's values are multiplied by, in nits, for a lightmap stored at a
    /// scale other than the one the scene is lit at. One leaves them as they are.
    pub lightmap_exposure: f32,
}

/// What kind of light to spawn and how it behaves.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsLightConfig {
    /// `0` directional, `1` point, `2` spot.
    pub kind: i32,
    /// Illuminance in lux for a directional light, luminous power in lumens for the other two.
    pub intensity: f32,
    /// Linear RGB.
    pub color: [f32; 3],
    /// How far the light reaches. Point and spot only.
    pub range: f32,
    /// Radius of the emitting sphere, which softens the shadow edge. Point and spot only.
    pub radius: f32,
    /// Bit one casts shadows into a shadow map, and bit two casts contact shadows as well.
    pub shadows: i32,
    /// Radians from the axis within which a spot light is at full brightness.
    pub inner_angle: f32,
    /// Radians from the axis at which a spot light has fallen to nothing.
    pub outer_angle: f32,
    /// How far along its own normal a surface is pushed before it is tested against the shadow
    /// map. Trades shadow acne for a shadow that starts slightly late.
    pub shadow_depth_bias: f32,
    /// The same, along the surface normal, which handles a surface lit at a glancing angle.
    pub shadow_normal_bias: f32,
}
