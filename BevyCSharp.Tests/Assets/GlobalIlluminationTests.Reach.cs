using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A wall in shade on the far side of a yard seen along its diagonal, 56 units off along each
    /// axis, whose other wall the sun lights, reads at Medium the light its reference gives it, the
    /// probes reaching it as High's do.
    /// </summary>
    /// <remarks>
    /// Medium's probes lie in three cascades, the last in a box 64 units wide lying 0.35 of its
    /// width ahead of the eye, which a wall 56 units off along each axis of a diagonal view lies
    /// past. The light past the probes, a sky over a ground, takes its ground from a cell in front
    /// of the wall, which lies in the wall's own shadow at a low sun, so a wall lit by it alone
    /// takes none of the sunlit wall's and floor's light, as the feature test's yard's far wall at
    /// eight in the morning. The ambient light is none and the camera has no environment map, so
    /// the wall's light comes from the yard's sunlit faces alone.
    /// </remarks>
    [SkippableFact]
    public void AFarWallInShadeTakesTheYardsLightAtMedium()
    {
        Needs.Shaders();
        var supported = false;
        var (begun, read, ready) = (false, false, 0);
        new PictureRun
        {
            Width = 160,
            Height = 90,
            Frames = 6000,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 1.7f, 0f));
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1.7f, 0f), new Vec3(56f, 4f, -56f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight((1f, 1f, 1f), 0f);
                var white = Render.CreateMaterial(0.8f, 0.8f, 0.8f);
                ecs.SetName(PictureRun.Cube(ecs, white, 200f, new Vec3(0f, -100f, 0f)), "floor");
                void Wall(string name, Vec3 at, Vec3 size)
                {
                    var wall = ecs.Spawn();
                    Render.SetMesh(ecs, wall, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
                    Render.SetMaterial(ecs, wall, white);
                    ecs.Add(wall, Transform.At(at.X, at.Y, at.Z));
                    ecs.SetName(wall, name);
                }
                Wall("sunlit", new Vec3(28f, 8f, -56.25f), new Vec3(56f, 16f, 0.5f));
                Wall("shaded", new Vec3(56.25f, 8f, -28f), new Vec3(0.5f, 16f, 56f));
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(new Vec3(0.45f, 0.35f, 0.82f), Vec3.Zero, Vec3.UnitY));
                Render.SetGlobalIllumination(GlobalIllumination.Medium);
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("settled", _ => !supported || SceneField.Plan is { } plan && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 120)
            .Until("drawn", _ => !supported || EngineDrawsOn(traced: true))
            .Until("ready", _ => !supported || (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3)
            .Until("referenced", world =>
            {
                if (!supported) return true;
                var ecs = world.Resource<EcsWorld>();
                if (!begun)
                {
                    begun = true;
                    Assert.Null(GiReference.BeginReference(ecs, null, 256));
                    return false;
                }
                return GiReference.Step(ecs) is not null;
            })
            .Until("read", world =>
            {
                if (!supported) return true;
                var ecs = world.Resource<EcsWorld>();
                if (!read)
                {
                    read = true;
                    Assert.Null(GiReference.BeginPicture(ecs, null));
                    return false;
                }
                return GiReference.Step(ecs) is not null;
            })
            .Go();
        Needs.RayQueries(supported);

        var traced = GiReference.Last!;
        var picture = GiReference.LastPicture!.Value.Light;
        var face = traced.Names.Single(n => n.Value == "shaded -x").Key;
        var (shown, wanted) = (0.0, 0.0);
        for (var i = 0; i < traced.Regions.Length; i++)
        {
            if (traced.Regions[i] != face) continue;
            for (var c = 0; c < 3; c++)
            {
                shown += picture[i * 3 + c];
                wanted += traced.Light[i * 3 + c];
            }
        }

        var light = shown / wanted - 1;
        output.WriteLine($"the far wall reads {light:+0.000;-0.000} of its reference's light");
        Assert.True(Math.Abs(light) < 0.3, $"the far wall reads {light:+0.000;-0.000} of its reference's light");
    }
}
