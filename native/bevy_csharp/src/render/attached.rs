//! Meshes and materials attached to entities through the components that carry a handle, the path
//! an entity's asset was loaded from, and how a mesh is batched and culled.

use crate::interop::status;

#[cfg(feature = "render")]
use crate::state::with_world;

/// Attaches an asset to an entity through one of the components that carry a handle.
///
/// `component` is `Mesh3d`, `MeshMaterial3d`, `Mesh2d`, `MeshMaterial2d` or `MaterialNode`. These
/// go through Bevy's own insert rather than a byte copy, both because the handle has to be retyped
/// and because inserting them pulls in the components Bevy requires alongside, such as `Transform`
/// and `Visibility`.
///
/// # Safety
/// `component` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_ecs_insert_asset(
    entity: u64,
    component: *const core::ffi::c_char,
    handle: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, component, handle);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::ecs::entity::Entity;
            use bevy::mesh::{Mesh, Mesh3d};
            use bevy::pbr::{MeshMaterial3d, StandardMaterial};

            let Some(component) = (unsafe { crate::interop::cstr_to_string(component) }) else {
                return status::NULL_ARG;
            };

            with_world(|world| {
                let Some(untyped) = crate::assets::clone_handle(world, handle) else {
                    return status::NO_ENTITY;
                };

                let entity = Entity::from_bits(entity);
                let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
                    return status::NO_ENTITY;
                };

                // try_typed rather than typed, because asking for a mesh component with a material
                // handle is a mistake the managed side can make, and it should be an error rather
                // than a panic crossing the boundary.
                match component.as_str() {
                    "Mesh3d" => match untyped.try_typed::<Mesh>() {
                        Ok(handle) => {
                            entity_mut.insert(Mesh3d(handle));
                            status::OK
                        }
                        Err(_) => status::NO_COMPONENT,
                    },
                    // A mesh a 2D camera draws, with the color material 2D meshes are drawn with.
                    "Mesh2d" => match untyped.try_typed::<Mesh>() {
                        Ok(handle) => {
                            entity_mut.insert(bevy::mesh::Mesh2d(handle));
                            status::OK
                        }
                        Err(_) => status::NO_COMPONENT,
                    },
                    "MeshMaterial2d" => match untyped.clone().try_typed::<bevy::sprite_render::ColorMaterial>() {
                        Ok(handle) => {
                            entity_mut.insert(bevy::sprite_render::MeshMaterial2d(handle));
                            status::OK
                        }
                        // Not a color material, so it may be a 2D material drawn by a shader the
                        // caller wrote.
                        Err(_) if crate::render::shaders::attach_2d(&mut entity_mut, &untyped) => status::OK,
                        Err(_) => status::NO_COMPONENT,
                    },
                    // A node drawn by a shader the caller wrote, the one kind of interface
                    // material there is from C#.
                    "MaterialNode" if crate::render::ui_material::attach(&mut entity_mut, &untyped) => status::OK,
                    "MaterialNode" => status::NO_COMPONENT,
                    #[cfg(feature = "meshlet")]
                    "MeshletMesh3d" => match untyped
                        .try_typed::<bevy::pbr::experimental::meshlet::MeshletMesh>()
                    {
                        Ok(handle) => {
                            crate::render::meshlets::attach(entity_mut.into_world_mut(), entity, handle);
                            status::OK
                        }
                        Err(_) => status::NO_COMPONENT,
                    },
                    "MeshMaterial3d" => match untyped.clone().try_typed::<StandardMaterial>() {
                        Ok(handle) => {
                            entity_mut.insert(MeshMaterial3d(handle));
                            status::OK
                        }

                        // Not the standard one, so it may be a material drawn by a shader the
                        // caller wrote. The asset table is untyped, so which it is can only be
                        // found by asking.
                        Err(_) => {
                            #[cfg(feature = "meshlet")]
                            if crate::render::meshlets::attach_material(&mut entity_mut, &untyped) {
                                return status::OK;
                            }

                            if crate::render::leaves::attach(&mut entity_mut, &untyped)
                                || crate::render::impostors::attach(&mut entity_mut, &untyped)
                                || crate::render::sway::attach(&mut entity_mut, &untyped)
                                || crate::render::shaders::attach(&mut entity_mut, &untyped)
                            {
                                status::OK
                            } else {
                                status::NO_COMPONENT
                            }
                        }
                    },
                    _ => status::NO_COMPONENT,
                }
            })
        }
    })
}

