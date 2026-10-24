//! The denoiser over Solari's lit result, so the ray-traced tier's picture is clean without DLSS.
//!
//! Solari writes the light it resampled into the view target as one color, with the surface's
//! color and its emissive in it, and leaves the noise to DLSS Ray Reconstruction, which the
//! bridge does not build. This is spatiotemporal variance-guided filtering (Schied and others,
//! 2017) as a node of the bridge's own, in five stages run as compute passes over the view target
//! after Solari has lit it and before Bevy's post-processing:
//!
//! 1. Demodulation. The deferred G-buffer's emissive, times the exposure as Solari applies it, is
//!    taken off, and the rest divided by the share of the light the surface sends back, its diffuse
//!    color and its specular reflection at the angle it is seen at, above [`ALBEDO_FLOOR`], so the
//!    filter blurs light rather than the texture the light falls on.
//! 2. Temporal accumulation. The light is reprojected by Bevy's motion vectors onto the previous
//!    frame's history, four texels weighed bilinearly, each kept where the previous depth, which
//!    Solari double-buffers, and the previous normal agree with this frame's ([`agrees`]), and the
//!    history and the first two moments of luminance accumulated beside it, up to
//!    [`DenoiserSettings::history_limit`] frames.
//! 3. The variance of luminance, from the moments where the history holds [`VARIANCE_FRAMES`] or
//!    more, and from a seven by seven neighborhood of this frame's light where it holds fewer.
//! 4. Five à-trous passes at steps 1, 2, 4, 8 and 16, each tap of a five by five B3 spline kernel
//!    weighted by its depth, normal and luminance differences ([`weight`]), the luminance's against
//!    the root of the variance blurred over three by three, the variance filtered along with the
//!    squares of the weights, and the first pass's light kept as the next frame's history.
//! 5. Remodulation, the filtered light times the same share, plus the emissive, into the view
//!    target.
//!
//! The stage order and the defaults are SVGF's. The shader is `denoiser.wgsl`, with no imports so a
//! test validates it as it is, and the functions here that it mirrors, the weights and the
//! reprojection, are tested as pure functions.

use std::sync::Mutex;
use std::sync::atomic::{AtomicBool, Ordering};

use crate::interop::status;

/// What the denoiser is told, the history's clamp and the widths of the three edge-stopping
/// weights, at SVGF's defaults. Laid out for C as the managed `NativeDenoiserSettings` reads it,
/// four floats in this order.
#[repr(C)]
#[derive(Clone, Copy, Debug, PartialEq)]
pub struct DenoiserSettings {
    /// How many frames the history holds at most, so new light still weighs a frame of that many.
    pub history_limit: f32,
    /// How far a tap's depth may differ, against the depth's change over its reach, the paper's
    /// sigma z.
    pub sigma_depth: f32,
    /// The power a tap's normal's agreement is raised to, the paper's sigma n.
    pub sigma_normal: f32,
    /// How far a tap's luminance may differ, against the root of the variance, the paper's sigma l.
    pub sigma_luminance: f32,
}

impl DenoiserSettings {
    /// Whether every setting is one the filter can use, the history at least a frame and each
    /// width a finite number above nought, since a width of nought divides a weight by nothing.
    pub fn usable(&self) -> bool {
        self.history_limit.is_finite()
            && self.history_limit >= 1.0
            && [self.sigma_depth, self.sigma_normal, self.sigma_luminance]
                .iter()
                .all(|width| width.is_finite() && *width > 0.0)
    }
}

impl Default for DenoiserSettings {
    fn default() -> Self {
        Self {
            history_limit: 32.0,
            sigma_depth: 1.0,
            sigma_normal: 128.0,
            sigma_luminance: 4.0,
        }
    }
}

/// The least share the light is divided by and multiplied back by, so a black surface keeps its
/// light rather than dividing by nothing.
pub const ALBEDO_FLOOR: f32 = 0.01;

/// How far a previous surface's plane may lie from the current point, as a fraction of its
/// distance from the camera, for the history to be kept.
pub const PLANE_DISTANCE: f32 = 0.01;

/// The least cosine between the current and the previous normal for the history to be kept.
pub const NORMAL_THRESHOLD: f32 = 0.9;

/// How many frames of history the moments' variance is read from, fewer taking it from the
/// neighborhood.
pub const VARIANCE_FRAMES: f32 = 4.0;

/// The à-trous passes' steps, in pixels.
pub const STEPS: [u32; 5] = [1, 2, 4, 8, 16];

/// Whether the denoiser runs where ray tracing does, on until it is turned off.
static ENABLED: AtomicBool = AtomicBool::new(true);

