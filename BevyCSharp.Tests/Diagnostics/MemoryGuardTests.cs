using System.Runtime;
using System.Runtime.InteropServices;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the memory cap, the memory the process holds read as it grows, the most it held, the cap
/// taken from the config before the environment, and what is said past it.
/// </summary>
/// <remarks>
/// The stop itself ends the process, which here is the whole suite, so it is tried on a running
/// app instead, and what it decides and says is tried here.
/// </remarks>
[Collection("engine")]
public sealed partial class MemoryGuardTests
{
    [Fact]
    public unsafe void TheMemoryHeldIsReadAsItGrows()
    {
        const nuint size = 256 << 20;

        // The reading is the whole process's, so whatever the rest of the process would free is
        // freed before the first reading. The garbage earlier tests left, the 64 megabyte array of
        // the test beside this one among it, is collected and given back first, a full collection
        // with its finalizers twice over, since a finalizer can free what the second collection
        // takes, and aggressive, so the collector decommits what it freed now rather than over the
        // seconds after. Then no collection runs until the second reading, since one in between
        // would give pages back while this counts them in, and on macOS, whose allocator returns
        // freed blocks to the system at once, the footprint would read the growth short by what it
        // gave back.
        for (var pass = 0; pass < 2; pass++)
        {
            GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
        }

        var above = OperatingSystem.IsLinux() ? LeaveFreedResident() : null;
        var still = GC.TryStartNoGCRegion(16 << 20);
        try
        {
            var before = MemoryGuard.ResidentBytes();

            // A quarter of a gigabyte mapped from the system rather than asked of an allocator, the
            // GC's or the C library's, either of which hands a request pages it holds where they
            // fit, glibc from what it keeps freed inside its heap before it maps anew, so the pages
            // written would read as nothing grown. A byte a page written, so its pages are the
            // machine's and not only promised. Read on Windows as the private bytes committed,
            // which Windows cannot trim as it trims a working set, which can read a fraction of
            // this, and on macOS as the physical footprint, which counts the pages the system
            // compressed under pressure, which the resident size leaves out on a runner short of
            // memory.
            var held = MapFresh(size);
            Assert.True(held != null, "the system mapped nothing");
            try
            {
                for (nuint at = 0; at < size; at += 4096) held[at] = 1;
                var after = MemoryGuard.ResidentBytes();

                // Three quarters of it, since the C allocator and the threads the runtime keeps
                // may still give pages back while this runs.
                Assert.True(before > 0);
                Assert.True(after - before > 192L << 20, $"the process held {before} bytes and then {after}");
            }
            finally
            {
                Unmap(held, size);
            }
        }
        finally
        {
            if (still && GCSettings.LatencyMode == GCLatencyMode.NoGCRegion) GC.EndNoGCRegion();

            if (above != null)
            {
                NativeMemory.Free(above);
                MemoryGuard.GiveBackFreed();
            }
        }
    }

    /// <summary>
    /// Leaves glibc's heap holding a third of a gigabyte freed and resident, as an earlier test's
    /// frees can leave it, and answers the block kept above it, which the caller frees.
    /// </summary>
    /// <remarks>
    /// In blocks smaller than glibc maps alone, so they come from its heap, each written a byte a
    /// page so its pages are the machine's, and freed with the block above them kept, so they join
    /// into one free stretch inside the heap rather than its end, which a free gives back at once.
    /// </remarks>
    private static unsafe byte* LeaveFreedResident()
    {
        const int count = 5120;
        const nuint size = 64 << 10;
        var blocks = new byte*[count];
        for (var i = 0; i < count; i++)
        {
            blocks[i] = (byte*)NativeMemory.Alloc(size);
            for (nuint at = 0; at < size; at += 4096) blocks[i][at] = 1;
        }

        var above = (byte*)NativeMemory.Alloc(size);
        for (var i = 0; i < count; i++) NativeMemory.Free(blocks[i]);
        return above;
    }

    /// <summary>
    /// <paramref name="size"/> bytes mapped from the system, pages the process held none of before,
    /// none resident until written, or null where the system maps none.
    /// </summary>
    private static unsafe byte* MapFresh(nuint size)
    {
        const uint Commit = 0x1000, Reserve = 0x2000, ReadWrite = 0x04;
        if (OperatingSystem.IsWindows()) return (byte*)VirtualAlloc(0, size, Commit | Reserve, ReadWrite);

        // Readable and writable, private and anonymous, whose flag is another number on macOS.
        const int Read = 1, Write = 2, Private = 2;
        var anonymous = OperatingSystem.IsMacOS() ? 0x1000 : 0x20;
        var mapped = Mmap(0, size, Read | Write, Private | anonymous, -1, 0);
        return mapped == -1 ? null : (byte*)mapped;
    }

    /// <summary>Gives back what <see cref="MapFresh"/> mapped.</summary>
    private static unsafe void Unmap(byte* block, nuint size)
    {
        const uint Release = 0x8000;
        if (OperatingSystem.IsWindows()) VirtualFree((nint)block, 0, Release);
        else Munmap((nint)block, size);
    }

    [LibraryImport("libc", EntryPoint = "mmap")]
    private static partial nint Mmap(nint address, nuint length, int protection, int flags, int descriptor, nint offset);

    [LibraryImport("libc", EntryPoint = "munmap")]
    private static partial int Munmap(nint address, nuint length);

