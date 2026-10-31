using System.Numerics;
using System.Runtime.InteropServices;
using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// Covers grass planted from the texture collection's grass cards, its patches, where its cards
/// stand and sway from, and how it takes the light.
/// </summary>
[Collection("engine")]
public sealed class GrassTests(ITestOutputHelper output)
{
    /// <summary>
    /// A patch's cards stand on the ground, two crossed a clump, each as tall as its clump and as
    /// wide as the set's picture is for that height, and both sway streams of each card's vertices
    /// hold its root, the middle of its foot, so the root stays where it stands as the blade sways.
    /// </summary>
    [Fact]
    public void APatchStandsEachCardOnTheGroundWithItsRootInBothSwayStreams()
    {
        var settings = new GrassSettings { Density = 4f, Height = 0.6f, Variation = 0.3f };
        var mesh = Grass.Patch(settings, 2f, 8f, 6f, 7);
        var limbs = MemoryMarshal.Cast<byte, Vector4>(mesh.Streams![0].Data).ToArray();
        var own = MemoryMarshal.Cast<byte, Vector4>(mesh.Streams![1].Data).ToArray();

        // Four a square meter over 8 by 6 meters, a clump a cell of a 16 by 12 grid.
        Assert.Equal(16 * 12 * 2 * 4, mesh.Positions.Length);
        for (var card = 0; card < mesh.Positions.Length / 4; card++)
        {
            var corners = mesh.Positions.AsSpan(card * 4, 4).ToArray();
            var root = (corners[0] + corners[1]) * 0.5f;
            var tall = corners[3].Y;
            Assert.True(corners[0].Y == 0f && corners[1].Y == 0f && MathF.Abs(corners[2].Y - tall) < 1e-6f, $"card {card} stands from {corners[0].Y} to {tall}");
            Assert.InRange(tall, 0.6f * 0.7f - 1e-5f, 0.6f * 1.3f + 1e-5f);
            Assert.Equal(tall * 2f, (corners[1] - corners[0]).Length, 4);
            Assert.InRange(root.X, -4f, 4f);
            Assert.InRange(root.Z, -3f, 3f);
            for (var corner = 0; corner < 4; corner++)
            {
                var (limb, held) = (limbs[card * 4 + corner], own[card * 4 + corner]);
                Assert.True((new Vec3(limb.X, limb.Y, limb.Z) - root).Length < 1e-5f && (new Vec3(held.X, held.Y, held.Z) - root).Length < 1e-5f, $"card {card} sways about {limb} and {held}, its root at {root}");
            }
        }
    }

    /// <summary>The same seed plants the same patch, and another seed another.</summary>
    [Fact]
    public void TheSameSeedPlantsTheSamePatchAndAnotherAnother()
    {
        var settings = new GrassSettings();
        var once = Grass.Patch(settings, 1f, 4f, 4f, 11).Positions;
        Assert.Equal(once, Grass.Patch(settings, 1f, 4f, 4f, 11).Positions);
        Assert.NotEqual(once, Grass.Patch(settings, 1f, 4f, 4f, 12).Positions);
    }

    /// <summary>
    /// Grass over 20 by 12 meters in patches of eight stands as six patches under the entity planted,
    /// the last of each row and column cut to what is left, a run with no renderer drawing none, and
    /// a set that is not grass is refused.
    /// </summary>
    [Fact]
    public void AStretchIsCutIntoPatchesUnderTheEntityPlanted()
    {
        var middles = new List<Vec3>();
        var meshes = 0;
        using var harness = new EngineHarness(frames: 2);
        harness.OnContext(Stage.Startup, ctx =>
        {
            var ecs = ctx.Ecs;
            var grass = Grass.Plant(ecs, new Vec3(5f, 1f, -3f), new Vec2(20f, 12f));
            Assert.Equal("Grass", ecs.NameOf(grass));
            foreach (var patch in ecs.ChildrenOf(grass))
            {
                middles.Add(ecs.GetOrDefault<Transform>(patch).Translation);
                if (Render.MeshOf(ecs, patch).IsValid) meshes++;
            }

            Assert.Throws<ArgumentException>(() => Grass.Plant(ecs, Vec3.Zero, new Vec2(4f, 4f), new GrassSettings { Set = "silver-birch-leaves" }));
            Assert.Throws<ArgumentException>(() => Grass.Plant(ecs, Vec3.Zero, new Vec2(0f, 4f)));
        });
        harness.Run();

        Assert.Equal(6, middles.Count);
        Assert.Equal(0, meshes);
        Assert.Equal([-6f, 2f, 8f], middles.Select(middle => middle.X).Distinct().Order());
        Assert.Equal([-2f, 4f], middles.Select(middle => middle.Z).Distinct().Order());
    }

