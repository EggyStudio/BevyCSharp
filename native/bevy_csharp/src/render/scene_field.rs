//! What the scene's distance field is built from, which the managed side reads each frame, the
//! meshes drawn that cast shadows, each with a number for its mesh, its matrix, its material's color
//! and emission, whether it has an inside and its bounds in its own space, the camera the field
//! lies around, and a pool of the meshes' triangles in one shader buffer.
//!
//! The managed side works out which cascades to build and from which meshes, as 3DEngine's
//! `SceneFieldPlan` does, and runs the field's compute shaders over this pool, so the bridge holds
//! only what it alone can read, the meshes' data and the entities drawing them.
//!
//! A mesh is a number the bridge gives it the first time it is gathered, with a generation that
//! moves on each time Bevy says its data changed, so a mesh written again in place is another mesh
//! to the plan, as a game's section meshed again after an edit is, and is pooled again.
//!
//! Each triangle is three corners in the pool, sixteen bytes each, its place in the mesh's own space
//! as three floats' bits, and in the fourth word its vertex's color as three sRGB bytes, white where
//! the mesh has none, with the bit above them set where the edge from the corner to the triangle's
//! next is open, shared by no other triangle by the places of its ends, which the splat reads to keep
//! a cell past an open mesh's rim in front of it.

use crate::interop::status;

/// A mesh drawn that casts shadows, as [`bcs_scene_field_gather`] writes it.
///
/// The offsets are fixed and written out on the managed side, which a test of both sides holds to
/// the same numbers.
#[repr(C)]
#[derive(Clone, Copy, Default, Debug)]
pub struct BcsFieldDraw {
    /// The mesh's number, the same for every entity drawing it.
    pub mesh: u32,
    /// How many times Bevy said the mesh's data changed since it was numbered.
    pub generation: u32,
    /// Bit 0 where the material has no back faces to cull, so the mesh has no inside, bit 1 where
    /// the mesh is skinned, so its shape moves whatever its matrix does.
    pub flags: u32,
    /// Nothing, so the rows start on sixteen bytes.
    pub _pad: u32,
    /// The rows of the matrix from the mesh's own space to the world's, three of four.
    pub rows: [f32; 12],
    /// The material's base color in linear light, white for a material the bridge cannot read.
    pub color: [f32; 4],
    /// The material's emission in linear light.
    pub emission: [f32; 4],
    /// The mesh's bounds in its own space, the least corner, then the greatest.
    pub low: [f32; 4],
    pub high: [f32; 4],
}

/// The camera the field lies around, as [`bcs_scene_field_gather`] writes it.
#[repr(C)]
#[derive(Clone, Copy, Default, Debug)]
pub struct BcsFieldEye {
    /// Where it is.
    pub position: [f32; 3],
    /// 1 where there is a camera, 0 where there is none and nothing else here is meant.
    pub found: u32,
    /// The way it faces, a length of one.
    pub forward: [f32; 3],
    /// How far its far plane lies, which the field's far cascade reaches no further than, or
    /// infinity where its projection has none.
    pub far: f32,
    /// The camera's entity, its bits, which the field's contact shadows are drawn on.
    pub camera: u64,
}

