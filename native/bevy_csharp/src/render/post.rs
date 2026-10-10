//! What a camera does to the picture once the scene has been drawn.

pub mod effects;
pub mod environment;
pub mod lighting;

#[cfg(feature = "render")]
pub use environment::{
    gather_faces, reinterpret_cubemaps, PendingCubemap, PendingCubemaps, PendingEnvironment,
    PendingEnvironments, PendingFace, PendingFaces,
};
#[cfg(feature = "render")]
pub use lighting::materials_deferred;
#[cfg(feature = "render")]
use lighting::set_deferred;

use crate::interop::{status, BcsPostConfig};

#[cfg(feature = "render")]
use super::refuse_unless_camera;
#[cfg(feature = "render")]
use crate::state::with_world;

/// Takes temporal antialiasing off a camera, along with what it brought with it.
///
/// Bevy adds the jitter, the mip bias and the two prepasses as required components and leaves
/// them behind when the component that asked for them goes. Left alone the jitter keeps nudging
/// the projection every frame with nothing to resolve it, which reads as a shimmer, and the
/// prepasses keep drawing the scene again for nobody.
///
/// The motion vector prepass is the one piece that is not exclusively temporal antialiasing's,
/// because motion blur asks for it too, so it stays while a camera is still smearing.
#[cfg(feature = "render")]
fn drop_temporal(entity: &mut bevy::ecs::world::EntityWorldMut) {
    use bevy::anti_alias::taa::TemporalAntiAliasing;
    use bevy::core_pipeline::prepass::{DepthPrepass, MotionVectorPrepass};
    use bevy::post_process::motion_blur::MotionBlur;
    use bevy::render::camera::{MipBias, TemporalJitter};

    if !entity.contains::<TemporalAntiAliasing>() {
        return;
    }

    entity.remove::<(TemporalAntiAliasing, TemporalJitter, MipBias)>();

    // Kept where the game asked for depth itself, which a shader pass reading it does.
    if !entity
        .get::<RequestedPrepass>()
        .is_some_and(|requested| requested.0 & 1 != 0)
    {
        entity.remove::<DepthPrepass>();
    }

    if !entity.contains::<MotionBlur>() && !asked_for_motion(entity) {
        entity.remove::<MotionVectorPrepass>();
    }

    keep_traced(entity);
}

/// Whether the game asked for motion vectors itself, which a pass or a compute shader reading them
/// does, so that taking an effect off does not take them away.
#[cfg(feature = "render")]
pub fn asked_for_motion(entity: &bevy::ecs::world::EntityWorldMut) -> bool {
    entity
        .get::<RequestedPrepass>()
        .is_some_and(|requested| requested.0 & 4 != 0)
}

/// Puts back what ray-traced lighting reads, where a camera has it and something else took one of
/// its prepasses off as it went.
///
/// Solari requires its prepasses as it is inserted and is not asked again, so an effect taken off
/// after it, as motion blur takes the motion vectors it brought, left it reading nothing and the
/// picture unlit, the feature test's Cornell box among it.
#[cfg(feature = "render")]
pub fn keep_traced(entity: &mut bevy::ecs::world::EntityWorldMut) {
    #[cfg(feature = "solari")]
    if entity.contains::<bevy::solari::realtime::SolariLighting>() {
        use bevy::core_pipeline::prepass::{
            DeferredPrepass, DeferredPrepassDoubleBuffer, DepthPrepass, DepthPrepassDoubleBuffer,
            MotionVectorPrepass,
        };

        entity.insert((
            DeferredPrepass,
            DepthPrepass,
            MotionVectorPrepass,
            DeferredPrepassDoubleBuffer,
            DepthPrepassDoubleBuffer,
        ));
    }

    #[cfg(not(feature = "solari"))]
    let _ = entity;
}

