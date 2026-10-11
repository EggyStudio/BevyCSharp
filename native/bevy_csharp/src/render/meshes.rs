//! Meshes made from the vertices C# hands over, written over in place, and read back.

use crate::interop::status;
#[cfg(feature = "render")]
use crate::state::with_world;


/// What a mesh holds, without its vertices.
///
/// The counts a tool shows and the attributes a shader can rely on, read in one call rather than
/// by copying every vertex across to count them.
#[repr(C)]
#[derive(Clone, Copy, Default)]
pub struct BcsMeshInfo {
    /// How many vertices.
    pub vertices: u32,
    /// How many indices, or `0` for a mesh drawn without them.
    pub indices: u32,
    /// `16` or `32` for the width of an index, or `0` for none.
    pub index_bits: u32,
    /// `0` triangles, `1` a triangle strip, `2` lines, `3` a line strip, `4` points.
    pub topology: u32,
    /// Which attributes it has, one bit each: `1` normals, `2` tangents, `4` UVs, `8` a second UV
    /// set, `16` vertex colors, `32` joint indices, `64` joint weights, `128` a stream of the game's
    /// own.
    pub attributes: u32,
    /// The corner of its bounds with the smallest coordinates.
    pub min: [f32; 3],
    /// The corner with the largest.
    pub max: [f32; 3],
}

/// Reads what a mesh holds: its counts, its attributes and its bounds.
///
/// # Safety
/// `out` must be writable for one [`BcsMeshInfo`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_mesh_info(handle: i32, out: *mut BcsMeshInfo) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        // Every profile has meshes, so this is not behind the renderer as the rest of the file is.
        crate::state::with_world(|world| {
            use bevy::asset::Assets;
            use bevy::mesh::{Indices, Mesh, PrimitiveTopology};

            let Some(handle) = crate::assets::clone_handle(world, handle) else {
                return status::NOT_PRESENT;
            };
            let Ok(handle) = handle.try_typed::<Mesh>() else {
                return status::INVALID_STATE;
            };
            let Some(meshes) = world.get_resource::<Assets<Mesh>>() else {
                return status::UNSUPPORTED;
            };
            // A mesh still loading, or one kept only in the render world, has nothing to read here.
            let Some(mesh) = meshes.get(&handle) else {
                return status::NOT_PRESENT;
            };

            let mut info = BcsMeshInfo {
                vertices: mesh.count_vertices() as u32,
                ..Default::default()
            };

            (info.indices, info.index_bits) = match mesh.indices() {
                Some(Indices::U16(indices)) => (indices.len() as u32, 16),
                Some(Indices::U32(indices)) => (indices.len() as u32, 32),
                None => (0, 0),
            };

            info.topology = match mesh.primitive_topology() {
                PrimitiveTopology::TriangleList => 0,
                PrimitiveTopology::TriangleStrip => 1,
                PrimitiveTopology::LineList => 2,
                PrimitiveTopology::LineStrip => 3,
                PrimitiveTopology::PointList => 4,
            };

            let attributes = [
                (Mesh::ATTRIBUTE_NORMAL, 1),
                (Mesh::ATTRIBUTE_TANGENT, 2),
                (Mesh::ATTRIBUTE_UV_0, 4),
                (Mesh::ATTRIBUTE_UV_1, 8),
                (Mesh::ATTRIBUTE_COLOR, 16),
                (Mesh::ATTRIBUTE_JOINT_INDEX, 32),
                (Mesh::ATTRIBUTE_JOINT_WEIGHT, 64),
            ];
            for (attribute, bit) in attributes {
                if mesh.contains_attribute(attribute) {
                    info.attributes |= bit;
                }
            }
            if super::streams::all().iter().any(|stream| mesh.contains_attribute(stream.attribute)) {
                info.attributes |= 128;
            }

            let positions = mesh.attribute(Mesh::ATTRIBUTE_POSITION).and_then(|p| p.as_float3());
            if let Some(positions) = positions {
                let mut min = [f32::MAX; 3];
                let mut max = [f32::MIN; 3];
                for position in positions {
                    for axis in 0..3 {
                        min[axis] = min[axis].min(position[axis]);
                        max[axis] = max[axis].max(position[axis]);
                    }
                }
                if !positions.is_empty() {
                    (info.min, info.max) = (min, max);
                }
            }

            unsafe { out.write(info) };
            status::OK
        })
    })
}

