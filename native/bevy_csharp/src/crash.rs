//! What a panic said, kept for the managed side, and a panic that does not come back to it handed
//! to its crash log as it happens.
//!
//! A panic inside a call from C# is caught by [`crate::interop::guard`] and comes back as a status,
//! and the managed side reads what it said with [`bcs_last_panic`] to put it in the exception it
//! throws, which the crash log writes if nothing catches it. A panic on a thread of Bevy's own,
//! the render thread or a task pool's, has no call from C# to come back through, and may end the
//! process before any status is read, so the hook hands it to the writer the managed side gave
//! [`bcs_crash_writer`] there and then, which writes the same crash file the managed side writes
//! for an exception. Bevy carries a panic in one of its tasks over to the thread waiting on it, so
//! such a panic is often written here and again as the exception it becomes, and the managed side
//! keeps both in one file.
//!
//! A panic the bridge expects and catches, as Bevy's decoder throws for a file that is no sound, is
//! made inside [`quietly`], which the hook says nothing of and the process's earlier hook is not
//! told of either.
//!
//! From the frame the app begins ending until it is destroyed ([`ending`]), a panic on a thread of
//! Bevy's own is kept and printed as the process's hook prints one, and not written as a crash.
//! Bevy's file watcher panicked there as the app ended, sending an event on a channel the asset
//! server had already closed, and the crash file it was written to made the next run say the last
//! one crashed when it had ended as it was asked to.

use core::cell::Cell;
use std::sync::atomic::{AtomicBool, AtomicUsize, Ordering};
use std::sync::{Mutex, Once};

/// What the most recent panic said, with where it was and the stack that led there.
static LAST: Mutex<String> = Mutex::new(String::new());

/// The managed side's writer for a panic outside the guard, as a function pointer, or zero for
/// none.
static WRITER: AtomicUsize = AtomicUsize::new(0);

/// Whether the app has begun ending, after which a panic outside the guard is not a crash.
static ENDING: AtomicBool = AtomicBool::new(false);

/// Says the app has begun ending, on the frame an exit is decided, or that it is gone, once it has
/// been destroyed or another is made.
pub fn ending(ended: bool) {
    ENDING.store(ended, Ordering::Release);
}

/// Whether the app has begun ending, from the frame an exit is decided.
pub fn is_ending() -> bool {
    ENDING.load(Ordering::Acquire)
}

thread_local! {
    /// How many guards this thread is inside, a panic inside one coming back as a status.
    static GUARDED: Cell<u32> = const { Cell::new(0) };

    /// Whether this thread is running something whose panic is expected and caught.
    static QUIET: Cell<bool> = const { Cell::new(false) };
}

/// Runs `f`, which catches what it calls, as inside the guard, so a panic in it is one the managed
/// side hears as a status rather than one written as it happens.
pub fn guarded<T>(f: impl FnOnce() -> T) -> T {
    GUARDED.with(|depth| depth.set(depth.get() + 1));
    let result = f();
    GUARDED.with(|depth| depth.set(depth.get() - 1));
    result
}

/// Runs `f`, which catches a panic it expects, with that panic kept from the hook and from the
/// process's earlier hook, which would print it as though it were a fault.
pub fn quietly<T>(f: impl FnOnce() -> T) -> T {
    install();
    let was = QUIET.with(|quiet| quiet.replace(true));
    let result = f();
    QUIET.with(|quiet| quiet.set(was));
    result
}

/// Puts the hook before the process's panic hook, once for the process.
pub fn install() {
    static ONCE: Once = Once::new();
    ONCE.call_once(|| {
        let previous = std::panic::take_hook();
        std::panic::set_hook(Box::new(move |info| {
            if QUIET.with(Cell::get) {
                return;
            }

            said(info);
            previous(info);
        }));
    });
}

/// Keeps what a panic said, and hands it to the managed side's writer where it will not come back
/// as a status.
fn said(info: &std::panic::PanicHookInfo<'_>) {
    let text = describe(info);

    // A lock poisoned by a panic while it was held keeps the text it had, since a hook that
    // panicked itself would end the process with nothing said.
    if let Ok(mut last) = LAST.lock() {
        last.clone_from(&text);
    }

    if GUARDED.with(Cell::get) > 0 || ENDING.load(Ordering::Acquire) {
        return;
    }

    let writer = WRITER.load(Ordering::Acquire);
    if writer != 0 {
        // SAFETY: only `bcs_crash_writer` stores here, and it stores a function of this type.
        let write: extern "C" fn(*const u8, i32) = unsafe { core::mem::transmute(writer) };
        write(text.as_ptr(), i32::try_from(text.len()).unwrap_or(i32::MAX));
    }
}

/// A panic as the crash file has it, the thread, where, what it said and the stack.
fn describe(info: &std::panic::PanicHookInfo<'_>) -> String {
    let thread = std::thread::current();
    let name = thread.name().unwrap_or("unnamed");

    let payload = info.payload();
    let message = payload
        .downcast_ref::<&str>()
        .copied()
        .or_else(|| payload.downcast_ref::<String>().map(String::as_str))
        .unwrap_or("a panic with no message");

    let place = info
        .location()
        .map(|at| format!("{}:{}:{}", at.file(), at.line(), at.column()))
        .unwrap_or_else(|| "a place it did not say".to_string());

    // Captured whatever RUST_BACKTRACE says, since a player's machine has it unset and the stack
    // is the most of what a report from one can say.
    let stack = std::backtrace::Backtrace::force_capture();
    format!("thread '{name}' panicked at {place}:\n{message}\n\nstack:\n{stack}")
}