/// Writes the meshes drawn this frame that cast shadows to `out`, as many as `capacity` holds,
/// how many there are to `count`, which may be more, and the camera the field lies around to
/// `eye`. A caller given fewer than there are calls again with room for them.
///
/// A mesh is drawn where its entity is visible down its hierarchy, whatever a view's frustum
/// leaves out, since the field reaches behind the camera as well.
///
/// # Safety
/// `out` must be writable for `capacity` draws, or null where `capacity` is zero; `eye` for a
/// [`BcsFieldEye`] and `count` for an `i32`.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_scene_field_gather(
    out: *mut BcsFieldDraw,
    capacity: i32,
    eye: *mut BcsFieldEye,
    count: *mut i32,
) -> i32 {
    crate::interop::guard(|| {
        if eye.is_null() || count.is_null() || capacity < 0 || (out.is_null() && capacity > 0) {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        crate::state::with_world(|world| {
            let room: &mut [BcsFieldDraw] =
                if capacity > 0 { unsafe { core::slice::from_raw_parts_mut(out, capacity as usize) } } else { &mut [] };
            let (found, seen) = gather(world, room);
            unsafe {
                eye.write(seen);
                count.write(found.min(i32::MAX as usize) as i32);
            }
            status::OK
        })
    })
}

/// Puts the meshes `meshes` names in the pool where they are not, a number and a generation each,
/// and writes to `out` where each one's triangles start in the pool and how many it has, none for
/// a mesh whose data the bridge cannot read. Answers the pool's buffer key.
///
/// The pool is made again holding only the meshes asked for where those it holds would not fit
/// beside them in its buffer, so meshes no longer built from leave it, and a caller asks for every
/// mesh a frame's builds read each time, since a place answered before may have moved.
///
/// # Safety
/// `meshes` must be readable for `count` pairs of `u32`, and `out` writable for as many.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_scene_field_pool(meshes: *const u32, count: i32, out: *mut u32) -> i32 {
    crate::interop::guard(|| {
        if count < 0 || (count > 0 && (meshes.is_null() || out.is_null())) {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        crate::state::with_world(|world| {
            let asked: &[u32] = if count > 0 { unsafe { core::slice::from_raw_parts(meshes, count as usize * 2) } } else { &[] };
            let answers: &mut [u32] =
                if count > 0 { unsafe { core::slice::from_raw_parts_mut(out, count as usize * 2) } } else { &mut [] };
            pool(world, asked, answers)
        })
    })
}

/// Writes the bytes of the pool's buffer to `out`, sixteen a corner of its capacity, zero before it
/// was made.
///
/// # Safety
/// `out` must be writable for an `i64`.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_scene_field_pool_bytes(out: *mut i64) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        crate::state::with_world(|world| {
            let bytes = world.get_resource::<FieldPool>().map_or(0, |pool| (pool.capacity * CORNER_BYTES) as i64);
            unsafe { out.write(bytes) };
            status::OK
        })
    })
}

#[cfg(feature = "render")]
pub use built::*;

#[cfg(feature = "render")]
mod built {
    use std::collections::HashMap;

    use bevy::asset::{AssetEvent, AssetId, Assets, Handle};
    use bevy::color::ColorToComponents;
    use bevy::ecs::message::MessageReader;
    use bevy::ecs::resource::Resource;
    use bevy::ecs::system::ResMut;
    use bevy::ecs::world::World;
    use bevy::mesh::{Indices, Mesh, PrimitiveTopology, VertexAttributeValues};
    use bevy::render::storage::ShaderBuffer;

    use super::{BcsFieldDraw, BcsFieldEye};
    use crate::interop::status;

    /// Bytes of one corner in the pool.
    pub const CORNER_BYTES: usize = 16;

    /// The bit of a corner's fourth word set where the edge from it to its triangle's next corner
    /// is open.
    pub const OPEN_EDGE: u32 = 1 << 24;

    /// The meshes the field has met, each by its number, with its generation and its bounds once
    /// worked out, for a mesh whose entity has no bounds of Bevy's.
    #[derive(Resource, Default)]
    pub struct FieldMeshes {
        numbers: HashMap<AssetId<Mesh>, u32>,
        ids: Vec<AssetId<Mesh>>,
        generations: Vec<u32>,
        bounds: Vec<Option<[f32; 6]>>,
    }

    impl FieldMeshes {
        /// The number of a mesh, given it the first time it is met.
        fn number(&mut self, id: AssetId<Mesh>) -> u32 {
            if let Some(&number) = self.numbers.get(&id) {
                return number;
            }

            let number = self.ids.len() as u32;
            self.numbers.insert(id, number);
            self.ids.push(id);
            self.generations.push(0);
            self.bounds.push(None);
            number
        }
    }