/// Which prepasses a game asked a camera for, as the flags it gave.
///
/// Remembered so that what something else took off with it, as temporal antialiasing does with the
/// depth prepass when it goes, can be left where the game still asked for it.
#[cfg(feature = "render")]
#[derive(bevy::ecs::component::Component)]
pub struct RequestedPrepass(pub u32);

/// Asks a camera to draw depth, normals and motion vectors before the scene, for its shader passes
/// and compute shaders to read.
///
/// `flags` is a bit each: `1` depth, `2` normals, `4` motion vectors, `8` Bevy's G-buffer, which
/// also turns deferred rendering on, `16` keeping the previous frame's depth and G-buffer beside
/// this frame's, which does nothing without one of them, and `32` Bevy's depth pyramid, which turns
/// its GPU occlusion culling on for the camera, and brings depth. A bit left clear takes that
/// prepass off again, unless something else on the camera needs it, which is temporal antialiasing
/// for depth and motion, motion blur for motion, and screen-space reflections for depth and the
/// G-buffer.
///
/// A prepass draws the scene a second time, so it is only worth asking for when something reads
/// what it draws. A multisampled camera draws them multisampled, which a pass cannot bind, so a
/// camera read this way needs `Msaa` of one.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_prepass(camera: u64, flags: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, flags);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::anti_alias::taa::TemporalAntiAliasing;
            use bevy::core_pipeline::prepass::{
                DeferredPrepass, DeferredPrepassDoubleBuffer, DepthPrepass,
                DepthPrepassDoubleBuffer, MotionVectorPrepass, NormalPrepass,
            };
            use bevy::pbr::ScreenSpaceReflections;
            use bevy::post_process::motion_blur::MotionBlur;

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            with_world(|world| {
                if let Some(refusal) = refuse_unless_camera(world, entity) {
                    return refusal;
                }

                // Deferred is a switch for every camera's materials as well as a prepass on this
                // one, and is left on when this camera stops asking, since others may be reading it.
                if flags & 8 != 0 {
                    set_deferred(world, true);
                }

                // Where materials are deferred, Bevy queues each one into the deferred phase of
                // every camera drawing a prepass, and a camera with a prepass but no G-buffer has
                // no such phase, which ends the app. So any prepass brings the G-buffer then.
                let gbuffer = flags & 8 != 0 || (flags & (1 | 2 | 4) != 0 && materials_deferred(world));

                let mut camera = world.entity_mut(entity);
                camera.insert(RequestedPrepass(flags));
                let reflecting = camera.contains::<ScreenSpaceReflections>();

                // The pyramid is built from the depth prepass, by Bevy's occlusion culling, without
                // which it does not exist.
                if flags & 32 != 0 {
                    camera.insert(bevy::render::occlusion_culling::OcclusionCulling);
                } else {
                    camera.remove::<bevy::render::occlusion_culling::OcclusionCulling>();
                }

                // The G-buffer is drawn over the depth the depth prepass leaves, so it brings that.
                if flags & (1 | 32) != 0 || gbuffer {
                    camera.insert(DepthPrepass);
                } else if !camera.contains::<TemporalAntiAliasing>() && !reflecting {
                    camera.remove::<DepthPrepass>();
                }

                if gbuffer {
                    camera.insert(DeferredPrepass);
                } else if !reflecting {
                    camera.remove::<DeferredPrepass>();
                }

                // Last frame's depth, and G-buffer where there is one, kept beside this frame's.
                // Removed first either way, since each requires its prepass and would put it back.
                camera.remove::<(DepthPrepassDoubleBuffer, DeferredPrepassDoubleBuffer)>();

                if flags & 16 != 0 && flags & (1 | 8) != 0 {
                    camera.insert(DepthPrepassDoubleBuffer);

                    if flags & 8 != 0 {
                        camera.insert(DeferredPrepassDoubleBuffer);
                    }
                }

                if flags & 2 != 0 {
                    camera.insert(NormalPrepass);
                } else {
                    camera.remove::<NormalPrepass>();
                }

                if flags & 4 != 0 {
                    camera.insert(MotionVectorPrepass);
                } else if !camera.contains::<TemporalAntiAliasing>() && !camera.contains::<MotionBlur>() {
                    camera.remove::<MotionVectorPrepass>();
                }

                keep_traced(&mut camera);
                status::OK
            })
        }
    })
}

