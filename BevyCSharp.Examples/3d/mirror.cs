// Bevy's mirror example, examples/3d/mirror.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates a mirror: a second camera, the main one reflected across the mirror's plane, draws
// the world into an image, and the mirror shows that image at the pixels it covers. Dragging moves
// the camera around the scene or the running fox across the ground, as the buttons choose.
internal static class Mirror
{
    private const string Projection = "bevy_camera::projection::Projection";

    private static readonly Vec3 CameraTarget = new(-25f, 20f, 0f);
    private const float CameraOrbitDistance = 500f;
    private const float CameraPitchSpeed = 0.003f, CameraYawSpeed = 0.004f;
    private const float CameraPitchLimit = MathF.PI / 2f - 0.01f;

    // Turned a quarter turn back about X, so the plane faces down the Z axis, and raised.
    private const float MirrorRotationAngle = -MathF.PI / 2f;
    private static readonly Vec3 MirrorPosition = new(-25f, 75f, 0f);

    private static Entity _camera, _mirrorCamera, _mirror, _fox, _help;
    private static ShaderMaterial _mirrorMaterial;
    private static AssetHandle _mirrorImage;
    private static (uint Width, uint Height) _imageSize;
    private static bool _moveFox, _foxPlaying;
    private static RadioButtons<bool>? _buttons;

