using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Bevy;
using Bevy.Scripting;
using Xunit;

namespace Bevy.Tests;

/// <summary>A script's generation, let go once the script is compiled again.</summary>
/// <remarks>
/// <para>
/// A generation is an assembly in a context that can be unloaded, and anything of the process's or
/// the app's that keeps one of its types, methods or delegates keeps the whole of it, so a host
/// compiling on every save would gather every generation for as long as the app lives, and a
/// registration the process kept would run a stale one in every app made after. Taken from
/// 3DEngine's test of the same name (its <c>d7e370ed</c>), where the process's list of
/// registrations held each one.
/// </para>
/// <para>
/// Each script holds a kind of thing a game's module initializers and systems put in the process's
/// and the app's tables, a behavior's registration, a component's schema, a system, a state with
/// systems scoped to it, a message of its own and a resource of its own, under the same names in
/// each generation as an edited script has them. A class named for its generation alone finds the
/// generation among every load context. The script is compiled again while the app runs, and the
/// first generation is looked for while the app still lives, as an editor's would be.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class ScriptGenerationTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-script-generations-");

    public void Dispose() => _folder.Dispose();

    public static TheoryData<string> Scripts => new()
    {
        "",
        "[Behavior] public partial struct Lamp { public float Glow; }",
        "[Behavior] public partial struct Lamp { public float Glow; [OnUpdate] public void Warm(BehaviorContext ctx) => Glow += 1f; }",
        "[Behavior] public partial struct Lamp { [OnUpdate] public static void Warm(BehaviorContext ctx) { } }",
        "[InitialState(Off)] public enum Lit { Off, On }",
        "[InitialState(Off)] public enum Lit { Off, On } [Behavior] public partial struct Lamp { [OnUpdate, InState(Lit.Off)] public static void Warm(BehaviorContext ctx) { } }",
        "[InitialState(Off)] public enum Lit { Off, On } [Behavior] public partial struct Lamp { [OnEnter(Lit.On)] public static void Warm(BehaviorContext ctx) { } }",
        "public readonly record struct Rang(int Times); [Behavior] public partial struct Bell { [OnUpdate] public static void Ring(BehaviorContext ctx) { ctx.Send(new Rang(1)); ctx.World.InsertResource(new Rang(2)); } }",
    };

    [Theory]
    [MemberData(nameof(Scripts))]
    public void AGenerationIsUnloadedWhenItsScriptIsCompiledAgain(string script)
    {
        WeakReference first;
        bool collectibleKept;

        using var harness = new EngineHarness(frames: 4);
        harness.App.EnableDynamicSystems();
        var host = new ScriptHost(harness.App, _folder.Path);
        try
        {
            Write("First" + Guid.NewGuid().ToString("N"), script, out var marker);
            Assert.True(host.Reload(), host.LastError);
            first = Generation(marker);
            collectibleKept = BehaviorRegistry.Snapshot().Any(register => register.Method.Module.Assembly.IsCollectible);

            var frame = 0;
            harness.On(Stage.Update, world =>
            {
                if (frame++ != 1) return;

                Write("Second" + Guid.NewGuid().ToString("N"), script, out _);
                Assert.True(host.Reload(), host.LastError);
            });
            harness.Run();

            for (var i = 0; i < 20 && first.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        finally
        {
            host.Retire();
        }

        Assert.False(collectibleKept, "the process's list of registrations keeps a script's, which every app after would run");
        Assert.False(first.IsAlive, "the app or the process holds the first generation while the app lives");
    }

    private void Write(string marker, string script, out string written)
    {
        written = marker;
        File.WriteAllText(_folder.File("Lamp.cs"), $"using Bevy;\nnamespace Generations;\npublic sealed class {marker};\n{script}\n");
    }

    /// <summary>
    /// The load context holding the generation that declares the marker, held weakly, from a method
    /// of its own so no local of the caller's keeps it.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Generation(string marker) =>
        new(AssemblyLoadContext.All.Single(context => context.Assemblies.Any(assembly => assembly.GetType("Generations." + marker) is not null)));
}
