//! Meshes made from the vertices C# hands over, and written over in place, with the streams of the
//! game's own they carry. What a mesh holds is read back in [`super::meshes`].

use crate::interop::status;

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
    /// How the mesh is made, four bytes of bits, [`LEAVE_NORMALS_OUT`].
    pub flags: u32,
}

/// A triangle mesh given no normals is left without them rather than given worked-out ones, for a
/// mesh whose vertex shader finds its normal in a stream of its own and never reads twelve bytes a
/// vertex it would otherwise carry.
pub const LEAVE_NORMALS_OUT: u32 = 1;

/// Builds a mesh from vertices and answers its asset key.
///
/// A triangle mesh given no normals has them worked out, smooth where it is indexed and flat
/// where it is not, because every lit material and every shader reading a normal would otherwise
/// read zeros, unless its flags say [`LEAVE_NORMALS_OUT`]. Built in any profile, since a mesh is
/// data until something draws it.
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

    if data.normals.is_null() && topology == PrimitiveTopology::TriangleList && data.flags & LEAVE_NORMALS_OUT == 0 {
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

#[cfg(test)]
mod tests {
    use super::*;

    /// The managed mirror is checked against these same numbers, which stops the two drifting apart
    /// one field at a time.
    #[test]
    fn the_mesh_data_has_the_layout_the_managed_side_mirrors() {
        use core::mem::{offset_of, size_of};

        use crate::render::streams::BcsMeshStream;

        assert_eq!(offset_of!(BcsMeshData, topology), 52);
        assert_eq!(offset_of!(BcsMeshData, streams), 56);
        assert_eq!(offset_of!(BcsMeshData, stream_count), 64);
        assert_eq!(offset_of!(BcsMeshData, flags), 68);
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
        let packed: Vec<u8> = [3u32, 5, 8].iter().flat_map(|value| value.to_ne_bytes()).collect();
        let name = "TestStreamMesh";

        let stream = |format: i32| BcsMeshStream {
            name: name.as_ptr(),
            name_len: name.len() as i32,
            format,
            location: 9,
            data: packed.as_ptr(),
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
            flags: 0,
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

    /// A triangle mesh given no normals is given worked-out ones, and one asked to leave them out
    /// holds none.
    #[test]
    fn a_mesh_asked_to_leave_its_normals_out_holds_none() {
        let positions = [0.0f32, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0];
        let data = |flags: u32| BcsMeshData {
            positions: positions.as_ptr(),
            vertex_count: 3,
            normals: core::ptr::null(),
            uvs: core::ptr::null(),
            colors: core::ptr::null(),
            indices: core::ptr::null(),
            index_count: 0,
            topology: 0,
            streams: core::ptr::null(),
            stream_count: 0,
            flags,
        };

        let worked_out = unsafe { mesh_from(&data(0)) }.expect("a plain mesh is made");
        let left_out = unsafe { mesh_from(&data(LEAVE_NORMALS_OUT)) }.expect("a mesh without normals is made");

        assert!(worked_out.contains_attribute(bevy::mesh::Mesh::ATTRIBUTE_NORMAL));
        assert!(!left_out.contains_attribute(bevy::mesh::Mesh::ATTRIBUTE_NORMAL));
    }
}
