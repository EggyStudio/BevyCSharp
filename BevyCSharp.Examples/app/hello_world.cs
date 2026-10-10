// Bevy's hello_world example, examples/hello_world.rs at v0.20.0, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Application;

// The smallest app there is, a system that says hello in the log as it starts.
internal static class HelloWorld
{
    public static void Build(App app) => app.Startup(_ => Log.Info("Hello, World!"), "hello_world.HelloWorldSystem");
}
