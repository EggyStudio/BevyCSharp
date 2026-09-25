//! The entry points for shaders the game wrote: programs, materials, passes over a camera's
//! picture, and compute shaders run over buffers.
//!
//! What a program is and how it is kept compiled is in [`super::programs`], how a shader's own
//! globals are laid out is in [`super::reflect`], and how values become a bind group is in
//! [`super::values`]. This is the C boundary over them, present in every build so the managed side
//! links against any of them, and answering [`status::UNSUPPORTED`] where there is no renderer.
//!
//! **Values are set by name on a target.** A target is a material (an asset key), a shader
//! instance (what a pass or a dispatch runs, made by [`bcs_shader_instance_create`]), or an
//! entity, meaning the material it is drawn with. A value is checked against what the program
//! declares when the program has compiled, and refused with a sentence
//! [`bcs_shader_last_error`] hands back when it does not fit. Before the first compile nothing can
//! be checked, so it is kept and checked when the material is prepared.

use crate::interop::status;

/// A file, or Slang source, and an entry point in it.
///
/// Every string is NUL-terminated UTF-8. A null `path` and a null `source` leave the stage out, and
/// a null `entry` is the name the stage's entry point usually has: `vertex`, `fragment`, or `main`
/// for compute.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsShaderStage {
    pub path: *const core::ffi::c_char,
    pub entry: *const core::ffi::c_char,
    /// Slang source, in place of a path, or null.
    pub source: *const core::ffi::c_char,
}

/// A name the shader is compiled with defined.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsShaderDefine {
    /// NUL-terminated UTF-8.
    pub name: *const core::ffi::c_char,
    /// `0` a boolean, `1` a signed integer, `2` an unsigned one.
    pub kind: i32,
    /// The value. For a boolean, non-zero is true.
    pub value: i32,
}

/// Which Slang files a program is made of.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsShaderProgramConfig {
    pub vertex: BcsShaderStage,
    pub fragment: BcsShaderStage,
    pub prepass_vertex: BcsShaderStage,
    pub prepass_fragment: BcsShaderStage,
    pub compute: BcsShaderStage,
    /// A full-screen pass over a camera's picture.
    pub pass: BcsShaderStage,
    pub defines: *const BcsShaderDefine,
    pub define_count: i32,
}

/// How a sampler reads. Mirrors [`super::values::SamplerSettings`].
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsSamplerConfig {
    /// `0` clamp to the edge, `1` repeat, `2` mirror, for U, V and W.
    pub address: [i32; 3],
    /// Non-zero for linear, for magnifying, minifying and between mip levels.
    pub linear: [i32; 3],
    pub anisotropy: i32,
}

// -- Errors a call leaves behind

/// Why the last value was refused, for the managed side to put in its exception.
static LAST_ERROR: std::sync::Mutex<String> = std::sync::Mutex::new(String::new());

#[cfg(feature = "render")]
fn refuse(message: String) -> i32 {
    if let Ok(mut last) = LAST_ERROR.lock() {
        *last = message;
    }

    status::INVALID_STATE
}

/// Writes why the last call refused a value, and returns its length in bytes.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_last_error(out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        let text = LAST_ERROR.lock().map(|text| text.clone()).unwrap_or_default();
        unsafe { crate::interop::write_text(&text, out, capacity) }
    })
}

// -- Programs

