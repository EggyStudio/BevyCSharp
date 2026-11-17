using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GiReferenceTests
{
    /// <summary>
    /// A floor painted by its textures, white and full metal by its factors with its color and its
    /// metal in its maps, and a patch emitting by its emissive texture read in the reference as the
    /// picture draws them, under a sun 60 degrees up, the direct light alone.
    /// </summary>
    /// <remarks>
    /// A scene made to be drawn, as the scene packs are, holds its colors in its textures and
    /// leaves its factors white and its metal at one for its maps to say, so a reference of the
    /// factors alone reads such a floor as a white mirror and such an emission as white. The material
    /// buffer gives a ray's hit each texture's mean where no mask holds it, a color's in linear
    /// light by its image's sRGB flag, and a texture of one texel over and over reads its mean
    /// wherever a path meets it.
    /// </remarks>
    [SkippableFact]
    public void ATexturedFloorAndAPatchEmittingByItsTextureReadInTheReferenceAsThePictureDrawsThem()
    {
        Needs.Shaders();
        var supported = false;
        string? traced = null, read = null;
        var (askedTrace, askedRead) = (false, false);
        static AssetHandle Flat(byte r, byte g, byte b, bool srgb) =>
            Render.CreateImage([r, g, b, 255, r, g, b, 255, r, g, b, 255, r, g, b, 255], 2, 2, srgb, mipmaps: false);
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
                var floor = PictureRun.Cube(ecs, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColorTexture = Flat(128, 128, 128, srgb: true),
                    Metallic = 1f,
                    Roughness = 1f,
                    MetallicRoughnessTexture = Flat(0, 200, 0, srgb: false),
                }), 40f, new Vec3(0f, -20f, 0f));
                ecs.SetName(floor, "floor");

                var patch = ecs.Spawn();
                Render.SetMesh(ecs, patch, Render.CreateMesh(MeshShape.Plane, 2f, 2f));
                Render.SetMaterial(ecs, patch, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0f, 0f, 0f, 1f),
                    Emissive = (20f, 20f, 20f, 0f),
                    EmissiveTexture = Flat(255, 128, 0, srgb: true),
                }));
                ecs.Add(patch, Transform.At(-1.5f, 0.01f, 0f));
                ecs.SetName(patch, "patch");

                var elevation = 60f * MathF.PI / 180f;
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
                ecs.Add(sun, Transform.LookingAt(new Vec3(MathF.Cos(elevation), MathF.Sin(elevation), 0f), Vec3.Zero, Vec3.UnitY));
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("ready", _ => !supported || Render.PipelinesReady())
            .Wait(3)
            .Until("traced", world => !supported || Read(world.Resource<EcsWorld>(), ref askedTrace, ref traced, ecs => GiReference.BeginReference(ecs, null, 16, bounces: 0)))
            .Until("read", world => !supported || Read(world.Resource<EcsWorld>(), ref askedRead, ref read, ecs => GiReference.BeginPicture(ecs, null)))
            .Go();
        Needs.RayQueries(supported);

        var reference = GiReference.Last!;
        var picture = GiReference.LastPicture!.Value.Light;
        foreach (var (name, channels) in new[] { ("floor", 3), ("patch", 2) })
        {
            var (shown, wanted, pixels) = (new double[3], new double[3], 0);
            for (var y = 1; y < (int)Height - 1; y++)
            for (var x = 1; x < (int)Width - 1; x++)
            {
                // A pixel whose four neighbors show what it shows, since a reference's pixel at an
                // edge takes its paths across both sides.
                var i = y * (int)Width + x;
                if (!Shows(reference, i, name) || !Shows(reference, i - 1, name) || !Shows(reference, i + 1, name)
                    || !Shows(reference, i - (int)Width, name) || !Shows(reference, i + (int)Width, name))
                    continue;
                pixels++;
                for (var c = 0; c < 3; c++)
                    (shown[c], wanted[c]) = (shown[c] + picture[i * 3 + c], wanted[c] + reference.Light[i * 3 + c]);
            }
            output.WriteLine($"{name}, {pixels} pixels: the picture {shown[0] / pixels:0.000}, {shown[1] / pixels:0.000}, {shown[2] / pixels:0.000}, the reference {wanted[0] / shown[0]:0.000}, {wanted[1] / shown[1]:0.000} and {wanted[2] / shown[2]:0.000} of it");
            Assert.True(pixels > 100, $"the {name} shows whole in {pixels} pixels");
            for (var c = 0; c < channels; c++)
                Assert.True(Math.Abs(wanted[c] / shown[c] - 1) < 0.08, $"the reference's {name} reads {wanted[0] / shown[0]:0.000}, {wanted[1] / shown[1]:0.000} and {wanted[2] / shown[2]:0.000} of the picture's, red, green and blue");
        }
    }

    /// <summary>
    /// A reference of a wall of full metal holds every pixel a number, a path's draw of its gloss
    /// at the wall landing on the largest of the hash's values.
    /// </summary>
    /// <remarks>
    /// A path at a face chooses its gloss where its draw falls under the gloss's weight, which is 1
    /// at full metal, and a hash within 128 of 2^32 rounds to 1 as a float, one draw in 2^25, so a
    /// path whose draw is that 1 takes the diffuse share the face does not have, 0 over 0, and its
    /// pixel reads not a number, unless the draw is held under 1. The weight is a share over
    /// itself, which a GPU dividing by a reciprocal makes exactly 1 at some faces and a little
    /// under it at others, where the draw of 1 takes the diffuse share at a weight of nothing, so
    /// the test traces the first eight seeds whose paths draw that 1 at the wall, each at a pixel
    /// of its own and so a weight of its own, found by the reference's own draws for a pixel's
    /// paths mirrored here, the fourth a path's choice at its first face.
    /// </remarks>
    [SkippableFact]
    public void AReferenceOfAWallOfFullMetalHoldsEveryPixelANumber()
    {
        Needs.Shaders();
        const int Paths = 64, Seeds = 8;
        var drawing = SeedsDrawingOne((int)(Width * Height), Paths, Seeds);
        foreach (var (seed, pixel, path) in drawing)
            output.WriteLine($"seed {seed} draws 1 at pixel {pixel}'s path {path}");
        var supported = false;
        var unnumbered = new List<string>();
        var run = new PictureRun
        {
            Width = Width,
            Height = Height,
            Frames = 1800,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 0f, 6f));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                PictureRun.Cube(ecs, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (0.9f, 0.9f, 0.9f, 1f),
                    Metallic = 1f,
                    Roughness = 0.5f,
                }), 40f, new Vec3(0f, 0f, -20f));
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
                ecs.Add(sun, Transform.LookingAt(new Vec3(0.3f, 0.4f, 1f), Vec3.Zero, Vec3.UnitY));
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("ready", _ => !supported || Render.PipelinesReady());
        foreach (var (seed, _, _) in drawing)
        {
            var (asked, traced) = (false, (string?)null);
            run = run.Until($"traced from seed {seed}", world =>
            {
                if (!supported) return true;
                if (!Read(world.Resource<EcsWorld>(), ref asked, ref traced, ecs => GiReference.BeginReference(ecs, null, Paths, bounces: 1, seed: (int)seed))) return false;
                var light = GiReference.Last!.Light;
                var count = Enumerable.Range(0, light.Length / 3).Count(i => !float.IsFinite(light[i * 3]) || !float.IsFinite(light[i * 3 + 1]) || !float.IsFinite(light[i * 3 + 2]));
                if (count > 0) unnumbered.Add($"{count} from seed {seed}");
                return true;
            });
        }
        run.Go();
        Needs.RayQueries(supported);

        Assert.True(unnumbered.Count == 0, $"pixels that are not a number, {string.Join(", ", unnumbered)}");
    }

    // Whether a reference's pixel shows the named entity, facing either way.
    private static bool Shows(GiReference.Traced traced, int pixel, string name) =>
        traced.Names.TryGetValue(traced.Regions[pixel], out var shown) && shown.StartsWith(name + " ", StringComparison.Ordinal);

    // The first seeds of the reference's whose paths draw 1 as a float at a path's choice of its
    // gloss at its first face, as many as asked, each with the first pixel and path that draws it,
    // as gi_reference.slang starts a pixel's path's draws and makes them.
    private static List<(uint Seed, int Pixel, int Path)> SeedsDrawingOne(int pixels, int paths, int count)
    {
        // A draw moves the state to state * a + c, so the fourth's state is the first's times a^4
        // plus c times 1 + a + a^2 + a^3, the first draw moving the state on, the next two placing
        // the path in its pixel and the fourth choosing.
        const uint A = 747796405u, C = 2891336453u;
        var (a4, c4) = unchecked((A * A * A * A, C * (1u + A + A * A + A * A * A)));
        var found = new List<(uint, int, int)>();
        for (var seed = 0u; found.Count < count; seed++)
        {
            Assert.True(seed < 1_000_000u, "no seeds below a million draw a 1");
            var drawn = false;
            for (var pixel = 0; pixel < pixels && !drawn; pixel++)
            for (var path = 0; path < paths && !drawn; path++)
            {
                var state = unchecked(((uint)pixel * 9781u + (uint)path * 6271u + seed * 7919u + 1u) * a4 + c4);
                if (Hashed(state) < 1f) continue;
                drawn = true;
                found.Add((seed, pixel, path));
            }
        }
        return found;
    }

    // A draw from the state it moved to, as the reference's random() hashes it.
    private static float Hashed(uint state)
    {
        unchecked
        {
            var packed = ((state >> (int)((state >> 28) + 4)) ^ state) * 277803737u;
            return (float)((packed >> 22) ^ packed) / 4294967296f;
        }
    }
}
