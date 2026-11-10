using Bevy;
using Bevy.Scripting;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A script calls the members an extension block gives a type as that type's own, as a game's
/// compiled code does, through the compiler the script host carries.
/// </summary>
/// <remarks>
/// The members global illumination and the sky give <see cref="Render"/>, and those the rendering
/// gives <see cref="EcsWorld"/>, are declared in extension blocks, which a compiler older than the
/// language's own reads as static methods of the blocks' classes alone, so a script naming them on
/// the types they extend compiles only where the host's compiler reads the blocks.
/// </remarks>
[Collection("engine")]
public sealed class ScriptExtensionTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-script-extensions-");

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void AScriptCallsTheMembersABlockGivesATypeAsTheTypesOwn()
    {
        using var harness = new EngineHarness(frames: 1);
        harness.App.EnableDynamicSystems();
        var host = new ScriptHost(harness.App, _folder.Path);
        try
        {
            File.WriteAllText(_folder.File("Lit.cs"), """
                using Bevy;

                namespace Extended;

                [Behavior]
                public partial struct Lit
                {
                    [OnUpdate]
                    public static void Light(BehaviorContext ctx)
                    {
                        Render.SetGlobalIllumination(GlobalIllumination.Off);
                        Render.SetClouds(Entity.None, null);
                        ctx.Ecs.SpawnPointLight(Vec3.Zero);
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
