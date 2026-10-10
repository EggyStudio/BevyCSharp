//! The images, dispatches, draws and passes a camera runs every frame, each over a shader instance
//! [`super::shaders`] makes, copied onto the camera where the render world takes them from.

use crate::interop::status;
#[cfg(feature = "render")]
use super::shaders::ShaderInstances;

/// One image a camera owns, as the managed side describes it.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsViewImage {
    /// NUL-terminated UTF-8.
    pub name: *const core::ffi::c_char,
    /// An index into [`super::compute::IMAGE_FORMATS`].
    pub format: i32,
    /// A fraction of the picture's size.
    pub scale: f32,
    /// Non-zero to keep last frame's as well, under the name with `_previous` after it.
    pub history: i32,
    /// How many mip levels, at least one.
    pub mips: i32,
    /// The frame point at which the picture is copied in, `1` to `3` as for a dispatch, or `-1`
    /// for none. Only into a format a draw can write numbers to, which is a float or eight-bit one.
    pub copy: i32,
    /// Non-zero to clear it to zero at the start of every frame.
    pub clear: i32,
}

/// Whether the picture can be copied into an image of `format`, which a draw writing floats does.
#[cfg(feature = "render")]
fn copy_target(format: bevy::render::render_resource::TextureFormat) -> bool {
    use bevy::render::render_resource::TextureFormat;

    matches!(
        format,
        TextureFormat::Rgba8Unorm
            | TextureFormat::Rgba16Float
            | TextureFormat::Rgba32Float
            | TextureFormat::R32Float
            | TextureFormat::Rg32Float
            | TextureFormat::R16Float
    )
}

/// One dispatch a camera runs every frame, as the managed side describes it.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsViewDispatch {
    pub instance: i32,
    /// `0` after the prepass, `1` after opaque geometry, `2` before tonemapping, `3` after it.
    pub point: i32,
    /// `0` counted from the picture's size, `1` fixed, `2` read from a buffer.
    pub mode: i32,
    /// The workgroup's size in pixels for `0`, or the workgroups themselves for `1`.
    pub groups: [u32; 3],
    /// For `0`, the fraction of the picture covered.
    pub scale: f32,
    /// For `2`, the buffer holding the counts and the byte offset into it.
    pub buffer: i32,
    pub offset: u32,
}

/// Gives a camera the images its shaders keep, replacing any it had. A count of zero takes them
/// all away.
///
/// # Safety
/// `images` must point at `count` readable [`BcsViewImage`]s whose names are NUL-terminated, or
/// be null when `count` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_view_images(
    camera: u64,
    images: *const BcsViewImage,
    count: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, images, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::views::{BcsViewImages, ViewImageSpec};

            if count < 0 || (images.is_null() && count > 0) {
                return status::NULL_ARG;
            }

            let given: &[BcsViewImage] = if count > 0 {
                unsafe { core::slice::from_raw_parts(images, count as usize) }
            } else {
                &[]
            };

            let mut specs = Vec::with_capacity(given.len());

            for image in given {
                let Some(name) = (unsafe { crate::interop::cstr_to_string(image.name) }) else {
                    return status::NULL_ARG;
                };

                let Some(&(format, _)) = usize::try_from(image.format)
                    .ok()
                    .and_then(|index| super::compute::IMAGE_FORMATS.get(index))
                else {
                    return status::NULL_ARG;
                };

                // A camera's images are written by shaders, which a compressed format cannot be.
                if name.is_empty() || !(image.scale > 0.0) || image.scale > 16.0 || format.is_compressed() {
                    return status::NULL_ARG;
                }

                // Inside the prepass and at the point after it there is no picture yet to copy, and
                // an integer image cannot hold one, so both are refused rather than copied as zeros.
                let copy = match image.copy {
                    -1 => None,
                    1..=3 => super::views::FramePoint::from_number(image.copy),
                    _ => return status::NULL_ARG,
                };

                if copy.is_some() && !copy_target(format) {
                    return status::NULL_ARG;
                }

                specs.push(ViewImageSpec {
                    name,
                    format,
                    scale: image.scale,
                    history: image.history != 0,
                    mips: image.mips.max(1) as u32,
                    copy,
                    clear: image.clear != 0,
                });
            }

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            crate::state::with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let mut camera = world.entity_mut(entity);

                if specs.is_empty() {
                    camera.remove::<BcsViewImages>();
                } else {
                    camera.insert(BcsViewImages(specs));
                }

                status::OK
            })
        }
    })
}

