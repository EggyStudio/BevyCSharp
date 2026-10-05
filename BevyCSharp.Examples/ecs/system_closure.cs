// Bevy's system_closure example, examples/ecs/system_closure.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Shows how to use closures as systems, and how one keeps state between frames by capturing it,
// as Bevy's Local does.
internal static class SystemClosure
{
    public static void Build(App app)
    {
        Action<BehaviorContext> simpleClosure = _ => Console.WriteLine("Hello from a simple closure!");

        // A closure that makes a closure, the inner one keeping the string it was given and
        // changing it each time it runs.
        static Action<BehaviorContext> ComplexClosure(string value) => _ =>
        {
            Console.WriteLine($"Hello from a complex closure! {value}");
            value = $"{value} - updated";
        };

        var outsideVariable = "bar";

        app.Update(simpleClosure, "system_closure.Simple");
        app.Update(ComplexClosure("foo"), "system_closure.Complex");
        app.Update(_ => Console.WriteLine("Hello from an inlined closure!"), "system_closure.Inlined");
        app.Update(_ => Console.WriteLine($"Hello from an inlined closure that captured the 'outside_variable'! {outsideVariable}"), "system_closure.Captured");
    }
}
