//! Values set by name on a target, a material, a shader instance or the material an entity is
//! drawn with, each checked against what its program declares once the program has compiled.

use crate::interop::status;
use super::shaders::BcsSamplerConfig;
#[cfg(feature = "render")]
use super::shaders::{Instance, ShaderInstances, refuse};

// -- Setting values by name

/// A target is a material, by asset key.
pub const TARGET_MATERIAL: i32 = 0;
/// A target is a shader instance, by number.
pub const TARGET_INSTANCE: i32 = 1;
/// A target is the material an entity is drawn with, by the entity's bits.
pub const TARGET_ENTITY: i32 = 2;

/// What a value is being set on.
#[cfg(feature = "render")]
pub enum Target<'a> {
    Material(&'a mut super::material::BcsMaterial),
    Instance(&'a mut Instance),
}

/// A material a target names, drawn on a mesh, on a 2D mesh or on an interface node.
#[cfg(feature = "render")]
enum MaterialHandle {
    Mesh(bevy::asset::Handle<super::material::BcsMaterial>),
    Flat(bevy::asset::Handle<super::material2d::BcsMaterial2d>),
    Node(bevy::asset::Handle<super::ui_material::BcsUiMaterial>),
}

/// The material handle a target names, where it names a material.
#[cfg(feature = "render")]
fn material_handle(world: &bevy::ecs::world::World, kind: i32, id: i64) -> Option<MaterialHandle> {
    use super::material::{BcsMaterial, BcsMaterial3d};
    use super::material2d::{BcsMaterial2d, BcsMeshMaterial2d};
    use super::ui_material::BcsUiMaterial;

    match kind {
        TARGET_MATERIAL => {
            let untyped = crate::assets::clone_handle(world, i32::try_from(id).ok()?)?;
            untyped
                .clone()
                .try_typed::<BcsMaterial>()
                .map(MaterialHandle::Mesh)
                .or_else(|_| untyped.clone().try_typed::<BcsMaterial2d>().map(MaterialHandle::Flat))
                .or_else(|_| untyped.try_typed::<BcsUiMaterial>().map(MaterialHandle::Node))
                .ok()
        }
        // A sprite's material before its 2D mesh's, which on a sprite is the copy the sprite is
        // drawn with and is made again from the material whenever either changes.
        TARGET_ENTITY => {
            let entity = crate::ecs::entity_from(id as u64);
            world
                .get::<BcsMaterial3d>(entity)
                .map(|material| MaterialHandle::Mesh(material.0.clone()))
                .or_else(|| {
                    world
                        .get::<bevy::sprite_render::SpriteMaterial<BcsMaterial2d>>(entity)
                        .map(|material| MaterialHandle::Flat(material.0.clone()))
                })
                .or_else(|| world.get::<BcsMeshMaterial2d>(entity).map(|material| MaterialHandle::Flat(material.0.clone())))
                .or_else(|| {
                    world
                        .get::<bevy::ui_render::ui_material::MaterialNode<BcsUiMaterial>>(entity)
                        .map(|material| MaterialHandle::Node(material.0.clone()))
                })
        }
        _ => None,
    }
}

/// What a change answers where the target holds what it was given before it was asked, which leaves
/// the target unmarked, and which [`with_target`] answers as [`status::OK`].
#[cfg(feature = "render")]
const UNCHANGED: i32 = i32::MIN;

/// Runs `f` on the material `handle` names, the part of it `part` picks out, where it stands, and
/// marks it changed when `f` answers [`status::OK`].
///
/// Marked through `Assets::get_mut`, which has Bevy prepare its bind group again. A material that
/// answers [`UNCHANGED`] or a refusal is left unmarked.
#[cfg(feature = "render")]
fn edit<A: bevy::asset::Asset>(
    world: &mut bevy::ecs::world::World,
    handle: &bevy::asset::Handle<A>,
    part: fn(&mut A) -> &mut super::material::BcsMaterial,
    f: impl FnOnce(Target<'_>) -> i32,
) -> i32 {
    let Some(mut assets) = world.get_resource_mut::<bevy::asset::Assets<A>>() else {
        return status::UNSUPPORTED;
    };

    let Some(mut slot) = assets.get_mut(handle) else {
        return status::NO_COMPONENT;
    };

    match f(Target::Material(part(slot.bypass_change_detection()))) {
        status::OK => {
            slot.into_inner();
            status::OK
        }
        UNCHANGED => status::OK,
        refused => refused,
    }
}

/// Runs `f` on a target where it stands, and marks it changed when `f` answers [`status::OK`].
///
/// Every `f` changes nothing where it answers anything else, refusing before it writes, so the
/// target is never copied, which it was for every value set, its every value with it.
#[cfg(feature = "render")]
pub(super) fn with_target(kind: i32, id: i64, f: impl FnOnce(Target<'_>) -> i32) -> i32 {
    crate::state::with_world(|world| {
        if kind == TARGET_INSTANCE {
            let Some(mut instances) = world.get_resource_mut::<ShaderInstances>() else {
                return status::NO_COMPONENT;
            };

            let Some(instance) = usize::try_from(id)
                .ok()
                .and_then(|id| instances.get_mut(id))
            else {
                return status::NO_COMPONENT;
            };

            return match f(Target::Instance(&mut *instance)) {
                status::OK => {
                    instance.version += 1;
                    status::OK
                }
                UNCHANGED => status::OK,
                refused => refused,
            };
        }

        match material_handle(world, kind, id) {
            Some(MaterialHandle::Mesh(handle)) => edit(world, &handle, |material| material, f),
            Some(MaterialHandle::Flat(handle)) => edit(world, &handle, |flat| &mut flat.material, f),
            Some(MaterialHandle::Node(handle)) => edit(world, &handle, |node| &mut node.material, f),
            None => status::NO_COMPONENT,
        }
    })
}

/// Reads a target without changing it.
#[cfg(feature = "render")]
pub(super) fn read_target(kind: i32, id: i64, f: impl FnOnce(&Target<'_>)) -> i32 {
    with_target(kind, id, |target| {
        f(&target);
        UNCHANGED
    })
}

/// The layout a target's values are checked against, where its program has compiled.
#[cfg(feature = "render")]
fn layout_of(target: &Target<'_>) -> Option<std::sync::Arc<super::reflect::Layout>> {
    use super::material::Surface;

    match target {
        Target::Material(material) => super::programs::with_program(material.program, |program| {
            match material.surface {
                Surface::Mesh => program.material.clone(),
                Surface::Flat => program.material2d.clone(),
                Surface::Node => program.material_ui.clone(),
            }
        })?,
        Target::Instance(instance) => super::programs::with_program(instance.program, |program| {
            program.pass.clone().or_else(|| program.compute.clone())
        })?,
    }
}

/// Puts a value on a target under a name, checking it where the program can say, and leaves the
/// target unmarked where it held that value under the name before, as a game setting what a frame
/// needs every frame does.
#[cfg(feature = "render")]
pub(super) fn put(kind: i32, id: i64, name: String, value: super::values::Value) -> i32 {
    with_target(kind, id, |mut target| {
        if let Some(layout) = layout_of(&target) {
            if let Err(message) = super::values::check(&layout, &name, &value) {
                return refuse(format!("{name} was not set, because {message}"));
            }

            // Compiled into a material's pipelines, and a pass or a dispatch has one pipeline for
            // every instance of its program, which no instance's constant could change alone.
            if matches!(target, Target::Instance(_))
                && matches!(layout.find(&name), Some(super::reflect::Target::Constant(_)))
            {
                return refuse(format!(
                    "{name} was not set, because it is a pipeline constant, which a material sets \
                     and a pass or a dispatch takes as the shader declares it"
                ));
            }
        }

        let values = match &mut target {
            Target::Material(material) => &mut material.values,
            Target::Instance(instance) => &mut instance.values,
        };

        if values.entries.get(&name) == Some(&value) {
            return UNCHANGED;
        }

        values.entries.insert(name, value);
        status::OK
    })
}

/// Sets numbers under a name, a scalar, a vector, a matrix, or an array of any of them.
///
/// `scalar` is `0` float, `1` signed integer, `2` unsigned integer. `components` is how many
/// numbers one element is, one for a scalar, four for a `float4`, sixteen for a `float4x4`, and
/// `count` is how many elements there are, which is one unless it is an array.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string, and `data` must point at `components` times
/// `count` readable four-byte numbers.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_set_numbers(
    kind: i32,
    id: i64,
    name: *const core::ffi::c_char,
    scalar: i32,
    components: i32,
    data: *const u8,
    count: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, id, name, scalar, components, data, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::reflect::Scalar;

            let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
                return status::NULL_ARG;
            };

            if components <= 0 || count < 0 || (data.is_null() && count > 0) {
                return status::NULL_ARG;
            }

            let length = components as usize * count as usize * 4;
            let bytes = if length > 0 {
                unsafe { core::slice::from_raw_parts(data, length) }.to_vec()
            } else {
                Vec::new()
            };

            let scalar = match scalar {
                1 => Scalar::I32,
                2 => Scalar::U32,
                _ => Scalar::F32,
            };

            put(
                kind,
                id,
                name,
                super::values::Value::Numbers {
                    scalar,
                    components: components as u32,
                    data: bytes,
                },
            )
        }
    })
}

