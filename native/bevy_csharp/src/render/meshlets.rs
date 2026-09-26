//! Bevy's meshlets: meshes cut into small clusters that are culled and chosen a level of detail
//! at a time on the GPU, drawn into a visibility buffer and shaded from it.
//!
//! This is virtualized geometry as Bevy ships it, and the first thing a scene of dense meshes
//! wants. It is behind the `meshlet` feature, which the build takes with `--meshlet`, because the
//! processor that cuts a mesh into clusters compiles C and C++ libraries every other profile does
//! without. Even then it is off until an app asks for it with a cluster budget, since Bevy's plugin
//! ends the process on a GPU without 64-bit texture atomics and on any camera drawing more than
//! once a pixel. The bridge asks the GPU before adding the plugin, so a machine that cannot run it
//! is told and carries on without it, and turns multisampling off on every camera while it runs,
//! so no setting a game makes can reach the check that would end it.
//!
//! A meshlet mesh is made from an ordinary one with [`bcs_render_create_meshlet_mesh`], which
//! takes seconds for a large mesh and so runs on a worker; the handle it answers is empty until
//! the work is done, and an entity carrying it draws nothing until then. It is drawn with Bevy's
//! standard material, the same way a mesh is.

use crate::interop::status;
#[cfg(feature = "meshlet")]
use crate::state::with_world;

/// Whether the meshlet plugin is running in this app, which is what the managed side asks before
/// making meshlet meshes.
#[cfg(feature = "meshlet")]
static ACTIVE: std::sync::atomic::AtomicBool = std::sync::atomic::AtomicBool::new(false);

/// Adds the meshlet plugin with room for `clusters` clusters at once, if the GPU can run it.
///
/// `clusters` is capped at Bevy's own limit of two to the twenty-fifth, since the plugin ends the
/// process past it. Each costs four bytes of GPU memory, and too few shows as meshes flickering
/// or missing parts.
#[cfg(feature = "meshlet")]
pub fn install(app: &mut bevy::app::App, clusters: u32, backends: Option<wgpu::Backends>) {
    use bevy::app::Update;
    use bevy::ecs::schedule::IntoScheduleConfigs;
    use bevy::ecs::lifecycle::Insert;
    use bevy::ecs::observer::On;
    use bevy::ecs::system::Query;
    use bevy::render::view::Msaa;

    let needed = bevy::pbr::experimental::meshlet::MeshletPlugin::required_wgpu_features();

    if let Err(lacking) = super::adapter::lacking(backends, needed) {
        bevy::log::warn!(
            "Meshlets were asked for, but the GPU cannot draw them ({lacking}), so meshlet meshes \
             draw nothing in this run."
        );
        return;
    }

    app.add_plugins(bevy::pbr::experimental::meshlet::MeshletPlugin {
        cluster_buffer_slots: clusters.min(1 << 25),
    });

    // On every insert, which is how both a new camera's default and a later post-processing
    // setting arrive, so there is no frame on which a camera draws more than once a pixel.
    app.add_observer(|insert: On<Insert, Msaa>, mut cameras: Query<&mut Msaa>| {
        if let Ok(mut msaa) = cameras.get_mut(insert.entity)
            && *msaa != Msaa::Off
        {
            *msaa = Msaa::Off;
        }
    });

    app.init_resource::<Conversions>();
    app.add_systems(Update, (convert_meshes, attach_made).chain());
    ACTIVE.store(true, std::sync::atomic::Ordering::Relaxed);
}

/// One mesh on its way to becoming a meshlet mesh.
#[cfg(feature = "meshlet")]
struct Conversion {
    mesh: bevy::asset::Handle<bevy::mesh::Mesh>,
    into: bevy::asset::Handle<bevy::pbr::experimental::meshlet::MeshletMesh>,
    quantization: u8,
    /// Where to write the finished mesh as a `.meshlet_mesh` file, for a game to ship baked.
    save_to: Option<std::path::PathBuf>,
    task: Option<bevy::tasks::Task<Result<bevy::pbr::experimental::meshlet::MeshletMesh, String>>>,
}

/// Writes a meshlet mesh as the file Bevy's loader reads, through Bevy's own saver, so the two
/// agree on the format by construction.
#[cfg(feature = "meshlet")]
async fn save(meshlet: &bevy::pbr::experimental::meshlet::MeshletMesh, path: &std::path::Path) -> Result<(), String> {
    use bevy::asset::saver::{AssetSaver, SavedAsset};
    use bevy::pbr::experimental::meshlet::MeshletMeshSaver;

    let mut bytes: Vec<u8> = Vec::new();
    let asset_path = bevy::asset::AssetPath::from(path.to_string_lossy().into_owned());

    MeshletMeshSaver
        .save(&mut bytes, SavedAsset::from_asset(meshlet), &(), asset_path)
        .await
        .map_err(|error| error.to_string())?;

    if let Some(directory) = path.parent() {
        std::fs::create_dir_all(directory).map_err(|error| error.to_string())?;
    }

    std::fs::write(path, bytes).map_err(|error| format!("writing {}: {error}", path.display()))
}

