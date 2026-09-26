//! Light probes: boxes in the scene that light what is inside them from an image rather than from
//! a lamp.
//!
//! Bevy has two kinds. A reflection probe is the pair of cubemaps a camera can be lit by, placed
//! in a room so the room's surfaces reflect the room rather than the sky outside. An irradiance
//! volume is a 3D image holding, for a grid of points across its box, the light a surface facing
//! each of the six axis directions receives there, which is diffuse indirect light that varies
//! through space.
//!
//! The volume is the one a global illumination package cares about. It is an ordinary 3D image,
//! so a compute shader can write it every frame, and Bevy's own materials then read it as their
//! indirect diffuse light, ranked above a reflection probe and the camera's environment map. That
//! is a way into Bevy's lighting that needs no change to Bevy: a world-space technique answers
//! into a grid, and everything drawn with a standard material is lit by the answer. `bcs_scene`
//! carries the layout, so a shader addresses the image by voxel and direction rather than by the
//! packing.
//!
//! Both are components on an entity whose transform places, turns and sizes the box, which is a
//! unit cube before its scale. A probe on a camera is refused, because a camera is lit by
//! `bcs_render_set_environment_map` and the same components there mean something else.

use crate::interop::status;
#[cfg(feature = "render")]
use crate::state::with_world;

/// Reads the falloff a caller passed, three floats or null for none.
///
/// # Safety
/// `falloff` must be null or point to three readable floats.
#[cfg(feature = "render")]
unsafe fn falloff_of(falloff: *const f32) -> bevy::math::Vec3 {
    if falloff.is_null() {
        return bevy::math::Vec3::ZERO;
    }

    // SAFETY: the caller promised three readable floats.
    let parts = unsafe { core::slice::from_raw_parts(falloff, 3) };
    bevy::math::Vec3::new(parts[0], parts[1], parts[2]).clamp(bevy::math::Vec3::ZERO, bevy::math::Vec3::ONE)
}

/// Refuses an entity that is gone or is a camera.
#[cfg(feature = "render")]
fn refuse_probe(world: &bevy::ecs::world::World, entity: bevy::ecs::entity::Entity) -> Option<i32> {
    let Ok(entity_ref) = world.get_entity(entity) else {
        return Some(status::NO_ENTITY);
    };

    if entity_ref.contains::<bevy::camera::Camera>() {
        return Some(status::INVALID_STATE);
    }

    None
}

/// Takes the probe marker off once neither kind of light is left on the entity, since a probe
/// with nothing to give still takes a slot in every view it is near.
#[cfg(feature = "render")]
fn drop_probe_if_empty(world: &mut bevy::ecs::world::World, entity: bevy::ecs::entity::Entity) {
    use bevy::light::{EnvironmentMapLight, IrradianceVolume, LightProbe};

    let mut probe = world.entity_mut(entity);

    if !probe.contains::<EnvironmentMapLight>() && !probe.contains::<IrradianceVolume>() {
        probe.remove::<LightProbe>();
    }
}

