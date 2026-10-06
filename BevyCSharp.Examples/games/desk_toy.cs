// Bevy's desk_toy example, examples/showcase/desk_toy.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Games;

// The Bevy logo as a desk toy on a transparent window, with googly eyes. The logo is dragged with
// the left button and its pupils swing behind it, Space takes the window's border and background
// away so the logo sits on the desktop and the pointer passes through everywhere else, and a right
// click on the logo quits.
internal static class DeskToy
{
    internal const float BevyLogoRadius = 128f;

    // Where each bird's eye is on branding/icon.png, from the middle, and its radius, measured by
    // hand from the picture.
    private static readonly (float X, float Y, float Radius)[] BirdsEyes =
    [
        (145f - 128f, -(56f - 128f), 12f),
        (198f - 128f, -(87f - 128f), 10f),
        (222f - 128f, -(140f - 128f), 8f),
    ];

    internal static readonly Color WindowClearColor = Color.FromSrgb(0.2f, 0.2f, 0.2f);

    // Bevy's resources, where the pointer is in the world, where on the logo a drag took hold,
    // and whether the window is see-through.
    internal static Vec2? CursorWorldPos, DragOperation;
    internal static bool WindowTransparency;

    private static Entity _camera;

    public static void Configure(Config config)
    {
        config.Title = "Bevy Desk Toy";
        config.Transparent = true;
    }

    public static void Build(App app)
    {
        app.Startup(Setup, "desk_toy.Setup");

        // First of the chain, before the logo's own systems read where the pointer is.
        app.AddSystem(Stage.Update, new SystemDescriptor(world => GetCursorWorldPos(new BehaviorContext(world)), "desk_toy.GetCursorWorldPos")
            .Before("BevyLogo.UpdateCursorHitTest"));
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        (CursorWorldPos, DragOperation, WindowTransparency) = (null, null, false);
        Render.SetClearColor(WindowClearColor);
        _camera = Render2d.SpawnCamera2d();

        var instructions = ecs.Spawn();
        ecs.Add(instructions, Transform.At(0f, -300f, 100f));
        ecs.Insert<Text2dRef>(instructions).Value = "Press Space to play on your desktop! Press it again to return.\nRight click Bevy logo to exit.";
        var font = ecs.Insert<TextFontRef>(instructions);
        font.Font = new FontSource.Handle(AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf"));
        font.FontSize = new FontSize.Px(25f);
        ecs.Add(instructions, new InstructionsText());

        // One circle of radius one, scaled to each part of each eye.
        var circle = Render.CreateMesh(MeshShape.Circle, 1f);
        var outline = Render2d.CreateMaterial(new ColorMaterialSettings { Color = (0f, 0f, 0f, 1f) });
        var sclera = Render2d.CreateMaterial(new ColorMaterialSettings { Color = (1f, 1f, 1f, 1f) });
        var pupil = Render2d.CreateMaterial(new ColorMaterialSettings { Color = Color.FromSrgb(0.2f, 0.2f, 0.2f) });
        var highlight = Render2d.CreateMaterial(new ColorMaterialSettings { Color = Color.FromSrgb(1f, 1f, 1f, 0.2f) });

        var logo = ecs.Spawn();
        ecs.Add(logo, Transform.Identity);
        Render2d.SetSprite(ecs, logo, AssetServer.Load(AssetKind.Image, "branding/icon.png"));
        ecs.Add(logo, new BevyLogo());

        Entity Part(Entity parent, Transform at, AssetHandle? material = null)
        {
            var part = ecs.Spawn();
            ecs.Add(part, at);
            ecs.Insert<VisibilityRef>(part);
            if (material is { } drawn)
            {
                Render2d.SetMesh(ecs, part, circle);
                Render2d.SetMaterial(ecs, part, drawn);
            }

            ecs.SetParent(part, parent);
            return part;
        }

        foreach (var (x, y, radius) in BirdsEyes)
        {
            var (pupilRadius, highlightRadius, highlightOffset) = (radius * 0.6f, radius * 0.3f, radius * 0.3f);
            Part(logo, new Transform(new Vec3(x, y - 1f, 1f), Quat.Identity, new Vec3(radius + 2f, radius + 2f, 1f)), outline);

            var eye = Part(logo, Transform.At(x, y, 2f));
            Part(eye, new Transform(Vec3.Zero, Quat.Identity, new Vec3(radius, radius, 0f)), sclera);

            var moving = Part(eye, Transform.At(0f, 0f, 1f));
            ecs.Add(moving, new Pupil { EyeRadius = radius, PupilRadius = pupilRadius });
            Part(moving, new Transform(Vec3.Zero, Quat.Identity, new Vec3(pupilRadius, pupilRadius, 1f)), pupil);
            Part(moving, new Transform(new Vec3(-highlightOffset, highlightOffset, 1f), Quat.Identity, new Vec3(highlightRadius, highlightRadius, 1f)), highlight);
        }
    }

    private static void GetCursorWorldPos(BehaviorContext ctx)
    {
        var (x, y) = ctx.Input.MousePosition;
        CursorWorldPos = Render.TryRay(_camera, x, y, out var world, out _) ? new Vec2(world.X, world.Y) : null;
    }

    // Whether the pointer is on a logo standing where the transform says.
    internal static bool Over(in Transform logo) =>
        CursorWorldPos is { } cursor && (new Vec2(logo.Translation.X, logo.Translation.Y) - cursor).Length < BevyLogoRadius;
}

/// <summary>The Bevy logo, which a drag moves about and a right click on quits.</summary>
[Behavior]
public partial struct BevyLogo
{
    /// <summary>
    /// A window with its border takes every click, and one without takes only those on the logo,
    /// letting the rest through to whatever is behind it. An offscreen run has no window to change.
    /// </summary>
    [OnUpdate]
    public void UpdateCursorHitTest(BehaviorContext ctx, in Transform transform)
    {
        var window = Window.Entity();
        if (window == Entity.None) return;
        var cursor = ctx.Ecs.Wrap<CursorOptionsRef>(window);
        if (ctx.Ecs.Wrap<WindowRef>(window).Decorations)
        {
            cursor.HitTest = true;
            return;
        }

        if (DeskToy.CursorWorldPos is not null) cursor.HitTest = DeskToy.Over(transform);
    }

