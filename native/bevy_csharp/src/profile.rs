//! What a frame spends, split where this bridge can see the splits, into the managed systems Bevy
//! calls, the calls those make back across the C ABI, the schedule around them and the render.
//!
//! Off unless asked for, since even counting costs something on every crossing. While on, every
//! crossing is counted and one in [`SAMPLE_EVERY`] is timed, into atomics that
//! [`bcs_profile_take`] reads and clears, so a reader asks for a span of frames and divides.
//!
//! A crossing is counted only while a managed system runs, which is where a game's calls are. The
//! calls made while an app is built, and the one call that holds the whole run, are left out, as
//! they would swamp the frames they are not part of.

use core::cell::Cell;
use std::sync::atomic::{AtomicBool, AtomicU64, Ordering};
use std::time::Instant;

use bevy::prelude::*;

use crate::interop::status;

/// Whether anything is counted.
static ON: AtomicBool = AtomicBool::new(false);

static FRAMES: AtomicU64 = AtomicU64::new(0);
static FRAME_NANOS: AtomicU64 = AtomicU64::new(0);
static SCHEDULE_NANOS: AtomicU64 = AtomicU64::new(0);
static MANAGED_CALLS: AtomicU64 = AtomicU64::new(0);
static MANAGED_NANOS: AtomicU64 = AtomicU64::new(0);
static CROSSINGS: AtomicU64 = AtomicU64::new(0);
static RENDERS: AtomicU64 = AtomicU64::new(0);
static RENDER_NANOS: AtomicU64 = AtomicU64::new(0);
static CROSSING_NANOS: AtomicU64 = AtomicU64::new(0);

thread_local! {
    /// How many managed systems this thread is inside, so a crossing is counted only from one.
    static IN_MANAGED: Cell<u32> = const { Cell::new(0) };

    /// How many crossings this thread is inside, so one made from inside another is not counted
    /// twice.
    static IN_CROSSING: Cell<u32> = const { Cell::new(0) };
}

/// Whether counting is on, read once per call by the paths it wraps.
#[inline]
pub fn on() -> bool {
    ON.load(Ordering::Relaxed)
}

/// One crossing in this many is timed, and the rest counted, with the time scaled up from those.
///
/// Timing one takes two clock reads, which on this machine is longer than a cheap crossing itself,
/// so timing every one would double what it measured. A sample of a few hundred calls a frame is
/// as good an average as all of them.
const SAMPLE_EVERY: u64 = 32;

static SAMPLED: AtomicU64 = AtomicU64::new(0);

/// Runs a call from C#, counting it, and timing one in [`SAMPLE_EVERY`], when it was made from a
/// managed system.
#[inline]
pub fn crossing<T>(f: impl FnOnce() -> T) -> T {
    if !on() || IN_MANAGED.with(Cell::get) == 0 || IN_CROSSING.with(Cell::get) > 0 {
        return f();
    }

    let counted = CROSSINGS.fetch_add(1, Ordering::Relaxed);
    if counted % SAMPLE_EVERY != 0 {
        return f();
    }

    IN_CROSSING.with(|depth| depth.set(depth.get() + 1));
    let started = Instant::now();
    let result = f();
    CROSSING_NANOS.fetch_add(started.elapsed().as_nanos() as u64, Ordering::Relaxed);
    SAMPLED.fetch_add(1, Ordering::Relaxed);
    IN_CROSSING.with(|depth| depth.set(depth.get() - 1));

    result
}

/// Runs a managed system, counting it and its time, the crossings it makes included.
#[inline]
pub fn managed(f: impl FnOnce()) {
    if !on() {
        f();
        return;
    }

    // A system run from inside a crossing, as a script loaded mid-frame runs its startup, is the
    // crossing's time and counted there.
    let nested = IN_CROSSING.with(Cell::get) > 0;
    let outer = IN_CROSSING.with(|depth| depth.replace(0));

    IN_MANAGED.with(|depth| depth.set(depth.get() + 1));
    let started = Instant::now();
    f();
    if !nested {
        MANAGED_NANOS.fetch_add(started.elapsed().as_nanos() as u64, Ordering::Relaxed);
        MANAGED_CALLS.fetch_add(1, Ordering::Relaxed);
    }
    IN_MANAGED.with(|depth| depth.set(depth.get() - 1));
    IN_CROSSING.with(|depth| depth.set(outer));
}

/// When the frame being counted began, and when its schedule did.
#[derive(Resource, Default)]
struct FrameClock {
    began: Option<Instant>,
}

