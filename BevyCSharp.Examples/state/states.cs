// Bevy's states example, examples/state/states.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.States;

// Illustrates states, a menu with a Play button that leaves it for the game, where a logo moved by
// the arrow keys shifts its color over time, each state's systems running only while it holds. The
// button is Bevy's widget, which starts the game by its activation as it is pressed.
internal static class StatesExample
{
    internal enum AppState { Menu, InGame }

    private const float Speed = 100f;

    internal static readonly Color Normal = Color.FromSrgb(0.15f, 0.15f, 0.15f);
    internal static readonly Color Hovered = Color.FromSrgb(0.25f, 0.25f, 0.25f);
    internal static readonly Color Pressed = Color.FromSrgb(0.35f, 0.75f, 0.35f);

    private static Entity _menu, _logo = Entity.None;

    // The logo, for an example built on this one that takes it down again.
    internal static Entity Logo => _logo;

    public static void Build(App app)
    {
        app.AddState(AppState.Menu);
        app.Startup(ctx =>
        {
            Render2d.SpawnCamera2d();
            ObserveActivate(ctx.Ecs);
        }, "states.Setup");

        app.AddStateSystem(AppState.Menu, entering: true, new SystemDescriptor(world => SetupMenu(new BehaviorContext(world)), "states.SetupMenu"));
        app.AddStateSystem(AppState.Menu, entering: false, new SystemDescriptor(world => CleanupMenu(new BehaviorContext(world)), "states.CleanupMenu"));

        app.AddStateSystem(AppState.InGame, entering: true, new SystemDescriptor(world => SetupGame(new BehaviorContext(world)), "states.SetupGame"));
        app.On(Stage.Update, Movement, "states.Movement", BehaviorConditions.InState(AppState.InGame));
        app.On(Stage.Update, ChangeColor, "states.ChangeColor", BehaviorConditions.InState(AppState.InGame));
    }

    internal static void SetupMenu(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _menu = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Justify = UiJustify.Center, Align = UiAlign.Center });
        var button = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(150f),
            Height = Length.Px(65f),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Color = (Normal.R, Normal.G, Normal.B, 1f),
        });
        ecs.Insert<ButtonRef>(button);
        ecs.Insert<ActivateOnPressRef>(button);
        ecs.Insert<HoveredRef>(button);
        ecs.Add(button, new HoverStyled());
        ecs.SetParent(button, _menu);

        var light = Color.FromSrgb(0.9f, 0.9f, 0.9f);
        ecs.SetParent(Ui.SpawnText("Play", new UiSettings { Color = (light.R, light.G, light.B, 1f) }, 33f), button);
    }

    // Bevy's on_activate_start_game, a button activated in the menu starting the game.
    internal static void ObserveActivate(EcsWorld ecs) => ecs.Observe<Activate>(on =>
    {
        if (StateRegistry.Current<AppState>() == AppState.Menu) on.Context.SetState(AppState.InGame);
    });

    internal static void CleanupMenu(BehaviorContext ctx) => ctx.Ecs.Despawn(_menu);

    internal static void SetupGame(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _logo = ecs.Spawn();
        ecs.Add(_logo, Transform.Identity);
        Render2d.SetSprite(ecs, _logo, AssetServer.Load(AssetKind.Image, "branding/icon.png"));
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

/// <summary>
/// A button lighter while the pointer is over it, as Bevy's <c>hover_style</c> colors one as its
/// <c>Hovered</c> changes.
/// </summary>
[Behavior]
public partial struct HoverStyled
{
    /// <summary>Whether it was hovered when last colored.</summary>
    public bool Hovered;

    /// <summary>Colored again where its hover has changed.</summary>
    [OnUpdate]
    public void HoverStyle(BehaviorContext ctx)
    {
        var hovered = ctx.Ecs.Get<HoveredRef>(ctx.Entity)?.Value == true;
        if (hovered == Hovered) return;

        Hovered = hovered;
        ctx.Ecs.Wrap<BackgroundColorRef>(ctx.Entity).Value = hovered ? StatesExample.Hovered : StatesExample.Normal;
    }
}
