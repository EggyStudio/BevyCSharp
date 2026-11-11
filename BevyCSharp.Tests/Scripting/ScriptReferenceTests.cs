using Bevy;
using Bevy.Scripting;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A script calls a member of the library whose signature names a module's type, as a game's
/// compiled code does, the host compiling it against every assembly the library names.
/// </summary>
/// <remarks>
/// <c>Render.SetWeather</c> takes the Sky module's WeatherState, whose assembly the process loads
/// only once something has set a weather, so a host compiling against the loaded assemblies alone
/// fails the script for want of the type, though nothing in it is wrong.
/// </remarks>
[Collection("engine")]
public sealed class ScriptReferenceTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-script-references-");

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void AScriptCallsAMemberNamingAModulesType()
    {
        using var harness = new EngineHarness(frames: 1);
        harness.App.EnableDynamicSystems();
        var host = new ScriptHost(harness.App, _folder.Path);
        try
        {
            File.WriteAllText(_folder.File("Weathered.cs"), """
                using Bevy;
                using Sky;

                namespace Weathered;

                [Behavior]
                public partial struct Clear
                {
                    [OnUpdate]
                    public static void Weather(BehaviorContext ctx)
                    {
                        WeatherState? none = null;
                        Render.SetWeather(Entity.None, none);
                    }
                }
                """);

            Assert.True(host.Reload(), host.LastError);
        }
        finally
        {
            host.Retire();
        }
    }
}