/// Makes a program from the Slang named, and answers its number.
///
/// Answers the same number for the same description, so a program made in a system that runs
/// every frame is still one program. A program needs a fragment shader, a pass or a compute
/// shader. Each stage is compiled in the background, and a material drawn by it appears once the
/// compile has finished, which [`bcs_shader_program_state`] reports.
///
/// Returns [`status::NULL_ARG`] where there is nothing to run or a define has no name,
/// [`status::NO_COMPONENT`] where a file is not Slang, and [`status::UNSUPPORTED`] where there is
/// no renderer.
///
/// # Safety
/// `config` must point to a readable [`BcsShaderProgramConfig`] whose strings are null or
/// NUL-terminated, and whose `defines` points at `define_count` readable defines.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_program_create(config: *const BcsShaderProgramConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = config;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::programs::{Define, ProgramDescription, StageFile, create};

            if config.is_null() {
                return status::NULL_ARG;
            }

            let config = unsafe { *config };

            if config.define_count < 0 || (config.defines.is_null() && config.define_count > 0) {
                return status::NULL_ARG;
            }

            let stage = |stage: BcsShaderStage| {
                let entry = unsafe { crate::interop::cstr_to_string(stage.entry) };

                if let Some(source) = unsafe { crate::interop::cstr_to_string(stage.source) }
                    && !source.is_empty()
                {
                    return Some((StageFile::Source(source), entry));
                }

                let path = unsafe { crate::interop::cstr_to_string(stage.path) }?;
                (!path.is_empty()).then_some((StageFile::Path(path), entry))
            };

            let mut description = ProgramDescription {
                stages: [
                    stage(config.vertex),
                    stage(config.fragment),
                    stage(config.prepass_vertex),
                    stage(config.prepass_fragment),
                    stage(config.compute),
                    stage(config.pass),
                ],
                defines: Vec::new(),
            };

            if config.define_count > 0 {
                let defines = unsafe {
                    core::slice::from_raw_parts(config.defines, config.define_count as usize)
                };

                for define in defines {
                    let Some(name) = (unsafe { crate::interop::cstr_to_string(define.name) }) else {
                        return status::NULL_ARG;
                    };

                    description.defines.push(match define.kind {
                        1 => Define::Int(name, define.value),
                        2 => Define::UInt(name, define.value as u32),
                        _ => Define::Bool(name, define.value != 0),
                    });
                }
            }

            crate::state::with_world(|world| create(world, description))
        }
    })
}

/// Reports whether a program can run yet: `0` compiling, `1` ready, `2` failed.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_program_state(program: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = program;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| super::programs::state(world, program))
        }
    })
}

/// Reports how many times a program's shaders have been replaced, counting the first time.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_program_generation(program: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = program;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| super::programs::generation(world, program))
        }
    })
}

/// Writes what a program's compiler said, a paragraph per stage, and returns its length in bytes.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_program_diagnostics(
    program: i32,
    out: *mut u8,
    capacity: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (program, out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| match super::programs::diagnostics(world, program) {
                Some(text) => unsafe { crate::interop::write_text(&text, out, capacity) },
                None => status::NO_COMPONENT,
            })
        }
    })
}

/// Writes which files a program is made of, and returns its length in bytes.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_program_describe(
    program: i32,
    out: *mut u8,
    capacity: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (program, out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| match super::programs::describe(world, program) {
                Some(text) => unsafe { crate::interop::write_text(&text, out, capacity) },
                None => status::NO_COMPONENT,
            })
        }
    })
}

/// Writes what a program's shaders declare, a line per binding and field, and returns its length.
///
/// Empty before the program has compiled.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_program_layout(
    program: i32,
    out: *mut u8,
    capacity: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (program, out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let text = super::programs::describe_layout(program).unwrap_or_default();
            unsafe { crate::interop::write_text(&text, out, capacity) }
        }
    })
}

/// Reports how many programs the app has made. They are numbered from zero.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_program_count() -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| super::programs::count(world))
        }
    })
}

/// Compiles every stage of a program again now, whether a file changed or not.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_program_reload(program: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = program;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| super::programs::reload(world, program))
        }
    })
}

/// Reports `1` where there is a `slangc` to compile with, and `0` where there is not.
///
/// Without one a shader still loads from the cache a machine with one wrote, so this says whether
/// an edit can be compiled rather than whether shaders work at all.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_slang_available() -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            0
        }

        #[cfg(feature = "render")]
        {
            super::slang::compiler().is_some() as i32
        }
    })
}

/// Says whether a validation error from the renderer closes the app (`0`, Bevy's answer) or is
/// logged and survived (non-zero).
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_keep_rendering_after_errors(keep: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = keep;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            super::material::keep_rendering_after_errors(keep != 0);
            status::OK
        }
    })
}

/// Writes the last error the renderer reported, and returns its length in bytes.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_last_render_error(out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let text = super::material::last_error();
            unsafe { crate::interop::write_text(&text, out, capacity) }
        }
    })
}

// -- Materials

#[cfg(feature = "render")]
fn alpha_mode(alpha: i32, cutoff: f32) -> bevy::material::AlphaMode {
    use bevy::material::AlphaMode;

    match alpha {
        1 => AlphaMode::Mask(cutoff),
        2 => AlphaMode::Blend,
        3 => AlphaMode::Add,
        4 => AlphaMode::Multiply,
        5 => AlphaMode::Premultiplied,
        _ => AlphaMode::Opaque,
    }
}

/// Whether `program` names a program the running app made.
#[cfg(feature = "render")]
fn program_exists(program: i32) -> bool {
    program >= 0 && super::programs::lookup(program as u32).is_some()
}

