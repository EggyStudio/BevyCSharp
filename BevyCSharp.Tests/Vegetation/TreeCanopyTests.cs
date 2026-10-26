using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// A crown's leaves in the scene's distance field as a surface that lets a share of global
/// illumination's light through a meter of it, so the floor under a forest takes the share of the
/// sky's light its canopy lets through.
/// </summary>
[Collection("engine")]
public sealed class TreeCanopyTests(ITestOutputHelper output)
{
    [Fact]
    public void ACrownLetsAboutHalfTheLightThroughAMeter()
    {
        // The leaf area each preset's crown holds in each half meter cell its leaves lie in, the
        // leaf's share of its card, and the light a meter of it lets through by Beer and Lambert's
        // law with half a leaf's area facing a ray, against the share the leaves are given.
        foreach (var species in new[] { TreeSpecies.QuakingAspen, TreeSpecies.BlackTupelo, TreeSpecies.WeepingWillow, TreeSpecies.CaliforniaBlackOak })
        {
            var leaves = Trees.BuildLeaves(Trees.Grow(species, 0x7E57_0003u), species, 0x7E57_0003u);
            var held = new Dictionary<(int, int, int), float>();
            for (var card = 0; card < leaves.Positions.Length / 4; card++)
            {
                var p = leaves.Positions.AsSpan(card * 4, 4);
                var area = Vec3.Cross(p[1] - p[0], p[3] - p[0]).Length * 0.6f;
                var middle = (p[0] + p[1] + p[2] + p[3]) * 0.25f;
                var cell = ((int)MathF.Floor(middle.X * 2f), (int)MathF.Floor(middle.Y * 2f), (int)MathF.Floor(middle.Z * 2f));
                held[cell] = held.GetValueOrDefault(cell) + area;
            }

            var density = held.Values.Sum() / (held.Count * 0.125f);
            var through = MathF.Exp(-0.5f * density);
            output.WriteLine($"{species.LeafSet}'s crown, {density:0.00} square meters of leaf a cubic meter, letting {through:0.00} of the light through a meter");
            Assert.InRange(through, Trees.LeafLightThrough - 0.2f, Trees.LeafLightThrough + 0.2f);
        }
    }

    [SkippableFact]
    public void AForestFloorTakesTheSkyLightItsCanopyLetsThrough()
    {
        Needs.Shaders();

        // Nine tupelos on a grid seven meters apart over a gray floor under a bright sky alone, the
        // floor under the middle tree read from beside its trunk at High, with no trees, with their
        // leaves held solid in the field and with them letting half the light through a meter. The
        // sky is bright enough that the floor under the canopy lies well above the picture's
        // darkest levels, where a few levels of the probes' noise turned the two canopies about.
        float Floor(float? through)
        {
            IReadOnlyList<Entity> trees = [];
            var run = new PictureRun
            {
                Width = 64,
                Height = 64,
                Frames = 4000,
                Scene = ecs =>
                {
                    var eye = new Vec3(1.5f, 1.6f, 1.5f);
                    var camera = PictureRun.Camera(ecs, eye);
                    ecs.Add(camera, Transform.LookingAt(eye, new Vec3(3f, 0f, 3f), Vec3.UnitY));
                    Render.SetAmbientLight(camera, (1f, 1f, 1f), 2400f);
                    var floor = ecs.Spawn();
                    Render.SetMesh(ecs, floor, Render.CreateMesh(MeshShape.Cuboid, 60f, 1f, 60f));
                    Render.SetMaterial(ecs, floor, Render.CreateMaterial(new MaterialSettings { BaseColor = (0.22f, 0.22f, 0.22f, 1f), Roughness = 1f }));
                    ecs.Add(floor, Transform.At(0f, -0.5f, 0f));
                    if (through is { } share)
                    {
                        trees = [.. Enumerable.Range(0, 9).Select(i => Trees.Plant(ecs, "black-tupelo", (uint)(i + 1), new Vec3((i % 3 - 1) * 7f, 0f, (i / 3 - 1) * 7f)))];
                        Render.SetLightThrough(Trees.LeafMaterial(TreeSpecies.BlackTupelo), share);
                    }

                    Render.SetSceneField(2, 0.25f);
                    Render.SetGlobalIllumination(GlobalIllumination.High);
                },
            };

            // Traced for a while after the trees are dressed, which waits on their pictures and may come
            // after the probes have traced many frames without them.
            var dressedAt = 0L;
            run.Until("dressed", world => trees.All(tree => world.Resource<EcsWorld>().ChildrenOf(tree).Length > 0))
                .Do("dressed at", _ => dressedAt = WorldProbes.TracedFrames)
                .Until("traced", _ => WorldProbes.TracedFrames >= dressedAt + 150)
                .Wait(30)
                .Capture("floor")
                .Go();

            var picture = run.Picture("floor");
            var (r, g, b) = (0f, 0f, 0f);
            for (var y = -6; y <= 6; y++)
            for (var x = -6; x <= 6; x++)
            {
                var at = (((int)picture.Height / 2 + y) * (int)picture.Width + (int)picture.Width / 2 + x) * 4;
                (r, g, b) = (r + Linear(picture.Pixels[at]), g + Linear(picture.Pixels[at + 1]), b + Linear(picture.Pixels[at + 2]));
            }

            static float Linear(byte value) => MathF.Pow(value / 255f, 2.2f) / 169f;
            return 0.2126f * r + 0.7152f * g + 0.0722f * b;
        }

        var open = Floor(null);
        var solid = Floor(0f);
        var through = Floor(Trees.LeafLightThrough);
        output.WriteLine($"the floor under nine tupelos at High, {open:0.0000} open, {solid:0.0000} under leaves held solid and {through:0.0000} under leaves letting half through a meter, {(through - solid) / (open - solid):0.00} of what the solid canopy holds back");
        Assert.True(open > solid, $"the canopy keeps some of the sky's light off the floor, {solid:0.0000} against {open:0.0000}");
        Assert.True(through > solid + 0.1f * (open - solid), $"the leaves let the sky's light through to the floor, {through:0.0000} between {solid:0.0000} and {open:0.0000}");
    }
}
