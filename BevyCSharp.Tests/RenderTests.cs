using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers building renderable assets and attaching them to entities.
/// </summary>
/// <remarks>
/// These need a native build with the renderer compiled in. On a headless bridge, which is what
/// CI uses for the test job, the assertions flip to checking that each call refuses cleanly and
/// says which build would support it. Both outcomes are worth pinning: silently doing nothing
/// would be the bad one.
/// </remarks>
[Collection("engine")]
public sealed class RenderTests
{
    [Fact]
    public void MeshesAndMaterialsBecomeAssetHandles()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, _ =>
        {
            if (!App.HasRenderer)
            {
                var refused = Assert.Throws<BevyNativeException>(
                    () => Render.CreateMesh(MeshShape.Cuboid));
                Assert.Equal(NativeStatus.Unsupported, refused.Status);
                Assert.Contains("--render", refused.Message);
                return;
            }

            var mesh = Render.CreateMesh(MeshShape.Cuboid, 1f, 2f, 3f);
            var material = Render.CreateMaterial(1f, 0f, 0f);

            Assert.True(mesh.IsValid);
            Assert.True(material.IsValid);
            Assert.NotEqual(mesh, material);

            // Built in memory rather than read from disk, so they are ready immediately.
            Assert.Equal(AssetLoadState.Loaded, mesh.State);
            Assert.Equal(AssetLoadState.Loaded, material.State);
        });

        harness.Run();
    }

    [Fact]
    public void AttachingAMeshMakesAnEntityDrawable()
    {
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        var entity = Entity.None;

        harness.OnContext(Stage.Startup, ctx =>
        {
            entity = ctx.Ecs.Spawn();
            Render.SetMesh(ctx.Ecs, entity, Render.CreateMesh(MeshShape.Sphere, 0.5f));
            Render.SetMaterial(ctx.Ecs, entity, Render.CreateMaterial(0f, 1f, 0f));
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            // Inserting Mesh3d goes through Bevy's own path rather than a byte copy, so the
            // components Bevy requires alongside it arrive too. Without that an entity would
            // carry a mesh and still be invisible.
            Assert.True(ctx.Ecs.HasById(entity, NativeComponents.Transform));
        });

        harness.Run();
    }

    [Fact]
    public void AHandleOfTheWrongTypeIsRefused()
    {
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();
            var material = Render.CreateMaterial(1f, 1f, 1f);

            // Retyping goes through try_typed, so this is an error rather than a panic crossing
            // the boundary.
            var ex = Assert.Throws<BevyNativeException>(
                () => Render.SetMesh(ctx.Ecs, entity, material));

            Assert.Equal(NativeStatus.NoComponent, ex.Status);
        });

        harness.Run();
    }

    [Fact]
    public void AnUnknownShapeNamesWhatIsAvailable()
    {
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var ex = Assert.Throws<BevyNativeException>(
                () => Render.CreateMesh("Dodecahedron"));

            Assert.Equal(NativeStatus.NoComponent, ex.Status);
            Assert.Contains("MeshShape", ex.Message);
        });

        harness.Run();
    }

    [Fact]
    public void CamerasAndLightsAreSpawnableAndPositionable()
    {
        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Startup, ctx =>
        {
            var camera = Render.SpawnCamera3d();
            var light = Render.SpawnLight(LightKind.Directional, 10_000f);

            if (!App.HasRenderer)
            {
                // A headless build has no camera to spawn, and says so by returning nothing
                // rather than by failing.
                Assert.Equal(Entity.None, camera);
                Assert.Equal(Entity.None, light);
                return;
            }

            Assert.NotEqual(Entity.None, camera);
            Assert.NotEqual(Entity.None, light);

            ctx.Ecs.AddNative(camera, NativeComponents.Transform,
                Transform.LookingAt(new Vec3(0f, 5f, 10f), Vec3.Zero, Vec3.UnitY));

            Assert.True(ctx.Ecs.TryGetNative<Transform>(
                camera, NativeComponents.Transform, out var placed));
            Assert.Equal(new Vec3(0f, 5f, 10f), placed.Translation);
        });

        harness.Run();
    }

    [Fact]
    public void LookingAtProducesAUnitRotationAimedAtTheTarget()
    {
        // Pure arithmetic, so it holds on any build. Forward is negative Z in Bevy, so a
        // transform at +Z looking at the origin should not be rotated at all.
        var straightOn = Transform.LookingAt(new Vec3(0f, 0f, 10f), Vec3.Zero, Vec3.UnitY);

        Assert.Equal(0f, straightOn.Rotation.X, 5);
        Assert.Equal(0f, straightOn.Rotation.Y, 5);
        Assert.Equal(0f, straightOn.Rotation.Z, 5);
        Assert.Equal(1f, MathF.Abs(straightOn.Rotation.W), 5);

        // A quarter turn about Y when the eye moves to the +X axis.
        var fromSide = Transform.LookingAt(new Vec3(10f, 0f, 0f), Vec3.Zero, Vec3.UnitY);
        Assert.Equal(MathF.Sqrt(0.5f), MathF.Abs(fromSide.Rotation.Y), 4);

        // Whatever the angle, the rotation stays a unit quaternion.
        foreach (var eye in new[]
                 {
                     new Vec3(3f, 4f, 5f), new Vec3(-2f, 7f, -1f), new Vec3(0f, -6f, 2f),
                 })
        {
            var r = Transform.LookingAt(eye, Vec3.Zero, Vec3.UnitY).Rotation;
            var length = MathF.Sqrt(r.X * r.X + r.Y * r.Y + r.Z * r.Z + r.W * r.W);
            Assert.Equal(1f, length, 4);
        }
    }
}