/// Writes the names a mesh gives its morph targets into `out`, a line each in their order, and
/// answers the length in bytes, nothing where it names none.
///
/// Answers [`status::NOT_PRESENT`] for a mesh still loading and [`status::INVALID_STATE`] for a key
/// that names something else. A glTF file names its targets in its extras, which Bevy reads.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_morph_target_names(mesh: i32, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        // Every profile has meshes, as for the counts above.
        crate::state::with_world(|world| {
            use bevy::asset::Assets;
            use bevy::mesh::Mesh;

            let Some(handle) = crate::assets::clone_handle(world, mesh) else {
                return status::NOT_PRESENT;
            };
            let Ok(handle) = handle.try_typed::<Mesh>() else {
                return status::INVALID_STATE;
            };
            let Some(mesh) = world.get_resource::<Assets<Mesh>>().and_then(|meshes| meshes.get(&handle)) else {
                return status::NOT_PRESENT;
            };

            // Bevy builds morph targets where it draws, so a headless build's meshes have none.
            #[cfg(feature = "render")]
            let names = mesh.morph_target_names().map_or_else(String::new, |names| names.join("\n"));
            #[cfg(not(feature = "render"))]
            let names = {
                let _ = mesh;
                String::new()
            };
            unsafe { crate::interop::write_text(&names, out, capacity) }
        })
    })
}

/// Works out tangents for a mesh that has none, from its normals and texture coordinates, which
/// a normal map and anisotropy read, as Bevy's `with_generated_tangents` does for a primitive.
///
/// Answers `OK`, a mesh with tangents already keeping them, `INVALID_STATE` where it lacks what
/// they are worked out from, normals, texture coordinates or indexed triangles, or `UNSUPPORTED`
/// in a headless bridge.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_mesh_generate_tangents(handle: i32) -> i32 {
    crate::interop::guard(|| {
        // Every profile has meshes, as `bcs_render_mesh_info` says.
        crate::state::with_world(|world| {
            use bevy::asset::Assets;
            use bevy::mesh::Mesh;

            let Some(handle) = crate::assets::clone_handle(world, handle) else {
                return status::NOT_PRESENT;
            };
            let Ok(handle) = handle.try_typed::<Mesh>() else {
                return status::INVALID_STATE;
            };
            let Some(mut meshes) = world.get_resource_mut::<Assets<Mesh>>() else {
                return status::UNSUPPORTED;
            };
            let Some(mesh) = meshes.get_mut(&handle) else {
                return status::NOT_PRESENT;
            };

            // Through the whole asset, which marks it changed, so the renderer takes the tangents
            // where a mutable borrow that writes nothing would leave it as it was.
            let mesh = mesh.into_inner();
            if mesh.attribute(Mesh::ATTRIBUTE_TANGENT).is_some() {
                return status::OK;
            }

            // Worked out by mikktspace, which comes with the renderer, so a headless bridge, which
            // draws nothing a tangent is read by, answers that it has none.
            #[cfg(feature = "render")]
            {
                match mesh.generate_tangents() {
                    Ok(()) => status::OK,
                    Err(_) => status::INVALID_STATE,
                }
            }

            #[cfg(not(feature = "render"))]
            {
                status::UNSUPPORTED
            }
        })
    })
}