/// Starts a frame's count at the top of `First`, and counts the frame before it whole.
fn frame_began(mut clock: ResMut<FrameClock>) {
    let now = Instant::now();

    if on() {
        if let Some(began) = clock.began {
            FRAME_NANOS.fetch_add(now.duration_since(began).as_nanos() as u64, Ordering::Relaxed);
            FRAMES.fetch_add(1, Ordering::Relaxed);
        }
    }

    clock.began = Some(now);
}

/// Counts the main schedule's time at the end of `Last`, which is the frame less the waiting
/// between frames and the render running beside it.
fn frame_ended(clock: Res<FrameClock>) {
    if !on() {
        return;
    }

    if let Some(began) = clock.began {
        SCHEDULE_NANOS.fetch_add(began.elapsed().as_nanos() as u64, Ordering::Relaxed);
    }
}

/// Puts the frame's two clock systems in.
pub fn install(app: &mut App) {
    app.init_resource::<FrameClock>();
    app.add_systems(First, frame_began);
    app.add_systems(Last, frame_ended);
}

/// The phases of Bevy's render schedule, in the order it runs them, which [`bcs_profile_phases`]
/// names in the same order.
#[cfg(feature = "render")]
const PHASES: usize = 11;

/// Each phase's time over the frames counted.
static PHASE_NANOS: [AtomicU64; 11] = [const { AtomicU64::new(0) }; 11];

/// When each phase of the render schedule being counted began, and when the last one ended.
#[cfg(feature = "render")]
#[derive(Resource, Default)]
struct RenderClock {
    marks: [Option<Instant>; 12],
}

/// Notes when phase `I` began, or with `I` past the last phase, when the schedule ended.
#[cfg(feature = "render")]
fn mark<const I: usize>(mut clock: ResMut<RenderClock>) {
    let now = Instant::now();
    if I == 0 {
        clock.marks = [None; 12];
    }
    clock.marks[I] = Some(now);

    if I != PHASES || !on() {
        return;
    }

    if let Some(began) = clock.marks[0] {
        RENDER_NANOS.fetch_add(now.duration_since(began).as_nanos() as u64, Ordering::Relaxed);
        RENDERS.fetch_add(1, Ordering::Relaxed);
    }

    for phase in 0..PHASES {
        if let (Some(from), Some(to)) = (clock.marks[phase], clock.marks[phase + 1]) {
            PHASE_NANOS[phase].fetch_add(to.duration_since(from).as_nanos() as u64, Ordering::Relaxed);
        }
    }
}

/// Puts clock systems at the two ends of the render schedule, which runs beside the main one on a
/// thread of its own, so its whole time is seen and not only the passes the GPU timings name:
/// preparing what was extracted, queuing it, submitting it, and waiting on the GPU where it waits.
#[cfg(feature = "render")]
pub fn install_render(app: &mut App) {
    use bevy::render::{Render as Schedule, RenderApp, RenderSystems as Set};

    let Some(render_app) = app.get_sub_app_mut(RenderApp) else {
        return;
    };

    render_app.init_resource::<RenderClock>();
    render_app.add_systems(Schedule, mark::<0>.before(Set::ExtractCommands));
    render_app.add_systems(Schedule, mark::<1>.after(Set::ExtractCommands).before(Set::PrepareMeshes));
    render_app.add_systems(Schedule, mark::<2>.after(Set::PrepareMeshes).before(Set::CreateViews));
    render_app.add_systems(Schedule, mark::<3>.after(Set::CreateViews).before(Set::Specialize));
    render_app.add_systems(Schedule, mark::<4>.after(Set::Specialize).before(Set::PrepareViews));
    render_app.add_systems(Schedule, mark::<5>.after(Set::PrepareViews).before(Set::Queue));
    render_app.add_systems(Schedule, mark::<6>.after(Set::Queue).before(Set::PhaseSort));
    render_app.add_systems(Schedule, mark::<7>.after(Set::PhaseSort).before(Set::Prepare));
    render_app.add_systems(Schedule, mark::<8>.after(Set::Prepare).before(Set::Render));
    render_app.add_systems(Schedule, mark::<9>.after(Set::Render).before(Set::Cleanup));
    render_app.add_systems(Schedule, mark::<10>.after(Set::Cleanup).before(Set::PostCleanup));
    render_app.add_systems(Schedule, mark::<11>.after(Set::PostCleanup));
}

