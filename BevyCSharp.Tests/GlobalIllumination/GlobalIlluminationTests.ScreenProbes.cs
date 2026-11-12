using System.Numerics;
using Bevy;
using Xunit;

namespace Bevy.Tests;

// Global illumination's screen probes, the cascades' light read alone and the glow lights read from
// probes standing for them alone where the probes are left out, a probe fed a value that is not a
// number leaving the light it blends into finite, the land far from the world's origin lit as near
// it, and a still view holding a glow light's light from frame to frame.
public sealed partial class GlobalIlluminationTests
{
    /// <summary>
    /// With the screen's probes left out a frame draws the cascades' light alone, as it does with them
    /// traced and left unread, the probes neither made nor traced, and the cascades bring a red wall's
    /// light to a white block's side.
    /// </summary>
    [SkippableFact]
    public void AFrameWithTheScreensProbesLeftOutDrawsTheCascadesLightAlone()
    {
        Needs.Shaders();
        var state = (On: true, GlowOnly: true);
        var eye = new Vec3(-1.9f, 0.6f, 1.5f);
        var run = new PictureRun
        {
            Width = 96,
            Height = 64,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, eye);
                ecs.Add(camera, Transform.LookingAt(eye, new Vec3(0f, 0.5f, 0f), Vec3.UnitY));
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 200f);
                Floor(ecs, (0.8f, 0.8f, 0.8f));
                Block(ecs, (0.8f, 0.05f, 0.05f), new Vec3(-2.2f, 2f, 0f), new Vec3(0.4f, 4f, 8f));
                Block(ecs, (0.9f, 0.9f, 0.9f), new Vec3(0.5f, 0.5f, 0f), new Vec3(1f, 1f, 1f));
                Sun(ecs, new Vec3(4f, 6f, 1f));
                Render.SetSceneField(2, 0.25f);
                Render.SetGlobalIllumination(GlobalIllumination.Low);
            },
        };
        try
        {
            // The cascades read alone through the switch that traces the screen's probes and leaves
            // them unread, then with the probes left out, then with no global illumination.
            run.Until("settled", _ => Settled(3))
                .Until("traced", _ => WorldProbes.TracedFrames >= 60)
                .Until("drawn", _ => EngineDrawsOn(true))
                .Do("leaving the probes unread", _ => WorldProbes.ScreenUnread = true)
                .Wait(10)
                .Capture("unread")
                .Do("leaving the probes out", _ =>
                {
                    WorldProbes.ScreenUnread = false;
                    Render.SetGlobalIlluminationScreenProbes(false);
                })
                .Wait(10)
                .Do("reading the probes' state", _ => state = WorldProbes.ScreenState)
                .Capture("left")
                .Do("turning global illumination off", _ => Render.SetGlobalIllumination(GlobalIllumination.Off))
                .Wait(10)
                .Capture("still")
                .Go();
        }
        finally
        {
            WorldProbes.ScreenUnread = false;
        }

        Assert.False(state.On, "the screen's probes are neither made nor traced");
        var (unread, left, still) = (run.Picture("unread"), run.Picture("left"), run.Picture("still"));
        var differing = 0;
        for (var i = 0; i < left.Pixels.Length; i += 4)
            if (Math.Abs(left.Pixels[i] - unread.Pixels[i]) > 1 || Math.Abs(left.Pixels[i + 1] - unread.Pixels[i + 1]) > 1
                || Math.Abs(left.Pixels[i + 2] - unread.Pixels[i + 2]) > 1)
                differing++;
        Assert.True(differing == 0, $"{differing} pixels read otherwise than the switch's, where the frame reads the cascades' light as it does");
        var (lit, dark) = (Middle(left), Middle(still));
        Assert.True(lit.R > dark.R + 20, $"the cascades bring the wall's light to the block's side, {lit} against {dark}");
    }

    /// <summary>
    /// With the screen's probes left out, probes stand for a glow light alone and a pixel reads the
    /// texels around it, which gives the floor the light a pixel marching to the light itself gives
    /// it, and with no glow light lit none are made.
    /// </summary>
    [SkippableFact]
    public void WithTheScreensProbesLeftOutAGlowLightIsReadFromProbesStandingForItAlone()
    {
        Needs.Shaders();
        var standing = (On: false, GlowOnly: false);
        var gone = (On: true, GlowOnly: true);
        var strip = Entity.None;
        var eye = new Vec3(0f, 1.4f, 2.3f);
        var run = new PictureRun
        {
            Width = 160,
            Height = 96,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0f, 0f, 0f, 1f), FieldOfView = 65f });
                ecs.Add(camera, Transform.LookingAt(eye, new Vec3(0f, 1.2f, -2.5f), Vec3.UnitY));
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                var white = (0.8f, 0.8f, 0.8f);
                Block(ecs, white, new Vec3(0f, -0.15f, 0f), new Vec3(5.6f, 0.3f, 5.6f));
                Block(ecs, white, new Vec3(0f, 3.15f, 0f), new Vec3(5.6f, 0.3f, 5.6f));
                Block(ecs, white, new Vec3(0f, 1.5f, -2.65f), new Vec3(5.6f, 3f, 0.3f));
                Block(ecs, white, new Vec3(-2.65f, 1.5f, 0f), new Vec3(0.3f, 3f, 5.6f));
                Block(ecs, white, new Vec3(2.65f, 1.5f, 0f), new Vec3(0.3f, 3f, 5.6f));
                strip = ecs.Spawn();
                Render.SetMesh(ecs, strip, Render.CreateMesh(MeshShape.Cuboid, 2f, 0.06f, 0.06f));
                Render.SetMaterial(ecs, strip, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (1f, 1f, 1f, 1f),
                    Roughness = 1f,
                    Emissive = (30f, 27f, 22f, 0f),
                }));
                ecs.Add(strip, Transform.At(0f, 1.5f, -2.45f));
                Render.SetSceneField(2, 0.25f);
                Render.SetGlobalIllumination(GlobalIllumination.Low);
            },
        };
        try
        {
            run.Until("settled", _ => Settled(6))
                .Until("traced", _ => WorldProbes.TracedFrames >= 60)
                .Until("drawn", _ => EngineDrawsOn(true))
                .Do("leaving the probes unread", _ => WorldProbes.ScreenUnread = true)
                .Wait(10)
                .Capture("marched")
                .Do("leaving the probes out", _ =>
                {
                    WorldProbes.ScreenUnread = false;
                    Render.SetGlobalIlluminationScreenProbes(false);
                })
                .Wait(40)
                .Do("reading the probes' state", _ => standing = WorldProbes.ScreenState)
                .Capture("read")
                .Do("putting the strip out", world =>
                {
                    var ecs = world.Resource<EcsWorld>();
                    Render.SetMaterial(ecs, strip, Render.CreateMaterial(1f, 1f, 1f, roughness: 1f));
                })
                .Wait(20)
                .Do("reading the probes' state again", _ => gone = WorldProbes.ScreenState)
                .Go();
        }
        finally
        {
            WorldProbes.ScreenUnread = false;
        }

        Assert.True(standing is { On: true, GlowOnly: true }, $"probes stand for the glow lights alone, {standing}");
        Assert.False(gone.On, "with no glow light the screen's probes left out are not made at all");
        var (byMarching, byReading) = (Mean(run.Picture("marched"), 0, 60, 160, 36), Mean(run.Picture("read"), 0, 60, 160, 36));
        Assert.True(byReading.X > 40, $"the strip lights the floor, {byReading}");
        Assert.True(Vector3.Distance(byMarching, byReading) < 6, $"as a pixel marching to it does, {byReading} against {byMarching}");
    }

    /// <summary>
    /// The middle screen probe's light a value that is not a number for three frames, as a normal
    /// that is not a number gives a probe, and then its own again, leaves no pixel the blends
    /// carried it to black.
    /// </summary>
    [SkippableFact]
    public void AScreenProbeFedAValueThatIsNotANumberLeavesTheLightItBlendsIntoFinite()
    {
        Needs.Shaders();
        const int Width = 96, Height = 64, Tile = 16;
        var eye = new Vec3(0f, 1.5f, 4.5f);
        var run = new PictureRun
        {
            Width = Width,
            Height = Height,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, eye);
                ecs.Add(camera, Transform.LookingAt(eye, new Vec3(0f, 1.2f, 0f), Vec3.UnitY));
                Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                Block(ecs, (0.8f, 0.8f, 0.8f), new Vec3(0f, -0.15f, 0f), new Vec3(6f, 0.3f, 6f));
                Block(ecs, (0.86f, 0.08f, 0.08f), new Vec3(-1.6f, 1.5f, 0f), new Vec3(0.3f, 3f, 6f));
                Block(ecs, (0.8f, 0.8f, 0.8f), new Vec3(-0.3f, 0.75f, 0.3f), new Vec3(1f, 1.5f, 1f));
                var lamp = ecs.Spawn();
                Render.SetMesh(ecs, lamp, Render.CreateMesh(MeshShape.Cuboid, 0.2f, 0.2f, 0.2f));
                Render.SetMaterial(ecs, lamp, Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (1f, 1f, 1f, 1f),
                    Roughness = 1f,
                    Emissive = (4f, 3.1f, 1.9f, 0f),
                }));
                ecs.Add(lamp, Transform.At(1.2f, 0.1f, 0.8f));
                var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 60_000f, Range = 10f, Shadows = true });
                ecs.Add(light, Transform.At(0.5f, 2.6f, 0.8f));
                Render.SetSceneField(2, 0.25f);
                Render.SetGlobalIllumination(GlobalIllumination.Low);
            },
        };
        const int across = (Width + Tile - 1) / Tile, down = (Height + Tile - 1) / Tile;
        try
        {
            run.Until("settled", _ => Settled(4))
                .Until("traced", _ => WorldProbes.TracedFrames >= 60)
                .Until("drawn", _ => EngineDrawsOn(true))
                .Capture("before")
                .Do("feeding the middle probe", _ => WorldProbes.FedScreenProbe = down / 2 * across + across / 2 + 1)
                .Wait(3)
                .Do("feeding it no more", _ => WorldProbes.FedScreenProbe = 0)
                .Capture("after")
                .Go();
        }
        finally
        {
            WorldProbes.FedScreenProbe = 0;
        }

        static int Black(CapturedImage image)
        {
            var black = 0;
            for (var i = 0; i < image.Pixels.Length; i += 4)
                if (Math.Max(image.Pixels[i], Math.Max(image.Pixels[i + 1], image.Pixels[i + 2])) < 3) black++;
            return black;
        }

        var (before, after) = (Black(run.Picture("before")), Black(run.Picture("after")));
        Assert.True(after <= before, $"no pixel the probe's non-number reached stays black, {after} black after it where {before} before");
    }

    /// <summary>
    /// A desert village at a field of view of 40 degrees, sand, houses and 56 torches under a low
    /// sun, drawn 10,000 units from the world's origin and at it, leaves no more of the land black
    /// far from the origin than near it. Far from the origin a texel's neighbor read back from the
    /// depth can fall on the same point, which makes the texel's normal a value that is not a
    /// number, and the glow lights' light at the screen's probes would carry it and their history
    /// spread it.
    /// </summary>
    [SkippableFact]
    public void FarFromTheWorldsOriginGlowLightsLeaveNoMoreOfTheLandBlackThanNearIt()
    {
        Needs.Shaders();
        var (near, far) = (BlackAt(0f), BlackAt(10_000f));
        Assert.True(far < near * 1.15 + 50, $"the land far from the origin is as lit as near it, {far} pixels black where {near}");

        static int BlackAt(float offset)
        {
            var at = new Vec3(offset, 0f, -offset);
            var eye = at + new Vec3(0f, 20f, 20f);
            var pitch = -18f * MathF.PI / 180f;
            var run = new PictureRun
            {
                Width = 320,
                Height = 180,
                Frames = 2400,
                Scene = ecs =>
                {
                    var camera = Render.SpawnCamera3d(new CameraSettings { Clear = ClearMode.Custom, ClearColor = (0.59f, 0.76f, 0.94f, 1f), FieldOfView = 40f });
                    ecs.Add(camera, Transform.LookingAt(eye, eye + new Vec3(0f, MathF.Sin(pitch), -MathF.Cos(pitch)), Vec3.UnitY));
                    Render.SetAmbientLight(camera, (1f, 1f, 1f), 0f);
                    var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = true, Intensity = 4000f, Color = (1f, 0.78f, 0.59f) });
                    ecs.Add(sun, Transform.LookingAt(new Vec3(-0.6f, 0.35f, -0.4f), Vec3.Zero, Vec3.UnitY));
                    var sand = (0.86f, 0.81f, 0.64f);
                    Block(ecs, sand, at + new Vec3(0f, -0.5f, -200f), new Vec3(600f, 1f, 600f));
                    var random = new Random(1);
                    for (var i = 0; i < 40; i++)
                        Block(ecs, sand, at + new Vec3(random.Next(-60, 60), 2f, random.Next(-160, -40)), new Vec3(5f, 4f, 5f));
                    var torch = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Roughness = 1f, Emissive = (4f, 3.1f, 1.9f, 0f) });
                    var mesh = Render.CreateMesh(MeshShape.Cuboid, 0.15f, 0.4f, 0.15f);
                    for (var i = 0; i < 56; i++)
                    {
                        var lamp = ecs.Spawn();
                        Render.SetMesh(ecs, lamp, mesh);
                        Render.SetMaterial(ecs, lamp, torch);
                        var place = at + new Vec3(random.Next(-60, 60) + 0.5f, 1.2f, random.Next(-160, -40) + 0.5f);
                        ecs.Add(lamp, Transform.At(place.X, place.Y, place.Z));
                    }
                    Render.SetSceneField(4, 0.25f, 2);
                    Render.SetGlobalIllumination(GlobalIllumination.High);
                },
            };
            run.Until("settled", _ => Settled(97))
                .Until("traced", _ => WorldProbes.TracedFrames >= 120)
                .Until("drawn", _ => EngineDrawsOn(true))
                .Capture("land")
                .Go();
            var picture = run.Picture("land");
            var black = 0;
            for (var i = 0; i < picture.Pixels.Length; i += 4)
                if (picture.Pixels[i] + picture.Pixels[i + 1] + picture.Pixels[i + 2] == 0) black++;
            return black;
        }
    }

    /// <summary>
    /// A still view of a wall seen at a slant and lit by a glow light on the floor beside it alone
    /// holds the wall's light from frame to frame, its middle half's pixels moving by less than a
    /// tenth of a level between frames on average, as the screen's probes the glow light's light is
    /// read from step through their sixteen places within a tile.
    /// </summary>
    /// <remarks>
    /// The glow light's light is blended over the frames a texel's place held still, up to 32. A
    /// texel looking up its place in the frame before at its own middle, a quarter of a texel off
    /// the pixel it takes its point from, would find its light shared with three other texels and
    /// keep little more than a frame's, so a still view's wall would move from frame to frame with
    /// the probes.
    /// </remarks>
    [SkippableFact]
    public void AStillViewOfAWallLitByAGlowLightHoldsItsLightFromFrameToFrame()
    {
        Needs.Shaders();
        const float Size = 8f, High = 3f, T = 0.4f;
        var h = Size / 2f;
        var eye = new Vec3(-1f, 1.5f, 3f);
        var run = new PictureRun
        {
            Width = 320,
            Height = 180,
            Frames = 2400,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, eye);
                ecs.Add(camera, Transform.LookingAt(eye, new Vec3(-h, 1.2f, -2f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAmbientLight((1f, 1f, 1f), 0f);
                var white = (0.787f, 0.787f, 0.787f);
                Block(ecs, white, new Vec3(0f, -T / 2f, 0f), new Vec3(Size + 2f * T, T, Size + 2f * T));
                Block(ecs, white, new Vec3(0f, High + T / 2f, 0f), new Vec3(Size + 2f * T, T, Size + 2f * T));
                Block(ecs, white, new Vec3(-h - T / 2f, High / 2f, 0f), new Vec3(T, High, Size));
                Block(ecs, white, new Vec3(h + T / 2f, High / 2f, 0f), new Vec3(T, High, Size));
                Block(ecs, white, new Vec3(0f, High / 2f, h + T / 2f), new Vec3(Size, High, T));
                Block(ecs, white, new Vec3(0f, High / 2f, -h - T / 2f), new Vec3(Size, High, T));
                var lantern = ecs.Spawn();
                Render.SetMesh(ecs, lantern, Render.CreateMesh(MeshShape.Cuboid, 0.3f, 0.5f, 0.3f));
                Render.SetMaterial(ecs, lantern, Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Roughness = 1f, Emissive = (60f, 48f, 30f, 0f) }));
                ecs.Add(lantern, Transform.At(-h + 0.5f, 0.25f, -1f));
                Render.SetGlobalIllumination(GlobalIllumination.Medium);
            },
        };
        run.Until("settled", _ => Settled(7))
            .Until("traced", _ => WorldProbes.TracedFrames >= 120)
            .Until("drawn", _ => EngineDrawsOn(true))
            .Wait(120);
        for (var i = 0; i < 8; i++) run.Capture($"frame {i}").Wait(1);
        run.Go();

        static double Apart(CapturedImage a, CapturedImage b)
        {
            double sum = 0;
            var count = 0;
            for (var y = a.Height / 4; y < a.Height * 3 / 4; y++)
            for (var x = a.Width / 4; x < a.Width * 3 / 4; x++)
            {
                var at = (int)(y * a.Width + x) * 4;
                sum += Math.Abs(0.2126 * (a.Pixels[at] - b.Pixels[at]) + 0.7152 * (a.Pixels[at + 1] - b.Pixels[at + 1]) + 0.0722 * (a.Pixels[at + 2] - b.Pixels[at + 2]));
                count++;
            }
            return sum / count;
        }

        var apart = Enumerable.Range(0, 7).Average(i => Apart(run.Picture($"frame {i}"), run.Picture($"frame {i + 1}")));
        Assert.True(apart < 0.1, $"a still view holds the glow light's light from frame to frame, its pixels moving by {apart:0.000} of a level on average");
    }

    /// <summary>The mean color of the middle five pixels a side of a picture, in levels.</summary>
    private static (int R, int G, int B) Middle(CapturedImage picture)
    {
        var (r, g, b) = (0, 0, 0);
        for (var y = -2; y <= 2; y++)
        for (var x = -2; x <= 2; x++)
        {
            var at = (((int)picture.Height / 2 + y) * (int)picture.Width + (int)picture.Width / 2 + x) * 4;
            (r, g, b) = (r + picture.Pixels[at], g + picture.Pixels[at + 1], b + picture.Pixels[at + 2]);
        }
        return (r / 25, g / 25, b / 25);
    }

    /// <summary>The mean color of a rectangle of a picture, in levels.</summary>
    private static Vector3 Mean(CapturedImage picture, int left, int top, int width, int height)
    {
        var sum = Vector3.Zero;
        for (var y = top; y < top + height; y++)
        for (var x = left; x < left + width; x++)
        {
            var at = (y * (int)picture.Width + x) * 4;
            sum += new Vector3(picture.Pixels[at], picture.Pixels[at + 1], picture.Pixels[at + 2]);
        }
        return sum / (width * height);
    }
}
