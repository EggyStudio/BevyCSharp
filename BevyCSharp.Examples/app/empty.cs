using Bevy;

namespace BevyCSharp.Examples.Application;

// An empty application, which does nothing. Bevy's runs its schedule once with no plugins and
// returns, and this one runs a frame headless and returns, printing nothing.
internal static class Empty
{
    public static void Build(App app)
    {
    }
}
