using Bevy;

namespace BevyCSharp.Examples.Application;

// Demonstrates a plugin group of a game's own, two plugins added as one, printing "hello" and
// "world" every frame.
internal static class PluginGroupExample
{
    public static void Build(App app) => app.AddPlugins(new HelloWorldPlugins());

    private sealed class HelloWorldPlugins : IPluginGroup
    {
        // In the order they are built, as Bevy's PluginGroupBuilder adds them.
        public IEnumerable<(IPlugin, int)> GetPlugins() => [(new PrintHelloPlugin(), 0), (new PrintWorldPlugin(), 1)];
    }

    private sealed class PrintHelloPlugin : IPlugin
    {
        public void Build(App app) => app.Update(_ => Console.WriteLine("hello"), "plugin_group.PrintHello");
    }

    private sealed class PrintWorldPlugin : IPlugin
    {
        public void Build(App app) => app.Update(_ => Console.WriteLine("world"), "plugin_group.PrintWorld");
    }
}
