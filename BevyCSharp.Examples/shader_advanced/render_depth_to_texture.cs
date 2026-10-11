// Bevy's render_depth_to_texture example, examples/shader_advanced/render_depth_to_texture.rs at
// v0.20.0, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Shading;

// A camera that renders depth alone, into no window and no image, and a plane showing what it saw:
// a turning cube, near in white and far in black. The depth camera is marked by a lime cone, and
// W, A, S and D move it around the cube.
//
// Bevy's copies the camera's depth into a depth texture after the prepass and reads it with a
// material of its own. This watches the camera's prepass depth with Shaders.Watch, which draws it
// into an image each frame that a plain unlit material shows, the same numbers as Bevy's draws.
internal static class RenderDepthToTexture
{
    private const uint DepthTextureSize = 256;
    private const float CameraMovementSpeed = 2f;

    private static Entity _depthCamera;

    public static void Build(App app)
    {
        app.Startup(Setup, "render_depth_to_texture.Setup");
        app.Update(DrawCameraGizmo, "render_depth_to_texture.DrawCameraGizmo");
        app.Update(MoveCamera, "render_depth_to_texture.MoveCamera");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        var cube = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 3f, 3f, 3f), Render.CreateMaterial((1f, 1f, 1f, 1f)), Transform.Identity);
        ecs.Add(cube, new RotatingCube());

        ecs.SpawnPointLight(new Vec3(5f, 6f, 7f));

        // Bevy's spawn_depth_only_camera, drawing nothing but its depth prepass, at 256 pixels square.
        _depthCamera = Render.SpawnCamera3d(new CameraSettings { Order = -1 });
        ecs.Add(_depthCamera, Transform.LookingAt(new Vec3(-4f, -5f, 5f), Vec3.Zero, Vec3.UnitY));
        ecs.Wrap<RenderTargetRef>(_depthCamera).Value = new RenderTarget.None(DepthTextureSize, DepthTextureSize);
        Render.SetPostProcessing(_depthCamera, new PostSettings { Msaa = 1 });
        ecs.Insert<DepthPrepassRef>(_depthCamera);

        // Bevy's spawn_plane, showing the depth.
        var depth = Shaders.Watch(_depthCamera, "depth", DepthTextureSize, DepthTextureSize);
        ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Rectangle, 4f, 4f),
            Render.CreateMaterial(new MaterialSettings { BaseColorTexture = depth, Unlit = true }),
            new Transform(new Vec3(10f, 4f, 0f), Quat.Identity, new Vec3(2.5f)));

        var main = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(5f, 2f, 30f), new Vec3(5f, 2f, 0f), Vec3.UnitY));
        Render.SetPostProcessing(main, new PostSettings { Msaa = 1 });

        Ui.SpawnText("Use WASD to move the secondary camera", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    // Bevy's draw_camera_gizmo, a cone where the depth camera is, pointing the way it looks.
    private static void DrawCameraGizmo(BehaviorContext ctx)
    {
        var transform = ctx.Ecs.GetRef<Transform>(_depthCamera);
        Gizmos.Cone(transform.Translation, transform.Rotation * Quat.FromRotationX(MathF.PI / 2f), 1f, 3f, Color.FromSrgb(0.196f, 0.804f, 0.196f));
    }

    // Bevy's move_camera, around the cube by its spherical coordinates.
    private static void MoveCamera(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var step = ctx.Time.Delta * CameraMovementSpeed;
        var (inclination, azimuth) = (0f, 0f);
        if (input.KeyDown(Key.W)) inclination -= step;
        if (input.KeyDown(Key.S)) inclination += step;
        if (input.KeyDown(Key.A)) azimuth += step;
        if (input.KeyDown(Key.D)) azimuth -= step;
        if (inclination == 0f && azimuth == 0f) return;

        ref var transform = ref ctx.Ecs.GetRef<Transform>(_depthCamera);
        var p = transform.Translation;
        var radius = p.Length;
        var newInclination = Math.Clamp(MathF.Acos(p.Y / radius) + inclination, 0.01f, MathF.PI - 0.01f);
        var newAzimuth = MathF.Atan2(p.Z, p.X) + azimuth;

        var at = radius * new Vec3(MathF.Sin(newInclination) * MathF.Cos(newAzimuth), MathF.Cos(newInclination), MathF.Sin(newInclination) * MathF.Sin(newAzimuth));
        transform = Transform.LookingAt(at, Vec3.Zero, Vec3.UnitY);
    }
}

/// <summary>Turns the cube about each axis at its own speed, as Bevy's rotate_cube does.</summary>
[Behavior]
public partial struct RotatingCube
{
    [OnUpdate]
    public void Rotate(BehaviorContext ctx, ref Transform transform)
    {
        var dt = ctx.Time.Delta;
        transform.Rotation = Quat.FromRotationZ(-1.3f * dt) * Quat.FromRotationY(1.1f * dt) * Quat.FromRotationX(1.5f * dt) * transform.Rotation;
    }
}
