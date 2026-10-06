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
//!
//! The images, dispatches, draws and passes a camera runs are in [`super::shader_views`], values
//! set by name on a target in [`super::shader_targets`], and buffers and images in
//! [`super::shader_buffers`], each reachable through this module as well.

use crate::interop::status;

pub use super::shader_buffers::*;
pub use super::shader_targets::*;
pub use super::shader_views::*;

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
    /// The vertex and fragment shaders of geometry drawn on a camera out of buffers.
    pub draw_vertex: BcsShaderStage,
    pub draw_fragment: BcsShaderStage,
    /// Bit zero compiles the compute stage to SPIR-V rather than WGSL.
    pub flags: i32,
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
pub(super) fn refuse(message: String) -> i32 {
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
                    stage(config.draw_vertex),
                    stage(config.draw_fragment),
                ],
                defines: Vec::new(),
                compute_spirv: config.flags & 1 != 0,
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

/// Makes a shader instance for a program, which a pass over a camera's picture or a dispatch runs,
/// and answers its number.
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
            crate::state::with_world(|world| {
                queue_dispatch(world, instance, [x.max(1), y.max(1), z.max(1)], None)
            })
        }
    })
}

/// Runs an instance's compute shader once, this frame, before any camera draws, with as many
/// workgroups as the three unsigned integers at `offset` bytes into `buffer` say.
///
/// The counts are read on the GPU when the dispatch runs, so one compute shader can decide how much
/// work the next one does without the answer crossing back to the CPU. `offset` is a multiple of
/// four.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_dispatch_indirect(instance: i32, buffer: i32, offset: u32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (instance, buffer, offset);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if offset % 4 != 0 {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| {
                let Some(handle) = super::compute::buffer_handle(world, buffer) else {
                    return status::NO_COMPONENT;
                };

                queue_dispatch(world, instance, [1, 1, 1], Some((handle, offset as u64)))
            })
        }
    })
}

/// Queues a dispatch of what an instance holds now.
#[cfg(feature = "render")]
fn queue_dispatch(
    world: &mut bevy::ecs::world::World,
    instance: i32,
    workgroups: [u32; 3],
    indirect: Option<(bevy::asset::Handle<bevy::render::storage::ShaderBuffer>, u64)>,
) -> i32 {
    use super::compute::{Dispatch, queue};

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
            workgroups,
            indirect,
        },
    )
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
    /// Moves on with every change, which tells a pass its bind group has to be rebuilt.
    pub version: u64,
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

    /// The managed mirror is checked against these same numbers, which stops the two drifting apart
    /// one field at a time.
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
        assert_eq!(offset_of!(BcsShaderProgramConfig, draw_vertex), 160);
        assert_eq!(offset_of!(BcsShaderProgramConfig, draw_fragment), 184);
        assert_eq!(offset_of!(BcsShaderProgramConfig, flags), 208);
        assert_eq!(size_of::<BcsShaderProgramConfig>(), 216);
    }

    #[test]
    fn the_sampler_config_has_the_layout_the_managed_side_mirrors() {
        assert_eq!(offset_of!(BcsSamplerConfig, linear), 12);
        assert_eq!(offset_of!(BcsSamplerConfig, anisotropy), 24);
        assert_eq!(size_of::<BcsSamplerConfig>(), 28);
    }
}