/// Makes an entity a reflection probe lit by a pair of baked cubemaps, or takes it off.
///
/// The entity's transform is the box, a unit cube before its scale, and everything drawn inside it
/// reflects the pair instead of the camera's environment. Both images are a column of six square
/// faces like a camera's baked pair, and the light waits for both to become cubes, which is what
/// [`crate::render::post::reinterpret_cubemaps`] does on the frame their pixels arrive.
///
/// `falloff` is three floats between zero and one, or null for a hard edge: how much of the box,
/// on each axis, the probe's influence fades across, so a room with several probes blends from one
/// to the next. A negative for either image takes the reflection off.
///
/// Returns [`status::INVALID_STATE`] for a camera, and [`status::NO_COMPONENT`] where a key names
/// no image.
///
/// # Safety
/// `falloff` must be null or point to three readable floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_reflection_probe(
    entity: u64,
    diffuse: i32,
    specular: i32,
    intensity: f32,
    falloff: *const f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, diffuse, specular, intensity, falloff);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::light::{EnvironmentMapLight, LightProbe};

            use super::post::{PendingCubemaps, PendingEnvironment, PendingEnvironments};

            let entity = Entity::from_bits(entity);
            // SAFETY: forwarded from the caller's promise.
            let falloff = unsafe { falloff_of(falloff) };

            with_world(|world| {
                if let Some(refusal) = refuse_probe(world, entity) {
                    return refusal;
                }

                let diffuse = match super::image_handle(world, diffuse) {
                    Err(refusal) => return refusal,
                    Ok(handle) => handle,
                };

                let specular = match super::image_handle(world, specular) {
                    Err(refusal) => return refusal,
                    Ok(handle) => handle,
                };

                // A pair still waiting to become cubes would otherwise land after this and undo
                // it, so whatever was asked for this entity before is forgotten either way.
                world
                    .get_resource_or_init::<PendingEnvironments>()
                    .0
                    .retain(|waiting| waiting.target != entity);

                let (Some(diffuse), Some(specular)) = (diffuse, specular) else {
                    world.entity_mut(entity).remove::<EnvironmentMapLight>();
                    drop_probe_if_empty(world, entity);
                    return status::OK;
                };

                {
                    let mut cubemaps = world.get_resource_or_init::<PendingCubemaps>();
                    cubemaps.push(diffuse.clone());
                    cubemaps.push(specular.clone());
                }

                world
                    .get_resource_or_init::<PendingEnvironments>()
                    .0
                    .push(PendingEnvironment {
                        target: entity,
                        diffuse,
                        specular,
                        intensity,
                        rotation: bevy::math::Quat::IDENTITY,
                    });

                // The marker goes on now, with nothing to give until the light arrives, so the
                // box is in place and its falloff set however long the images take.
                world.entity_mut(entity).insert(LightProbe { falloff });
                status::OK
            })
        }
    })
}

/// Makes an entity an irradiance volume lit from a 3D image, or takes it off.
///
/// The image holds a grid of `x` by `y` by `z` points as a texture `x` wide, `2y` high and `3z`
/// deep: the light a surface facing each axis direction receives at each point, in a layout
/// `bcs_scene::irradiance_texel` addresses. Bevy samples it filtered, so its format has to be one
/// that filters, which among the formats a compute shader writes is `Rgba16Float`. It can come
/// from a file or be made by `bcs_shader_image_create` and written every frame.
///
/// `intensity` scales what the image holds into candelas per square meter. `falloff` is as for a
/// reflection probe. A negative `voxels` takes the volume off.
///
/// Returns [`status::INVALID_STATE`] for a camera or for an image that has loaded and is not 3D,
/// since Bevy would bind it where a 3D texture goes and the frame would fail rather than the call,
/// and [`status::NO_COMPONENT`] where the key names no image.
///
/// # Safety
/// `falloff` must be null or point to three readable floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_irradiance_volume(
    entity: u64,
    voxels: i32,
    intensity: f32,
    falloff: *const f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, voxels, intensity, falloff);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::light::{IrradianceVolume, LightProbe};

            let entity = Entity::from_bits(entity);
            // SAFETY: forwarded from the caller's promise.
            let falloff = unsafe { falloff_of(falloff) };

            with_world(|world| {
                if let Some(refusal) = refuse_probe(world, entity) {
                    return refusal;
                }

                let voxels = match super::image_handle(world, voxels) {
                    Err(refusal) => return refusal,
                    Ok(handle) => handle,
                };

                let Some(voxels) = voxels else {
                    world.entity_mut(entity).remove::<IrradianceVolume>();
                    drop_probe_if_empty(world, entity);
                    return status::OK;
                };

                if !may_be_a_volume(world, &voxels) {
                    return status::INVALID_STATE;
                }

                world.entity_mut(entity).insert((
                    LightProbe { falloff },
                    IrradianceVolume {
                        voxels,
                        intensity,
                        ..Default::default()
                    },
                ));

                status::OK
            })
        }
    })
}

/// Whether an image can be an irradiance volume, as far as can be told yet.
///
/// One still loading is given the benefit of the doubt, and [`drop_flat_volumes`] takes it off
/// again if it arrives flat.
#[cfg(feature = "render")]
fn may_be_a_volume(world: &bevy::ecs::world::World, image: &bevy::asset::Handle<bevy::image::Image>) -> bool {
    world
        .resource::<bevy::asset::Assets<bevy::image::Image>>()
        .get(image)
        .is_none_or(|image| image.texture_descriptor.dimension == bevy::render::render_resource::TextureDimension::D3)
}