/// Sets bytes under a name, copied as they are to where the name is, a struct laid out as the
/// shader lays it out, or a whole `ConstantBuffer`.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string, and `data` must point at `length` readable bytes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_set_bytes(
    kind: i32,
    id: i64,
    name: *const core::ffi::c_char,
    data: *const u8,
    length: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, id, name, data, length);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
                return status::NULL_ARG;
            };

            if length < 0 || (data.is_null() && length > 0) {
                return status::NULL_ARG;
            }

            let bytes = if length > 0 {
                unsafe { core::slice::from_raw_parts(data, length as usize) }.to_vec()
            } else {
                Vec::new()
            };

            put(kind, id, name, super::values::Value::Bytes(bytes))
        }
    })
}

/// Puts an image under a name, at `index` where the name is an array of textures. A key of zero
/// or less takes it off again.
///
/// `mip` below zero binds the whole image, and zero or more binds that one mip level of it, so a
/// shader building a pyramid a level at a time can read one level and write the next.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_set_image(
    kind: i32,
    id: i64,
    name: *const core::ffi::c_char,
    index: i32,
    image: i32,
    mip: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, id, name, index, image, mip);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
                return status::NULL_ARG;
            };

            if index < 0 {
                return status::NULL_ARG;
            }

            let key = super::values::element_name(&name, index as u32);
            let level = u32::try_from(mip).ok();

            if image <= 0 {
                return unset(kind, id, key);
            }

            let handle = crate::state::with_world_opt(|world| {
                crate::render::image_handle(world, image)
            });

            match handle {
                Some(Ok(Some(handle))) => {
                    put(kind, id, key, super::values::Value::Image(handle, level))
                }
                Some(Err(refusal)) => refusal,
                _ => status::NO_WORLD,
            }
        }
    })
}

