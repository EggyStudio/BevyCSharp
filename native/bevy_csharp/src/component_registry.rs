//! C# components registered with the world, and the layouts of the components the managed side
//! mirrors, which it checks its own against.

use crate::interop::status;
use crate::state::{app_mut, with_world, BcsApp};

/// Translates the storage selector the managed side sends into Bevy's own.
///
/// `0` is table storage, which suits almost everything. Components sit in contiguous columns that a
/// query can walk without indirection. `1` is sparse-set storage, which trades that for cheap
/// insertion and removal, because adding or removing one does not move the entity between
/// archetypes. It suits a tag that is toggled far more often than it is iterated.
fn storage_from(storage: i32) -> Option<bevy::ecs::component::StorageType> {
    match storage {
        0 => Some(bevy::ecs::component::StorageType::Table),
        1 => Some(bevy::ecs::component::StorageType::SparseSet),
        _ => None,
    }
}

/// Registers a component layout with the Bevy world, returning its `ComponentId`.
///
/// The layout is padded to its alignment, which Bevy requires. `size` must therefore already be a
/// multiple of `align` for the round-trip to be lossless, which `Unsafe.SizeOf<T>()` guarantees for
/// a blittable C# struct.
///
/// `storage` selects table or sparse-set storage; see [`storage_from`].
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string; `handle` must be a live app.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_component_register(
    handle: *mut BcsApp,
    name: *const core::ffi::c_char,
    size: u32,
    align: u32,
    storage: i32,
) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if align == 0 || !align.is_power_of_two() {
            return status::NULL_ARG;
        }
        let Some(storage) = storage_from(storage) else {
            return status::NULL_ARG;
        };
        let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
            return status::NULL_ARG;
        };

        let Ok(layout) = core::alloc::Layout::from_size_align(size as usize, align as usize) else {
            return status::NULL_ARG;
        };

        // SAFETY: the data is plain bytes owned by C#. There is no Drop glue to run, the
        // component is mutable, and cloning is left to the managed side.
        let descriptor = unsafe {
            bevy::ecs::component::ComponentDescriptor::new_with_layout(
                name,
                storage,
                layout.pad_to_align(),
                None,
                true,
                // No summary tick, as a component of Bevy's has none unless it asks, since C#
                // writes through column pointers that would not keep one.
                false,
                // Bevy clones only what implements Clone or Reflect, which bytes from C# do not, so
                // every C# component is cloned by copying its bytes, through C# for its handles.
                bevy::ecs::component::ComponentCloneBehavior::Custom(crate::lifecycle::cloned),
                None,
            )
        };

        let id = app.app.world_mut().register_component_with_descriptor(descriptor);
        id.index() as i32
    })
}

/// Registers a component layout while the app is already running.
///
/// [`bcs_app_create`]'s handle is mutably borrowed for the whole of [`bcs_app_run`], so a system
/// needing a component type it has not seen before must register it through the world loan instead
/// of through the handle.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_component_register_live(
    name: *const core::ffi::c_char,
    size: u32,
    align: u32,
    storage: i32,
) -> i32 {
    crate::interop::guard(|| {
        if align == 0 || !align.is_power_of_two() {
            return status::NULL_ARG;
        }
        let Some(storage) = storage_from(storage) else {
            return status::NULL_ARG;
        };
        let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
            return status::NULL_ARG;
        };
        let Ok(layout) = core::alloc::Layout::from_size_align(size as usize, align as usize) else {
            return status::NULL_ARG;
        };

        with_world(|world| {
            // SAFETY: see `bcs_component_register`.
            let descriptor = unsafe {
                bevy::ecs::component::ComponentDescriptor::new_with_layout(
                    name.clone(),
                    storage,
                    layout.pad_to_align(),
                    None,
                    true,
                    // No summary tick, as in `bcs_component_register`.
                    false,
                    // Bevy clones only what implements Clone or Reflect, which bytes from C# do not, so
                // every C# component is cloned by copying its bytes, through C# for its handles.
                bevy::ecs::component::ComponentCloneBehavior::Custom(crate::lifecycle::cloned),
                    None,
                )
            };
            world.register_component_with_descriptor(descriptor).index() as i32
        })
    })
}

