//! A panic's stack as offsets into the bridge's library, with the library's build id, so a stack
//! written on a machine whose library carries no symbol table is named afterwards from the full
//! library kept for the same build (`build/name-crash.py`).
//!
//! std's own stack names a frame from the library's symbol table, which a stripped library does not
//! carry, and a frame's address alone says nothing past the run it was taken in, since the library
//! loads at another place each time. A frame's offset from where the library begins, read as the
//! symbol table reads its addresses, together with the build id that says which build's symbols
//! hold it, names the frame on any machine the library ran on.
//!
//! The frames are walked by the system's unwinder, `_Unwind_Backtrace` where the C runtime is
//! Unix's and `RtlCaptureStackBackTrace` on Windows, and the library's place, its end and its build
//! id are read from its own image as it is loaded, the ELF's program headers and its GNU build id
//! note, the Mach-O's segments and its UUID, or the PE's size and its CodeView record.

use std::fmt::Write;
use std::sync::OnceLock;

/// The most frames a stack is written with.
const MOST_FRAMES: usize = 128;

/// The bridge's library as it is loaded in this process.
struct Loaded {
    /// Where its image begins in memory.
    base: usize,
    /// Where its image ends in memory.
    end: usize,
    /// The address the image's first segment has in its own file, which a frame's offset from
    /// `base` is added to so it reads as the symbol table's addresses read, 0 for an ELF or a PE
    /// image and `__TEXT`'s address for a Mach-O one.
    file_base: usize,
    /// The build id, in hex, or `unknown` where the image carries none.
    build: String,
}

/// The library as it is loaded, read once for the process.
fn loaded() -> Option<&'static Loaded> {
    static LOADED: OnceLock<Option<Loaded>> = OnceLock::new();
    LOADED.get_or_init(|| image::find(written as fn() -> String as usize)).as_ref()
}

/// The stack this is called from, a line naming the bridge's build and a line for each frame, a
/// frame in the bridge as `bridge+0x` and its offset, and any other as its address.
///
/// An offset is the frame's return address, which lies a byte or more past the call it returns
/// from, so a namer looks up the byte before it.
pub fn written() -> String {
    let mut addresses = [0usize; MOST_FRAMES];
    let count = unwind::capture(&mut addresses);
    let mut text = String::new();
    let Some(bridge) = loaded() else {
        let _ = writeln!(text, "bridge not found among the loaded images, frames as addresses");
        for (index, address) in addresses[..count].iter().enumerate() {
            let _ = writeln!(text, "  {index}: 0x{address:x}");
        }
        return text;
    };

    let _ = writeln!(text, "bridge {} {}", image::FORMAT, bridge.build);
    for (index, &address) in addresses[..count].iter().enumerate() {
        if (bridge.base..bridge.end).contains(&address) {
            let _ = writeln!(text, "  {index}: bridge+0x{:x}", address - bridge.base + bridge.file_base);
        } else {
            let _ = writeln!(text, "  {index}: 0x{address:x}");
        }
    }
    text
}

/// Reads a value of `T` at `at` in the loaded image, where it may not be aligned.
///
/// # Safety
/// `at` must lie within a mapped part of the image with `size_of::<T>()` bytes after it.
unsafe fn read<T: Copy>(at: usize) -> T {
    // SAFETY: the caller keeps `at` within the mapped image, and an unaligned read takes any place.
    unsafe { (at as *const T).read_unaligned() }
}

/// The bytes in hex, in order.
#[cfg(not(windows))]
fn hex(bytes: &[u8]) -> String {
    bytes.iter().fold(String::with_capacity(bytes.len() * 2), |mut text, byte| {
        let _ = write!(text, "{byte:02x}");
        text
    })
}

#[cfg(unix)]
mod unwind {
    use core::ffi::c_void;

    /// The unwinder's context for one frame, which only its own functions read.
    #[repr(C)]
    struct Context {
        _private: [u8; 0],
    }

    /// The step's answer that walks on, and the one that stops the walk.
    const WALK_ON: i32 = 0;
    const STOP: i32 = 5;

    unsafe extern "C" {
        unsafe fn _Unwind_Backtrace(step: extern "C" fn(*mut Context, *mut c_void) -> i32, data: *mut c_void) -> i32;
        unsafe fn _Unwind_GetIP(context: *mut Context) -> usize;
    }

    /// The frames the walk has filled and the room for them.
    struct Filling<'a> {
        into: &'a mut [usize],
        count: usize,
    }

    extern "C" fn step(context: *mut Context, data: *mut c_void) -> i32 {
        // SAFETY: `capture` hands the walk a `Filling` that lives until the walk returns.
        let filling = unsafe { &mut *data.cast::<Filling<'_>>() };
        if filling.count >= filling.into.len() {
            return STOP;
        }
        // SAFETY: the unwinder hands each step a context that is valid for the step.
        filling.into[filling.count] = unsafe { _Unwind_GetIP(context) };
        filling.count += 1;
        WALK_ON
    }

    /// Walks the calling thread's stack into `into`, answering how many frames it filled.
    pub fn capture(into: &mut [usize]) -> usize {
        let mut filling = Filling { into, count: 0 };
        // SAFETY: the step is a function of the type the unwinder takes, and `filling` outlives the
        // walk, which returns before this does.
        unsafe { _Unwind_Backtrace(step, (&raw mut filling).cast()) };
        filling.count
    }
}