/// Replaces the dispatches a camera runs every frame with `count` of them, in order. A count of
/// zero takes them all away.
///
/// # Safety
/// `dispatches` must point at `count` readable [`BcsViewDispatch`]es, or be null when `count` is
/// zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_view_dispatches(
    camera: u64,
    dispatches: *const BcsViewDispatch,
    count: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, dispatches, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::views::{FramePoint, Workgroups};

            if count < 0 || (dispatches.is_null() && count > 0) {
                return status::NULL_ARG;
            }

            let given: &[BcsViewDispatch] = if count > 0 {
                unsafe { core::slice::from_raw_parts(dispatches, count as usize) }
            } else {
                &[]
            };

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            crate::state::with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let known = world
                    .get_resource::<ShaderInstances>()
                    .map(|instances| instances.0.len())
                    .unwrap_or(0);

                let mut list = Vec::with_capacity(given.len());

                for dispatch in given {
                    if dispatch.instance < 0 || dispatch.instance as usize >= known {
                        return status::NO_COMPONENT;
                    }

                    let Some(point) = FramePoint::from_number(dispatch.point) else {
                        return status::NULL_ARG;
                    };

                    let workgroups = match dispatch.mode {
                        0 => Workgroups::PerPixel {
                            size: [dispatch.groups[0].max(1), dispatch.groups[1].max(1)],
                            scale: if dispatch.scale > 0.0 { dispatch.scale } else { 1.0 },
                        },
                        1 => Workgroups::Fixed(dispatch.groups.map(|count| count.max(1))),
                        2 => {
                            if dispatch.offset % 4 != 0 {
                                return status::NULL_ARG;
                            }

                            let Some(buffer) = super::compute::buffer_handle(world, dispatch.buffer)
                            else {
                                return status::NO_COMPONENT;
                            };

                            Workgroups::Indirect {
                                buffer,
                                offset: dispatch.offset as u64,
                            }
                        }
                        _ => return status::NULL_ARG,
                    };

                    list.push((dispatch.instance as usize, point, workgroups));
                }

                let mut camera = world.entity_mut(entity);

                if list.is_empty() {
                    camera.remove::<(DispatchInstances, super::views::BcsViewDispatches)>();
                } else {
                    camera.insert(DispatchInstances(list));
                }

                status::OK
            })
        }
    })
}

/// One draw a camera makes every frame, as the managed side describes it.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsViewDraw {
    pub instance: i32,
    /// As [`BcsViewDispatch::point`].
    pub point: i32,
    /// `0` a fixed count, `1` counts read from a buffer, `2` workgroups of mesh shaders.
    pub mode: i32,
    pub vertices: u32,
    pub instances: u32,
    /// For `1`, the buffer holding the counts and the byte offset into it.
    pub buffer: i32,
    pub offset: u32,
    /// `0` opaque, `1` alpha blended, `2` added.
    pub blend: i32,
    /// `1` to write depth as well as test against it, and `2` to be drawn into the camera's
    /// directional shadow maps as well, so it casts shadows.
    pub depth_write: i32,
    /// NUL-terminated UTF-8 naming the camera's images to draw into, one to a line in the order of
    /// the fragment shader's outputs, or null for the picture.
    pub target: *const core::ffi::c_char,
    /// For `2`, how many workgroups of a program drawing with mesh shaders run, across, down and
    /// deep.
    pub groups: [u32; 3],
}

