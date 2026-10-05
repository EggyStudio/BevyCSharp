// Bevy's plugin example, examples/app/plugin.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Application;

// Demonstrates a plugin of a game's own, made and added, which prints its message once a second.
internal static class PluginExample
{
    public static void Build(App app) =>
        app.AddPlugin(new PrintMessagePlugin(TimeSpan.FromSeconds(1), "This is an example plugin"));

    /// <summary>Prints a message every time a wait passes.</summary>
    private sealed class PrintMessagePlugin(TimeSpan wait, string message) : IPlugin
    {
        public void Build(App app)
        {
            app.Startup(ctx => ctx.World.InsertResource(new PrintMessageState(message, (float)wait.TotalSeconds)), "plugin.Setup");
            app.Update(ctx =>
            {
                var state = ctx.Res<PrintMessageState>();
                state.Elapsed += ctx.Time.Delta;
                if (state.Elapsed < state.Wait) return;

                state.Elapsed -= state.Wait;
                Console.WriteLine(state.Message);
            }, "plugin.PrintMessage");
        }
    }

    private sealed class PrintMessageState(string message, float wait)
    {
        public string Message { get; } = message;
        public float Wait { get; } = wait;
        public float Elapsed { get; set; }
    }
}