/// Takes an irradiance volume off an entity whose image turned out not to be 3D.
///
/// A file named as a volume is only known to be one once it has decoded, and a flat one would be
/// bound where Bevy's lighting reads a 3D texture, which is a validation failure every frame
/// rather than one message. Said once per entity, and the volume is gone, so it is not said again.
#[cfg(feature = "render")]
pub fn drop_flat_volumes(
    mut commands: bevy::ecs::system::Commands,
    volumes: bevy::ecs::system::Query<(bevy::ecs::entity::Entity, &bevy::light::IrradianceVolume)>,
    images: bevy::ecs::system::Res<bevy::asset::Assets<bevy::image::Image>>,
) {
    use bevy::render::render_resource::TextureDimension;

    for (entity, volume) in &volumes {
        let Some(image) = images.get(&volume.voxels) else { continue };

        if image.texture_descriptor.dimension != TextureDimension::D3 {
            bevy::log::warn!(
                "An irradiance volume's image is not a 3D texture, so the volume was taken off \
                 {entity}. It needs to be as wide as the grid, twice as high and three times as deep."
            );

            commands.entity(entity).remove::<bevy::light::IrradianceVolume>();
        }
    }
}

/// Which way each face camera of a captured probe looks, and which way is up for it, in the order
/// a cube's layers are in.
///
/// Bevy samples a cube with Z negated, since cube maps are left-handed and its world is not, so the
/// last two layers look down world minus Z and then plus Z. The ups are the cube map convention's,
/// which puts +Y's top toward +Z and -Y's toward -Z.
#[cfg(feature = "render")]
const FACES: [(bevy::math::Vec3, bevy::math::Vec3); 6] = [
    (bevy::math::Vec3::X, bevy::math::Vec3::Y),
    (bevy::math::Vec3::NEG_X, bevy::math::Vec3::Y),
    (bevy::math::Vec3::Y, bevy::math::Vec3::Z),
    (bevy::math::Vec3::NEG_Y, bevy::math::Vec3::NEG_Z),
    (bevy::math::Vec3::NEG_Z, bevy::math::Vec3::Y),
    (bevy::math::Vec3::Z, bevy::math::Vec3::Y),
];

/// How many frames a probe captured once keeps its cameras on, counted from when nothing is left
/// compiling.
///
/// More than one, because the first frame a camera draws into a new image can go before the image
/// is on the GPU, and the probe's own light arrives a frame after its faces do.
#[cfg(feature = "render")]
const CAPTURE_FRAMES: u32 = 4;

/// Whether the render world had pipelines still compiling at the end of its last frame.
///
/// A capture asked for at startup is otherwise taken of a scene whose materials have not compiled
/// yet, which draws nothing, and a probe captured once would keep that empty room. Starts true,
/// since before the render world has run a frame nothing is known to be ready.
#[cfg(feature = "render")]
static PIPELINES_BUSY: std::sync::atomic::AtomicBool = std::sync::atomic::AtomicBool::new(true);

/// A light probe that renders its own reflection: six cameras at its center, one per face of a
/// cube, whose pictures are copied into the cube Bevy filters into the probe's light.
#[cfg(feature = "render")]
#[derive(bevy::ecs::component::Component)]
pub struct ProbeCapture {
    cube: bevy::asset::Handle<bevy::image::Image>,
    faces: [bevy::asset::Handle<bevy::image::Image>; 6],
    cameras: [bevy::ecs::entity::Entity; 6],
    size: u32,
    /// Captures every frame, rather than for [`CAPTURE_FRAMES`] after being asked.
    live: bool,
    frames_left: u32,
}

/// One of a captured probe's face cameras, which goes when the probe or its capture does.
#[cfg(feature = "render")]
#[derive(bevy::ecs::component::Component)]
pub struct ProbeFace {
    probe: bevy::ecs::entity::Entity,
    face: usize,
}

