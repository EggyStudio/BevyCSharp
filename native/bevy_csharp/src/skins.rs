//! Skinned meshes built in code: the joints each vertex follows, the skin's inverse bindposes and
//! the joint entities that move it.
//!
//! Bevy skins a mesh on the GPU from two vertex attributes, four joints a vertex and how much each
//! moves it, and from a `SkinnedMesh` on the entity naming the joints as entities and the inverse
//! bindposes as an asset, the inverse of where each joint stood when the mesh was bound to it. A
//! model brings all three from its file. A mesh made in code is given them here, apart from the
//! mesh's own description, so a mesh with no skin crosses the bridge as it always did.
//!
//! The attributes and the component are Bevy's mesh crate's, which every profile has. The bounds
//! that follow the joints as they move, `DynamicSkinnedMeshBounds`, are the camera's, and are added
//! where the `render` feature is, a headless run culling nothing.

use crate::interop::status;

/// Gives the mesh behind a key the joints each of its vertices follows, four a vertex, and the
/// weight of each, four floats a vertex, and works out the bounds of the mesh around each joint,
/// which Bevy culls a skinned mesh by as it moves.
///
/// Answers [`status::INVALID_STATE`] where `count` is not the mesh's number of vertices, and
/// [`status::NOT_PRESENT`] for a key that names no mesh kept on this side, as one still loading.
///
/// # Safety
/// `joints` and `weights` must each hold `count * 4` values.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_mesh_set_joints(mesh: i32, joints: *const u16, weights: *const f32, count: i32) -> i32 {
    crate::interop::guard(|| {
        if joints.is_null() || weights.is_null() || count <= 0 {
            return status::NULL_ARG;
        }

        let count = count as usize;
        let joints: Vec<[u16; 4]> = unsafe { core::slice::from_raw_parts(joints, count * 4) }
            .chunks_exact(4)
            .map(|j| [j[0], j[1], j[2], j[3]])
            .collect();
        let weights: Vec<[f32; 4]> = unsafe { core::slice::from_raw_parts(weights, count * 4) }
            .chunks_exact(4)
            .map(|w| [w[0], w[1], w[2], w[3]])
            .collect();

        crate::state::with_world(|world| {
            use bevy::asset::Assets;
            use bevy::mesh::{Mesh, VertexAttributeValues};

            let Some(handle) = crate::assets::clone_handle(world, mesh).and_then(|h| h.try_typed::<Mesh>().ok()) else {
                return status::NOT_PRESENT;
            };
            let Some(mut meshes) = world.get_resource_mut::<Assets<Mesh>>() else {
                return status::UNSUPPORTED;
            };
            // Written through what into_inner hands back, since Bevy marks an asset changed only
            // where it is written, and a mesh drawn already would keep its old vertices otherwise.
            let Some(mesh) = meshes.get_mut(&handle) else {
                return status::NOT_PRESENT;
            };
            let mesh = mesh.into_inner();
            if mesh.count_vertices() != count {
                return status::INVALID_STATE;
            }

            // Sixteen bits an index, as Bevy's own example has them, which [u16; 4] alone would
            // leave Bevy to read as normalized.
            mesh.insert_attribute(Mesh::ATTRIBUTE_JOINT_INDEX, VertexAttributeValues::Uint16x4(joints));
            mesh.insert_attribute(Mesh::ATTRIBUTE_JOINT_WEIGHT, weights);
            match mesh.generate_skinned_mesh_bounds() {
                Ok(()) => status::OK,
                Err(_) => status::INVALID_STATE,
            }
        })
    })
}

/// Makes a skin's inverse bindposes from transforms, ten floats each, the translation, the rotation
/// as a quaternion and the scale, and writes its key in the asset table.
///
/// # Safety
/// `transforms` must hold `count * 10` floats, and `key` must be writable.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_skin_create(transforms: *const f32, count: i32, key: *mut i32) -> i32 {
    crate::interop::guard(|| {
        if transforms.is_null() || key.is_null() || count <= 0 {
            return status::NULL_ARG;
        }

        use bevy::math::{Mat4, Quat, Vec3};
        let poses: Vec<Mat4> = unsafe { core::slice::from_raw_parts(transforms, count as usize * 10) }
            .chunks_exact(10)
            .map(|t| {
                Mat4::from_scale_rotation_translation(
                    Vec3::new(t[7], t[8], t[9]),
                    Quat::from_xyzw(t[3], t[4], t[5], t[6]),
                    Vec3::new(t[0], t[1], t[2]),
                )
            })
            .collect();

        crate::state::with_world(|world| {
            use bevy::asset::Assets;
            use bevy::mesh::skinning::SkinnedMeshInverseBindposes;

            let Some(mut skins) = world.get_resource_mut::<Assets<SkinnedMeshInverseBindposes>>() else {
                return status::UNSUPPORTED;
            };
            let handle = skins.add(SkinnedMeshInverseBindposes::from(poses)).untyped();
            let added = crate::assets::insert_handle(world, handle);
            if added < 0 {
                return added;
            }
            unsafe { key.write(added) };
            status::OK
        })
    })
}

/// Skins an entity's mesh with a skin's inverse bindposes and the joint entities that move it, in
/// the order of the bindposes and of the indices the mesh's vertices name.
///
/// # Safety
/// `joints` must hold `count` entities.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_skin_set(entity: u64, skin: i32, joints: *const u64, count: i32) -> i32 {
    crate::interop::guard(|| {
        if joints.is_null() || count <= 0 {
            return status::NULL_ARG;
        }

        let joints: Vec<_> = unsafe { core::slice::from_raw_parts(joints, count as usize) }
            .iter()
            .map(|&joint| crate::ecs::entity_from(joint))
            .collect();

        crate::state::with_world(|world| {
            use bevy::mesh::skinning::{SkinnedMesh, SkinnedMeshInverseBindposes};

            let Some(inverse_bindposes) =
                crate::assets::clone_handle(world, skin).and_then(|h| h.try_typed::<SkinnedMeshInverseBindposes>().ok())
            else {
                return status::NOT_PRESENT;
            };
            if joints.iter().any(|&joint| world.get_entity(joint).is_err()) {
                return status::NO_ENTITY;
            }
            let Ok(mut entity_mut) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
                return status::NO_ENTITY;
            };

            entity_mut.insert(SkinnedMesh { inverse_bindposes, joints });
            #[cfg(feature = "render")]
            entity_mut.insert(bevy::camera::visibility::DynamicSkinnedMeshBounds);
            status::OK
        })
    })
}
