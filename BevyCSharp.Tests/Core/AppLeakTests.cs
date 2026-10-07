using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// An app made and closed a hundred times gives back what it took, which a suite making hundreds of
/// them and a program starting a level as a new app both need.
/// </summary>
/// <remarks>
/// SHARED.md's row on an app's whole life, which 3DEngine's <c>AppLeakTests</c> holds (its
/// <c>c06ec659</c>), where every app read the runtime's assemblies for a script compiler and held
/// them in native memory until a full collection came. Resident memory is read before any
/// collection, since memory a closed app gives back only to a finalizer is held until one comes,
/// and the heap after one. The first twenty warm what stays for the process, the pools and the
/// threads, which the next eighty should not add to.
/// </remarks>
[Collection("engine")]
public sealed class AppLeakTests(ITestOutputHelper output)
{
    [Fact]
    public void AnAppMadeAndClosedAHundredTimesLeavesNothingBehind()
    {
        double residentAt20 = 0, residentAt100 = 0, heapAt20 = 0, heapAt100 = 0;

        for (var i = 1; i <= 100; i++)
        {
            using (var harness = new EngineHarness(frames: 2, discoverBehaviors: true))
                harness.Run();

            if (i is not (20 or 100)) continue;

            var resident = Environment.WorkingSet / 1e6;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var heap = GC.GetTotalMemory(forceFullCollection: true) / 1e6;
            output.WriteLine($"{i,3} apps: resident {resident:0} MB, heap {heap:0.0} MB");

            if (i == 20) (residentAt20, heapAt20) = (resident, heap);
            else (residentAt100, heapAt100) = (resident, heap);
        }

        Assert.True(heapAt100 - heapAt20 < 5, $"the heap held {heapAt20:0.0} MB after twenty apps and {heapAt100:0.0} MB after a hundred");
        Assert.True(residentAt100 - residentAt20 < 50, $"the process held {residentAt20:0} MB after twenty apps and {residentAt100:0} MB after a hundred");
    }
}
