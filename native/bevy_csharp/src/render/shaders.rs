//! The entry points for shaders the game wrote: programs, the materials they draw, the passes a
//! camera runs over its picture, and the compute shaders run over buffers.
//!
//! What a program is and how it is kept compiled is in [`super::programs`], what a material
//! carries is in [`super::material`], what a pass reads is in [`super::passes`], and what a
//! dispatch reads and how a buffer lives is in [`super::compute`]. This is the C boundary over both, present in every build so
//! the managed side links against any of them, and answering [`status::UNSUPPORTED`] where there
//! is no renderer.

use crate::interop::status;

/// A file, or source text, and an entry point in it.
///
/// Every string is NUL-terminated UTF-8. A null `path` and a null `source` leave the stage to
/// Bevy, and a null `entry` is the name Bevy's own shaders use for that stage, which is `vertex`,
/// `fragment` or, for compute, `main`.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsShaderStage {
    pub path: *const core::ffi::c_char,
    pub entry: *const core::ffi::c_char,
    /// The code itself, in place of a path, or null.
    pub source: *const core::ffi::c_char,
    /// What `source` is written in: `1` WGSL, `2` Slang.
    pub language: i32,
}

/// A name the shader is compiled with defined.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsShaderDefine {
    /// NUL-terminated UTF-8.
    pub name: *const core::ffi::c_char,
    /// `0` a boolean, `1` a signed integer, `2` an unsigned one. The only three kinds naga_oil's
    /// preprocessor knows, and so the only three a WGSL shader can be given.
    pub kind: i32,
    /// The value. For a boolean, non-zero is true.
    pub value: i32,
}

/// Which shaders a program is made of.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsShaderProgramConfig {
    pub vertex: BcsShaderStage,
    /// Required unless there is a compute shader, because Bevy's own fragment shader reads a
    /// material laid out differently.
    pub fragment: BcsShaderStage,
    pub prepass_vertex: BcsShaderStage,
    pub prepass_fragment: BcsShaderStage,
    pub defines: *const BcsShaderDefine,
    pub define_count: i32,
    /// A compute shader, which a program may have instead of a fragment shader.
    pub compute: BcsShaderStage,
}

/// Everything a shader material is made of.
///
/// Texture fields are asset keys, or zero or less for none.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsShaderMaterialConfig {
    /// The program that draws it.
    pub program: i32,
    /// Up to sixty-four floats, or null.
    pub parameters: *const f32,
    pub parameter_count: i32,
    /// Any number of bytes for the storage buffer, or null.
    pub data: *const u8,
    pub data_length: i32,
    pub textures: [i32; 8],
    pub cubes: [i32; 2],
    pub arrays: [i32; 2],
    pub volumes: [i32; 2],
    /// `0` opaque, `1` masked at `alpha_cutoff`, `2` blended and sorted, `3` added to what is
    /// behind, `4` multiplied with it, `5` premultiplied.
    pub alpha: i32,
    pub alpha_cutoff: f32,
    /// `0` back faces, `1` front faces, `2` neither.
    pub cull: i32,
    pub depth_bias: f32,
    /// A buffer key to bind in place of `data`, or zero or less for the material's own bytes.
    pub buffer: i32,
}

// -- Programs

