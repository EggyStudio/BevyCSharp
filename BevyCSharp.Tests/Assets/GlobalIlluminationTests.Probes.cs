using Bevy;
using Xunit;

namespace Bevy.Tests;

public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// A closed room lit by a lamp, an irradiance volume over the back wall's left half and a
    /// reflection probe over its right half with a chrome block in it, under a camera with an
    /// environment map of its own, reads the light and the color its reference gives each half of
    /// the wall, and the chrome block's faces read their blue light at none or more.
    /// </summary>
    /// <remarks>
    /// The pass took back the camera's environment map's light alone. Bevy's lighting gave the
    /// volume's light where the volume holds a surface and no map's diffuse light there, and the
    /// reflection probe's light, diffuse and reflected, in place of the map's where the probe holds
    /// a surface. So the volume's half took the volume's light and lost the blue map's, and the
    /// probe's half kept the probe's warm light beside the probes', and the chrome lost the blue
    /// map's reflection it never had, as the feature test's light hall's probe's bay read 35 to 58%
    /// over its reference and its volume's bay crimson. The room is closed, so the reference's
    /// light is the lamp's and what bounces from it.
    /// </remarks>
    [SkippableFact]
    public void TheLightBevysLightProbesGaveIsTakenBackAsTheyGaveIt()
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
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 1.5f, 3f));
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1.5f, 3f), new Vec3(0f, 1.2f, -4f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight((1f, 1f, 1f), 0f);
                var blue = Cube(0.2f, 0.4f, 1f);
                Render.SetEnvironmentMap(camera, blue, blue, intensity: 1500f);

                var white = Render.CreateMaterial(0.8f, 0.8f, 0.8f, roughness: 0.9f);
                void Block(string name, Vec3 at, Vec3 size, AssetHandle material)
                {
                    var block = ecs.Spawn();
                    Render.SetMesh(ecs, block, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
                    Render.SetMaterial(ecs, block, material);
                    ecs.Add(block, Transform.At(at.X, at.Y, at.Z));
                    ecs.SetName(block, name);
                }

                // A room eight units across and three high, closed on every side by walls thicker
                // than the field's finest cells, its back wall in two halves.
                Block("room", new Vec3(0f, -0.2f, 0f), new Vec3(8.8f, 0.4f, 8.8f), white);
                Block("room", new Vec3(0f, 3.2f, 0f), new Vec3(8.8f, 0.4f, 8.8f), white);
                Block("room", new Vec3(-4.2f, 1.5f, 0f), new Vec3(0.4f, 3f, 8f), white);
                Block("room", new Vec3(4.2f, 1.5f, 0f), new Vec3(0.4f, 3f, 8f), white);
                Block("room", new Vec3(0f, 1.5f, 4.2f), new Vec3(8f, 3f, 0.4f), white);
                Block("volume wall", new Vec3(-2f, 1.5f, -4.2f), new Vec3(4f, 3f, 0.4f), white);
                Block("probe wall", new Vec3(2f, 1.5f, -4.2f), new Vec3(4f, 3f, 0.4f), white);
                Block("chrome", new Vec3(2f, 0.5f, -2.5f), new Vec3(1f, 1f, 1f),
                    Render.CreateMaterial(new MaterialSettings { BaseColor = (0.95f, 0.95f, 0.95f, 1f), Metallic = 1f, Roughness = 0.1f }));

                var lamp = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 200_000f, Range = 14f, Radius = 0.2f, Shadows = false });
                ecs.Add(lamp, Transform.At(0f, 2.6f, 0f));

                // Each over a back quarter of the room and past its walls, since Bevy's light probe
                // gives a surface on its box's side none of its light, the volume magenta from
                // every side and the probe warm in every direction.
                var volume = ecs.Spawn();
                ecs.Add(volume, new Transform(new Vec3(-2.15f, 1.5f, -2.65f), Quat.Identity, new Vec3(4.3f, 3.4f, 3.3f)));
                Render.SetIrradianceVolume(volume, Volume(0.9f, 0.2f, 0.7f), intensity: 600f);
                var probe = ecs.Spawn();
                ecs.Add(probe, new Transform(new Vec3(2.15f, 1.5f, -2.65f), Quat.Identity, new Vec3(4.3f, 3.4f, 3.3f)));
                var warm = Cube(0.9f, 0.5f, 0.2f);
                Render.SetReflectionProbe(probe, warm, warm, intensity: 1500f);

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
                    Assert.Null(GiReference.BeginReference(ecs, null, 256, gloss: GiReference.Gloss.FirstFace));
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

        // Each region's light summed, red, green and blue, and its pixels.
        (double[] Shown, double[] Wanted, int Pixels) Region(uint key)
        {
            var (shown, wanted, pixels) = (new double[3], new double[3], 0);
            for (var i = 0; i < traced.Regions.Length; i++)
            {
                if (traced.Regions[i] != key) continue;
                pixels++;
                for (var c = 0; c < 3; c++)
                {
                    shown[c] += picture[i * 3 + c];
                    wanted[c] += traced.Light[i * 3 + c];
                }
            }
            return (shown, wanted, pixels);
        }

        // Every reading said before any is held, so a failure shows them all.
        var faults = new List<string>();
        foreach (var half in new[] { "volume wall +z", "probe wall +z" })
        {
            var (shown, wanted, _) = Region(traced.Names.Single(n => n.Value == half).Key);
            var light = shown.Sum() / wanted.Sum() - 1;
            var balance = shown[2] / shown[0] / (wanted[2] / wanted[0]) - 1;
            output.WriteLine($"the {half} reads {light:+0.000;-0.000} of its reference's light and its blue over its red {balance:+0.000;-0.000} of the reference's, {shown[0]:0.0} {shown[1]:0.0} {shown[2]:0.0} against {wanted[0]:0.0} {wanted[1]:0.0} {wanted[2]:0.0}");
            if (Math.Abs(light) >= 0.3) faults.Add($"the {half} reads {light:+0.000;-0.000} of its reference's light");
            if (Math.Abs(balance) >= 0.15) faults.Add($"the {half}'s blue over its red reads {balance:+0.000;-0.000} of its reference's");
        }

        var faces = traced.Names.Where(n => n.Value.StartsWith("chrome ", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(faces);
        foreach (var face in faces)
        {
            var (shown, _, pixels) = Region(face.Key);
            output.WriteLine($"the {face.Value} reflects {shown[2] / pixels:0.000} blue a pixel");
            if (shown[2] / pixels < 0) faults.Add($"the {face.Value} reads {shown[2] / pixels:0.000} blue a pixel");
        }

        Assert.True(faults.Count == 0, string.Join("; ", faults));
    }

    /// <summary>
    /// A cube of one color every way, four pixels a side, a column of its six faces.
    /// </summary>
    private static AssetHandle Cube(float r, float g, float b)
    {
        var pixels = new byte[4 * 4 * 6 * 4];
        for (var at = 0; at < pixels.Length; at += 4)
            (pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3]) = ((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), 255);
        var image = Render.CreateImage(pixels, 4, 24, srgb: false);
        Render.MakeCubemap(image);
        return image;
    }

    /// <summary>
    /// An irradiance volume of one point, which lights a surface facing any way with one color.
    /// </summary>
    private static AssetHandle Volume(float r, float g, float b)
    {
        var pixels = new byte[6 * 4];
        for (var at = 0; at < pixels.Length; at += 4)
            (pixels[at], pixels[at + 1], pixels[at + 2], pixels[at + 3]) = ((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), 255);
        var image = Render.CreateImage(pixels, 1, 6, srgb: false);
        Render.MakeVolume(image, 3);
        return image;
    }
}