/// Replaces the draws a camera makes every frame with `count` of them, in order. A count of zero
/// takes them all away.
///
/// # Safety
/// `draws` must point at `count` readable [`BcsViewDraw`]s, or be null when `count` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_view_draws(
    camera: u64,
    draws: *const BcsViewDraw,
    count: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, draws, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::views::{DrawBlend, DrawCount, FramePoint};

            if count < 0 || (draws.is_null() && count > 0) {
                return status::NULL_ARG;
            }

            let given: &[BcsViewDraw] = if count > 0 {
                unsafe { core::slice::from_raw_parts(draws, count as usize) }
            } else {
                &[]
            };

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            crate::state::with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let known = world
                    .get_resource::<ShaderInstances>()
                    .map(|instances| instances.0.len())
                    .unwrap_or(0);

                let mut list = Vec::with_capacity(given.len());

                for draw in given {
                    if draw.instance < 0 || draw.instance as usize >= known {
                        return status::NO_COMPONENT;
                    }

                    let Some(point) = FramePoint::from_number(draw.point) else {
                        return status::NULL_ARG;
                    };

                    let count = match draw.mode {
                        0 => DrawCount::Fixed {
                            vertices: draw.vertices,
                            instances: draw.instances.max(1),
                        },
                        1 => {
                            if draw.offset % 4 != 0 {
                                return status::NULL_ARG;
                            }

                            let Some(buffer) = super::compute::buffer_handle(world, draw.buffer)
                            else {
                                return status::NO_COMPONENT;
                            };

                            DrawCount::Indirect {
                                buffer,
                                offset: draw.offset as u64,
                            }
                        }
                        2 => DrawCount::Groups {
                            x: draw.groups[0],
                            y: draw.groups[1],
                            z: draw.groups[2],
                        },
                        _ => return status::NULL_ARG,
                    };

                    let blend = match draw.blend {
                        1 => DrawBlend::Alpha,
                        2 => DrawBlend::Add,
                        _ => DrawBlend::Opaque,
                    };

                    // Several names are one to a line, in the order of the fragment shader's
                    // outputs.
                    let target: Vec<String> = if draw.target.is_null() {
                        Vec::new()
                    } else {
                        match unsafe { crate::interop::cstr_to_string(draw.target) } {
                            Some(names) if !names.trim().is_empty() => {
                                names.lines().map(str::trim).filter(|name| !name.is_empty()).map(String::from).collect()
                            }
                            _ => return status::NULL_ARG,
                        }
                    };

                    list.push((
                        draw.instance as usize,
                        point,
                        count,
                        blend,
                        (draw.depth_write & 1 != 0, draw.depth_write & 2 != 0),
                        target,
                    ));
                }

                let mut camera = world.entity_mut(entity);

                if list.is_empty() {
                    camera.remove::<(DrawInstances, super::views::BcsViewDraws)>();
                } else {
                    camera.insert(DrawInstances(list));
                }

                status::OK
            })
        }
    })
}

/// Which instances a camera draws every frame, where in its frame, how many vertices, and how.
#[cfg(feature = "render")]
#[derive(bevy::ecs::component::Component, Clone)]
#[allow(clippy::type_complexity)]
pub struct DrawInstances(
    pub  Vec<(
        usize,
        super::views::FramePoint,
        super::views::DrawCount,
        super::views::DrawBlend,
        (bool, bool),
        Vec<String>,
    )>,
);

/// Copies what each camera's draw instances hold onto the camera every frame, where the render
/// world takes it from.
#[cfg(feature = "render")]
pub fn sync_view_draws(
    mut commands: bevy::ecs::system::Commands,
    instances: Option<bevy::ecs::system::Res<ShaderInstances>>,
    cameras: bevy::ecs::system::Query<(bevy::ecs::entity::Entity, &DrawInstances)>,
) {
    use super::views::{BcsViewDraws, ViewDraw};

    let Some(instances) = instances else {
        return;
    };

    for (entity, wanted) in &cameras {
        let draws = wanted
            .0
            .iter()
            .filter_map(|(id, point, count, blend, (depth_write, casts_shadows), target)| {
                let instance = instances.0.get(*id)?;
                Some(ViewDraw {
                    program: instance.program,
                    values: instance.values.clone(),
                    point: *point,
                    count: count.clone(),
                    blend: *blend,
                    depth_write: *depth_write,
                    casts_shadows: *casts_shadows,
                    targets: target.clone(),
                })
            })
            .collect();

        commands.entity(entity).insert(BcsViewDraws(draws));
    }
}

