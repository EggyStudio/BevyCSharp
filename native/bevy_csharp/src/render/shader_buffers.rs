//! Buffers and images a shader reads and writes, the pools of geometry and the scenes rays are
//! traced against, and reading any of them back.

use crate::interop::status;
#[cfg(feature = "render")]
use super::shader_targets::{put, unset};

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

/// Makes a buffer with `capacity` slots the engine fills every frame with the transforms of the
/// entities put in them, this frame's and the previous one's, and answers its asset key.
///
/// See [`super::instances`] for the layout.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_instance_buffer_create(capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = capacity;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if capacity <= 0 {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| super::instances::create(world, capacity as u32))
        }
    })
}

/// Makes an empty geometry pool and writes the asset keys of its vertex, index and mesh buffers to
/// `keys`, in that order. The mesh buffer's key is the pool's.
///
/// See [`super::pools`] for the layout.
///
/// # Safety
/// `keys` must point at three writable integers.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_geometry_pool_create(keys: *mut i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = keys;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if keys.is_null() {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| match super::pools::create(world) {
                Ok(made) => {
                    // SAFETY: the caller promised three writable integers.
                    unsafe { core::ptr::copy_nonoverlapping(made.as_ptr(), keys, 3) };
                    status::OK
                }
                Err(refusal) => refusal,
            })
        }
    })
}

/// Adds a mesh to a geometry pool and answers its number there, counted from zero.
///
/// Returns [`status::NOT_PRESENT`] where the mesh has not loaded yet, and [`status::NULL_ARG`]
/// for a mesh that is not triangles or has no positions.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_geometry_pool_add(pool: i32, mesh: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (pool, mesh);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| super::pools::add(world, pool, mesh))
        }
    })
}

/// Whether this device can build ray scenes and trace rays through them. `1` or `0`.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_ray_queries_supported() -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            0
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world_opt(|world| super::rays::supported(world) as i32).unwrap_or(0)
        }
    })
}

/// Makes a ray scene over the geometry pool whose mesh table is `pool`, with `capacity` slots for
/// instances, and answers its key.
///
/// See [`super::rays`]. Returns [`status::UNSUPPORTED`] on a device without ray queries and
/// [`status::NO_COMPONENT`] where `pool` names no pool.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_ray_scene_create(pool: i32, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (pool, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if capacity <= 0 {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| super::rays::create(world, pool, capacity as u32))
        }
    })
}

/// Puts an entity made of pool mesh `mesh` in a slot of a ray scene, where it is traced at its
/// transform every frame. An entity of zero bits, or a negative mesh, empties the slot.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_ray_scene_set(scene: i32, slot: i32, entity: u64, mesh: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (scene, slot, entity, mesh);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if slot < 0 {
                return status::NULL_ARG;
            }

            let entity = (entity != 0 && mesh >= 0).then(|| bevy::ecs::entity::Entity::from_bits(entity));

            crate::state::with_world(|world| {
                super::rays::set(world, scene, slot as u32, entity, mesh.max(0) as u32)
            })
        }
    })
}

/// Builds pool mesh `mesh` of a ray scene again from what the pool holds now, or every mesh where
/// `mesh` is negative, for geometry a compute shader has moved. See [`super::rays::rebuild`].
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_ray_scene_rebuild(scene: i32, mesh: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (scene, mesh);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let mesh = (mesh >= 0).then_some(mesh as u32);
            crate::state::with_world(|world| super::rays::rebuild(world, scene, mesh))
        }
    })
}

/// Puts a ray scene from [`bcs_shader_ray_scene_create`] under a name. A key of zero or less takes
/// it off again.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_set_ray_scene(
    kind: i32,
    id: i64,
    name: *const core::ffi::c_char,
    scene: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (kind, id, name, scene);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
                return status::NULL_ARG;
            };

            if scene <= 0 {
                return unset(kind, id, name);
            }

            put(kind, id, name, super::values::Value::RayScene(scene))
        }
    })
}

/// Makes a buffer with `capacity` slots the engine fills every frame with the standard materials of
/// the entities put in them, and answers its asset key. Slots are set as an instance buffer's are.
///
/// See [`super::instances`] for the layout.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_material_buffer_create(capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = capacity;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if capacity <= 0 {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| super::instances::create_materials(world, capacity as u32))
        }
    })
}