/// Puts a buffer from [`bcs_shader_buffer_create`] under a name. A key of zero or less takes it
/// off again.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_set_buffer(
    kind: i32,
    id: i64,
    name: *const core::ffi::c_char,
    buffer: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, id, name, buffer);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
                return status::NULL_ARG;
            };

            if buffer <= 0 {
                return unset(kind, id, name);
            }

            let handle = crate::state::with_world_opt(|world| {
                super::compute::buffer_handle(world, buffer)
            });

            match handle {
                Some(Some(handle)) => put(kind, id, name, super::values::Value::Buffer(handle)),
                Some(None) => status::NO_COMPONENT,
                None => status::NO_WORLD,
            }
        }
    })
}

/// Sets how the sampler under a name reads, at `index` where the name is an array of samplers.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string, and `config` must point to a readable
/// [`BcsSamplerConfig`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_set_sampler(
    kind: i32,
    id: i64,
    name: *const core::ffi::c_char,
    index: i32,
    config: *const BcsSamplerConfig,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, id, name, index, config);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
                return status::NULL_ARG;
            };

            if config.is_null() || index < 0 {
                return status::NULL_ARG;
            }

            let config = unsafe { *config };
            let settings = super::values::SamplerSettings {
                address: config.address.map(|mode| mode.clamp(0, 2) as u8),
                linear: config.linear.map(|linear| linear != 0),
                anisotropy: config.anisotropy.clamp(1, 16) as u16,
            };

            put(
                kind,
                id,
                super::values::element_name(&name, index as u32),
                super::values::Value::Sampler(settings),
            )
        }
    })
}

