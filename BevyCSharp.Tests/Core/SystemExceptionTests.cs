using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A system that throws in every frame has its exception logged whole once and then counted, at
/// each power of ten and in all as the run ends, rather than a trace every frame.
/// </summary>
[Collection("engine")]
public sealed class SystemExceptionTests
{
    [Fact]
    [ExpectsError("system", "Tests.Thrower")]
    public void AnExceptionThrownEveryFrameIsLoggedWholeOnceThenCounted()
    {
        using var app = new App(new Config { Headless = true, HeadlessFrames = 25, FailFastOnSystemException = false });
        var ran = 0;
        app.AddSystem(Stage.Update, new SystemDescriptor(_ =>
        {
            ran++;
            throw new InvalidOperationException("deliberate");
        }, "Tests.Thrower"));

        var logged = new List<string>();
        void Heard(App? by, string kind, string text, Exception? exception)
        {
            if (by == app && kind == "system") lock (logged) logged.Add(text);
        }

        EngineLog.ErrorLogged += Heard;
        try
        {
            app.Run();
        }
        finally
        {
            EngineLog.ErrorLogged -= Heard;
        }

        Assert.True(ran >= 10, $"the system ran {ran} times");
        Assert.Equal(3, logged.Count);
        Assert.StartsWith("[BevyCSharp] System 'Tests.Thrower' threw InvalidOperationException: deliberate", logged[0]);
        Assert.Contains(" at ", logged[0]);
        Assert.Equal("[BevyCSharp] System 'Tests.Thrower' has thrown InvalidOperationException 10 times, the last: deliberate", logged[1]);
        Assert.Equal($"[BevyCSharp] System 'Tests.Thrower' threw InvalidOperationException {ran} times in all", logged[2]);
    }
}
