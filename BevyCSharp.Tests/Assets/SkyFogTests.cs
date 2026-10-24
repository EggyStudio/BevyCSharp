using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The fog <see cref="Render.SetSkyFog"/> draws, read off pictures of level ground running far
/// ahead under it, in a color of its own so the fog is told from the scene by its hue.
/// </summary>
[Collection("engine")]
public sealed class SkyFogTests
{
    // Red, bright enough to read plainly without tonemapping and dim enough not to clip.
    private static readonly SkyFogSettings Red = new() { Color = (1f, 0.05f, 0.05f), Brightness = 500f, Height = 1000f };

    [SkippableFact]
    public void FarGroundFadesIntoTheFogAndNearGroundKeepsItsColor()
    {
        Needs.Renderer();
        var fogged = Ground(new SkyFogSettings { Color = Red.Color, Brightness = Red.Brightness, Height = 1000f, Visibility = 300f });

        // The bottom row sees the ground 17 ahead, the row 13 below the horizon 200 ahead.
        var near = fogged.At(fogged.Width / 2, fogged.Height - 2);
        var far = fogged.At(fogged.Width / 2, fogged.Height / 2 + 13);
        Assert.True(Redness(far) > Redness(near) + 80, $"far {far}, near {near}");
        Assert.True(near.G > 60, $"the near ground's own green is mostly there, {near}");
    }

    [SkippableFact]
    public void TheFogThinsUpward()
    {
        Needs.Renderer();
        // A gray wall 200 ahead standing 80 high, in fog that falls by e every 5, seen at 3.5 high
        // and at 75.
        var fogged = Ground(new SkyFogSettings { Color = Red.Color, Brightness = Red.Brightness, Height = 5f, Visibility = 100f }, eye: 2f, wall: true);

        var low = fogged.At(fogged.Width / 2, fogged.Height / 2 - 2);
        var high = fogged.At(fogged.Width / 2, fogged.Height / 2 - 90);
        Assert.True(Redness(low) > 100, $"the wall's foot is lost in the fog, {low}");
        Assert.True(Redness(high) < 60, $"its top stands clear, {high}");
    }

    [SkippableFact]
    public void BelowTheHorizonTheBandDarkensDownward()
    {
        Needs.Renderer();
        var sky = Ground(Red, ground: false);

        var above = sky.At(sky.Width / 2, sky.Height / 2 - 20);
        var under = sky.At(sky.Width / 2, sky.Height / 2 + 2);
        var deep = sky.At(sky.Width / 2, sky.Height - 2);
        Assert.True(Redness(under) > 110, $"right under the horizon is the fog's color, {under}");
        Assert.True(deep.R < under.R - 40 && Redness(deep) > 40, $"30 degrees down it darkens and keeps its hue, {deep} under {under}");
        Assert.True(Redness(above) < 20, $"above the horizon is the camera's own, {above}");
    }

    // Each of Bevy's passes over the picture takes it into a fresh texture, and the fog is drawn
    // before them all, so they carry it on rather than the picture from before it.
    [SkippableTheory]
    [InlineData("vignette")]
    [InlineData("bloom")]
    [InlineData("temporal")]
    public void BevysPassesOverThePictureKeepTheFog(string pass)
    {
        Needs.Renderer();
        var sky = Ground(Red, ground: false, pass: pass);

        var under = sky.At(sky.Width / 2, sky.Height / 2 + 4);
        Assert.True(Redness(under) > 100, $"under {pass}, {under}");
    }

    [SkippableFact]
    public void AMultisampledCameraDrawsNone()
    {
        Needs.Renderer();
        // Its depth cannot be read, which would read as nothing in front of every pixel.
        var sky = Ground(Red, msaa: 4);

        var near = sky.At(sky.Width / 2, sky.Height - 2);
        var under = sky.At(sky.Width / 2, sky.Height / 2 + 13);
        Assert.True(Redness(near) < 20 && Redness(under) < 20, $"near {near}, toward the horizon {under}");
    }