/// The settings the next frame is filtered with.
static SETTINGS: Mutex<DenoiserSettings> = Mutex::new(DenoiserSettings {
    history_limit: 32.0,
    sigma_depth: 1.0,
    sigma_normal: 128.0,
    sigma_luminance: 4.0,
});

/// Whether the denoiser's node was added to this app, which only a bridge with Solari running does.
static PRESENT: AtomicBool = AtomicBool::new(false);

/// Turns the denoiser on or off, for a capture of Solari's own picture beside the denoised one.
pub fn set_enabled(on: bool) {
    ENABLED.store(on, Ordering::Relaxed);
}

/// Whether the denoiser is on.
pub fn enabled() -> bool {
    ENABLED.load(Ordering::Relaxed)
}

/// Sets what the denoiser is told from the next frame.
pub fn set_settings(settings: DenoiserSettings) {
    if let Ok(mut known) = SETTINGS.lock() {
        *known = settings;
    }
}

/// What the denoiser is told.
pub fn settings() -> DenoiserSettings {
    SETTINGS.lock().map(|known| *known).unwrap_or_default()
}

/// Whether this app has the node, which a bridge without Solari, or an adapter that cannot trace
/// rays, never does.
pub fn present() -> bool {
    PRESENT.load(Ordering::Relaxed)
}

/// Says the node is not there, for every app a process builds, before Solari is asked for.
pub fn forget() {
    PRESENT.store(false, Ordering::Relaxed);
    ENABLED.store(true, Ordering::Relaxed);
    set_settings(DenoiserSettings::default());
}

/// Turns the denoiser on or off, with `on` 1 or 0, and sets what it is told from the next frame,
/// `settings` where it is given and SVGF's defaults where it is null.
///
/// Every setting is applied on every call, so a call without settings puts SVGF's back. The state
/// is the app's rather than a camera's, as the node's place in the schedule is. Returns
/// [`status::UNSUPPORTED`] where this app has no node, which a bridge without Solari, an app that
/// did not ask for ray-traced lighting and an adapter that cannot trace rays never have, and
/// [`status::NULL_ARG`] for an `on` other than 0 or 1 or settings that are not
/// [`DenoiserSettings::usable`].
///
/// # Safety
/// `settings` must be null or readable for a [`DenoiserSettings`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_ray_traced_denoiser(on: i32, settings: *const DenoiserSettings) -> i32 {
    crate::interop::guard(|| {
        if !present() {
            return status::UNSUPPORTED;
        }

        let settings = if settings.is_null() {
            DenoiserSettings::default()
        } else {
            unsafe { settings.read() }
        };

        if !(0..=1).contains(&on) || !settings.usable() {
            return status::NULL_ARG;
        }

        set_settings(settings);
        set_enabled(on == 1);
        status::OK
    })
}

/// Whether this app has the denoiser's node and it is on, `1` or `0`.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_ray_traced_denoiser_active() -> i32 {
    crate::interop::guard(|| (present() && enabled()) as i32)
}

/// A tap's weight by its depth, normal and luminance differences from the middle's, as the shader's
/// `weight` works it out.
///
/// The depth's difference is measured against the depth's change over the tap's reach, the normal's
/// agreement raised to [`DenoiserSettings::sigma_normal`], and the luminance's difference against
/// the root of the variance, so a tap across an edge, round a corner or past a shadow's edge weighs
/// little and the filter stops there.
pub fn weight(
    settings: &DenoiserSettings,
    depth_difference: f32,
    depth_scale: f32,
    normal_dot: f32,
    luminance_difference: f32,
    luminance_scale: f32,
) -> f32 {
    let by_depth = (-depth_difference.abs() / (settings.sigma_depth * depth_scale + 1e-4)).exp();
    let by_normal = normal_dot.max(0.0).powf(settings.sigma_normal);
    let by_luminance = (-luminance_difference.abs() / (settings.sigma_luminance * luminance_scale + 1e-6)).exp();
    by_depth * by_normal * by_luminance
}

/// Whether a previous frame's surface is the current one, its plane within [`PLANE_DISTANCE`] of
/// the current point at its distance from the camera and its normal within [`NORMAL_THRESHOLD`],
/// as the shader's `agrees` works it out.
pub fn agrees(plane: f32, distance: f32, normal_dot: f32) -> bool {
    plane <= PLANE_DISTANCE * distance && normal_dot >= NORMAL_THRESHOLD
}

