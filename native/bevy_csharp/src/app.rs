//! App lifecycle: construction, stage wiring, system registration and the run loop.

use core::ffi::c_void;
use core::time::Duration;

use bevy::app::{App, AppExit, First, Last, PostUpdate, PreUpdate, ScheduleRunnerPlugin, Startup, Update};
use bevy::ecs::schedule::{IntoScheduleConfigs, SystemSet};
use bevy::ecs::world::World;
use bevy::prelude::Resource;

use crate::interop::{status, BcsConfig};
use crate::state::{app_mut, loan_world, with_world, BcsApp, CleanupList, SystemReg};

/// The scheduling stage a C# system asked for. Mirrors `Bevy.Stage` on the managed side.
#[derive(Clone, Copy, PartialEq, Eq)]
#[repr(i32)]
pub enum Stage {
    /// Once, before the loop starts.
    Startup = 0,
    /// Top of each frame.
    First = 1,
    /// Before `Update`.
    PreUpdate = 2,
    /// Main gameplay stage.
    Update = 3,
    /// After `Update`; where queued commands are applied.
    PostUpdate = 4,
    /// Drawing and overlay work, ordered before `Last`.
    Render = 5,
    /// End of each frame.
    Last = 6,
    /// Once, after the loop exits.
    Cleanup = 7,
    /// Internal: mirrors Bevy's time and input into C#, ahead of every user system.
    FrameSync = 8,
    /// Internal: applies the managed command buffer, after every user `PostUpdate` system.
    CommandFlush = 9,
}

impl Stage {
    /// Converts the discriminant C# sent, rejecting anything out of range.
    pub fn from_i32(value: i32) -> Option<Stage> {
        Some(match value {
            0 => Stage::Startup,
            1 => Stage::First,
            2 => Stage::PreUpdate,
            3 => Stage::Update,
            4 => Stage::PostUpdate,
            5 => Stage::Render,
            6 => Stage::Last,
            7 => Stage::Cleanup,
            8 => Stage::FrameSync,
            9 => Stage::CommandFlush,
            _ => return None,
        })
    }
}

/// Orders the C# systems Bevy cannot order for itself.
///
/// Every C# system is an exclusive system, so Bevy serialises them but leaves the order
/// unspecified. These sets pin down the three places where order actually matters:
/// the frame-state sync must precede all user work, the command flush must follow all
/// user `PostUpdate` work, and `Stage.Render` must precede `Stage.Last`.
#[derive(SystemSet, Debug, Clone, PartialEq, Eq, Hash)]
enum BcsSet {
    Sync,
    First,
    PostUpdate,
    Flush,
    Render,
    Last,
    /// Where the engine decides whether this is the final frame.
    ExitCheck,
    Cleanup,
}

/// Marker inserted once the `Cleanup` callbacks have run, so they run exactly once.
#[derive(Resource)]
struct CleanupRan;

/// Frame budget for headless runs, used to stop after a fixed number of ticks.
#[derive(Resource)]
struct HeadlessFrameLimit {
    remaining: u32,
}