/// Starts watching one of a camera's images. Every frame, once the camera's frame is done, it is
/// drawn into an eight-bit image of `width` by `height`, each value times `scale` plus `offset`.
/// Answers that image's asset key, which anything that draws images can show.
///
/// The name is any a shader on the camera reads an image by, or `depth`, `normals` or `motion`
/// for the prepass's. See [`super::watch`].
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_watch_view_image(
    camera: u64,
    name: *const core::ffi::c_char,
    width: u32,
    height: u32,
    scale: f32,
    offset: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, name, width, height, scale, offset);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
                return status::NULL_ARG;
            };

            if name.is_empty() || width == 0 || height == 0 {
                return status::NULL_ARG;
            }

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            crate::state::with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                super::watch::watch(world, entity, name, width, height, scale, offset)
            })
        }
    })
}

/// Copies out the names a watch on the camera would find, as of the last frame drawn, one a line,
/// by the text convention. What an inspector lists as watchable, since the names a camera has
/// depend on settings made in several places and only the render world knows the answer.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_drawn_view_image_names(camera: u64, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let text = super::watch::drawn_names(camera);
            unsafe { crate::interop::write_text(&text, out, capacity) }
        }
    })
}

/// Stops watching one of a camera's images.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_unwatch_view_image(camera: u64, name: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, name);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
                return status::NULL_ARG;
            };

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            crate::state::with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                super::watch::unwatch(world, entity, &name);
                status::OK
            })
        }
    })
}

/// Which instances a camera dispatches every frame, where in its frame, and how many workgroups.
#[cfg(feature = "render")]
#[derive(bevy::ecs::component::Component, Clone)]
pub struct DispatchInstances(
    pub Vec<(usize, super::views::FramePoint, super::views::Workgroups)>,
);

/// Copies what each camera's dispatch instances hold onto the camera every frame, where the render
/// world takes it from.
///
/// Every frame rather than when something changed, because a dispatch on a camera is rebuilt every
/// frame anyway, since the images it writes trade places.
#[cfg(feature = "render")]
pub fn sync_view_dispatches(
    mut commands: bevy::ecs::system::Commands,
    instances: Option<bevy::ecs::system::Res<ShaderInstances>>,
    cameras: bevy::ecs::system::Query<(bevy::ecs::entity::Entity, &DispatchInstances)>,
) {
    use super::views::{BcsViewDispatches, ViewDispatch};

    let Some(instances) = instances else {
        return;
    };

    for (entity, wanted) in &cameras {
        let dispatches = wanted
            .0
            .iter()
            .filter_map(|(id, point, workgroups)| {
                let instance = instances.0.get(*id)?;
                Some(ViewDispatch {
                    program: instance.program,
                    values: instance.values.clone(),
                    point: *point,
                    workgroups: workgroups.clone(),
                })
            })
            .collect();

        commands.entity(entity).insert(BcsViewDispatches(dispatches));
    }
}

