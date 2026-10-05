//! How gizmos are drawn, set from C#: their width, their layers, whether each group is on, their
//! line style and depth bias, and Bevy's own groups for lights and bounding boxes.

use crate::interop::status;
#[cfg(feature = "render")]
use crate::gizmos::FrontGizmos;

/// Hands each group `which` names to `apply`, one at a time.
///
/// `0` the two this bridge draws with, `1` the one the scene can hide, `2` the one it cannot, `3`
/// Bevy's light gizmos, `4` its bounding boxes, and `5` every group the store holds, Bevy's own
/// among them. One at a time because the store hands out a borrow of itself per group. A group
/// whose plugin this build left out is passed over rather than panicking, as asking the store for
/// it by type would.
#[cfg(feature = "render")]
fn for_groups(
    store: &mut bevy::gizmos::config::GizmoConfigStore,
    which: i32,
    mut apply: impl FnMut(&mut bevy::gizmos::config::GizmoConfig),
) {
    use bevy::gizmos::aabb::AabbGizmoConfigGroup;
    use bevy::gizmos::config::DefaultGizmoConfigGroup;
    use bevy::light::gizmos::LightGizmoConfigGroup;

    match which {
        1 => {
            if let Some((config, _)) = store.get_config_mut::<DefaultGizmoConfigGroup>() {
                apply(config);
            }
        }
        2 => {
            if let Some((config, _)) = store.get_config_mut::<FrontGizmos>() {
                apply(config);
            }
        }
        3 => {
            if let Some((config, _)) = store.get_config_mut::<LightGizmoConfigGroup>() {
                apply(config);
            }
        }
        4 => {
            if let Some((config, _)) = store.get_config_mut::<AabbGizmoConfigGroup>() {
                apply(config);
            }
        }
        5 => {
            for (_, config, _) in store.iter_mut() {
                apply(config);
            }
        }
        _ => {
            if let Some((config, _)) = store.get_config_mut::<DefaultGizmoConfigGroup>() {
                apply(config);
            }
            if let Some((config, _)) = store.get_config_mut::<FrontGizmos>() {
                apply(config);
            }
        }
    }
}

/// Sets how gizmos are drawn.
///
/// `width` is the line thickness in pixels, and `layers` is the render layer mask deciding which
/// cameras see gizmos at all; a mask of `0` is Bevy's own default of layer zero. `enabled` at zero
/// stops the drawing without the caller having to stop asking for it, for a debug overlay bound to
/// a key.
///
/// `which` picks the groups, as [`for_groups`] reads it. A shape's `in_front` already chooses
/// between the bridge's two, so setting them apart turns a floor grid off while leaving the
/// handles drawn over it, without either side of the editor knowing about the other.
///
/// Returns [`status::UNSUPPORTED`] where there is nothing to draw on.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_gizmo_configure(width: f32, layers: u32, enabled: i32, which: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (width, layers, enabled, which);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::visibility::RenderLayers;
            use bevy::gizmos::config::GizmoConfigStore;

            crate::state::with_world(|world| {
                let Some(mut store) = world.get_resource_mut::<GizmoConfigStore>() else {
                    return status::UNSUPPORTED;
                };

                // Zero means "whatever Bevy starts with" rather than "no layers at all", because a
                // caller that wanted gizmos on no camera would turn them off instead, and a
                // zeroed struct arriving from the managed side should change nothing.
                let layers = if layers == 0 {
                    RenderLayers::default()
                } else {
                    crate::render::scene::layers_from(layers).unwrap_or_default()
                };

                let apply = |config: &mut bevy::gizmos::config::GizmoConfig| {
                    // A width of zero is a caller leaving it alone rather than asking for lines
                    // with no thickness, which would draw nothing and read as a broken bridge.
                    if width > 0.0 {
                        config.line.width = width;
                    }

                    config.render_layers = layers.clone();
                    config.enabled = enabled != 0;
                };

                for_groups(&mut store, which, apply);
                status::OK
            })
        }
    })
}

