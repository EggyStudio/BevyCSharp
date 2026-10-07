//! Building the materials a picture is made of, and attaching meshes and materials to entities.

use crate::interop::{status, BcsMaterialConfig};

#[cfg(feature = "render")]
use super::image_handle;
#[cfg(feature = "render")]
use crate::state::with_world;


/// Builds a physically based material and returns an asset handle for it.
///
/// Color components are linear sRGB in the range zero to one. `metallic` and `roughness` follow
/// the usual convention: zero metallic for a dielectric, roughness near zero for a mirror.
///
/// A texture is named by the asset key of an already-loaded image, which is how the two halves of
/// the asset surface meet: `bcs_asset_load` produces the key, this consumes it. The image does
/// not have to have finished loading; the material picks it up when it arrives, which is the
/// behavior a Bevy handle has anyway.
///
/// # Safety
/// `config` must point to a readable [`BcsMaterialConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_material_create(config: *const BcsMaterialConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = config;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::pbr::StandardMaterial;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let material = match standard_material(world, &config) {
                    Ok(material) => material,
                    Err(status) => return status,
                };

                let Some(mut materials) = world.get_resource_mut::<Assets<StandardMaterial>>()
                else {
                    return status::UNSUPPORTED;
                };
                let handle = materials.add(material).untyped();

                crate::assets::insert_handle(world, handle)
            })
        }
    })
}

/// A standard material built from the settings a material is made with, its textures resolved
/// from the keys the program holds, or the status a key that names no image gives.
#[cfg(feature = "render")]
fn standard_material(
    world: &mut bevy::ecs::world::World,
    config: &BcsMaterialConfig,
) -> Result<bevy::pbr::StandardMaterial, i32> {
    use bevy::asset::Handle;
    use bevy::color::{Color, LinearRgba};
    use bevy::image::Image;
    use bevy::material::AlphaMode;
    use bevy::pbr::StandardMaterial;

    // Resolved before the material is built, because each one needs the world and
    // building it needs the world back to insert the result.
    let mut textures: [Option<Handle<Image>>; 12] = Default::default();
    let keys = [
        config.base_color_texture,
        config.normal_map,
        config.metallic_roughness_texture,
        config.emissive_texture,
        config.occlusion_texture,
        config.clearcoat_texture,
        config.clearcoat_roughness_texture,
        config.clearcoat_normal_texture,
        config.specular_transmission_texture,
        config.diffuse_transmission_texture,
        config.thickness_texture,
        config.anisotropy_texture,
    ];

    for (slot, key) in keys.iter().enumerate() {
        match image_handle(world, *key) {
            Ok(handle) => textures[slot] = handle,
            Err(status) => return Err(status),
        }
    }

    let [
        base_color_texture,
        normal_map_texture,
        metallic_roughness_texture,
        emissive_texture,
        occlusion_texture,
        clearcoat_texture,
        clearcoat_roughness_texture,
        clearcoat_normal_texture,
        specular_transmission_texture,
        diffuse_transmission_texture,
        thickness_texture,
        anisotropy_texture,
    ] = textures;

    let alpha_mode = match config.alpha_mode {
        1 => AlphaMode::Mask(config.alpha_cutoff),
        2 => AlphaMode::Blend,
        3 => AlphaMode::Add,
        4 => AlphaMode::Multiply,
        5 => AlphaMode::Premultiplied,
        _ => AlphaMode::Opaque,
    };

    let material = StandardMaterial {
        base_color: Color::linear_rgba(
            config.base_color[0],
            config.base_color[1],
            config.base_color[2],
            config.base_color[3],
        ),
        metallic: config.metallic,
        perceptual_roughness: config.roughness,
        emissive: LinearRgba::new(
            config.emissive[0],
            config.emissive[1],
            config.emissive[2],
            config.emissive[3],
        ),
        alpha_mode,
        double_sided: config.double_sided != 0,
        // A double-sided material still culls unless the back faces are kept, which
        // is a separate field and the one people actually mean.
        cull_mode: if config.double_sided != 0 {
            None
        } else {
            Some(bevy::render::render_resource::Face::Back)
        },
        unlit: config.unlit != 0,
        // Scale first, then rotate, then shift, which is the order that makes a
        // scale of eight mean "eight tiles" whatever the other two are set to.
        uv_transform: bevy::math::Affine2::from_scale_angle_translation(
            bevy::math::Vec2::new(config.uv_scale[0], config.uv_scale[1]),
            config.uv_rotation,
            bevy::math::Vec2::new(config.uv_offset[0], config.uv_offset[1]),
        ),
        base_color_texture,
        normal_map_texture,
        metallic_roughness_texture,
        emissive_texture,
        occlusion_texture,
        reflectance: config.reflectance,
        clearcoat: config.clearcoat,
        clearcoat_perceptual_roughness: config.clearcoat_roughness,
        specular_transmission: config.specular_transmission,
        diffuse_transmission: config.diffuse_transmission,
        thickness: config.thickness,
        ior: config.ior,
        attenuation_distance: config.attenuation_distance,
        attenuation_color: Color::linear_rgba(
            config.attenuation_color[0],
            config.attenuation_color[1],
            config.attenuation_color[2],
            config.attenuation_color[3],
        ),
        anisotropy_strength: config.anisotropy_strength,
        anisotropy_rotation: config.anisotropy_rotation,
        clearcoat_texture,
        clearcoat_roughness_texture,
        clearcoat_normal_texture,
        specular_transmission_texture,
        diffuse_transmission_texture,
        thickness_texture,
        anisotropy_texture,
        lightmap_exposure: config.lightmap_exposure,
        ..Default::default()
    };

    Ok(material)
}