/// Makes a program from the shaders named, and answers its number.
///
/// Answers the same number for the same description, so a program made in a system that runs
/// every frame is still one program. Each stage is a `.wgsl` or a `.slang` file under the asset
/// root. A Slang stage is compiled in the background, and a material drawn by it appears once the
/// compile has finished, which [`bcs_shader_program_state`] reports.
///
/// Returns [`status::NULL_ARG`] where there is neither a fragment nor a compute shader or a define
/// has no name,
/// [`status::NO_COMPONENT`] where a file is neither WGSL nor Slang, and [`status::UNSUPPORTED`]
/// where there is no renderer.
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

                if let Some(source) = unsafe { crate::interop::cstr_to_string(stage.source) } {
                    let file = match stage.language {
                        2 => StageFile::Slang(source),
                        _ => StageFile::Wgsl(source),
                    };
                    return Some((file, entry));
                }

                let path = unsafe { crate::interop::cstr_to_string(stage.path) }?;
                Some((StageFile::Path(path), entry)).filter(|(file, _)| {
                    !matches!(file, StageFile::Path(path) if path.is_empty())
                })
            };

            let mut description = ProgramDescription {
                stages: [
                    stage(config.vertex),
                    stage(config.fragment),
                    stage(config.prepass_vertex),
                    stage(config.prepass_fragment),
                    stage(config.compute),
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

/// Reports whether a program can draw yet: `0` compiling or loading, `1` ready, `2` failed.
///
/// A failed program still draws, with the last version that compiled or with a fallback, and the
/// answer is failed regardless, because what is on disk is not what is on screen.
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

/// Reports how many times a program's shaders have been replaced, counting each first load.
///
/// Only ever grows. A caller that edits a file reads this first and waits for it to move, which is
/// how it knows the edit has reached the pipelines rather than guessing a number of frames.
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

/// Writes what a program's compilers said, and returns its length in bytes.
///
/// Errors where a stage failed and warnings where one succeeded with them, a paragraph per stage,
/// and an empty string where nothing was said. The usual text convention.
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

/// Compiles or reloads every stage of a program now, whether a file changed or not.
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

/// Reports `1` where there is a `slangc` to compile Slang with, and `0` where there is not.
///
/// Without one a Slang shader still loads from the cache a machine with one wrote, so this says
/// whether an edit can be compiled rather than whether Slang works at all.
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
///
/// Process-wide, and readable from any thread, because the renderer asks from its own.
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

/// Runs `f` on the shader material behind an asset key.
///
/// Changing it through `Assets::get_mut` is what marks it changed, which is what has Bevy prepare
/// its bind group again, so whatever `f` sets reaches the shader on the next frame.
#[cfg(feature = "render")]
fn with_material(
    material: i32,
    f: impl FnOnce(&mut bevy::ecs::world::World, &mut super::material::BcsShaderMaterial) -> i32,
) -> i32 {
    use super::material::BcsShaderMaterial;

    crate::state::with_world(|world| {
        let Some(untyped) = crate::assets::clone_handle(world, material) else {
            return status::NO_COMPONENT;
        };

        let Ok(handle) = untyped.try_typed::<BcsShaderMaterial>() else {
            return status::NO_COMPONENT;
        };

        // Taken out and put back, because `f` may need the world to resolve an image key.
        let Some(mut current) = world
            .get_resource::<bevy::asset::Assets<BcsShaderMaterial>>()
            .and_then(|assets| assets.get(&handle).cloned())
        else {
            return status::NO_COMPONENT;
        };

        let answer = f(world, &mut current);

        if answer == status::OK
            && let Some(mut assets) =
                world.get_resource_mut::<bevy::asset::Assets<BcsShaderMaterial>>()
            && let Some(mut slot) = assets.get_mut(&handle)
        {
            *slot = current;
        }

        answer
    })
}

/// Makes a material drawn by a program, and answers its asset key.
///
/// Returns a negative where the program does not exist, a texture key names no image, or there is
/// no renderer.
///
/// # Safety
/// `config` must point to a readable [`BcsShaderMaterialConfig`], whose `parameters` points at
/// `parameter_count` floats and whose `data` points at `data_length` bytes, or either is null
/// with a count of zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_material_create(config: *const BcsShaderMaterialConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = config;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::material::{BcsShaderMaterial, PARAMETER_COUNT};
            use bevy::render::render_resource::Face;

            if config.is_null() {
                return status::NULL_ARG;
            }

            let config = unsafe { *config };

            if config.parameter_count < 0
                || config.parameter_count as usize > PARAMETER_COUNT
                || (config.parameters.is_null() && config.parameter_count > 0)
                || config.data_length < 0
                || (config.data.is_null() && config.data_length > 0)
            {
                return status::NULL_ARG;
            }

            if !program_exists(config.program) {
                return status::INVALID_STATE;
            }

            let mut material = BcsShaderMaterial::new(config.program as u32);

            if config.parameter_count > 0 {
                let given = unsafe {
                    core::slice::from_raw_parts(config.parameters, config.parameter_count as usize)
                };
                material.parameters[..given.len()].copy_from_slice(given);
            }

            if config.data_length > 0 {
                let given =
                    unsafe { core::slice::from_raw_parts(config.data, config.data_length as usize) };
                material.data = std::sync::Arc::from(given);
            }

            material.alpha = alpha_mode(config.alpha, config.alpha_cutoff);
            let buffer_key = config.buffer;
            material.depth_bias = config.depth_bias;
            material.cull = match config.cull {
                1 => Some(Face::Front),
                2 => None,
                _ => Some(Face::Back),
            };

            crate::state::with_world(|world| {
                for (slots, keys) in [
                    (&mut material.textures[..], &config.textures[..]),
                    (&mut material.cubes[..], &config.cubes[..]),
                    (&mut material.arrays[..], &config.arrays[..]),
                    (&mut material.volumes[..], &config.volumes[..]),
                ] {
                    for (slot, key) in slots.iter_mut().zip(keys) {
                        match crate::render::image_handle(world, *key) {
                            Ok(handle) => *slot = handle,
                            Err(refusal) => return refusal,
                        }
                    }
                }

                match super::compute::optional_buffer(world, buffer_key) {
                    Ok(buffer) => material.buffer = buffer,
                    Err(refusal) => return refusal,
                }

                let Some(mut assets) =
                    world.get_resource_mut::<bevy::asset::Assets<BcsShaderMaterial>>()
                else {
                    return status::UNSUPPORTED;
                };

                let handle = assets.add(material);
                crate::assets::insert_handle(world, handle.untyped())
            })
        }
    })
}