    /// The pool of triangles, kept here as well so a mesh added is an append rather than a read
    /// back, with where each mesh's triangles start and how many it has, by its number and
    /// generation.
    #[derive(Resource, Default)]
    pub struct FieldPool {
        buffer: Option<(i32, Handle<ShaderBuffer>)>,
        corners: Vec<[u32; 4]>,
        placed: HashMap<(u32, u32), (u32, u32)>,
        pub(super) capacity: usize,
    }

    /// Numbers the meshes and keeps their generations, from nothing for a new app.
    pub fn install(app: &mut bevy::app::App) {
        app.init_resource::<FieldMeshes>();
        app.init_resource::<FieldPool>();
        app.add_systems(bevy::app::Last, track);
    }

    /// Moves a mesh's generation on where Bevy says its data changed or it went, so the plan takes
    /// it as another mesh and the pool holds it again.
    fn track(mut events: MessageReader<AssetEvent<Mesh>>, mut meshes: ResMut<FieldMeshes>) {
        for event in events.read() {
            let (AssetEvent::Modified { id } | AssetEvent::Removed { id }) = event else {
                continue;
            };

            if let Some(&number) = meshes.numbers.get(id) {
                meshes.generations[number as usize] = meshes.generations[number as usize].wrapping_add(1);
                meshes.bounds[number as usize] = None;
            }
        }
    }

    /// The meshes drawn that cast shadows, as many as `out` holds, how many there are, and the
    /// camera the field lies around.
    pub fn gather(world: &mut World, out: &mut [BcsFieldDraw]) -> (usize, BcsFieldEye) {
        use bevy::camera::primitives::Aabb;
        use bevy::camera::visibility::InheritedVisibility;
        use bevy::ecs::query::{Has, Without};
        use bevy::light::NotShadowCaster;
        use bevy::mesh::Mesh3d;
        use bevy::mesh::skinning::SkinnedMesh;
        use bevy::pbr::{MeshMaterial3d, StandardMaterial};
        use bevy::transform::components::GlobalTransform;

        let mut eye = BcsFieldEye::default();
        if let Some((camera, place, forward, _)) = crate::render::scene_counts::view_camera(world) {
            eye.position = place.to_array();
            eye.forward = forward.to_array();
            eye.found = 1;
            eye.camera = camera.to_bits();
            eye.far = match world.get::<bevy::camera::Projection>(camera) {
                Some(bevy::camera::Projection::Perspective(perspective)) => perspective.far,
                Some(bevy::camera::Projection::Orthographic(orthographic)) => orthographic.far,
                _ => f32::INFINITY,
            };
        }

        world.init_resource::<FieldMeshes>();
        let mut query = world.query_filtered::<(
            &Mesh3d,
            &GlobalTransform,
            &InheritedVisibility,
            Option<&MeshMaterial3d<StandardMaterial>>,
            Option<&Aabb>,
            Has<SkinnedMesh>,
        ), Without<NotShadowCaster>>();

        let found = world.resource_scope::<FieldMeshes, usize>(|world, mut meshes| {
            let materials = world.get_resource::<Assets<StandardMaterial>>();
            let data = world.get_resource::<Assets<Mesh>>();
            let mut found = 0usize;

            for (mesh, place, visible, material, bounds, skinned) in query.iter(world) {
                if !visible.get() {
                    continue;
                }

                let number = meshes.number(mesh.id());
                let index = found;
                found += 1;
                let Some(draw) = out.get_mut(index) else { continue };

                // Bevy's bounds of the entity where it worked them out, and the mesh's own,
                // kept for the next frame, where it has none.
                let (low, high) = match bounds {
                    Some(aabb) => ((aabb.center - aabb.half_extents).to_array(), (aabb.center + aabb.half_extents).to_array()),
                    None => {
                        let kept = &mut meshes.bounds[number as usize];
                        if kept.is_none() {
                            *kept = data.and_then(|data| data.get(&mesh.0)).and_then(bounds_of);
                        }
                        let b = kept.unwrap_or([0.0; 6]);
                        ([b[0], b[1], b[2]], [b[3], b[4], b[5]])
                    }
                };

                let affine = place.affine();
                let (x, y, z, t) = (affine.matrix3.x_axis, affine.matrix3.y_axis, affine.matrix3.z_axis, affine.translation);

                let standard = material.and_then(|material| materials.and_then(|materials| materials.get(&material.0)));
                let color = standard.map_or([1.0; 4], |standard| standard.base_color.to_linear().to_f32_array());
                let emission = standard.map_or([0.0; 4], |standard| standard.emissive.to_f32_array());
                let open = standard.is_some_and(|standard| standard.double_sided || standard.cull_mode.is_none());

                *draw = BcsFieldDraw {
                    mesh: number,
                    generation: meshes.generations[number as usize],
                    flags: u32::from(open) | (u32::from(skinned) << 1),
                    _pad: 0,
                    rows: [x.x, y.x, z.x, t.x, x.y, y.y, z.y, t.y, x.z, y.z, z.z, t.z],
                    color: [color[0], color[1], color[2], 1.0],
                    emission: [emission[0], emission[1], emission[2], 0.0],
                    low: [low[0], low[1], low[2], 0.0],
                    high: [high[0], high[1], high[2], 0.0],
                };
            }

            found
        });

        (found, eye)
    }

