//! Meshes compressed as Bevy compresses them, one made in code once it is and a glTF file's as the
//! file loads.
//!
//! Bevy's `MeshCompressionArgs::regular` keeps a mesh's positions as sixteen-bit numbers within its
//! bounds, its normals and tangents as two sixteen-bit numbers each and its texture coordinates as
//! sixteen bits within their range, its colors as eight bits and its joint weights as sixteen, and
//! its indices as sixteen bits where they fit, a fraction of the memory, which its own materials
//! decode as they draw. A shader of the program's own reads the attributes as they are kept, so a
//! mesh one draws is best left uncompressed.

use crate::interop::status;
use crate::state::with_world;

/// Compresses a mesh as Bevy's `MeshCompressionArgs::regular` does, in place.
///
/// Answers [`status::NOT_PRESENT`] for a mesh still loading and [`status::INVALID_STATE`] for a key
/// that names something else. An attribute Bevy could not compress is kept as it was and named in
/// the log, the rest compressed, as Bevy's own loader leaves it.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_compress_mesh(mesh: i32) -> i32 {
    crate::interop::guard(|| {
        use bevy::asset::Assets;
        use bevy::mesh::{Mesh, MeshCompressionArgs};

        with_world(|world| {
            let Some(handle) = crate::assets::clone_handle(world, mesh) else {
                return status::NOT_PRESENT;
            };
            let Ok(handle) = handle.try_typed::<Mesh>() else {
                return status::INVALID_STATE;
            };
            let Some(mut meshes) = world.get_resource_mut::<Assets<Mesh>>() else {
                return status::UNSUPPORTED;
            };
            let Some(mut mesh) = meshes.get_mut(&handle) else {
                return status::NOT_PRESENT;
            };

            if let Err(errors) = mesh.compress_mesh(&MeshCompressionArgs::regular()) {
                for error in errors {
                    bevy::log::warn!("A mesh's attribute was left uncompressed: {error}");
                }
            }
            status::OK
        })
    })
}

/// Starts loading an asset of a glTF file, `kind` being `Scene`, `Mesh` or `Gltf`, with its meshes
/// compressed as they load, as Bevy's glTF loader does given `MeshCompressionArgs::regular`, and
/// answers the handle C# refers to it by.
///
/// The setting is the file's, so every asset taken from one load of it shares it.
///
/// # Safety
/// `kind` and `path` must be NUL-terminated UTF-8 strings.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_asset_load_compressed(
    kind: *const core::ffi::c_char,
    path: *const core::ffi::c_char,
) -> i32 {
    crate::interop::guard(|| {
        let Some(kind) = (unsafe { crate::interop::cstr_to_string(kind) }) else {
            return status::NULL_ARG;
        };
        let Some(path) = (unsafe { crate::interop::cstr_to_string(path) }) else {
            return status::NULL_ARG;
        };

        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, path);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::AssetServer;
            use bevy::gltf::GltfLoaderSettings;
            use bevy::mesh::MeshCompressionArgs;

            with_world(|world| {
                // A clone of the server, which shares its state, so the world is free for the key.
                let Some(server) = world.get_resource::<AssetServer>().cloned() else {
                    return status::UNSUPPORTED;
                };

                let builder = || {
                    server.load_builder().with_settings(|settings: &mut GltfLoaderSettings| {
                        settings.mesh_compression = Some(MeshCompressionArgs::regular());
                    })
                };
                let handle = match kind.as_str() {
                    "Scene" => builder().load::<bevy::world_serialization::WorldAsset>(path).untyped(),
                    "Mesh" => builder().load::<bevy::mesh::Mesh>(path).untyped(),
                    "Gltf" => builder().load::<bevy::gltf::Gltf>(path).untyped(),
                    _ => return status::NO_COMPONENT,
                };

                crate::assets::key_for(world, handle)
            })
        }
    })
}

#[cfg(test)]
mod tests {
    use bevy::asset::{Assets, Handle};
    use bevy::prelude::Cuboid;
    use bevy::mesh::{Indices, Mesh};
    use bevy::prelude::World;

    use crate::interop::status;

    #[test]
    fn a_mesh_is_compressed_in_place() {
        let mut world = World::new();
        world.init_resource::<Assets<Mesh>>();
        let handle: Handle<Mesh> = world.resource_mut::<Assets<Mesh>>().add(Cuboid::default());
        let key = crate::assets::key_for(&mut world, handle.clone().untyped());

        let before = world.resource::<Assets<Mesh>>().get(&handle).unwrap();
        assert!(matches!(before.indices(), Some(Indices::U32(_))));
        assert!(before.final_aabb.is_none());

        let answer = crate::state::loan_world(&mut world, || super::bcs_render_compress_mesh(key));
        assert_eq!(answer, status::OK);

        // Its indices kept in sixteen bits, which fit, and its bounds kept beside the positions
        // they are now measured within.
        let after = world.resource::<Assets<Mesh>>().get(&handle).unwrap();
        assert!(matches!(after.indices(), Some(Indices::U16(_))));
        assert!(after.final_aabb.is_some());

        // A key that names nothing is answered as nothing there.
        let missing = crate::state::loan_world(&mut world, || super::bcs_render_compress_mesh(key + 1000));
        assert_eq!(missing, status::NOT_PRESENT);
    }
}
