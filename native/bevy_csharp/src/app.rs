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
            use bevy::window::{PresentMode, Window, WindowPlugin};

            let present_mode = if config.vsync != 0 {
                PresentMode::AutoVsync
            } else {
                PresentMode::AutoNoVsync
            };

            app.add_plugins(DefaultPlugins.set(WindowPlugin {
                primary_window: Some(Window {
                    title: title.clone().unwrap_or_else(|| "BevyCSharp".to_string()),
                    resolution: (config.width as f32, config.height as f32).into(),
                    present_mode,
                    ..default()
                }),
                ..default()
            }));
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
    }

    let _ = title;

    // Pin the orderings that matter between exclusive C# systems.
    app.configure_sets(First, (BcsSet::Sync, BcsSet::First).chain());
    app.configure_sets(PostUpdate, (BcsSet::PostUpdate, BcsSet::Flush).chain());
    // `Cleanup` must come after `ExitCheck`, or on the final frame it would look for a pending
    // `AppExit` that has not been written yet and skip - with no later frame to catch it.
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
    // runner - so once `run` returns there is no world left to clean up. This system watches
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
/// already be a multiple of `align` for the round-trip to be lossless — which is what
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
        // that needs the real world - the `Cleanup` stage included - has to happen inside the
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

/// ABI version. C# refuses to load a native library whose version it does not know.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_abi_version() -> i32 {
    crate::ABI_VERSION
}