/// Sets what a camera does to the picture after the scene has been drawn.
///
/// Every effect is applied on every call, so a config describes the whole pipeline rather than
/// one change to it, so an effect the config leaves off is removed from the camera if it was
/// there.
/// That keeps a settings screen honest, since turning bloom off is the same call as turning it on.
///
/// Bloom reads a high dynamic range target, so asking for it without `hdr` gets a picture where
/// nothing is bright enough to scatter. The two are left to the caller rather than forced together,
/// because a game may need the range without the glow.
///
/// Temporal antialiasing is the one arm that can be refused. It resolves the whole picture from
/// past frames, which a multisampled target has not got, and Bevy answers the pair by warning once
/// a frame and drawing nothing, so a config asking for both is reported as
/// [`status::INVALID_STATE`] and the camera is left as it was. It also needs a 3D camera, because
/// the jitter it reads back is only applied to one, and on a 2D camera the pass finds nothing to
/// resolve.
///
/// # Safety
/// `config` must point to a readable [`BcsPostConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_post(entity: u64, config: *const BcsPostConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::anti_alias::contrast_adaptive_sharpening::ContrastAdaptiveSharpening;
            use bevy::anti_alias::fxaa::{Fxaa, Sensitivity};
            use bevy::anti_alias::smaa::{Smaa, SmaaPreset};
            use bevy::anti_alias::taa::TemporalAntiAliasing;
            use bevy::camera::Hdr;
            use bevy::core_pipeline::tonemapping::{DebandDither, Tonemapping};
            use bevy::post_process::bloom::{Bloom, BloomCompositeMode, BloomPrefilter};
            use bevy::render::view::Msaa;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            let tonemapping = match config.tonemapping {
                0 => Tonemapping::None,
                1 => Tonemapping::Reinhard,
                2 => Tonemapping::ReinhardLuminance,
                3 => Tonemapping::AcesFitted,
                4 => Tonemapping::AgX,
                5 => Tonemapping::SomewhatBoringDisplayTransform,
                7 => Tonemapping::BlenderFilmic,
                _ => Tonemapping::TonyMcMapface,
            };

            let msaa = match config.msaa {
                2 => Msaa::Sample2,
                4 => Msaa::Sample4,
                8 => Msaa::Sample8,
                _ => Msaa::Off,
            };

            // Refused before anything is written, so a camera that asked for the impossible pair
            // keeps the pipeline it had rather than half of the new one.
            if config.antialias == 3 && msaa != Msaa::Off {
                return status::INVALID_STATE;
            }

            let sensitivity = match config.antialias_quality {
                0 => Sensitivity::Low,
                2 => Sensitivity::High,
                3 => Sensitivity::Ultra,
                _ => Sensitivity::Medium,
            };

            let preset = match config.antialias_quality {
                0 => SmaaPreset::Low,
                2 => SmaaPreset::High,
                3 => SmaaPreset::Ultra,
                _ => SmaaPreset::Medium,
            };

            with_world(|world| {
                let entity = crate::ecs::entity_from(entity);

                if let Some(status) = refuse_unless_camera(world, entity) {
                    return status;
                }

                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                entity_mut.insert(tonemapping);
                entity_mut.insert(if config.dither != 0 {
                    DebandDither::Enabled
                } else {
                    DebandDither::Disabled
                });
                entity_mut.insert(msaa);

                if config.hdr != 0 {
                    entity_mut.insert(Hdr);
                } else {
                    entity_mut.remove::<Hdr>();
                }

                if config.bloom != 0 {
                    entity_mut.insert(Bloom {
                        intensity: config.bloom_intensity,
                        prefilter: BloomPrefilter {
                            threshold: config.bloom_threshold,
                            threshold_softness: config.bloom_threshold_softness,
                        },
                        composite_mode: if config.bloom_mode == 1 {
                            BloomCompositeMode::Additive
                        } else {
                            BloomCompositeMode::EnergyConserving
                        },
                        ..Bloom::NATURAL
                    });
                } else {
                    entity_mut.remove::<Bloom>();
                }

                match config.antialias {
                    1 => {
                        entity_mut.remove::<Smaa>();
                        drop_temporal(&mut entity_mut);
                        entity_mut.insert(Fxaa {
                            enabled: true,
                            edge_threshold: sensitivity,
                            edge_threshold_min: sensitivity,
                        });
                    }
                    2 => {
                        entity_mut.remove::<Fxaa>();
                        drop_temporal(&mut entity_mut);
                        entity_mut.insert(Smaa { preset });
                    }
                    3 => {
                        entity_mut.remove::<Fxaa>();
                        entity_mut.remove::<Smaa>();

                        // Inserted only if it is not there, unlike every other effect here. The
                        // component's one field asks Bevy to throw away the frames it has
                        // accumulated, and the config has nothing to say about it, so writing a
                        // fresh one on every call would keep clearing the history the pass
                        // exists to build.
                        if !entity_mut.contains::<TemporalAntiAliasing>() {
                            entity_mut.insert(TemporalAntiAliasing::default());
                        }
                    }
                    _ => {
                        entity_mut.remove::<Fxaa>();
                        entity_mut.remove::<Smaa>();
                        drop_temporal(&mut entity_mut);
                    }
                }

                if config.sharpen > 0.0 {
                    entity_mut.insert(ContrastAdaptiveSharpening {
                        enabled: true,
                        sharpening_strength: config.sharpen,
                        denoise: false,
                    });
                } else {
                    entity_mut.remove::<ContrastAdaptiveSharpening>();
                }

                status::OK
            })
        }
    })
}