/// Takes a value off a target, so the name goes back to zeros or the stand-in.
#[cfg(feature = "render")]
pub(super) fn unset(kind: i32, id: i64, name: String) -> i32 {
    with_target(kind, id, |mut target| {
        let values = match &mut target {
            Target::Material(material) => &mut material.values,
            Target::Instance(instance) => &mut instance.values,
        };

        if values.entries.remove(&name).is_some() { status::OK } else { UNCHANGED }
    })
}

/// Takes the value under a name off a target.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_unset(kind: i32, id: i64, name: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, id, name);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
                return status::NULL_ARG;
            };

            unset(kind, id, name)
        }
    })
}

/// Writes the numbers set under a name, as the four-byte numbers they were given as, and returns
/// how many bytes there are. Zero where nothing was set.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string, and `out` writable for `capacity` bytes or null
/// when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_get_numbers(
    kind: i32,
    id: i64,
    name: *const core::ffi::c_char,
    out: *mut u8,
    capacity: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, id, name, out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
                return status::NULL_ARG;
            };

            let mut bytes = Vec::new();

            let answer = read_target(kind, id, |target| {
                let values = match target {
                    Target::Material(material) => &material.values,
                    Target::Instance(instance) => &instance.values,
                };

                if let Some(super::values::Value::Numbers { data, .. }) = values.entries.get(&name)
                {
                    bytes = data.clone();
                }
            });

            if answer != status::OK {
                return answer;
            }

            let needed = bytes.len() as i32;

            if out.is_null() || capacity < needed {
                return needed;
            }

            unsafe { core::ptr::copy_nonoverlapping(bytes.as_ptr(), out, bytes.len()) };
            needed
        }
    })
}

/// Writes what a target's program declares, one line per name, and returns its length in bytes.
///
/// Each line is tab-separated. `number`, the name, the scalar (`float`, `int`, `uint`, `bool`), how
/// many numbers an element is and how many elements, for a number. `texture`, `image`, `buffer` or
/// `sampler`, the name and how many, for the rest. Empty before the program has compiled. An
/// inspector draws a widget per row from it.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_target_names(
    kind: i32,
    id: i64,
    out: *mut u8,
    capacity: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, id, out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::reflect::{BindingKind, FieldType, LOOSE};

            let mut lines = Vec::new();

            let answer = read_target(kind, id, |target| {
                let Some(layout) = layout_of(target) else {
                    return;
                };

                for binding in layout.bindings.values() {
                    match &binding.kind {
                        BindingKind::Uniform { fields, .. } => {
                            for field in fields {
                                let name = if binding.name == LOOSE {
                                    field.name.clone()
                                } else {
                                    format!("{}.{}", binding.name, field.name)
                                };

                                let (element, count) = match &field.ty {
                                    FieldType::Array { element, count, .. } => {
                                        (element.as_ref(), *count)
                                    }
                                    other => (other, 1),
                                };

                                match (element.scalar(), element.components()) {
                                    (Some(scalar), Some(components)) => lines.push(format!(
                                        "number\t{name}\t{}\t{components}\t{count}",
                                        scalar.describe()
                                    )),
                                    _ => lines.push(format!("struct\t{name}\t{count}")),
                                }
                            }
                        }
                        other => {
                            let word = match other {
                                BindingKind::Texture { .. } => "texture",
                                BindingKind::StorageTexture { .. } => "image",
                                BindingKind::Storage { .. } => "buffer",
                                BindingKind::Sampler { .. } => "sampler",
                                BindingKind::AccelerationStructure => "scene",
                                BindingKind::Uniform { .. } => unreachable!(),
                            };

                            lines.push(format!(
                                "{word}\t{}\t{}",
                                binding.name,
                                binding.count.unwrap_or(1)
                            ));
                        }
                    }
                }
            });

            if answer != status::OK {
                return answer;
            }

            unsafe { crate::interop::write_text(&lines.join("\n"), out, capacity) }
        }
    })
}

