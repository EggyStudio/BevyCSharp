// Bevy's cooldown example, examples/usage/cooldown.rs at v0.19.1, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Usage;

// Four foods to click and eat, each then out of reach for its own cooldown, the time left shown by
// a pale cover over its button that shrinks away. A click on one still cooling down says how long
// is left.
internal static class CooldownExample
{
    // The foods, by name, how long each cools down for and its picture in the sheet.
    private static readonly (string Name, float Cooldown, uint Index)[] Foods =
    [
        ("an apple", 2f, 2),
        ("a burger", 1f, 23),
        ("chocolate", 10f, 32),
        ("cherries", 4f, 41),
    ];

    // The text that says what happened, which Bevy finds as the only text there is.
    internal static Entity Text;

    public static void Build(App app) => app.Startup(Setup, "cooldown.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        var texture = AssetServer.Load(AssetKind.Image, "textures/food_kenney.png");
        var layout = Render2d.CreateAtlas(64, 64, 7, 7);

        var row = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
            ColumnGap = Length.Px(15f),
        });

        // Tailwind's slate at 400 behind each picture, and its 50 at half alpha over it while it
        // cools down. Each button carries its name and its cooldown, as Bevy's build_ability gives
        // them, and its cover is its child.
        var slate400 = Color.FromSrgb8(148, 163, 184);
        var slate50 = Color.FromSrgb8(248, 250, 252) with { A = 0.5f };
        foreach (var (name, cooldown, index) in Foods)
        {
            var button = Ui.SpawnNode(new UiSettings
            {
                Interactive = true,
                Width = Length.Px(80f),
                Height = Length.Px(80f),
                Direction = UiDirection.ColumnReverse,
                Color = slate400,
            });
            Ui.SetImage(button, new UiImageSettings { Image = texture, Atlas = layout, Frame = index });
            ecs.SetName(button, name);
            ecs.Add(button, new Cooldown { Timer = GameTimer.FromSeconds(cooldown, TimerMode.Once) });
            ecs.SetParent(button, row);

            ecs.SetParent(Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(0f), Color = slate50 }), button);
        }

        Text = Ui.SpawnText("*Click some food to eat it*", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }
}

/// <summary>A food's cooldown, the time before it can be eaten again once it has been.</summary>
[Behavior]
public partial struct Cooldown
{
    /// <summary>The cooldown, run once each time the food is eaten.</summary>
    public GameTimer Timer;

    /// <summary>
    /// A press on a food eats it and starts its cooldown, or says how long is left on it, once for
    /// each time the button's interaction changes, as Bevy's <c>activate_ability</c> does.
    /// </summary>
    [OnUpdate]
    [Changed(typeof(Interaction))]
    public void ActivateAbility(BehaviorContext ctx)
    {
        if (Ui.InteractionOf(ctx.Entity) != UiInteraction.Pressed) return;

        var name = ctx.Ecs.NameOf(ctx.Entity);
        if (!ctx.Ecs.Has<ActiveCooldown>(ctx.Entity))
        {
            Timer.Reset();
            ctx.Cmd.Add(ctx.Entity, new ActiveCooldown());
            Ui.SetText(CooldownExample.Text, $"You ate {name}");
        }
        else
        {
            Ui.SetText(CooldownExample.Text, $"You can eat {name} again in {MathF.Ceiling(Timer.Remaining)} seconds.");
        }
    }

    /// <summary>
    /// The cover as tall as the share of the cooldown still to go, and gone with the cooldown once
    /// it is over, as Bevy's <c>animate_cooldowns</c> does.
    /// </summary>
    [OnUpdate]
    [With(typeof(ActiveCooldown))]
    public void AnimateCooldowns(BehaviorContext ctx)
    {
        var cover = ctx.Ecs.Wrap<NodeRef>(ctx.Ecs.ChildrenOf(ctx.Entity)[0]);
        if (Timer.Tick(ctx.Time.Delta).JustFinished)
        {
            ctx.Cmd.Remove<ActiveCooldown>(ctx.Entity);
            cover.Height = new Val.Percent(0f);
        }
        else
        {
            cover.Height = new Val.Percent((1f - Timer.Fraction) * 100f);
        }
    }
}

/// <summary>A food cooling down, added and taken off far more often than it is read, so sparse, as Bevy's is.</summary>
public struct ActiveCooldown : ISparseComponent;
