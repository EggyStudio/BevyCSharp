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
/// from the seventieth app to the hundredth against the greater of the twentieth's and the
/// thirtieth's, as 3DEngine's resident memory is judged from the greatest of its early readings.
/// On macOS the heap rises and falls back by several megabytes every few dozen apps, a leak raises
/// the floor as it raises every reading, and a trough after the base is not a rise. On the macOS
/// suite of <c>5f051299</c>, build run 39547498414, the heap read 101.10 and 103.34 MB at the twentieth app and the
/// thirtieth, fell to 90.84 at the fortieth and climbed back 2 MB every ten apps to 102.94 at the
/// hundredth, while its census found 0.3 MB more alive, and the least of the twentieth to the
/// fiftieth, the trough, made a rise of 6.0 where the base read here makes none. A leak of an
/// eighth of a megabyte an app still fails, over the forty apps from the thirtieth to the
/// seventieth.
/// </para>
/// <para>
/// Where <c>BCS_GCDUMP</c> names <c>dotnet-gcdump</c>, as the macOS job has it, the heap's types are
/// counted after the twentieth app and the hundredth, so a failure names what grew, and the output
/// says it on a pass. The census's own session makes the runtime's event source build its manifest
/// by reflection once, some 683 parameter infos, 253 method infos and its table of events, which a
/// census added at the sixtieth app found grown from the twentieth and not from the sixtieth to the
/// hundredth, on Linux with <c>BCS_GCDUMP</c> set and this test alone run, so those are the census's
/// and no app's.
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
