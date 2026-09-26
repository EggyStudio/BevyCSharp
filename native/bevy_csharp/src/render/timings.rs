//! How long each render pass takes, on the CPU that records it and on the GPU that runs it.
//!
//! A technique made of a dozen passes is tuned one pass at a time, and a frame's total says nothing
//! about which. Bevy measures its own passes with timestamp queries where the adapter has them, and
//! the bridge measures each dispatch, pass and draw a shader program makes under the program's file
//! name, so a package's passes sit in the same list as Bevy's shadows and tonemapping.
//!
//! Off unless the app asks (`Config.GpuTimings`), since every measured pass writes timestamps and
//! every frame reads them back. Where the adapter has no timestamp queries only CPU times arrive.

#[cfg(not(feature = "render"))]
use crate::interop::status;

/// What the last frame's timings came to, as text, which the entry point hands out at any time
/// rather than only from inside a system, so a command line or an editor panel can ask.
#[cfg(feature = "render")]
static TIMINGS: std::sync::Mutex<String> = std::sync::Mutex::new(String::new());

/// Forgets the timings of an app before this one, which is called for every app built, since the
/// text outlives the app that wrote it and one that did not ask for timings would read another's.
#[cfg(feature = "render")]
pub fn forget() {
    if let Ok(mut timings) = TIMINGS.lock() {
        timings.clear();
    }
}

/// Adds Bevy's render diagnostics and what copies them out.
#[cfg(feature = "render")]
pub fn install(app: &mut bevy::app::App) {
    app.add_plugins(bevy::render::diagnostic::RenderDiagnosticsPlugin);
    app.add_systems(bevy::app::Last, collect);
}

/// Writes every render timing Bevy holds as a line each, `name`, CPU and GPU milliseconds apart by
/// tabs, smoothed over the last frames so a number can be read. A pass the GPU did not time has
/// `-1` for it.
#[cfg(feature = "render")]
fn collect(store: Option<bevy::ecs::system::Res<bevy::diagnostic::DiagnosticsStore>>) {
    use std::collections::BTreeMap;
    use std::fmt::Write;

    let Some(store) = store else { return };
    let mut passes: BTreeMap<String, (f64, f64)> = BTreeMap::new();

    for diagnostic in store.iter() {
        let path = diagnostic.path().as_str();

        let Some(rest) = path.strip_prefix("render/") else { continue };
        let Some((name, field)) = rest.rsplit_once('/') else { continue };
        let Some(value) = diagnostic.smoothed() else { continue };

        let entry = passes.entry(name.to_string()).or_insert((-1.0, -1.0));

        match field {
            "elapsed_cpu" => entry.0 = value,
            "elapsed_gpu" => entry.1 = value,
            _ => {}
        }
    }

    let mut text = String::new();

    for (name, (cpu, gpu)) in passes {
        let _ = writeln!(text, "{name}\t{cpu:.4}\t{gpu:.4}");
    }

    if let Ok(mut timings) = TIMINGS.lock() {
        *timings = text;
    }
}

/// Copies the last frame's render timings out, by the text convention, a line a pass: its name,
/// then CPU and GPU milliseconds, apart by tabs, `-1` where there is none. Empty where the app did
/// not ask for timings.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_render_timings(out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        #[cfg(not(feature = "render"))]
        {
            let _ = (out, capacity);
            status::UNSUPPORTED
        }

        #[cfg(feature = "render")]
        {
            let text = TIMINGS.lock().map(|text| text.clone()).unwrap_or_default();
            unsafe { crate::interop::write_text(&text, out, capacity) }
        }
    })
}
