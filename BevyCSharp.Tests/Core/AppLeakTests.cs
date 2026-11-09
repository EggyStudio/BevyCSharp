using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// An app made and closed a hundred times gives back what it took, which a suite making hundreds of
/// them and a program starting a level as a new app both need.
/// </summary>
/// <remarks>
/// <para>
/// SHARED.md's row on an app's whole life. An app that reads the runtime's assemblies for a script
/// compiler can hold them in native memory until a full collection comes. Resident memory is read
/// before any collection, since memory a closed app gives back only to a finalizer is held until
/// one comes, and the heap after one. The first twenty warm what stays for the process, the pools
/// and the threads, which the next eighty should not add to.
/// </para>
/// <para>
/// The heap is read after every tenth app as the bytes the last full blocking collection kept, its
/// live objects, and judged by how far its floor rose, the least reading from the seventieth app to
/// the hundredth against the greater of the twentieth's and the thirtieth's, so a heap that swings
/// is judged against the top of its early swing. <see cref="GC.GetTotalMemory"/> counts with the
/// live objects what a collection left on the heap where it did not compact, which on macOS rises
/// and falls back by megabytes every few dozen apps, and on a run climbs by megabytes over the
/// hundred apps while the census finds well under a megabyte more alive. A leak of an eighth of a
/// megabyte an app still fails, over the forty apps from the thirtieth to the seventieth.
/// </para>
/// <para>
/// Where <c>BCS_GCDUMP</c> names <c>dotnet-gcdump</c>, as the macOS job has it, the heap's types
/// are counted after the twentieth app and the hundredth, so a failure names what grew, and the
/// output says it on a pass. The census's own session makes the runtime's event source build its
/// manifest by reflection once, its parameter and method infos and its table of events, which grow
/// the heap between the first census and the next and not after, so those are the census's and no
/// app's.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class AppLeakTests(ITestOutputHelper output)
{
    [Fact]
    public void AnAppMadeAndClosedAHundredTimesLeavesNothingBehind()
    {
        double residentAt20 = 0, residentAt100 = 0;
        var heaps = new List<(int Apps, double Heap)>();
        HeapCensus? censusAt20 = null;
        string? grown = null;

        for (var i = 1; i <= 100; i++)
        {
            using (var harness = new EngineHarness(frames: 2, discoverBehaviors: true))
                harness.Run();

            if (i % 10 != 0) continue;

            var resident = Environment.WorkingSet / 1e6;

            // The twentieth app's census is taken before its heap is read, so what the census keeps
            // is in both readings and not in what is compared, and the hundredth's after.
            if (i == 20 && HeapCensus.Available)
            {
                (censusAt20, var failure) = HeapCensus.Take();
                if (failure is not null) grown = $"no census, {failure}";
            }

            // The live objects, the bytes the last full blocking collection kept, since what an
            // uncompacted collection leaves on the heap swings on macOS (the remarks say by how much).
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var heap = GC.GetGCMemoryInfo(GCKind.FullBlocking).PromotedBytes / 1e6;
            heaps.Add((i, heap));
            output.WriteLine($"{i,3} apps: resident {resident:0} MB, heap {heap:0.00} MB");

            if (i == 20) residentAt20 = resident;
            if (i == 100) residentAt100 = resident;

            if (i == 100 && censusAt20 is not null)
            {
                var (census, failure) = HeapCensus.Take();
                grown = census is null ? $"no census, {failure}" : census.GrownSince(censusAt20, 5);
                output.WriteLine($"grown from the twentieth app to the hundredth, {grown}");
            }
        }

        var floorRise = heaps.Where(r => r.Apps >= 70).Min(r => r.Heap) - heaps.Where(r => r.Apps is 20 or 30).Max(r => r.Heap);
        var series = $"{Environment.NewLine}the heap after every ten apps in MB, "
            + string.Join(", ", heaps.Select(r => $"{r.Apps}: {r.Heap:0.00}"))
            + (grown is null ? "" : $"{Environment.NewLine}grown from the twentieth app to the hundredth, {grown}");

        Assert.True(floorRise < 5, $"the heap's floor rose {floorRise:0.0} MB from the twentieth app to the hundredth{series}");
        Assert.True(residentAt100 - residentAt20 < 50, $"the process held {residentAt20:0} MB after twenty apps and {residentAt100:0} MB after a hundred{series}");
    }
}
