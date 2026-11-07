using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A white wall turned from a sun ten degrees up under the camera's air, lit by the white floor
    /// before it and nothing else, reads the light and the color its reference gives it, the probes
    /// lighting the floor they bounce from by the sun through the air, as Bevy lights the picture.
    /// </summary>
    /// <remarks>
    /// The probes were lit by the sun as it is above the air, so at a low sun the light they
    /// bounced was brighter than the floor the picture showed and near white where the picture's
    /// floor was reddened, and a wall in shade read brighter and bluer than its reference. The
    /// ambient light is none and the camera has no environment map, so the wall's light is the
    /// floor's alone.
    /// </remarks>
    [SkippableFact]
    public void AWallInShadeTakesTheFloorsLightAsItsReferenceDoesUnderALowSun()
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
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 1.6f, 5f));
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1.6f, 5f), new Vec3(0f, 1f, -2f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAtmosphere(camera, new AtmosphereSettings());
                Render.SetAmbientLight((1f, 1f, 1f), 0f);
                var white = Render.CreateMaterial(0.8f, 0.8f, 0.8f);
                ecs.SetName(PictureRun.Cube(ecs, white, 40f, new Vec3(0f, -20f, 0f)), "floor");
                var wall = ecs.Spawn();
                Render.SetMesh(ecs, wall, Render.CreateMesh(MeshShape.Cuboid, 8f, 4f, 0.5f));
                Render.SetMaterial(ecs, wall, white);
                ecs.Add(wall, Transform.At(0f, 2f, -3f));
                ecs.SetName(wall, "wall");
                var elevation = 10f * MathF.PI / 180f;
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
                ecs.Add(sun, Transform.LookingAt(new Vec3(MathF.Cos(elevation), MathF.Sin(elevation), 0f), Vec3.Zero, Vec3.UnitY));
                Render.SetGlobalIllumination(GlobalIllumination.High);
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
        var face = traced.Names.Single(n => n.Value == "wall +z").Key;
        var (shown, wanted) = (new double[3], new double[3]);
        for (var i = 0; i < traced.Regions.Length; i++)
        {
            if (traced.Regions[i] != face) continue;
            for (var c = 0; c < 3; c++)
            {
                shown[c] += picture[i * 3 + c];
                wanted[c] += traced.Light[i * 3 + c];
            }
        }

        var light = (shown[0] + shown[1] + shown[2]) / (wanted[0] + wanted[1] + wanted[2]) - 1;
        var balance = shown[2] / shown[0] / (wanted[2] / wanted[0]) - 1;
        output.WriteLine($"the wall reads {light:+0.000;-0.000} of its reference's light, its blue over its red {balance:+0.000;-0.000} of the reference's");
        Assert.True(Math.Abs(light) < 0.3, $"the wall reads {light:+0.000;-0.000} of its reference's light");
        Assert.True(Math.Abs(balance) < 0.15, $"the wall's blue over its red reads {balance:+0.000;-0.000} of its reference's");
    }
}
