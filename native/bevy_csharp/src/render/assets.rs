//! Building the materials a picture is made of. Attaching meshes and materials to entities is
//! [`super::attached`]'s.

use crate::interop::{status, BcsMaterialConfig};

#[cfg(feature = "render")]
use super::image_handle;
#[cfg(feature = "render")]
use crate::state::with_world;


/// Builds a physically based material and returns an asset handle for it.
///
/// Color components are linear sRGB in the range zero to one. `metallic` and `roughness` follow
/// the usual convention, zero metallic for a dielectric, roughness near zero for a mirror.
///
/// A texture is named by the asset key of an already-loaded image, which is how the two halves of
/// the asset surface meet. `bcs_asset_load` produces the key, this consumes it. The image does
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

/// Builds Bevy's forward decal material over a standard one made from `config`, and returns an
/// asset handle for it.
///
/// A forward decal is drawn on the surfaces behind its box, faded where it is far from them by
/// `depth_fade_factor`, and `depth_bias` sets which of two overlapping decals is drawn over the
/// other, as Bevy's decal example lays three of them over each other. The camera drawing it needs
/// a depth prepass, which the decal reads the surfaces from.
///
/// # Safety
/// `config` must point to a readable [`BcsMaterialConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_material_create_forward_decal(
    config: *const BcsMaterialConfig,
    depth_fade_factor: f32,
    depth_bias: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (config, depth_fade_factor, depth_bias);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::asset::Assets;
            use bevy::pbr::decal::{ForwardDecalMaterial, ForwardDecalMaterialExt};
            use bevy::pbr::StandardMaterial;

            if config.is_null() {
                return status::NULL_ARG;
            }
            let config = unsafe { *config };

            with_world(|world| {
                let mut base = match standard_material(world, &config) {
                    Ok(material) => material,
                    Err(status) => return status,
                };
                base.depth_bias = depth_bias;
                let material = ForwardDecalMaterial { base, extension: ForwardDecalMaterialExt { depth_fade_factor } };

                let Some(mut materials) = world.get_resource_mut::<Assets<ForwardDecalMaterial<StandardMaterial>>>() else {
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
    use bevy::material::{AlphaMode, OpaqueRendererMethod};
    use bevy::pbr::{ParallaxMappingMethod, StandardMaterial};

    // Resolved before the material is built, because each one needs the world and
    // building it needs the world back to insert the result.
    let mut textures: [Option<Handle<Image>>; 15] = Default::default();
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
        config.depth_map,
        config.specular_texture,
        config.specular_tint_texture,
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
        depth_map,
        specular_texture,
        specular_tint_texture,
    ] = textures;

    let alpha_mode = match config.alpha_mode {
        1 => AlphaMode::Mask(config.alpha_cutoff),
        2 => AlphaMode::Blend,
        3 => AlphaMode::Add,
        4 => AlphaMode::Multiply,
        5 => AlphaMode::Premultiplied,
        6 => AlphaMode::AlphaToCoverage,
        _ => AlphaMode::Opaque,
    };

    let parallax_mapping_method = match config.parallax_method {
        1 => ParallaxMappingMethod::Relief { max_steps: config.relief_steps },
        _ => ParallaxMappingMethod::Occlusion,
    };

    // Auto follows the app's default, which `bcs_render_set_deferred` switches, so a material
    // left at it changes with the rest and one set either way stays.
    let opaque_render_method = match config.opaque_render_method {
        1 => OpaqueRendererMethod::Forward,
        2 => OpaqueRendererMethod::Deferred,
        _ => OpaqueRendererMethod::Auto,
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
        depth_map,
        parallax_depth_scale: config.parallax_depth_scale,
        parallax_mapping_method,
        max_parallax_layer_count: config.parallax_layers,
        specular_tint: Color::linear_rgba(
            config.specular_tint[0],
            config.specular_tint[1],
            config.specular_tint[2],
            config.specular_tint[3],
        ),
        specular_texture,
        specular_tint_texture,
        opaque_render_method,
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
            use bevy::material::{AlphaMode, OpaqueRendererMethod};
            use bevy::pbr::{ParallaxMappingMethod, StandardMaterial};

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
                    AlphaMode::AlphaToCoverage => (6, 0.5),
                    _ => (0, 0.5),
                };

                // Occlusion mapping takes no steps, and reads back with Bevy's own number for relief
                // mapping, so a material switched over starts where Bevy's would.
                let (parallax_method, relief_steps) = match material.parallax_mapping_method {
                    ParallaxMappingMethod::Relief { max_steps } => (1, max_steps),
                    ParallaxMappingMethod::Occlusion => (0, 5),
                };
                let opaque_render_method = match material.opaque_render_method {
                    OpaqueRendererMethod::Forward => 1,
                    OpaqueRendererMethod::Deferred => 2,
                    OpaqueRendererMethod::Auto => 0,
                };

                let base = material.base_color.to_linear();
                let tint = material.specular_tint.to_linear();
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
                    depth_map: key(material.depth_map),
                    parallax_depth_scale: material.parallax_depth_scale,
                    parallax_method,
                    relief_steps,
                    parallax_layers: material.max_parallax_layer_count,
                    specular_tint: [tint.red, tint.green, tint.blue, tint.alpha],
                    specular_texture: key(material.specular_texture),
                    specular_tint_texture: key(material.specular_tint_texture),
                    opaque_render_method,
                };

                unsafe { out.write(config) };
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

    /// Where the managed mirror (`NativeMaterialLayoutTests`) expects the fields, since a field
    /// added on one side in another place than on the other reads every field after it wrongly
    /// while the two sizes can still agree.
    #[test]
    fn the_material_config_is_laid_out_where_the_mirror_expects() {
        use core::mem::{offset_of, size_of};

        assert_eq!(offset_of!(BcsMaterialConfig, emissive), 24);
        assert_eq!(offset_of!(BcsMaterialConfig, uv_scale), 76);
        assert_eq!(offset_of!(BcsMaterialConfig, attenuation_color), 128);
        assert_eq!(offset_of!(BcsMaterialConfig, lightmap_exposure), 180);
        assert_eq!(offset_of!(BcsMaterialConfig, depth_map), 184);
        assert_eq!(offset_of!(BcsMaterialConfig, relief_steps), 196);
        assert_eq!(offset_of!(BcsMaterialConfig, specular_tint), 204);
        assert_eq!(offset_of!(BcsMaterialConfig, opaque_render_method), 228);
        assert_eq!(size_of::<BcsMaterialConfig>(), 232);
    }

    fn app() -> App {
        let mut app = App::new();
        app.add_plugins(bevy::app::TaskPoolPlugin::default());
        app.add_plugins(bevy::asset::AssetPlugin::default());
        crate::assets::init_asset_once::<Mesh>(&mut app);
        app
    }

    /// A material's settings with every field away from its default, each texture none.
    fn a_config() -> BcsMaterialConfig {
        BcsMaterialConfig {
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
            depth_map: -1,
            parallax_depth_scale: 0.09,
            parallax_method: 1,
            relief_steps: 4,
            parallax_layers: 32.0,
            specular_tint: [1.0, 0.5, 0.0, 1.0],
            specular_texture: -1,
            specular_tint_texture: -1,
            opaque_render_method: 1,
        }
    }

    /// A forward decal material keeps its standard base, its fade and its depth bias, an entity
    /// made a decal with it carries Bevy's marker and the material, and a standard material is
    /// refused, since it would draw the decal's box rather than what is behind it.
    #[test]
    fn a_forward_decal_is_made_with_its_own_material() {
        use bevy::pbr::decal::{ForwardDecal, ForwardDecalMaterial};
        use bevy::pbr::{MeshMaterial3d, StandardMaterial};

        // Bevy's plugin for them, which holds the box mesh a decal is drawn on, as the app has.
        let mut app = app();
        crate::assets::init_asset_once::<StandardMaterial>(&mut app);
        crate::assets::init_asset_once::<bevy::image::Image>(&mut app);
        crate::assets::init_asset_once::<bevy::shader::Shader>(&mut app);
        app.add_plugins(bevy::pbr::decal::ForwardDecalPlugin);
        let decal = app.world_mut().spawn_empty().id().to_bits();
        let config = a_config();

        loan_world(app.world_mut(), || {
            let key = unsafe { bcs_material_create_forward_decal(&config, 0.5, 2.0) };
            assert!(key >= 0, "making the decal material failed with {key}");
            let standard = unsafe { bcs_material_create(&config) };

            assert_eq!(crate::render::attached::bcs_render_set_forward_decal(decal, standard), status::NO_COMPONENT);
            assert_eq!(crate::render::attached::bcs_render_set_forward_decal(decal, key), status::OK);
        });

        let world = app.world();
        let entity = bevy::ecs::entity::Entity::from_bits(decal);
        assert!(world.get::<ForwardDecal>(entity).is_some());
        let handle = &world.get::<MeshMaterial3d<ForwardDecalMaterial<StandardMaterial>>>(entity).unwrap().0;
        let material = world.resource::<bevy::asset::Assets<ForwardDecalMaterial<StandardMaterial>>>().get(handle).unwrap();
        assert_eq!((material.extension.depth_fade_factor, material.base.depth_bias), (0.5, 2.0));
        let color = material.base.base_color.to_linear();
        assert_eq!([color.red, color.green, color.blue, color.alpha], config.base_color);
    }

    #[cfg(feature = "render")]
    #[test]
    fn a_material_is_read_back_as_it_was_made() {
        use bevy::pbr::StandardMaterial;

        let mut app = app();
        crate::assets::init_asset_once::<StandardMaterial>(&mut app);
        crate::assets::init_asset_once::<bevy::image::Image>(&mut app);

        let made = a_config();

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
            assert_eq!(made.parallax_depth_scale, read.parallax_depth_scale);
            assert_eq!((1, 4), (read.parallax_method, read.relief_steps));
            assert_eq!(made.parallax_layers, read.parallax_layers);
            assert_eq!(made.specular_tint, read.specular_tint);
            assert_eq!(1, read.opaque_render_method);
            assert_eq!(-1, read.depth_map);
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