/// A mesh described vertex by vertex.
///
/// Every array but `positions` may be null. Positions and normals are three floats a vertex, UVs
/// two and colors four, in linear RGBA.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsMeshData {
    pub positions: *const f32,
    pub vertex_count: i32,
    pub normals: *const f32,
    pub uvs: *const f32,
    pub colors: *const f32,
    /// Indices into the vertices, or null to take them in order.
    pub indices: *const u32,
    pub index_count: i32,
    /// `0` triangles, `1` lines, `2` points, `3` a line strip, `4` a triangle strip.
    pub topology: i32,
    /// Streams of the game's own (see [`super::streams`]), `stream_count` of them, or null.
    pub streams: *const super::streams::BcsMeshStream,
    pub stream_count: i32,
}

/// Builds a mesh from vertices and answers its asset key.
///
/// A triangle mesh given no normals has them worked out, smooth where it is indexed and flat
/// where it is not, because every lit material and every shader reading a normal would otherwise
/// read zeros. Built in any profile, since a mesh is data until something draws it.
///
/// Returns [`status::NULL_ARG`] where a count is negative, a pointer the count needs is null, or
/// an index names no vertex.
///
/// # Safety
/// `data` must point to a readable [`BcsMeshData`] whose arrays hold what their counts say.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_mesh_create_from(data: *const BcsMeshData) -> i32 {
    crate::interop::guard(|| {
        use bevy::asset::Assets;
        use bevy::mesh::Mesh;

        let mesh = match unsafe { mesh_from(data) } {
            Ok(mesh) => mesh,
            Err(code) => return code,
        };

        crate::state::with_world(|world| {
            let Some(mut meshes) = world.get_resource_mut::<Assets<Mesh>>() else {
                return status::UNSUPPORTED;
            };

            let handle = meshes.add(mesh).untyped();
            crate::assets::insert_handle(world, handle)
        })
    })
}

/// The mesh vertices describe, or [`status::NULL_ARG`] where a count is negative, a pointer the count
/// needs is null, or an index names no vertex.
///
/// # Safety
/// `data` must be null or point to a readable [`BcsMeshData`] whose arrays hold what their counts
/// say.
unsafe fn mesh_from(data: *const BcsMeshData) -> Result<bevy::mesh::Mesh, i32> {
    use bevy::asset::RenderAssetUsages;
    use bevy::mesh::{Indices, Mesh, PrimitiveTopology};

    if data.is_null() {
        return Err(status::NULL_ARG);
    }

    let data = unsafe { *data };

    if data.vertex_count <= 0
        || data.positions.is_null()
        || data.index_count < 0
        || (data.indices.is_null() && data.index_count > 0)
        || data.stream_count < 0
        || (data.streams.is_null() && data.stream_count > 0)
    {
        return Err(status::NULL_ARG);
    }

    let count = data.vertex_count as usize;
    let streams = unsafe { streams_of(&data, count)? };

    let floats = |pointer: *const f32, width: usize| -> Option<&[f32]> {
        (!pointer.is_null())
            .then(|| unsafe { core::slice::from_raw_parts(pointer, count * width) })
    };

    let topology = match data.topology {
        1 => PrimitiveTopology::LineList,
        2 => PrimitiveTopology::PointList,
        3 => PrimitiveTopology::LineStrip,
        4 => PrimitiveTopology::TriangleStrip,
        _ => PrimitiveTopology::TriangleList,
    };

    let positions = floats(data.positions, 3)
        .map(|all| all.chunks_exact(3).map(|p| [p[0], p[1], p[2]]).collect::<Vec<_>>())
        .unwrap_or_default();

    let mut mesh = Mesh::new(topology, RenderAssetUsages::default())
        .with_inserted_attribute(Mesh::ATTRIBUTE_POSITION, positions);

    if let Some(normals) = floats(data.normals, 3) {
        let normals: Vec<[f32; 3]> =
            normals.chunks_exact(3).map(|n| [n[0], n[1], n[2]]).collect();
        mesh.insert_attribute(Mesh::ATTRIBUTE_NORMAL, normals);
    }

    if let Some(uvs) = floats(data.uvs, 2) {
        let uvs: Vec<[f32; 2]> = uvs.chunks_exact(2).map(|t| [t[0], t[1]]).collect();
        mesh.insert_attribute(Mesh::ATTRIBUTE_UV_0, uvs);
    }

    if let Some(colors) = floats(data.colors, 4) {
        let colors: Vec<[f32; 4]> =
            colors.chunks_exact(4).map(|c| [c[0], c[1], c[2], c[3]]).collect();
        mesh.insert_attribute(Mesh::ATTRIBUTE_COLOR, colors);
    }

    if data.index_count > 0 {
        let indices =
            unsafe { core::slice::from_raw_parts(data.indices, data.index_count as usize) };

        // A strip is broken where an index is the largest there is, which starts the strip
        // again from the next, as the GPU reads it. Anywhere else that index names no vertex.
        let strip = matches!(topology, PrimitiveTopology::LineStrip | PrimitiveTopology::TriangleStrip);
        if indices
            .iter()
            .any(|index| *index as usize >= count && !(strip && *index == u32::MAX))
        {
            return Err(status::NULL_ARG);
        }

        mesh.insert_indices(Indices::U32(indices.to_vec()));
    }

    for (stream, bytes) in streams {
        mesh.insert_attribute(stream.attribute, stream.format.values(bytes));
    }

    if data.normals.is_null() && topology == PrimitiveTopology::TriangleList {
        // Smooth normals need indices, and flat ones need there to be none, so the mesh is
        // asked for whichever its shape allows.
        if mesh.indices().is_some() {
            mesh.compute_smooth_normals();
        } else {
            mesh.compute_flat_normals();
        }
    }

    Ok(mesh)
}