#[cfg(windows)]
mod unwind {
    use core::ffi::c_void;

    #[link(name = "kernel32")]
    unsafe extern "system" {
        unsafe fn RtlCaptureStackBackTrace(skip: u32, count: u32, frames: *mut *mut c_void, hash: *mut u32) -> u16;
    }

    /// Walks the calling thread's stack into `into`, answering how many frames it filled.
    pub fn capture(into: &mut [usize]) -> usize {
        let count = u32::try_from(into.len()).unwrap_or(u32::MAX).min(u32::from(u16::MAX));
        // SAFETY: `into` holds `count` pointer-sized slots, and the hash is not asked for.
        let filled = unsafe { RtlCaptureStackBackTrace(0, count, into.as_mut_ptr().cast(), core::ptr::null_mut()) };
        usize::from(filled)
    }
}

#[cfg(any(target_os = "linux", target_os = "android"))]
mod image {
    use super::{Loaded, hex, read};

    pub const FORMAT: &str = "elf";

    const LOAD: u32 = 1;
    const NOTE: u32 = 4;
    const GNU_BUILD_ID: u32 = 3;

    /// The ELF image holding `address`, its place, its end and its GNU build id note.
    pub fn find(address: usize) -> Option<Loaded> {
        let base = super::dl::base_of(address)?;
        // SAFETY: the dynamic loader maps an image's ELF header and program headers at its base,
        // and the reads stay within what the headers say the loaded segments hold.
        unsafe {
            if read::<[u8; 4]>(base) != *b"\x7fELF" {
                return None;
            }
            let headers = read::<u64>(base + 0x20) as usize;
            let size = read::<u16>(base + 0x36) as usize;
            let count = read::<u16>(base + 0x38) as usize;
            let (mut end, mut build) = (0usize, None);
            for index in 0..count {
                let header = base + headers + index * size;
                let kind = read::<u32>(header);
                let at = read::<u64>(header + 0x10) as usize;
                let length = read::<u64>(header + 0x28) as usize;
                if kind == LOAD {
                    end = end.max(at + length);
                } else if kind == NOTE && build.is_none() {
                    build = build_id(base + at, length);
                }
            }
            Some(Loaded { base, end: base + end, file_base: 0, build: build.unwrap_or_else(|| "unknown".into()) })
        }
    }

    /// The GNU build id among the notes in `length` bytes at `at`.
    ///
    /// # Safety
    /// The notes must be mapped.
    unsafe fn build_id(at: usize, length: usize) -> Option<String> {
        let mut note = at;
        while note + 12 <= at + length {
            let named = note + 12;
            // SAFETY: the caller keeps the notes mapped, and each read stays within them, the name's
            // four bytes read only where the header says it holds four.
            let (name, described, kind) = unsafe { (read::<u32>(note) as usize, read::<u32>(note + 4) as usize, read::<u32>(note + 8)) };
            let gnu = name == 4 && unsafe { read::<[u8; 4]>(named) } == *b"GNU\0";
            let description = named + name.next_multiple_of(4);
            if kind == GNU_BUILD_ID && gnu {
                // SAFETY: the description lies within the notes, as its header says.
                let bytes = unsafe { core::slice::from_raw_parts(description as *const u8, described) };
                return Some(hex(bytes));
            }
            note = description + described.next_multiple_of(4);
        }
        None
    }
}

#[cfg(target_vendor = "apple")]
mod image {
    use super::{Loaded, hex, read};

    pub const FORMAT: &str = "mach-o";

    const MAGIC_64: u32 = 0xfeed_facf;
    const SEGMENT_64: u32 = 0x19;
    const UUID: u32 = 0x1b;

    /// The Mach-O image holding `address`, its place, its end, its `__TEXT` address and its UUID.
    pub fn find(address: usize) -> Option<Loaded> {
        let base = super::dl::base_of(address)?;
        // SAFETY: the dynamic loader maps an image's Mach-O header and load commands at its base,
        // and the reads stay within the commands' size.
        unsafe {
            if read::<u32>(base) != MAGIC_64 {
                return None;
            }
            let count = read::<u32>(base + 16) as usize;
            let (mut command, mut text, mut end, mut build) = (base + 32, None, 0usize, None);
            for _ in 0..count {
                let kind = read::<u32>(command);
                let size = read::<u32>(command + 4) as usize;
                if kind == SEGMENT_64 {
                    let name = read::<[u8; 16]>(command + 8);
                    let at = read::<u64>(command + 24) as usize;
                    let length = read::<u64>(command + 32) as usize;
                    if name.starts_with(b"__TEXT\0") {
                        text = Some(at);
                    }
                    end = end.max(at + length);
                } else if kind == UUID {
                    build = Some(hex(&read::<[u8; 16]>(command + 8)));
                }
                command += size;
            }
            let text = text?;
            Some(Loaded { base, end: base + (end - text), file_base: text, build: build.unwrap_or_else(|| "unknown".into()) })
        }
    }
}

