using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// 3DEngine's bounce rooms at each quality of global illumination against references path-traced
/// through the GPU's rays (<c>gi_reference.slang</c>), each view's picture held within its measured
/// distance from its reference, and a lit room's floor within the bound of its own.
/// </summary>
/// <remarks>
/// <para>
/// A reference is the view's light traced with 4096 paths a pixel at 160 by 90, averaged to 80 by
/// 45, in the units of the picture before tonemapping, kept under <c>references/bounce</c>; setting
/// <c>BCS_WRITE_REFERENCES=1</c> traces them again. The picture's light is averaged the same, and a
/// view's distance is its pixels' differences summed over the reference's light, so one region's
/// excess does not hide another's shortfall. The tests run where the GPU traces rays, as the
/// references are traced through them and the bounds were measured on such a GPU, the development
/// machine's RTX 4070 Laptop GPU.
/// </para>
/// <para>
/// The rooms are 3DEngine's, its lights given in Bevy's units as the light a white surface facing
/// one returns times the camera's exposure, so the pictures hold the light 3DEngine's do, and its
/// colors taken from sRGB to linear. Two views are lit by emitters alone, the strip's and the
/// glowing blocks', whose light global illumination carries as glow lights (<c>GlowLights</c>).
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class BounceRoomsTests
{
    private const uint Width = 160, Height = 90;
    private const int Down = 2;

    /// <summary>
    /// Each view at each quality, bound 15% over what it read on the development machine's GPU, as
    /// 3DEngine bounds its own, so a mend that widens no view keeps every bound and one that narrows
    /// a view is held to it by measuring again.
    /// </summary>
    /// <remarks>
    /// The views read 0.10 to 0.11 of their light from their references in the Cornell box, 0.12 in
    /// the thin room, 0.25 in the corridor, 0.11 to 0.12 in the window's, 0.02 among the red walls,
    /// 0.18 to 0.19 in the strip's, 0.05 to 0.07 on the grazing floor, 0.06 in the carried lamp's and
    /// 0.05 around the glowing blocks, where 3DEngine's read 0.12 to 0.14, 0.09, 0.20 to 0.24, 0.27 to
    /// 0.30, 0.02, 0.22, 0.11 to 0.12, 0.11 to 0.12 and 0.05 to 0.06. Before the emitters' light was
    /// carried, the Cornell box read 0.11, the strip's 1.00 and the blocks' 0.30. The strip is thinner
    /// than a pixel at 160 by 90, so the picture shows none of it where the reference's paths across
    /// each pixel meet it.
    /// </remarks>
    [SkippableTheory]
    [InlineData(1, GlobalIllumination.Low, 0.12)]
    [InlineData(1, GlobalIllumination.Medium, 0.12)]
    [InlineData(1, GlobalIllumination.High, 0.13)]
    [InlineData(2, GlobalIllumination.Low, 0.14)]
    [InlineData(2, GlobalIllumination.Medium, 0.14)]
    [InlineData(2, GlobalIllumination.High, 0.14)]
    [InlineData(3, GlobalIllumination.Low, 0.29)]
    [InlineData(3, GlobalIllumination.Medium, 0.30)]
    [InlineData(3, GlobalIllumination.High, 0.30)]
    [InlineData(4, GlobalIllumination.Low, 0.14)]
    [InlineData(4, GlobalIllumination.Medium, 0.14)]
    [InlineData(4, GlobalIllumination.High, 0.14)]
    [InlineData(5, GlobalIllumination.Low, 0.03)]
    [InlineData(5, GlobalIllumination.Medium, 0.03)]
    [InlineData(5, GlobalIllumination.High, 0.03)]
    [InlineData(6, GlobalIllumination.Low, 0.21)]
    [InlineData(6, GlobalIllumination.Medium, 0.21)]
    [InlineData(6, GlobalIllumination.High, 0.22)]
    [InlineData(7, GlobalIllumination.Low, 0.06)]
    [InlineData(7, GlobalIllumination.Medium, 0.08)]
    [InlineData(7, GlobalIllumination.High, 0.09)]
    [InlineData(8, GlobalIllumination.Low, 0.08)]
    [InlineData(8, GlobalIllumination.Medium, 0.08)]
    [InlineData(8, GlobalIllumination.High, 0.07)]
    [InlineData(9, GlobalIllumination.Low, 0.06)]
    [InlineData(9, GlobalIllumination.Medium, 0.06)]
    [InlineData(9, GlobalIllumination.High, 0.06)]
    public void EachRoomLiesWithinItsMeasuredDistanceFromItsReference(int view, GlobalIllumination quality, double bound)
    {
        Needs.Shaders();
        var path = Path.Combine(References(), $"view-{view}.pfm");
        var trace = !File.Exists(path) || Environment.GetEnvironmentVariable("BCS_WRITE_REFERENCES") == "1";
        var (supported, picture) = Measure(view, quality, trace ? 4096 : 0);
        Needs.RayQueries(supported);

        if (trace)
        {
            var traced = GiReference.Last!;
            Directory.CreateDirectory(References());
            GiReference.WritePfm(path, GiReference.Averaged(traced.Light, traced.Width, traced.Height, Down), traced.Width / Down, traced.Height / Down);
        }

        var (_, _, reference) = GiReference.ReadPfm(path);
        var distance = GiReference.Distance(GiReference.Averaged(picture, (int)Width, (int)Height, Down), reference);
        Assert.True(distance < bound, $"view {view} at {quality} lies {distance:0.000} of its light from its reference, bound {bound}");
    }

    /// <summary>
    /// The Cornell box's floor, lit by the lamp under its ceiling and the light its walls send back,
    /// reads at <see cref="GlobalIllumination.High"/> within 16% of a reference of 1024 paths a pixel
    /// traced in the same run, its light summed over the floor's pixels, where it read 13.2% over on
    /// the development machine's GPU. With the lamp panel dark in both it reads 15.5% over, so the
    /// excess is the probes' indirect light, and the panel's light, carried as a glow light, is about
    /// what the reference gives; before that light was carried the floor read 2.5% under, the excess
    /// less the panel's missing light.
    /// </summary>
    [SkippableFact]
    public void ALitRoomsFloorLiesWithinTheBoundOfItsReference()
    {
        Needs.Shaders();
        var (supported, picture) = Measure(1, GlobalIllumination.High, 1024);
        Needs.RayQueries(supported);

        var traced = GiReference.Last!;
        var floor = traced.Names.Single(n => n.Value == "cornell floor +y").Key;
        double seen = 0, wanted = 0;
        for (var i = 0; i < traced.Regions.Length; i++)
        {
            if (traced.Regions[i] != floor) continue;
            for (var c = 0; c < 3; c++)
            {
                seen += picture[i * 3 + c];
                wanted += traced.Light[i * 3 + c];
            }
        }
        var off = seen / wanted - 1;
        Assert.True(Math.Abs(off) < 0.16, $"the floor reads {off:+0.000;-0.000} of its reference's light");
    }

    /// <summary>Where the references are kept.</summary>
    private static string References() =>
        Path.Combine(CheatsheetTests.RepositoryRoot(), "BevyCSharp.Tests", "references", "bounce");

    /// <summary>
    /// The light of a view at a quality once the field has settled and the probes have traced for a
    /// while, three floats a pixel before tonemapping, with whether the GPU traces rays, and a
    /// reference of <paramref name="samples"/> paths a pixel traced first where any are asked for.
    /// </summary>
    private static (bool Supported, float[] Picture) Measure(int view, GlobalIllumination quality, int samples)
    {
        var supported = false;
        var (begun, read) = (false, false);
        var ready = 0;
        new PictureRun
        {
            Width = Width,
            Height = Height,
            Frames = 6000,
            Scene = ecs =>
            {
                Rooms.Build(ecs, view);
                Render.SetSceneField(3, 0.15f, 2);
                Render.SetGlobalIllumination(quality);
            },
        }
            .Do("asking for rays", _ => supported = Shaders.SupportsRayQueries)
            .Until("settled", _ => !supported || SceneField.Plan is { } plan && plan.Shapes.Count == 0
                && Enumerable.Range(0, plan.Cascades).All(c => plan.BuiltOrigin(c) is not null))
            .Until("traced", _ => !supported || WorldProbes.TracedFrames >= 90)
            // The field's draws on the camera, and then every pipeline ready for three frames running,
            // as PictureRun.Capture waits, since the pass that adds the probes' light compiles its
            // pipeline after its program and a picture read before it holds none of that light
            // (GlobalIlluminationTests.EngineDrawsOn).
            .Until("drawn", _ => !supported || GlobalIlluminationTests.EngineDrawsOn(traced: true))
            .Until("ready", _ => !supported || (ready = Render.PipelinesReady() ? ready + 1 : 0) >= 3)
            .Until("referenced", world =>
            {
                if (!supported || samples == 0) return true;
                var ecs = world.Resource<EcsWorld>();
                if (!begun)
                {
                    begun = true;
                    Assert.Null(GiReference.BeginReference(ecs, null, samples));
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

        return (supported, supported ? GiReference.LastPicture!.Value.Light : []);
    }

    /// <summary>
    /// 3DEngine's bounce rooms, each a view of a way light bounces that global illumination is
    /// measured by, in turn the Cornell box, a room of thin walls with a bright lamp outside, a
    /// corridor open at one end, a room lit through a window, red walls at three distances from white
    /// blocks in the sun, a room with a glowing strip, a large room's floor seen at a grazing angle, a
    /// room with a carried lamp, and a dim room lit by glowing blocks alone.
    /// </summary>
    private static class Rooms
    {
        public static readonly (Vec3 Eye, Vec3 Target, float Fov)[] Views =
        [
            (new Vec3(0f, 2.5f, 8f), new Vec3(0f, 2.4f, 0f), 45f),
            (new Vec3(38.3f, 1.4f, 1.6f), new Vec3(41.8f, 1.1f, -0.4f), 60f),
            (new Vec3(87.5f, 1.4f, 0f), new Vec3(72f, 1f, 0f), 60f),
            (new Vec3(122.6f, 1.6f, 2.6f), new Vec3(118f, 0.8f, -1f), 65f),
            (new Vec3(172f, 4f, 9f), new Vec3(162f, 0.6f, -5f), 50f),
            (new Vec3(200f, 1.4f, 2.3f), new Vec3(200f, 1.3f, -2.5f), 65f),
            (new Vec3(234.5f, 0.35f, 5.5f), new Vec3(245f, 0.15f, -5f), 60f),
            (new Vec3(282.6f, 1.7f, 2.6f), new Vec3(278.5f, 1f, -1.5f), 65f),
            (new Vec3(320f, 2.6f, 3.2f), new Vec3(320f, 0f, -0.3f), 60f),
        ];

        /// <summary>The camera's exposure as Bevy works it out at its default of 9.7 EV.</summary>
        private static readonly float Exposure = 1f / (1.2f * MathF.Pow(2f, 9.7f));

        public static void Build(EcsWorld ecs, int view)
        {
            var (eye, target, fov) = Views[view - 1];
            var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f), FieldOfView = fov });
            ecs.Add(camera, Transform.LookingAt(eye, target, Vec3.UnitY));
            Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);

            Sun(ecs, new Vec3(0.8f, -1f, 0.25f), (255, 245, 230), 1.5f);
            Lamp(ecs, new Vec3(0f, 4.2f, 0f), (255, 236, 210), 9f, 12f, shadows: false);
            Lamp(ecs, new Vec3(40f, 2.2f, 0f), (255, 255, 255), 0.6f, 8f, shadows: false);
            Lamp(ecs, new Vec3(43f, 1.5f, 0f), (255, 255, 255), 25f, 10f, shadows: true);
            Lamp(ecs, new Vec3(240f, 2.6f, 0f), (255, 236, 210), 14f, 16f, shadows: false);
            var carried = new Vec3(278f, 2f, -1.5f);
            Lamp(ecs, carried, (255, 230, 200), 8f, 10f, shadows: true);

            var white = Material(ecs, (220, 220, 215));
            var red = Material(ecs, (200, 30, 30));
            var green = Material(ecs, (30, 170, 40));
            var dim = Material(ecs, (60, 60, 58));
            var glow = Material(ecs, (255, 255, 255), (255, 240, 220), 6f);
            var strip = Material(ecs, (255, 255, 255), (255, 230, 190), 30f);
            var lamp = Material(ecs, (230, 190, 110), (255, 200, 120), 4f);

            Box(ecs, "cornell floor", new Vec3(0f, -0.15f, 0f), new Vec3(5.6f, 0.3f, 5.6f), white);
            Box(ecs, "cornell ceiling", new Vec3(0f, 5.15f, 0f), new Vec3(5.6f, 0.3f, 5.6f), white);
            Box(ecs, "cornell back wall", new Vec3(0f, 2.5f, -2.65f), new Vec3(5.6f, 5.6f, 0.3f), white);
            Box(ecs, "cornell red wall", new Vec3(-2.65f, 2.5f, 0f), new Vec3(0.3f, 5.6f, 5.6f), red);
            Box(ecs, "cornell green wall", new Vec3(2.65f, 2.5f, 0f), new Vec3(0.3f, 5.6f, 5.6f), green);
            Box(ecs, "cornell tall block", new Vec3(-0.9f, 1.5f, -0.8f), new Vec3(1.4f, 3f, 1.4f), white, 20f);
            Box(ecs, "cornell short block", new Vec3(1f, 0.7f, 0.7f), new Vec3(1.4f, 1.4f, 1.4f), white, -18f);
            Box(ecs, "cornell lamp panel", new Vec3(0f, 4.97f, 0f), new Vec3(1.6f, 0.06f, 1.6f), glow);

            Room(ecs, "thin", new Vec3(40f, 0f, 0f), new Vec3(4f, 3f, 4f), 0.1f, white);
            Box(ecs, "thin block", new Vec3(40.8f, 0.5f, -0.6f), new Vec3(1f, 1f, 1f), white);

            Room(ecs, "corridor", new Vec3(80f, 0f, 0f), new Vec3(16f, 2.5f, 2f), 0.3f, white, openToward: true);

            Box(ecs, "window floor", new Vec3(120f, -0.15f, 0f), new Vec3(6.6f, 0.3f, 6.6f), white);
            Box(ecs, "window ceiling", new Vec3(120f, 3.15f, 0f), new Vec3(6.6f, 0.3f, 6.6f), white);
            Box(ecs, "window back wall", new Vec3(120f, 1.5f, -3.15f), new Vec3(6.6f, 3f, 0.3f), white);
            Box(ecs, "window front wall", new Vec3(120f, 1.5f, 3.15f), new Vec3(6.6f, 3f, 0.3f), white);
            Box(ecs, "window green wall", new Vec3(123.15f, 1.5f, 0f), new Vec3(0.3f, 3f, 6f), green);
            Box(ecs, "window sill", new Vec3(116.85f, 0.25f, 0f), new Vec3(0.3f, 0.5f, 6f), white);
            Box(ecs, "window lintel", new Vec3(116.85f, 2.8f, 0f), new Vec3(0.3f, 0.4f, 6f), white);
            Box(ecs, "window left jamb", new Vec3(116.85f, 1.55f, -2.5f), new Vec3(0.3f, 2.1f, 1f), white);
            Box(ecs, "window right jamb", new Vec3(116.85f, 1.55f, 2.5f), new Vec3(0.3f, 2.1f, 1f), white);

            Box(ecs, "sunlit floor", new Vec3(165f, -0.15f, -6f), new Vec3(30f, 0.3f, 30f), white);
            float[] apart = [1f, 3f, 6f];
            for (var i = 0; i < 3; i++)
            {
                var z = -i * 5f;
                Box(ecs, $"sunlit block {i + 1}", new Vec3(160f, 0.75f, z), new Vec3(1f, 1.5f, 1f), white);
                Box(ecs, $"sunlit red wall {i + 1}", new Vec3(160.5f + apart[i] + 0.15f, 1.5f, z), new Vec3(0.3f, 3f, 2.5f), red);
            }

            Room(ecs, "strip", new Vec3(200f, 0f, 0f), new Vec3(5f, 3f, 5f), 0.3f, white);
            Box(ecs, "strip", new Vec3(200f, 1.5f, -2.45f), new Vec3(2f, 0.06f, 0.06f), strip);

            Room(ecs, "grazing", new Vec3(240f, 0f, 0f), new Vec3(12f, 3f, 12f), 0.3f, white);

            Room(ecs, "carried", new Vec3(280f, 0f, 0f), new Vec3(6f, 3f, 6f), 0.3f, white);
            Box(ecs, "carried red wall", new Vec3(277.15f, 1.5f, 0f), new Vec3(0.3f, 3f, 6f), red);
            Box(ecs, "carried green panel", new Vec3(280f, 1.25f, -1f), new Vec3(0.2f, 2.5f, 2.5f), green);
            var bulb = ecs.Spawn();
            Render.SetMesh(ecs, bulb, Render.CreateMesh(MeshShape.Sphere, 0.08f));
            Render.SetMaterial(ecs, bulb, Material(ecs, (255, 230, 200)));
            ecs.Add(bulb, Transform.At(carried.X, carried.Y, carried.Z));

            Box(ecs, "dim floor", new Vec3(320f, -0.15f, 0f), new Vec3(8.6f, 0.3f, 8.6f), white);
            Box(ecs, "dim ceiling", new Vec3(320f, 3.15f, 0f), new Vec3(8.6f, 0.3f, 8.6f), dim);
            Box(ecs, "dim back wall", new Vec3(320f, 1.5f, -4.15f), new Vec3(8.6f, 3f, 0.3f), dim);
            Box(ecs, "dim front wall", new Vec3(320f, 1.5f, 4.15f), new Vec3(8.6f, 3f, 0.3f), dim);
            Box(ecs, "dim left wall", new Vec3(315.85f, 1.5f, 0f), new Vec3(0.3f, 3f, 8f), dim);
            Box(ecs, "dim right wall", new Vec3(324.15f, 1.5f, 0f), new Vec3(0.3f, 3f, 8f), dim);
            Box(ecs, "dim glowing block", new Vec3(320f, 0.3f, 0f), new Vec3(0.6f, 0.6f, 0.6f), lamp);
            Box(ecs, "dim screen", new Vec3(321.2f, 0.45f, 0f), new Vec3(0.15f, 0.9f, 1.2f), dim);
            for (var i = 0; i < 4; i++)
                Box(ecs, $"dim glowing block {i + 2}", new Vec3(317.7f + (i & 1) * 0.6f, 0.3f, -2.1f + (i >> 1) * 0.6f), new Vec3(0.6f, 0.6f, 0.6f), lamp);
        }

        // A room's floor, ceiling and four walls of a thickness around a floor's middle, its wall
        // toward -x left open where asked.
        private static void Room(EcsWorld ecs, string name, Vec3 floor, Vec3 size, float thick, AssetHandle color, bool openToward = false)
        {
            var (w, h, d, t) = (size.X, size.Y, size.Z, thick);
            Box(ecs, $"{name} floor", floor + new Vec3(0f, -t / 2f, 0f), new Vec3(w + 2f * t, t, d + 2f * t), color);
            Box(ecs, $"{name} ceiling", floor + new Vec3(0f, h + t / 2f, 0f), new Vec3(w + 2f * t, t, d + 2f * t), color);
            Box(ecs, $"{name} back wall", floor + new Vec3(0f, h / 2f, -d / 2f - t / 2f), new Vec3(w + 2f * t, h, t), color);
            Box(ecs, $"{name} front wall", floor + new Vec3(0f, h / 2f, d / 2f + t / 2f), new Vec3(w + 2f * t, h, t), color);
            if (!openToward) Box(ecs, $"{name} left wall", floor + new Vec3(-w / 2f - t / 2f, h / 2f, 0f), new Vec3(t, h, d), color);
            Box(ecs, $"{name} right wall", floor + new Vec3(w / 2f + t / 2f, h / 2f, 0f), new Vec3(t, h, d), color);
        }

        private static void Box(EcsWorld ecs, string name, Vec3 middle, Vec3 size, AssetHandle material, float turn = 0f)
        {
            var box = ecs.Spawn();
            Render.SetMesh(ecs, box, Render.CreateMesh(MeshShape.Cuboid, size.X, size.Y, size.Z));
            Render.SetMaterial(ecs, box, material);
            var placed = Transform.At(middle.X, middle.Y, middle.Z);
            placed.Rotation = Quat.FromRotationY(turn * MathF.PI / 180f);
            ecs.Add(box, placed);
            ecs.SetName(box, name);
        }

        // A material of an sRGB color, rough as 3DEngine's are by default, glowing at a multiple of
        // white where an emission is given.
        private static AssetHandle Material(EcsWorld ecs, (int R, int G, int B) color, (int R, int G, int B)? emission = null, float intensity = 0f)
        {
            var (r, g, b) = Linear(color);
            var glow = emission is { } e ? Linear(e) : (0f, 0f, 0f);
            return Render.CreateMaterial(new MaterialSettings
            {
                BaseColor = (r, g, b, 1f),
                Roughness = 0.5f,
                Emissive = (glow.Item1 * intensity, glow.Item2 * intensity, glow.Item3 * intensity, 0f),
            });
        }

        // A sun whose light a white surface facing it returns at the given multiple of white.
        private static void Sun(EcsWorld ecs, Vec3 toward, (int R, int G, int B) color, float strength)
        {
            var sun = Render.SpawnLight(new LightSettings
            {
                Kind = LightKind.Directional,
                Color = Linear(color),
                Intensity = strength * MathF.PI / Exposure,
                Shadows = true,
            });
            ecs.Add(sun, Transform.LookingAt(-toward, Vec3.Zero, Vec3.UnitY));
        }

        // A point light whose light a white surface facing it a unit away returns at the given
        // multiple of white, a lamp's luminous power over four pi over pi.
        private static void Lamp(EcsWorld ecs, Vec3 at, (int R, int G, int B) color, float strength, float range, bool shadows)
        {
            var lamp = Render.SpawnLight(new LightSettings
            {
                Kind = LightKind.Point,
                Color = Linear(color),
                Intensity = strength * 4f * MathF.PI * MathF.PI / Exposure,
                Range = range,
                Shadows = shadows,
            });
            ecs.Add(lamp, Transform.At(at.X, at.Y, at.Z));
        }

        private static (float R, float G, float B) Linear((int R, int G, int B) color) => (Decode(color.R), Decode(color.G), Decode(color.B));

        private static float Decode(int value)
        {
            var v = value / 255f;
            return v <= 0.04045f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
        }
    }
}
