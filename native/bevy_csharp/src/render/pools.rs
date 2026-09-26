//! Geometry pools: the vertices and triangles of many meshes in shared buffers, reachable from any
//! shader by a mesh's number and a triangle's.
//!
//! A ray traced in a compute shader, or a scene voxelized by one, meets triangles of whatever mesh
//! is there, so every mesh it might meet has to be in memory it can index, not in the vertex
//! buffers only a draw of that mesh binds. A pool is three buffers: every vertex, as
//! `bcs_scene::PoolVertex` (a position, a normal and texture coordinates, 48 bytes); every
//! triangle's three vertex numbers, counted from the start of its mesh; and a table of the meshes,
//! as `bcs_scene::PoolMesh`, saying where each one's vertices and indices start, how many there
//! are, and its bounds. Adding a mesh appends it, growing the buffers as needed and having
//! everything that holds them built against the grown ones.
//!
//! Put an entity's mesh in a pool and the entity in the same slot of an instance buffer and a
//! material buffer, and a shader has where each triangle is, how it is lit and what it is made of,
//! everything shading a ray's hit needs.

#![cfg(feature = "render")]

use std::collections::HashMap;

use bevy::asset::{Assets, Handle};
use bevy::ecs::resource::Resource;
use bevy::ecs::world::World;
use bevy::mesh::{Indices, Mesh, PrimitiveTopology, VertexAttributeValues};
use bevy::render::storage::ShaderBuffer;

use crate::interop::status;

/// Bytes of one vertex in a pool.
pub const VERTEX_BYTES: usize = 48;

/// Bytes of one entry in a pool's mesh table.
pub const MESH_BYTES: usize = 48;

/// One pool's contents, kept here as well so a mesh added is an append rather than a read back.
struct Pool {
    vertices: Handle<ShaderBuffer>,
    indices: Handle<ShaderBuffer>,
    meshes: Handle<ShaderBuffer>,
    vertex_bytes: Vec<u8>,
    index_bytes: Vec<u8>,
    mesh_bytes: Vec<u8>,
    vertex_count: u32,
    index_count: u32,
    mesh_count: u32,
}

/// Every pool, by the asset key of its mesh table.
#[derive(Resource, Default)]
pub struct GeometryPools(HashMap<i32, Pool>);

/// Makes an empty pool and answers the keys of its vertex, index and mesh buffers, in that order.
pub fn create(world: &mut World) -> Result<[i32; 3], i32> {
    let mut make = |bytes: usize| {
        let key = super::compute::create_buffer(world, &[], bytes as u64);
        if key <= 0 { Err(key) } else { Ok(key) }
    };

    // Room for one of each to start with, since a buffer is never empty.
    let keys = [make(VERTEX_BYTES)?, make(12)?, make(MESH_BYTES)?];

    let handle = |key: i32| super::compute::buffer_handle(world, key).ok_or(status::NO_COMPONENT);
    let (vertices, indices, meshes) = (handle(keys[0])?, handle(keys[1])?, handle(keys[2])?);

    world.get_resource_or_init::<GeometryPools>().0.insert(
        keys[2],
        Pool {
            vertices,
            indices,
            meshes,
            vertex_bytes: Vec::new(),
            index_bytes: Vec::new(),
            mesh_bytes: Vec::new(),
            vertex_count: 0,
            index_count: 0,
            mesh_count: 0,
        },
    );

    Ok(keys)
}

/// Adds the mesh `mesh` names to the pool whose mesh table is `pool`, and answers its number in
/// the pool.
///
/// Returns [`status::NOT_PRESENT`] where the mesh has not loaded yet, [`status::NULL_ARG`] for a
/// mesh that is not triangles or has no positions, and [`status::NO_COMPONENT`] where a key names
/// neither a pool nor a mesh.
pub fn add(world: &mut World, pool: i32, mesh: i32) -> i32 {
    let Some(handle) = crate::assets::clone_handle(world, mesh).and_then(|handle| handle.try_typed::<Mesh>().ok())
    else {
        return status::NO_COMPONENT;
    };

    if !world.get_resource::<GeometryPools>().is_some_and(|pools| pools.0.contains_key(&pool)) {
        return status::NO_COMPONENT;
    }

    let Some(found) = world.resource::<Assets<Mesh>>().get(&handle) else {
        return status::NOT_PRESENT;
    };

    let Some((vertices, indices, bounds)) = flatten(found) else {
        return status::NULL_ARG;
    };

    let mut pools = world.resource_mut::<GeometryPools>();
    let entry = pools.0.get_mut(&pool).expect("checked above");

    let number = entry.mesh_count;
    let first_vertex = entry.vertex_count;
    let first_index = entry.index_count;

    entry.vertex_bytes.extend_from_slice(bytemuck::cast_slice(&vertices));
    entry.index_bytes.extend_from_slice(bytemuck::cast_slice(&indices));

    let counts = [first_vertex, (vertices.len() / 12) as u32, first_index, indices.len() as u32];
    entry.mesh_bytes.extend_from_slice(bytemuck::cast_slice(&counts));
    entry
        .mesh_bytes
        .extend_from_slice(bytemuck::cast_slice(&[bounds[0], bounds[1], bounds[2], 0.0, bounds[3], bounds[4], bounds[5], 0.0]));

    entry.vertex_count += (vertices.len() / 12) as u32;
    entry.index_count += indices.len() as u32;
    entry.mesh_count += 1;

    let uploads = [
        (entry.vertices.clone(), entry.vertex_bytes.clone()),
        (entry.indices.clone(), entry.index_bytes.clone()),
        (entry.meshes.clone(), entry.mesh_bytes.clone()),
    ];

    for (buffer, bytes) in uploads {
        upload(world, &buffer, bytes);
    }

    number as i32
}