/// Resolves one of Bevy's own components to the id the ECS entry points take.
///
/// C# components are registered from a layout because Bevy has never heard of them. Bevy's own
/// components are the opposite problem. They are Rust types the managed side has no handle on,
/// so it asks for them by name and gets back the same kind of id. Everything downstream, the
/// inserts, the queries, the chunked iteration, is already keyed on ids rather than types, so
/// nothing else has to change to make these usable.
///
/// Registration is idempotent, and doing it here rather than looking the id up means a component
/// works even if no plugin has touched it yet.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_component_id_of(name: *const core::ffi::c_char) -> i32 {
    crate::interop::guard(|| {
        let Some(name) = (unsafe { crate::interop::cstr_to_string(name) }) else {
            return status::NULL_ARG;
        };

        with_world(|world| {
            let id = match name.as_str() {
                "Transform" => world.register_component::<bevy::transform::components::Transform>(),
                "GlobalTransform" => {
                    world.register_component::<bevy::transform::components::GlobalTransform>()
                }
                "ChildOf" => world.register_component::<bevy::ecs::hierarchy::ChildOf>(),
                "Children" => world.register_component::<bevy::ecs::hierarchy::Children>(),
                "WorldInstance" => {
                    world.register_component::<bevy::world_serialization::WorldInstance>()
                }
                // Reached through the prelude rather than its defining crate, because Visibility
                // moved from bevy_render to bevy_camera in 0.19, and the prelude survives such
                // moves.
                #[cfg(feature = "render")]
                "Visibility" => world.register_component::<bevy::prelude::Visibility>(),
                #[cfg(feature = "render")]
                "InheritedVisibility" => {
                    world.register_component::<bevy::prelude::InheritedVisibility>()
                }
                #[cfg(feature = "render")]
                "ViewVisibility" => world.register_component::<bevy::prelude::ViewVisibility>(),
                // The managed side's handle for a node that reacts to the pointer, the bridge's own
                // component since Bevy's `Interaction` was deprecated.
                #[cfg(feature = "render")]
                "Interaction" => world.register_component::<crate::ui::PointerOnNode>(),
                #[cfg(feature = "render")]
                "Atmosphere" => {
                    world.register_component::<bevy::light::atmosphere::Atmosphere>()
                }
                // Anything else is looked up by its full type path in Bevy's registry, so every
                // reflected component resolves without an arm here. The arms above stay, because
                // the mirrors name their components by short name.
                path => match crate::reflected::component_id(world, path) {
                    Some(id) => id,
                    None => return status::NO_COMPONENT,
                },
            };
            id.index() as i32
        })
    })
}

/// Reports the size and alignment Bevy uses for a component.
///
/// The managed side mirrors a handful of Bevy's structs so it can read and write them in place,
/// and those mirrors have to match byte for byte. They are easy to get subtly wrong: `Quat` is
/// SIMD-backed and sixteen-byte aligned on most targets, which pads `Transform` out to 48 bytes
/// rather than the 40 its fields suggest. Checking the real numbers turns that class of mistake
/// into an error at startup instead of memory corruption later.
///
/// # Safety
/// `size` and `align` must be writable, or null to skip that output.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_component_layout(
    component: i32,
    size: *mut u32,
    align: *mut u32,
) -> i32 {
    crate::interop::guard(|| {
        if component < 0 {
            return status::NO_COMPONENT;
        }

        with_world(|world| {
            let id = bevy::ecs::component::ComponentId::new(component as usize);
            let Some(info) = world.components().get_info(id) else {
                return status::NO_COMPONENT;
            };

            let layout = info.layout();
            if !size.is_null() {
                unsafe { size.write(layout.size() as u32) };
            }
            if !align.is_null() {
                unsafe { align.write(layout.align() as u32) };
            }
            status::OK
        })
    })
}