/// Makes a material drawn by a program, and answers its asset key.
///
/// `alpha` is `0` opaque, `1` masked at `cutoff`, `2` blended, `3` added, `4` multiplied and `5`
/// premultiplied. `cull` is `0` back faces, `1` front faces, `2` neither.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_material_create(
    program: i32,
    alpha: i32,
    cutoff: f32,
    cull: i32,
    depth_bias: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (program, alpha, cutoff, cull, depth_bias);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::material::BcsMaterial;
            use bevy::render::render_resource::Face;

            if !program_exists(program) {
                return status::INVALID_STATE;
            }

            let mut material = BcsMaterial::new(program as u32);
            material.alpha = alpha_mode(alpha, cutoff);
            material.depth_bias = depth_bias;
            material.cull = match cull {
                1 => Some(Face::Front),
                2 => None,
                _ => Some(Face::Back),
            };

            crate::state::with_world(|world| {
                let Some(mut assets) =
                    world.get_resource_mut::<bevy::asset::Assets<BcsMaterial>>()
                else {
                    return status::UNSUPPORTED;
                };

                let handle = assets.add(material);
                crate::assets::insert_handle(world, handle.untyped())
            })
        }
    })
}

/// Changes how a material is drawn: its program, alpha, faces culled and depth bias.
///
/// A negative `program` leaves the program as it is, and a negative `alpha` leaves the rest, so
/// changing the program alone needs nothing read back first.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_material_configure(
    material: i32,
    program: i32,
    alpha: i32,
    cutoff: f32,
    cull: i32,
    depth_bias: f32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (material, program, alpha, cutoff, cull, depth_bias);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::render::render_resource::Face;

            if program >= 0 && !program_exists(program) {
                return status::INVALID_STATE;
            }

            with_target(TARGET_MATERIAL, material as i64, |target| {
                let Target::Material(current) = target else {
                    return status::NO_COMPONENT;
                };

                if program >= 0 {
                    current.program = program as u32;
                }

                if alpha < 0 {
                    return status::OK;
                }

                current.alpha = alpha_mode(alpha, cutoff);
                current.depth_bias = depth_bias;
                current.cull = match cull {
                    1 => Some(Face::Front),
                    2 => None,
                    _ => Some(Face::Back),
                };

                status::OK
            })
        }
    })
}

/// Reports which program draws a material, or an entity's material with `kind` two.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_target_program(kind: i32, id: i64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, id);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let mut program = status::NO_COMPONENT;

            let answer = read_target(kind, id, |target| {
                program = match target {
                    Target::Material(material) => material.program as i32,
                    Target::Instance(instance) => instance.program as i32,
                };
            });

            if answer == status::OK { program } else { answer }
        }
    })
}

/// Gives an entity a shader material, if the handle names one.
#[cfg(feature = "render")]
pub fn attach(
    entity: &mut bevy::ecs::world::EntityWorldMut,
    untyped: &bevy::asset::UntypedHandle,
) -> bool {
    match untyped
        .clone()
        .try_typed::<super::material::BcsMaterial>()
    {
        Ok(handle) => {
            entity.insert(super::material::BcsMaterial3d(handle));
            true
        }
        Err(_) => false,
    }
}

// -- Shader instances, which passes and dispatches run

/// Makes a shader instance for a program, which is what a pass over a camera's picture or a
/// dispatch runs, and answers its number.
///
/// An instance holds values by name the way a material does, and keeps them from frame to frame.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_instance_create(program: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = program;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if !program_exists(program) {
                return status::INVALID_STATE;
            }

            crate::state::with_world(|world| {
                let mut instances = world.get_resource_or_init::<ShaderInstances>();
                instances.0.push(Instance {
                    program: program as u32,
                    values: Default::default(),
                    version: 0,
                });
                (instances.0.len() - 1) as i32
            })
        }
    })
}

/// Has a different program run an instance, keeping its values.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_instance_set_program(instance: i32, program: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (instance, program);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if !program_exists(program) {
                return status::INVALID_STATE;
            }

            with_target(TARGET_INSTANCE, instance as i64, |target| match target {
                Target::Instance(current) => {
                    current.program = program as u32;
                    current.version += 1;
                    status::OK
                }
                Target::Material(_) => status::NO_COMPONENT,
            })
        }
    })
}