/// Builds the Bevy app for the requested configuration.
///
/// In a `render` build with `headless == 0` this installs `DefaultPlugins` (window,
/// renderer, input backend). Otherwise it installs `MinimalPlugins` plus the input
/// plugin, so the same managed code runs unchanged without a display.
fn build_app(config: &BcsConfig, title: Option<String>, cleanup: CleanupList) -> App {
    let mut app = App::new();

    #[cfg(feature = "render")]
    let windowed = config.headless == 0;
    #[cfg(not(feature = "render"))]
    let windowed = false;

    if windowed {
        #[cfg(feature = "render")]
        {
            use bevy::prelude::*;
            use bevy::render::settings::{Backends, RenderCreation, WgpuSettings};
            use bevy::render::RenderPlugin;
            use bevy::window::{PresentMode, Window, WindowPlugin};

            let present_mode = if config.vsync != 0 {
                PresentMode::AutoVsync
            } else {
                PresentMode::AutoNoVsync
            };

            // `None` leaves wgpu's own preference order alone, which already puts Vulkan first
            // on Linux and Windows. Naming a backend pins the choice instead, and startup then
            // fails loudly if the machine cannot provide it, which is the point of asking.
            let backends = match config.backend {
                1 => Some(Backends::VULKAN),
                2 => Some(Backends::DX12),
                3 => Some(Backends::METAL),
                4 => Some(Backends::GL),
                _ => None,
            };

            let wgpu = match backends {
                Some(backends) => WgpuSettings {
                    backends: Some(backends),
                    ..default()
                },
                None => WgpuSettings::default(),
            };

            app.add_plugins(
                DefaultPlugins
                    .set(WindowPlugin {
                        primary_window: Some(Window {
                            title: title.clone().unwrap_or_else(|| "BevyCSharp".to_string()),
                            resolution: (config.width, config.height).into(),
                            present_mode,
                            ..default()
                        }),
                        ..default()
                    })
                    .set(RenderPlugin {
                        render_creation: RenderCreation::Automatic(Box::new(wgpu)),
                        ..default()
                    }),
            );
        }
    } else {
        use bevy::MinimalPlugins;
        use bevy::prelude::PluginGroup;

        let runner = if config.headless_fps > 0 {
            ScheduleRunnerPlugin::run_loop(Duration::from_secs_f64(
                1.0 / config.headless_fps as f64,
            ))
        } else {
            ScheduleRunnerPlugin::run_loop(Duration::ZERO)
        };
        app.add_plugins(MinimalPlugins.set(runner));
        app.add_plugins(bevy::input::InputPlugin);
        app.add_plugins(bevy::transform::TransformPlugin);
        // Assets are file loading and storage, with no GPU involvement, so a headless app can
        // serve them too. DefaultPlugins already includes this on a render build.
        app.add_plugins(bevy::asset::AssetPlugin::default());
    }

    let _ = title;

    // Bevy refuses to allocate a handle for an asset type it has not been told about, and says
    // so by panicking rather than failing the load. A render build gets these from the plugins
    // that own them; a headless one has to ask. Both calls are idempotent.
    {
        use bevy::asset::AssetApp;
        app.init_asset::<bevy::mesh::Mesh>();
        app.init_asset::<bevy::image::Image>();
        // Materials are data as much as meshes are, so a render build initialises them even
        // when the app is configured headless. Otherwise building one would fail on a bridge
        // that plainly has the renderer, which reads as a bug rather than a configuration.
        #[cfg(feature = "render")]
        app.init_asset::<bevy::pbr::StandardMaterial>();
    }

    // Pin the orderings that matter between exclusive C# systems.
    app.configure_sets(First, (BcsSet::Sync, BcsSet::First).chain());
    app.configure_sets(PostUpdate, (BcsSet::PostUpdate, BcsSet::Flush).chain());
    // `Cleanup` must come after `ExitCheck`, or on the final frame it would look for a pending
    // `AppExit` that has not been written yet and skip, with no later frame to catch it.
    app.configure_sets(
        Last,
        (
            BcsSet::Render,
            BcsSet::Last,
            BcsSet::ExitCheck,
            BcsSet::Cleanup,
        )
            .chain(),
    );

    // The `Cleanup` stage has to run *inside* the loop, on the final frame, because
    // `App::run` swaps the app out of the handle for an empty one before handing it to the
    // runner, so once `run` returns there is no world left to clean up. This system watches
    // for a pending `AppExit` and drains the callbacks on the frame the exit is decided.
    app.add_systems(
        Last,
        (move |world: &mut World| run_cleanup_on_exit(world, &cleanup)).in_set(BcsSet::Cleanup),
    );

    if config.headless_frames > 0 {
        app.insert_resource(HeadlessFrameLimit {
            remaining: config.headless_frames,
        });
        app.add_systems(Last, tick_frame_limit.in_set(BcsSet::ExitCheck));
    }

    app
}

/// Runs the `Cleanup` callbacks once, on the frame an exit is requested.
fn run_cleanup_on_exit(world: &mut World, cleanup: &CleanupList) {
    if world.contains_resource::<CleanupRan>() {
        return;
    }

    let exiting = world
        .get_resource::<bevy::ecs::message::Messages<AppExit>>()
        .is_some_and(|messages| !messages.is_empty());
    if !exiting {
        return;
    }

    world.insert_resource(CleanupRan);

    // Copy the callbacks out before loaning the world, so the lock is not held across
    // arbitrary managed code that might register more of them.
    let callbacks: Vec<SystemReg> = match cleanup.lock() {
        Ok(guard) => guard.clone(),
        Err(poisoned) => poisoned.into_inner().clone(),
    };

    loan_world(world, || {
        for callback in &callbacks {
            callback.invoke();
        }
    });
}

