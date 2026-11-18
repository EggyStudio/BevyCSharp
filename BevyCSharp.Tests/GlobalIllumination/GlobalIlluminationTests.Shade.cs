using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A dark floor of stone in a wall's shade under an open sky, seen low along it, reads its
    /// reference's light within a tenth with global illumination on, and no pixel of it below
    /// nothing.
    /// </summary>
    /// <remarks>
    /// In the shade the light from all around is nearly all the floor has, and the pass takes back
    /// what Bevy's lighting gave it before it adds the probes' light. Bevy reads the sky's
    /// reflection along the mirror way bent toward the normal as the surface grows rough, and a
    /// floor seen low reflects along a way near the horizon, where the sky is brighter than above
    /// it, so a take-back read along the mirror way itself takes back more than Bevy gave, and the
    /// floor reads under the probes' light and below nothing where the probes give it little.
    /// </remarks>
    [SkippableFact]
    public void AFloorInAWallsShadeUnderAnOpenSkyReadsItsReference()
    {
        Needs.Shaders();
        var supported = false;
        string? traced = null, read = null;
        var (askedTrace, askedRead, ready) = (false, false, 0);
        new PictureRun
        {
            Width = 160,
            Height = 90,
            Frames = 6000,
            Scene = ecs =>
            {
                var eye = new Vec3(-10f, 1.5f, 1f);
                var camera = PictureRun.Camera(ecs, eye);
                ecs.Add(camera, Transform.LookingAt(eye, new Vec3(2f, 0f, 1f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAtmosphere(camera, new AtmosphereSettings());
                Render.SetSkyLighting(camera);
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                var stone = Render.CreateMaterial(0.1f, 0.1f, 0.1f, roughness: 0.7f);
                ecs.SetName(PictureRun.Cube(ecs, stone, 40f, new Vec3(0f, -20f, 0f)), "floor");
                var wall = ecs.Spawn();
                Render.SetMesh(ecs, wall, Render.CreateMesh(MeshShape.Cuboid, 12f, 6f, 0.5f));
                Render.SetMaterial(ecs, wall, stone);
                ecs.Add(wall, Transform.At(0f, 3f, -2f));
                ecs.SetName(wall, "wall");

                // The floor's patch in the wall's shade, which the sun 45 degrees up behind the
                // wall casts 6 m long.
                var shade = ecs.Spawn();
                Render.SetMesh(ecs, shade, Render.CreateMesh(MeshShape.Plane, 8f, 3f));
                Render.SetMaterial(ecs, shade, stone);
                ecs.Add(shade, Transform.At(0f, 0.01f, 1f));
                ecs.SetName(shade, "shade");
                var elevation = 45f * MathF.PI / 180f;
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(new Vec3(0f, MathF.Sin(elevation), -MathF.Cos(elevation)), Vec3.Zero, Vec3.UnitY));
                Render.SetGlobalIllumination(GlobalIllumination.Medium);
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("settled", _ => !supported || SceneField.Plan is { } plan && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 120)
            .Until("drawn", _ => !supported || EngineDrawsOn(traced: true))
            .Until("ready", _ => !supported || (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3)
            .Until("referenced", world => !supported || ReadOnce(world.Resource<EcsWorld>(), ref askedTrace, ref traced, ecs => GiReference.BeginReference(ecs, null, 256)))
            .Until("read", world => !supported || ReadOnce(world.Resource<EcsWorld>(), ref askedRead, ref read, ecs => GiReference.BeginPicture(ecs, null)))
            .Go();
        Needs.RayQueries(supported);

        var reference = GiReference.Last!;
        var picture = GiReference.LastPicture!.Value.Light;
        var region = reference.Names.Single(n => n.Value == "shade +y").Key;
        var (pixels, shown, wanted, least) = (0, 0.0, 0.0, double.MaxValue);
        for (var i = 0; i < reference.Regions.Length; i++)
        {
            if (reference.Regions[i] != region) continue;
            pixels++;
            shown += 0.2126 * picture[i * 3] + 0.7152 * picture[i * 3 + 1] + 0.0722 * picture[i * 3 + 2];
            wanted += 0.2126 * reference.Light[i * 3] + 0.7152 * reference.Light[i * 3 + 1] + 0.0722 * reference.Light[i * 3 + 2];
            least = Math.Min(least, Math.Min(picture[i * 3], Math.Min(picture[i * 3 + 1], picture[i * 3 + 2])));
        }

        var light = shown / wanted - 1;
        output.WriteLine($"the shade, {pixels} pixels, reads {light:+0.000;-0.000} of its reference's light, its least channel {least:0.0000}");
        Assert.True(pixels > 200, $"the shade shows in {pixels} pixels");
        Assert.True(least >= 0, $"a pixel of the shade reads {least:0.0000}");
        Assert.True(Math.Abs(light) < 0.1, $"the shade reads {light:+0.000;-0.000} of its reference's light");
    }
}
