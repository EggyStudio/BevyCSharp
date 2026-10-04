using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.States;

// Illustrates states, a menu with a Play button that leaves it for the game, where a logo moved by
// the arrow keys shifts its color over time, each state's systems running only while it holds.
internal static class StatesExample
{
    internal enum AppState { Menu, InGame }

    private const float Speed = 100f;

    private static readonly Color Normal = Color.FromSrgb(0.15f, 0.15f, 0.15f);
    private static readonly Color Hovered = Color.FromSrgb(0.25f, 0.25f, 0.25f);
    private static readonly Color Pressed = Color.FromSrgb(0.35f, 0.75f, 0.35f);

    private static Entity _menu, _button, _logo = Entity.None;
    private static UiInteraction _last;

    public static void Build(App app)
    {
        app.AddState(AppState.Menu);
        app.Startup(_ => Render2d.SpawnCamera2d(), "states.Setup");

        app.AddStateSystem(AppState.Menu, entering: true, new SystemDescriptor(world => SetupMenu(new BehaviorContext(world)), "states.SetupMenu"));
        app.On(Stage.Update, Menu, "states.Menu", BehaviorConditions.InState(AppState.Menu));
        app.AddStateSystem(AppState.Menu, entering: false, new SystemDescriptor(world => CleanupMenu(new BehaviorContext(world)), "states.CleanupMenu"));

        app.AddStateSystem(AppState.InGame, entering: true, new SystemDescriptor(world => SetupGame(new BehaviorContext(world)), "states.SetupGame"));
        app.On(Stage.Update, Movement, "states.Movement", BehaviorConditions.InState(AppState.InGame));
        app.On(Stage.Update, ChangeColor, "states.ChangeColor", BehaviorConditions.InState(AppState.InGame));
    }

    internal static void SetupMenu(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _last = UiInteraction.None;
        _menu = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Justify = UiJustify.Center, Align = UiAlign.Center });
        _button = Ui.SpawnNode(new UiSettings
        {
            Interactive = true,
            Width = Length.Px(150f),
            Height = Length.Px(65f),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Color = (Normal.R, Normal.G, Normal.B, 1f),
        });
        ecs.SetParent(_button, _menu);

        var light = Color.FromSrgb(0.9f, 0.9f, 0.9f);
        ecs.SetParent(Ui.SpawnText("Play", new UiSettings { Color = (light.R, light.G, light.B, 1f) }, 33f), _button);
    }

    internal static void CleanupMenu(BehaviorContext ctx) => ctx.Ecs.Despawn(_menu);

    internal static void SetupGame(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _logo = ecs.Spawn();
        ecs.Add(_logo, Transform.Identity);
        Render2d.SetSprite(ecs, _logo, AssetServer.Load(AssetKind.Image, "branding/icon.png"));
    }

    // The button's color follows the pointer, and a press starts the game, each when it changes,
    // as Bevy's query of Changed<Interaction> sees it.
    internal static void Menu(BehaviorContext ctx)
    {
        var interaction = Ui.InteractionOf(_button);
        if (interaction == _last) return;
        _last = interaction;

        var color = interaction switch { UiInteraction.Pressed => Pressed, UiInteraction.Hovered => Hovered, _ => Normal };
        ctx.Ecs.Wrap<BackgroundColorRef>(_button).Value = color;
        if (interaction == UiInteraction.Pressed) ctx.SetState(AppState.InGame);
    }

    internal static void Movement(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var direction = new Vec3(
            (input.KeyDown(Key.ArrowRight) ? 1f : 0f) - (input.KeyDown(Key.ArrowLeft) ? 1f : 0f),
            (input.KeyDown(Key.ArrowUp) ? 1f : 0f) - (input.KeyDown(Key.ArrowDown) ? 1f : 0f),
            0f);
        if (direction == Vec3.Zero) return;

        var transform = ctx.Ecs.GetOrDefault<Transform>(_logo);
        transform.Translation += direction.Normalized * Speed * ctx.Time.Delta;
        ctx.Ecs.Set(_logo, transform);
    }

    // Blue past one, rising and falling slowly, the rest of the color kept.
    internal static void ChangeColor(BehaviorContext ctx)
    {
        if (ctx.Ecs.Get<SpriteRef>(_logo) is not { } sprite) return;
        sprite.Color = sprite.Color with { B = MathF.Sin(ctx.Time.Elapsed * 0.5f) + 2f };
    }
}