/// Gives every camera drawing to the same target the fewest samples any of them asked for.
///
/// Bevy keeps one picture per target that each camera drawing there renders into in turn, and a
/// camera's depth is made at its own sample count. So a camera multisampled four times after one
/// drawn once a pixel meets a picture of the wrong count, and wgpu refuses the pass, which ends the
/// app. The fewest rather than the most, since a camera asks for one sample because something
/// needs it to (a pass reading its prepass, deferred rendering, temporal antialiasing, Solari, the
/// interface's overlay), and none of those works multisampled. Said once per camera, so a game that
/// asked for four samples learns why it has one.
#[cfg(feature = "render")]
pub fn match_samples_per_target(
    mut cameras: bevy::ecs::system::Query<(
        bevy::ecs::entity::Entity,
        &bevy::camera::RenderTarget,
        &mut bevy::render::view::Msaa,
    )>,
    primary: bevy::ecs::system::Query<bevy::ecs::entity::Entity, bevy::ecs::query::With<bevy::window::PrimaryWindow>>,
    mut told: bevy::ecs::system::Local<std::collections::HashSet<bevy::ecs::entity::Entity>>,
) {
    use bevy::render::view::Msaa;

    let primary = primary.single().ok();
    let mut fewest: std::collections::HashMap<bevy::camera::NormalizedRenderTarget, Msaa> = Default::default();

    for (_, target, msaa) in &cameras {
        let Some(key) = target.normalize(primary) else { continue };
        let entry = fewest.entry(key).or_insert(*msaa);
        if msaa.samples() < entry.samples() {
            *entry = *msaa;
        }
    }

    for (entity, target, mut msaa) in &mut cameras {
        let Some(wanted) = target.normalize(primary).and_then(|key| fewest.get(&key).copied()) else {
            continue;
        };

        if msaa.samples() == wanted.samples() {
            continue;
        }

        if told.insert(entity) {
            bevy::log::info!(
                "Camera {entity} asked for {} samples a pixel and draws with {}, since another camera \
                 drawing to the same target draws with {} and cameras sharing a target have to match.",
                msaa.samples(),
                wanted.samples(),
                wanted.samples()
            );
        }

        *msaa = wanted;
    }
}


