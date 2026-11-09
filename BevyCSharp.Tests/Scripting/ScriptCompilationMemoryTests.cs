using Bevy;
using Bevy.Scripting;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>A script compiled a hundred times holds what it held after ten.</summary>
/// <remarks>
/// A reference holds its file's whole image in native memory that only its finalizer gives back,
/// while the GC's heap stays small and a full collection comes late, so a host that read every
/// loaded assembly again at each compilation would gather them with each save. The host reads each
/// file once for the process, and the test holds it to that. The process's resident memory is read
/// before any collection, as a program that allocates little on the heap would hold it.
/// </remarks>
[Collection("engine")]
public sealed class ScriptCompilationMemoryTests(ITestOutputHelper output) : IDisposable
{
    private readonly TestFolder _folder = new("bcs-script-compilations-");

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void AScriptCompiledAHundredTimesHoldsWhatItHeldAfterTen()
    {
        File.WriteAllText(_folder.File("Spin.cs"), """
            using Bevy;

            namespace Compilations;

            [Behavior]
            public partial struct Spin
            {
                public float Speed;

                [OnUpdate]
                public void Turn(BehaviorContext ctx) => Speed += ctx.Time.Delta;
            }
            """);

        using var harness = new EngineHarness(frames: 1);
        harness.App.EnableDynamicSystems();
        var host = new ScriptHost(harness.App, _folder.Path);
        double atTen = 0, atHundred = 0;
        try
        {
            for (var i = 1; i <= 100; i++)
            {
                Assert.True(host.Reload(), host.LastError);
                if (i % 10 != 0) continue;

                var resident = Environment.WorkingSet / 1e6;
                output.WriteLine($"{i,3} compilations: resident {resident:0} MB, {GC.CollectionCount(2)} full collections");
                if (i == 10) atTen = resident;
                if (i == 100) atHundred = resident;
            }
        }
        finally
        {
            host.Retire();
        }

        // A compilation reading the references again each time grows the process by hundreds of
        // megabytes over ninety compilations, where read once they leave a few tens of megabytes,
        // the heap no full collection has come for, under the bound of 48.
        Assert.True(atHundred - atTen < 48, $"the process held {atTen:0} MB after ten compilations and {atHundred:0} MB after a hundred");
    }
}