    [LibraryImport("kernel32.dll", EntryPoint = "VirtualAlloc")]
    private static partial nint VirtualAlloc(nint address, nuint size, uint type, uint protection);

    [LibraryImport("kernel32.dll", EntryPoint = "VirtualFree")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool VirtualFree(nint address, nuint size, uint type);

    /// <summary>
    /// Memory the C allocator holds free inside its heap is given back to the system as an app ends,
    /// where glibc kept it for whatever came next, as it keeps what a GPU driver drawing on the
    /// processor freed with an app, so a test host running app after app held more with each.
    /// </summary>
    [SkippableFact]
    public unsafe void WhatTheCAllocatorHoldsFreeIsGivenBackAsAnAppEnds()
    {
        Skip.IfNot(OperatingSystem.IsLinux(), "glibc keeps freed memory in its heaps, where the other systems' allocators choose for themselves");

        // Half a gigabyte in blocks smaller than glibc maps alone, so they come from its heap, and
        // every other one freed, the last kept, so each freed block lies between two that stay and
        // inside the heap, where a free keeps it, and none joins another or the heap's end, which a
        // free gives back at once. A byte a page written, so their pages are the machine's and not
        // only promised.
        const int count = 8192;
        const nuint size = 64 << 10;
        var blocks = new byte*[count];
        for (var i = 0; i < count; i++)
        {
            blocks[i] = (byte*)NativeMemory.Alloc(size);
            for (nuint at = 0; at < size; at += 4096) blocks[i][at] = 1;
        }

        try
        {
            for (var i = 0; i < count; i += 2) NativeMemory.Free(blocks[i]);
            var freed = MemoryGuard.ResidentBytes();

            using (var harness = new EngineHarness(frames: 1)) harness.Run();
            var after = MemoryGuard.ResidentBytes();

            Assert.True(freed - after > 128L << 20, $"the process held {freed} bytes with half the blocks freed and {after} once an app had ended");
        }
        finally
        {
            for (var i = 1; i < count; i += 2) NativeMemory.Free(blocks[i]);
        }
    }

    /// <summary>
    /// A C library without glibc's call, as musl is, or none to load, answers so the first time an app
    /// ends and is not asked again, and the app ends as it would.
    /// </summary>
    [SkippableFact]
    public void ACLibraryWithoutTheCallIsAskedOnce()
    {
        Skip.IfNot(OperatingSystem.IsLinux(), "the memory is given back on Linux alone");

        var trim = MemoryGuard.Trim;
        var asked = 0;
        try
        {
            MemoryGuard.Trim = _ =>
            {
                asked++;
                throw new EntryPointNotFoundException("Unable to find an entry point named 'malloc_trim' in shared library 'libc'.");
            };
            MemoryGuard.TrimMissing = false;

            for (var app = 0; app < 2; app++)
                using (var harness = new EngineHarness(frames: 1)) harness.Run();

            Assert.Equal(1, asked);
            Assert.True(MemoryGuard.TrimMissing);
        }
        finally
        {
            MemoryGuard.Trim = trim;
            MemoryGuard.TrimMissing = false;
        }
    }

    /// <summary>
    /// The peak is never below a resident size read, the readings being all it has where the
    /// system answers no peak of its own, as macOS does.
    /// </summary>
    [Fact]
    public void ThePeakIsAtLeastTheMostTheProcessWasReadToHold()
    {
        var held = new byte[64 << 20];
        for (var at = 0; at < held.Length; at += 4096) held[at] = 1;
        var resident = MemoryGuard.ResidentBytes();
        GC.KeepAlive(held);

        Assert.True(MemoryGuard.PeakBytes() >= resident, $"the peak was {MemoryGuard.PeakBytes()} after a reading of {resident}");
    }

    [Fact]
    public void PastTheCapIsSaidWithBothAndTheVariableAndNotBelowIt()
    {
        const long Gigabyte = 1L << 30;

        Assert.Null(MemoryGuard.Past(3 * Gigabyte, 4 * Gigabyte));
        Assert.Null(MemoryGuard.Past(9 * Gigabyte, 0));

        var said = MemoryGuard.Past(5 * Gigabyte, 4 * Gigabyte);
        Assert.NotNull(said);
        Assert.Contains("5.00 GB", said, StringComparison.Ordinal);
        Assert.Contains("4.00 GB", said, StringComparison.Ordinal);
        Assert.Contains(MemoryGuard.Variable, said, StringComparison.Ordinal);
    }

    [Fact]
    public void TheConfigsCapComesBeforeTheEnvironmentsAndTheDefaultIsAtMostEight()
    {
        var was = Environment.GetEnvironmentVariable(MemoryGuard.Variable);
        try
        {
            Environment.SetEnvironmentVariable(MemoryGuard.Variable, "3.5");
            Assert.Equal(3.5, MemoryGuard.CapFor(new Config()));
            Assert.Equal(2, MemoryGuard.CapFor(new Config { MemoryCap = 2 }));

            Environment.SetEnvironmentVariable(MemoryGuard.Variable, "not a number");
            Assert.Equal(0, MemoryGuard.CapFor(new Config()));
        }
        finally
        {
            Environment.SetEnvironmentVariable(MemoryGuard.Variable, was);
        }

        Assert.InRange(MemoryGuard.DefaultCap, 0.1, 8);
    }
}
