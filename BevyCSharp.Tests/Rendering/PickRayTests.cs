using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers casting a ray at the scene's meshes, which is where a model dropped on the editor's view
/// is put.
/// </summary>
/// <remarks>
/// An offscreen app with a camera, since Bevy's mesh ray cast only meets what a camera can see, and
/// a bridge with the renderer, which carries mesh picking. Without either the test has nothing to
/// ask.
/// </remarks>
[Collection("engine")]
public sealed class PickRayTests
{
    [SkippableFact]
    public void ARayMeetsTheTopOfACubeAndPassesWhatIsOffTheDefaultLayer()
    {
        Needs.Renderer();

        using var app = new App(Config.OffscreenFor(320, 180, frames: 90));
        app.AddPlugin(new EnginePlugin());

        Entity cube = default;
        (bool Hit, Entity Entity, Vec3 Point, Vec3 Normal, Vec2? Uv)? down = null;
        bool? beside = null;
        bool? hidden = null;

        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world =>
            {
                var ecs = world.Resource<EcsWorld>();
                var camera = Render.SpawnCamera3d();
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 4f, 8f), Vec3.Zero, Vec3.UnitY));

                var mesh = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
                var material = Render.CreateMaterial(new MaterialSettings());

                cube = ecs.Spawn();
                ecs.Add(cube, Transform.At(0f, 0f, 0f));
                Render.SetMesh(ecs, cube, mesh);
                Render.SetMaterial(ecs, cube, material);

                // A cube where a ray will pass, drawn on a layer of its own as the editor's
                // previews are, which the cast leaves out.
                var preview = ecs.Spawn();
                ecs.Add(preview, Transform.At(3f, 0f, 0f));
                Render.SetMesh(ecs, preview, mesh);
                Render.SetMaterial(ecs, preview, material);
                Render.SetLayers(ecs, preview, 1u << 12);
            },
            "Test.Make"));

        app.AddSystem(Stage.Update, new SystemDescriptor(
            world =>
            {
                if (world.Resource<Time>().FrameCount != 60) return;

                down = Picking.TryCast(new Vec3(0f, 5f, 0f), -Vec3.UnitY, out var entity, out var point, out var normal, out var uv)
                    ? (true, entity, point, normal, uv)
                    : (false, Entity.None, Vec3.Zero, Vec3.Zero, null);

                beside = Picking.TryCast(new Vec3(-3f, 5f, 0f), -Vec3.UnitY, out _, out _, out _);
                hidden = Picking.TryCast(new Vec3(3f, 5f, 0f), -Vec3.UnitY, out _, out _, out _);
            },
            "Test.Cast"));

        Assert.Equal(0, app.Run());

        Assert.NotNull(down);
        Assert.True(down.Value.Hit, "the ray straight down missed the cube");
        Assert.Equal(cube, down.Value.Entity);
        Assert.Equal(0.5f, down.Value.Point.Y, 3);
        Assert.Equal(1f, down.Value.Normal.Y, 3);

        // The middle of the top face, which is the middle of the texture that face wears.
        Assert.NotNull(down.Value.Uv);
        Assert.Equal(0.5f, down.Value.Uv.Value.X, 3);
        Assert.Equal(0.5f, down.Value.Uv.Value.Y, 3);

        // Nothing to meet beside it, and the one on the preview layer is passed through.
        Assert.False(beside);
        Assert.False(hidden);
    }
}