/// Writes settings over an existing standard material, so everything drawn with it changes.
///
/// The settings [`bcs_material_create`] takes, laid over the asset in place rather than made into
/// a new one, which is how a tool edits a material that a scene or a file already shares. Reports
/// [`status::INVALID_STATE`] for a handle to something other than a standard material.
///
/// # Safety
/// `config` must point at one readable [`BcsMaterialConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_material_write(handle: i32, config: *const BcsMaterialConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (handle, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::pbr::StandardMaterial;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let Some(handle) = crate::assets::clone_handle(world, handle) else {
                    return status::NOT_PRESENT;
                };
                let Ok(handle) = handle.try_typed::<StandardMaterial>() else {
                    return status::INVALID_STATE;
                };
                let material = match standard_material(world, &config) {
                    Ok(material) => material,
                    Err(status) => return status,
                };

                let Some(mut materials) = world.get_resource_mut::<Assets<StandardMaterial>>() else {
                    return status::UNSUPPORTED;
                };
                // Written through the asset's guard, since a write through it marks the asset
                // changed for the renderer, and a guard taken and dropped without one marks nothing.
                let Some(mut held) = materials.get_mut(&handle) else {
                    return status::NOT_PRESENT;
                };
                *held = material;
                status::OK
            })
        }
    })
}

