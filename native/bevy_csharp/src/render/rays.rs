//! Ray scenes: acceleration structures the game builds over the meshes of a geometry pool, for
//! rays a compute shader of its own traces.
//!
//! Solari keeps one acceleration structure, over the meshes given to ray-traced lighting, and a
//! shader reaches it through `bcs_ray`. A package tracing rays for its own purposes often wants a
//! different scene: stand-ins simpler than what is drawn, a level of detail meant for rays, or no
//! Solari at all. A ray scene is that. Its meshes are a pool's (see [`super::pools`]), each built
//! once into a bottom-level structure, and its instances are entities in numbered slots, each with
//! the pool mesh it is made of, placed where their transforms are every frame.
//!
//! A shader declares a `RaytracingAccelerationStructure` and is handed a scene by name, like any
//! other value, and traces it with `bcs_ray::trace_in`. A hit's instance is the slot, so an
//! instance buffer or a material buffer with the same entities in the same slots describes it, and
//! its instance ID is the pool mesh, which `bcs_scene` reads the triangle's corners from.
//!
//! **Where it is built.** The render world, which holds the device, builds each new mesh's
//! structure once the pool's buffers are on the GPU, and the top-level structure again every
//! frame from where the entities are. The result is kept in a table outside the world, which the
//! bind groups of the frame's dispatches read from, as [`super::programs`] keeps its table.
//!
//! A pool's buffers are made usable as ray tracing input when the device can trace rays, since a
//! device without ray queries refuses that usage.

#![cfg(feature = "render")]

use std::collections::HashMap;
use std::sync::RwLock;

use bevy::app::App;
use bevy::asset::Handle;
use bevy::ecs::entity::Entity;
use bevy::ecs::resource::Resource;
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::system::{Res, ResMut};
use bevy::ecs::world::World;
use bevy::math::Mat4;
use bevy::render::render_asset::RenderAssets;
use bevy::render::render_resource::{
    AccelerationStructureFlags, AccelerationStructureGeometryFlags, AccelerationStructureUpdateMode,
    Blas, BlasBuildEntry, BlasGeometries, BlasGeometrySizeDescriptors, BlasTriangleGeometry,
    BlasTriangleGeometrySizeDescriptor, CommandEncoderDescriptor, CreateBlasDescriptor,
    CreateTlasDescriptor, IndexFormat, Tlas, TlasInstance, VertexFormat,
};
use bevy::render::renderer::{RenderDevice, RenderQueue};
use bevy::render::settings::WgpuFeatures;
use bevy::render::storage::{GpuShaderBuffer, ShaderBuffer};
use bevy::render::{ExtractSchedule, MainWorld, Render, RenderApp, RenderSystems};
use bevy::transform::components::GlobalTransform;

use crate::interop::status;

/// Whether the device can build acceleration structures and trace rays through them.
pub fn supported(world: &World) -> bool {
    world
        .get_resource::<RenderDevice>()
        .is_some_and(|device| device.features().contains(WgpuFeatures::EXPERIMENTAL_RAY_QUERY))
}

/// One scene as the game describes it: which pool its meshes are, and what is in each slot.
struct Scene {
    /// The pool, by the key of its mesh table.
    pool: i32,
    /// An entity and the pool mesh it is made of, per slot.
    slots: Vec<Option<(Entity, u32)>>,
}

/// Every ray scene, by its key.
#[derive(Resource, Default)]
pub struct RayScenes {
    next: i32,
    scenes: HashMap<i32, Scene>,
}

/// The top-level structure of every scene built so far, by key, for the bind groups of the frame's
/// dispatches to bind. Emptied when an app is built, as the program table is.
static BUILT: RwLock<Vec<(i32, Tlas)>> = RwLock::new(Vec::new());

/// The top-level structure of scene `key`, once the render world has built it.
pub fn tlas(key: i32) -> Option<Tlas> {
    BUILT
        .read()
        .ok()?
        .iter()
        .find(|(built, _)| *built == key)
        .map(|(_, tlas)| tlas.clone())
}

/// Makes an empty scene over the pool whose mesh table is `pool`, with room for `capacity`
/// instances, and answers its key.
///
/// Returns [`status::UNSUPPORTED`] on a device without ray queries and [`status::NO_COMPONENT`]
/// where `pool` names no pool.
pub fn create(world: &mut World, pool: i32, capacity: u32) -> i32 {
    if !supported(world) {
        return status::UNSUPPORTED;
    }

    if super::pools::geometry(world, pool).is_none() {
        return status::NO_COMPONENT;
    }

    let mut scenes = world.get_resource_or_init::<RayScenes>();
    scenes.next += 1;
    let key = scenes.next;

    scenes.scenes.insert(
        key,
        Scene {
            pool,
            slots: vec![None; capacity.max(1) as usize],
        },
    );

    key
}

