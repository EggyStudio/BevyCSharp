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
public sealed class MemoryGuardTests
{
    [Fact]
    public unsafe void TheMemoryHeldIsReadAsItGrows()
    {
        const nuint size = 256 << 20;
        var before = MemoryGuard.ResidentBytes();

        // A quarter of a gigabyte from the system rather than from the GC, which may hand an array
        // pages it kept resident from earlier tests, so the growth read would be what it held back
        // rather than what the guard reads. A byte a page written, so its pages are the machine's
        // and not only promised.
        var held = (byte*)NativeMemory.Alloc(size);
        try
        {
            for (nuint at = 0; at < size; at += 4096) held[at] = 1;
            var after = MemoryGuard.ResidentBytes();

            // Three quarters of it, since the reading is the whole process's, and the rest of the
            // process, the GC and the C allocator among it, may give pages back while this runs.
            Assert.True(before > 0);
            Assert.True(after - before > 192L << 20, $"the process held {before} bytes and then {after}");
        }
        finally
        {
            NativeMemory.Free(held);
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