/// Overwrites some of a material's floats, starting at `offset`, and leaves the rest.
///
/// # Safety
/// `values` must point at `count` readable floats, or be null when `count` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_material_set_parameters(
    material: i32,
    offset: i32,
    values: *const f32,
    count: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (material, offset, values, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::material::PARAMETER_COUNT;

            if offset < 0
                || count < 0
                || (offset + count) as usize > PARAMETER_COUNT
                || (values.is_null() && count > 0)
            {
                return status::NULL_ARG;
            }

            let given: Vec<f32> = if count > 0 {
                unsafe { core::slice::from_raw_parts(values, count as usize) }.to_vec()
            } else {
                Vec::new()
            };

            with_material(material, |_, current| {
                let start = offset as usize;
                current.parameters[start..start + given.len()].copy_from_slice(&given);
                status::OK
            })
        }
    })
}

/// Reads a material's floats into `out`, and returns how many were written.
///
/// # Safety
/// `out` must be writable for `count` floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_material_get_parameters(
    material: i32,
    out: *mut f32,
    count: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (material, out, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::material::PARAMETER_COUNT;

            if out.is_null() || count < 0 {
                return status::NULL_ARG;
            }

            let wanted = (count as usize).min(PARAMETER_COUNT);
            let mut read = [0.0f32; PARAMETER_COUNT];

            let answer = with_material(material, |_, current| {
                read = current.parameters;
                // Not a change, so nothing is written back and nothing is prepared again.
                status::NOT_PRESENT
            });

            if answer != status::NOT_PRESENT {
                return answer;
            }

            unsafe { core::ptr::copy_nonoverlapping(read.as_ptr(), out, wanted) };
            wanted as i32
        }
    })
}

/// Replaces a material's data with `length` bytes.
///
/// # Safety
/// `bytes` must point at `length` readable bytes, or be null when `length` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_material_set_data(
    material: i32,
    bytes: *const u8,
    length: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (material, bytes, length);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if length < 0 || (bytes.is_null() && length > 0) {
                return status::NULL_ARG;
            }

            let given: std::sync::Arc<[u8]> = if length > 0 {
                std::sync::Arc::from(unsafe { core::slice::from_raw_parts(bytes, length as usize) })
            } else {
                std::sync::Arc::from(Vec::new())
            };

            with_material(material, |_, current| {
                current.data = given;
                status::OK
            })
        }
    })
}