/// Makes `entity` a forward decal drawn with the forward decal material `material` names, as Bevy's
/// `ForwardDecal` with its material does, the decal's box from the entity's transform.
///
/// Answers [`status::NO_COMPONENT`] where the key names no forward decal material, as a standard
/// material, which would draw the decal's box rather than what is behind it.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_forward_decal(entity: u64, material: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, material);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::pbr::decal::{ForwardDecal, ForwardDecalMaterial};
            use bevy::pbr::{MeshMaterial3d, StandardMaterial};

            with_world(|world| {
                let Some(Ok(handle)) = crate::assets::clone_handle(world, material)
                    .map(|untyped| untyped.try_typed::<ForwardDecalMaterial<StandardMaterial>>())
                else {
                    return status::NO_COMPONENT;
                };
                let Ok(mut decal) = world.get_entity_mut(crate::ecs::entity_from(entity)) else {
                    return status::NO_ENTITY;
                };
                decal.insert((ForwardDecal, MeshMaterial3d(handle)));
                status::OK
            })
        }
    })
}

/// Writes where an entity's mesh or material was loaded from, and returns its length in bytes.
///
/// `which` is `0` for the mesh and `1` for the material. The answer is the asset path, which an
/// editor can show and a person can point at a different file. An asset built in memory rather than
/// loaded has no path and answers an empty string, which is the honest answer rather than a made-up
/// name.
///
/// The usual text convention. Pass null with a capacity of zero to learn the length, then call
/// again with a buffer that size.
///
/// Returns [`status::NO_COMPONENT`] where the entity carries no such thing.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_asset_path(
    entity: u64,
    which: i32,
    out: *mut u8,
    capacity: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, which, out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::AssetServer;
            use bevy::pbr::{MeshMaterial3d, StandardMaterial};
            use bevy::render::mesh::Mesh3d;

            crate::state::with_world(|world| {
                let entity = crate::ecs::entity_from(entity);

                let id = match which {
                    0 => world.get::<Mesh3d>(entity).map(|mesh| mesh.0.id().untyped()),
                    1 => world
                        .get::<MeshMaterial3d<StandardMaterial>>(entity)
                        .map(|material| material.0.id().untyped()),
                    _ => return status::NULL_ARG,
                };

                let Some(id) = id else {
                    return status::NO_COMPONENT;
                };

                let Some(server) = world.get_resource::<AssetServer>() else {
                    return status::UNSUPPORTED;
                };

                // An asset made in memory has no path, which is most of what this project's own
                // meshes and materials are, so an empty answer is ordinary rather than a failure.
                let path = server
                    .get_path(id)
                    .map(|path| path.to_string())
                    .unwrap_or_default();

                unsafe { crate::interop::write_text(&path, out, capacity) }
            })
        }
    })
}

/// Says how an entity's mesh is treated beyond what it looks like.
///
/// A bit each, `1` is never culled for being out of view, `2` casts no shadow, `4` receives none,
/// `8` is drawn alone rather than batched with the meshes it shares a mesh and a material with, and
/// `16` is left out of the CPU's culling for the GPU's alone. A bit left clear takes that behavior
/// off again, so the flags are the whole answer rather than additions to it.
///
/// A mesh drawn somewhere its own bounds do not say needs to escape culling, which covers any mesh
/// a vertex shader moves far from where it was built, and one whose vertices a buffer places. Bevy
/// culls by the bounds it worked out from the mesh, so such a mesh vanishes whenever those stale
/// bounds leave the view.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_render_set_mesh_flags(entity: u64, flags: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, flags);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::camera::visibility::{NoCpuCulling, NoFrustumCulling};
            use bevy::light::{NotShadowCaster, NotShadowReceiver};
            use bevy::render::batching::NoAutomaticBatching;

            with_world(|world| {
                let Ok(mut entity) = world.get_entity_mut(bevy::ecs::entity::Entity::from_bits(entity))
                else {
                    return status::NO_ENTITY;
                };

                if flags & 1 != 0 {
                    entity.insert(NoFrustumCulling);
                } else {
                    entity.remove::<NoFrustumCulling>();
                }

                if flags & 2 != 0 {
                    entity.insert(NotShadowCaster);
                } else {
                    entity.remove::<NotShadowCaster>();
                }

                if flags & 4 != 0 {
                    entity.insert(NotShadowReceiver);
                } else {
                    entity.remove::<NotShadowReceiver>();
                }

                if flags & 8 != 0 {
                    entity.insert(NoAutomaticBatching);
                } else {
                    entity.remove::<NoAutomaticBatching>();
                }

                if flags & 16 != 0 {
                    entity.insert(NoCpuCulling);
                } else {
                    entity.remove::<NoCpuCulling>();
                }

                status::OK
            })
        }
    })
}