    /// Pools the meshes asked for, a number and a generation each, writing where each starts and
    /// how many triangles it has, and answers the pool's buffer key.
    pub fn pool(world: &mut World, asked: &[u32], answers: &mut [u32]) -> i32 {
        world.init_resource::<FieldMeshes>();
        world.init_resource::<FieldPool>();

        // Each mesh by its number and its generation as it stands, since a mesh changed after the
        // caller gathered it is pooled as it is now.
        let wanted: Vec<(u32, u32, Option<AssetId<Mesh>>)> = {
            let meshes = world.resource::<FieldMeshes>();
            asked
                .chunks_exact(2)
                .map(|pair| {
                    let number = pair[0];
                    let known = (number as usize) < meshes.ids.len();
                    let generation = if known { meshes.generations[number as usize] } else { pair[1] };
                    (number, generation, known.then(|| meshes.ids[number as usize]))
                })
                .collect()
        };

        // The triangles of the meshes not yet pooled, read from Bevy's copy of each.
        let mut adding: Vec<((u32, u32), Vec<[u32; 4]>)> = Vec::new();
        {
            let pool = world.resource::<FieldPool>();
            let data = world.get_resource::<Assets<Mesh>>();
            for &(number, generation, id) in &wanted {
                if pool.placed.contains_key(&(number, generation)) || adding.iter().any(|(key, _)| *key == (number, generation)) {
                    continue;
                }
                let corners = id.and_then(|id| data.and_then(|data| data.get(id))).and_then(corners_of).unwrap_or_default();
                adding.push(((number, generation), corners));
            }
        }

        let mut pool = world.resource_mut::<FieldPool>();
        let more: usize = adding.iter().map(|(_, corners)| corners.len()).sum();
        let changed = !adding.is_empty() || pool.buffer.is_none();

        // Made again with the meshes asked for where the rest would not fit beside them, which
        // drops the meshes no longer built from, at twice their size so the next few fit.
        if pool.corners.len() + more > pool.capacity {
            let keep: Vec<(u32, u32)> = wanted.iter().map(|&(number, generation, _)| (number, generation)).collect();
            let old = std::mem::take(&mut pool.corners);
            let placed = std::mem::take(&mut pool.placed);
            for key in keep {
                if let Some(&(first, triangles)) = placed.get(&key)
                    && !pool.placed.contains_key(&key)
                {
                    let start = pool.corners.len() as u32 / 3;
                    let from = first as usize * 3;
                    pool.corners.extend_from_slice(&old[from..from + triangles as usize * 3]);
                    pool.placed.insert(key, (start, triangles));
                }
            }
            pool.capacity = ((pool.corners.len() + more) * 2).max(3 * 1024);
        }

        for (key, corners) in adding {
            let start = pool.corners.len() as u32 / 3;
            pool.placed.insert(key, (start, corners.len() as u32 / 3));
            pool.corners.extend_from_slice(&corners);
        }

        for (answer, &(number, generation, _)) in answers.chunks_exact_mut(2).zip(&wanted) {
            let (first, triangles) = pool.placed.get(&(number, generation)).copied().unwrap_or((0, 0));
            answer[0] = first;
            answer[1] = triangles;
        }

        // Sent to the GPU again only where it gained a mesh, since a pool is sent whole.
        let bytes: Option<Vec<u8>> = changed.then(|| {
            let mut bytes: Vec<u8> = bytemuck::cast_slice(&pool.corners).to_vec();
            bytes.resize(pool.capacity * CORNER_BYTES, 0);
            bytes
        });
        let made = pool.buffer.clone();
        drop(pool);

        let (key, handle) = match made {
            Some(made) => made,
            None => {
                let key = crate::render::compute::create_buffer(world, &[], (CORNER_BYTES * 3 * 1024) as u64);
                if key <= 0 {
                    return key;
                }
                let Some(handle) = crate::render::compute::buffer_handle(world, key) else {
                    return status::NO_COMPONENT;
                };
                world.resource_mut::<FieldPool>().buffer = Some((key, handle.clone()));
                (key, handle)
            }
        };

        if let Some(bytes) = bytes {
            upload(world, &handle, bytes);
        }
        key
    }