    /// <summary>A drag started by a press on the logo, keeping where on it the pointer took hold.</summary>
    [OnUpdate]
    [After("BevyLogo.UpdateCursorHitTest")]
    public void StartDrag(BehaviorContext ctx, in Transform transform)
    {
        if (!ctx.Input.MousePressed(MouseButton.Left) || DeskToy.CursorWorldPos is not { } cursor) return;
        var offset = new Vec2(transform.Translation.X, transform.Translation.Y) - cursor;
        if (offset.Length < DeskToy.BevyLogoRadius) DeskToy.DragOperation = offset;
    }

    /// <summary>The drag let go with the button.</summary>
    [OnUpdate]
    [After("BevyLogo.UpdateCursorHitTest")]
    public void EndDrag(BehaviorContext ctx)
    {
        if (ctx.Input.MouseReleased(MouseButton.Left)) DeskToy.DragOperation = null;
    }

    /// <summary>
    /// The logo following the pointer while it is dragged, and each pupil pushed the other way by
    /// how fast it went, since a pupil moves within its eye and would otherwise be carried along
    /// fixed.
    /// </summary>
    [OnUpdate]
    [After("BevyLogo.UpdateCursorHitTest")]
    public void Drag(BehaviorContext ctx, ref Transform transform)
    {
        if (DeskToy.CursorWorldPos is not { } cursor || DeskToy.DragOperation is not { } offset || ctx.Time.Delta <= 0f) return;

        var moved = cursor + offset;
        var velocity = (moved - new Vec2(transform.Translation.X, transform.Translation.Y)) * (1f / ctx.Time.Delta);
        transform.Translation = new Vec3(moved.X, moved.Y, transform.Translation.Z);
        foreach (var pupil in ctx.Ecs.Query<Pupil>()) pupil.Component.Velocity -= velocity;
    }

    /// <summary>A right click on the logo quits.</summary>
    [OnUpdate]
    [After("BevyLogo.UpdateCursorHitTest")]
    public void Quit(BehaviorContext ctx, in Transform transform)
    {
        if (ctx.Input.MousePressed(MouseButton.Right) && DeskToy.Over(transform)) ctx.Exit();
    }
}

/// <summary>The instructions, hidden while the logo sits on the desktop.</summary>
[Behavior]
public partial struct InstructionsText
{
    /// <summary>
    /// Space takes the window's border and background away and keeps it above the rest, with the
    /// instructions hidden, or gives them all back.
    /// </summary>
    [OnUpdate]
    [After("BevyLogo.UpdateCursorHitTest")]
    public void ToggleTransparency(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space)) return;

        DeskToy.WindowTransparency = !DeskToy.WindowTransparency;
        ctx.Ecs.Wrap<VisibilityRef>(ctx.Entity).Value = DeskToy.WindowTransparency ? VisibilityRef.ValueVariant.Hidden : VisibilityRef.ValueVariant.Visible;
        if (Window.Entity() != Entity.None) Window.SetStyle(decorations: !DeskToy.WindowTransparency, alwaysOnTop: DeskToy.WindowTransparency);
        Render.SetClearColor(DeskToy.WindowTransparency ? (0f, 0f, 0f, 0f) : DeskToy.WindowClearColor);
    }
}

/// <summary>The moving part of a googly eye.</summary>
[Behavior]
public partial struct Pupil
{
    /// <summary>The radius of the eye it moves in.</summary>
    public float EyeRadius;

    /// <summary>Its own radius.</summary>
    public float PupilRadius;

    /// <summary>How fast it moves within its eye.</summary>
    public Vec2 Velocity;

    /// <summary>
    /// Slowed, moved, and bounced back off the edge of its eye with three quarters of its speed,
    /// which is not how anything moves but looks right for a googly eye, after the logo's drag.
    /// </summary>
    [OnUpdate]
    [After("BevyLogo.Drag")]
    public void MovePupils(BehaviorContext ctx, ref Transform transform)
    {
        var delta = ctx.Time.Delta;
        var wiggleRadius = EyeRadius - PupilRadius;
        var translation = new Vec2(transform.Translation.X, transform.Translation.Y);
        Velocity *= MathF.Pow(0.04f, delta);
        translation += Velocity * delta;
        if (translation.Length > wiggleRadius)
        {
            translation *= wiggleRadius / translation.Length;
            Velocity *= -0.75f;
        }

        transform.Translation = new Vec3(translation.X, translation.Y, transform.Translation.Z);
    }
}