/// The streams of the game's own `data` carries, each registered (see [`super::streams`]) with the
/// bytes it brings, `count` vertices of its format.
///
/// [`status::NULL_ARG`] for a stream with no name or no bytes, of a format there is not, at a
/// location outside the streams' range or at one another stream of the mesh takes, and
/// [`status::INVALID_STATE`] for a name the running app gave another format or location.
///
/// # Safety
/// `data.streams` must hold `data.stream_count` streams whose names and bytes hold what they say.
unsafe fn streams_of<'a>(
    data: &BcsMeshData,
    count: usize,
) -> Result<Vec<(super::streams::Stream, &'a [u8])>, i32> {
    use super::streams::{FIRST_LOCATION, Format, LAST_LOCATION, register};

    if data.stream_count == 0 {
        return Ok(Vec::new());
    }

    let given = unsafe { core::slice::from_raw_parts(data.streams, data.stream_count as usize) };
    let mut taken = 0u32;
    let mut streams = Vec::with_capacity(given.len());

    for stream in given {
        let format = Format::from_code(stream.format).ok_or(status::NULL_ARG)?;
        let location = stream.location;

        if stream.name.is_null() || stream.name_len <= 0 || stream.data.is_null() {
            return Err(status::NULL_ARG);
        }
        if !(FIRST_LOCATION..=LAST_LOCATION).contains(&location) || taken & (1 << location) != 0 {
            return Err(status::NULL_ARG);
        }
        taken |= 1 << location;

        let name = unsafe { core::slice::from_raw_parts(stream.name, stream.name_len as usize) };
        let name = core::str::from_utf8(name).map_err(|_| status::NULL_ARG)?;

        // Read while the call lasts, which the caller's pinned arrays outlive.
        let bytes: &'a [u8] = unsafe { core::slice::from_raw_parts(stream.data, count * format.size()) };
        streams.push((register(name, format, location)?, bytes));
    }

    Ok(streams)
}