#[cfg(windows)]
mod image {
    use super::{Loaded, read};
    use core::ffi::c_void;
    use std::fmt::Write;

    pub const FORMAT: &str = "pe";

    /// GetModuleHandleExW's flags for the module holding an address, its count of references left
    /// as it is.
    const FROM_ADDRESS: u32 = 0x4;
    const UNCHANGED_REFCOUNT: u32 = 0x2;
    const CODEVIEW: u32 = 2;

    #[link(name = "kernel32")]
    unsafe extern "system" {
        unsafe fn GetModuleHandleExW(flags: u32, name: *const u16, module: *mut *mut c_void) -> i32;
    }

    /// The PE image holding `address`, its place, its end and its CodeView GUID and age.
    pub fn find(address: usize) -> Option<Loaded> {
        let mut module = core::ptr::null_mut();
        // SAFETY: with FROM_ADDRESS the name is read as an address within the module, which
        // `address` is, and the module's handle is written to `module`.
        if unsafe { GetModuleHandleExW(FROM_ADDRESS | UNCHANGED_REFCOUNT, address as *const u16, &raw mut module) } == 0 {
            return None;
        }
        let base = module as usize;
        // SAFETY: a loaded module's headers lie at its base, and its debug directory and the
        // CodeView record it points at lie within the image.
        unsafe {
            let headers = base + read::<u32>(base + 0x3c) as usize;
            if read::<[u8; 4]>(headers) != *b"PE\0\0" {
                return None;
            }
            let optional = headers + 24;
            let size = read::<u32>(optional + 56) as usize;
            let directory = base + read::<u32>(optional + 112 + 6 * 8) as usize;
            let length = read::<u32>(optional + 112 + 6 * 8 + 4) as usize;
            let mut build = None;
            for entry in (directory..directory + length).step_by(28) {
                if read::<u32>(entry + 12) == CODEVIEW {
                    let record = base + read::<u32>(entry + 20) as usize;
                    if read::<[u8; 4]>(record) == *b"RSDS" {
                        build = Some(guid_and_age(record + 4));
                    }
                }
            }
            Some(Loaded { base, end: base + size, file_base: 0, build: build.unwrap_or_else(|| "unknown".into()) })
        }
    }

    /// A CodeView GUID and age as a symbol server names a PDB by them, the GUID's fields in order
    /// as hex and the age after it.
    ///
    /// # Safety
    /// The GUID and the age must lie within the image.
    unsafe fn guid_and_age(at: usize) -> String {
        // SAFETY: the caller keeps the record within the image.
        let (first, second, third, rest, age) = unsafe { (read::<u32>(at), read::<u16>(at + 4), read::<u16>(at + 6), read::<[u8; 8]>(at + 8), read::<u32>(at + 16)) };
        let mut text = format!("{first:08X}{second:04X}{third:04X}");
        for byte in rest {
            let _ = write!(text, "{byte:02X}");
        }
        let _ = write!(text, "{age:X}");
        text
    }
}

#[cfg(all(unix, not(any(target_os = "linux", target_os = "android", target_vendor = "apple"))))]
mod image {
    use super::Loaded;

    pub const FORMAT: &str = "unknown";

    /// No image is read on a system whose image format is not one of the three.
    pub fn find(_address: usize) -> Option<Loaded> {
        None
    }
}

#[cfg(unix)]
mod dl {
    use core::ffi::{c_char, c_void};

    /// What `dladdr` says of an address, of which the image's base is read.
    #[repr(C)]
    struct Info {
        file: *const c_char,
        base: *mut c_void,
        symbol: *const c_char,
        symbol_at: *mut c_void,
    }

    unsafe extern "C" {
        unsafe fn dladdr(address: *const c_void, info: *mut Info) -> i32;
    }

    /// Where the image holding `address` begins, or nothing where the loader knows of none.
    pub fn base_of(address: usize) -> Option<usize> {
        let mut info = Info { file: core::ptr::null(), base: core::ptr::null_mut(), symbol: core::ptr::null(), symbol_at: core::ptr::null_mut() };
        // SAFETY: `info` is a place for dladdr's answer, and the address is only looked up.
        let found = unsafe { dladdr(address as *const c_void, &raw mut info) };
        (found != 0 && !info.base.is_null()).then_some(info.base as usize)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn the_stack_names_the_bridges_build_and_a_frame_in_it() {
        let text = written();
        let first = text.lines().next().unwrap_or_default();
        assert!(first.starts_with("bridge ") && !first.ends_with(" unknown"), "{text}");
        assert!(text.contains(": bridge+0x"), "{text}");
    }
}