#[cfg(all(test, feature = "render"))]
mod tests {
    use super::*;
    use crate::render::reflect::Scalar;
    use crate::render::values::Value;
    use bevy::ecs::world::World;

    /// A world holding one instance of a program no table names, so nothing a test sets is checked.
    fn one_instance() -> World {
        let mut world = World::new();
        world.init_resource::<ShaderInstances>();
        world.resource_mut::<ShaderInstances>().push(Instance {
            program: u32::MAX,
            values: Default::default(),
            version: 0,
        });
        world
    }

    fn number(value: f32) -> Value {
        Value::Numbers {
            scalar: Scalar::F32,
            components: 1,
            data: value.to_le_bytes().to_vec(),
        }
    }

    fn version(world: &World) -> u64 {
        world.resource::<ShaderInstances>().get(0).map_or(u64::MAX, |instance| instance.version)
    }

    /// A value set again as it stands, a name taken off that has none and a read leave the instance
    /// at its version, so the pass that runs it keeps the bind group it built, and every change
    /// moves it on once.
    #[test]
    fn only_a_change_moves_an_instance_on() {
        let mut world = one_instance();

        let mut versions = Vec::new();
        for step in 0..6 {
            crate::state::loan_world(&mut world, || match step {
                0 | 1 => assert_eq!(put(TARGET_INSTANCE, 0, "radius".into(), number(2.0)), status::OK),
                2 => assert_eq!(put(TARGET_INSTANCE, 0, "radius".into(), number(3.0)), status::OK),
                3 => assert_eq!(read_target(TARGET_INSTANCE, 0, |_| {}), status::OK),
                _ => assert_eq!(unset(TARGET_INSTANCE, 0, "radius".into()), status::OK),
            });
            versions.push(version(&world));
        }

        assert_eq!(versions, [1, 1, 2, 2, 3, 3]);
    }

    /// An instance taken away answers that it is gone to whatever names its number after, and lets
    /// go of the images its values name.
    #[test]
    fn a_destroyed_instance_lets_go_of_its_values() {
        let mut world = one_instance();
        let image = bevy::asset::Assets::<bevy::image::Image>::default().add(bevy::image::Image::default());
        let bevy::asset::Handle::Strong(strong) = &image else {
            panic!("an added asset's handle is strong");
        };

        crate::state::loan_world(&mut world, || {
            assert_eq!(put(TARGET_INSTANCE, 0, "picture".into(), Value::Image(image.clone(), None)), status::OK);
        });
        assert_eq!(std::sync::Arc::strong_count(strong), 2);

        crate::state::loan_world(&mut world, || {
            assert_eq!(super::super::shaders::bcs_shader_instance_destroy(0), status::OK);
            assert_eq!(super::super::shaders::bcs_shader_instance_destroy(0), status::NO_COMPONENT);
            assert_eq!(put(TARGET_INSTANCE, 0, "radius".into(), number(1.0)), status::NO_COMPONENT);
        });
        assert_eq!(std::sync::Arc::strong_count(strong), 1);
        assert!(world.resource::<ShaderInstances>().get(0).is_none());
    }
}