/// The four previous texels a pixel's history is read from and their bilinear weights, from Bevy's
/// motion vector, the move across the screen since the previous frame as a fraction of its size,
/// as the shader's temporal stage works them out.
pub fn previous_taps(pixel: [i32; 2], motion: [f32; 2], size: [f32; 2]) -> [([i32; 2], f32); 4] {
    let previous = [pixel[0] as f32 - motion[0] * size[0], pixel[1] as f32 - motion[1] * size[1]];
    let base = [previous[0].floor(), previous[1].floor()];
    let fraction = [previous[0] - base[0], previous[1] - base[1]];

    std::array::from_fn(|i| {
        let corner = [(i & 1) as f32, (i >> 1) as f32];
        let tap = [(base[0] + corner[0]) as i32, (base[1] + corner[1]) as i32];
        let along = |axis: usize| if corner[axis] == 0.0 { 1.0 - fraction[axis] } else { fraction[axis] };
        (tap, along(0) * along(1))
    })
}

/// The history's new value and length after a frame, the new light weighed as one frame of as
/// many as the history holds, up to the clamp, as the shader's temporal stage accumulates both the
/// light and its moments.
pub fn accumulate(history: f32, current: f32, frames: f32, limit: f32) -> (f32, f32) {
    let frames = (frames + 1.0).min(limit);
    let alpha = 1.0 / frames;
    (history + (current - history) * alpha, frames)
}

/// The à-trous kernel's weight at a reach from the middle, the B3 spline's.
pub fn kernel(reach: i32) -> f32 {
    match reach.abs() {
        0 => 0.375,
        1 => 0.25,
        _ => 0.0625,
    }
}

#[cfg(feature = "solari")]
pub use node::install;

#[cfg(feature = "solari")]
mod node;

#[cfg(test)]
mod tests {
    use super::*;

    const CLOSE: f32 = 1e-5;

    #[test]
    fn the_settings_lie_where_the_managed_side_writes_them() {
        assert_eq!(std::mem::size_of::<DenoiserSettings>(), 16);
        assert_eq!(std::mem::offset_of!(DenoiserSettings, sigma_depth), 4);
        assert_eq!(std::mem::offset_of!(DenoiserSettings, sigma_normal), 8);
        assert_eq!(std::mem::offset_of!(DenoiserSettings, sigma_luminance), 12);
    }

    #[test]
    fn a_history_under_a_frame_or_a_width_of_nought_is_refused() {
        assert!(DenoiserSettings::default().usable());
        assert!(!DenoiserSettings { history_limit: 0.5, ..DenoiserSettings::default() }.usable());
        assert!(!DenoiserSettings { sigma_depth: 0.0, ..DenoiserSettings::default() }.usable());
        assert!(!DenoiserSettings { sigma_luminance: f32::NAN, ..DenoiserSettings::default() }.usable());
        assert!(!DenoiserSettings { sigma_normal: f32::INFINITY, ..DenoiserSettings::default() }.usable());
    }

    #[test]
    fn a_bridge_without_the_node_refuses_the_setting_and_reports_it_off() {
        forget();
        assert_eq!(unsafe { bcs_render_set_ray_traced_denoiser(1, std::ptr::null()) }, status::UNSUPPORTED);
        assert_eq!(bcs_render_ray_traced_denoiser_active(), 0);
    }

    #[test]
    fn a_tap_like_the_middle_weighs_one_and_one_round_a_corner_nothing() {
        let settings = DenoiserSettings::default();
        assert!((weight(&settings, 0.0, 1.0, 1.0, 0.0, 1.0) - 1.0).abs() < CLOSE);

        // Square to the middle's normal, or facing away, nothing.
        assert!(weight(&settings, 0.0, 1.0, 0.0, 0.0, 1.0) < CLOSE);
        assert!(weight(&settings, 0.0, 1.0, -0.5, 0.0, 1.0) < CLOSE);

        // Ten degrees off is nearly nothing at SVGF's power of 128.
        let off = 10f32.to_radians().cos();
        assert!(weight(&settings, 0.0, 1.0, off, 0.0, 1.0) < 0.15);
    }

    #[test]
    fn the_depth_and_the_luminance_fall_off_by_their_widths() {
        let settings = DenoiserSettings::default();

        // A depth as far off as its change over the reach weighs one e less, and twice as far two.
        let one = weight(&settings, 0.5, 0.5, 1.0, 0.0, 1.0);
        let two = weight(&settings, 1.0, 0.5, 1.0, 0.0, 1.0);
        assert!((one - (-1.0f32).exp()).abs() < 1e-3);
        assert!((two - (-2.0f32).exp()).abs() < 1e-3);

        // A luminance four deviations off, SVGF's sigma l, weighs one e less, and a wider spread
        // forgives more.
        let lum = weight(&settings, 0.0, 1.0, 1.0, 4.0, 1.0);
        assert!((lum - (-1.0f32).exp()).abs() < 1e-3);
        assert!(weight(&settings, 0.0, 1.0, 1.0, 4.0, 2.0) > lum);

        // Wider settings forgive more.
        let wide = DenoiserSettings { sigma_luminance: 8.0, ..settings };
        assert!(weight(&wide, 0.0, 1.0, 1.0, 4.0, 1.0) > lum);
    }