/// Sets what a gizmo line looks like, for the groups `which` names as [`for_groups`] reads it.
///
/// `style` is `0` solid, `1` dotted and `2` dashed, where `gap_scale` and `line_scale` are the
/// lengths of the gap and of the visible run, both measured in line widths. `joint` is `0` none,
/// `1` mitred, `2` round and `3` bevelled, and `joint_resolution` is how many triangles a round
/// joint is drawn with. `perspective` at non-zero makes the width a size at the near plane rather
/// than a size on screen, so a line further away is drawn thinner, which only a 3D camera with a
/// perspective projection can honor.
///
/// Separate from [`bcs_gizmo_configure`] because how thick a line is and who can see it is one
/// decision and what the line looks like is another, and most callers set the first.
///
/// Returns [`status::UNSUPPORTED`] where there is nothing to draw on.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_gizmo_style(
    style: i32,
    gap_scale: f32,
    line_scale: f32,
    joint: i32,
    joint_resolution: u32,
    perspective: i32,
    which: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (style, gap_scale, line_scale, joint, joint_resolution, perspective, which);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::gizmos::config::{GizmoConfigStore, GizmoLineJoint, GizmoLineStyle};

            crate::state::with_world(|world| {
                let Some(mut store) = world.get_resource_mut::<GizmoConfigStore>() else {
                    return status::UNSUPPORTED;
                };

                // Bevy's own defaults where a caller passed nothing, so a zeroed request asks for
                // the dashes it would have drawn rather than for a line of zero-length dashes,
                // which is a line that draws nothing.
                let style = match style {
                    1 => GizmoLineStyle::Dotted,
                    2 => GizmoLineStyle::Dashed {
                        gap_scale: if gap_scale > 0.0 { gap_scale } else { 1.0 },
                        line_scale: if line_scale > 0.0 { line_scale } else { 3.0 },
                    },
                    _ => GizmoLineStyle::Solid,
                };

                let joint = match joint {
                    1 => GizmoLineJoint::Miter,
                    2 => GizmoLineJoint::Round(if joint_resolution > 0 {
                        joint_resolution
                    } else {
                        4
                    }),
                    3 => GizmoLineJoint::Bevel,
                    _ => GizmoLineJoint::None,
                };

                let apply = |config: &mut bevy::gizmos::config::GizmoConfig| {
                    config.line.style = style;
                    config.line.joints = joint;
                    config.line.perspective = perspective != 0;
                };

                for_groups(&mut store, which, apply);
                status::OK
            })
        }
    })
}

/// Moves the gizmos of the groups `which` names toward the camera or away from it before they are
/// depth tested, from `-1`, in front of everything, through `0`, where they are, to `1`, behind
/// everything. Bevy clamps it to that range.
///
/// What draws a group over the scene or into it after the fact, which a debug view bound to a key
/// toggles. The bridge's in-front group starts at `-1` and every other at `0`.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_gizmo_depth_bias(bias: f32, which: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (bias, which);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| {
                let Some(mut store) = world.get_resource_mut::<bevy::gizmos::config::GizmoConfigStore>() else {
                    return status::UNSUPPORTED;
                };

                for_groups(&mut store, which, |config| config.depth_bias = bias.clamp(-1.0, 1.0));
                status::OK
            })
        }
    })
}

/// Sets how Bevy draws the shapes of lights, whether every light is drawn rather than only those
/// carrying `ShowLightGizmo`, and how they are colored, `0` all in `color`, `1` a color for each
/// entity, `2` each in its light's own color, and `3` a color for each kind of light.
///
/// `color` is linear RGBA, read only by the first mode, and may be null for the others.
///
/// # Safety
/// `color` must be null or point to four readable floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_gizmo_lights(draw_all: i32, mode: i32, color: *const f32) -> i32 {
    crate::interop::guard(|| {
        let color = (!color.is_null()).then(|| unsafe { core::slice::from_raw_parts(color, 4) }.to_vec());

        #[cfg(not(feature = "render"))]
        {
            let _ = (draw_all, mode, color);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::color::Color;
            use bevy::light::gizmos::{LightGizmoColor, LightGizmoConfigGroup};

            crate::state::with_world(|world| {
                let Some(mut store) = world.get_resource_mut::<bevy::gizmos::config::GizmoConfigStore>() else {
                    return status::UNSUPPORTED;
                };
                let Some((_, lights)) = store.get_config_mut::<LightGizmoConfigGroup>() else {
                    return status::UNSUPPORTED;
                };

                lights.draw_all = draw_all != 0;
                lights.color = match mode {
                    0 => {
                        let Some(color) = &color else {
                            return status::NULL_ARG;
                        };
                        LightGizmoColor::Manual(Color::linear_rgba(color[0], color[1], color[2], color[3]))
                    }
                    1 => LightGizmoColor::Varied,
                    3 => LightGizmoColor::ByLightType,
                    _ => LightGizmoColor::MatchLightColor,
                };
                status::OK
            })
        }
    })
}

/// Sets whether Bevy draws every entity's bounding box rather than only those carrying
/// `ShowAabbGizmo`, and in what color, linear RGBA, or a color for each box where `color` is
/// null.
///
/// # Safety
/// `color` must be null or point to four readable floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_gizmo_bounds(draw_all: i32, color: *const f32) -> i32 {
    crate::interop::guard(|| {
        let color = (!color.is_null()).then(|| unsafe { core::slice::from_raw_parts(color, 4) }.to_vec());

        #[cfg(not(feature = "render"))]
        {
            let _ = (draw_all, color);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::color::Color;
            use bevy::gizmos::aabb::AabbGizmoConfigGroup;

            crate::state::with_world(|world| {
                let Some(mut store) = world.get_resource_mut::<bevy::gizmos::config::GizmoConfigStore>() else {
                    return status::UNSUPPORTED;
                };
                let Some((_, bounds)) = store.get_config_mut::<AabbGizmoConfigGroup>() else {
                    return status::UNSUPPORTED;
                };

                bounds.draw_all = draw_all != 0;
                bounds.default_color = color.map(|c| Color::linear_rgba(c[0], c[1], c[2], c[3]));
                status::OK
            })
        }
    })
}