/// Puts an image in one of a material's texture slots, or empties it with a key of zero or less.
///
/// `kind` is `0` for the eight 2D textures, `1` the two cubemaps, `2` the two array textures and
/// `3` the two 3D textures.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_material_set_texture(
    material: i32,
    kind: i32,
    index: i32,
    image: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (material, kind, index, image);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if index < 0 {
                return status::NULL_ARG;
            }

            with_material(material, |world, current| {
                let slots = match kind {
                    0 => &mut current.textures[..],
                    1 => &mut current.cubes[..],
                    2 => &mut current.arrays[..],
                    3 => &mut current.volumes[..],
                    _ => return status::NULL_ARG,
                };

                let Some(slot) = slots.get_mut(index as usize) else {
                    return status::NULL_ARG;
                };

                match crate::render::image_handle(world, image) {
                    Ok(handle) => {
                        *slot = handle;
                        status::OK
                    }
                    Err(refusal) => refusal,
                }
            })
        }
    })
}

/// Has a different program draw a material.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_material_set_program(material: i32, program: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (material, program);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if !program_exists(program) {
                return status::INVALID_STATE;
            }

            with_material(material, |_, current| {
                current.program = program as u32;
                status::OK
            })
        }
    })
}

/// Reports which program draws a material.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_material_program(material: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = material;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let mut program = status::NO_COMPONENT;

            let answer = with_material(material, |_, current| {
                program = current.program as i32;
                status::NOT_PRESENT
            });

            if answer == status::NOT_PRESENT {
                program
            } else {
                answer
            }
        }
    })
}

/// Changes how a material treats what it draws where it is not opaque. `alpha` as for
/// [`bcs_shader_material_create`].
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_material_set_alpha(material: i32, alpha: i32, cutoff: f32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (material, alpha, cutoff);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_material(material, |_, current| {
                current.alpha = alpha_mode(alpha, cutoff);
                status::OK
            })
        }
    })
}

/// Runs `f` on the shader material an entity is drawn with.
///
/// Through the entity rather than an asset key, because what an inspector has is the entity, and
/// asking the asset table for a key every frame would fill it with keys nobody releases.
#[cfg(feature = "render")]
fn with_entity_material(
    entity: u64,
    f: impl FnOnce(&mut super::material::BcsShaderMaterial) -> i32,
) -> i32 {
    use super::material::BcsShaderMaterial;

    crate::state::with_world(|world| {
        let entity = bevy::ecs::entity::Entity::from_bits(entity);

        let Ok(entity_ref) = world.get_entity(entity) else {
            return status::NO_ENTITY;
        };

        let Some(handle) = entity_ref
            .get::<bevy::pbr::MeshMaterial3d<BcsShaderMaterial>>()
            .map(|material| material.0.clone())
        else {
            return status::NO_COMPONENT;
        };

        let Some(mut assets) = world.get_resource_mut::<bevy::asset::Assets<BcsShaderMaterial>>()
        else {
            return status::UNSUPPORTED;
        };

        // Read through the shared reference first, so a question does not mark the material
        // changed and have it prepared again for nothing.
        let Some(mut current) = assets.get(&handle).cloned() else {
            return status::NO_COMPONENT;
        };

        let answer = f(&mut current);

        if answer == status::OK
            && let Some(mut slot) = assets.get_mut(&handle)
        {
            *slot = current;
        }

        answer
    })
}

/// Reports which program draws an entity's material, or [`status::NO_COMPONENT`] where it is not
/// drawn by one.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_entity_program(entity: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = entity;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let mut program = status::NO_COMPONENT;

            let answer = with_entity_material(entity, |current| {
                program = current.program as i32;
                status::NOT_PRESENT
            });

            if answer == status::NOT_PRESENT { program } else { answer }
        }
    })
}