/// Runs an instance's compute shader once, this frame, before any camera draws.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_dispatch(instance: i32, x: u32, y: u32, z: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (instance, x, y, z);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::compute::{Dispatch, queue};

            crate::state::with_world(|world| {
                let Some(found) = world
                    .get_resource::<ShaderInstances>()
                    .and_then(|instances| instances.0.get(usize::try_from(instance).ok()?))
                    .cloned()
                else {
                    return status::NO_COMPONENT;
                };

                queue(
                    world,
                    Dispatch {
                        program: found.program,
                        values: found.values,
                        workgroups: [x.max(1), y.max(1), z.max(1)],
                    },
                )
            })
        }
    })
}

/// Replaces the passes a camera runs over its picture with `count` instances, in order.
///
/// `after_tonemapping` holds a flag per instance: non-zero runs it on the picture as the screen
/// will show it, zero on the linear one. A count of zero takes every pass off.
///
/// # Safety
/// `instances` and `after_tonemapping` must each point at `count` readable integers, or be null
/// when `count` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_shader_passes(
    camera: u64,
    instances: *const i32,
    after_tonemapping: *const i32,
    count: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, instances, after_tonemapping, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if count < 0 || ((instances.is_null() || after_tonemapping.is_null()) && count > 0) {
                return status::NULL_ARG;
            }

            let (ids, afters): (&[i32], &[i32]) = if count > 0 {
                unsafe {
                    (
                        core::slice::from_raw_parts(instances, count as usize),
                        core::slice::from_raw_parts(after_tonemapping, count as usize),
                    )
                }
            } else {
                (&[], &[])
            };

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            crate::state::with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let known = world
                    .get_resource::<ShaderInstances>()
                    .map(|instances| instances.0.len())
                    .unwrap_or(0);

                if ids.iter().any(|id| *id < 0 || *id as usize >= known) {
                    return status::NO_COMPONENT;
                }

                let list = ids
                    .iter()
                    .zip(afters)
                    .map(|(id, after)| (*id as usize, *after != 0))
                    .collect::<Vec<_>>();

                let mut camera = world.entity_mut(entity);

                if list.is_empty() {
                    camera.remove::<(PassInstances, super::passes::BcsShaderPasses)>();
                } else {
                    camera.insert(PassInstances(list));
                }

                status::OK
            })
        }
    })
}

/// Every shader instance the app has made, by number.
#[cfg(feature = "render")]
#[derive(bevy::ecs::resource::Resource, Default)]
pub struct ShaderInstances(pub Vec<Instance>);

/// What a pass or a dispatch runs: a program and values by name.
#[cfg(feature = "render")]
#[derive(Clone, Debug)]
pub struct Instance {
    pub program: u32,
    pub values: super::values::Values,
    /// Moves on with every change, which is what tells a pass its bind group has to be rebuilt.
    pub version: u64,
}

/// Which instances a camera runs as passes, and on which side of tonemapping.
#[cfg(feature = "render")]
#[derive(bevy::ecs::component::Component, Clone)]
pub struct PassInstances(pub Vec<(usize, bool)>);

/// Copies what each camera's pass instances hold onto the camera, where the render world takes it
/// from, whenever an instance has changed.
#[cfg(feature = "render")]
pub fn sync_passes(
    mut commands: bevy::ecs::system::Commands,
    instances: Option<bevy::ecs::system::Res<ShaderInstances>>,
    cameras: bevy::ecs::system::Query<(
        bevy::ecs::entity::Entity,
        &PassInstances,
        Option<&super::passes::BcsShaderPasses>,
    )>,
) {
    use super::passes::{BcsShaderPasses, ShaderPass};

    let Some(instances) = instances else {
        return;
    };

    for (entity, wanted, current) in &cameras {
        let passes: Vec<ShaderPass> = wanted
            .0
            .iter()
            .filter_map(|(id, after)| {
                let instance = instances.0.get(*id)?;
                Some(ShaderPass {
                    program: instance.program,
                    values: instance.values.clone(),
                    version: instance.version,
                    after_tonemapping: *after,
                })
            })
            .collect();

        let same = current.is_some_and(|current| {
            current.passes.len() == passes.len()
                && current.passes.iter().zip(&passes).all(|(a, b)| {
                    a.program == b.program
                        && a.version == b.version
                        && a.after_tonemapping == b.after_tonemapping
                })
        });

        if !same {
            commands.entity(entity).insert(BcsShaderPasses { passes });
        }
    }
}

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

