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
    // The moving part of a googly eye, the radius of the eye it moves in and its own.
    internal struct Pupil
    {
        public float EyeRadius;
        public float PupilRadius;
        public Vec2 Velocity;
    }

    private const float BevyLogoRadius = 128f;

    // Where each bird's eye is on branding/icon.png, from the middle, and its radius, measured by
    // hand from the picture.
    private static readonly (float X, float Y, float Radius)[] BirdsEyes =
    [
        (145f - 128f, -(56f - 128f), 12f),
        (198f - 128f, -(87f - 128f), 10f),
        (222f - 128f, -(140f - 128f), 8f),
    ];

    private static readonly (float R, float G, float B, float A) WindowClearColor = Color.FromSrgb(0.2f, 0.2f, 0.2f);

    private static Entity _camera, _logo, _instructions;
    private static Vec2? _cursorWorldPos, _dragOffset;
    private static bool _windowTransparency;

    public static void Configure(Config config)
    {
        config.Title = "Bevy Desk Toy";
        config.Transparent = true;
    }

    public static void Build(App app)
    {
        app.Startup(Setup, "desk_toy.Setup");

        // One system for the lot, since Bevy chains them in this order.
        app.Update(ctx =>
        {
            GetCursorWorldPos(ctx);
            UpdateCursorHitTest(ctx);
            var input = ctx.Input;
            if (input.MousePressed(MouseButton.Left)) StartDrag(ctx);
            if (input.MouseReleased(MouseButton.Left)) _dragOffset = null;
            if (_dragOffset is not null) Drag(ctx);
            if (input.MousePressed(MouseButton.Right) && OverLogo(ctx)) ctx.Exit();
            if (input.KeyPressed(Key.Space)) ToggleTransparency(ctx);
            MovePupils(ctx);
        }, "desk_toy.Update");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        (_cursorWorldPos, _dragOffset, _windowTransparency) = (null, null, false);
        Render.SetClearColor(WindowClearColor);
        _camera = Render2d.SpawnCamera2d();

        _instructions = ecs.Spawn();
        ecs.Add(_instructions, Transform.At(0f, -300f, 100f));
        ecs.Insert<Text2dRef>(_instructions).Value = "Press Space to play on your desktop! Press it again to return.\nRight click Bevy logo to exit.";
        var font = ecs.Insert<TextFontRef>(_instructions);
        font.Font = new FontSource.Handle(AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf"));
        font.FontSize = new FontSize.Px(25f);

        // One circle of radius one, scaled to each part of each eye.
        var circle = Render.CreateMesh(MeshShape.Circle, 1f);
        var outline = Render2d.CreateMaterial(new ColorMaterialSettings { Color = (0f, 0f, 0f, 1f) });
        var sclera = Render2d.CreateMaterial(new ColorMaterialSettings { Color = (1f, 1f, 1f, 1f) });
        var pupil = Render2d.CreateMaterial(new ColorMaterialSettings { Color = Color.FromSrgb(0.2f, 0.2f, 0.2f) });
        var highlight = Render2d.CreateMaterial(new ColorMaterialSettings { Color = Color.FromSrgb(1f, 1f, 1f, 0.2f) });

        _logo = ecs.Spawn();
        ecs.Add(_logo, Transform.Identity);
        Render2d.SetSprite(ecs, _logo, AssetServer.Load(AssetKind.Image, "branding/icon.png"));

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
            Part(_logo, new Transform(new Vec3(x, y - 1f, 1f), Quat.Identity, new Vec3(radius + 2f, radius + 2f, 1f)), outline);

            var eye = Part(_logo, Transform.At(x, y, 2f));
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
        _cursorWorldPos = Render.TryRay(_camera, x, y, out var world, out _) ? new Vec2(world.X, world.Y) : null;
    }

    private static Vec2 LogoAt(BehaviorContext ctx)
    {
        var at = ctx.Ecs.GetOrDefault<Transform>(_logo).Translation;
        return new Vec2(at.X, at.Y);
    }

    private static bool OverLogo(BehaviorContext ctx) => _cursorWorldPos is { } cursor && (LogoAt(ctx) - cursor).Length < BevyLogoRadius;

    // A window with its border takes every click, and one without takes only those on the logo,
    // letting the rest through to whatever is behind it. An offscreen run has no window to change.
    private static void UpdateCursorHitTest(BehaviorContext ctx)
    {
        var window = Window.Entity();
        if (window == Entity.None) return;
        var cursor = ctx.Ecs.Wrap<CursorOptionsRef>(window);
        if (ctx.Ecs.Wrap<WindowRef>(window).Decorations)
        {
            cursor.HitTest = true;
            return;
        }

        if (_cursorWorldPos is not null) cursor.HitTest = OverLogo(ctx);
    }

    // A drag starts on the logo and keeps where on it the pointer took hold.
    private static void StartDrag(BehaviorContext ctx)
    {
        if (_cursorWorldPos is not { } cursor) return;
        var offset = LogoAt(ctx) - cursor;
        if (offset.Length < BevyLogoRadius) _dragOffset = offset;
    }

    // The logo follows the pointer, and each pupil is pushed the other way by how fast it went,
    // since a pupil moves within its eye and would otherwise be carried along fixed.
    private static void Drag(BehaviorContext ctx)
    {
        if (_cursorWorldPos is not { } cursor || _dragOffset is not { } offset || ctx.Time.Delta <= 0f) return;
        var ecs = ctx.Ecs;
        var moved = cursor + offset;
        var velocity = (moved - LogoAt(ctx)) * (1f / ctx.Time.Delta);

        var at = ecs.GetOrDefault<Transform>(_logo);
        ecs.Set(_logo, at with { Translation = new Vec3(moved.X, moved.Y, at.Translation.Z) });
        foreach (var entity in ecs.EntitiesWith<Pupil>())
        {
            var pupil = ecs.GetOrDefault<Pupil>(entity);
            pupil.Velocity -= velocity;
            ecs.Set(entity, pupil);
        }
    }

    // The window loses its border and background and stays above the rest, with the instructions
    // hidden, or is given them all back.
    private static void ToggleTransparency(BehaviorContext ctx)
    {
        _windowTransparency = !_windowTransparency;
        ctx.Ecs.Wrap<VisibilityRef>(_instructions).Value = _windowTransparency ? VisibilityRef.ValueVariant.Hidden : VisibilityRef.ValueVariant.Visible;
        if (Window.Entity() != Entity.None) Window.SetStyle(decorations: !_windowTransparency, alwaysOnTop: _windowTransparency);
        Render.SetClearColor(_windowTransparency ? (0f, 0f, 0f, 0f) : WindowClearColor);
    }

    // Each pupil slows, moves, and bounces back off the edge of its eye with three quarters of its
    // speed, which is not how anything moves but looks right for a googly eye.
    private static void MovePupils(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var delta = ctx.Time.Delta;
        foreach (var entity in ecs.EntitiesWith<Pupil>())
        {
            var (pupil, at) = (ecs.GetOrDefault<Pupil>(entity), ecs.GetOrDefault<Transform>(entity));
            var wiggleRadius = pupil.EyeRadius - pupil.PupilRadius;
            var translation = new Vec2(at.Translation.X, at.Translation.Y);
            pupil.Velocity *= MathF.Pow(0.04f, delta);
            translation += pupil.Velocity * delta;
            if (translation.Length > wiggleRadius)
            {
                translation *= wiggleRadius / translation.Length;
                pupil.Velocity *= -0.75f;
            }

            ecs.Set(entity, pupil);
            ecs.Set(entity, at with { Translation = new Vec3(translation.X, translation.Y, at.Translation.Z) });
        }
    }
}