/// Reads the floats of an entity's shader material into `out`, and returns how many were written.
///
/// # Safety
/// `out` must be writable for `count` floats.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_entity_parameters(entity: u64, out: *mut f32, count: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, out, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::material::PARAMETER_COUNT;

            if out.is_null() || count < 0 {
                return status::NULL_ARG;
            }

            let wanted = (count as usize).min(PARAMETER_COUNT);
            let mut read = [0.0f32; PARAMETER_COUNT];

            let answer = with_entity_material(entity, |current| {
                read = current.parameters;
                status::NOT_PRESENT
            });

            if answer != status::NOT_PRESENT {
                return answer;
            }

            unsafe { core::ptr::copy_nonoverlapping(read.as_ptr(), out, wanted) };
            wanted as i32
        }
    })
}

/// Overwrites some of the floats of an entity's shader material, starting at `offset`.
///
/// # Safety
/// `values` must point at `count` readable floats, or be null when `count` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_entity_set_parameters(
    entity: u64,
    offset: i32,
    values: *const f32,
    count: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (entity, offset, values, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::material::PARAMETER_COUNT;

            if offset < 0
                || count < 0
                || (offset + count) as usize > PARAMETER_COUNT
                || (values.is_null() && count > 0)
            {
                return status::NULL_ARG;
            }

            let given: Vec<f32> = if count > 0 {
                unsafe { core::slice::from_raw_parts(values, count as usize) }.to_vec()
            } else {
                Vec::new()
            };

            with_entity_material(entity, |current| {
                let start = offset as usize;
                current.parameters[start..start + given.len()].copy_from_slice(&given);
                status::OK
            })
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
        .try_typed::<super::material::BcsShaderMaterial>()
    {
        Ok(handle) => {
            entity.insert(bevy::pbr::MeshMaterial3d(handle));
            true
        }
        Err(_) => false,
    }
}

// -- Passes over a camera's picture

/// One full-screen pass a camera runs over what it drew.
///
/// Texture fields are asset keys, or zero or less for none.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsShaderPassConfig {
    /// The program whose fragment shader is run over the picture.
    pub program: i32,
    /// Up to sixty-four floats, or null.
    pub parameters: *const f32,
    pub parameter_count: i32,
    /// Any number of bytes for the storage buffer, or null.
    pub data: *const u8,
    pub data_length: i32,
    pub textures: [i32; 4],
    /// Non-zero to run after tonemapping, on the picture as the screen will show it, rather than
    /// before, on the linear one.
    pub after_tonemapping: i32,
    /// A buffer key to bind in place of `data`, or zero or less for the pass's own bytes.
    pub buffer: i32,
}

/// Replaces the passes a camera runs over its picture, in the order given. A count of zero takes
/// them all off.
///
/// Returns [`status::INVALID_STATE`] where a program does not exist, [`status::NOT_PRESENT`] where
/// the entity is not a camera, and [`status::UNSUPPORTED`] where there is no renderer.
///
/// # Safety
/// `passes` must point at `count` readable configs, each of whose pointers is valid for its count,
/// or be null when `count` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_shader_passes(
    camera: u64,
    passes: *const BcsShaderPassConfig,
    count: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, passes, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::material::PARAMETER_COUNT;
            use super::passes::{BcsShaderPasses, ShaderPass};

            if count < 0 || (passes.is_null() && count > 0) {
                return status::NULL_ARG;
            }

            let configs: &[BcsShaderPassConfig] = if count > 0 {
                unsafe { core::slice::from_raw_parts(passes, count as usize) }
            } else {
                &[]
            };

            let entity = bevy::ecs::entity::Entity::from_bits(camera);

            crate::state::with_world(|world| {
                if let Some(refusal) = crate::render::refuse_unless_camera(world, entity) {
                    return refusal;
                }

                let mut built = Vec::with_capacity(configs.len());

                for config in configs {
                    if config.parameter_count < 0
                        || config.parameter_count as usize > PARAMETER_COUNT
                        || (config.parameters.is_null() && config.parameter_count > 0)
                        || config.data_length < 0
                        || (config.data.is_null() && config.data_length > 0)
                    {
                        return status::NULL_ARG;
                    }

                    if !program_exists(config.program) {
                        return status::INVALID_STATE;
                    }

                    let buffer = match super::compute::optional_buffer(world, config.buffer) {
                        Ok(buffer) => buffer,
                        Err(refusal) => return refusal,
                    };

                    let mut pass = ShaderPass {
                        program: config.program as u32,
                        parameters: [0.0; PARAMETER_COUNT],
                        data: std::sync::Arc::from(Vec::new()),
                        buffer,
                        textures: Default::default(),
                        after_tonemapping: config.after_tonemapping != 0,
                    };

                    if config.parameter_count > 0 {
                        let given = unsafe {
                            core::slice::from_raw_parts(
                                config.parameters,
                                config.parameter_count as usize,
                            )
                        };
                        pass.parameters[..given.len()].copy_from_slice(given);
                    }

                    if config.data_length > 0 {
                        pass.data = std::sync::Arc::from(unsafe {
                            core::slice::from_raw_parts(config.data, config.data_length as usize)
                        });
                    }

                    for (slot, key) in pass.textures.iter_mut().zip(config.textures) {
                        match crate::render::image_handle(world, key) {
                            Ok(handle) => *slot = handle,
                            Err(refusal) => return refusal,
                        }
                    }

                    built.push(pass);
                }

                let mut entity_mut = world.entity_mut(entity);

                if built.is_empty() {
                    entity_mut.remove::<BcsShaderPasses>();
                } else {
                    entity_mut.insert(BcsShaderPasses { passes: built });
                }

                status::OK
            })
        }
    })
}

