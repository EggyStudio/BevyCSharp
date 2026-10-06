// Bevy's cooldown example, examples/usage/cooldown.rs at v0.19.1, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Usage;

// Four foods to click and eat, each then out of reach for its own cooldown, the time left shown by
// a pale cover over its button that shrinks away. A click on one still cooling down says how long
// is left.
internal static class Cooldown
{
    private sealed class FoodItem(string name, float cooldown, uint index)
    {
        public string Name { get; } = name;
        public float Seconds { get; } = cooldown;
        public uint Index { get; } = index;
        public Entity Button { get; set; }
        public Entity Cover { get; set; }
        public float Elapsed { get; set; }
        public bool Active { get; set; }
        public UiInteraction Last { get; set; }
    }

    private static readonly FoodItem[] Foods =
    [
        new("an apple", 2f, 2),
        new("a burger", 1f, 23),
        new("chocolate", 10f, 32),
        new("cherries", 4f, 41),
    ];

    private static Entity _text;

    public static void Build(App app)
    {
        app.Startup(Setup, "cooldown.Setup");
        app.Update(ActivateAbility, "cooldown.ActivateAbility");
        app.Update(AnimateCooldowns, "cooldown.AnimateCooldowns");
    }

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
        // cools down.
        var slate400 = Color.FromSrgb8(148, 163, 184);
        var slate50 = Color.FromSrgb8(248, 250, 252) with { A = 0.5f };
        foreach (var food in Foods)
        {
            (food.Elapsed, food.Active, food.Last) = (0f, false, UiInteraction.None);
            food.Button = Ui.SpawnNode(new UiSettings
            {
                Interactive = true,
                Width = Length.Px(80f),
                Height = Length.Px(80f),
                Direction = UiDirection.ColumnReverse,
                Color = slate400,
            });
            Ui.SetImage(food.Button, new UiImageSettings { Image = texture, Atlas = layout, Frame = food.Index });
            ecs.SetParent(food.Button, row);

            food.Cover = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(0f), Color = slate50 });
            ecs.SetParent(food.Cover, food.Button);
        }

        _text = Ui.SpawnText("*Click some food to eat it*", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    // A press on a food eats it and starts its cooldown, or says how long is left on it, once for
    // each time the button's interaction changes to pressed, as Bevy's query of a changed
    // Interaction sees it.
    private static void ActivateAbility(BehaviorContext ctx)
    {
        foreach (var food in Foods)
        {
            var interaction = Ui.InteractionOf(food.Button);
            if (interaction == food.Last) continue;
            food.Last = interaction;
            if (interaction != UiInteraction.Pressed) continue;

            if (!food.Active)
            {
                (food.Elapsed, food.Active) = (0f, true);
                Ui.SetText(_text, $"You ate {food.Name}");
            }
            else
            {
                Ui.SetText(_text, $"You can eat {food.Name} again in {MathF.Ceiling(food.Seconds - food.Elapsed)} seconds.");
            }
        }
    }

    // The cover is as tall as the share of the cooldown still to go, and gone when it is over.
    private static void AnimateCooldowns(BehaviorContext ctx)
    {
        foreach (var food in Foods)
        {
            if (!food.Active) continue;
            food.Elapsed = MathF.Min(food.Elapsed + ctx.Time.Delta, food.Seconds);
            var node = ctx.Ecs.Wrap<NodeRef>(food.Cover);
            if (food.Elapsed >= food.Seconds)
            {
                food.Active = false;
                node.Height = new Val.Percent(0f);
            }
            else
            {
                node.Height = new Val.Percent((1f - food.Elapsed / food.Seconds) * 100f);
            }
        }
    }
}
