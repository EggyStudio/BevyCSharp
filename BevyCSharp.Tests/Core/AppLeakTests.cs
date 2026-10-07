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
/// SHARED.md's row on an app's whole life, which 3DEngine's <c>AppLeakTests</c> holds (its
/// <c>c06ec659</c>), where every app read the runtime's assemblies for a script compiler and held
/// them in native memory until a full collection came. Resident memory is read before any
/// collection, since memory a closed app gives back only to a finalizer is held until one comes,
/// and the heap after one. The first twenty warm what stays for the process, the pools and the
/// threads, which the next eighty should not add to.
/// </para>
/// <para>
/// The heap is read after every tenth app, and judged by how far its floor rose, the least reading
/// from the twentieth app to the fiftieth against the least from the seventieth to the hundredth,
/// as 3DEngine's has been since its <c>596535ce</c>. On macOS the heap rises and falls back by
/// several megabytes every few dozen apps, and the two readings it was judged by before fell on a
/// trough and a crest, where a leak raises the floor as it raises every reading. Where
/// <c>BCS_GCDUMP</c> names <c>dotnet-gcdump</c>, as the macOS job has it, the heap's types are
/// counted after the twentieth app and the hundredth, so a failure names what grew.
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

            GC.Collect();
            GC.WaitForPendingFinalizers();
            var heap = GC.GetTotalMemory(forceFullCollection: true) / 1e6;
            heaps.Add((i, heap));
            output.WriteLine($"{i,3} apps: resident {resident:0} MB, heap {heap:0.00} MB");

            if (i == 20) residentAt20 = resident;
            if (i == 100) residentAt100 = resident;

            if (i == 100 && censusAt20 is not null)
            {
                var (census, failure) = HeapCensus.Take();
                grown = census is null ? $"no census, {failure}" : census.GrownSince(censusAt20, 5);
            }
        }

        var floorRise = heaps.Where(r => r.Apps >= 70).Min(r => r.Heap) - heaps.Where(r => r.Apps is >= 20 and <= 50).Min(r => r.Heap);
        var series = $"{Environment.NewLine}the heap after every ten apps in MB, "
            + string.Join(", ", heaps.Select(r => $"{r.Apps}: {r.Heap:0.00}"))
            + (grown is null ? "" : $"{Environment.NewLine}grown from the twentieth app to the hundredth, {grown}");

        Assert.True(floorRise < 5, $"the heap's floor rose {floorRise:0.0} MB from the twentieth app to the hundredth{series}");
        Assert.True(residentAt100 - residentAt20 < 50, $"the process held {residentAt20:0} MB after twenty apps and {residentAt100:0} MB after a hundred{series}");
    }
}