    public static void Build(App app)
    {
        (_moveFox, _foxPlaying, _fox) = (false, false, Entity.None);

        app.Startup(Setup, "mirror.Setup");
        app.SpawnGltf("models/animated/Fox.glb", (ctx, root) =>
        {
            ctx.Ecs.Set(root, Transform.At(-50f, 0f, -100f));
            _fox = root;
        });
        app.Update(HandleWindowResize, "mirror.HandleWindowResize");
        app.Update(HandleButtons, "mirror.HandleButtons");
        app.Update(MoveCameraOnMouseDown, "mirror.MoveCamera");
        app.Update(MoveFoxOnMouseDown, "mirror.MoveFox");
        app.Update(PlayFoxAnimation, "mirror.PlayFoxAnimation");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 1f, -2f).Normalized * CameraOrbitDistance, CameraTarget, Vec3.UnitY));

        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 5000f, Shadows = false });
        ecs.Add(sun, Transform.LookingAt(new Vec3(-85f, 16f, -200f), new Vec3(-50f, 0f, 100f), Vec3.UnitY));

        // A green disc of ground, laid flat.
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Circle, 200f), Render.CreateMaterial(Color.FromSrgb8(0, 128, 0)),
            new Transform(new Vec3(-25f, 0f, 0f), Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));

        // The image the mirror world is drawn into, as large as the window in pixels, and the
        // camera drawing it, before the main one and with its faces' winding flipped back, since
        // the reflection turns every face inside out.
        _imageSize = PhysicalSize();
        _mirrorImage = Render.CreateTarget(_imageSize.Width, _imageSize.Height);
        _mirrorCamera = Render.SpawnCamera3d(new CameraSettings { Order = -1 });
        Render.SetCameraTarget(_mirrorCamera, _mirrorImage);
        ecs.Wrap<CameraRef>(_mirrorCamera).InvertCulling = true;

        _mirrorMaterial = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/screen_space_texture_material.slang"))
            .SetTexture("emissive_texture", _mirrorImage);
        _mirror = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 1f, 1f), _mirrorMaterial,
            new Transform(MirrorPosition, Quat.FromRotationX(MirrorRotationAngle), new Vec3(300f, 1f, 150f)));
        UpdateMirrorCamera(ecs);

        var column = RadioButtons<bool>.Column();
        _buttons = new RadioButtons<bool>(ecs, column, "Drag Action", [(false, "Move Camera"), (true, "Move Fox")], _moveFox);
        _help = Ui.SpawnText(HelpText(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    private static (uint Width, uint Height) PhysicalSize()
    {
        var (width, height) = Window.Size();
        var scale = Window.Scale();
        return ((uint)(width * scale), (uint)(height * scale));
    }

    // The mirror camera is the main one reflected across the mirror's plane, through the origin
    // with its normal down the Z axis. Reflecting a transform is reflecting its matrix, which
    // flips the Z of each axis and of the translation and leaves a matrix of negative determinant,
    // which glam, and so Bevy, takes apart as a scale of minus one on X.
    private static void UpdateMirrorCamera(EcsWorld ecs)
    {
        var camera = ecs.GetOrDefault<Transform>(_camera);
        var mirror = ecs.GetOrDefault<Transform>(_mirror);

        Vec3 Reflect(Vec3 v) => new(v.X, v.Y, -v.Z);
        var (x, y, z) = (camera.Rotation * Vec3.UnitX, camera.Rotation * Vec3.UnitY, camera.Rotation * Vec3.UnitZ);
        var reflected = Quat.FromBasis(Reflect(x) * -1f, Reflect(y), Reflect(z));
        ecs.Set(_mirrorCamera, new Transform(Reflect(camera.Translation), reflected, new Vec3(-1f, 1f, 1f)));

        // The near plane is the mirror's own, so nothing behind the mirror is drawn into it: its
        // normal in the main camera's view space and its distance from the camera.
        var normal = mirror.Rotation * Vec3.UnitY;
        var distance = Vec3.Dot(normal, mirror.Translation - camera.Translation);
        var facing = mirror.Rotation * -Vec3.UnitY;
        var inView = new Vec3(Vec3.Dot(x, facing), Vec3.Dot(y, facing), Vec3.Dot(z, facing)).Normalized;

        // Bevy's default perspective, which the main camera has, with the mirror's near plane. The
        // projection holds it in a variant beside the orthographic one's scaling mode, which no
        // wrapper types, so it is written as JSON.
        ecs.SetReflected(_mirrorCamera, Projection, string.Empty, FormattableString.Invariant(
            $$$"""{"Perspective":{"fov":0.7853982,"aspect_ratio":1.0,"near":0.1,"far":1000.0,"near_clip_plane":[{{{inView.X}}},{{{inView.Y}}},{{{inView.Z}}},{{{distance}}}]}}"""));
    }

    // The image is as large as the window, so it is made again when the window is resized.
    private static void HandleWindowResize(BehaviorContext ctx)
    {
        var size = PhysicalSize();
        if (size == _imageSize) return;

        _imageSize = size;
        _mirrorImage = Render.CreateTarget(size.Width, size.Height);
        Render.SetCameraTarget(_mirrorCamera, _mirrorImage);
        _mirrorMaterial.SetTexture("emissive_texture", _mirrorImage);
    }

    private static void HandleButtons(BehaviorContext ctx)
    {
        if (!_buttons!.Pressed(out var moveFox) || moveFox == _moveFox) return;
        _moveFox = moveFox;
        _buttons.Select(ctx.Ecs, moveFox);
        Ui.SetText(_help, HelpText());
    }

    // As camera_orbit does, though about the origin, while it keeps looking where it was.
    private static void MoveCameraOnMouseDown(BehaviorContext ctx)
    {
        var input = ctx.Input;
        if (_moveFox || !input.MouseDown(MouseButton.Left)) return;
        if (input.MouseDeltaX == 0f && input.MouseDeltaY == 0f) return;

        var ecs = ctx.Ecs;
        var camera = ecs.GetOrDefault<Transform>(_camera);
        var euler = camera.Rotation.ToEuler();
        var pitch = Math.Clamp(euler.X + input.MouseDeltaY * CameraPitchSpeed, -CameraPitchLimit, CameraPitchLimit);
        var yaw = euler.Y + input.MouseDeltaX * CameraYawSpeed;

        var rotation = Quat.FromRotationY(yaw) * Quat.FromRotationX(pitch);
        var forward = rotation * -Vec3.UnitZ;
        ecs.Set(_camera, new Transform(forward * -CameraOrbitDistance, rotation, Vec3.One));
        UpdateMirrorCamera(ecs);
    }

    // The fox follows the point of the ground under the pointer.
    private static void MoveFoxOnMouseDown(BehaviorContext ctx)
    {
        var input = ctx.Input;
        if (!_moveFox || _fox == Entity.None || !input.MouseDown(MouseButton.Left)) return;

        var (x, y) = input.MousePosition;
        if (!Render.TryRay(_camera, x, y, out var origin, out var direction) || MathF.Abs(direction.Y) < 1e-6f) return;
        var distance = -origin.Y / direction.Y;
        if (distance < 0f) return;

        var ground = origin + direction * distance;
        var fox = ctx.Ecs.GetOrDefault<Transform>(_fox);
        ctx.Ecs.Set(_fox, fox with { Translation = new Vec3(ground.X, fox.Translation.Y, ground.Z) });
    }

    // The fox's first clip, over and over, once the model has arrived.
    private static void PlayFoxAnimation(BehaviorContext ctx)
    {
        if (_foxPlaying || _fox == Entity.None || !Animation.TryClips(_fox, out var clips) || clips.Count == 0) return;
        _foxPlaying = Animation.Play(_fox, clips[0], new AnimationSettings { Repeat = true });
    }

    private static string HelpText() => $"Click and drag to move the {(_moveFox ? "fox" : "camera")}";
}