/// Makes an entity a reflection probe that renders what is around it, or with a `size` of zero
/// stops.
///
/// Six cameras at the probe's center draw the scene into a cube `size` texels a side, which has to
/// be a power of two, and Bevy filters the cube into the probe's light on the GPU. `live` captures
/// every frame, at the cost of drawing the scene six more times a frame, and otherwise the probe
/// captures over a few frames once nothing is left compiling, and keeps that until
/// [`bcs_render_recapture_probe`] asks again. `near` is where the
/// face cameras start seeing, so a probe inside a small object does not capture the inside of it.
///
/// The faces are drawn at Bevy's default exposure and without tonemapping, so what they hold is
/// the scene's light times that exposure, and an `intensity` near a thousand undoes it.
///
/// Returns [`status::INVALID_STATE`] for a camera, and [`status::NULL_ARG`] for a size that is not
/// a power of two.
///
/// # Safety
/// `falloff` must be null or point to three readable floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_probe_capture(
    entity: u64,
    size: u32,
    intensity: f32,
    falloff: *const f32,
    live: i32,
    near: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, size, intensity, falloff, live, near);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;

            let entity = Entity::from_bits(entity);
            // SAFETY: forwarded from the caller's promise.
            let falloff = unsafe { falloff_of(falloff) };

            with_world(|world| {
                if let Some(refusal) = refuse_probe(world, entity) {
                    return refusal;
                }

                stop_capture(world, entity);

                if size == 0 {
                    drop_probe_if_empty(world, entity);
                    return status::OK;
                }

                // Bevy's filter panics on anything else, inside a system, so it is refused here.
                if !size.is_power_of_two() || size > 8192 {
                    return status::NULL_ARG;
                }

                start_capture(world, entity, size, intensity, falloff, live != 0, near);
                status::OK
            })
        }
    })
}

/// Captures a probe made by [`bcs_render_set_probe_capture`] again, for one that is not live and
/// whose surroundings have changed.
///
/// Returns [`status::NOT_PRESENT`] where the entity has no capture.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_recapture_probe(entity: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = entity;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let entity = bevy::ecs::entity::Entity::from_bits(entity);

            with_world(|world| {
                let Ok(mut probe) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                let Some(mut capture) = probe.get_mut::<ProbeCapture>() else {
                    return status::NOT_PRESENT;
                };

                capture.frames_left = CAPTURE_FRAMES;
                status::OK
            })
        }
    })
}

/// Makes the cube, the six face images and the six cameras, and puts the light on the probe.
#[cfg(feature = "render")]
fn start_capture(
    world: &mut bevy::ecs::world::World,
    entity: bevy::ecs::entity::Entity,
    size: u32,
    intensity: f32,
    falloff: bevy::math::Vec3,
    live: bool,
    near: f32,
) {
    use bevy::asset::{Assets, RenderAssetUsages};
    use bevy::camera::{Camera, Camera3d, PerspectiveProjection, Projection, RenderTarget};
    use bevy::core_pipeline::tonemapping::Tonemapping;
    use bevy::ecs::name::Name;
    use bevy::image::Image;
    use bevy::light::{GeneratedEnvironmentMapLight, LightProbe};
    use bevy::render::render_resource::{
        Extent3d, TextureDimension, TextureFormat, TextureUsages, TextureViewDescriptor,
        TextureViewDimension,
    };
    use bevy::camera::Hdr;
    use bevy::transform::components::Transform;

    // Half floats, because a lit scene is brighter than white in places and a reflection of a lamp
    // clipped to white is a gray lamp.
    let face_image = || {
        let mut image = Image::new_fill(
            Extent3d { width: size, height: size, depth_or_array_layers: 1 },
            TextureDimension::D2,
            &[0; 8],
            TextureFormat::Rgba16Float,
            RenderAssetUsages::default(),
        );

        image.texture_descriptor.usage =
            TextureUsages::COPY_SRC | TextureUsages::RENDER_ATTACHMENT | TextureUsages::TEXTURE_BINDING;

        image
    };

    let mut cube = Image::new_fill(
        Extent3d { width: size, height: size, depth_or_array_layers: 6 },
        TextureDimension::D2,
        &[0; 8],
        TextureFormat::Rgba16Float,
        RenderAssetUsages::default(),
    );

    cube.texture_descriptor.usage = TextureUsages::COPY_DST | TextureUsages::TEXTURE_BINDING;
    cube.texture_view_descriptor = Some(TextureViewDescriptor {
        dimension: Some(TextureViewDimension::Cube),
        ..Default::default()
    });

    let (cube, faces) = {
        let mut images = world.resource_mut::<Assets<Image>>();
        let cube = images.add(cube);
        let faces = [(); 6].map(|_| images.add(face_image()));
        (cube, faces)
    };

    const NAMES: [&str; 6] = ["+X", "-X", "+Y", "-Y", "+Z", "-Z"];

    let cameras: [bevy::ecs::entity::Entity; 6] = core::array::from_fn(|face| {
        world
            .spawn((
                Camera3d::default(),
                Camera {
                    // Before every camera the game made, so a frame's faces are drawn before
                    // anything that might show them.
                    order: -1000 + face as isize,
                    ..Default::default()
                },
                RenderTarget::Image(faces[face].clone().into()),
                Projection::Perspective(PerspectiveProjection {
                    fov: core::f32::consts::FRAC_PI_2,
                    aspect_ratio: 1.0,
                    near: if near > 0.0 { near } else { 0.05 },
                    ..Default::default()
                }),
                // Light as it is, for Bevy's filter to turn into light again; a tonemapped face
                // would be a picture of the room rather than the light in it.
                Tonemapping::None,
                Hdr,
                Transform::default(),
                ProbeFace { probe: entity, face },
                Name::new(format!("Probe face {}", NAMES[face])),
            ))
            .id()
    });

    world.entity_mut(entity).insert((
        LightProbe { falloff },
        GeneratedEnvironmentMapLight {
            environment_map: cube.clone(),
            intensity,
            ..Default::default()
        },
        ProbeCapture {
            cube,
            faces,
            cameras,
            size,
            live,
            frames_left: CAPTURE_FRAMES,
        },
    ));
}