/// Puts an entity in a slot of an instance or material buffer, or empties the slot where `entity`
/// is zero.
///
/// Returns [`status::NOT_PRESENT`] where the slot is past the buffer's capacity, and
/// [`status::NO_COMPONENT`] where the key is not an instance buffer.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_instance_buffer_set(buffer: i32, slot: i32, entity: u64) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (buffer, slot, entity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if slot < 0 {
                return status::NOT_PRESENT;
            }

            let entity = (entity != 0).then(|| bevy::ecs::entity::Entity::from_bits(entity));

            crate::state::with_world(|world| {
                super::instances::set(world, buffer, slot as u32, entity)
            })
        }
    })
}

/// Makes a buffer at least `size` bytes, keeping what it holds, and returns its new size.
///
/// See [`super::compute::grow_buffer`] for what happens to what already had it.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_buffer_grow(buffer: i32, size: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (buffer, size);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if size < 0 {
                return status::NULL_ARG;
            }

            crate::state::with_world(|world| super::compute::grow_buffer(world, buffer, size as u64))
        }
    })
}

/// Starts copying an image back from the GPU, and answers the ticket its texels arrive under, which
/// [`bcs_shader_buffer_take`] takes as it takes a buffer's bytes.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_image_read(image: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = image;
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| super::compute::read_image(world, image))
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
/// `depth` above one makes a 3D image. `format` indexes [`super::compute::IMAGE_FORMATS`]. `mips`
/// above one gives it that many mip levels, as many as its size allows, all starting as zeros.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_shader_image_create(
    width: u32,
    height: u32,
    depth: u32,
    format: i32,
    mips: u32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (width, height, depth, format, mips);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            crate::state::with_world(|world| {
                super::compute::create_image_with_mips(world, width, height, depth, format, mips)
            })
        }
    })
}

/// Writes `length` bytes of texels into a region of an image, `width` by `height` by `depth`
/// texels at `x`, `y`, `z` of mip level `mip`, on the GPU before this frame's work runs.
///
/// A texture streamer uploads a tile into its cache with this, and an image changed a piece at a
/// time uses it rather than being made again. The texels are in the format's own layout, row after
/// row and slice after slice. Returns [`status::NULL_ARG`] for a region outside the level, and
/// [`status::BUFFER_TOO_SMALL`] where `length` is not exactly the region's size in bytes.
///
/// # Safety
/// `texels` must point at `length` readable bytes.
#[unsafe(no_mangle)]
#[allow(clippy::too_many_arguments)]
pub unsafe extern "C" fn bcs_shader_image_write(
    image: i32,
    x: u32,
    y: u32,
    z: u32,
    width: u32,
    height: u32,
    depth: u32,
    mip: u32,
    texels: *const u8,
    length: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (image, x, y, z, width, height, depth, mip, texels, length);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if texels.is_null() || length < 0 {
                return status::NULL_ARG;
            }

            // SAFETY: the caller promised `length` readable bytes.
            let bytes = unsafe { core::slice::from_raw_parts(texels, length as usize) };

            crate::state::with_world(|world| {
                super::compute::write_image(world, image, [x, y, z], [width, height, depth], mip, bytes)
            })
        }
    })
}

/// Makes an image as [`bcs_shader_image_create`] does, starting with `length` bytes of texels in
/// the format's own layout rather than zeros.
///
/// Returns [`status::BUFFER_TOO_SMALL`] where `length` is not exactly the image's size in bytes.
///
/// # Safety
/// `texels` must point at `length` readable bytes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_shader_image_create_from(
    width: u32,
    height: u32,
    depth: u32,
    format: i32,
    texels: *const u8,
    length: i32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (width, height, depth, format, texels, length);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            if texels.is_null() || length <= 0 {
                return status::NULL_ARG;
            }

            let given = unsafe { core::slice::from_raw_parts(texels, length as usize) };

            crate::state::with_world(|world| {
                super::compute::create_image_from(world, width, height, depth, format, Some(given))
            })
        }
    })
}
