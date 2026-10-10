//! Bevy's Solari, lighting traced against the scene with hardware ray queries, direct light from
//! every light and emissive surface, and indirect light bounced off everything, in real time.
//!
//! It is world-space global illumination and traced reflections as an engine ships them, and the
//! reference a package doing either differently is weighed against. It sits behind the `solari`
//! feature and an app's `Config.RayTracedLighting`, since adding it makes every Bevy material
//! deferred, and it is added only where the adapter has ray queries and binding arrays, which the
//! bridge asks first. A camera draws with it once `bcs_render_set_ray_traced_lighting` turns it on,
//! and a mesh takes part once `bcs_render_set_ray_traced` has made it the shape the ray tracing
//! structures are built from.

use crate::interop::status;
#[cfg(feature = "solari")]
use crate::state::with_world;

/// Whether Solari is running in this app.
#[cfg(feature = "solari")]
static ACTIVE: std::sync::atomic::AtomicBool = std::sync::atomic::AtomicBool::new(false);

/// Solari's scene in the render world, which a dispatch of a shader importing `bcs_ray` binds as
/// its group two.
#[cfg(feature = "solari")]
pub type SceneBindings = bevy::solari::scene::RaytracingSceneBindings;

/// Whether Solari is running in this app, which makes every Bevy material deferred.
#[cfg(feature = "render")]
pub fn running() -> bool {
    #[cfg(feature = "solari")]
    {
        ACTIVE.load(std::sync::atomic::Ordering::Relaxed)
    }

    #[cfg(not(feature = "solari"))]
    {
        false
    }
}

/// The running Solari's description of its scene group, kept from the render world once Solari
/// has made it, since from Bevy 0.20 it is made with the render device and not alone.
#[cfg(feature = "solari")]
static LAYOUT: std::sync::Mutex<Option<bevy::render::render_resource::BindGroupLayoutDescriptor>> =
    std::sync::Mutex::new(None);

/// The layout of Solari's scene group, or `None` where this app has no Solari to bind it or Solari
/// has not described it yet, which a dispatch waits a frame for.
#[cfg(feature = "render")]
pub fn scene_layout() -> Option<bevy::render::render_resource::BindGroupLayoutDescriptor> {
    #[cfg(feature = "solari")]
    if ACTIVE.load(std::sync::atomic::Ordering::Relaxed) {
        return LAYOUT.lock().ok().and_then(|layout| layout.clone());
    }

    None
}

/// Keeps the description of Solari's scene group, the first frame it is there.
#[cfg(feature = "solari")]
fn keep_layout(bindings: Option<bevy::ecs::system::Res<SceneBindings>>) {
    if let Some(bindings) = bindings
        && let Ok(mut layout) = LAYOUT.lock()
        && layout.is_none()
    {
        *layout = Some(bindings.bind_group_layout.clone());
    }
}

/// Says Solari is not running, before an app decides whether to add it.
///
/// Called for every app a process builds, since a flag left on by an earlier app with it would
/// otherwise answer for a later one without it.
#[cfg(feature = "solari")]
pub fn forget() {
    ACTIVE.store(false, std::sync::atomic::Ordering::Relaxed);
    if let Ok(mut layout) = LAYOUT.lock() {
        *layout = None;
    }
}

/// Adds Solari, if the adapter Bevy would choose can run it.
#[cfg(feature = "solari")]
pub fn install(app: &mut bevy::app::App, backends: Option<wgpu::Backends>) {
    use bevy::ecs::lifecycle::Insert;
    use bevy::ecs::observer::On;
    use bevy::ecs::system::Query;
    use bevy::render::view::Msaa;

    let needed = bevy::solari::SolariPlugins::required_wgpu_features();

    if let Err(lacking) = super::adapter::lacking(backends, needed) {
        bevy::log::warn!(
            "Ray-traced lighting was asked for, but the GPU cannot trace rays ({lacking}), so \
             cameras are lit the usual way in this run."
        );
        return;
    }

    app.add_plugins(bevy::solari::SolariPlugins);

    if let Some(renderer) = app.get_sub_app_mut(bevy::render::RenderApp) {
        use bevy::ecs::schedule::IntoScheduleConfigs;
        renderer.add_systems(
            bevy::render::Render,
            keep_layout.in_set(bevy::render::RenderSystems::PrepareResources),
        );
    }

    // A camera traced with Solari draws once a pixel, which is kept by setting it on every insert
    // rather than refusing a post-processing setting that arrives later.
    app.add_observer(
        |insert: On<Insert<Msaa>>,
         mut cameras: Query<&mut Msaa, bevy::ecs::query::With<bevy::solari::realtime::SolariLighting>>| {
            if let Ok(mut msaa) = cameras.get_mut(insert.entity)
                && *msaa != Msaa::Off
            {
                *msaa = Msaa::Off;
            }
        },
    );

    ACTIVE.store(true, std::sync::atomic::Ordering::Relaxed);
}

/// Whether Solari is running: built in, asked for, and the adapter can trace rays. `1` or `0`.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_ray_tracing_active() -> i32 {
    #[cfg(feature = "solari")]
    {
        ACTIVE.load(std::sync::atomic::Ordering::Relaxed) as i32
    }

    #[cfg(not(feature = "solari"))]
    {
        0
    }
}

