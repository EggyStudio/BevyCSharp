//! What this library was built with and what the running app installed, which the managed side
//! asks before it reaches for something one profile leaves out.

use crate::interop::status;
#[cfg(feature = "render")]
use crate::state::with_world;

/// Reports which native profile this library was built with: `1` if the renderer is
/// compiled in, `0` for a headless-only build.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_has_render() -> i32 {
    if cfg!(feature = "render") { 1 } else { 0 }
}

/// Reports which UI profile this library was built with: `1` if the HTML and CSS surface is
/// compiled in, `0` otherwise.
///
/// Separate from [`bcs_has_render`] because the editor profile is a superset of the render one. A
/// build can draw without carrying the document surface, and the managed side has to be able to
/// tell those apart before it opens a panel.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_has_editor() -> i32 {
    if cfg!(feature = "editor") { 1 } else { 0 }
}

/// Reports whether this library carries a game's assets compiled in: `1` when it was built with
/// `--embed`, `0` otherwise.
///
/// A library built so reads every asset from what it carries, whatever asset root an app names,
/// which an app reading its own files from that root has to know.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_has_embedded_assets() -> i32 {
    if cfg!(feature = "embed") { 1 } else { 0 }
}

/// Whether this app installed the interface, as [`bcs_has_interface`] reports.
pub(crate) static INTERFACE_INSTALLED: std::sync::atomic::AtomicBool =
    std::sync::atomic::AtomicBool::new(false);

/// Reports whether the running app installed the interface: `1` when it did, `0` otherwise.
///
/// A different question again from [`bcs_has_editor`], which answers what the library was built
/// with rather than what this app asked for. A build carrying the surface still draws no interface
/// unless the config turned it on, and something drawing one needs to know which of the two is
/// missing before it tells anybody to rebuild.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_has_interface() -> i32 {
    if INTERFACE_INSTALLED.load(std::sync::atomic::Ordering::Relaxed) { 1 } else { 0 }
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
/// or before the renderer has initialized.
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

                // SAFETY: the caller's contract for `out` and `capacity` is the one
                // `write_text` documents, which is the convention every entry point here
                // returning text follows.
                unsafe { crate::interop::write_text(&text, out, capacity) }
            })
        }
    })
}