/// Gives the bridge the managed side's writer for a panic outside the guard, and puts the hook in.
///
/// The writer is called on the thread that panicked, which may be one of Bevy's, with the text
/// as UTF-8 and its length in bytes, and must not unwind.
///
/// # Safety
/// `writer` must be null or a function that may be called from any thread for as long as the
/// process runs.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_crash_writer(writer: Option<extern "C" fn(*const u8, i32)>) -> i32 {
    crate::interop::guard(|| {
        install();
        WRITER.store(writer.map_or(0, |write| write as usize), Ordering::Release);
        crate::interop::status::OK
    })
}

/// Writes what the most recent panic said into `out`, or nothing where none has happened.
///
/// Answers the text's length in bytes, which a buffer too small for it answers too, so the caller
/// asks again with room for it.
///
/// # Safety
/// `out` must be writable for `capacity` bytes, or null when `capacity` is zero.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_last_panic(out: *mut u8, capacity: i32) -> i32 {
    crate::interop::guard(|| {
        let Ok(last) = LAST.lock() else {
            return 0;
        };
        unsafe { crate::interop::write_text(&last, out, capacity) }
    })
}

/// Panics on purpose, for a crash log to be tried: inside the guard when `guarded` is not zero,
/// which answers a status, and otherwise on a thread of its own, which the hook writes as it
/// happens and which ends that thread alone.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_panic_on_purpose(guarded: i32) -> i32 {
    if guarded != 0 {
        return crate::interop::guard(|| panic!("a panic asked for, to try the crash log"));
    }

    crate::interop::guard(|| {
        let thread = std::thread::Builder::new()
            .name("panic on purpose".to_string())
            .spawn(|| panic!("a panic asked for on a thread of its own, to try the crash log"));

        match thread.map(std::thread::JoinHandle::join) {
            Ok(Err(_)) => crate::interop::status::OK,
            _ => crate::interop::status::INVALID_STATE,
        }
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::sync::Mutex as Gate;

    /// The tests here change the process's hook and writer, so they take turns.
    static TURN: Gate<()> = Gate::new(());

    static WRITTEN: Mutex<String> = Mutex::new(String::new());

    extern "C" fn keep(text: *const u8, length: i32) {
        let bytes = unsafe { std::slice::from_raw_parts(text, length as usize) };
        *WRITTEN.lock().unwrap() = String::from_utf8_lossy(bytes).into_owned();
    }

    fn last() -> String {
        LAST.lock().unwrap().clone()
    }

    #[test]
    fn a_panic_inside_the_guard_is_kept_and_not_written() {
        let _turn = TURN.lock();
        unsafe { bcs_crash_writer(Some(keep)) };
        WRITTEN.lock().unwrap().clear();

        assert_eq!(crate::interop::status::PANIC, bcs_panic_on_purpose(1));
        assert!(last().contains("a panic asked for, to try the crash log"), "{}", last());
        assert!(last().contains("stack:"));
        assert!(WRITTEN.lock().unwrap().is_empty(), "a panic the guard caught was written as a crash");

        unsafe { bcs_crash_writer(None) };
    }

    #[test]
    fn a_panic_on_a_thread_of_its_own_is_written_as_it_happens() {
        let _turn = TURN.lock();
        unsafe { bcs_crash_writer(Some(keep)) };
        WRITTEN.lock().unwrap().clear();

        assert_eq!(crate::interop::status::OK, bcs_panic_on_purpose(0));
        let written = WRITTEN.lock().unwrap().clone();
        assert!(written.contains("thread 'panic on purpose' panicked at"), "{written}");
        assert!(written.contains("on a thread of its own"));

        unsafe { bcs_crash_writer(None) };
    }

    #[test]
    fn a_panic_once_the_app_has_begun_ending_is_kept_and_not_written() {
        let _turn = TURN.lock();
        unsafe { bcs_crash_writer(Some(keep)) };
        WRITTEN.lock().unwrap().clear();

        ending(true);
        assert_eq!(crate::interop::status::OK, bcs_panic_on_purpose(0));
        ending(false);

        assert!(last().contains("on a thread of its own"), "{}", last());
        assert!(WRITTEN.lock().unwrap().is_empty(), "a panic after the app began ending was written as a crash");

        unsafe { bcs_crash_writer(None) };
    }

    #[test]
    fn a_panic_made_quietly_is_neither_kept_nor_written() {
        let _turn = TURN.lock();
        unsafe { bcs_crash_writer(Some(keep)) };
        WRITTEN.lock().unwrap().clear();
        LAST.lock().unwrap().clear();

        let caught = quietly(|| std::panic::catch_unwind(|| panic!("expected and caught")));
        assert!(caught.is_err());
        assert!(last().is_empty(), "{}", last());
        assert!(WRITTEN.lock().unwrap().is_empty());

        unsafe { bcs_crash_writer(None) };
    }
}