/// Takes a capture off an entity: its cameras, and the light made from them.
///
/// Both halves of the light go, because Bevy answers a generated map with an ordinary one it
/// inserts beside it, and the ordinary one left behind would keep lighting from a cube nothing
/// writes any more.
#[cfg(feature = "render")]
fn stop_capture(world: &mut bevy::ecs::world::World, entity: bevy::ecs::entity::Entity) {
    use bevy::light::{EnvironmentMapLight, GeneratedEnvironmentMapLight};

    let Some(capture) = world.entity_mut(entity).take::<ProbeCapture>() else {
        return;
    };

    for camera in capture.cameras {
        if let Ok(camera) = world.get_entity_mut(camera) {
            camera.despawn();
        }
    }

    world
        .entity_mut(entity)
        .remove::<(GeneratedEnvironmentMapLight, EnvironmentMapLight)>();
}

/// Puts each captured probe's face cameras at its center, pointing their ways, and turns them off
/// once a probe captured once has had its frames.
///
/// Run before transforms are propagated, so the cameras draw from where the probe is this frame.
/// The probe's own transform is read where it has no parent, which is the usual case and the one
/// that is current before propagation; a probe under a parent is read from its global transform,
/// which is a frame behind.
#[cfg(feature = "render")]
pub fn aim_probe_faces(
    mut probes: bevy::ecs::system::Query<(
        &mut ProbeCapture,
        &bevy::transform::components::Transform,
        &bevy::transform::components::GlobalTransform,
        bevy::ecs::query::Has<bevy::ecs::hierarchy::ChildOf>,
    )>,
    mut faces: bevy::ecs::system::Query<
        (&mut bevy::transform::components::Transform, &mut bevy::camera::Camera),
        (
            bevy::ecs::query::With<ProbeFace>,
            bevy::ecs::query::Without<ProbeCapture>,
        ),
    >,
) {
    for (mut capture, local, global, parented) in &mut probes {
        let center = if parented { global.translation() } else { local.translation };

        let active = capture.live || capture.frames_left > 0;

        // A frame drawn while something is still compiling does not count, since whatever is
        // compiling is missing from it.
        if !PIPELINES_BUSY.load(std::sync::atomic::Ordering::Relaxed) {
            capture.frames_left = capture.frames_left.saturating_sub(1);
        }

        for (face, camera) in capture.cameras.iter().enumerate() {
            let Ok((mut transform, mut camera)) = faces.get_mut(*camera) else { continue };
            let (forward, up) = FACES[face];

            *transform = bevy::transform::components::Transform::from_translation(center)
                .looking_to(forward, up);

            if camera.is_active != active {
                camera.is_active = active;
            }
        }
    }
}