/// The material handle a target names, where it names a material.
#[cfg(feature = "render")]
fn material_handle(
    world: &bevy::ecs::world::World,
    kind: i32,
    id: i64,
) -> Option<bevy::asset::Handle<super::material::BcsMaterial>> {
    match kind {
        TARGET_MATERIAL => crate::assets::clone_handle(world, i32::try_from(id).ok()?)?
            .try_typed::<super::material::BcsMaterial>()
            .ok(),
        TARGET_ENTITY => world
            .get::<super::material::BcsMaterial3d>(bevy::ecs::entity::Entity::from_bits(id as u64))
            .map(|material| material.0.clone()),
        _ => None,
    }
}

/// Runs `f` on a target, and keeps what it changed when it answers [`status::OK`].
///
/// A material is taken out and put back through `Assets::get_mut`, which is what marks it changed
/// and has Bevy prepare its bind group again.
#[cfg(feature = "render")]
fn with_target(kind: i32, id: i64, f: impl FnOnce(Target<'_>) -> i32) -> i32 {
    use super::material::BcsMaterial;

    crate::state::with_world(|world| {
        if kind == TARGET_INSTANCE {
            let Some(mut instances) = world.get_resource_mut::<ShaderInstances>() else {
                return status::NO_COMPONENT;
            };

            let Some(instance) = usize::try_from(id)
                .ok()
                .and_then(|id| instances.0.get_mut(id))
            else {
                return status::NO_COMPONENT;
            };

            let mut copy = instance.clone();
            let answer = f(Target::Instance(&mut copy));

            if answer == status::OK {
                copy.version = instance.version + 1;
                *instance = copy;
            }

            return answer;
        }

        let Some(handle) = material_handle(world, kind, id) else {
            return status::NO_COMPONENT;
        };

        let Some(mut assets) = world.get_resource_mut::<bevy::asset::Assets<BcsMaterial>>() else {
            return status::UNSUPPORTED;
        };

        let Some(mut copy) = assets.get(&handle).cloned() else {
            return status::NO_COMPONENT;
        };

        let answer = f(Target::Material(&mut copy));

        if answer == status::OK
            && let Some(mut slot) = assets.get_mut(&handle)
        {
            *slot = copy;
        }

        answer
    })
}

/// Reads a target without changing it.
#[cfg(feature = "render")]
fn read_target(kind: i32, id: i64, f: impl FnOnce(&Target<'_>)) -> i32 {
    let mut f = Some(f);

    // Answering something other than OK keeps the target unchanged, which is what a read wants.
    let answer = with_target(kind, id, |target| {
        if let Some(f) = f.take() {
            f(&target);
        }
        status::NOT_PRESENT
    });

    if answer == status::NOT_PRESENT {
        status::OK
    } else {
        answer
    }
}

/// The layout a target's values are checked against, where its program has compiled.
#[cfg(feature = "render")]
fn layout_of(target: &Target<'_>) -> Option<std::sync::Arc<super::reflect::Layout>> {
    match target {
        Target::Material(material) => super::programs::lookup(material.program)?.material,
        Target::Instance(instance) => {
            let program = super::programs::lookup(instance.program)?;
            program.pass.or(program.compute)
        }
    }
}

/// Puts a value on a target under a name, checking it where the program can say.
#[cfg(feature = "render")]
fn put(kind: i32, id: i64, name: String, value: super::values::Value) -> i32 {
    with_target(kind, id, |mut target| {
        if let Some(layout) = layout_of(&target)
            && let Err(message) = super::values::check(&layout, &name, &value)
        {
            return refuse(format!("{name} was not set, because {message}"));
        }

        let values = match &mut target {
            Target::Material(material) => &mut material.values,
            Target::Instance(instance) => &mut instance.values,
        };

        values.entries.insert(name, value);
        status::OK
    })
}

/// Sets numbers under a name: a scalar, a vector, a matrix, or an array of any of them.
///
/// `scalar` is `0` float, `1` signed integer, `2` unsigned integer. `components` is how many
/// numbers one element is: one for a scalar, four for a `float4`, sixteen for a `float4x4`, and
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

/// Sets bytes under a name, copied as they are to where the name is: a struct laid out as the
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
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_set_image(
    kind: i32,
    id: i64,
    name: *const core::ffi::c_char,
    index: i32,
    image: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, id, name, index, image);
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

            if image <= 0 {
                return unset(kind, id, key);
            }

            let handle = crate::state::with_world_opt(|world| {
                crate::render::image_handle(world, image)
            });

            match handle {
                Some(Ok(Some(handle))) => put(kind, id, key, super::values::Value::Image(handle)),
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
fn unset(kind: i32, id: i64, name: String) -> i32 {
    with_target(kind, id, |mut target| {
        let values = match &mut target {
            Target::Material(material) => &mut material.values,
            Target::Instance(instance) => &mut instance.values,
        };

        values.entries.remove(&name);
        status::OK
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
/// `sampler`, the name and how many, for the rest. Empty before the program has compiled. It is
/// what an inspector draws a widget per row from.
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

// -- Buffers and images

/// Makes a buffer of `size` bytes, or of `length` if that is more, starting with `bytes`, and
/// answers its asset key.
///
/// # Safety
/// `bytes` must point at `length` readable bytes, or be null when `length` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_buffer_create(bytes: *const u8, length: i32, size: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (bytes, length, size);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if length < 0 || size < 0 || (bytes.is_null() && length > 0) {
                return status::NULL_ARG;
            }

            let given: &[u8] = if length > 0 {
                unsafe { core::slice::from_raw_parts(bytes, length as usize) }
            } else {
                &[]
            };

            crate::state::with_world(|world| {
                super::compute::create_buffer(world, given, size as u64)
            })
        }
    })
}

/// Replaces a buffer's contents with `length` bytes, padded with zeros to its size.
///
/// # Safety
/// `bytes` must point at `length` readable bytes, or be null when `length` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_buffer_write(buffer: i32, bytes: *const u8, length: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (buffer, bytes, length);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if length < 0 || (bytes.is_null() && length > 0) {
                return status::NULL_ARG;
            }

            let given: &[u8] = if length > 0 {
                unsafe { core::slice::from_raw_parts(bytes, length as usize) }
            } else {
                &[]
            };

            crate::state::with_world(|world| super::compute::write_buffer(world, buffer, given))
        }
    })
}

