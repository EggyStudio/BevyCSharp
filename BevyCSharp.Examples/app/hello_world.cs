// Bevy's hello_world example, examples/hello_world.rs at v0.19.1, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Application;

// The smallest app there is, a system that says hello.
internal static class HelloWorld
{
    public static void Build(App app) => app.Update(_ => Console.WriteLine("hello world"), "hello_world.HelloWorldSystem");
}