    /// Puts `bytes` in the pool's buffer, made larger first where they do not fit, with whatever
    /// holds it built against the new one, as a geometry pool's buffers are.
    fn upload(world: &mut World, handle: &Handle<ShaderBuffer>, bytes: Vec<u8>) {
        let grew = {
            let mut assets = world.resource_mut::<Assets<ShaderBuffer>>();
            let Some(mut buffer) = assets.get_mut(handle) else { return };

            let size = buffer.buffer_size();
            let mut data = bytes;
            let wanted = crate::render::compute::buffer_size(data.len() as u64).max(size);
            data.resize(wanted as usize, 0);

            buffer.copy_on_resize = false;
            crate::render::compute::fill(&mut buffer, &data);
            wanted != size
        };

        if grew {
            crate::render::compute::rebind_buffer(world, handle);
        }
    }

    /// The box around a mesh's places in its own space, or none for a mesh with none the bridge
    /// can read, as one whose data went to the render world alone.
    fn bounds_of(mesh: &Mesh) -> Option<[f32; 6]> {
        let Ok(Some(VertexAttributeValues::Float32x3(positions))) = mesh.try_attribute_option(Mesh::ATTRIBUTE_POSITION) else {
            return None;
        };

        let mut b = [f32::MAX, f32::MAX, f32::MAX, f32::MIN, f32::MIN, f32::MIN];
        for p in positions {
            for axis in 0..3 {
                b[axis] = b[axis].min(p[axis]);
                b[axis + 3] = b[axis + 3].max(p[axis]);
            }
        }

        (!positions.is_empty()).then_some(b)
    }

    /// A mesh's triangles as the pool's corners, or none for a mesh of no triangles the bridge can
    /// read. The reads that answer rather than panic, since a mesh whose data went to the render
    /// world keeps a husk here.
    pub fn corners_of(mesh: &Mesh) -> Option<Vec<[u32; 4]>> {
        if mesh.primitive_topology() != PrimitiveTopology::TriangleList {
            return None;
        }

        let Ok(Some(VertexAttributeValues::Float32x3(positions))) = mesh.try_attribute_option(Mesh::ATTRIBUTE_POSITION) else {
            return None;
        };

        let colors = match mesh.try_attribute_option(Mesh::ATTRIBUTE_COLOR) {
            Ok(Some(VertexAttributeValues::Float32x4(colors))) if colors.len() == positions.len() => Some(colors),
            _ => None,
        };

        let indices: Vec<u32> = match mesh.try_indices_option() {
            Ok(Some(Indices::U16(indices))) => indices.iter().map(|&index| index as u32).collect(),
            Ok(Some(Indices::U32(indices))) => indices.clone(),
            Ok(None) => (0..positions.len() as u32).collect(),
            Err(_) => return None,
        };

        let count = indices.len() / 3 * 3;
        if count == 0 || indices[..count].iter().any(|&index| index as usize >= positions.len()) {
            return None;
        }

        let places: Vec<[u32; 3]> = indices[..count].iter().map(|&index| positions[index as usize].map(f32::to_bits)).collect();
        let open = open_edges(&places);

        Some(
            (0..count)
                .map(|i| {
                    let color = colors.map_or(0x00FF_FFFF, |colors| srgb_bytes(colors[indices[i] as usize]));
                    let [x, y, z] = places[i];
                    [x, y, z, color | if open[i] { OPEN_EDGE } else { 0 }]
                })
                .collect(),
        )
    }