/// Replaces the passes a camera runs over its picture with `count` instances, in order.
///
/// `after_tonemapping` holds a place per instance: `0` on the linear picture before tonemapping,
/// `1` on the picture as the screen will show it, and `2` on the lit opaque geometry before
/// transparent geometry is drawn. A count of zero takes every pass off.
///
/// # Safety
/// `instances` and `after_tonemapping` must each point at `count` readable integers, or be null
/// when `count` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_shader_passes(
    camera: u64,
    instances: *const i32,
    after_tonemapping: *const i32,
    count: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, instances, after_tonemapping, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if count < 0 || ((instances.is_null() || after_tonemapping.is_null()) && count > 0) {
                return status::NULL_ARG;
            }

            let (ids, afters): (&[i32], &[i32]) = if count > 0 {
                unsafe {
                    (
                        core::slice::from_raw_parts(instances, count as usize),
                        core::slice::from_raw_parts(after_tonemapping, count as usize),
                    )
                }
            } else {
                (&[], &[])
            };

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            crate::state::with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let known = world
                    .get_resource::<ShaderInstances>()
                    .map(|instances| instances.0.len())
                    .unwrap_or(0);

                if ids.iter().any(|id| *id < 0 || *id as usize >= known) {
                    return status::NO_COMPONENT;
                }

                let mut list = Vec::with_capacity(ids.len());

                for (id, place) in ids.iter().zip(afters) {
                    let Some(place) = super::passes::PassPlace::from_number(*place) else {
                        return status::NULL_ARG;
                    };

                    list.push((*id as usize, place));
                }

                let mut camera = world.entity_mut(entity);

                if list.is_empty() {
                    camera.remove::<(PassInstances, super::passes::BcsShaderPasses)>();
                } else {
                    camera.insert(PassInstances(list));
                }

                status::OK
            })
        }
    })
}

/// Which instances a camera runs as passes, and where in its frame.
#[cfg(feature = "render")]
#[derive(bevy::ecs::component::Component, Clone)]
pub struct PassInstances(pub Vec<(usize, super::passes::PassPlace)>);

/// Copies what each camera's pass instances hold onto the camera, where the render world takes it
/// from, whenever an instance has changed.
#[cfg(feature = "render")]
pub fn sync_passes(
    mut commands: bevy::ecs::system::Commands,
    instances: Option<bevy::ecs::system::Res<ShaderInstances>>,
    cameras: bevy::ecs::system::Query<(
        bevy::ecs::entity::Entity,
        &PassInstances,
        Option<&super::passes::BcsShaderPasses>,
    )>,
) {
    use super::passes::{BcsShaderPasses, ShaderPass};

    let Some(instances) = instances else {
        return;
    };

    for (entity, wanted, current) in &cameras {
        let passes: Vec<ShaderPass> = wanted
            .0
            .iter()
            .filter_map(|(id, place)| {
                let instance = instances.0.get(*id)?;
                Some(ShaderPass {
                    program: instance.program,
                    values: instance.values.clone(),
                    version: instance.version,
                    place: *place,
                })
            })
            .collect();

        let same = current.is_some_and(|current| {
            current.passes.len() == passes.len()
                && current.passes.iter().zip(&passes).all(|(a, b)| {
                    a.program == b.program
                        && a.version == b.version
                        && a.place == b.place
                })
        });

        if !same {
            commands.entity(entity).insert(BcsShaderPasses { passes });
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use core::mem::{offset_of, size_of};

    #[test]
    fn the_view_configs_have_the_layout_the_managed_side_mirrors() {
        assert_eq!(offset_of!(BcsViewImage, format), 8);
        assert_eq!(offset_of!(BcsViewImage, mips), 20);
        assert_eq!(offset_of!(BcsViewImage, copy), 24);
        assert_eq!(offset_of!(BcsViewImage, clear), 28);
        assert_eq!(size_of::<BcsViewImage>(), 32);
        assert_eq!(offset_of!(BcsViewDispatch, groups), 12);
        assert_eq!(offset_of!(BcsViewDispatch, scale), 24);
        assert_eq!(offset_of!(BcsViewDispatch, offset), 32);
        assert_eq!(size_of::<BcsViewDispatch>(), 36);
    }

    #[test]
    fn the_view_draw_has_the_layout_the_managed_side_mirrors() {
        assert_eq!(offset_of!(BcsViewDraw, vertices), 12);
        assert_eq!(offset_of!(BcsViewDraw, offset), 24);
        assert_eq!(offset_of!(BcsViewDraw, depth_write), 32);
        assert_eq!(offset_of!(BcsViewDraw, target), 40);
        assert_eq!(offset_of!(BcsViewDraw, groups), 48);
        assert_eq!(size_of::<BcsViewDraw>(), 64);
    }
}
