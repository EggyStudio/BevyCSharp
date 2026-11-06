using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// A leaf's way out of its crown turned with its tree, so a tree yawed a quarter round reads as its
/// unturned twin seen from a quarter round.
/// </summary>
[Collection("engine")]
public sealed class TreeWayOutTests(ITestOutputHelper output)
{
    /// <summary>
    /// A tree, its eye and its sun turned a quarter round about the trunk together read the picture
    /// the unturned ones read, its leaves' light the same pixel for pixel within a few hundredths.
    /// </summary>
    /// <remarks>
    /// The leaf material lights a leaf's diffuse share by its normal bent toward the way out of the
    /// crown its mesh carries in its second texture coordinates, and read that way as the world's,
    /// so every tree its seed or a level yawed lit its crown as if unturned, the side toward the
    /// sun shaded as the side its yaw had carried there. The vertex stage turns the way as the
    /// mesh's normals are turned.
    /// </remarks>
    [SkippableFact]
    public void ATreeTurnedAQuarterReadsAsItsTwinSeenFromAQuarterRound()
    {
        Needs.Shaders();
        var straight = Seen(0f);
        var turned = Seen(MathF.PI / 2f);

        var (lit, difference, sum) = (0, 0.0, 0.0);
        for (var i = 0; i < straight.Length; i += 3)
        {
            var (a, b) = (straight[i + 1], turned[i + 1]);
            if (a <= 0.01f && b <= 0.01f) continue;
            lit++;
            difference += Math.Abs(a - b);
            sum += a;
        }
        output.WriteLine($"over {lit} lit pixels the turned tree's light differs from the unturned one's by {difference / sum:0.000} of it");
        Assert.True(lit > straight.Length / 3 / 20, $"the tree covers {lit} pixels");
        Assert.True(difference / sum < 0.05, $"the turned tree's light differs by {difference / sum:0.000} of it");
    }

    /// <summary>
    /// The picture's light before tonemapping of an aspen at the origin turned by
    /// <paramref name="turn"/> about the vertical, seen from an eye and lit by a sun turned with
    /// it, in still air under a uniform sky.
    /// </summary>
    private static float[] Seen(float turn)
    {
        string? read = null;
        var asked = false;
        Entity planted = default;
        var round = Quat.FromRotationY(turn);
        var wind = Trees.Wind;
        Trees.Wind = Vec3.Zero;
        try
        {
            new PictureRun
            {
                Width = 192,
                Height = 192,
                Frames = 3000,
                Scene = ecs =>
                {
                    var eye = round * new Vec3(0f, 5f, 16f);
                    var camera = PictureRun.Camera(ecs, eye);
                    ecs.Set(camera, Transform.LookingAt(eye, new Vec3(0f, 5f, 0f), Vec3.UnitY));
                    Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                    Render.SetAmbientLight(camera, (1f, 1f, 1f), 300f);
                    var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f });
                    ecs.Add(sun, Transform.LookingAt(round * new Vec3(8f, 6f, 3f), Vec3.Zero, Vec3.UnitY));
                    planted = Trees.Plant(ecs, "quaking-aspen", 3, Vec3.Zero);
                    ecs.GetRef<Transform>(planted).Rotation = round;
                },
            }
                .Until("ready", _ => Render.PipelinesReady())
                .Until("dressed", world => world.Resource<EcsWorld>().ChildrenOf(planted).Length > 0)
                .Wait(120)
                .Until("read", world => Read(world.Resource<EcsWorld>(), ref asked, ref read))
                .Go();
        }
        finally
        {
            Trees.Wind = wind;
        }

        return GiReference.LastPicture!.Value.Light;
    }

    // Asks for the picture's light once and answers whether the read is done.
    private static bool Read(EcsWorld ecs, ref bool asked, ref string? said)
    {
        if (!asked)
        {
            asked = true;
            Assert.Null(GiReference.BeginPicture(ecs, null));
            return false;
        }
        return (said = GiReference.Step(ecs)) is not null;
    }
}