/// Puts `entity`, made of pool mesh `mesh`, in `slot` of scene `key`, or empties the slot where
/// `entity` is `None`.
///
/// Returns [`status::NO_COMPONENT`] for a scene that does not exist and [`status::NULL_ARG`] for a
/// slot past the scene's capacity.
pub fn set(world: &mut World, key: i32, slot: u32, entity: Option<Entity>, mesh: u32) -> i32 {
    let Some(mut scenes) = world.get_resource_mut::<RayScenes>() else {
        return status::NO_COMPONENT;
    };

    let Some(scene) = scenes.scenes.get_mut(&key) else {
        return status::NO_COMPONENT;
    };

    let Some(place) = scene.slots.get_mut(slot as usize) else {
        return status::NULL_ARG;
    };

    *place = entity.map(|entity| (entity, mesh));
    status::OK
}

// -- The render world's half

/// One scene as the render world receives it.
struct Extracted {
    key: i32,
    vertices: Handle<ShaderBuffer>,
    indices: Handle<ShaderBuffer>,
    /// Where each pool mesh's vertices and indices start and how many there are.
    meshes: Vec<[u32; 4]>,
    capacity: u32,
    /// A slot, the pool mesh in it, and its transform as the first three rows of its matrix.
    instances: Vec<(u32, u32, [f32; 12])>,
}

#[derive(Resource, Default)]
struct ExtractedScenes(Vec<Extracted>);

/// What the render world has built of one scene.
struct Built {
    tlas: Tlas,
    capacity: u32,
    /// Each pool mesh's bottom-level structure, by its number, once built.
    blas: Vec<Option<Blas>>,
}

#[derive(Resource, Default)]
struct BuiltScenes(HashMap<i32, Built>);

pub fn install(app: &mut App) {
    if let Ok(mut built) = BUILT.write() {
        built.clear();
    }

    app.init_resource::<RayScenes>();

    let Some(render_app) = app.get_sub_app_mut(RenderApp) else {
        return;
    };

    render_app
        .init_resource::<ExtractedScenes>()
        .init_resource::<BuiltScenes>()
        .add_systems(ExtractSchedule, extract_scenes)
        .add_systems(Render, build_scenes.in_set(RenderSystems::PrepareResources));
}

/// Takes every scene's pool and where its entities are over to the render world.
fn extract_scenes(mut main_world: ResMut<MainWorld>, mut extracted: ResMut<ExtractedScenes>) {
    extracted.0.clear();

    let world: &mut World = &mut main_world;

    let Some(scenes) = world.get_resource::<RayScenes>() else {
        return;
    };

    let described: Vec<(i32, i32, Vec<Option<(Entity, u32)>>)> = scenes
        .scenes
        .iter()
        .map(|(key, scene)| (*key, scene.pool, scene.slots.clone()))
        .collect();

    let mut transforms = world.query::<&GlobalTransform>();

    for (key, pool, slots) in described {
        let Some((vertices, indices, meshes)) = super::pools::geometry(world, pool) else {
            continue;
        };

        let instances = slots
            .iter()
            .enumerate()
            .filter_map(|(slot, place)| {
                let (entity, mesh) = (*place)?;

                // A mesh the pool does not have yet, or an entity with nowhere to be, is left out
                // this frame rather than put at the origin.
                if mesh as usize >= meshes.len() {
                    return None;
                }

                let transform = transforms.get(world, entity).ok()?.to_matrix();
                Some((slot as u32, mesh, rows(&transform)))
            })
            .collect();

        extracted.0.push(Extracted {
            key,
            vertices,
            indices,
            meshes,
            capacity: slots.len() as u32,
            instances,
        });
    }
}

/// The first three rows of a matrix, which is what an instance of a top-level structure holds.
fn rows(matrix: &Mat4) -> [f32; 12] {
    matrix.transpose().to_cols_array()[..12]
        .try_into()
        .expect("twelve of sixteen")
}

