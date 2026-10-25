using Bevy;
using Xunit;
using Xunit.Abstractions;

namespace Bevy.Tests;

/// <summary>
/// A planted tree's far level, its thickest limbs as tubes and each limb's foliage on two crossed
/// cards, planned from the grown tree and pictured once a variant by a camera of its own, and drawn
/// past six of the tree's heights in place of the reduced level.
/// </summary>
[Collection("engine")]
public sealed class TreeCardsTests(ITestOutputHelper output)
{
    [Fact]
    public void AFarTreeGathersEachLimbsFoliageOntoTwoCrossedCardsFacingOut()
    {
        foreach (var species in new[] { TreeSpecies.QuakingAspen, TreeSpecies.BlackTupelo, TreeSpecies.CaliforniaBlackOak })
        {
            var skeleton = Trees.Grow(species, 2);
            var leaves = Trees.BuildLeaves(skeleton, species, 2);
            var plan = Trees.PlanFarLevel(skeleton, species, 2)!;
            var cards = plan.Cards;

            // At most sixteen limbs, each with its pair, every leaf on one limb's pair of cards.
            Assert.InRange(cards.Count, 2, 32);
            Assert.Equal(0, cards.Count % 2);
            Assert.Equal(2 * leaves.Positions.Length, cards.Sum(card => card.Leaves.Positions.Length));

            var crown = leaves.Positions.Aggregate(Vec3.Zero, (sum, place) => sum + place) * (1f / leaves.Positions.Length);
            foreach (var card in cards)
            {
                // A card holds what it pictures, faces out of the crown, and is a frame of its own.
                Assert.All(card.Leaves.Positions.Concat(card.Stems.Positions), place =>
                {
                    var away = place - card.Middle;
                    Assert.True(MathF.Abs(Vec3.Dot(away, card.Right)) <= card.HalfWidth + 1e-3f && MathF.Abs(Vec3.Dot(away, card.Up)) <= card.HalfHeight + 1e-3f, $"{place} stands outside its card");
                });
                Assert.True(Vec3.Dot(card.Face, card.Middle - crown) >= -1e-3f, $"a card faces into the crown at {card.Middle}");
                Assert.Equal(card.Face, Vec3.Cross(card.Right, card.Up).Normalized, (a, b) => (a - b).Length < 1e-3f);
            }

            // Each card has a cell of the atlas of its own, and the far level draws a rectangle a
            // card with the way out of the crown.
            Assert.Equal(cards.Count, cards.Select(card => (card.Column, card.Row)).Distinct().Count());
            Assert.All(cards, card => Assert.True(card.Column < plan.Across && card.Row < plan.Across));
            Assert.Equal(cards.Count * 4, plan.Quads.Positions.Length);
            Assert.Equal(cards.Count * 8, plan.Quads.SecondUvs!.Length);
            output.WriteLine($"{species.LeafSet}'s tree, {cards.Count} cards in an atlas {plan.Across} cells a side, {plan.Limbs.Indices!.Length / 3} triangles of limbs");
        }

        // A fern's fronds are its whole leaves and it has no far level.
        Assert.Null(Trees.PlanFarLevel(Trees.Grow(TreeSpecies.Fern, 2), TreeSpecies.Fern, 2));
    }

    [SkippableTheory]
    [InlineData("quaking-aspen")]
    [InlineData("black-tupelo")]
    [InlineData("california-black-oak")]
    public void AFarTreeOfCardsCoversItsCrownAsTheReducedOneDoes(string name)
    {
        Needs.Renderer();

        // A planted tree at eight of its heights, where its far level of cards is drawn, against
        // the same variant's reduced level built by hand at the same place, in the sun under a pale
        // sky. The cards cover the crown within a quarter and are as bright within a fifth.
        var species = Trees.Species(name)!;
        var seed = 0x7E57_0003u;
        var skeleton = Trees.Grow(species, seed);
        var height = skeleton.Bounds.High.Y;
        Entity camera = default, planted = default;
        var reduced = new List<Entity>();
        var run = new PictureRun
        {
            Width = 640,
            Height = 480,
            Frames = 3000,
            Scene = ecs =>
            {
                camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 45f, Far = 5000f });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, height * 0.55f, 0f) + new Vec3(0f, 0.15f, 1f).Normalized * (8f * height), new Vec3(0f, height * 0.55f, 0f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetClearColor((0.55f, 0.68f, 0.85f, 1f));
                Render.SetAmbientLight((0.55f, 0.68f, 0.85f), 1500f);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 30_000f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(new Vec3(6f, 8f, 4f), Vec3.Zero, Vec3.UnitY));

