using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Demonstrates a startup system, one that runs once when the app starts, before every system that
// runs each frame.
internal static class StartupSystem
{
    public static void Build(App app)
    {
        app.Startup(_ => Console.WriteLine("startup system ran first"));
        app.Update(_ => Console.WriteLine("normal system ran second"));
    }
}