/// Puts `bytes` in a buffer, making it larger first where they do not fit, and having whatever
/// holds it built against the new one.
fn upload(world: &mut World, handle: &Handle<ShaderBuffer>, bytes: Vec<u8>) {
    let grew = {
        let mut assets = world.resource_mut::<Assets<ShaderBuffer>>();
        let Some(mut buffer) = assets.get_mut(handle) else { return };

        let size = buffer.buffer_description.size;
        let wanted = super::compute::buffer_size(bytes.len() as u64).max(size);

        let mut data = bytes;
        data.resize(wanted as usize, 0);

        buffer.buffer_description.size = wanted;
        buffer.copy_on_resize = false;
        buffer.data = Some(data);

        wanted != size
    };

    if grew {
        super::compute::rebind_buffer(world, handle);
    }
}

/// A mesh's vertices as three `float4`s each, its indices counted from its first vertex, and its
/// bounds, or `None` for one a pool cannot hold.
fn flatten(mesh: &Mesh) -> Option<(Vec<f32>, Vec<u32>, [f32; 6])> {
    if mesh.primitive_topology() != PrimitiveTopology::TriangleList {
        return None;
    }

    let Some(VertexAttributeValues::Float32x3(positions)) = mesh.attribute(Mesh::ATTRIBUTE_POSITION) else {
        return None;
    };

    let normals = match mesh.attribute(Mesh::ATTRIBUTE_NORMAL) {
        Some(VertexAttributeValues::Float32x3(normals)) => Some(normals),
        _ => None,
    };

    let uvs = match mesh.attribute(Mesh::ATTRIBUTE_UV_0) {
        Some(VertexAttributeValues::Float32x2(uvs)) => Some(uvs),
        _ => None,
    };

    let mut vertices = Vec::with_capacity(positions.len() * 12);
    let mut bounds = [f32::MAX, f32::MAX, f32::MAX, f32::MIN, f32::MIN, f32::MIN];

    for (index, position) in positions.iter().enumerate() {
        let normal = normals.and_then(|normals| normals.get(index)).copied().unwrap_or([0.0, 0.0, 0.0]);
        let uv = uvs.and_then(|uvs| uvs.get(index)).copied().unwrap_or([0.0, 0.0]);

        vertices.extend_from_slice(&[position[0], position[1], position[2], 1.0]);
        vertices.extend_from_slice(&[normal[0], normal[1], normal[2], 0.0]);
        vertices.extend_from_slice(&[uv[0], uv[1], 0.0, 0.0]);

        for axis in 0..3 {
            bounds[axis] = bounds[axis].min(position[axis]);
            bounds[axis + 3] = bounds[axis + 3].max(position[axis]);
        }
    }

    // A mesh without indices draws its vertices in order, three a triangle.
    let indices: Vec<u32> = match mesh.indices() {
        Some(Indices::U16(indices)) => indices.iter().map(|index| *index as u32).collect(),
        Some(Indices::U32(indices)) => indices.clone(),
        None => (0..positions.len() as u32).collect(),
    };

    Some((vertices, indices, bounds))
}

#[cfg(test)]
mod tests {
    use super::*;

    /// A slot of the vertex buffer and of the mesh table are the sizes `bcs_scene` declares.
    #[test]
    fn a_triangle_flattens_to_three_vertices_and_its_bounds() {
        let mut mesh = Mesh::new(PrimitiveTopology::TriangleList, Default::default());
        mesh.insert_attribute(Mesh::ATTRIBUTE_POSITION, vec![[0.0f32, 0.0, 0.0], [2.0, 0.0, 0.0], [0.0, 3.0, -1.0]]);

        let (vertices, indices, bounds) = flatten(&mesh).expect("a triangle list");

        assert_eq!(vertices.len() * 4, 3 * VERTEX_BYTES);
        assert_eq!(indices, vec![0, 1, 2]);
        assert_eq!(bounds, [0.0, 0.0, -1.0, 2.0, 3.0, 0.0]);
    }
}
