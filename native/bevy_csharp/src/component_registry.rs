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
                "ChildOf" => world.register_component::<bevy::ecs::hierarchy::ChildOf>(),
                "Children" => world.register_component::<bevy::ecs::hierarchy::Children>(),
                "WorldInstance" => {
                    world.register_component::<bevy::world_serialization::WorldInstance>()
                }
                // The managed side's handle for a node that reacts to the pointer, the bridge's own
                // component since Bevy's `Interaction` was deprecated.
                #[cfg(feature = "render")]
                "Interaction" => world.register_component::<crate::ui::PointerOnNode>(),
                #[cfg(feature = "render")]
                "Atmosphere" => {
                    world.register_component::<bevy::light::atmosphere::Atmosphere>()
                }
                // Anything else is looked up by its full type path in Bevy's registry, so every
                // reflected component resolves without an arm here, the mirrors of the library and
                // the generated ones among them. The arms above are for the handles that name a
                // component without mirroring it, and the bridge's own interaction, which no path
                // reaches.
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
/// A mirror the description holds the layout of is checked field by field against
/// `bcs_reflect_layout`. One a game writes for itself, with no such layout, is checked by its size
/// here, which catches the commonest way such a mirror goes wrong: `Quat` is SIMD-backed and
/// sixteen-byte aligned on most targets, which pads a struct holding one past the size its fields
/// suggest, and reading a mirror of the wrong size returns plausible nonsense rather than failing.
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

