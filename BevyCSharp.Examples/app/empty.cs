// Bevy's empty example, examples/app/empty.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

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