    /// Whether each edge of a mesh's triangles is open, its edge from each corner to the next, which
    /// no other triangle shares by the places of its ends, so a mesh whose faces keep vertices of
    /// their own, as a cube's do, is closed where its faces meet and a ground plane open at its rim.
    pub fn open_edges(places: &[[u32; 3]]) -> Vec<bool> {
        let edge = |i: usize| {
            let (a, b) = (places[i], places[i / 3 * 3 + (i % 3 + 1) % 3]);
            if a <= b { (a, b) } else { (b, a) }
        };

        let mut shared: HashMap<([u32; 3], [u32; 3]), u32> = HashMap::with_capacity(places.len());
        for i in 0..places.len() {
            *shared.entry(edge(i)).or_insert(0) += 1;
        }

        (0..places.len()).map(|i| shared[&edge(i)] == 1).collect()
    }

    /// A color in linear light as three sRGB bytes, red lowest.
    fn srgb_bytes(linear: [f32; 4]) -> u32 {
        let byte = |l: f32| {
            let l = l.clamp(0.0, 1.0);
            let s = if l <= 0.003_130_8 { l * 12.92 } else { 1.055 * l.powf(1.0 / 2.4) - 0.055 };
            (s * 255.0 + 0.5) as u32
        };
        byte(linear[0]) | (byte(linear[1]) << 8) | (byte(linear[2]) << 16)
    }
}

#[cfg(test)]
mod tests {
    use super::{BcsFieldDraw, BcsFieldEye};

    /// The offsets the managed side writes out, which a field moved here must move there too.
    #[test]
    fn a_draw_lies_where_the_managed_side_reads_it() {
        assert_eq!(std::mem::offset_of!(BcsFieldDraw, flags), 8);
        assert_eq!(std::mem::offset_of!(BcsFieldDraw, rows), 16);
        assert_eq!(std::mem::offset_of!(BcsFieldDraw, color), 64);
        assert_eq!(std::mem::offset_of!(BcsFieldDraw, emission), 80);
        assert_eq!(std::mem::offset_of!(BcsFieldDraw, low), 96);
        assert_eq!(std::mem::offset_of!(BcsFieldDraw, high), 112);
        assert_eq!(std::mem::size_of::<BcsFieldDraw>(), 128);
        assert_eq!(std::mem::offset_of!(BcsFieldEye, found), 12);
        assert_eq!(std::mem::offset_of!(BcsFieldEye, forward), 16);
        assert_eq!(std::mem::offset_of!(BcsFieldEye, far), 28);
        assert_eq!(std::mem::offset_of!(BcsFieldEye, camera), 32);
        assert_eq!(std::mem::size_of::<BcsFieldEye>(), 40);
    }

    /// A quad of two triangles is open at its four sides and closed along its diagonal, whether
    /// the triangles share vertices or keep their own.
    #[cfg(feature = "render")]
    #[test]
    fn an_edge_is_open_where_no_other_triangle_shares_its_ends() {
        let at = |x: f32, z: f32| [x.to_bits(), 0f32.to_bits(), z.to_bits()];
        let quad = [at(0.0, 0.0), at(1.0, 0.0), at(1.0, 1.0), at(0.0, 0.0), at(1.0, 1.0), at(0.0, 1.0)];
        assert_eq!(super::open_edges(&quad), [true, true, false, false, true, true]);
    }
}
