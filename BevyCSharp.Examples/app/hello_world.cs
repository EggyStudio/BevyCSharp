using Bevy;

namespace BevyCSharp.Examples.Application;

// The smallest app there is, a system that says hello.
internal static class HelloWorld
{
    public static void Build(App app) => app.Update(_ => Console.WriteLine("hello world"), "hello_world.HelloWorldSystem");
}
