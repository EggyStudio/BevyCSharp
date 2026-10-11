// Bevy's nondeterministic_system_order example, examples/ecs/nondeterministic_system_order.rs at
// v0.20.0, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Shows how to find the systems whose order is left open though their order matters. Bevy picks an
// order for systems that say nothing of it, which may change between builds, so two systems where
// one writes what the other reads give different results by whichever runs first. Asking the
// schedule to report them lists each such pair as the app starts.
//
// C# systems say what they read and write with Read and Write, which the report is made from, and
// the report looks among this example's own systems, since this app's Update holds every example's
// behaviors, some of which conflict.
internal static class NondeterministicSystemOrder
{
    internal sealed class A
    {
        public int Value;
    }

    internal sealed class B
    {
        public int Value;
    }

    public static void Build(App app)
    {
        // Each pair whose order is left open said in the log as the app starts.
        app.DetectAmbiguities(Stage.Update, among: name => name.StartsWith("NondeterministicSystemOrder.", StringComparison.Ordinal));
        app.Startup(ctx =>
        {
            ctx.World.InitResource<A>();
            ctx.World.InitResource<B>();
        }, "nondeterministic_system_order.Setup");

        // Some of these have their order stated, and some do not, so the report names the ones that
        // do not and conflict. reads_a_and_b says its order against adds_one_to_b does not matter.
        app.AddSystem(Stage.Update, new SystemDescriptor(ReadsA).Read<A>());
        app.AddSystem(Stage.Update, new SystemDescriptor(WritesA).Write<A>());
        app.AddSystem(Stage.Update, new SystemDescriptor(AddsOneToB).Write<B>());
        app.AddSystem(Stage.Update, new SystemDescriptor(DoublesB).Write<B>().After("NondeterministicSystemOrder.AddsOneToB"));
        app.AddSystem(Stage.Update, new SystemDescriptor(ReadsB).Read<B>().After("NondeterministicSystemOrder.DoublesB"));
        app.AddSystem(Stage.Update, new SystemDescriptor(ReadsAAndB).Read<A>().Read<B>().AmbiguousWith("NondeterministicSystemOrder.AddsOneToB"));
    }

    private static void ReadsA(World world) => _ = world.Resource<A>().Value;

    private static void WritesA(World world) => world.Resource<A>().Value += 1;

    // The two steps of B saturate rather than wrap, as Bevy's do.
    private static void AddsOneToB(World world)
    {
        var b = world.Resource<B>();
        b.Value = b.Value == int.MaxValue ? int.MaxValue : b.Value + 1;
    }

    private static void DoublesB(World world)
    {
        var b = world.Resource<B>();
        b.Value = b.Value > int.MaxValue / 2 ? int.MaxValue : b.Value * 2;
    }

    // B is always even here, since it is doubled after one is added and read after it is doubled.
    private static void ReadsB(World world)
    {
        var b = world.Resource<B>().Value;
        if (b % 2 != 0 && b != int.MaxValue) throw new InvalidOperationException($"B is {b}, which the order stated keeps even.");
    }

    // What this logs changes with whether A was written yet this frame, which the order of these
    // systems leaves open.
    private static void ReadsAAndB(World world)
    {
        var (a, b) = (world.Resource<A>().Value, world.Resource<B>().Value);
        if (b < 10) Log.Info($"{a}, {b}");
    }
}