    #[test]
    fn a_history_is_kept_on_the_same_surface_and_dropped_off_it() {
        // Ten meters away, a plane a centimeter off is the same surface and one twenty off is not.
        assert!(agrees(0.01, 10.0, 1.0));
        assert!(!agrees(0.2, 10.0, 1.0));

        // A normal turned twenty degrees is the same surface, and forty degrees another.
        assert!(agrees(0.0, 10.0, 20f32.to_radians().cos()));
        assert!(!agrees(0.0, 10.0, 40f32.to_radians().cos()));
    }

    #[test]
    fn a_still_pixel_reads_its_own_texel_and_a_moving_one_reads_where_it_was() {
        let size = [1280.0, 720.0];
        let still = previous_taps([100, 50], [0.0, 0.0], size);
        assert_eq!(still[0], ([100, 50], 1.0));
        assert!(still[1..].iter().all(|(_, weight)| *weight == 0.0));

        // Moved a pixel right since the last frame, it was a pixel left.
        let moved = previous_taps([100, 50], [1.0 / 1280.0, 0.0], size);
        let (tap, weight) = moved.iter().copied().fold(([0, 0], 0.0), |best, tap| if tap.1 > best.1 { tap } else { best });
        assert_eq!(tap, [99, 50]);
        assert!((weight - 1.0).abs() < 1e-3);

        // Half a texel each way, four texels a quarter each, and always the whole of one between them.
        let half = previous_taps([100, 50], [0.5 / 1280.0, 0.5 / 720.0], size);
        assert!(half.iter().all(|(_, weight)| (weight - 0.25).abs() < 1e-3));
        assert_eq!(half.iter().map(|(tap, _)| *tap).collect::<Vec<_>>(), vec![[99, 49], [100, 49], [99, 50], [100, 50]]);
        for motion in [[0.3 / 1280.0, -0.7 / 720.0], [-2.25 / 1280.0, 3.5 / 720.0]] {
            let sum: f32 = previous_taps([10, 10], motion, size).iter().map(|(_, weight)| weight).sum();
            assert!((sum - 1.0).abs() < 1e-5);
        }
    }

    #[test]
    fn the_history_takes_the_first_frame_whole_and_then_a_frame_of_up_to_its_limit() {
        // No history, the light as it is.
        assert_eq!(accumulate(0.0, 3.0, 0.0, 32.0), (3.0, 1.0));

        // A second frame is half of each.
        assert_eq!(accumulate(3.0, 1.0, 1.0, 32.0), (2.0, 2.0));

        // At the limit a frame weighs a thirty-second, however long the history.
        let (value, frames) = accumulate(0.0, 32.0, 100.0, 32.0);
        assert_eq!(frames, 32.0);
        assert!((value - 1.0).abs() < CLOSE);
    }

    #[test]
    fn the_kernel_sums_to_one_and_the_steps_double() {
        let sum: f32 = (-2..=2).map(kernel).sum();
        assert!((sum - 1.0).abs() < CLOSE);
        assert!(STEPS.windows(2).all(|pair| pair[1] == pair[0] * 2));
        assert_eq!(STEPS[0], 1);
    }

    #[test]
    fn the_settings_default_to_svgfs() {
        let defaults = DenoiserSettings::default();
        assert_eq!((defaults.history_limit, defaults.sigma_depth, defaults.sigma_normal, defaults.sigma_luminance), (32.0, 1.0, 128.0, 4.0));
    }

    /// The shader parses and validates as it is, every stage an entry point.
    #[cfg(feature = "render")]
    #[test]
    fn the_shader_validates_with_its_five_stages() {
        let module = naga::front::wgsl::parse_str(include_str!("denoiser.wgsl")).expect("the denoiser's shader parses");
        naga::valid::Validator::new(naga::valid::ValidationFlags::all(), naga::valid::Capabilities::default())
            .validate(&module)
            .expect("the denoiser's shader validates");

        let entries: Vec<_> = module.entry_points.iter().map(|entry| entry.name.as_str()).collect();
        assert_eq!(entries, ["demodulate", "temporal", "variance", "atrous", "remodulate"]);
    }
}