                // Seed three is the fourth variant, turned and sized as the planted tree is.
                planted = Trees.Plant(ecs, name, 3, Vec3.Zero);
                var stems = ecs.Spawn();
                Render.SetMesh(ecs, stems, Modeling.Meshes(Trees.Build(skeleton, TreeDetail.Reduced))[0]);
                Render.SetMaterial(ecs, stems, Trees.BarkMaterial(species));
                var cards = ecs.Spawn();
                var mesh = Render.CreateMesh(Trees.BuildLeaves(skeleton, species, seed, TreeDetail.Reduced));
                Render.GenerateTangents(mesh);
                Render.SetMesh(ecs, cards, mesh);
                Render.SetMaterial(ecs, cards, Trees.LeafMaterial(species));
                foreach (var part in new[] { stems, cards })
                {
                    ecs.Add(part, Transform.Identity);
                    ecs.Add(part, Visibility.Hidden);
                    reduced.Add(part);
                }
            },
        };

        run.Wait(10).Until("ready", _ => Render.PipelinesReady())
            .Until("dressed", world => world.Resource<EcsWorld>().ChildrenOf(planted).Length > 0)
            .Wait(60)
            .Capture("cards")
            .Do("reduced", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                var body = ecs.ChildrenOf(planted)[0];
                ecs.GetRef<Visibility>(planted) = Visibility.Hidden;

                // The reduced level built by hand turned and sized as the planted tree's body is.
                foreach (var part in reduced)
                {
                    ecs.GetRef<Transform>(part) = ecs.GetOrDefault<Transform>(body);
                    ecs.GetRef<Visibility>(part) = Visibility.Inherited;
                }
            })
            .Wait(30)
            .Capture("reduced")
            .Go();

        (int Pixels, float Brightness) Read(CapturedImage picture)
        {
            var sky = picture.At(0, 0);
            var (pixels, sum) = (0, 0f);
            for (var y = 0u; y < picture.Height; y++)
            for (var x = 0u; x < picture.Width; x++)
            {
                var (r, g, b, _) = picture.At(x, y);
                if (Math.Abs(r - sky.R) + Math.Abs(g - sky.G) + Math.Abs(b - sky.B) <= 30) continue;
                pixels++;
                sum += 0.2126f * r + 0.7152f * g + 0.0722f * b;
            }

            return (pixels, pixels > 0 ? sum / pixels : 0f);
        }

        var (far, near) = (Read(run.Picture("cards")), Read(run.Picture("reduced")));
        output.WriteLine($"{name} at eight heights, the cards {far.Pixels} pixels at {far.Brightness:F0} and the reduced level {near.Pixels} at {near.Brightness:F0}");
        Assert.InRange(far.Pixels / (float)near.Pixels, 0.7f, 1.25f);
        Assert.InRange(far.Brightness / near.Brightness, 0.8f, 1.2f);
    }
    [SkippableFact]
    public void TheFarLevelsCostIsReadInADistantForest()
    {
        Needs.Shaders();

        // Forty-eight tupelos planted on a disc 160 meters off, past six of their heights, drawn in
        // their far level of limbs and cards, and the same trees in their reduced level built by
        // hand at the same places, turns and sizes, at 1920 by 1080.
        var species = TreeSpecies.BlackTupelo;
        var reducedOf = new Dictionary<int, (MeshData Stems, MeshData Leaves)>();
        for (var variant = 0; variant < Trees.Variants; variant++)
        {
            var seed = (uint)(0x7E57_0000 + variant);
            var skeleton = Trees.Grow(species, seed);
            reducedOf[variant] = (Modeling.Build(Trees.Build(skeleton, TreeDetail.Reduced))[0], Trees.BuildLeaves(skeleton, species, seed, TreeDetail.Reduced));
        }

        IReadOnlyList<PassTiming> far = [], reduced = [];
        IReadOnlyList<Entity> trees = [];
        var built = new List<Entity>();
        var run = new PictureRun
        {
            Width = 1920,
            Height = 1080,
            Frames = 2400,
            Configure = config => config.GpuTimings = true,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 45f, Far = 5_000f });
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 1.7f, 0f), new Vec3(0f, 12f, -160f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAtmosphere(camera, new AtmosphereSettings());
                Render.SetSkyLighting(camera);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 12_500f, Shadows = true });
                ecs.Add(sun, Transform.LookingAt(new Vec3(-3f, 5f, 2f), Vec3.Zero, Vec3.UnitY));
                trees = Trees.PlantForest(ecs, "black-tupelo", 7, new Vec3(0f, 0f, -160f), 40f, 48);
            },
        };

        run.Wait(10).Until("ready", _ => Render.PipelinesReady())
            .Until("dressed", world => trees.All(tree => world.Resource<EcsWorld>().ChildrenOf(tree).Length > 0))
            .Wait(240)
            .Do("far", _ => far = Render.Timings())
            .Do("reduced", world =>
            {
                var ecs = world.Resource<EcsWorld>();
                var (bark, leaves) = (Trees.BarkMaterial(species), Trees.LeafMaterial(species));
                foreach (var tree in trees)
                {
                    var body = ecs.GetOrDefault<Transform>(ecs.ChildrenOf(tree)[0]);
                    var at = new Transform(ecs.GetOrDefault<Transform>(tree).Translation, body.Rotation, body.Scale);
                    var (stems, crown) = reducedOf[(int)(ecs.GetOrDefault<Tree>(tree).Seed % Trees.Variants)];
                    foreach (var (data, material) in new[] { (stems, bark), (crown, leaves) })
                    {
                        var part = ecs.Spawn();
                        var mesh = Render.CreateMesh(data);
                        Render.GenerateTangents(mesh);
                        Render.SetMesh(ecs, part, mesh);
                        Render.SetMaterial(ecs, part, material);
                        ecs.Add(part, at);
                        built.Add(part);
                    }

                    ecs.GetRef<Visibility>(tree) = Visibility.Hidden;
                }
            })
            .Wait(10).Until("ready again", _ => Render.PipelinesReady()).Wait(240)
            .Do("timings", _ => reduced = Render.Timings())
            .Go();

        double? Pass(IReadOnlyList<PassTiming> timings, string name) => timings.FirstOrDefault(timing => timing.Name == name).GpuMilliseconds;
        string[] passes = ["per_view_shadow_pass", "main_opaque_pass_3d"];
        Skip.If(passes.Any(name => Pass(far, name) is null || Pass(reduced, name) is null), "needs a GPU whose passes the app can time");
        foreach (var name in passes) output.WriteLine($"48 tupelos 160 meters off at 1920 by 1080, {name} {Pass(reduced, name):0.00} ms in their reduced level and {Pass(far, name):0.00} ms in their far one");
    }
}