/// The meshes being cut into clusters, and the ones that have been.
///
/// The finished ones are remembered by id because Bevy takes a meshlet mesh out of its asset
/// collection once it is on the GPU, so whether it exists there says nothing about whether it was
/// made.
#[cfg(feature = "meshlet")]
#[derive(bevy::ecs::resource::Resource, Default)]
struct Conversions {
    working: Vec<Conversion>,
    made: std::collections::HashSet<bevy::asset::AssetId<bevy::pbr::experimental::meshlet::MeshletMesh>>,
    /// Handles this bridge reserved, so one not among them came from somewhere else, a file the
    /// asset server loads, and needs no waiting here.
    reserved: std::collections::HashSet<bevy::asset::AssetId<bevy::pbr::experimental::meshlet::MeshletMesh>>,
}

/// An entity waiting for its meshlet mesh to be made before it is given it.
///
/// Bevy's meshlet renderer skips an entity whose mesh the asset server is still loading, but takes
/// any other handle as made, and one still being converted ends the frame with a panic. So the
/// entity carries this until the conversion lands.
#[cfg(feature = "meshlet")]
#[derive(bevy::ecs::component::Component)]
struct WaitingMeshlet(bevy::asset::Handle<bevy::pbr::experimental::meshlet::MeshletMesh>);

/// Gives an entity a meshlet mesh to draw, now if it has been made and once it is otherwise, and
/// takes any ordinary mesh off it, which would draw it a second time.
#[cfg(feature = "meshlet")]
pub fn attach(
    world: &mut bevy::ecs::world::World,
    entity: bevy::ecs::entity::Entity,
    handle: bevy::asset::Handle<bevy::pbr::experimental::meshlet::MeshletMesh>,
) {
    use bevy::pbr::experimental::meshlet::MeshletMesh3d;

    let waiting = world
        .get_resource::<Conversions>()
        .is_some_and(|conversions| conversions.reserved.contains(&handle.id()) && !conversions.made.contains(&handle.id()));

    let mut entity = world.entity_mut(entity);
    entity.remove::<(bevy::mesh::Mesh3d, MeshletMesh3d)>();

    if waiting {
        entity.insert(WaitingMeshlet(handle));
    } else {
        entity.remove::<WaitingMeshlet>();
        entity.insert(MeshletMesh3d(handle));
    }
}

/// Gives each waiting entity its meshlet mesh once the mesh has been made.
#[cfg(feature = "meshlet")]
fn attach_made(
    mut commands: bevy::ecs::system::Commands,
    conversions: bevy::ecs::system::Res<Conversions>,
    waiting: bevy::ecs::system::Query<(bevy::ecs::entity::Entity, &WaitingMeshlet)>,
) {
    for (entity, meshlet) in &waiting {
        if conversions.made.contains(&meshlet.0.id()) {
            commands
                .entity(entity)
                .remove::<WaitingMeshlet>()
                .insert(bevy::pbr::experimental::meshlet::MeshletMesh3d(meshlet.0.clone()));
        }
    }
}

/// Starts converting each mesh once it has loaded, and puts each finished meshlet mesh where its
/// handle points.
#[cfg(feature = "meshlet")]
fn convert_meshes(
    mut conversions: bevy::ecs::system::ResMut<Conversions>,
    meshes: bevy::ecs::system::Res<bevy::asset::Assets<bevy::mesh::Mesh>>,
    mut meshlets: bevy::ecs::system::ResMut<bevy::asset::Assets<bevy::pbr::experimental::meshlet::MeshletMesh>>,
) {
    use bevy::pbr::experimental::meshlet::MeshletMesh;
    use bevy::tasks::futures_lite::future;

    let Conversions { working, made, .. } = &mut *conversions;

    working.retain_mut(|conversion| {
        let Some(task) = &mut conversion.task else {
            let Some(mesh) = meshes.get(&conversion.mesh) else {
                // Still loading, which a mesh from a file is for a few frames.
                return true;
            };

            let mesh = only_what_meshlets_keep(mesh);
            let quantization = conversion.quantization;
            let save_to = conversion.save_to.clone();

            conversion.task = Some(bevy::tasks::AsyncComputeTaskPool::get().spawn(async move {
                let meshlet = MeshletMesh::from_mesh(&mesh?, quantization).map_err(|error| error.to_string())?;

                if let Some(path) = save_to {
                    save(&meshlet, &path).await?;
                }

                Ok(meshlet)
            }));

            return true;
        };

        let Some(done) = bevy::tasks::block_on(future::poll_once(task)) else {
            return true;
        };

        match done {
            Ok(meshlet) => {
                let _ = meshlets.insert(conversion.into.id(), meshlet);
                made.insert(conversion.into.id());
            }
            Err(error) => bevy::log::warn!("A mesh could not be made into a meshlet mesh: {error}"),
        }

        false
    });
}