/// Lights a camera with Solari, or with `on` zero the usual way again.
///
/// Solari reads the G-buffer, depth, motion and the previous frame's of each, which it asks the
/// camera for itself, and writes the picture from compute, so the camera's picture is made
/// writable by compute and drawn once a pixel. ReSTIR is on, where Bevy leaves it off. Returns
/// [`status::UNSUPPORTED`] where Solari is not running.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_ray_traced_lighting(camera: u64, on: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "solari"))]
        {
            let _ = (camera, on);
            status::UNSUPPORTED
        }

        #[cfg(feature = "solari")]
        {
            use bevy::camera::CameraMainTextureUsages;
            use bevy::render::render_resource::TextureUsages;
            use bevy::render::view::Msaa;
            use bevy::solari::realtime::SolariLighting;

            if !ACTIVE.load(std::sync::atomic::Ordering::Relaxed) {
                return status::UNSUPPORTED;
            }

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            with_world(|world| {
                if let Some(refusal) = super::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let mut camera = world.entity_mut(entity);

                if on == 0 {
                    camera.remove::<SolariLighting>();
                    return status::OK;
                }

                // ReSTIR reuses samples between pixels and frames, and Bevy leaves it off for a
                // denoiser to clean the picture instead. Bevy's denoiser is DLSS's, which the
                // bridge does not build, and without one ReSTIR halved the noise between two
                // frames of a Cornell box lit by a panel, for 1.3 ms more of the GPU at 1280 by
                // 720 on a laptop's RTX 4070.
                camera.insert((
                    SolariLighting { restir: true, ..SolariLighting::default() },
                    CameraMainTextureUsages::default().with(TextureUsages::STORAGE_BINDING),
                    Msaa::Off,
                ));

                status::OK
            })
        }
    })
}

/// Makes an entity's mesh take part in ray tracing, reshaping the mesh the way Solari builds its
/// structures from: exactly positions, normals, texture coordinates and tangents, which are worked
/// out where it has none, thirty-two bit indices, and ray tracing enabled on it. A mesh with no
/// texture coordinates is given coordinates and tangents of zero.
///
/// The mesh is changed in place, so the entity keeps drawing it as it did and the rays meet the
/// same triangles the picture shows. The entity's material has to be Bevy's standard material.
/// Returns [`status::NULL_ARG`] for a mesh that is not indexed triangles with normals,
/// [`status::NOT_PRESENT`] where it has not loaded, and [`status::UNSUPPORTED`] where Solari is not
/// running.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_ray_traced(entity: u64, mesh: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "solari"))]
        {
            let _ = (entity, mesh);
            status::UNSUPPORTED
        }

        #[cfg(feature = "solari")]
        {
            use bevy::asset::Assets;
            use bevy::mesh::{Indices, Mesh, PrimitiveTopology};
            use bevy::solari::scene::RaytracingMesh3d;

            if !ACTIVE.load(std::sync::atomic::Ordering::Relaxed) {
                return status::UNSUPPORTED;
            }

            let entity = bevy::ecs::entity::Entity::from_bits(entity);

            with_world(|world| {
                let Some(handle) = crate::assets::clone_handle(world, mesh).and_then(|handle| handle.try_typed::<Mesh>().ok())
                else {
                    return status::NO_COMPONENT;
                };

                if world.get_entity(entity).is_err() {
                    return status::NO_ENTITY;
                }

                {
                    let mut meshes = world.resource_mut::<Assets<Mesh>>();
                    let Some(mut found) = meshes.get_mut(&handle) else {
                        return status::NOT_PRESENT;
                    };

                    if found.primitive_topology() != PrimitiveTopology::TriangleList
                        || found.attribute(Mesh::ATTRIBUTE_NORMAL).is_none()
                        || found.indices().is_none()
                    {
                        return status::NULL_ARG;
                    }

                    // A mesh drawn in plain colors often has no texture coordinates, and Solari
                    // reads them whether a texture is there or not. Bevy's own example gives such
                    // a mesh coordinates and tangents of zero, which no texture lookup is then
                    // made from, rather than refusing it.
                    if found.attribute(Mesh::ATTRIBUTE_UV_0).is_none() {
                        let count = found.count_vertices();
                        found.insert_attribute(Mesh::ATTRIBUTE_UV_0, vec![[0.0f32, 0.0]; count]);
                        found.insert_attribute(Mesh::ATTRIBUTE_TANGENT, vec![[0.0f32, 0.0, 0.0, 0.0]; count]);
                    }

                    let kept = [
                        Mesh::ATTRIBUTE_POSITION.id,
                        Mesh::ATTRIBUTE_NORMAL.id,
                        Mesh::ATTRIBUTE_UV_0.id,
                        Mesh::ATTRIBUTE_TANGENT.id,
                    ];

                    let extra: Vec<_> = found
                        .attributes()
                        .map(|(attribute, _)| attribute.id)
                        .filter(|id| !kept.contains(id))
                        .collect();

                    for id in extra {
                        found.remove_attribute(id);
                    }

                    if found.attribute(Mesh::ATTRIBUTE_TANGENT).is_none() && found.generate_tangents().is_err() {
                        return status::NULL_ARG;
                    }

                    if let Some(Indices::U16(indices)) = found.indices() {
                        let wide: Vec<u32> = indices.iter().map(|index| *index as u32).collect();
                        found.insert_indices(Indices::U32(wide));
                    }

                    found.enable_raytracing = true;
                }

                world.entity_mut(entity).insert(RaytracingMesh3d(handle));
                status::OK
            })
        }
    })
}