/// The render schedule's phases by name, a line each, in the order [`bcs_profile_phases`] gives
/// their times.
const PHASE_NAMES: &str = "extract commands\nprepare meshes\ncreate views\nspecialize\nprepare views\nqueue\nsort\nprepare\nrender\ncleanup\npost cleanup";

/// Each phase's time since the last read, in nanoseconds, which starts its count over, and their
/// names, a line each, in the same order.
///
/// # Safety
/// `nanos` must be writable for `count` values, and `names` for `capacity` bytes or null when
/// `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_profile_phases(nanos: *mut u64, count: i32, names: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        if !nanos.is_null() {
            for (phase, total) in PHASE_NANOS.iter().enumerate().take(count.max(0) as usize) {
                unsafe { *nanos.add(phase) = total.swap(0, Ordering::Relaxed) };
            }
        }

        unsafe { crate::interop::write_text(PHASE_NAMES, names, capacity) }
    })
}

/// What a span of frames spent, as [`bcs_profile_take`] hands it over.
#[repr(C)]
#[derive(Default)]
pub struct BcsProfile {
    /// Frames counted whole.
    pub frames: u64,
    /// Their time from the top of one to the top of the next.
    pub frame_nanos: u64,
    /// Their time from the top of `First` to the end of `Last`.
    pub schedule_nanos: u64,
    /// Managed systems run.
    pub managed_calls: u64,
    /// Their time, the crossings they made included.
    pub managed_nanos: u64,
    /// Calls the managed systems made into this bridge.
    pub crossings: u64,
    /// Those calls' time, from entering the bridge to leaving it, scaled up from the ones timed,
    /// less what timing them cost.
    pub crossing_nanos: u64,
    /// Render schedules run, which a pipelined render runs beside the frames they draw.
    pub renders: u64,
    /// Their time from first system to last.
    pub render_nanos: u64,
}

const _: () = assert!(core::mem::size_of::<BcsProfile>() == 72);

/// Turns counting on or off, clearing what was counted.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_profile_enable(on: i32) -> i32 {
    crate::interop::guard(|| {
        take();
        ON.store(on != 0, Ordering::Relaxed);
        status::OK
    })
}

/// Does nothing, through the same guard every entry point takes, for timing a crossing alone.
///
/// A call into the bridge costs the transition out of .NET and back and the guard around it,
/// which the bridge's own clock cannot see, since it starts inside. Timed in a loop from C#,
/// this is that cost.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_profile_noop() -> i32 {
    crate::interop::guard(|| status::OK)
}

/// Reads what was counted since the last read, and starts the count over.
///
/// # Safety
/// `out` must point to a writable [`BcsProfile`].
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_profile_take(out: *mut BcsProfile) -> i32 {
    crate::interop::guard(|| {
        if out.is_null() {
            return status::NULL_ARG;
        }

        unsafe { *out = take() };
        status::OK
    })
}

/// What timing nothing costs, two clock reads and the arithmetic between them, which a sampled
/// crossing's time holds besides the crossing itself.
fn clock_nanos() -> u64 {
    const TIMES: u32 = 1000;

    let started = Instant::now();
    let mut kept = 0u128;
    for _ in 0..TIMES {
        let inner = Instant::now();
        kept += core::hint::black_box(inner.elapsed()).as_nanos();
    }
    let _ = core::hint::black_box(kept);

    (started.elapsed().as_nanos() / u128::from(TIMES)) as u64
}

fn take() -> BcsProfile {
    // The sampled crossings' time scaled up to all of them.
    let crossings = CROSSINGS.swap(0, Ordering::Relaxed);
    let sampled = SAMPLED.swap(0, Ordering::Relaxed);
    let timed = CROSSING_NANOS.swap(0, Ordering::Relaxed);
    let timed = timed.saturating_sub(sampled * clock_nanos());
    let crossing_nanos = if sampled == 0 { 0 } else { (timed as u128 * crossings as u128 / sampled as u128) as u64 };

    BcsProfile {
        frames: FRAMES.swap(0, Ordering::Relaxed),
        frame_nanos: FRAME_NANOS.swap(0, Ordering::Relaxed),
        schedule_nanos: SCHEDULE_NANOS.swap(0, Ordering::Relaxed),
        managed_calls: MANAGED_CALLS.swap(0, Ordering::Relaxed),
        managed_nanos: MANAGED_NANOS.swap(0, Ordering::Relaxed),
        crossings,
        crossing_nanos,
        renders: RENDERS.swap(0, Ordering::Relaxed),
        render_nanos: RENDER_NANOS.swap(0, Ordering::Relaxed),
    }
}