#[cfg(all(test, feature = "render"))]
mod tests {
    use bevy::asset::Handle;
    use bevy::camera::RenderTarget;
    use bevy::ecs::system::RunSystemOnce;
    use bevy::image::Image;
    use bevy::prelude::World;
    use bevy::render::view::Msaa;
    use bevy::window::{PrimaryWindow, Window, WindowRef};

    #[test]
    fn cameras_sharing_a_target_draw_with_the_fewest_samples() {
        let mut world = World::new();
        world.spawn((Window::default(), PrimaryWindow));

        // Two cameras on the window, one named by reference and one as the primary window, to
        // show both spellings count as the same target, and one on an image of its own that
        // nothing else draws to and so keeps what it asked for.
        let overlay = world.spawn((RenderTarget::Window(WindowRef::Primary), Msaa::Off)).id();
        let scene = world.spawn((RenderTarget::Window(WindowRef::Primary), Msaa::Sample4)).id();
        let image: Handle<Image> = Handle::default();
        let alone = world.spawn((RenderTarget::Image(image.into()), Msaa::Sample8)).id();

        world.run_system_once(super::match_samples_per_target).unwrap();

        assert_eq!(*world.get::<Msaa>(overlay).unwrap(), Msaa::Off);
        assert_eq!(*world.get::<Msaa>(scene).unwrap(), Msaa::Off);
        assert_eq!(*world.get::<Msaa>(alone).unwrap(), Msaa::Sample8);
    }
}

/// Keeps the render world's copy of the scene-wide ambient light and clear color equal to the main
/// world's, every frame.
///
/// Bevy copies each across when it has changed since its copying system last ran, and one set
/// from a managed startup system is missed that way: the scene's first frame draws with Bevy's
/// defaults and every frame after it too, until something changes the value again. Comparing the
/// two each frame costs a few floats and catches a change however it was made, so a value is in
/// the picture from the frame it was set.
#[cfg(feature = "render")]
pub fn install(app: &mut bevy::app::App) {
    use bevy::render::{ExtractSchedule, RenderApp};

    let Some(render_app) = app.get_sub_app_mut(RenderApp) else {
        return;
    };

    render_app.add_systems(ExtractSchedule, keep_scene_wide_values);
}

/// Copies the ambient light and the clear color across where the render world's differ.
#[cfg(feature = "render")]
fn keep_scene_wide_values(
    ambient: bevy::render::Extract<Option<bevy::ecs::system::Res<bevy::light::GlobalAmbientLight>>>,
    clear: bevy::render::Extract<Option<bevy::ecs::system::Res<bevy::camera::ClearColor>>>,
    mut commands: bevy::ecs::system::Commands,
    drawn_ambient: Option<bevy::ecs::system::ResMut<bevy::light::GlobalAmbientLight>>,
    drawn_clear: Option<bevy::ecs::system::ResMut<bevy::camera::ClearColor>>,
) {
    if let Some(ambient) = ambient.as_ref() {
        match drawn_ambient {
            Some(mut drawn) => {
                if drawn.color != ambient.color
                    || drawn.brightness != ambient.brightness
                    || drawn.affects_lightmapped_meshes != ambient.affects_lightmapped_meshes
                {
                    *drawn = (**ambient).clone();
                }
            }
            None => commands.insert_resource((**ambient).clone()),
        }
    }

    if let Some(clear) = clear.as_ref() {
        match drawn_clear {
            Some(mut drawn) => {
                if drawn.0 != clear.0 {
                    drawn.0 = clear.0;
                }
            }
            None => commands.insert_resource((**clear).clone()),
        }
    }
}
