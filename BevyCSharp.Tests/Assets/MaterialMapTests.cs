using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The standard material's specular tint, depth map and render method, drawn, so a setting that
/// reads back right but never reaches Bevy's shader fails here.
/// </summary>
[Collection("engine")]
public sealed class MaterialMapTests
{
    /// <summary>
    /// A black, smooth non-metal with the tint given, so a cube of it shows only its reflection.
    /// </summary>
    private static MaterialSettings Reflecting((float, float, float, float) tint, OpaqueRenderMethod drawn = OpaqueRenderMethod.Auto) => new()
    {
        BaseColor = (0f, 0f, 0f, 1f),
        Roughness = 0.4f,
        Reflectance = 1f,
        SpecularTint = tint,
        OpaqueRenderMethod = drawn,
    };

    /// <summary>A sun from behind the camera, which puts the highlight across the face it looks at.</summary>
    private static void Sun(EcsWorld ecs)
    {
        Render.SetAmbientLight((0f, 0f, 0f), 0f);
        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 2_000f, Shadows = false });
        ecs.Add(light, Transform.LookingAt(new Vec3(0f, 0f, 6f), Vec3.Zero, Vec3.UnitY));
    }

    [SkippableFact]
    public void ASpecularTintColorsTheReflection()
    {
        Needs.Renderer();

        var material = AssetHandle.None;
        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                Sun(ecs);
                material = Render.CreateMaterial(Reflecting((1f, 1f, 1f, 1f)));
                PictureRun.Cube(ecs, material, 3f);
            },
        };

        run.Wait(ShaderMaterialTests.Settled)
            .Capture("white")
            .Do("red", _ => Render.WriteMaterial(material, Reflecting((1f, 0f, 0f, 1f))))
            .Wait(10)
            .Capture("red")
            .Go();

        var white = Mean(run.Picture("white"), 0.5f);
        var red = Mean(run.Picture("red"), 0.5f);

        // Without a highlight to tint, the red one would pass as dark whatever the tint did.
        Assert.True(white.R > 20, $"the cube reflected nothing, at {white}");
        Assert.True(white.G > white.R * 0.7, $"an untinted reflection came out colored, at {white}");
        Assert.True(red.R > 20 && red.G < red.R * 0.5, $"a red tint left the reflection at {red}, from {white} untinted");
    }

    [SkippableFact]
    public void ADepthMapMovesTheTextureByEitherMethod()
    {
        Needs.Renderer();

        var material = AssetHandle.None;
        MaterialSettings Carved(float depth, ParallaxMethod method) => new()
        {
            BaseColorTexture = Checker(255, 0, 0, 0, 0, 255, srgb: true),
            DepthMap = Checker(0, 0, 0, 255, 255, 255, srgb: false),
            ParallaxDepthScale = depth,
            ParallaxMethod = method,
            ReliefSteps = 4,
            Unlit = true,
        };

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                PictureRun.Camera(ecs);
                material = Render.CreateMaterial(Carved(0f, ParallaxMethod.Occlusion));

                // With tangents, which a depth map is read along as a normal map is, and turned so
                // its faces are seen at a slant, since a face seen head on moves nothing.
                var mesh = Render.CreateMesh(MeshShape.Cuboid, 2.5f, 2.5f, 2.5f);
                Assert.True(Render.GenerateTangents(mesh));
                var cube = ecs.Spawn();
                Render.SetMesh(ecs, cube, mesh);
                Render.SetMaterial(ecs, cube, material);
                ecs.Add(cube, new Transform(Vec3.Zero, Quat.FromRotationY(0.6f) * Quat.FromRotationX(0.5f), Vec3.One));
            },
        };

        run.Wait(ShaderMaterialTests.Settled)
            .Capture("flat")
            .Do("occlusion", _ => Render.WriteMaterial(material, Carved(0.3f, ParallaxMethod.Occlusion)))
            .Wait(10)
            .Capture("occlusion")
            .Do("relief", _ => Render.WriteMaterial(material, Carved(0.3f, ParallaxMethod.Relief)))
            .Wait(10)
            .Capture("relief")
            .Go();

        var flat = run.Picture("flat");
        foreach (var carved in new[] { "occlusion", "relief" })
        {
            var moved = Differing(flat, run.Picture(carved));
            Assert.True(moved > flat.Width * flat.Height / 50, $"{carved} mapping moved {moved} pixels of the checker");
        }
    }

    [SkippableFact]
    public void AForwardMaterialKeepsItsTintWhileTheRestAreDeferred()
    {
        Needs.Renderer();

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                Render.SetDeferredRendering(true);
                var camera = PictureRun.Camera(ecs);
                Render.SetPostProcessing(camera, new PostSettings { Msaa = 1 });
                Shaders.SetPrepass(camera, depth: true, deferred: true);
                Sun(ecs);

                // The same red tint on both, which the G-buffer has no room for, so the deferred one
                // reflects white and the forward one red.
                PictureRun.Cube(ecs, Render.CreateMaterial(Reflecting((1f, 0f, 0f, 1f))), 1.6f, new Vec3(-1.2f, 0f, 0f));
                PictureRun.Cube(ecs, Render.CreateMaterial(Reflecting((1f, 0f, 0f, 1f), OpaqueRenderMethod.Forward)), 1.6f, new Vec3(1.2f, 0f, 0f));
            },
        };

        run.Wait(ShaderMaterialTests.Settled).Capture("picture").Go();

        var picture = run.Picture("picture");
        var deferred = Mean(picture, 0.5f - 1.2f / 4.3f);
        var forward = Mean(picture, 0.5f + 1.2f / 4.3f);

        Assert.True(deferred.R > 20 && forward.R > 20, $"a cube reflected nothing, the deferred one at {deferred} and the forward one at {forward}");
        Assert.True(deferred.G > deferred.R * 0.7, $"the deferred cube was tinted, at {deferred}");
        Assert.True(forward.G < forward.R * 0.5, $"the forward cube lost its tint among deferred ones, at {forward}");
    }

    /// <summary>
    /// A checker of two colors, eight squares a side, as raw pixels made into an image.
    /// </summary>
    private static AssetHandle Checker(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2, bool srgb)
    {
        const int size = 64;
        var pixels = new byte[size * size * 4];
        for (var i = 0; i < size * size; i++)
        {
            var first = (((i % size) / 8) + ((i / size) / 8)) % 2 == 0;
            (pixels[i * 4], pixels[i * 4 + 1], pixels[i * 4 + 2], pixels[i * 4 + 3]) = first ? (r1, g1, b1, (byte)255) : (r2, g2, b2, (byte)255);
        }

        return Render.CreateImage(pixels, size, size, srgb: srgb);
    }

    /// <summary>The mean color of a small square of the picture, its middle a share of the width across and halfway down.</summary>
    private static (double R, double G, double B) Mean(CapturedImage picture, float across)
    {
        var (middleX, middleY, half) = ((uint)(picture.Width * across), picture.Height / 2, picture.Width / 16);
        double r = 0, g = 0, b = 0;
        var count = 0;
        for (var y = middleY - half; y <= middleY + half; y++)
        {
            for (var x = middleX - half; x <= middleX + half; x++)
            {
                var pixel = picture.At(x, y);
                (r, g, b, count) = (r + pixel.R, g + pixel.G, b + pixel.B, count + 1);
            }
        }

        return (r / count, g / count, b / count);
    }

    /// <summary>How many pixels differ between two pictures by more than a little in any channel.</summary>
    private static int Differing(CapturedImage first, CapturedImage second)
    {
        var count = 0;
        for (var y = 0u; y < first.Height; y++)
        {
            for (var x = 0u; x < first.Width; x++)
            {
                var (a, b) = (first.At(x, y), second.At(x, y));
                if (Math.Abs(a.R - b.R) > 24 || Math.Abs(a.G - b.G) > 24 || Math.Abs(a.B - b.B) > 24) count++;
            }
        }

        return count;
    }
}