/// Builds each new pool mesh's bottom-level structure, and every scene's top-level structure from
/// where its entities are this frame.
fn build_scenes(
    extracted: Res<ExtractedScenes>,
    mut built: ResMut<BuiltScenes>,
    buffers: Res<RenderAssets<GpuShaderBuffer>>,
    device: Res<RenderDevice>,
    queue: Res<RenderQueue>,
) {
    if extracted.0.is_empty() {
        return;
    }

    let mut encoder = device.create_command_encoder(&CommandEncoderDescriptor {
        label: Some("bcs_ray_scenes"),
    });

    for scene in &extracted.0 {
        let (Some(vertices), Some(indices)) = (buffers.get(&scene.vertices), buffers.get(&scene.indices))
        else {
            continue;
        };

        let entry = built.0.entry(scene.key).or_insert_with(|| Built {
            tlas: make_tlas(&device, scene.capacity),
            capacity: scene.capacity,
            blas: Vec::new(),
        });

        if entry.capacity < scene.capacity {
            entry.tlas = make_tlas(&device, scene.capacity);
            entry.capacity = scene.capacity;
        }

        // Each mesh is built once, from whichever buffers hold it when it is first seen, since a
        // built structure keeps its own copy of the triangles and does not read the pool again.
        let mut sizes = Vec::new();

        for (number, &[first_vertex, vertex_count, first_index, index_count]) in scene.meshes.iter().enumerate() {
            if entry.blas.get(number).is_some_and(Option::is_some) {
                continue;
            }

            let vertex_end = (first_vertex as u64 + vertex_count as u64) * super::pools::VERTEX_BYTES as u64;
            let index_end = (first_index as u64 + index_count as u64) * 4;

            // The pool's buffers are replaced when they grow, so the ones here may still be the
            // smaller ones from before the mesh was added.
            if vertices.buffer.size() < vertex_end || indices.buffer.size() < index_end || index_count == 0 {
                continue;
            }

            let size = BlasTriangleGeometrySizeDescriptor {
                vertex_format: VertexFormat::Float32x3,
                vertex_count,
                index_format: Some(IndexFormat::Uint32),
                index_count: Some(index_count),
                flags: AccelerationStructureGeometryFlags::OPAQUE,
            };

            let blas = device.wgpu_device().create_blas(
                &CreateBlasDescriptor {
                    label: Some("bcs_ray_scene_mesh"),
                    flags: AccelerationStructureFlags::PREFER_FAST_TRACE,
                    update_mode: AccelerationStructureUpdateMode::Build,
                },
                BlasGeometrySizeDescriptors::Triangles {
                    descriptors: vec![size.clone()],
                },
            );

            if entry.blas.len() <= number {
                entry.blas.resize(number + 1, None);
            }

            entry.blas[number] = Some(blas);
            sizes.push((number, size, first_vertex, first_index));
        }

        let geometries: Vec<(usize, BlasTriangleGeometry)> = sizes
            .iter()
            .map(|(number, size, first_vertex, first_index)| {
                (
                    *number,
                    BlasTriangleGeometry {
                        size,
                        vertex_buffer: &vertices.buffer,
                        first_vertex: *first_vertex,
                        vertex_stride: super::pools::VERTEX_BYTES as u64,
                        index_buffer: Some(&indices.buffer),
                        first_index: Some(*first_index),
                        transform_buffer: None,
                        transform_buffer_offset: None,
                    },
                )
            })
            .collect();

        let entries: Vec<BlasBuildEntry> = geometries
            .into_iter()
            .map(|(number, geometry)| BlasBuildEntry {
                blas: entry.blas[number].as_ref().expect("made above"),
                geometry: BlasGeometries::TriangleGeometries(vec![geometry]),
            })
            .collect();

        // wgpu packs the instances it builds, so a slot left empty would move every slot after it
        // down one, and a hit's instance would stop being its slot. Every empty slot holds a
        // stand-in with a mask of zero instead, which no ray meets.
        const IDENTITY: [f32; 12] = [1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0];
        let stand_in = entry.blas.iter().flatten().next().cloned();

        for slot in 0..entry.capacity as usize {
            entry.tlas[slot] = stand_in.as_ref().map(|blas| TlasInstance::new(blas, IDENTITY, 0, 0));
        }

        for (slot, mesh, transform) in &scene.instances {
            let Some(Some(blas)) = entry.blas.get(*mesh as usize) else {
                continue;
            };

            // The pool mesh as the instance's ID, where a shader finds which triangles it hit.
            entry.tlas[*slot as usize] = Some(TlasInstance::new(blas, *transform, *mesh, 0xFF));
        }

        encoder.build_acceleration_structures(&entries, [&entry.tlas]);
    }

    queue.submit([encoder.finish()]);

    if let Ok(mut published) = BUILT.write() {
        published.clear();
        published.extend(built.0.iter().map(|(key, built)| (*key, built.tlas.clone())));
    }
}

fn make_tlas(device: &RenderDevice, capacity: u32) -> Tlas {
    device.wgpu_device().create_tlas(&CreateTlasDescriptor {
        label: Some("bcs_ray_scene"),
        max_instances: capacity.max(1),
        flags: AccelerationStructureFlags::PREFER_FAST_TRACE,
        update_mode: AccelerationStructureUpdateMode::Build,
    })
}
