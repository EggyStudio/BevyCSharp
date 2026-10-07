using Bevy;
using Bevy.Scripting;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>A script compiled a hundred times holds what it held after ten.</summary>
/// <remarks>
/// Each compilation read every assembly the process had loaded as a reference, and each reference
/// holds its file's whole image in native memory that only its finalizer gives back, while the GC's
/// heap stays small and a full collection comes late, so a host compiling on each save gathered
/// them. 3DEngine's test job reached the runner's 16 GB that way (its <c>c06ec659</c>). The process's
/// resident memory is read before any collection, as a program that allocates little on the heap
/// would hold it.
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

        // 346 MB more after a hundred than after ten, each compilation reading the references again,
        // and 20 MB more once they were read once, which is the heap no full collection has come for.
        Assert.True(atHundred - atTen < 48, $"the process held {atTen:0} MB after ten compilations and {atHundred:0} MB after a hundred");
    }
}
