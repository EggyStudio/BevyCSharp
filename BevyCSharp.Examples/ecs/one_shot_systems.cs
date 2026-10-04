using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Ecs;

// Shows how to run systems without scheduling them, two kept on entities and run when a key marks
// their entity, A for one and B for the other. Bevy registers each and keeps its id, and here the
// component keeps the system's place in a list.
internal static class OneShotSystems
{

    internal struct Callback
    {
        public int System;
    }

    internal struct Triggered;

    internal struct A;

    internal struct B;

    private static readonly List<Action<BehaviorContext>> Systems = [];
    private static Entity _last;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Systems.Clear();
            Render2d.SpawnCamera2d();

            // Bevy centers the text by its own alignment, and here a node the size of the window
            // centers it.
            var middle = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Justify = UiJustify.Center, Align = UiAlign.Center });
            var text = Ui.SpawnText(string.Empty, new UiSettings(), new UiTextSettings { Justify = TextJustify.Center });
            ecs.SetParent(text, middle);
            var style = new UiTextSettings { Justify = TextJustify.Center };
            Ui.SpawnTextSpan(text, "Press A or B to trigger a one-shot system\n", style, (1f, 1f, 1f, 1f));
            Ui.SpawnTextSpan(text, "Last Triggered: ", style, (1f, 1f, 1f, 1f));
            _last = Ui.SpawnTextSpan(text, "-", style, Scene.Srgb8(255, 165, 0));

            Systems.Add(SystemA);
            var a = ecs.Spawn();
            ecs.Add(a, new Callback { System = Systems.Count - 1 });
            ecs.Add(a, new A());

            // Bevy's runs system_b once straight away, before keeping it, as a world can.
            SystemB(ctx);
            Systems.Add(SystemB);
            var b = ecs.Spawn();
            ecs.Add(b, new Callback { System = Systems.Count - 1 });
            ecs.Add(b, new B());
        }, "one_shot_systems.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            if (ctx.Input.KeyPressed(Key.A)) foreach (var entity in ecs.EntitiesWith<A>()) ecs.Add(entity, new Triggered());
            if (ctx.Input.KeyPressed(Key.B)) foreach (var entity in ecs.EntitiesWith<B>()) ecs.Add(entity, new Triggered());

            foreach (var entity in ecs.EntitiesWith<Triggered>())
            {
                Systems[ecs.GetOrDefault<Callback>(entity).System](ctx);
                ecs.Remove<Triggered>(entity);
            }
        }, "one_shot_systems.TriggerAndEvaluate");
    }

    private static void SystemA(BehaviorContext ctx)
    {
        ctx.Ecs.Wrap<TextSpanRef>(_last).Value = "A";
        Console.WriteLine("A: One shot system registered with Commands was triggered");
    }

    private static void SystemB(BehaviorContext ctx)
    {
        ctx.Ecs.Wrap<TextSpanRef>(_last).Value = "B";
        Console.WriteLine("B: One shot system registered with World was triggered");
    }
}
