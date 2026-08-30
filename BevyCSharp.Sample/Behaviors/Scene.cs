using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// The default scene: a cube turning in place, lit, above a ground plane.
/// </summary>
/// <remarks>
/// Skipped on a build without a renderer, so the rest of the sample runs unchanged either way.
/// </remarks>
[Behavior]
public partial struct Scene
{
    /// <summary>Radians per second about the vertical axis.</summary>
    public float YawSpeed;

    /// <summary>Radians per second about the horizontal axis.</summary>
    public float PitchSpeed;

    /// <summary>Current yaw in radians.</summary>
    public float Yaw;

    /// <summary>Current pitch in radians.</summary>
    public float Pitch;

    [OnStartup]
    public static void Build(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var camera = Render.SpawnCamera3d();
        ctx.Ecs.AddNative(camera, NativeComponents.Transform,
            Transform.LookingAt(new Vec3(3.5f, 3f, 6f), Vec3.Zero, Vec3.UnitY));

        var sun = Render.SpawnLight(LightKind.Directional, 500f);
        ctx.Ecs.AddNative(sun, NativeComponents.Transform,
            Transform.LookingAt(new Vec3(4f, 8f, 5f), Vec3.Zero, Vec3.UnitY));

        if (Environment.GetEnvironmentVariable("BCS_NOGROUND") is null) {
        var ground = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, ground, Render.CreateMesh(MeshShape.Plane, 24f, 24f));
        Render.SetMaterial(ctx.Ecs, ground,
            Render.CreateMaterial(0.8f, 0.1f, 0.1f, roughness: 0.9f));
        ctx.Ecs.AddNative(ground, NativeComponents.Transform, Transform.At(0f, -1.2f, 0f));
        }

        var cube = ctx.Ecs.Spawn();
        Render.SetMesh(ctx.Ecs, cube, Render.CreateMesh(MeshShape.Cuboid, 1.6f, 1.6f, 1.6f));
        Render.SetMaterial(ctx.Ecs, cube,
            Render.CreateMaterial(0.25f, 0.55f, 0.85f, metallic: 0.1f, roughness: 0.35f));
        ctx.Ecs.AddNative(cube, NativeComponents.Transform, Transform.Identity);

        // Turning about two axes rather than one, so the cube reads as a solid rather than a
        // flat outline.
        ctx.Ecs.Add(cube, new Scene { YawSpeed = 0.9f, PitchSpeed = 0.35f });

        Console.WriteLine("[Scene] a rotating cube. Escape closes the window.");
    }

    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        Yaw += YawSpeed * ctx.Time.Delta;
        Pitch += PitchSpeed * ctx.Time.Delta;

        ref var transform = ref ctx.Ecs.GetNativeRef<Transform>(
            ctx.Entity, NativeComponents.Transform);

        transform.Rotation = Quat.FromRotationY(Yaw) * Quat.FromRotationX(Pitch);
    }
}