    /// <summary>
    /// A patch of grass with the sun behind it, seen from the shade side, glows through its blades
    /// as a crown's leaves do, its blades at least a quarter as bright as with the sun at the eye's
    /// back, where a solid card lit from behind takes the ambient light alone.
    /// </summary>
    [SkippableFact]
    public void GrassLitFromBehindLetsTheSunThroughItsBlades()
    {
        Needs.Renderer();

        float Seen(Vec3 sun)
        {
            var run = new PictureRun
            {
                Width = 160,
                Height = 90,
                Scene = ecs =>
                {
                    var camera = PictureRun.Camera(ecs, new Vec3(0f, 0.5f, 3f));
                    ecs.Set(camera, Transform.LookingAt(new Vec3(0f, 0.5f, 3f), new Vec3(0f, 0.35f, 0f), Vec3.UnitY));
                    Render.SetAmbientLight((1f, 1f, 1f), 100f);
                    var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 4_000f });
                    ecs.Add(light, Transform.LookingAt(sun, Vec3.Zero, Vec3.UnitY));
                    Grass.Plant(ecs, Vec3.Zero, new Vec2(2f, 2f), new GrassSettings { Density = 12f, Height = 0.7f });
                },
            };
            run.Until("ready", _ => Render.PipelinesReady()).Wait(ShaderMaterialTests.Settled).Capture("seen").Go();

            // The mean light of the pixels the grass covers, the black clear color left out.
            var picture = run.Picture("seen");
            double sum = 0;
            var count = 0;
            for (var at = 0; at < picture.Pixels.Length; at += 4)
            {
                var light = picture.Pixels[at] + picture.Pixels[at + 1] + picture.Pixels[at + 2];
                if (light < 6) continue;
                sum += light;
                count++;
            }

            Assert.True(count > picture.Pixels.Length / 4 / 5, $"the grass covers {count} pixels");
            return (float)(sum / count);
        }

        var front = Seen(new Vec3(0.3f, 1f, 3f));
        var behind = Seen(new Vec3(-0.3f, 1f, -3f));
        output.WriteLine($"the grass's mean light {front:0.0} lit from the eye's side and {behind:0.0} from behind");
        Assert.True(behind >= front / 4f, $"lit from behind the grass reads {behind:0.0} against {front:0.0} lit from the eye's side");
    }

    /// <summary>
    /// In a strong wind a clump's blades move from frame to frame, the top fifth of the rows it
    /// covers changing between two frames, where in still air two frames are the same. A clump's
    /// roots stand still as its blades sway, since both streams it sways by hold the root
    /// (<see cref="APatchStandsEachCardOnTheGroundWithItsRootInBothSwayStreams"/>) and the sway is
    /// nothing where a vertex is at its root; the picture cannot show that, since the card standing
    /// edge on reaches toward the eye and its near end shows lower as its blades lean.
    /// </summary>
    [SkippableFact]
    public void TheWindMovesTheBladesAndStillAirDoesNot()
    {
        Needs.Renderer();

        var run = new PictureRun
        {
            Width = 160,
            Height = 120,
            Scene = ecs =>
            {
                var camera = PictureRun.Camera(ecs, new Vec3(0f, 0.35f, 1.8f));
                ecs.Set(camera, Transform.LookingAt(new Vec3(0f, 0.35f, 1.8f), new Vec3(0f, 0.35f, 0f), Vec3.UnitY));
                Render.SetAmbientLight((1f, 1f, 1f), 600f);
                var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 20_000f });
                ecs.Add(light, Transform.LookingAt(new Vec3(1f, 2f, 3f), Vec3.Zero, Vec3.UnitY));
                Grass.Plant(ecs, Vec3.Zero, new Vec2(0.2f, 0.2f), new GrassSettings { Density = 25f, Height = 0.7f, Variation = 0f });
            },
        };

        Trees.Wind = Vec3.Zero;
        try
        {
            run.Until("ready", _ => Render.PipelinesReady()).Wait(ShaderMaterialTests.Settled)
                .Capture("still").Wait(13).Capture("still later")
                .Do("blow", _ => Trees.Wind = new Vec3(15f, 0f, 0f)).Wait(30)
                .Capture("blown").Wait(13).Capture("blown later")
                .Go();
        }
        finally
        {
            Trees.Wind = null;
        }

        // The rows the grass covers, those a tenth or more of whose pixels are not the black it stands
        // against, and the share of pixels that changed between two frames in the band's top fifth.
        var (still, blown, later) = (run.Picture("still"), run.Picture("blown"), run.Picture("blown later"));
        var rows = Enumerable.Range(0, (int)still.Height).Where(y => Enumerable.Range(0, (int)still.Width).Count(x => still.At((uint)x, (uint)y) is var p && p.R + p.G + p.B > 6) * 10 >= still.Width).ToArray();
        Assert.True(rows.Length > 20, $"the grass covers {rows.Length} rows");
        var (first, last, fifth) = ((uint)rows[0], (uint)rows[^1] + 1, (uint)(rows.Length / 5));
        Assert.True(Changed(still, run.Picture("still later"), first, last) < 0.002, "the grass moved in still air");

        var top = Changed(blown, later, first, first + fifth);
        output.WriteLine($"blown, {top:P1} of the top rows changed between two frames");
        Assert.True(top > 0.02, $"blown, {top:P1} of the top rows changed between two frames");
    }

    /// <summary>The share of the pixels of rows <paramref name="from"/> to <paramref name="to"/> that differ between two pictures.</summary>
    private static double Changed(CapturedImage a, CapturedImage b, uint from, uint to)
    {
        var (changed, all) = (0, 0);
        for (var y = from; y < to; y++)
        {
            for (var x = 0u; x < a.Width; x++)
            {
                var (p, q) = (a.At(x, y), b.At(x, y));
                if (Math.Abs(p.R - q.R) + Math.Abs(p.G - q.G) + Math.Abs(p.B - q.B) > 24) changed++;
                all++;
            }
        }

        return (double)changed / all;
    }
}
