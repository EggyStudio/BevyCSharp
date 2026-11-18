using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GiReferenceTests
{
    /// <summary>
    /// A blended decal of half alpha lying a centimeter over a gray floor, as a scene's dirt decals
    /// lie, reads in the reference as the picture blends it over the floor, under a sun 60 degrees
    /// up that casts shadows, the direct light alone.
    /// </summary>
    /// <remarks>
    /// Bevy draws a blended face over what lies behind it by its alpha, and its shadow map's depth
    /// bias leaves the floor a decal lies on lit. Traced whole, the decal reads its own red where
    /// the picture shows the floor through it, and with its paths passing it by its alpha alone
    /// the floor beneath reads in the decal's shadow.
    /// </remarks>
    [SkippableFact]
    public void ABlendedDecalOnAFloorReadsInTheReferenceAsThePictureBlendsIt()
    {
        var (shown, wanted, pixels) = ReadDecal(() => new MaterialSettings { BaseColor = (0.8f, 0.1f, 0.1f, 0.5f), Roughness = 1f, AlphaMode = AlphaMode.Blend }, _ => { });
        Assert.True(pixels > 100, $"the decal shows whole in {pixels} pixels");
        for (var c = 0; c < 3; c++)
            Assert.True(Math.Abs(wanted[c] / shown[c] - 1) < 0.08, $"the reference's decal reads {wanted[0] / shown[0]:0.000}, {wanted[1] / shown[1]:0.000} and {wanted[2] / shown[2]:0.000} of the picture's, red, green and blue");
    }

    /// <summary>
    /// A blended decal whose texture's alpha lays it on half its square in a checker, drawn after
    /// 24 cut-out cards of two textures each, more than the material buffer's masks hold, reads in
    /// the reference as the picture blends it over its square, its texture read where a path meets
    /// it.
    /// </summary>
    /// <remarks>
    /// The cards' base color textures and the decal's say where each lies and take the masks
    /// first, so the cards' surfaces past the masks' room are taken at their means. Masked in the
    /// order of the slots, the cards' surfaces would take the room before the decal and its
    /// texture be taken at its mean, the decal laid whole over its square.
    /// </remarks>
    [SkippableFact]
    public void ABlendedDecalAfterMoreTexturesThanTheMasksHoldKeepsItsShapeInTheReference()
    {
        static AssetHandle Flat(byte r, byte g, byte b, byte a, bool srgb) =>
            Render.CreateImage([r, g, b, a, r, g, b, a, r, g, b, a, r, g, b, a], 2, 2, srgb, mipmaps: false);
        var checker = new byte[16 * 16 * 4];
        for (var i = 0; i < 16 * 16; i++)
            (checker[i * 4], checker[i * 4 + 1], checker[i * 4 + 2], checker[i * 4 + 3]) = (230, 90, 90, (byte)(((i % 16 / 4) + (i / 16 / 4)) % 2 * 255));
        var (shown, wanted, pixels) = ReadDecal(
            () => new MaterialSettings { BaseColorTexture = Render.CreateImage(checker, 16, 16, true, mipmaps: false), Roughness = 1f, AlphaMode = AlphaMode.Blend },
            ecs =>
            {
                for (var card = 0; card < 24; card++)
                {
                    var spawned = ecs.Spawn();
                    Render.SetMesh(ecs, spawned, Render.CreateMesh(MeshShape.Plane, 1f, 1f));
                    Render.SetMaterial(ecs, spawned, Render.CreateMaterial(new MaterialSettings
                    {
                        BaseColorTexture = Flat((byte)(card * 10), 128, 64, 255, srgb: true),
                        MetallicRoughnessTexture = Flat(0, (byte)(card * 10), 0, 255, srgb: false),
                        AlphaMode = AlphaMode.Mask,
                    }));
                    ecs.Add(spawned, Transform.At(card * 1.5f - 18f, -60f, 0f));
                }
            },
            square: true);
        Assert.True(pixels > 100, $"the decal shows whole in {pixels} pixels");
        for (var c = 0; c < 3; c++)
            Assert.True(Math.Abs(wanted[c] / shown[c] - 1) < 0.08, $"the reference's decal reads {wanted[0] / shown[0]:0.000}, {wanted[1] / shown[1]:0.000} and {wanted[2] / shown[2]:0.000} of the picture's, red, green and blue");
    }

    /// <summary>
    /// A blended decal of half alpha lying a centimeter over a gray floor under an even sky and no
    /// sun reads in the reference half the light it reads drawn opaque and half the bare floor's
    /// at the same pixels, the light past the first face with it.
    /// </summary>
    /// <remarks>
    /// The floor's own paths meet the decal lying on it from behind, where Bevy culls the decal's
    /// faces, and pass it, so the floor beneath takes the sky as the bare floor does. Met by its
    /// alpha from behind as well, half the paths leaving the floor beneath would stop at the
    /// decal's unlit back, and the floor there would read well under the bare floor's light.
    /// </remarks>
    [SkippableFact]
    public void ABlendedDecalUnderASkyReadsInTheReferenceAsItsShareOfItselfAndOfTheFloorBeneath()
    {
        Needs.Shaders();
        var supported = false;
        var (decal, opaque) = (Entity.None, AssetHandle.None);
        var traced = new GiReference.Traced?[3];
        var (asked, said) = (new bool[3], new string?[3]);
        new PictureRun
        {
            Width = Width,
            Height = Height,
            Frames = 1200,
            Scene = ecs =>
            {
                var eye = new Vec3(0f, 5f, 0f);
                var camera = PictureRun.Camera(ecs, eye);
                ecs.Set(camera, Transform.LookingAt(eye, Vec3.Zero, -Vec3.UnitZ));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 500f);
                var floor = PictureRun.Cube(ecs, Render.CreateMaterial(0.5f, 0.5f, 0.5f, roughness: 1f), 40f, new Vec3(0f, -20f, 0f));
                ecs.SetName(floor, "floor");

                decal = ecs.Spawn();
                Render.SetMesh(ecs, decal, Render.CreateMesh(MeshShape.Plane, 2f, 2f));
                Render.SetMaterial(ecs, decal, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0.8f, 0.1f, 0.1f, 0.5f),
                    Roughness = 1f,
                    AlphaMode = AlphaMode.Blend,
                }));
                ecs.Add(decal, Transform.At(0f, 0.01f, 0f));
                ecs.SetName(decal, "decal");
                opaque = Render.CreateMaterial(0.8f, 0.1f, 0.1f, roughness: 1f);
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("ready", _ => !supported || Render.PipelinesReady())
            .Wait(3)
            .Until("blended", world => !supported || Read(world.Resource<EcsWorld>(), ref asked[0], ref said[0], ecs => GiReference.BeginReference(ecs, null, 64, bounces: 2)))
            .Do("drawing it opaque", world =>
            {
                traced[0] = GiReference.Last;
                if (supported) Render.SetMaterial(world.Resource<EcsWorld>(), decal, opaque);
            })
            .Wait(3)
            .Until("opaque", world => !supported || Read(world.Resource<EcsWorld>(), ref asked[1], ref said[1], ecs => GiReference.BeginReference(ecs, null, 64, bounces: 2)))
            .Do("taking it under the floor", world =>
            {
                traced[1] = GiReference.Last;
                if (supported) world.Resource<EcsWorld>().Set(decal, Transform.At(0f, -100f, 0f));
            })
            .Wait(3)
            .Until("bare", world => !supported || Read(world.Resource<EcsWorld>(), ref asked[2], ref said[2], ecs => GiReference.BeginReference(ecs, null, 64, bounces: 2)))
            .Do("keeping the bare floor", _ => traced[2] = GiReference.Last)
            .Go();
        Needs.RayQueries(supported);

        var (blended, drawn, bare) = (traced[0]!, traced[1]!, traced[2]!);
        var (read, wanted, pixels) = (new double[3], new double[3], 0);
        for (var y = 1; y < (int)Height - 1; y++)
        for (var x = 1; x < (int)Width - 1; x++)
        {
            var i = y * (int)Width + x;
            if (!Shows(blended, i, "decal") || !Shows(blended, i - 1, "decal") || !Shows(blended, i + 1, "decal")
                || !Shows(blended, i - (int)Width, "decal") || !Shows(blended, i + (int)Width, "decal"))
                continue;
            pixels++;
            for (var c = 0; c < 3; c++)
                (read[c], wanted[c]) = (read[c] + blended.Light[i * 3 + c], wanted[c] + 0.5 * (drawn.Light[i * 3 + c] + bare.Light[i * 3 + c]));
        }
        output.WriteLine($"decal, {pixels} pixels: half the opaque decal's light and half the bare floor's {wanted[0] / pixels:0.000}, {wanted[1] / pixels:0.000}, {wanted[2] / pixels:0.000}, the blended decal {read[0] / wanted[0]:0.000}, {read[1] / wanted[1]:0.000} and {read[2] / wanted[2]:0.000} of it");
        Assert.True(pixels > 100, $"the decal shows whole in {pixels} pixels");
        for (var c = 0; c < 3; c++)
            Assert.True(Math.Abs(read[c] / wanted[c] - 1) < 0.08, $"the blended decal reads {read[0] / wanted[0]:0.000}, {read[1] / wanted[1]:0.000} and {read[2] / wanted[2]:0.000} of half its opaque light and half the bare floor's, red, green and blue");
    }

    // A decal of the material made lying a centimeter over a gray floor under a sun 60 degrees up
    // that casts shadows, after what the scene adds before it, read in the reference's direct light
    // alone and in the picture over the pixels whose four neighbors show the decal too, since a
    // reference's pixel at an edge takes its paths across both sides, or over its square, the
    // pixels within the box around those the decal shows in but its edge, where its texture lays
    // it on part of the square and the floor shows in the rest.
    private (double[] Shown, double[] Wanted, int Pixels) ReadDecal(Func<MaterialSettings> material, Action<EcsWorld> before, bool square = false)
    {
        Needs.Shaders();
        var supported = false;
        string? traced = null, read = null;
        var (askedTrace, askedRead) = (false, false);
        new PictureRun
        {
            Width = Width,
            Height = Height,
            Frames = 900,
            Scene = ecs =>
            {
                var eye = new Vec3(0f, 5f, 0f);
                var camera = PictureRun.Camera(ecs, eye);
                ecs.Set(camera, Transform.LookingAt(eye, Vec3.Zero, -Vec3.UnitZ));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                var floor = PictureRun.Cube(ecs, Render.CreateMaterial(0.5f, 0.5f, 0.5f, roughness: 1f), 40f, new Vec3(0f, -20f, 0f));
                ecs.SetName(floor, "floor");
                before(ecs);

                var decal = ecs.Spawn();
                Render.SetMesh(ecs, decal, Render.CreateMesh(MeshShape.Plane, 2f, 2f));
                Render.SetMaterial(ecs, decal, Render.CreateMaterial(material()));
                ecs.Add(decal, Transform.At(0f, 0.01f, 0f));
                ecs.SetName(decal, "decal");

                var elevation = 60f * MathF.PI / 180f;
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(new Vec3(MathF.Cos(elevation), MathF.Sin(elevation), 0f), Vec3.Zero, Vec3.UnitY));
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("ready", _ => !supported || Render.PipelinesReady())
            .Wait(3)
            .Until("traced", world => !supported || Read(world.Resource<EcsWorld>(), ref askedTrace, ref traced, ecs => GiReference.BeginReference(ecs, null, 32, bounces: 0)))
            .Until("read", world => !supported || Read(world.Resource<EcsWorld>(), ref askedRead, ref read, ecs => GiReference.BeginPicture(ecs, null)))
            .Go();
        Needs.RayQueries(supported);

        var reference = GiReference.Last!;
        var picture = GiReference.LastPicture!.Value.Light;
        var (shown, wanted, pixels) = (new double[3], new double[3], 0);
        var shows = Enumerable.Range(0, (int)(Width * Height)).Where(i => Shows(reference, i, "decal")).ToList();
        var (left, right) = (shows.Min(i => i % (int)Width) + 1, shows.Max(i => i % (int)Width) - 1);
        var (top, bottom) = (shows.Min(i => i / (int)Width) + 1, shows.Max(i => i / (int)Width) - 1);
        for (var y = 1; y < (int)Height - 1; y++)
        for (var x = 1; x < (int)Width - 1; x++)
        {
            var i = y * (int)Width + x;
            if (square ? x < left || x > right || y < top || y > bottom
                : !Shows(reference, i, "decal") || !Shows(reference, i - 1, "decal") || !Shows(reference, i + 1, "decal")
                  || !Shows(reference, i - (int)Width, "decal") || !Shows(reference, i + (int)Width, "decal"))
                continue;
            pixels++;
            for (var c = 0; c < 3; c++)
                (shown[c], wanted[c]) = (shown[c] + picture[i * 3 + c], wanted[c] + reference.Light[i * 3 + c]);
        }
        output.WriteLine($"decal, {pixels} pixels: the picture {shown[0] / pixels:0.000}, {shown[1] / pixels:0.000}, {shown[2] / pixels:0.000}, the reference {wanted[0] / shown[0]:0.000}, {wanted[1] / shown[1]:0.000} and {wanted[2] / shown[2]:0.000} of it");
        return (shown, wanted, pixels);
    }
}