/// Counts down the headless frame budget and requests exit when it runs out.
fn tick_frame_limit(world: &mut World) {
    let done = match world.get_resource_mut::<HeadlessFrameLimit>() {
        Some(mut limit) => {
            limit.remaining = limit.remaining.saturating_sub(1);
            limit.remaining == 0
        }
        None => false,
    };
    if done {
        world.write_message(AppExit::Success);
    }
}

/// Creates the engine. Returns null on failure; the caller owns the handle.
///
/// # Safety
/// `config` must point to a valid [`BcsConfig`] for the duration of the call.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_app_create(config: *const BcsConfig) -> *mut BcsApp {
    crate::interop::guard_with(core::ptr::null_mut(), || {
        if config.is_null() {
            return core::ptr::null_mut();
        }
        let config = unsafe { *config };
        let title = unsafe { crate::interop::cstr_to_string(config.title) };
        let cleanup: CleanupList = Default::default();
        let app = build_app(&config, title, cleanup.clone());
        Box::into_raw(Box::new(BcsApp::new(app, cleanup)))
    })
}

/// Releases the engine and everything it owns.
///
/// # Safety
/// `handle` must come from [`bcs_app_create`] and must not be used afterwards.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_app_destroy(handle: *mut BcsApp) {
    crate::interop::guard_with((), || {
        if !handle.is_null() {
            drop(unsafe { Box::from_raw(handle) });
        }
    });
}

/// Registers a component layout with the Bevy world, returning its `ComponentId`.
///
/// The layout is padded to its alignment, which Bevy requires. `size` must therefore
/// already be a multiple of `align` for the round-trip to be lossless, which is what
/// `Unsafe.SizeOf<T>()` guarantees for a blittable C# struct.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string; `handle` must be a live app.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_component_register(
    handle: *mut BcsApp,
    name: *const core::ffi::c_char,
    size: u32,
    align: u32,
) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if align == 0 || !align.is_power_of_two() {
            return status::NULL_ARG;
        }
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
                bevy::ecs::component::StorageType::Table,
                layout.pad_to_align(),
                None,
                true,
                bevy::ecs::component::ComponentCloneBehavior::Default,
                None,
            )
        };

        let id = app.app.world_mut().register_component_with_descriptor(descriptor);
        id.index() as i32
    })
}

/// Registers a component layout while the app is already running.
///
/// [`bcs_app_create`]'s handle is mutably borrowed for the whole of [`bcs_app_run`], so a
/// system that wants a component type it has not seen before must register it through the
/// world loan instead of through the handle.
///
/// # Safety
/// `name` must be a NUL-terminated UTF-8 string.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_component_register_live(
    name: *const core::ffi::c_char,
    size: u32,
    align: u32,
) -> i32 {
    crate::interop::guard(|| {
        if align == 0 || !align.is_power_of_two() {
            return status::NULL_ARG;
        }
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
                    bevy::ecs::component::StorageType::Table,
                    layout.pad_to_align(),
                    None,
                    true,
                    bevy::ecs::component::ComponentCloneBehavior::Default,
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
/// components are the opposite problem: they are Rust types the managed side has no handle on,
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
                // Reached through the prelude rather than its defining crate: Visibility moved
                // from bevy_render to bevy_camera in 0.19, and the prelude survives such moves.
                #[cfg(feature = "render")]
                "Visibility" => world.register_component::<bevy::prelude::Visibility>(),
                #[cfg(feature = "render")]
                "InheritedVisibility" => {
                    world.register_component::<bevy::prelude::InheritedVisibility>()
                }
                _ => return status::NO_COMPONENT,
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
/// representation, which allows the compiler to reorder fields, and it does: the sixteen-byte
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

/// Registers a C# system in `stage`.
///
/// # Safety
/// `handle` must be a live app; `func` must remain callable until the app is destroyed.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_app_add_system(
    handle: *mut BcsApp,
    stage: i32,
    func: extern "C" fn(*mut c_void),
    user: *mut c_void,
) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if app.running {
            return status::ALREADY_RUNNING;
        }
        let Some(stage) = Stage::from_i32(stage) else {
            return status::NULL_ARG;
        };

        let reg = SystemReg { func, user };
        let run = move |world: &mut World| loan_world(world, || reg.invoke());

        match stage {
            Stage::Startup => {
                app.app.add_systems(Startup, run);
            }
            Stage::First => {
                app.app.add_systems(First, run.in_set(BcsSet::First));
            }
            Stage::PreUpdate => {
                app.app.add_systems(PreUpdate, run);
            }
            Stage::Update => {
                app.app.add_systems(Update, run);
            }
            Stage::PostUpdate => {
                app.app.add_systems(PostUpdate, run.in_set(BcsSet::PostUpdate));
            }
            Stage::Render => {
                app.app.add_systems(Last, run.in_set(BcsSet::Render));
            }
            Stage::Last => {
                app.app.add_systems(Last, run.in_set(BcsSet::Last));
            }
            Stage::FrameSync => {
                app.app.add_systems(First, run.in_set(BcsSet::Sync));
            }
            Stage::CommandFlush => {
                app.app.add_systems(PostUpdate, run.in_set(BcsSet::Flush));
            }
            Stage::Cleanup => {
                // Bevy has no post-loop schedule, so these are held in a shared list that the
                // in-loop cleanup system drains when an exit is requested.
                match app.cleanup.lock() {
                    Ok(mut guard) => guard.push(reg),
                    Err(poisoned) => poisoned.into_inner().push(reg),
                }
            }
        }

        status::OK
    })
}

