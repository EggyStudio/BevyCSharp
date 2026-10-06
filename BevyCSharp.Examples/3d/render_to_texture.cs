// Bevy's render_to_texture example, examples/3d/render_to_texture.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Shows how to render to a texture. Useful for mirrors, UI, or exporting images.
internal static class RenderToTexture
{
    // The first pass draws on its own layer, which the main camera does not see.
    private const uint FirstPassLayer = 1u << 1;

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        var image = Render.CreateTarget(512, 512);

        var inner = ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Cuboid, 4f, 4f, 4f),
            Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb(0.8f, 0.7f, 0.6f), Reflectance = 0.02f }),
            Transform.At(0f, 0f, 1f));
        Render.SetLayers(ecs, inner, FirstPassLayer);
        ecs.Add(inner, new FirstPassCube());

        // Lighting both passes.
        var light = ecs.SpawnPointLight(new Vec3(0f, 0f, 10f));
        Render.SetLayers(ecs, light, 1u | FirstPassLayer);

        // Drawn before the main camera, into the image, on a white ground.
        var first = ecs.SpawnCamera3d(
            Transform.LookingAt(new Vec3(0f, 0f, 15f), Vec3.Zero, Vec3.UnitY),
            new CameraSettings { Order = -1, Clear = ClearMode.Custom, ClearColor = (1f, 1f, 1f, 1f), Layers = FirstPassLayer });
        Render.SetCameraTarget(first, image);

        // The cube in the main pass, wearing the first pass as its texture.
        var outer = ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Cuboid, 4f, 4f, 4f),
            Render.CreateMaterial(new MaterialSettings { BaseColorTexture = image, Reflectance = 0.02f }),
            new Transform(new Vec3(0f, 0f, 1.5f), Quat.FromRotationX(-MathF.PI / 5f), Vec3.One));
        ecs.Add(outer, new MainPassCube());

        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 15f), Vec3.Zero, Vec3.UnitY));
    });
}

/// <summary>The cube the first pass draws, which turns about X and Z.</summary>
[Behavior]
public partial struct FirstPassCube
{
    [OnUpdate]
    public void Rotate(BehaviorContext ctx, ref Transform transform)
    {
        // Each turn about the world's axes, as Bevy's rotate_x and rotate_z are.
        transform.Rotation = Quat.FromRotationX(1.5f * ctx.Time.Delta) * transform.Rotation;
        transform.Rotation = Quat.FromRotationZ(1.3f * ctx.Time.Delta) * transform.Rotation;
    }
}

/// <summary>The cube the main pass draws, which turns about X and Y.</summary>
[Behavior]
public partial struct MainPassCube
{
    [OnUpdate]
    public void Rotate(BehaviorContext ctx, ref Transform transform)
    {
        transform.Rotation = Quat.FromRotationX(ctx.Time.Delta) * transform.Rotation;
        transform.Rotation = Quat.FromRotationY(0.7f * ctx.Time.Delta) * transform.Rotation;
    }
}