    [SkippableFact]
    public void TheConsoleTakesTheFogOffAndPutsItBackWhereItWas()
    {
        Needs.Renderer();
        string? off = null, said = null, back = null;
        SkyFogSettings? kept = null;
        var run = new PictureRun
        {
            Scene = _ =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings());
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetSkyFog(camera, Red);
            },
        };
        run.Wait(2).Do("console", _ =>
        {
            off = ConsoleSkyCommands.Fog("off");
            said = ConsoleSkyCommands.Fog();
            back = ConsoleSkyCommands.Fog("300", 20f);
            kept = Assert.Single(SkyFog.All()).Settings;
        }).Go();

        Assert.Equal("the sky's fog is off", off);
        Assert.Equal("no camera draws the sky's fog", said);
        Assert.EndsWith("on 1 camera", back);
        Assert.Equal((300f, 20f), (kept!.Visibility, kept.Height));
        Assert.Equal(Red.Color, kept.Color);
    }

    [Fact]
    public void SettingsOutOfRangeAreRefused()
    {
        Assert.Throws<ArgumentNullException>(() => Render.SetSkyFog(default, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetSkyFog(default, new SkyFogSettings { Visibility = 0f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetSkyFog(default, new SkyFogSettings { Height = -1f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetSkyFog(default, new SkyFogSettings { Base = float.NaN }));
    }

    // How much more red a pixel is than green and blue.
    private static int Redness((byte R, byte G, byte B, byte A) pixel) => pixel.R - Math.Max(pixel.G, pixel.B);

    // A level view from a height over unlit green ground running 400 ahead, under a fog, with no
    // tonemapping so a level reads as the fog left it. A wall can stand 200 ahead, and one of
    // Bevy's passes can run over the picture.
    private static CapturedImage Ground(SkyFogSettings fog, float eye = 10f, bool ground = true, bool wall = false, int msaa = 1, string? pass = null)
    {
        var run = new PictureRun
        {
            Width = 480,
            Height = 270,
            Frames = 1200,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 60f });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, eye, 0f), new Vec3(0f, eye, -1f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings
                {
                    Hdr = true,
                    Msaa = msaa,
                    Tonemapper = Tonemapper.None,
                    Bloom = pass == "bloom",
                    BloomIntensity = 0.3f,
                    AntiAlias = pass == "temporal" ? AntiAliasPass.Temporal : AntiAliasPass.None,
                });
                if (pass == "vignette") Render.SetEffects(camera, new EffectSettings { Vignette = 0.45f, VignetteRadius = 0.7f });

                if (ground)
                {
                    var floor = ecs.Spawn();
                    Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 800f, 1f, 400f));
                    Render.SetMaterial(ecs, floor, Render.CreateMaterial(new MaterialSettings { BaseColor = (0.1f, 0.45f, 0.1f, 1f), Unlit = true }));
                    ecs.Add(floor, Transform.At(0f, -0.5f, -200f));
                }

                if (wall)
                {
                    var face = ecs.Spawn();
                    Render.SetMesh(ecs, face, Render.CreateMesh(MeshShape.Cuboid, 800f, 80f, 1f));
                    Render.SetMaterial(ecs, face, Render.CreateMaterial(new MaterialSettings { BaseColor = (0.5f, 0.5f, 0.5f, 1f), Unlit = true }));
                    ecs.Add(face, Transform.At(0f, 40f, -200f));
                }

                Render.SetSkyFog(camera, fog);
            },
        };
        // The fog's draw is compiled from Slang while the app runs, which pipelines being ready
        // does not cover, so the run waits on the draw itself, then for its pipeline.
        run.Wait(10)
            .Until("compiled", _ => SkyFog.All().All(f => f.Ready))
            .Until("ready", _ => Render.PipelinesReady())
            .Wait(30)
            .Capture("fog")
            .Go();
        return run.Picture("fog");
    }
}
