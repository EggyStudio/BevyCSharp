using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The clouds <see cref="Render.SetClouds"/> draws, read off pictures of the sky under an atmosphere
/// with and without them.
/// </summary>
[Collection("engine")]
public sealed class CloudTests
{
    [SkippableFact]
    public void NoCoverageLeavesTheSkyAsItWas()
    {
        Needs.Shaders();
        var bare = Sky(null);
        var none = Sky(new CloudSettings { Coverage = 0f });
        Assert.True(Covered(bare, none) == 0, $"{Covered(bare, none)} of the sky's pixels changed under no cloud");
    }

    [SkippableFact]
    public void MoreCoverageCoversMoreOfTheSky()
    {
        Needs.Shaders();
        var bare = Sky(null);
        var shares = new[] { 0.3f, 0.6f, 1f }.Select(coverage => Covered(bare, Sky(new CloudSettings { Coverage = coverage }))).ToArray();
        Assert.True(shares[0] < shares[1] && shares[1] < shares[2], $"the shares covered rise {string.Join(", ", shares)}");
        Assert.True(shares[2] > 0.5, $"a full coverage covers {shares[2]} of the sky");
    }

    [SkippableFact]
    public void GeometryStandsInFrontOfTheClouds()
    {
        Needs.Shaders();
        var bare = Sky(null, wall: true);
        var clouded = Sky(new CloudSettings { Coverage = 1f }, wall: true);
        // The wall fills the view's left quarter, and the clouds stay behind it.
        for (var y = 0u; y < bare.Height; y += 8)
            for (var x = 0u; x < bare.Width / 4; x += 8)
            {
                var (a, b) = (bare.At(x, y), clouded.At(x, y));
                Assert.True(Math.Abs(a.R - b.R) <= 2 && Math.Abs(a.G - b.G) <= 2 && Math.Abs(a.B - b.B) <= 2, $"the wall reads {b} under the clouds where {a} without them at ({x}, {y})");
            }
    }

    [SkippableFact]
    public void TheSunsDiscIsHiddenBehindAnOvercast()
    {
        Needs.Shaders();
        // Looking straight at a sun 25 degrees up, under no cloud and under an overcast.
        var bare = Sky(null, pitch: 25f, facingSun: true);
        var overcast = Sky(new CloudSettings { Coverage = 1f, Density = 0.08f }, pitch: 25f, facingSun: true);
        double Brightest(CapturedImage picture)
        {
            var most = 0.0;
            for (var y = picture.Height / 2 - 6; y <= picture.Height / 2 + 6; y++)
                for (var x = picture.Width / 2 - 6; x <= picture.Width / 2 + 6; x++)
                    most = Math.Max(most, Luma(picture.At(x, y)));
            return most;
        }
        Assert.True(Brightest(bare) > 240, $"the bare sun reads {Brightest(bare)}");
        Assert.True(Brightest(overcast) < Brightest(bare) - 40, $"the sun behind the overcast reads {Brightest(overcast)} where bare {Brightest(bare)}");
    }

    [SkippableFact]
    public void AMultisampledCameraDrawsNone()
    {
        Needs.Shaders();
        // Its depth cannot be read, which says where the sky is.
        var bare = Sky(null, msaa: 4);
        var clouded = Sky(new CloudSettings { Coverage = 1f }, msaa: 4);
        Assert.True(Covered(bare, clouded) == 0, $"{Covered(bare, clouded)} of the sky's pixels changed on a multisampled camera");
    }

    [Fact]
    public void CloudsOutOfRangeAreRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetClouds(default, new CloudSettings { Bottom = 3000f, Top = 2000f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetClouds(default, new CloudSettings { Density = 0f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetClouds(default, new CloudSettings { Coverage = float.NaN }));
    }

    private static double Luma((byte R, byte G, byte B, byte A) pixel) => (0.2126 * pixel.R) + (0.7152 * pixel.G) + (0.0722 * pixel.B);

    // The share of the upper half's pixels, all sky, that differ by more than ten levels between two pictures.
    private static double Covered(CapturedImage bare, CapturedImage clouded)
    {
        var changed = 0;
        var all = 0;
        for (var y = 0u; y < bare.Height / 2; y += 2)
            for (var x = 0u; x < bare.Width; x += 2)
            {
                var (a, b) = (bare.At(x, y), clouded.At(x, y));
                if (Math.Max(Math.Abs(a.R - b.R), Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B))) > 10) changed++;
                all++;
            }
        return (double)changed / all;
    }

    // The sky looked at from two meters up, tilted up by a pitch, under an atmosphere with the sky's
    // light, a sun 45 degrees up or 25 ahead of the view, with clouds or none, a wall over the left of
    // the view, on a camera with some samples a pixel.
    private static CapturedImage Sky(CloudSettings? clouds, bool wall = false, float pitch = 30f, bool facingSun = false, int msaa = 1)
    {
        var run = new PictureRun
        {
            Width = 480,
            Height = 270,
            Frames = 1500,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 60f });
                var tilt = pitch * MathF.PI / 180f;
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 2f, 0f), new Vec3(-MathF.Cos(tilt), 2f + MathF.Sin(tilt), 0f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = msaa, Tonemapper = Tonemapper.TonyMcMapface });
                Render.SetAtmosphere(camera, new AtmosphereSettings());
                Render.SetSkyLighting(camera);

                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 12_500f, Shadows = false });
                var up = (facingSun ? pitch : 45f) * MathF.PI / 180f;
                var aside = facingSun ? 0f : -0.5f;
                ecs.Add(sun, Transform.LookingAt(new Vec3(-MathF.Cos(up), MathF.Sin(up), aside), Vec3.Zero, Vec3.UnitY));

                if (wall)
                {
                    // Ten meters ahead and to the left, tall enough to fill the left quarter of the view.
                    var face = ecs.Spawn();
                    Render.SetMesh(ecs, face, Render.CreateMesh(MeshShape.Cuboid, 1f, 400f, 20f));
                    Render.SetMaterial(ecs, face, Render.CreateMaterial(new MaterialSettings { BaseColor = (0.4f, 0.4f, 0.4f, 1f), Unlit = true }));
                    ecs.Add(face, Transform.At(-10f, 2f, 14.5f));
                }

                if (clouds is not null) Render.SetClouds(camera, clouds);
            },
        };
        run.Wait(10)
            .Until("compiled", _ => Clouds.All().Count() == (clouds is null ? 0 : 1) && (clouds is null || Clouds.Ready()))
            .Until("ready", _ => Render.PipelinesReady())
            .Wait(60)
            .Capture("sky")
            .Go();
        return run.Picture("sky");
    }
}