/// Writes vertices over the mesh a handle names, so everything drawn with it changes without being
/// pointed at a new one, as a mesh changed in place in Bevy does.
///
/// Returns what [`bcs_mesh_create_from`] refuses, and [`status::INVALID_STATE`] for a handle to
/// something other than a mesh.
///
/// # Safety
/// `data` must point to a readable [`BcsMeshData`] whose arrays hold what their counts say.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_mesh_write(handle: i32, data: *const BcsMeshData) -> i32 {
    crate::interop::guard(|| {
        use bevy::asset::Assets;
        use bevy::mesh::Mesh;

        let mesh = match unsafe { mesh_from(data) } {
            Ok(mesh) => mesh,
            Err(code) => return code,
        };

        crate::state::with_world(|world| {
            let Some(handle) = crate::assets::clone_handle(world, handle) else {
                return status::NO_COMPONENT;
            };
            let Ok(handle) = handle.try_typed::<Mesh>() else {
                return status::INVALID_STATE;
            };
            let Some(mut meshes) = world.get_resource_mut::<Assets<Mesh>>() else {
                return status::UNSUPPORTED;
            };

            // A skin's joints were given apart from the vertices, so a write of the same count of
            // vertices keeps them rather than leaving the mesh unskinned.
            let mut mesh = mesh;
            carry_joints(meshes.get(&handle), &mut mesh);

            match meshes.insert(&handle, mesh) {
                Ok(_) => status::OK,
                Err(_) => status::INVALID_STATE,
            }
        })
    })
}

/// Gives `mesh` the joints and weights of the mesh it is written over, where it has none of its own
/// and the same number of vertices, with the bounds a skinned mesh is culled by.
fn carry_joints(old: Option<&bevy::mesh::Mesh>, mesh: &mut bevy::mesh::Mesh) {
    use bevy::mesh::Mesh;

    let Some(old) = old else {
        return;
    };
    if mesh.contains_attribute(Mesh::ATTRIBUTE_JOINT_INDEX) || old.count_vertices() != mesh.count_vertices() {
        return;
    }

    if let (Some(joints), Some(weights)) =
        (old.attribute(Mesh::ATTRIBUTE_JOINT_INDEX), old.attribute(Mesh::ATTRIBUTE_JOINT_WEIGHT))
    {
        mesh.insert_attribute(Mesh::ATTRIBUTE_JOINT_INDEX, joints.clone());
        mesh.insert_attribute(Mesh::ATTRIBUTE_JOINT_WEIGHT, weights.clone());
        let _ = mesh.generate_skinned_mesh_bounds();
    }
}

/// Copies a mesh's triangles out: its positions, three floats each, and its indices, three a
/// triangle.
///
/// For whatever needs the shape rather than the picture, a physics engine building a collision
/// shape from a level above all. `counts` receives the number of positions and of indices first,
/// so a call with null buffers learns the sizes and a second copies. A mesh with no indices is
/// answered with its vertices taken in order.
///
/// Returns [`status::NOT_PRESENT`] while the mesh is still loading, [`status::NULL_ARG`] for one
/// that is not a list of triangles or has no positions, and [`status::BUFFER_TOO_SMALL`] where a
/// buffer is given that cannot hold it.
///
/// # Safety
/// `counts` must be writable for two integers. `positions` must be null or writable for
/// `position_capacity` floats, and `indices` null or writable for `index_capacity` integers.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_mesh_triangles(
    mesh: i32,
    positions: *mut f32,
    position_capacity: i32,
    indices: *mut u32,
    index_capacity: i32,
    counts: *mut i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (mesh, positions, position_capacity, indices, index_capacity, counts);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::mesh::{Indices, Mesh, PrimitiveTopology, VertexAttributeValues};

            if counts.is_null() {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let Some(handle) = crate::assets::clone_handle(world, mesh).and_then(|handle| handle.try_typed::<Mesh>().ok())
                else {
                    return status::NO_COMPONENT;
                };

                let Some(found) = world.resource::<Assets<Mesh>>().get(&handle) else {
                    return status::NOT_PRESENT;
                };

                if found.primitive_topology() != PrimitiveTopology::TriangleList {
                    return status::NULL_ARG;
                }

                let Some(VertexAttributeValues::Float32x3(corners)) = found.attribute(Mesh::ATTRIBUTE_POSITION) else {
                    return status::NULL_ARG;
                };

                let order: Vec<u32> = match found.indices() {
                    Some(Indices::U16(list)) => list.iter().map(|index| *index as u32).collect(),
                    Some(Indices::U32(list)) => list.clone(),
                    None => (0..corners.len() as u32).collect(),
                };

                unsafe {
                    counts.write(corners.len() as i32);
                    counts.add(1).write(order.len() as i32);
                }

                if positions.is_null() && indices.is_null() {
                    return status::OK;
                }

                if (position_capacity as usize) < corners.len() * 3 || (index_capacity as usize) < order.len() {
                    return status::BUFFER_TOO_SMALL;
                }

                unsafe {
                    core::ptr::copy_nonoverlapping(corners.as_ptr() as *const f32, positions, corners.len() * 3);
                    core::ptr::copy_nonoverlapping(order.as_ptr(), indices, order.len());
                }

                status::OK
            })
        }
    })
}