/// Runs the app. Blocks until the window closes or an exit is requested, then runs the
/// `Cleanup` stage. Returns [`status::OK`] on a clean exit.
///
/// # Safety
/// `handle` must be a live app that has not been run before.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_app_run(handle: *mut BcsApp) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if app.running {
            return status::ALREADY_RUNNING;
        }
        app.running = true;

        // `App::run` moves the app out of `app.app`, leaving an empty one behind. Everything
        // that needs the real world (the `Cleanup` stage included) has to happen inside the
        // loop, which is why cleanup is a system rather than something done here.
        let exit = app.app.run();

        match exit {
            AppExit::Success => status::OK,
            AppExit::Error(code) => code.get() as i32,
        }
    })
}

/// Requests a graceful shutdown from inside a system callback.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_app_request_exit() -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            world.write_message(AppExit::Success);
            status::OK
        })
    })
}

/// Reports which native profile this library was built with: `1` if the renderer is
/// compiled in, `0` for a headless-only build.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_has_render() -> i32 {
    if cfg!(feature = "render") { 1 } else { 0 }
}

/// Reports whether the caller is on the process main thread.
///
/// macOS requires the window event loop to own the main thread, and violating that crashes
/// deep inside AppKit rather than anywhere useful. Only Apple platforms actually need the
/// check, so everything else answers yes and the managed guard becomes a no-op.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_is_main_thread() -> i32 {
    #[cfg(target_vendor = "apple")]
    {
        unsafe extern "C" {
            fn pthread_main_np() -> core::ffi::c_int;
        }

        // SAFETY: a libc call with no arguments and no preconditions.
        i32::from(unsafe { pthread_main_np() } != 0)
    }

    #[cfg(not(target_vendor = "apple"))]
    {
        1
    }
}

/// ABI version. C# refuses to load a native library whose version it does not know.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_abi_version() -> i32 {
    crate::ABI_VERSION
}

/// Describes the graphics adapter the renderer actually chose, as UTF-8.
///
/// Writes at most `capacity` bytes into `out` (not NUL-terminated) and returns the number of
/// bytes the description needs. A return value greater than `capacity` means nothing usable was
/// written; grow the buffer and call again. Returns [`status::UNSUPPORTED`] in a headless build
/// or before the renderer has initialised.
///
/// # Safety
/// `out` must be valid for `capacity` writes.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_adapter(out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            with_world(|world| {
                let Some(info) = world.get_resource::<bevy::render::renderer::RenderAdapterInfo>()
                else {
                    return status::UNSUPPORTED;
                };

                let text = format!(
                    "{:?} | {} | {:?} | {}",
                    info.backend, info.name, info.device_type, info.driver
                );
                let bytes = text.as_bytes();

                if capacity > 0 && !out.is_null() && bytes.len() <= capacity as usize {
                    // SAFETY: length checked against the caller's stated capacity.
                    unsafe { core::ptr::copy_nonoverlapping(bytes.as_ptr(), out, bytes.len()) };
                }

                bytes.len() as i32
            })
        }
    })
}