/// Despawns face cameras whose probe is gone, since nothing else would.
#[cfg(feature = "render")]
pub fn drop_orphan_faces(
    mut commands: bevy::ecs::system::Commands,
    faces: bevy::ecs::system::Query<(bevy::ecs::entity::Entity, &ProbeFace)>,
    probes: bevy::ecs::system::Query<&ProbeCapture>,
) {
    for (entity, face) in &faces {
        let owned = probes
            .get(face.probe)
            .is_ok_and(|capture| capture.cameras.get(face.face) == Some(&entity));

        if !owned {
            commands.entity(entity).despawn();
        }
    }
}

/// What the render world copies each frame: six face images into the layers of a cube.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
struct ProbeCopies(Vec<ProbeCopy>);

#[cfg(feature = "render")]
struct ProbeCopy {
    faces: [bevy::asset::AssetId<bevy::image::Image>; 6],
    cube: bevy::asset::AssetId<bevy::image::Image>,
    size: u32,
}

#[cfg(feature = "render")]
fn extract_probe_copies(
    mut copies: bevy::ecs::system::ResMut<ProbeCopies>,
    captures: bevy::render::Extract<bevy::ecs::system::Query<&ProbeCapture>>,
) {
    copies.0.clear();

    for capture in &captures {
        // A probe whose cameras are off has nothing new in its faces, and copying the old pictures
        // again would only cost the copy.
        if !capture.live && capture.frames_left == 0 {
            continue;
        }

        copies.0.push(ProbeCopy {
            faces: capture.faces.each_ref().map(|face| face.id()),
            cube: capture.cube.id(),
            size: capture.size,
        });
    }
}

/// Notes whether anything is still compiling, for [`aim_probe_faces`] to read.
#[cfg(feature = "render")]
fn note_busy_pipelines(cache: bevy::ecs::system::Res<bevy::render::render_resource::PipelineCache>) {
    let busy = cache.waiting_pipelines().next().is_some();
    PIPELINES_BUSY.store(busy, std::sync::atomic::Ordering::Relaxed);
}

/// Copies each captured probe's faces into its cube, once the cameras have drawn them.
///
/// After the cameras rather than before, so the faces are this frame's. Bevy filters the cube
/// before any camera draws, so the probe's light is a frame behind its faces, which a reflection
/// does not show.
#[cfg(feature = "render")]
fn copy_probe_faces(
    copies: bevy::ecs::system::Res<ProbeCopies>,
    images: bevy::ecs::system::Res<bevy::render::render_asset::RenderAssets<bevy::render::texture::GpuImage>>,
    mut ctx: bevy::render::renderer::RenderContext,
) {
    use bevy::render::render_resource::{Extent3d, Origin3d, TexelCopyTextureInfo, TextureAspect};

    for copy in &copies.0 {
        let Some(cube) = images.get(copy.cube) else { continue };

        for (layer, face) in copy.faces.iter().enumerate() {
            let Some(face) = images.get(*face) else { continue };

            ctx.command_encoder().copy_texture_to_texture(
                face.texture.as_image_copy(),
                TexelCopyTextureInfo {
                    texture: &cube.texture,
                    mip_level: 0,
                    origin: Origin3d { x: 0, y: 0, z: layer as u32 },
                    aspect: TextureAspect::All,
                },
                Extent3d { width: copy.size, height: copy.size, depth_or_array_layers: 1 },
            );
        }
    }
}

/// Adds what keeps captured probes' cameras placed and copies their faces.
#[cfg(feature = "render")]
pub fn install(app: &mut bevy::app::App) {
    use bevy::app::PostUpdate;
    use bevy::ecs::schedule::IntoScheduleConfigs;
    use bevy::render::renderer::{RenderGraph, RenderGraphSystems};
    use bevy::render::{ExtractSchedule, Render, RenderApp, RenderSystems};

    app.add_systems(
        PostUpdate,
        (drop_orphan_faces, aim_probe_faces).before(bevy::transform::TransformSystems::Propagate),
    );

    let Some(render_app) = app.get_sub_app_mut(RenderApp) else {
        return;
    };

    render_app
        .init_resource::<ProbeCopies>()
        .add_systems(ExtractSchedule, extract_probe_copies)
        .add_systems(Render, note_busy_pipelines.in_set(RenderSystems::Cleanup))
        .add_systems(
            RenderGraph,
            copy_probe_faces
                .in_set(RenderGraphSystems::Render)
                .after(bevy::core_pipeline::schedule::camera_driver),
        );
}