/// Reads a standard material's settings back, in the form [`bcs_material_create`] takes them.
///
/// The inverse of making one, so a tool can show what a material is, whether code built it or a
/// glTF file brought it, and a scene can write it down as how to make it again. Each texture
/// comes back as the key the program already holds for it, or a new one.
///
/// # Safety
/// `out` must be writable for one [`BcsMaterialConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_material_read(handle: i32, out: *mut BcsMaterialConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (handle, out);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::{Assets, Handle};
            use bevy::image::Image;
            use bevy::material::AlphaMode;
            use bevy::pbr::StandardMaterial;

            if out.is_null() {
                return status::NULL_ARG;
            }

            with_world(|world| {
                let Some(handle) = crate::assets::clone_handle(world, handle) else {
                    return status::NOT_PRESENT;
                };
                let Ok(handle) = handle.try_typed::<StandardMaterial>() else {
                    return status::INVALID_STATE;
                };
                let Some(material) = world
                    .get_resource::<Assets<StandardMaterial>>()
                    .and_then(|materials| materials.get(&handle))
                    .cloned()
                else {
                    return status::NOT_PRESENT;
                };

                let mut key = |texture: Option<Handle<Image>>| match texture {
                    Some(texture) => crate::assets::key_for(world, texture.untyped()),
                    None => -1,
                };

                let (alpha_mode, alpha_cutoff) = match material.alpha_mode {
                    AlphaMode::Mask(cutoff) => (1, cutoff),
                    AlphaMode::Blend => (2, 0.5),
                    AlphaMode::Add => (3, 0.5),
                    AlphaMode::Multiply => (4, 0.5),
                    AlphaMode::Premultiplied => (5, 0.5),
                    _ => (0, 0.5),
                };

                let base = material.base_color.to_linear();
                let (scale, rotation, offset) = material.uv_transform.to_scale_angle_translation();

                let config = BcsMaterialConfig {
                    base_color: [base.red, base.green, base.blue, base.alpha],
                    metallic: material.metallic,
                    roughness: material.perceptual_roughness,
                    emissive: [
                        material.emissive.red,
                        material.emissive.green,
                        material.emissive.blue,
                        material.emissive.alpha,
                    ],
                    alpha_mode,
                    alpha_cutoff,
                    double_sided: i32::from(material.double_sided),
                    unlit: i32::from(material.unlit),
                    base_color_texture: key(material.base_color_texture),
                    normal_map: key(material.normal_map_texture),
                    metallic_roughness_texture: key(material.metallic_roughness_texture),
                    emissive_texture: key(material.emissive_texture),
                    occlusion_texture: key(material.occlusion_texture),
                    uv_scale: [scale.x, scale.y],
                    uv_rotation: rotation,
                    uv_offset: [offset.x, offset.y],
                    reflectance: material.reflectance,
                    clearcoat: material.clearcoat,
                    clearcoat_roughness: material.clearcoat_perceptual_roughness,
                    specular_transmission: material.specular_transmission,
                    diffuse_transmission: material.diffuse_transmission,
                    thickness: material.thickness,
                    ior: material.ior,
                    attenuation_distance: material.attenuation_distance,
                    attenuation_color: {
                        let color = material.attenuation_color.to_linear();
                        [color.red, color.green, color.blue, color.alpha]
                    },
                    anisotropy_strength: material.anisotropy_strength,
                    anisotropy_rotation: material.anisotropy_rotation,
                    clearcoat_texture: key(material.clearcoat_texture),
                    clearcoat_roughness_texture: key(material.clearcoat_roughness_texture),
                    clearcoat_normal_texture: key(material.clearcoat_normal_texture),
                    specular_transmission_texture: key(material.specular_transmission_texture),
                    diffuse_transmission_texture: key(material.diffuse_transmission_texture),
                    thickness_texture: key(material.thickness_texture),
                    anisotropy_texture: key(material.anisotropy_texture),
                    lightmap_exposure: material.lightmap_exposure,
                };

                unsafe { out.write(config) };
                status::OK
            })
        }
    })
}

/// Attaches an asset to an entity through one of the components that carry a handle.
///
/// `component` is `Mesh3d` or `MeshMaterial3d`. These go through Bevy's own insert rather than a
/// byte copy, both because the handle has to be retyped and because inserting them pulls in the
/// components Bevy requires alongside, such as `Transform` and `Visibility`.
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
                    "MeshMaterial2d" => match untyped.try_typed::<bevy::sprite_render::ColorMaterial>() {
                        Ok(handle) => {
                            entity_mut.insert(bevy::sprite_render::MeshMaterial2d(handle));
                            status::OK
                        }
                        Err(_) => status::NO_COMPONENT,
                    },
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

                            if crate::render::shaders::attach(&mut entity_mut, &untyped) {
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
/// A bit each: `1` is never culled for being out of view, `2` casts no shadow, `4` receives none.
/// A bit left clear takes that behavior off again, so the flags are the whole answer rather than
/// additions to it.
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
            use bevy::camera::visibility::NoFrustumCulling;
            use bevy::light::{NotShadowCaster, NotShadowReceiver};

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

                status::OK
            })
        }
    })
}

// Every test here draws on the renderer, so the module is left out of a build without it.
#[cfg(all(test, feature = "render"))]
mod tests {
    use super::*;
    use bevy::app::App;
    use bevy::mesh::Mesh;

    use crate::state::loan_world;

    fn app() -> App {
        let mut app = App::new();
        app.add_plugins(bevy::app::TaskPoolPlugin::default());
        app.add_plugins(bevy::asset::AssetPlugin::default());
        crate::assets::init_asset_once::<Mesh>(&mut app);
        app
    }