/// Copies a mesh's positions and the normal of each, three floats a vertex for both.
///
/// What a tool draws a mesh's normals from, a short line out of every vertex, which the positions
/// alone cannot give. `count` receives the number of vertices first, so a call with null buffers
/// learns the size and a second copies.
///
/// Returns [`status::NOT_PRESENT`] while the mesh is still loading, [`status::NULL_ARG`] for one
/// with no positions or no normals, and [`status::BUFFER_TOO_SMALL`] where a buffer is given that
/// cannot hold them.
///
/// # Safety
/// `count` must be writable for one integer. `positions` and `normals` must each be null or
/// writable for `capacity` floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_mesh_normals(
    mesh: i32,
    positions: *mut f32,
    normals: *mut f32,
    capacity: i32,
    count: *mut i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (mesh, positions, normals, capacity, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::mesh::{Mesh, VertexAttributeValues};

            if count.is_null() {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let Some(handle) = crate::assets::clone_handle(world, mesh).and_then(|handle| handle.try_typed::<Mesh>().ok())
                else {
                    return status::NO_COMPONENT;
                };

                let Some(found) = world.resource::<Assets<Mesh>>().get(&handle) else {
                    return status::NOT_PRESENT;
                };

                let (Some(VertexAttributeValues::Float32x3(corners)), Some(VertexAttributeValues::Float32x3(facing))) =
                    (found.attribute(Mesh::ATTRIBUTE_POSITION), found.attribute(Mesh::ATTRIBUTE_NORMAL))
                else {
                    return status::NULL_ARG;
                };

                unsafe { count.write(corners.len() as i32) };

                if positions.is_null() || normals.is_null() {
                    return status::OK;
                }

                if (capacity as usize) < corners.len() * 3 || facing.len() != corners.len() {
                    return status::BUFFER_TOO_SMALL;
                }

                unsafe {
                    core::ptr::copy_nonoverlapping(corners.as_ptr() as *const f32, positions, corners.len() * 3);
                    core::ptr::copy_nonoverlapping(facing.as_ptr() as *const f32, normals, facing.len() * 3);
                }

                status::OK
            })
        }
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use bevy::app::App;
    use bevy::asset::Assets;
    use bevy::mesh::{Mesh, MeshBuilder, Meshable};

    use crate::state::loan_world;

    fn app() -> App {
        let mut app = App::new();
        app.add_plugins(bevy::app::TaskPoolPlugin::default());
        app.add_plugins(bevy::asset::AssetPlugin::default());
        crate::assets::init_asset_once::<Mesh>(&mut app);
        app
    }

    #[test]
    fn a_cuboid_is_twenty_four_vertices_and_twelve_triangles() {
        // Four corners a face rather than eight a box, because each face has normals of its own.
        let mut app = app();
        let mesh = bevy::shape::Cuboid::new(1.0, 2.0, 3.0).mesh().build();
        let handle = app.world_mut().resource_mut::<Assets<Mesh>>().add(mesh).untyped();
        let key = crate::assets::key_for(app.world_mut(), handle);

        let mut info = BcsMeshInfo::default();
        let read = || unsafe { bcs_render_mesh_info(key, &mut info) };
        let code = loan_world(app.world_mut(), read);

        assert_eq!(status::OK, code);
        assert_eq!(24, info.vertices);
        assert_eq!(36, info.indices);
        assert_eq!(0, info.topology);
        assert_eq!(1 | 4, info.attributes & (1 | 4), "normals and UVs");
        assert_eq!([-0.5, -1.0, -1.5], info.min);
        assert_eq!([0.5, 1.0, 1.5], info.max);
    }

    #[cfg(feature = "render")]
    #[test]
    fn a_meshes_normals_come_back_with_its_positions() {
        let mut app = app();
        let mesh = bevy::shape::Cuboid::new(1.0, 1.0, 1.0).mesh().build();
        let handle = app.world_mut().resource_mut::<Assets<Mesh>>().add(mesh).untyped();
        let key = crate::assets::key_for(app.world_mut(), handle);

        let mut count = 0;
        let mut positions = vec![0.0f32; 24 * 3];
        let mut normals = vec![0.0f32; 24 * 3];
        let code = loan_world(app.world_mut(), || unsafe {
            assert_eq!(status::OK, bcs_render_mesh_normals(key, core::ptr::null_mut(), core::ptr::null_mut(), 0, &mut count));
            bcs_render_mesh_normals(key, positions.as_mut_ptr(), normals.as_mut_ptr(), 24 * 3, &mut count)
        });

        assert_eq!(status::OK, code);
        assert_eq!(24, count);

        // Every normal of a box is one unit along an axis.
        for normal in normals.chunks(3) {
            let length = (normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]).sqrt();
            assert!((length - 1.0).abs() < 1e-5);
        }
    }

    /// The managed mirror is checked against these same numbers, which stops the two drifting apart
    /// one field at a time.
    #[test]
    fn the_mesh_data_has_the_layout_the_managed_side_mirrors() {
        use core::mem::{offset_of, size_of};

        use crate::render::streams::BcsMeshStream;

        assert_eq!(offset_of!(BcsMeshData, topology), 52);
        assert_eq!(offset_of!(BcsMeshData, streams), 56);
        assert_eq!(offset_of!(BcsMeshData, stream_count), 64);
        assert_eq!(size_of::<BcsMeshData>(), 72);
        assert_eq!(offset_of!(BcsMeshStream, format), 12);
        assert_eq!(offset_of!(BcsMeshStream, location), 16);
        assert_eq!(offset_of!(BcsMeshStream, data), 24);
        assert_eq!(size_of::<BcsMeshStream>(), 32);
    }

    /// A mesh made with a stream of the game's own holds the registered attribute with the values
    /// given, and a second mesh bringing the same name in another format is refused.
    #[test]
    fn a_stream_lands_as_its_registered_attribute_and_another_shape_is_refused() {
        use crate::render::streams::{BcsMeshStream, Format, lookup};

        let positions = [0.0f32, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0];
        let words: Vec<u8> = [3u32, 5, 8].iter().flat_map(|word| word.to_ne_bytes()).collect();
        let name = "TestStreamMesh";

        let stream = |format: i32| BcsMeshStream {
            name: name.as_ptr(),
            name_len: name.len() as i32,
            format,
            location: 9,
            data: words.as_ptr(),
        };
        let data = |stream: &BcsMeshStream| BcsMeshData {
            positions: positions.as_ptr(),
            vertex_count: 3,
            normals: core::ptr::null(),
            uvs: core::ptr::null(),
            colors: core::ptr::null(),
            indices: core::ptr::null(),
            index_count: 0,
            topology: 0,
            streams: stream,
            stream_count: 1,
        };

        let uint = stream(Format::UInt.code());
        let mesh = unsafe { mesh_from(&data(&uint)) }.unwrap_or_else(|code| panic!("refused with {code}"));
        let registered = lookup(name).expect("the name is registered");

        assert_eq!(registered.location, 9);
        assert!(matches!(
            mesh.attribute(registered.attribute),
            Some(bevy::mesh::VertexAttributeValues::Uint32(values)) if values == &[3, 5, 8]
        ));

        let float = stream(Format::Float.code());
        assert!(matches!(unsafe { mesh_from(&data(&float)) }, Err(status::INVALID_STATE)));
    }
}
