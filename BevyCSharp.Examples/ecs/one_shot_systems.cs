// Bevy's one_shot_systems example, examples/ecs/one_shot_systems.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Ecs;

// Shows how to run systems without scheduling them, two kept on entities and run when a key marks
// their entity, A for one and B for the other. Bevy registers each and keeps its id, and here the
// component keeps the system's place in a list.
internal static class OneShotSystems
{
    // The systems the callbacks name, Bevy's registered systems, and the span they write.
    internal static readonly List<Action<EcsWorld>> Systems = [];
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
            _last = Ui.SpawnTextSpan(text, "-", style, Color.FromSrgb8(255, 165, 0));

            Systems.Add(SystemA);
            var a = ecs.Spawn();
            ecs.Add(a, new OneShotCallback { System = Systems.Count - 1 });
            ecs.Add(a, new A());

            // Bevy's runs system_b once straight away, before keeping it, as a world can.
            SystemB(ecs);
            Systems.Add(SystemB);
            var b = ecs.Spawn();
            ecs.Add(b, new OneShotCallback { System = Systems.Count - 1 });
            ecs.Add(b, new B());
        }, "one_shot_systems.Setup");

        // First of the chain, the mark added at once so the callbacks find it this frame, as
        // Bevy's chain applies the commands between the two.
        app.AddSystem(Stage.Update, new SystemDescriptor(world => TriggerSystem(new BehaviorContext(world)), "one_shot_systems.TriggerSystem")
            .Before("OneShotCallback.EvaluateCallbacks"));
    }

    // A marks the entity carrying A as triggered, and B the one carrying B.
    private static void TriggerSystem(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (ctx.Input.KeyPressed(Key.A)) foreach (var entity in ecs.EntitiesWith<A>()) ecs.Add(entity, new Triggered());
        if (ctx.Input.KeyPressed(Key.B)) foreach (var entity in ecs.EntitiesWith<B>()) ecs.Add(entity, new Triggered());
    }

    private static void SystemA(EcsWorld ecs)
    {
        ecs.Wrap<TextSpanRef>(_last).Value = "A";
        Console.WriteLine("A: One shot system registered with Commands was triggered");
    }

    private static void SystemB(EcsWorld ecs)
    {
        ecs.Wrap<TextSpanRef>(_last).Value = "B";
        Console.WriteLine("B: One shot system registered with World was triggered");
    }
}

/// <summary>
/// A system kept on an entity, by its place among the example's systems, as Bevy keeps its id,
/// Bevy's <c>Callback</c> under another name since callbacks' shares the namespace.
/// </summary>
[Behavior]
public partial struct OneShotCallback
{
    /// <summary>The system's place.</summary>
    public int System;

    /// <summary>The system run, once, when its entity is marked as triggered, and the mark taken off.</summary>
    [OnUpdate]
    [With(typeof(Triggered))]
    public void EvaluateCallbacks(BehaviorContext ctx)
    {
        ctx.Cmd.Run(OneShotSystems.Systems[System]);
        ctx.Cmd.Remove<Triggered>(ctx.Entity);
    }
}

/// <summary>An entity whose system runs this frame.</summary>
[Behavior]
public partial struct Triggered;

/// <summary>The entity A triggers.</summary>
[Behavior]
public partial struct A;

/// <summary>The entity B triggers.</summary>
[Behavior]
public partial struct B;