/// A copy of `mesh` with the three attributes a meshlet mesh is made of and nothing else, since
/// the processor refuses a mesh carrying more, and a tangent or a vertex color is what most meshes
/// carry besides. Tangents are worked out again when a meshlet mesh is drawn.
#[cfg(feature = "meshlet")]
fn only_what_meshlets_keep(mesh: &bevy::mesh::Mesh) -> Result<bevy::mesh::Mesh, String> {
    use bevy::mesh::Mesh;

    let kept = [Mesh::ATTRIBUTE_POSITION.id, Mesh::ATTRIBUTE_NORMAL.id, Mesh::ATTRIBUTE_UV_0.id];
    let mut copy = mesh.clone();

    let extra: Vec<_> = copy
        .attributes()
        .map(|(attribute, _)| attribute.id)
        .filter(|id| !kept.contains(id))
        .collect();

    for id in extra {
        copy.remove_attribute(id);
    }

    if copy.attribute(Mesh::ATTRIBUTE_NORMAL).is_none() {
        copy.compute_normals();
    }

    if copy.attribute(Mesh::ATTRIBUTE_UV_0).is_none() {
        return Err("it has no texture coordinates, which a meshlet mesh needs".into());
    }

    if copy.indices().is_none() {
        return Err("it has no indices, which a meshlet mesh needs".into());
    }

    Ok(copy)
}

/// Whether meshlets are running in this app: the bridge was built with them, the app asked for
/// them, and the GPU can draw them. `1` or `0`.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_meshlets_active() -> i32 {
    #[cfg(feature = "meshlet")]
    {
        ACTIVE.load(std::sync::atomic::Ordering::Relaxed) as i32
    }

    #[cfg(not(feature = "meshlet"))]
    {
        0
    }
}

/// Starts making a meshlet mesh from the mesh `mesh` names, and answers its asset key at once.
///
/// The work runs on a worker, once the mesh has loaded, and the handle is empty until it is done.
/// `quantization` is how finely positions are kept, as a power of two fractions of a centimeter,
/// and zero takes Bevy's default of four, a sixteenth of a centimeter. `save_to`, a path under the
/// asset root or null, is where the finished mesh is also written as a `.meshlet_mesh` file, which
/// is how a game bakes its meshes once and loads them after without converting again.
///
/// # Safety
/// `save_to` must be null or a NUL-terminated UTF-8 string.
///
/// Returns [`status::UNSUPPORTED`] where meshlets are not running, and [`status::NO_COMPONENT`]
/// where the key names no mesh.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_create_meshlet_mesh(
    mesh: i32,
    quantization: u32,
    save_to: *const core::ffi::c_char,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "meshlet"))]
        {
            let _ = (mesh, quantization, save_to);
            status::UNSUPPORTED
        }

        #[cfg(feature = "meshlet")]
        {
            use bevy::pbr::experimental::meshlet::{MeshletMesh, MESHLET_DEFAULT_VERTEX_POSITION_QUANTIZATION_FACTOR};

            if !ACTIVE.load(std::sync::atomic::Ordering::Relaxed) {
                return status::UNSUPPORTED;
            }

            let save_to = if save_to.is_null() {
                None
            } else {
                match unsafe { crate::interop::cstr_to_string(save_to) } {
                    Some(path) if !path.is_empty() => Some(path),
                    _ => return status::NULL_ARG,
                }
            };

            with_world(|world| {
                let save_to = save_to.map(|path| {
                    world
                        .get_resource::<super::programs::ShaderPrograms>()
                        .map_or_else(|| std::path::PathBuf::from("assets"), |programs| programs.root().to_path_buf())
                        .join(path)
                });

                let Some(mesh) = crate::assets::clone_handle(world, mesh)
                    .and_then(|handle| handle.try_typed::<bevy::mesh::Mesh>().ok())
                else {
                    return status::NO_COMPONENT;
                };

                let into = world.resource::<bevy::asset::Assets<MeshletMesh>>().reserve_handle();
                let mut conversions = world.resource_mut::<Conversions>();
                conversions.reserved.insert(into.id());

                conversions.working.push(Conversion {
                    mesh,
                    into: into.clone(),
                    quantization: if quantization == 0 {
                        MESHLET_DEFAULT_VERTEX_POSITION_QUANTIZATION_FACTOR
                    } else {
                        quantization.min(u8::MAX as u32) as u8
                    },
                    save_to,
                    task: None,
                });

                crate::assets::insert_handle(world, into.untyped())
            })
        }
    })
}