/// Runs `f` on one of a camera's passes.
#[cfg(feature = "render")]
fn with_pass(
    camera: u64,
    index: i32,
    f: impl FnOnce(&mut super::passes::ShaderPass) -> i32,
) -> i32 {
    let entity = bevy::ecs::entity::Entity::from_bits(camera);

    crate::state::with_world(|world| {
        let Ok(mut entity_mut) = world.get_entity_mut(entity) else {
            return status::NO_ENTITY;
        };

        let Some(mut passes) = entity_mut.get_mut::<super::passes::BcsShaderPasses>() else {
            return status::NOT_PRESENT;
        };

        match usize::try_from(index)
            .ok()
            .and_then(|index| passes.passes.get_mut(index))
        {
            Some(pass) => f(pass),
            None => status::NULL_ARG,
        }
    })
}

/// Overwrites some of a pass's floats, starting at `offset`.
///
/// # Safety
/// `values` must point at `count` readable floats, or be null when `count` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_shader_pass_parameters(
    camera: u64,
    index: i32,
    offset: i32,
    values: *const f32,
    count: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, index, offset, values, count);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::material::PARAMETER_COUNT;

            if offset < 0
                || count < 0
                || (offset + count) as usize > PARAMETER_COUNT
                || (values.is_null() && count > 0)
            {
                return status::NULL_ARG;
            }

            let given: Vec<f32> = if count > 0 {
                unsafe { core::slice::from_raw_parts(values, count as usize) }.to_vec()
            } else {
                Vec::new()
            };

            with_pass(camera, index, |pass| {
                let start = offset as usize;
                pass.parameters[start..start + given.len()].copy_from_slice(&given);
                status::OK
            })
        }
    })
}

/// Replaces a pass's data with `length` bytes.
///
/// # Safety
/// `bytes` must point at `length` readable bytes, or be null when `length` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_set_shader_pass_data(
    camera: u64,
    index: i32,
    bytes: *const u8,
    length: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (camera, index, bytes, length);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if length < 0 || (bytes.is_null() && length > 0) {
                return status::NULL_ARG;
            }

            let given: std::sync::Arc<[u8]> = if length > 0 {
                std::sync::Arc::from(unsafe { core::slice::from_raw_parts(bytes, length as usize) })
            } else {
                std::sync::Arc::from(Vec::new())
            };

            with_pass(camera, index, |pass| {
                pass.data = given;
                status::OK
            })
        }
    })
}

// -- Buffers and compute

