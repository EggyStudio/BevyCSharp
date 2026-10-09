// Bevy's render_ui_to_texture example, examples/ui/render_ui_to_texture.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// An interface drawn into a texture, which a turning cube wears, and a pointer of the game's own
// moved over it to where the mouse meets the cube, so the blue box on it lights under the mouse and
// is dragged across the texture as though it were a window.
internal static class RenderUiToTexture
{
    internal const float Rate = 0.1f;
    private const uint Size = 512;

    private static Entity _camera;
    private static AssetHandle _image;
    private static PointerId _pointer;
    private static Vec2 _cursorLast;

    public static void Build(App app)
    {
        app.Startup(Setup, "render_ui_to_texture.Setup");
        app.Update(DriveDiegeticPointer, "render_ui_to_texture.DriveDiegeticPointer");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _cursorLast = Vec2.Zero;
        _image = Render.CreateTarget(Size, Size);

        // Bevy's default directional light, of ten thousand lux, shining down its own -Z.
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
        ecs.Add(light, Transform.Identity);

        // The camera the interface is drawn by, into the texture, before the scene's.
        var textureCamera = Render2d.SpawnCamera2d(order: -1);
        Render.SetCameraTarget(textureCamera, _image);

        var root = Ui.SpawnNode(new UiSettings
        {
            Camera = textureCamera,
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Direction = UiDirection.Column,
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Color = Color.FromSrgb8(128, 128, 128),
        });

        // The box to drag, blue and red under the pointer, centered on the pointer across and fifty
        // pixels above it as it is dragged.
        var box = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Align = UiAlign.Center,
            Padding = Sides.All(Length.Px(20f)),
            Corners = Corners.All(Length.Px(10f)),
            Color = Color.FromSrgb8(0, 0, 255),
        });
        ecs.SetParent(box, root);
        ecs.SetParent(Ui.SpawnText("Drag Me!", new UiSettings { Color = (1f, 1f, 1f, 1f) }, 40f), box);

        ecs.Observe<Pointer<Drag>>(box, on =>
        {
            var size = on.Ecs.Wrap<ComputedNodeRef>(on.Entity).Size;
            var node = on.Ecs.Wrap<NodeRef>(on.Entity);
            (node.Left, node.Top) = (new Val.Px(on.Event.Position.X - size.X / 2f), new Val.Px(on.Event.Position.Y - 50f));
        });
        ecs.Observe<Pointer<Over>>(box, on => on.Ecs.Wrap<BackgroundColorRef>(on.Entity).Value = Color.FromSrgb8(255, 0, 0));
        ecs.Observe<Pointer<Out>>(box, on => on.Ecs.Wrap<BackgroundColorRef>(on.Entity).Value = Color.FromSrgb8(0, 0, 255));

        // The cube that wears the texture, turned half over so the texture reads the right way up.
        var material = Render.CreateMaterial(new MaterialSettings { BaseColorTexture = _image, Reflectance = 0.02f });
        var cube = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), material, new Transform(new Vec3(0f, 0f, 1.5f), Quat.FromRotationX(MathF.PI), Vec3.One));
        ecs.Add(cube, new TextureCube());

        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 5f), Vec3.Zero, Vec3.UnitY));

        // Bevy's CUBE_POINTER_ID, the pointer moved over the texture.
        _pointer = Picking.SpawnPointer();
    }

    // The pointer put where a ray from the mouse meets the cube, at that point of the texture, and
    // pressed and released as the mouse's buttons are, where it was last put.
    private static void DriveDiegeticPointer(BehaviorContext ctx)
    {
        var (ecs, input) = (ctx.Ecs, ctx.Input);
        var (x, y) = input.MousePosition;

        if (Render.TryRay(_camera, x, y, out var origin, out var direction)
            && Picking.TryCast(origin, direction, out var hit, out _, out _, out var uv)
            && ecs.Has<TextureCube>(hit)
            && uv is { } at)
        {
            var position = new Vec2(Size * at.X, Size * at.Y);
            if (position != _cursorLast)
            {
                Picking.MovePointer(_pointer, _image, position);
                _cursorLast = position;
            }
        }

        foreach (var (button, pointer) in new[] { (MouseButton.Left, PointerButton.Primary), (MouseButton.Right, PointerButton.Secondary), (MouseButton.Middle, PointerButton.Middle) })
        {
            if (input.MousePressed(button)) Picking.PressPointer(_pointer, _image, _cursorLast, pointer);
            if (input.MouseReleased(button)) Picking.ReleasePointer(_pointer, _image, _cursorLast, pointer);
        }
    }
}

/// <summary>
/// The cube wearing the interface's texture, Bevy's <c>Cube</c> under another name since
/// bevymark_3d's has it.
/// </summary>
[Behavior]
public partial struct TextureCube
{
    /// <summary>
    /// Turned a tenth of a radian a second about X and seven tenths of that about Y, as Bevy's
    /// rotator_system turns it.
    /// </summary>
    [OnUpdate]
    public void Rotator(BehaviorContext ctx, ref Transform transform)
    {
        var step = ctx.Time.Delta * RenderUiToTexture.Rate;
        transform.Rotation = Quat.FromRotationY(0.7f * step) * Quat.FromRotationX(step) * transform.Rotation;
    }
}