/// Reports a buffer's size in bytes.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_buffer_size(buffer: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = buffer;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| super::compute::size_of_buffer(world, buffer))
        }
    })
}

/// Starts copying a buffer back from the GPU, and answers the ticket its bytes arrive under.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_buffer_read(buffer: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = buffer;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| super::compute::read_buffer(world, buffer))
        }
    })
}

/// Takes the bytes a read brought back, and returns how many there are.
///
/// Returns [`status::NOT_PRESENT`] while they are on their way.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_buffer_take(ticket: i32, out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (ticket, out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| unsafe {
                super::compute::take_read(world, ticket, out, capacity)
            })
        }
    })
}

/// Makes an image a compute shader writes and anything samples, and answers its asset key.
///
/// `depth` above one makes a 3D image. `format` indexes [`super::compute::IMAGE_FORMATS`].
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_image_create(width: u32, height: u32, depth: u32, format: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (width, height, depth, format);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| {
                super::compute::create_image(world, width, height, depth, format)
            })
        }
    })
}

/// Reports which program draws an entity's material, or [`status::NO_COMPONENT`] where it is not
/// drawn by one.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_entity_program(entity: u64) -> i32 {
    bcs_shader_target_program(TARGET_ENTITY, entity as i64)
}

#[cfg(test)]
mod tests {
    use super::*;
    use core::mem::{offset_of, size_of};

    /// The managed mirror is checked against these same numbers, which is what stops the two
    /// drifting apart one field at a time.
    #[test]
    fn the_program_config_has_the_layout_the_managed_side_mirrors() {
        assert_eq!(size_of::<BcsShaderStage>(), 24);
        assert_eq!(offset_of!(BcsShaderStage, source), 16);
        assert_eq!(size_of::<BcsShaderDefine>(), 16);
        assert_eq!(offset_of!(BcsShaderProgramConfig, fragment), 24);
        assert_eq!(offset_of!(BcsShaderProgramConfig, compute), 96);
        assert_eq!(offset_of!(BcsShaderProgramConfig, pass), 120);
        assert_eq!(offset_of!(BcsShaderProgramConfig, defines), 144);
        assert_eq!(offset_of!(BcsShaderProgramConfig, define_count), 152);
        assert_eq!(size_of::<BcsShaderProgramConfig>(), 160);
    }

    #[test]
    fn the_sampler_config_has_the_layout_the_managed_side_mirrors() {
        assert_eq!(offset_of!(BcsSamplerConfig, linear), 12);
        assert_eq!(offset_of!(BcsSamplerConfig, anisotropy), 24);
        assert_eq!(size_of::<BcsSamplerConfig>(), 28);
    }
}
