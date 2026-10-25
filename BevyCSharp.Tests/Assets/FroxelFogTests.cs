using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The fog worked out in froxels (<see cref="SkyFogSettings.Froxels"/>), held to the closed form
/// where nothing but the layer is in it, and read where a shadow, a lamp and a box change it, with
/// the atmosphere's transmittance the froxels and the clouds after them light by.
/// </summary>
[Collection("engine")]
public sealed class FroxelFogTests
{
    // Red, bright enough to read plainly without tonemapping and dim enough not to clip.
    private static SkyFogSettings Red(float visibility = 300f, FroxelFogSettings? froxels = null) => new()
    {
        Color = (1f, 0.05f, 0.05f),
        Brightness = 500f,
        Height = 1000f,
        Visibility = visibility,
        Froxels = froxels,
    };

    [SkippableFact]
    public void WithNothingButTheLayerTheFroxelsAreTheClosedForm()
    {
        Needs.Shaders();
        var closed = Picture(Red(), ground: true);
        var froxels = Picture(Red(froxels: new FroxelFogSettings { Lumps = 0f, Distance = 150f }), ground: true);

        var worst = (Difference: 0, X: 0u, Y: 0u);
        for (var y = closed.Height / 2 + 2; y < closed.Height; y += 2)
            for (var x = 0u; x < closed.Width; x += 4)
            {
                var (a, b) = (closed.At(x, y), froxels.At(x, y));
                var difference = Math.Max(Math.Abs(a.R - b.R), Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B)));
                if (difference > worst.Difference) worst = (difference, x, y);
            }
        Assert.True(worst.Difference <= 6, $"the froxels drew {froxels.At(worst.X, worst.Y)} where the closed form drew {closed.At(worst.X, worst.Y)} at ({worst.X}, {worst.Y})");

        // Above the horizon the sky holds the layer's haze, and both leave it as it was.
        var (above, sky) = (froxels.At(froxels.Width / 2, froxels.Height / 4), closed.At(closed.Width / 2, closed.Height / 4));
        Assert.True(Math.Abs(above.R - sky.R) <= 2 && Math.Abs(above.B - sky.B) <= 2, $"the sky reads {above} under the froxels and {sky} under the closed form");
    }

    [SkippableFact]
    public void ARoofsShadowDarkensTheFogUnderIt()
    {
        Needs.Shaders();
        // Looking two degrees down at ground some 57 ahead, under a roof forty meters up, with the
        // sun high, the fog under the roof gets the sky's light alone.
        var froxels = new FroxelFogSettings { Lumps = 0f, Distance = 150f };
        var open = Picture(Red(150f, froxels), eye: 2f, ground: true, sun: true);
        var roofed = Picture(Red(150f, froxels), eye: 2f, ground: true, sun: true, roof: true);

        var row = open.Height / 2 + 9;
        var lit = open.At(open.Width / 2, row);
        var shaded = roofed.At(open.Width / 2, row);
        Assert.True(lit.R - shaded.R > 40, $"under the roof the fog reads {shaded} where open {lit}");
    }

    [SkippableFact]
    public void ALampGlowsInTheFog()
    {
        Needs.Shaders();
        var dim = new SkyFogSettings { Color = (0.2f, 0.2f, 0.2f), Brightness = 20f, Height = 1000f, Visibility = 200f };
        dim.Froxels = new FroxelFogSettings { Lumps = 0f, Distance = 60f, Lights = 0 };
        var dark = Picture(dim, eye: 2f, lamp: true);
        dim.Froxels = new FroxelFogSettings { Lumps = 0f, Distance = 60f };
        var lit = Picture(dim, eye: 2f, lamp: true);

        // A little beside the lamp, 20 ahead and a meter up, where the fog about it glows.
        var x = dark.Width / 2 + 20;
        var y = dark.Height / 2 - 13;
        Assert.True(Luma(lit.At(x, y)) - Luma(dark.At(x, y)) > 15, $"by the lamp the fog reads {lit.At(x, y)} lit and {dark.At(x, y)} not");
    }

    [SkippableFact]
    public void ABoxThickensTheFogInsideIt()
    {
        Needs.Shaders();
        var clear = Red(100_000f, new FroxelFogSettings { Lumps = 0f, Distance = 100f });
        var thin = Picture(clear, eye: 2f);
        clear.Froxels = new FroxelFogSettings
        {
            Lumps = 0f,
            Distance = 100f,
            Boxes = [new FogBox(new Vec3(0f, 6f, -30f), new Vec3(10f, 10f, 10f), Visibility: 5f, Edge: 0.5f)],
        };
        var boxed = Picture(clear, eye: 2f);

        // The box's middle, above the horizon, where the sky is otherwise clear.
        var (x, y) = (thin.Width / 2, thin.Height / 2 - 34);
        Assert.True(Redness(boxed.At(x, y)) - Redness(thin.At(x, y)) > 60, $"through the box the fog reads {boxed.At(x, y)}, beside it {thin.At(x, y)}");
    }

    [SkippableFact]
    public void TheAtmosphereLetsTheZenithsLightThroughAndReddensTheHorizons()
    {
        Needs.Shaders();
        var under = Transmittance(atmosphere: true);
        var zenith = Decode(under.At(4, 4));
        var horizon = Decode(under.At(under.Width - 4, 4));
        Assert.True(zenith.All(c => c > 0.6), $"toward the zenith {string.Join(", ", zenith)}");
        Assert.True(horizon[2] < zenith[2] / 3 && horizon[0] > horizon[2] * 2, $"toward the horizon {string.Join(", ", horizon)}");

        var bare = Transmittance(atmosphere: false);
        Assert.True(Decode(bare.At(bare.Width - 4, 4)).All(c => c > 0.98), $"with no atmosphere {bare.At(bare.Width - 4, 4)}");
    }

    [Fact]
    public void FroxelsThatReachNowhereOrNameTooManyBoxesAreRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetSkyFog(default, new SkyFogSettings { Froxels = new FroxelFogSettings { Distance = 0f } }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetSkyFog(default, new SkyFogSettings { Froxels = new FroxelFogSettings { LumpSize = 0f } }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetSkyFog(default, new SkyFogSettings
        {
            Froxels = new FroxelFogSettings { Boxes = Enumerable.Repeat(new FogBox(Vec3.Zero, Vec3.One, 1f), 9).ToArray() },
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.SetSkyFog(default, new SkyFogSettings
        {
            Froxels = new FroxelFogSettings { Boxes = [new FogBox(Vec3.Zero, Vec3.Zero, 1f)] },
        }));
    }

    private static int Redness((byte R, byte G, byte B, byte A) pixel) => pixel.R - Math.Max(pixel.G, pixel.B);

    private static double Luma((byte R, byte G, byte B, byte A) pixel) => (0.2126 * pixel.R) + (0.7152 * pixel.G) + (0.0722 * pixel.B);

    private static double[] Decode((byte R, byte G, byte B, byte A) pixel) =>
        [Decode(pixel.R / 255.0), Decode(pixel.G / 255.0), Decode(pixel.B / 255.0)];

    private static double Decode(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    // A level view from a height under a fog, with no tonemapping so a level reads as the fog left
    // it, over unlit ground or none, under a high sun with shadows, a roof forty meters up over the
    // way ahead, or a lamp twenty meters ahead.
    private static CapturedImage Picture(SkyFogSettings fog, float eye = 10f, bool ground = false, bool sun = false, bool roof = false, bool lamp = false)
    {
        var run = new PictureRun
        {
            Width = 480,
            Height = 270,
            Frames = 1500,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 60f });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, eye, 0f), new Vec3(0f, eye, -1f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1, Tonemapper = Tonemapper.None });

                if (ground)
                {
                    var floor = ecs.Spawn();
                    Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 800f, 1f, 400f));
                    Render.SetMaterial(ecs, floor, Render.CreateMaterial(new MaterialSettings { BaseColor = (0.1f, 0.45f, 0.1f, 1f), Unlit = true }));
                    ecs.Add(floor, Transform.At(0f, -0.5f, -200f));
                }

                if (sun)
                {
                    var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 12_500f, Shadows = true });
                    ecs.Add(light, Transform.LookingAt(new Vec3(0.2f, 1f, 0.3f), Vec3.Zero, Vec3.UnitY));
                }

                if (roof)
                {
                    var cover = ecs.Spawn();
                    Render.SetMesh(ecs, cover, Render.CreateMesh(MeshShape.Cuboid, 300f, 1f, 300f));
                    Render.SetMaterial(ecs, cover, Render.CreateMaterial(0.5f, 0.5f, 0.5f));
                    ecs.Add(cover, Transform.At(0f, 40f, -140f));
                }

                if (lamp)
                {
                    var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 2_000_000f, Range = 40f, Shadows = false });
                    ecs.Add(light, Transform.At(0f, eye + 1f, -20f));
                }

                Render.SetSkyFog(camera, fog);
            },
        };
        run.Wait(10)
            .Until("compiled", _ => SkyFog.All().All(f => f.Ready) && SkyFog.FroxelsReady())
            .Until("ready", _ => Render.PipelinesReady())
            .Wait(60)
            .Capture("fog")
            .Go();
        return run.Picture("fog");
    }

    // The light the atmosphere lets through from the camera toward the zenith on the left half of
    // the picture and toward the horizon on the right, drawn over it as it is.
    private static CapturedImage Transmittance(bool atmosphere)
    {
        ShaderInstance through = default;
        var run = new PictureRun
        {
            Width = 64,
            Height = 16,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 2f, 6f));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1, Tonemapper = Tonemapper.None });
                if (atmosphere) Render.SetAtmosphere(camera, new AtmosphereSettings());
                through = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
                {
                    DrawVertex = ShaderStage.Slang(Through, "vertex"),
                    DrawFragment = ShaderStage.Slang(Through, "fragment"),
                }));
                Shaders.SetViewDraws(camera, ViewDraw.Fixed(through, FramePoint.AfterTonemapping, 3, 1, DrawBlend.Opaque, writesDepth: false));
            },
        };
        run.Until("compiled", _ => through.Program.State == ShaderProgramState.Ready && Render.PipelinesReady())
            .Wait(20)
            .Capture("through")
            .Go();
        return run.Picture("through");
    }

    private const string Through = """
        import bcs_pass;

        [shader("vertex")]
        FullscreenOutput vertex(uint index : SV_VertexID)
        {
            var corner = fullscreenTriangle(index);
            corner.position.z = 1.0;
            return corner;
        }

        [shader("fragment")]
        float4 fragment(FullscreenOutput input) : SV_Target
        {
            float3 way = input.uv.x < 0.5 ? float3(0.0, 1.0, 0.0) : float3(0.0, 0.0, -1.0);
            return float4(passAtmosphereTransmittance(passView.world_position, way), 1.0);
        }
        """;
}