/// Makes a buffer of `size` bytes, or of `length` if that is more, starting with `bytes`, and
/// answers its asset key.
///
/// The size is fixed from then on, and rounded up to a whole number of words, never less than
/// sixteen. The rest of it is zero.
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
/// Returns [`status::BUFFER_TOO_SMALL`] where the bytes do not fit, since a buffer's size is fixed.
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
///
/// The copy is of the buffer as it stands after this frame's work, and arrives a frame or two
/// later.
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
/// Returns [`status::NOT_PRESENT`] while they are on their way. Bytes that do not fit `capacity`
/// are left for a second call with a buffer of the size returned, which is the text convention.
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

/// Makes an image a compute shader writes and a material samples, and answers its asset key.
///
/// `format` is `0` for eight bits a channel and `1` for a half float a channel, which are what
/// bindings six and seven of a dispatch write.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_image_create(width: u32, height: u32, format: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (width, height, format);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| {
                super::compute::create_image(world, width, height, format)
            })
        }
    })
}

/// One run of a compute shader.
#[repr(C)]
#[derive(Clone, Copy)]
pub struct BcsShaderDispatchConfig {
    /// A program with a compute stage.
    pub program: i32,
    /// Up to sixty-four floats, or null.
    pub parameters: *const f32,
    pub parameter_count: i32,
    /// Buffer keys at bindings one to four, or zero or less for none.
    pub buffers: [i32; 4],
    /// How many workgroups, along each axis.
    pub x: u32,
    pub y: u32,
    pub z: u32,
    /// Image keys written at bindings six and seven, from [`bcs_shader_image_create`].
    pub images: [i32; 2],
    /// Image keys read at bindings eight and nine.
    pub textures: [i32; 2],
}

/// Runs a compute shader once, this frame, before any camera draws.
///
/// Returns [`status::INVALID_STATE`] where the program does not exist, and
/// [`status::NO_COMPONENT`] where a buffer key names no buffer.
///
/// # Safety
/// `config` must point to a readable [`BcsShaderDispatchConfig`] whose `parameters` points at
/// `parameter_count` floats, or is null with a count of zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_dispatch(config: *const BcsShaderDispatchConfig) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = config;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use super::compute::{Dispatch, optional_buffer, queue};
            use super::material::PARAMETER_COUNT;

            if config.is_null() {
                return status::NULL_ARG;
            }

            let config = unsafe { *config };

            if config.parameter_count < 0
                || config.parameter_count as usize > PARAMETER_COUNT
                || (config.parameters.is_null() && config.parameter_count > 0)
            {
                return status::NULL_ARG;
            }

            if !program_exists(config.program) {
                return status::INVALID_STATE;
            }

            let mut parameters = [0.0f32; PARAMETER_COUNT];

            if config.parameter_count > 0 {
                let given = unsafe {
                    core::slice::from_raw_parts(config.parameters, config.parameter_count as usize)
                };
                parameters[..given.len()].copy_from_slice(given);
            }

            crate::state::with_world(|world| {
                let mut buffers: [Option<_>; 4] = Default::default();

                for (slot, key) in buffers.iter_mut().zip(config.buffers) {
                    match optional_buffer(world, key) {
                        Ok(buffer) => *slot = buffer,
                        Err(refusal) => return refusal,
                    }
                }

                let mut images: [Option<_>; 2] = Default::default();
                let mut textures: [Option<_>; 2] = Default::default();

                for (slots, keys) in [(&mut images, config.images), (&mut textures, config.textures)]
                {
                    for (slot, key) in slots.iter_mut().zip(keys) {
                        match crate::render::image_handle(world, key) {
                            Ok(handle) => *slot = handle,
                            Err(refusal) => return refusal,
                        }
                    }
                }

                queue(
                    world,
                    Dispatch {
                        program: config.program as u32,
                        parameters,
                        buffers,
                        images,
                        textures,
                        workgroups: [config.x.max(1), config.y.max(1), config.z.max(1)],
                    },
                )
            })
        }
    })
}