    #[cfg(feature = "render")]
    #[test]
    fn a_material_is_read_back_as_it_was_made() {
        use bevy::pbr::StandardMaterial;

        let mut app = app();
        crate::assets::init_asset_once::<StandardMaterial>(&mut app);
        crate::assets::init_asset_once::<bevy::image::Image>(&mut app);

        let made = BcsMaterialConfig {
            base_color: [0.25, 0.5, 0.75, 1.0],
            metallic: 0.8,
            roughness: 0.3,
            emissive: [2.0, 1.0, 0.0, 1.0],
            alpha_mode: 1,
            alpha_cutoff: 0.4,
            double_sided: 1,
            unlit: 0,
            base_color_texture: -1,
            normal_map: -1,
            metallic_roughness_texture: -1,
            emissive_texture: -1,
            occlusion_texture: -1,
            uv_scale: [4.0, 2.0],
            uv_rotation: 0.5,
            uv_offset: [0.25, 0.0],
            reflectance: 0.3,
            clearcoat: 0.9,
            clearcoat_roughness: 0.2,
            specular_transmission: 0.6,
            diffuse_transmission: 0.1,
            thickness: 0.05,
            ior: 1.33,
            attenuation_distance: 2.5,
            attenuation_color: [0.9, 0.5, 0.25, 1.0],
            anisotropy_strength: 0.7,
            anisotropy_rotation: 0.3,
            clearcoat_texture: -1,
            clearcoat_roughness_texture: -1,
            clearcoat_normal_texture: -1,
            specular_transmission_texture: -1,
            diffuse_transmission_texture: -1,
            thickness_texture: -1,
            anisotropy_texture: -1,
            lightmap_exposure: 250.0,
        };

        loan_world(app.world_mut(), || {
            let key = unsafe { bcs_material_create(&made) };
            assert!(key >= 0, "making the material failed with {key}");

            let mut read = made;
            read.metallic = 0.0;
            assert_eq!(status::OK, unsafe { bcs_render_material_read(key, &mut read) });

            assert_eq!(made.base_color, read.base_color);
            assert_eq!(made.metallic, read.metallic);
            assert_eq!(made.clearcoat, read.clearcoat);
            assert_eq!(made.clearcoat_roughness, read.clearcoat_roughness);
            assert_eq!(made.specular_transmission, read.specular_transmission);
            assert_eq!(made.diffuse_transmission, read.diffuse_transmission);
            assert_eq!(made.thickness, read.thickness);
            assert_eq!(made.ior, read.ior);
            assert_eq!(made.attenuation_distance, read.attenuation_distance);
            assert_eq!(made.attenuation_color, read.attenuation_color);
            assert_eq!(made.anisotropy_strength, read.anisotropy_strength);
            assert_eq!(made.lightmap_exposure, read.lightmap_exposure);
            assert_eq!(made.anisotropy_rotation, read.anisotropy_rotation);
            assert_eq!(-1, read.clearcoat_normal_texture);
            assert_eq!(made.reflectance, read.reflectance);
            assert_eq!(made.roughness, read.roughness);
            assert_eq!(made.emissive, read.emissive);
            assert_eq!((1, 0.4), (read.alpha_mode, read.alpha_cutoff));
            assert_eq!(1, read.double_sided);
            assert_eq!(-1, read.base_color_texture);
            let close = |a: f32, b: f32| (a - b).abs() < 1e-5;
            for (made, read) in [(made.uv_scale, read.uv_scale), (made.uv_offset, read.uv_offset)] {
                assert!(close(made[0], read[0]) && close(made[1], read[1]));
            }
            assert!((made.uv_rotation - read.uv_rotation).abs() < 1e-5);

            // Written over in place, so the same key reads back the new settings.
            let mut changed = made;
            changed.base_color = [1.0, 0.0, 0.0, 1.0];
            changed.roughness = 0.9;
            assert_eq!(status::OK, unsafe { bcs_render_material_write(key, &changed) });
            assert_eq!(status::OK, unsafe { bcs_render_material_read(key, &mut read) });
            assert_eq!([1.0, 0.0, 0.0, 1.0], read.base_color);
            assert_eq!(0.9, read.roughness);

            // Something other than a material is refused rather than overwritten.
            assert_eq!(status::NOT_PRESENT, unsafe { bcs_render_material_write(9999, &changed) });
        });
    }
}