/// Reports where Bevy places each field of `Transform`.
///
/// A size check alone is not enough to trust a mirrored struct. `Transform` uses Rust's default
/// representation, which allows the compiler to reorder fields, and it does. The sixteen-byte
/// aligned `Quat` is moved ahead of the two vectors. The reordered and source-order layouts
/// happen to be the same total size, so only the offsets tell the two apart.
///
/// # Safety
/// Each pointer must be writable, or null to skip that output.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_transform_layout(
    size: *mut u32,
    rotation: *mut u32,
    translation: *mut u32,
    scale: *mut u32,
) -> i32 {
    crate::interop::guard(|| {
        use bevy::transform::components::Transform;

        let write = |target: *mut u32, value: usize| {
            if !target.is_null() {
                unsafe { target.write(value as u32) };
            }
        };

        write(size, core::mem::size_of::<Transform>());
        write(rotation, core::mem::offset_of!(Transform, rotation));
        write(translation, core::mem::offset_of!(Transform, translation));
        write(scale, core::mem::offset_of!(Transform, scale));
        status::OK
    })
}

/// Reports where Bevy places each part of `GlobalTransform`.
///
/// The world-space result of propagation, and the one component a parented entity cannot compute
/// for itself. `GlobalTransform` wraps a private `Affine3A`, so its offsets cannot be taken the way
/// `Transform`'s are. They come from the affine instead, and the wrapper is confirmed to be nothing
/// but that affine by comparing the two sizes, because a single-field struct that is exactly the
/// size of its field has nowhere else to put it.
///
/// The offsets matter as much as they do for `Transform`, and for the same reason. `Vec3A` is
/// SIMD-backed and sixteen-byte aligned, so each of the four vectors occupies sixteen bytes
/// rather than the twelve its components need, and a mirror packing them tightly would read
/// every axis but the first from the wrong place while passing a size check on the total.
///
/// # Safety
/// Every output pointer must be writable, or null to skip that output.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_global_transform_layout(
    size: *mut u32,
    x_axis: *mut u32,
    y_axis: *mut u32,
    z_axis: *mut u32,
    translation: *mut u32,
) -> i32 {
    crate::interop::guard(|| {
        use bevy::math::{Affine3A, Mat3A};
        use bevy::transform::components::GlobalTransform;

        if core::mem::size_of::<GlobalTransform>() != core::mem::size_of::<Affine3A>() {
            return status::INVALID_STATE;
        }

        let write = |target: *mut u32, value: usize| {
            if !target.is_null() {
                unsafe { target.write(value as u32) };
            }
        };

        let matrix3 = core::mem::offset_of!(Affine3A, matrix3);

        write(size, core::mem::size_of::<GlobalTransform>());
        write(x_axis, matrix3 + core::mem::offset_of!(Mat3A, x_axis));
        write(y_axis, matrix3 + core::mem::offset_of!(Mat3A, y_axis));
        write(z_axis, matrix3 + core::mem::offset_of!(Mat3A, z_axis));
        write(translation, core::mem::offset_of!(Affine3A, translation));
        status::OK
    })
}

/// Reports the size of `Visibility` and the discriminant behind each of its variants.
///
/// The other mirrors are structs, where a size and a set of offsets pin the layout down. This one
/// is a fieldless enum, and what has to match is which number stands for which variant. Rust does
/// not promise a discriminant order for a default-representation enum, and nothing about a one-byte
/// mirror would look wrong if the engine renumbered them, because hiding an entity would quietly
/// start meaning something else.
///
/// # Safety
/// Every output pointer must be writable, or null to skip that output.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_visibility_layout(
    size: *mut u32,
    inherited: *mut u32,
    hidden: *mut u32,
    visible: *mut u32,
) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (size, inherited, hidden, visible);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            use bevy::prelude::Visibility;

            let write = |target: *mut u32, value: usize| {
                if !target.is_null() {
                    unsafe { target.write(value as u32) };
                }
            };

            write(size, core::mem::size_of::<Visibility>());
            write(inherited, Visibility::Inherited as usize);
            write(hidden, Visibility::Hidden as usize);
            write(visible, Visibility::Visible as usize);
            status::OK
        }
    })
}