/// Binds a buffer at a material's data binding in place of its own bytes, or takes it off again
/// with a key of zero or less.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_material_set_buffer(material: i32, buffer: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (material, buffer);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_material(material, |world, current| {
                match super::compute::optional_buffer(world, buffer) {
                    Ok(handle) => {
                        current.buffer = handle;
                        status::OK
                    }
                    Err(refusal) => refusal,
                }
            })
        }
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use core::mem::{offset_of, size_of};

    /// The managed mirror is checked against these same numbers, which is what stops the two
    /// drifting apart one field at a time.
    #[test]
    fn the_material_config_has_the_layout_the_managed_side_mirrors() {
        assert_eq!(offset_of!(BcsShaderMaterialConfig, program), 0);
        assert_eq!(offset_of!(BcsShaderMaterialConfig, parameters), 8);
        assert_eq!(offset_of!(BcsShaderMaterialConfig, parameter_count), 16);
        assert_eq!(offset_of!(BcsShaderMaterialConfig, data), 24);
        assert_eq!(offset_of!(BcsShaderMaterialConfig, data_length), 32);
        assert_eq!(offset_of!(BcsShaderMaterialConfig, textures), 36);
        assert_eq!(offset_of!(BcsShaderMaterialConfig, cubes), 68);
        assert_eq!(offset_of!(BcsShaderMaterialConfig, arrays), 76);
        assert_eq!(offset_of!(BcsShaderMaterialConfig, volumes), 84);
        assert_eq!(offset_of!(BcsShaderMaterialConfig, alpha), 92);
        assert_eq!(offset_of!(BcsShaderMaterialConfig, alpha_cutoff), 96);
        assert_eq!(offset_of!(BcsShaderMaterialConfig, cull), 100);
        assert_eq!(offset_of!(BcsShaderMaterialConfig, depth_bias), 104);
        assert_eq!(offset_of!(BcsShaderMaterialConfig, buffer), 108);
        assert_eq!(size_of::<BcsShaderMaterialConfig>(), 112);
    }

    #[test]
    fn the_pass_config_has_the_layout_the_managed_side_mirrors() {
        assert_eq!(offset_of!(BcsShaderPassConfig, parameters), 8);
        assert_eq!(offset_of!(BcsShaderPassConfig, data), 24);
        assert_eq!(offset_of!(BcsShaderPassConfig, data_length), 32);
        assert_eq!(offset_of!(BcsShaderPassConfig, textures), 36);
        assert_eq!(offset_of!(BcsShaderPassConfig, after_tonemapping), 52);
        assert_eq!(offset_of!(BcsShaderPassConfig, buffer), 56);
        assert_eq!(size_of::<BcsShaderPassConfig>(), 64);
    }

    #[test]
    fn the_dispatch_config_has_the_layout_the_managed_side_mirrors() {
        assert_eq!(offset_of!(BcsShaderDispatchConfig, parameters), 8);
        assert_eq!(offset_of!(BcsShaderDispatchConfig, parameter_count), 16);
        assert_eq!(offset_of!(BcsShaderDispatchConfig, buffers), 20);
        assert_eq!(offset_of!(BcsShaderDispatchConfig, x), 36);
        assert_eq!(offset_of!(BcsShaderDispatchConfig, z), 44);
        assert_eq!(offset_of!(BcsShaderDispatchConfig, images), 48);
        assert_eq!(offset_of!(BcsShaderDispatchConfig, textures), 56);
        assert_eq!(size_of::<BcsShaderDispatchConfig>(), 64);
    }

    #[test]
    fn the_program_config_has_the_layout_the_managed_side_mirrors() {
        assert_eq!(size_of::<BcsShaderStage>(), 32);
        assert_eq!(offset_of!(BcsShaderStage, source), 16);
        assert_eq!(offset_of!(BcsShaderStage, language), 24);
        assert_eq!(size_of::<BcsShaderDefine>(), 16);
        assert_eq!(offset_of!(BcsShaderProgramConfig, fragment), 32);
        assert_eq!(offset_of!(BcsShaderProgramConfig, prepass_fragment), 96);
        assert_eq!(offset_of!(BcsShaderProgramConfig, defines), 128);
        assert_eq!(offset_of!(BcsShaderProgramConfig, define_count), 136);
        assert_eq!(offset_of!(BcsShaderProgramConfig, compute), 144);
        assert_eq!(size_of::<BcsShaderProgramConfig>(), 176);
    }
}
